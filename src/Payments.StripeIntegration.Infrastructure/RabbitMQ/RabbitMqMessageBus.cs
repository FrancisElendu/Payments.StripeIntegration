using Microsoft.Extensions.Configuration;
using Payments.StripeIntegration.Application.Interfaces;
using RabbitMQ.Client;
using System.Text;

namespace Payments.StripeIntegration.Infrastructure.RabbitMQ
{
    public class RabbitMqMessageBus : IMessageBus
    {
        private readonly IConnection _connection;

        public RabbitMqMessageBus(IConnection connection)
        {
            _connection = connection;
        }

        public async Task PublishAsync(string messageType, string payload, CancellationToken ct)
        {
            using var channel = await _connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(
                exchange: "payments.exchange",
                type: ExchangeType.Direct,
                durable: true);

            var body = Encoding.UTF8.GetBytes(payload);

            var properties = new BasicProperties
            {
                Persistent = true
            };

            await channel.BasicPublishAsync(
                exchange: "payments.exchange",
                routingKey: messageType,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: ct);
        }
    }
}
