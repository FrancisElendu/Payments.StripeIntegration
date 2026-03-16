using Payments.StripeIntegration.Shared;
using Stripe;

namespace Payments.StripeIntegration.Domain.Events
{
    public class StripeWebhookReceivedEvent : IDomainEvent
    {
        public Event StripeEvent { get; }

        public StripeWebhookReceivedEvent(Event stripeEvent)
        {
            StripeEvent = stripeEvent;
        }
    }
}
