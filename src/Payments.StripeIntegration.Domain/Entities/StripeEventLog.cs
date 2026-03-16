using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payments.StripeIntegration.Domain.Entities
{
    public class StripeEventLog
    {
        [Key]
        public string EventId { get; set; }

        public string EventType { get; set; }

        public string Payload { get; set; }

        public bool Processed { get; set; }

        public DateTime ReceivedAt { get; set; }
    }
}
