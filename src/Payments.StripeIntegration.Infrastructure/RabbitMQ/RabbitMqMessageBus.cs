using Payments.StripeIntegration.Application.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Payments.StripeIntegration.Infrastructure.RabbitMQ
{
    //OLD Implementation without channel pooling, we will keep this for reference until we have implemented the channel pooling and then we will remove this
    //public class RabbitMqMessageBus : IMessageBus
    //{
    //    private readonly IConnection _connection;

    //    public RabbitMqMessageBus(IConnection connection)
    //    {
    //        _connection = connection;
    //    }

    //    public async Task PublishAsync(string messageType, string payload, CancellationToken ct)
    //    {
    //        using var channel = await _connection.CreateChannelAsync();

    //        await channel.ExchangeDeclareAsync(
    //            exchange: "payments.exchange",
    //            type: ExchangeType.Direct,
    //            durable: true);

    //        var body = Encoding.UTF8.GetBytes(payload);

    //        var properties = new BasicProperties
    //        {
    //            Persistent = true
    //        };

    //        await channel.BasicPublishAsync(
    //            exchange: "payments.exchange",
    //            routingKey: messageType,
    //            mandatory: false,
    //            basicProperties: properties,
    //            body: body,
    //            cancellationToken: ct);
    //    }
    //}

    public class RabbitMqMessageBus : IMessageBus
    {
        private readonly RabbitMqChannelPool _channelPool;
        private readonly IConnection _connection;
        private readonly CreateChannelOptions? options;

        public RabbitMqMessageBus(RabbitMqChannelPool channelPool, IConnection connection)
        {
            _channelPool = channelPool;
            _connection = connection;
        }

        public async Task PublishAsync(string messageType, string payload, CancellationToken ct)
        {
            const int retryCount = 3;

            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                IChannel? channel = null;

                try
                {
                    channel = await _connection.CreateChannelAsync(options, ct);

                    // Prevent duplicate event handlers (IMPORTANT with pooling)
                    channel.BasicReturnAsync -= OnMessageReturnedAsync;
                    channel.BasicReturnAsync += OnMessageReturnedAsync;

                    // Ensure exchange exists
                    await channel.ExchangeDeclareAsync(
                        exchange: "payments.exchange",
                        type: ExchangeType.Direct,
                        durable: true,
                        cancellationToken: ct);

                    var body = Encoding.UTF8.GetBytes(payload);

                    var properties = new BasicProperties
                    {
                        Persistent = true // survive broker restart
                    };

                    // Publish message
                    await channel.BasicPublishAsync(
                        exchange: "payments.exchange",
                        routingKey: messageType,
                        mandatory: true, // required for BasicReturn
                        basicProperties: properties,
                        body: body,
                        cancellationToken: ct);

                    // Success → return channel to pool
                    _channelPool.ReturnChannel(channel);
                    return;
                }
                catch (Exception ex)
                {
                    // Return channel if still usable
                    if (channel != null)
                        _channelPool.ReturnChannel(channel);

                    if (attempt == retryCount)
                        throw;

                    // Exponential backoff
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    await Task.Delay(delay, ct);
                }
            }
        }

        // Handles unroutable messages
        private async Task OnMessageReturnedAsync(object sender, BasicReturnEventArgs args)
        {
            var body = args.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            Console.WriteLine("MESSAGE RETURNED (UNROUTABLE)");
            Console.WriteLine($"RoutingKey: {args.RoutingKey}");
            Console.WriteLine($"ReplyCode: {args.ReplyCode}");
            Console.WriteLine($"ReplyText: {args.ReplyText}");
            Console.WriteLine($"Message: {message}");

            await Task.CompletedTask;

            // PRODUCTION: Persist this to DB (Outbox retry or DLQ)
            // Example:
            // SaveToFailedMessagesStore(message, args.RoutingKey);
        }
    }
}
