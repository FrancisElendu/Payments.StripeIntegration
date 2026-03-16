using Microsoft.Extensions.Configuration;
using Payments.StripeIntegration.Application.Interfaces;
using Stripe;

namespace Payments.StripeIntegration.Infrastructure.Stripe
{
    public class StripePaymentService : IStripePaymentService
    {
        public StripePaymentService(IConfiguration configuration)
        {
            // Load the secret key from configuration
            var secretKey = configuration["Stripe:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException("Stripe SecretKey is not configured.");

            StripeConfiguration.ApiKey = secretKey;
        }

        public async Task<PaymentIntent> CreatePaymentIntentAsync(
            decimal amount,
            string currency,
            string customerId,
            string description,
            CancellationToken cancellationToken)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Stripe amount is in cents
                Currency = currency,
                Customer = customerId,
                Description = description,
                PaymentMethodTypes = new List<string> { "card" }
            };

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options, cancellationToken: cancellationToken);

            return intent; // Stripe PaymentIntent Id
        }
    }
}
