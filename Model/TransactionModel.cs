using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fraude_pix.Models
{
    [Table("transactions")]
    public class TransactionModel   
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SenderId { get; set; }

        [Required]
        public Guid ReceiverId { get; set; }

        [Required]
        [MaxLength(100)]
        public string PixKey { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public string? Description { get; set; }

        public bool IsFraud { get; set; } = false;

        public string? FraudReason { get; set; }
    }
}
