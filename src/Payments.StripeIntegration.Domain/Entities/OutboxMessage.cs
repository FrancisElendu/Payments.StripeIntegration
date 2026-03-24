using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payments.StripeIntegration.Domain.Entities
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }

        public string Type { get; set; }

        public string Content { get; set; }

        public DateTime OccurredOn { get; set; }

        public bool Processed { get; set; }
        public bool Processing { get; set; }

        public int RetryCount { get; set; }          // NEW
        public int MaxRetries { get; set; } = 3;     // NEW

        public DateTime? NextRetryAt { get; set; }   // NEW

        public string? Error { get; set; }           // NEW

        public DateTime? ProcessedOn { get; set; }   // NEW

        public bool DeadLettered { get; set; }       // NEW
    }
}
