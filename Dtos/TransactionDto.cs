namespace fraude_pix.Dtos
{
    public class TransactionDto
    {
        public Guid SenderId { get; set; }
        public Guid ReceiverID { get; set; }
        public string PixKey { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Description { get; set; }

    }
}