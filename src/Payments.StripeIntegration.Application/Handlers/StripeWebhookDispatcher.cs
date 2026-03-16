using MediatR;
using Payments.StripeIntegration.Domain.Events;
using Stripe;

namespace Payments.StripeIntegration.Application.Handlers
{
    public class StripeWebhookDispatcher :
    INotificationHandler<StripeWebhookReceivedEvent>
    {
        private readonly IMediator _mediator;

        public StripeWebhookDispatcher(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(
            StripeWebhookReceivedEvent notification,
            CancellationToken ct)
        {
            var stripeEvent = notification.StripeEvent;

            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded
            && stripeEvent.Data.Object is PaymentIntent paymentIntent
            && paymentIntent.Metadata.TryGetValue("PaymentId", out var paymentIdStr)
            && Guid.TryParse(paymentIdStr, out var paymentId))
            {
                await _mediator.Publish(new PaymentSucceededEvent(paymentId, stripeEvent.Id), ct);
            }
            else
            {
                // Log or handle missing metadata / invalid GUID
                // e.g., ILogger or exception tracking
                throw new InvalidOperationException(
                    "PaymentId metadata is missing or invalid on PaymentIntent."
                );
            }

            //switch (stripeEvent.Type)
            //{
            //    case EventTypes.PaymentIntentSucceeded:

            //        // Safely cast to PaymentIntent
            //        if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
            //        {
            //            // Extract your internal PaymentId from metadata
            //            if (paymentIntent.Metadata.TryGetValue("PaymentId", out var paymentIdString)
            //                && Guid.TryParse(paymentIdString, out var paymentId))
            //            {
            //                // Publish the internal domain event
            //                await _mediator.Publish(
            //                    new PaymentSucceededEvent(paymentId, stripeEvent.Id),
            //                    ct
            //                );
            //            }
            //            else
            //            {
            //                // Log or handle missing metadata / invalid GUID
            //                // e.g., ILogger or exception tracking
            //                throw new InvalidOperationException(
            //                    "PaymentId metadata is missing or invalid on PaymentIntent."
            //                );
            //            }
            //        }

            //        break;
            //}
        }
    }
}
