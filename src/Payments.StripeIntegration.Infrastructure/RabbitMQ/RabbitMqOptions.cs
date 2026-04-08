using static RabbitMQ.Client.ExchangeType;
namespace Payments.StripeIntegration.Infrastructure.RabbitMQ
{
    public class RabbitMqOptions
    {
        public string ExchangeKey { get; set; } = "default";
        public string ExchangeName { get; set; } = "default.exchange";
        public string ExchangeType { get; set; }
        public bool Durable { get; set; } = true;
        public bool AutoDelete { get; set; } = false;
        public bool PersistentMessages { get; set; } = true;
        public bool Mandatory { get; set; } = true;
        public bool HandleUnroutableMessages { get; set; } = true;
        public Dictionary<string, object>? ExchangeArguments { get; set; }
        public Dictionary<string, object>? DefaultHeaders { get; set; }
        public int RetryCount { get; set; } = 3;
        public TimeSpan? RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(2);

        public RabbitMqOptions()
        {
            ExchangeType = Direct;
        }
    }
}
