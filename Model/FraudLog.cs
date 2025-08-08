using System;
using System.ComponentModel.DataAnnotations;

namespace fraude_pix.Models
{
    public class FraudLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TransactionId { get; set; }

        public string FraudReason { get; set; }

        public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    }
}
