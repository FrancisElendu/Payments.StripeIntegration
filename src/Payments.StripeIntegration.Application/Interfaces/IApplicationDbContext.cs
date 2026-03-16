using Microsoft.EntityFrameworkCore;
using Payments.StripeIntegration.Domain.Entities;

namespace Payments.StripeIntegration.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Payment> Payments { get; }

        DbSet<StripeEventLog> StripeEventLogs { get; }


        DbSet<OutboxMessage> OutboxMessages { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
