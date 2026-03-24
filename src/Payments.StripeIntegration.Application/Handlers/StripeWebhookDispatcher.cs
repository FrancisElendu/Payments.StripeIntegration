using MediatR;
using Payments.StripeIntegration.Application.Interfaces;
using Payments.StripeIntegration.Domain.Entities;
using Payments.StripeIntegration.Domain.Events;
using Stripe;
using System.Text.Json;

namespace Payments.StripeIntegration.Application.Handlers
{
    public class StripeWebhookDispatcher : INotificationHandler<StripeWebhookReceivedEvent>
    {
        //private readonly IMediator _mediator;
        private readonly IApplicationDbContext _db;

        public StripeWebhookDispatcher(IApplicationDbContext db)  //IApplicationDbContext db
        {
            //_mediator = mediator;
            _db = db;
        }

        public async Task Handle(
            StripeWebhookReceivedEvent notification,
            CancellationToken ct)
        {
            var stripeEvent = notification.StripeEvent;

            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded && stripeEvent.Data.Object is PaymentIntent paymentIntent)
            {
                if (!paymentIntent.Metadata.TryGetValue("PaymentId", out var paymentIdString)
                    || !Guid.TryParse(paymentIdString, out var paymentId))
                {
                    throw new InvalidOperationException(
                        "PaymentId metadata is missing or invalid on PaymentIntent."
                    );
                }

                //await _mediator.Publish(new PaymentSucceededEvent(paymentId, stripeEvent.Id), ct);
                var domainEvent = new PaymentSucceededEvent(paymentId, stripeEvent.Id);

                _db.OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = domainEvent.GetType().AssemblyQualifiedName!,
                    Content = JsonSerializer.Serialize(domainEvent),
                    Processed = false,
                    OccurredOn = DateTime.UtcNow
                });

                await _db.SaveChangesAsync(ct);
            }
        }
    }
}
