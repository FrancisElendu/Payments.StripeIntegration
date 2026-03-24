namespace Payments.StripeIntegration.Application.Interfaces
{
    public interface IStripeWebhookService
    {
        Task HandleEventAsync(string jsonPayload, string stripeSignature, CancellationToken ct);
    }
}
