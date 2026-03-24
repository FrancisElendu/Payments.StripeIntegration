using Microsoft.EntityFrameworkCore;
using Payments.StripeIntegration.Application.Interfaces;
using Payments.StripeIntegration.Infrastructure;
using Payments.StripeIntegration.Infrastructure.Outbox;
using Payments.StripeIntegration.Infrastructure.Persistence;
using Payments.StripeIntegration.Infrastructure.Stripe;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//StripeConfiguration.ApiKey =
//    builder.Configuration["Stripe:SecretKey"];

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddMediatR(cfg =>
//    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Payments.StripeIntegration.Application.AssemblyReference).Assembly));

//builder.Services.AddHostedService<OutboxProcessor>();

//builder.Services.AddScoped<IStripePaymentService, StripePaymentService>();
//builder.Services.AddScoped<IApplicationDbContext>(provider =>
//    provider.GetRequiredService<ApplicationDbContext>());

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
