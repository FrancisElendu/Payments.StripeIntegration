using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Payments.StripeIntegration.Application.Interfaces;
using Payments.StripeIntegration.Infrastructure.Outbox;
using Payments.StripeIntegration.Infrastructure.Persistence;
using Payments.StripeIntegration.Infrastructure.RabbitMQ;
using Payments.StripeIntegration.Infrastructure.Stripe;
using RabbitMQ.Client;

namespace Payments.StripeIntegration.Infrastructure
{
    public static class DependencyInjection
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            // Add RabbitMQ options
            services.Configure<RabbitMqOptions>(config.GetSection("RabbitMQ"));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
            
            services.AddScoped<IStripePaymentService, StripePaymentService>();
            
            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());

            services.AddHostedService<OutboxProcessor>();

            services.AddScoped<IStripeWebhookService, StripeWebhookService>();

            services.AddSingleton<IConnection>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetRequiredService<ILogger<RabbitMqChannelPool>>();

                var factory = new ConnectionFactory
                {
                    HostName = config["RabbitMQ:Host"],
                    UserName = config["RabbitMQ:Username"],
                    Password = config["RabbitMQ:Password"],
                    VirtualHost = config["RabbitMQ:VirtualHost"] ?? "/",
                    Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
                    //DispatchConsumersAsync = true // Important for async consumers
                };

                logger.LogInformation("Creating RabbitMQ connection to {Host}:{Port}", factory.HostName, factory.Port);

                // Safe to block ONLY at startup
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            services.AddSingleton<RabbitMqChannelPool>();
            services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
        }
    }
}
