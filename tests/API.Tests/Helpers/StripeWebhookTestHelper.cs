using Payments.StripeIntegration.Domain.Events;
using Stripe;

namespace API.Tests.Helpers
{
    public static class StripeWebhookTestHelper
    {
        public static StripeWebhookReceivedEvent CreatePaymentIntentSucceededEvent(
            Guid paymentId,
            string stripeEventId = "evt_test_123",
            string paymentIntentId = "pi_test_123")
        {
            var paymentIntent = new PaymentIntent
            {
                Id = paymentIntentId,
                Metadata = new Dictionary<string, string>
                {
                    { "PaymentId", paymentId.ToString() }
                }
            };

            var stripeEvent = new Event
            {
                Id = stripeEventId,
                Type = EventTypes.PaymentIntentSucceeded,
                Data = new EventData
                {
                    Object = paymentIntent
                }
            };

            return new StripeWebhookReceivedEvent(stripeEvent);
        }

        public static StripeWebhookReceivedEvent CreatePaymentIntentPaymentFailedEvent()
        {
            var paymentIntent = new PaymentIntent
            {
                Id = "pi_test_123",
                Metadata = new Dictionary<string, string>()
            };

            var stripeEvent = new Event
            {
                Id = "evt_test_123",
                Type = EventTypes.PaymentIntentPaymentFailed,
                Data = new EventData
                {
                    Object = paymentIntent
                }
            };

            return new StripeWebhookReceivedEvent(stripeEvent);
        }

        public static StripeWebhookReceivedEvent CreatePaymentIntentSucceededEventWithWrongObjectType()
        {
            var stripeEvent = new Event
            {
                Id = "evt_test_123",
                Type = EventTypes.PaymentIntentSucceeded,
                Data = new EventData
                {
                    Object = new Customer() // Wrong object type
                }
            };

            return new StripeWebhookReceivedEvent(stripeEvent);
        }

        public static StripeWebhookReceivedEvent CreatePaymentIntentSucceededEventWithoutMetadata()
        {
            var paymentIntent = new PaymentIntent
            {
                Id = "pi_test_123",
                Metadata = new Dictionary<string, string>()
            };

            var stripeEvent = new Event
            {
                Id = "evt_test_123",
                Type = EventTypes.PaymentIntentSucceeded,
                Data = new EventData
                {
                    Object = paymentIntent
                }
            };

            return new StripeWebhookReceivedEvent(stripeEvent);
        }

        public static StripeWebhookReceivedEvent CreatePaymentIntentSucceededEventWithInvalidMetadata()
        {
            var paymentIntent = new PaymentIntent
            {
                Id = "pi_test_123",
                Metadata = new Dictionary<string, string>
                {
                    { "PaymentId", "invalid-guid" }
                }
            };

            var stripeEvent = new Event
            {
                Id = "evt_test_123",
                Type = EventTypes.PaymentIntentSucceeded,
                Data = new EventData
                {
                    Object = paymentIntent
                }
            };

            return new StripeWebhookReceivedEvent(stripeEvent);
        }

        public static StripeWebhookReceivedEvent CreateStripeEventWithType(string eventType)
        {
            var paymentIntent = new PaymentIntent
            {
                Id = "pi_test_123",
                Metadata = new Dictionary<string, string>()
            };

            var stripeEvent = new Event
            {
                Id = "evt_test_123",
                Type = eventType,
                Data = new EventData
                {
                    Object = paymentIntent
                }
            };

            return new StripeWebhookReceivedEvent(stripeEvent);
        }
    }
}
