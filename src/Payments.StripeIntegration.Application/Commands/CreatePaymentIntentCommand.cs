using MediatR;

namespace Payments.StripeIntegration.Application.Commands
{
    public class CreatePaymentIntentCommand : IRequest<Guid>
    {
        public decimal Amount { get; }
        public string Currency { get; }
        public string CustomerId { get; } // Optional: Stripe customer
        public string Description { get; }

        public CreatePaymentIntentCommand(decimal amount, string currency, string customerId, string description)
        {
            Amount = amount;
            Currency = currency;
            CustomerId = customerId;
            Description = description;
        }
    }
}
