using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payments.StripeIntegration.Infrastructure.Persistence;
using Stripe;

namespace Payments.StripeIntegration.Api.Controllers
{
    [Route("api/webhooks/stripe")]
    [ApiController]
    public class StripeWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public StripeWebhookController(
            ApplicationDbContext db,
            IConfiguration config)
        {
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

            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                var intent = stripeEvent.Data.Object as PaymentIntent;

                var payment = await _db.Payments
                    .FirstOrDefaultAsync(x => x.StripePaymentIntentId == intent.Id);

                payment.MarkSucceeded();

                await _db.SaveChangesAsync();
            }

            return Ok();
        }
    }
}
