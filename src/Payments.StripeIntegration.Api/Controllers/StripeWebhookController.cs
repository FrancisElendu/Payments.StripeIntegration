using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payments.StripeIntegration.Domain.Events;
using Payments.StripeIntegration.Infrastructure.Persistence;
using Payments.StripeIntegration.Infrastructure.Persistence.Entities;
using Stripe;

namespace Payments.StripeIntegration.Api.Controllers
{
    [Route("api/webhooks/stripe")]
    [ApiController]
    public class StripeWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly IMediator _mediator;

        public StripeWebhookController(
            IMediator mediator,
            ApplicationDbContext db,
            IConfiguration config)
        {
            _mediator = mediator;
            _db = db;
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _config["Stripe:WebhookSecret"]);


            var exists = await _db.StripeEventLogs
            .AnyAsync(x => x.EventId == stripeEvent.Id);

            if (exists)
                return Ok();

            _db.StripeEventLogs.Add(new StripeEventLog
            {
                EventId = stripeEvent.Id,
                EventType = stripeEvent.Type,
                Payload = json,
                Processed = false,
                ReceivedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            await _mediator.Publish(
            new StripeWebhookReceivedEvent(stripeEvent));

            //if (stripeEvent.Type == "payment_intent.succeeded")
            //{
            //    var intent = stripeEvent.Data.Object as PaymentIntent;

            //    var payment = await _db.Payments
            //        .FirstOrDefaultAsync(x => x.StripePaymentIntentId == intent.Id);

            //    payment.MarkSucceeded();

            //    await _db.SaveChangesAsync();
            //}

            return Ok();
        }
    }
}
