using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Payments.StripeIntegration.Api.Controllers;
using Payments.StripeIntegration.Domain.Entities;
using Payments.StripeIntegration.Domain.Events;
using Payments.StripeIntegration.Infrastructure.Persistence;
using Stripe;
using System.Security.Cryptography;
using System.Text;

namespace API.Tests
{
    public class StripeWebhookControllerTests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly StripeWebhookController _controller;

        public StripeWebhookControllerTests()
        {
            // Create InMemory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            _mediatorMock = new Mock<IMediator>();
            _configMock = new Mock<IConfiguration>();

            _configMock
                .Setup(x => x["Stripe:WebhookSecret"])
                .Returns("whsec_test_secret");

            _controller = new StripeWebhookController(
                _mediatorMock.Object,
                _dbContext,
                _configMock.Object);
        }

        private void SetupHttpContext(string json)
        {
            var secret = "whsec_test_secret";

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var payload = $"{timestamp}.{json}";

            // Generate HMACSHA256 signature
            var secretBytes = Encoding.UTF8.GetBytes(secret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(secretBytes);
            var hash = hmac.ComputeHash(payloadBytes);

            var signature = BitConverter.ToString(hash).Replace("-", "").ToLower();

            var stripeHeader = $"t={timestamp},v1={signature}";

            var httpContext = new DefaultHttpContext();

            httpContext.Request.Body =
                new MemoryStream(Encoding.UTF8.GetBytes(json));

            httpContext.Request.Headers["Stripe-Signature"] = stripeHeader;

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task HandleWebhook_Should_Save_StripeEventLog()
        {
            // Arrange
            var json = """
            {
              "id": "evt_test_webhook",
              "object": "event",
              "api_version": "2026-02-25.clover",
              "created": 1700000000,
              "livemode": false,
              "pending_webhooks": 1,
              "type": "payment_intent.succeeded",
              "request": {
                "id": null,
                "idempotency_key": null
              },
              "data": {
                "object": {
                  "id": "pi_test",
                  "object": "payment_intent",
                  "amount": 1000,
                  "currency": "usd",
                  "status": "succeeded"
                }
              }
            }
            """;

            SetupHttpContext(json);

            // Act
            var result = await _controller.HandleWebhook(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkResult>();

            var log = await _dbContext.StripeEventLogs.FirstOrDefaultAsync();

            log.Should().NotBeNull();
            log.EventId.Should().Be("evt_test_webhook");
        }


        [Fact]
        public async Task HandleWebhook_Should_Save_OutboxMessage_When_New_Event()
        {
            // Arrange
            var json = """
            {
              "id": "evt_test_webhook",
              "object": "event",
              "api_version": "2026-02-25.clover",
              "created": 1700000000,
              "livemode": false,
              "pending_webhooks": 1,
              "type": "payment_intent.succeeded",
              "request": {
                "id": null,
                "idempotency_key": null
              },
              "data": {
                "object": {
                  "id": "pi_test",
                  "object": "payment_intent",
                  "amount": 1000,
                  "currency": "usd",
                  "status": "succeeded"
                }
              }
            }
            """;

            SetupHttpContext(json);

            // Act
            var result = await _controller.HandleWebhook(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkResult>();

            var log = await _dbContext.StripeEventLogs.FirstOrDefaultAsync(x => x.EventId == "evt_test_webhook");
            log.Should().NotBeNull();
            log.EventType.Should().Be("payment_intent.succeeded");

            var outbox = await _dbContext.OutboxMessages.FirstOrDefaultAsync();
            outbox.Should().NotBeNull();
            outbox.Processed.Should().BeFalse();
            outbox.Type.Should().Contain(nameof(StripeWebhookReceivedEvent));
        }

        [Fact]
        public async Task HandleWebhook_Should_Not_Save_Duplicate_StripeEventLog()
        {
            // Arrange
            var json = """
            {
              "id": "evt_duplicate",
              "object": "event",
              "api_version": "2026-02-25.clover",
              "created": 1700000000,
              "livemode": false,
              "pending_webhooks": 1,
              "type": "payment_intent.succeeded",
              "request": {
                "id": null,
                "idempotency_key": null
              },
              "data": {
                "object": {
                  "id": "pi_test",
                  "object": "payment_intent",
                  "amount": 1000,
                  "currency": "usd",
                  "status": "succeeded"
                }
              }
            }
            """;

            // Seed an existing log
            _dbContext.StripeEventLogs.Add(new StripeEventLog
            {
                EventId = "evt_duplicate",
                EventType = "payment_intent.succeeded",
                Payload = json,
                Processed = false,
                ReceivedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            SetupHttpContext(json);

            // Act
            var result = await _controller.HandleWebhook(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkResult>();
            _dbContext.StripeEventLogs.Count().Should().Be(1); // No new log
            _dbContext.OutboxMessages.Count().Should().Be(0); // No new outbox
        }
    }
}
