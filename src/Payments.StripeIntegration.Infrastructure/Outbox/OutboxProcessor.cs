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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _provider.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var messages = await db.OutboxMessages
                    .Where(x => !x.Processed)
                    .Take(20)
                    .ToListAsync();

                foreach (var message in messages)
                {
                    var type = Type.GetType(message.Type);

                    var domainEvent = JsonSerializer.Deserialize(
                        message.Content,
                        type);

                    await mediator.Publish((INotification)domainEvent);

                    message.Processed = true;
                }

                await db.SaveChangesAsync();

                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
