using MediatR;
using Payments.StripeIntegration.Application.Commands;
using Payments.StripeIntegration.Application.Interfaces;
using Payments.StripeIntegration.Domain.Entities;

namespace Payments.StripeIntegration.Application.Handlers
{
    public class CreatePaymentIntentHandler : IRequestHandler<CreatePaymentIntentCommand, (Guid PaymentId, string ClientSecret)>
    {
        private readonly IApplicationDbContext _db;
        private readonly IStripePaymentService _stripeService;

        public CreatePaymentIntentHandler(
            IApplicationDbContext db,
            IStripePaymentService stripeService)
        {
            _db = db;
            _stripeService = stripeService;
        }

        public async Task<(Guid PaymentId, string ClientSecret)> Handle(CreatePaymentIntentCommand request, CancellationToken cancellationToken)
        {
            // Call Stripe API to create a PaymentIntent
            var intent = await _stripeService.CreatePaymentIntentAsync(
                request.Amount,
                request.Currency,
                request.CustomerId,
                request.Description,
                cancellationToken
            );

            // Create domain entity
            var payment = new Payment(intent.Id, request.Amount, request.Currency);

            // Optional: status already set to "Pending" in constructor
            // Domain event will fire when payment succeeds
            _db.Payments.Add(payment);

            // Persist to database
            await _db.SaveChangesAsync(cancellationToken);

            // Return Payment Id
            return (payment.Id, intent.ClientSecret);
        }
    }
}
