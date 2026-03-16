using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payments.StripeIntegration.Application.Interfaces
{
    public interface IStripePaymentService
    {
        Task<string> CreatePaymentIntentAsync(
        decimal amount,
        string currency,
        string customerId,
        string description,
        CancellationToken cancellationToken);
    }
}
