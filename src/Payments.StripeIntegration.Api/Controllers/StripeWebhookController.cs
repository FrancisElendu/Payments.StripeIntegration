using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payments.StripeIntegration.Application.Interfaces;
using Payments.StripeIntegration.Domain.Entities;
using Payments.StripeIntegration.Domain.Events;
using Stripe;

namespace Payments.StripeIntegration.Api.Controllers
{
    [Route("api/stripe/webhook")]
    [ApiController]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly IMediator _mediator;

        public StripeWebhookController(
            IMediator mediator,
            IApplicationDbContext db,
            IConfiguration config)
        {
            _mediator = mediator;
            _db = db;
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook(CancellationToken cancellationToken)
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            
            //var cancellationToken = HttpContext.RequestAborted;  //can do it this way as well, but we are passing it from the method parameter, so we can use it in the service layer as well

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

            await _db.SaveChangesAsync(cancellationToken);

            await _mediator.Publish(
            new StripeWebhookReceivedEvent(stripeEvent), cancellationToken);

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
