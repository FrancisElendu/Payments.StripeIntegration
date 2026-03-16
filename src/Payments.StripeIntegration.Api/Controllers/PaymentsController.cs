using MediatR;
using Microsoft.AspNetCore.Mvc;
using Payments.StripeIntegration.Application.Commands;

namespace Payments.StripeIntegration.Api.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PaymentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("create-intent")]
        public async Task<IActionResult> CreateIntent(
            [FromBody] CreatePaymentIntentCommand command)
        {
            var clientSecret = await _mediator.Send(command);

            return Ok(new
            {
                ClientSecret = clientSecret
            });
        }
    }
}
