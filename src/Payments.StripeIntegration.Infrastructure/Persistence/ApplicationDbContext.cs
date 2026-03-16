using Microsoft.EntityFrameworkCore;
using Payments.StripeIntegration.Application.Interfaces;
using Payments.StripeIntegration.Domain.Entities;
using Payments.StripeIntegration.Infrastructure.Persistence.Entities;
using Payments.StripeIntegration.Shared;
using System.Text.Json;

namespace Payments.StripeIntegration.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public DbSet<Payment> Payments { get; set; }

        //public DbSet<StripeEventLog> StripeEvents { get; set; }
        public DbSet<StripeEventLog> StripeEventLogs { get; set; }
        

        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            var domainEvents = ChangeTracker
                .Entries<BaseEntity>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

            foreach (var domainEvent in domainEvents)
            {
                OutboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = domainEvent.GetType().AssemblyQualifiedName,
                    Content = JsonSerializer.Serialize(domainEvent),
                    OccurredOn = DateTime.UtcNow
                });
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
