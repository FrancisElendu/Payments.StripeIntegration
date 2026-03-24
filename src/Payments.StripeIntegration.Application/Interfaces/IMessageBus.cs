namespace Payments.StripeIntegration.Application.Interfaces
{
    public interface IMessageBus
    {
        Task PublishAsync(string messageType, string payload, CancellationToken ct);
    }
}
