using Payments.StripeIntegration.Application.Interfaces;
using Stripe;

namespace Payments.StripeIntegration.Infrastructure.Stripe
{
    public class StripePaymentService : IStripePaymentService
    {
        public StripePaymentService()
        {
            StripeConfiguration.ApiKey = "sk_test_XXXXXXXX"; // use secret from configuration
        }

        public async Task<string> CreatePaymentIntentAsync(
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

            return intent.Id; // Stripe PaymentIntent Id
        }
    }
}
