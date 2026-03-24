using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Payments.StripeIntegration.Application.Interfaces;
using Payments.StripeIntegration.Domain.Entities;
using Payments.StripeIntegration.Domain.Events;
using System.Text.Json;
using Stripe;

namespace Payments.StripeIntegration.Infrastructure.Stripe
{
    public class StripeWebhookService : IStripeWebhookService
    {
        private readonly IApplicationDbContext _db;
        private readonly string _webhookSecret;
        private readonly IMediator _mediator;

        public StripeWebhookService(
            IApplicationDbContext db,
            IConfiguration config,
            IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
            _webhookSecret = config["Stripe:WebhookSecret"]
                ?? throw new InvalidOperationException("Stripe WebhookSecret not configured.");
        }

        public async Task HandleEventAsync(string jsonPayload, string stripeSignature, CancellationToken ct)
        {
            // Construct Stripe Event (mockable for tests)
            var stripeEvent = EventUtility.ConstructEvent(
                jsonPayload,
                stripeSignature,
                _webhookSecret,
                throwOnApiVersionMismatch: false);

            // Avoid duplicate processing
            if (await _db.StripeEventLogs.AnyAsync(x => x.EventId == stripeEvent.Id, ct))
                return;

            _db.StripeEventLogs.Add(new StripeEventLog
            {
                EventId = stripeEvent.Id,
                EventType = stripeEvent.Type,
                Payload = jsonPayload,
                Processed = false,
                ReceivedAt = DateTime.UtcNow
            });

            // Add domain event to Outbox
            var domainEvent = new StripeWebhookReceivedEvent(stripeEvent);
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
