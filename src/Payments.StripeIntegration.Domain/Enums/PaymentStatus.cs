using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payments.StripeIntegration.Domain.Enums
{
    public enum PaymentStatus
    {
        Pending = 0,
        Succeeded = 1,
        Failed = 2
    }
}
