using MediatR;
using Microsoft.Extensions.Logging;
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
        private readonly RabbitMqOptions _options;
        private readonly ILogger<RabbitMqMessageBus> _logger;
        //private readonly CreateChannelOptions? options;

        public RabbitMqMessageBus(RabbitMqChannelPool channelPool, 
                        IConnection connection,
                        RabbitMqOptions options,
                        ILogger<RabbitMqMessageBus> logger)
        {
            _channelPool = channelPool;
            _connection = connection;
            _options = options;
            _logger = logger;
        }

        public async Task PublishAsync(string messageType, string payload, CancellationToken ct)
        {
            //const int retryCount = 3;
            var retryCount = _options.RetryCount;
            var baseDelay = _options.RetryBaseDelay ?? TimeSpan.FromSeconds(2);

            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                IChannel? channel = null;

                try
                {
                    //var options = new CreateChannelOptions
                    //(
                    //    publisherConfirmationsEnabled: true,        // Enable publisher confirms
                    //    publisherConfirmationTrackingEnabled: true  // Track confirmations for mandatory messages
                    //);
                    
                    //channel = await _connection.CreateChannelAsync(options, ct);
                    channel = await _channelPool.GetChannelAsync(ct);

                    // Prevent duplicate event handlers (IMPORTANT with pooling)
                    channel.BasicReturnAsync -= OnMessageReturnedAsync;
                    channel.BasicReturnAsync += OnMessageReturnedAsync;

                    // Ensure exchange exists
                    await channel.ExchangeDeclareAsync(
                    exchange: _options.ExchangeName,
                    type: _options.ExchangeType,
                    durable: _options.Durable,
                    autoDelete: _options.AutoDelete,
                    arguments: _options.ExchangeArguments,
                    cancellationToken: ct);
                    //await channel.ExchangeDeclareAsync(
                    //    exchange: "payments.exchange",
                    //    type: ExchangeType.Direct,
                    //    durable: true,
                    //    cancellationToken: ct);

                    var body = Encoding.UTF8.GetBytes(payload);

                    var properties = new BasicProperties
                    {
                        Persistent = _options.PersistentMessages,
                        DeliveryMode = (DeliveryModes)(_options.PersistentMessages ? 2 : 1),
                        ContentType = "application/json",
                        MessageId = Guid.NewGuid().ToString(),
                        Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    };

                    if (_options.DefaultHeaders?.Any() == true)
                    {
                        properties.Headers = new Dictionary<string, object>(_options.DefaultHeaders);
                    }

                    //var properties = new BasicProperties
                    //{
                    //    Persistent = true // survive broker restart
                    //};

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(5));  // 5 second timeout

                    // Publish message
                    await channel.BasicPublishAsync(
                    exchange: _options.ExchangeName,
                    routingKey: messageType,
                    mandatory: _options.Mandatory,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: cts.Token);
                    //await channel.BasicPublishAsync(
                    //    exchange: "payments.exchange",
                    //    routingKey: messageType,
                    //    mandatory: true, // required for BasicReturn
                    //    basicProperties: properties,
                    //    body: body,
                    //    cancellationToken: cts.Token);

                    // Success → return channel to pool
                    _channelPool.ReturnChannel(channel);
                    return;
                }

                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // Timeout or cancellation - confirmation not received
                    if (channel != null)
                    {
                        channel.BasicReturnAsync -= OnMessageReturnedAsync;
                        await channel.CloseAsync(ct);
                    }
                    throw;
                }

                catch (Exception ex)
                {
                    // Return channel if still usable
                    if (channel != null)
                    {
                        channel.BasicReturnAsync -= OnMessageReturnedAsync;
                        _channelPool.ReturnChannel(channel);
                    }
                        

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
