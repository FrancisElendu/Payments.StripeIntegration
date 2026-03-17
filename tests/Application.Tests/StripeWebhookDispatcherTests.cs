using FluentAssertions;
using MediatR;
using Moq;
using Payments.StripeIntegration.Application.Handlers;
using Payments.StripeIntegration.Domain.Events;
using Stripe;

namespace Application.Tests
{
    public class StripeWebhookDispatcherTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly StripeWebhookDispatcher _dispatcher;

        public StripeWebhookDispatcherTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _dispatcher = new StripeWebhookDispatcher(_mediatorMock.Object);
        }


        [Fact]
        public async Task Handle_Should_Publish_PaymentSucceededEvent_When_Event_Is_PaymentIntentSucceeded()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var stripeEvent = new Event
            {
                Id = "evt_1",
                Type = EventTypes.PaymentIntentSucceeded,
                Data = new EventData()
                {
                    Object = new PaymentIntent
                    {
                        Id = "pi_1",
                        Metadata = new Dictionary<string, string>
                        {
                            { "PaymentId", paymentId.ToString() }
                        }
                    }
                }
            };

            var notification = new StripeWebhookReceivedEvent(stripeEvent);

            // Act
            await _dispatcher.Handle(notification, CancellationToken.None);

            // Assert
            _mediatorMock.Verify(m => m.Publish(
                It.Is<PaymentSucceededEvent>(e => e.PaymentId == paymentId && e.StripeEventId == "evt_1"),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Not_Publish_When_Event_Is_OtherType()
        {
            // Arrange
            var stripeEvent = new Event
            {
                Id = "evt_2",
                Type = "charge.succeeded", // not PaymentIntentSucceeded
                Data = new EventData
                {
                    Object = new Charge //  not PaymentIntent
                    {
                        Id = "ch_1"
                    }
                }
            };

            var notification = new StripeWebhookReceivedEvent(stripeEvent);

            // Act
            await _dispatcher.Handle(notification, CancellationToken.None);

            // Assert
            _mediatorMock.Verify(
                m => m.Publish(It.IsAny<PaymentSucceededEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_Should_Throw_When_Metadata_Missing_For_PaymentIntentSucceeded()
        {
            // Arrange
            var stripeEvent = new Event
            {
                Id = "evt_3",
                Type = EventTypes.PaymentIntentSucceeded,
                Data = new EventData
                {
                    Object = new PaymentIntent
                    {
                        Metadata = new Dictionary<string, string>() // Initialize to avoid NullReferenceException
                    } 
                }
            };
            var notification = new StripeWebhookReceivedEvent(stripeEvent);

            // Act
            Func<Task> act = async () => await _dispatcher.Handle(notification, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("PaymentId metadata is missing or invalid on PaymentIntent.");
        }
    }
}
