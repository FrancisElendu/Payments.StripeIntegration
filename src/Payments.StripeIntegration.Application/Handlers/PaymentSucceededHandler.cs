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
                .FirstAsync(x => x.Id == notification.PaymentId);

            payment.MarkSucceeded();

            await _db.SaveChangesAsync(ct);

            // Example side effects
            // send receipt
            // update ledger
            // trigger fulfillment

            await Task.CompletedTask;
        }
    }
}
