using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Payments.StripeIntegration.Application.Interfaces;

namespace Payments.StripeIntegration.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminRetryController : ControllerBase
    {
        private readonly IApplicationDbContext _db;
        public AdminRetryController(IApplicationDbContext db)
        {
                _db = db;   
        }
        [HttpPost("outbox/retry/{id}")]
        public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
        {
            var message = await _db.OutboxMessages.FindAsync(id);

            if (message == null)
                return NotFound();

            message.RetryCount = 0;
            message.DeadLettered = false;
            message.NextRetryAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return Ok();
        }
    }
}
