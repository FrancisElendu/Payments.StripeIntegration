using MediatR;
using Microsoft.EntityFrameworkCore;
using Payments.StripeIntegration.Application.Interfaces;
using Payments.StripeIntegration.Domain.Enums;
using Payments.StripeIntegration.Domain.Events;

namespace Payments.StripeIntegration.Application.Handlers
{
    public class PaymentSucceededHandler : INotificationHandler<PaymentSucceededEvent>
    {
        private readonly IApplicationDbContext _db;

        public PaymentSucceededHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task Handle(
            PaymentSucceededEvent notification,
            CancellationToken ct)
        {
            var payment = await _db.Payments
                .FirstOrDefaultAsync(x => x.Id == notification.PaymentId, ct);

            if (payment == null)
                throw new InvalidOperationException("Payment not found.");

            // 1. Idempotency check: avoid double-processing
            if (payment.Status == PaymentStatus.Succeeded)
                return;

            payment.StripeEventId = notification.StripeEventId;
            payment.MarkSucceeded();

            await _db.SaveChangesAsync(ct);

            // 2. Mark Stripe webhook as processed
            var webhookLog = await _db.StripeEventLogs
                .FirstOrDefaultAsync(x => x.EventId == notification.StripeEventId, ct);

            if (webhookLog != null && !webhookLog.Processed)
            {
                webhookLog.Processed = true;
                await _db.SaveChangesAsync(ct);
            }
        }
    }
}
