using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Payments.StripeIntegration.Infrastructure.Persistence;
using System.Text.Json;

namespace Payments.StripeIntegration.Infrastructure.Outbox
{
    public class OutboxProcessor : BackgroundService
    {
        private readonly IServiceProvider _provider;

        public OutboxProcessor(IServiceProvider provider)
        {
            _provider = provider;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                using var scope = _provider.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var messages = await db.OutboxMessages
                    .Where(x => !x.Processed && !x.Processing)
                    .OrderBy(x => x.OccurredOn)
                    .Take(20)
                    .ToListAsync(ct);

                if (!messages.Any())
                {
                    await Task.Delay(2000, ct);
                    continue;
                }

                foreach (var message in messages)
                {
                    // Mark message as being processed
                    message.Processing = true;
                }

                await db.SaveChangesAsync(ct);

                foreach (var message in messages)
                {
                    var type = Type.GetType(message.Type);

                    if (type == null)
                        continue;

                    var domainEvent = JsonSerializer.Deserialize(
                        message.Content,
                        type);

                    if (domainEvent is INotification notification)
                    {
                        await mediator.Publish(notification, ct);
                    }

                    message.Processed = true;
                }

                await db.SaveChangesAsync(ct);

                await Task.Delay(2000, ct);
            }
        }
    }
}
