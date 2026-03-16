using Microsoft.EntityFrameworkCore;
using Payments.StripeIntegration.Domain.Entities;

namespace Payments.StripeIntegration.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Payment> Payments { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
