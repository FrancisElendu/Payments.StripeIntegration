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

            switch (stripeEvent.Type)
            {
                case Events.PaymentIntentSucceeded:

                    var paymentIntent =
                        stripeEvent.Data.Object as PaymentIntent;

                    await _mediator.Publish(
                        new PaymentSucceededEvent(
                            Guid.Parse(paymentIntent.Metadata["PaymentId"])
                        ));

                    break;
            }
        }
    }
}
