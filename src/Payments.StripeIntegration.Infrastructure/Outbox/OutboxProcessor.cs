using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Payments.StripeIntegration.Application.Interfaces;
using Payments.StripeIntegration.Infrastructure.Persistence;

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
                //var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                var now = DateTime.UtcNow;

                var messages = await db.OutboxMessages
                    .Where(x => !x.Processed &&
                        !x.Processing &&
                        !x.DeadLettered &&
                        (x.NextRetryAt == null || x.NextRetryAt <= now))
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
                    try
                    {
                        await bus.PublishAsync(message.Type, message.Content, ct);

                        message.Processed = true;
                        message.ProcessedOn = DateTime.UtcNow;
                        message.Processing = false;
                    }
                    catch (Exception ex)
                    {
                        message.RetryCount++;
                        message.Error = ex.Message;
                        message.Processing = false;

                        if (message.RetryCount >= message.MaxRetries)
                            message.DeadLettered = true;
                    }
                }

                //foreach (var message in messages)
                //{
                //    try
                //    {
                //        var type = Type.GetType(message.Type);

                //        if (type == null)
                //            throw new InvalidOperationException($"Type not found: {message.Type}");

                //        var domainEvent = JsonSerializer.Deserialize(message.Content, type);

                //        if (domainEvent is INotification notification)
                //        {
                //            await mediator.Publish(notification, ct);
                //        }

                //        // SUCCESS
                //        message.Processed = true;
                //        message.ProcessedOn = DateTime.UtcNow;
                //        message.Processing = false;
                //    }
                //    catch (Exception ex)
                //    {
                //        // FAILURE
                //        message.RetryCount++;
                //        message.Error = ex.Message;
                //        message.Processing = false;

                //        if (message.RetryCount >= message.MaxRetries)
                //        {
                //            message.DeadLettered = true; // Move to DLQ
                //        }
                //        else
                //        {
                //            // Exponential backoff
                //            var delaySeconds = Math.Pow(2, message.RetryCount);
                //            message.NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
                //        }
                //    }
                //}

                //foreach (var message in messages)
                //{
                //    var type = Type.GetType(message.Type);

                //    if (type == null)
                //        continue;

                //    var domainEvent = JsonSerializer.Deserialize(
                //        message.Content,
                //        type);

                //    if (domainEvent is INotification notification)
                //    {
                //        await mediator.Publish(notification, ct);
                //    }

                //    message.Processed = true;
                //}

                await db.SaveChangesAsync(ct);

                await Task.Delay(2000, ct);
            }
        }
    }
}
