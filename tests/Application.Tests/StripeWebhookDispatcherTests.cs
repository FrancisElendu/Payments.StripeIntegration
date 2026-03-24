using API.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Payments.StripeIntegration.Application.Handlers;
using Payments.StripeIntegration.Application.Interfaces;
using Payments.StripeIntegration.Domain.Entities;
using Payments.StripeIntegration.Domain.Events;
using Stripe;
using System.Text.Json;

namespace Application.Tests
{
    public class StripeWebhookDispatcherTests
    {
        private readonly Mock<IApplicationDbContext> _dbContextMock;
        private readonly Mock<DbSet<OutboxMessage>> _outboxMessagesDbSetMock;
        private readonly StripeWebhookDispatcher _sut;

        public StripeWebhookDispatcherTests()
        {
            _dbContextMock = new Mock<IApplicationDbContext>();
            _outboxMessagesDbSetMock = new Mock<DbSet<OutboxMessage>>();

            _dbContextMock.Setup(x => x.OutboxMessages)
                .Returns(_outboxMessagesDbSetMock.Object);

            _sut = new StripeWebhookDispatcher(_dbContextMock.Object);
        }

        [Fact]
        public async Task Handle_WithPaymentIntentSucceeded_AndValidMetadata_ShouldAddOutboxMessageAndSaveChanges()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var stripeEventId = "evt_test_123";

            var notification = StripeWebhookTestHelper.CreatePaymentIntentSucceededEvent(
                paymentId, stripeEventId);

            OutboxMessage capturedOutboxMessage = null;
            _outboxMessagesDbSetMock.Setup(x => x.Add(It.IsAny<OutboxMessage>()))
                .Callback<OutboxMessage>(msg => capturedOutboxMessage = msg);

            _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _sut.Handle(notification, CancellationToken.None);

            // Assert
            _outboxMessagesDbSetMock.Verify(x => x.Add(It.IsAny<OutboxMessage>()), Times.Once);
            _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            capturedOutboxMessage.Should().NotBeNull();
            capturedOutboxMessage.Id.Should().NotBeEmpty();
            capturedOutboxMessage.Processed.Should().BeFalse();
            capturedOutboxMessage.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            capturedOutboxMessage.Type.Should().Be(typeof(PaymentSucceededEvent).AssemblyQualifiedName);

            var deserializedEvent = JsonSerializer.Deserialize<PaymentSucceededEvent>(capturedOutboxMessage.Content);
            deserializedEvent.Should().NotBeNull();
            deserializedEvent.PaymentId.Should().Be(paymentId);
            deserializedEvent.StripeEventId.Should().Be(stripeEventId);
        }

        [Fact]
        public async Task Handle_WithNonPaymentIntentSucceededEvent_ShouldNotAddOutboxMessage()
        {
            // Arrange
            var notification = StripeWebhookTestHelper.CreatePaymentIntentPaymentFailedEvent();

            // Act
            await _sut.Handle(notification, CancellationToken.None);

            // Assert
            _outboxMessagesDbSetMock.Verify(x => x.Add(It.IsAny<OutboxMessage>()), Times.Never);
            _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithPaymentIntentSucceeded_ButDataObjectIsNotPaymentIntent_ShouldNotAddOutboxMessage()
        {
            // Arrange
            var notification = StripeWebhookTestHelper.CreatePaymentIntentSucceededEventWithWrongObjectType();
            // Act
            await _sut.Handle(notification, CancellationToken.None);

            // Assert
            _outboxMessagesDbSetMock.Verify(x => x.Add(It.IsAny<OutboxMessage>()), Times.Never);
            _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithPaymentIntentSucceeded_ButMissingPaymentIdMetadata_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var notification = StripeWebhookTestHelper.CreatePaymentIntentSucceededEventWithoutMetadata();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.Handle(notification, CancellationToken.None));

            exception.Message.Should().Be("PaymentId metadata is missing or invalid on PaymentIntent.");

            _outboxMessagesDbSetMock.Verify(x => x.Add(It.IsAny<OutboxMessage>()), Times.Never);
            _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithPaymentIntentSucceeded_ButInvalidPaymentIdFormat_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var notification = StripeWebhookTestHelper.CreatePaymentIntentSucceededEventWithInvalidMetadata();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.Handle(notification, CancellationToken.None));

            exception.Message.Should().Be("PaymentId metadata is missing or invalid on PaymentIntent.");

            _outboxMessagesDbSetMock.Verify(x => x.Add(It.IsAny<OutboxMessage>()), Times.Never);
            _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenSaveChangesFails_ShouldThrowExceptionAndNotAddOutboxMessage()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var notification = StripeWebhookTestHelper.CreatePaymentIntentSucceededEvent(paymentId);

            var expectedException = new DbUpdateException("Database error");
            _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
                _sut.Handle(notification, CancellationToken.None));

            exception.Should().Be(expectedException);
            _outboxMessagesDbSetMock.Verify(x => x.Add(It.IsAny<OutboxMessage>()), Times.Once);
            _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(EventTypes.PaymentIntentCreated)]
        [InlineData(EventTypes.PaymentIntentCanceled)]
        [InlineData(EventTypes.PaymentIntentProcessing)]
        [InlineData(EventTypes.ChargeSucceeded)]
        [InlineData(EventTypes.CheckoutSessionCompleted)]
        public async Task Handle_WithOtherStripeEventTypes_ShouldNotAddOutboxMessage(string eventType)
        {
            // Arrange
            var notification = StripeWebhookTestHelper.CreateStripeEventWithType(eventType);

            // Act
            await _sut.Handle(notification, CancellationToken.None);

            // Assert
            _outboxMessagesDbSetMock.Verify(x => x.Add(It.IsAny<OutboxMessage>()), Times.Never);
            _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithCancellationToken_ShouldPassCancellationTokenToSaveChanges()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var notification = StripeWebhookTestHelper.CreatePaymentIntentSucceededEvent(paymentId);

            var cancellationToken = new CancellationToken(true);

            _dbContextMock.Setup(x => x.SaveChangesAsync(cancellationToken))
                .ReturnsAsync(1);

            // Act
            await _sut.Handle(notification, cancellationToken);

            // Assert
            _dbContextMock.Verify(x => x.SaveChangesAsync(cancellationToken), Times.Once);
        }
    }
}
