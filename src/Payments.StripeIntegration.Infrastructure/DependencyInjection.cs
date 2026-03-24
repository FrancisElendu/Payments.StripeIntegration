using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
            services.AddScoped<IStripePaymentService, StripePaymentService>();
            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());

            services.AddHostedService<OutboxProcessor>();

            services.AddSingleton<IMessageBus, RabbitMqMessageBus>();

            services.AddSingleton<IConnection>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();

                var factory = new ConnectionFactory
                {
                    HostName = config["RabbitMQ:Host"],
                    UserName = config["RabbitMQ:Username"],
                    Password = config["RabbitMQ:Password"]
                };

                // Safe to block ONLY at startup
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });
        }
    }
}
