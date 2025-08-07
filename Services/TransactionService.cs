using fraude_pix.Dtos;
using fraude_pix.Models;
using fraude_pix.Data;
using fraude_pix.Mappers;

namespace fraude_pix.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _context;

        public TransactionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TransactionModel> CreateTransactionAsync(TransactionDto dto)
        {
            // Validações manuais
            if (dto.SenderId == Guid.Empty || dto.ReceiverID == Guid.Empty)
                throw new ArgumentException("SenderId e ReceiverId são obrigatórios.");

            if (dto.SenderId == dto.ReceiverID)
                throw new ArgumentException("Sender e Receiver não podem ser iguais.");

            if (string.IsNullOrWhiteSpace(dto.PixKey))
                throw new ArgumentException("PixKey é obrigatória.");

            if (dto.Amount <= 0)
                throw new ArgumentException("Amount deve ser maior que zero.");

            if (dto.Timestamp > DateTime.UtcNow)
                throw new ArgumentException("Timestamp não pode ser no futuro.");

            // Mapeia e salva
            var transaction = TransactionMapper.ToModel(dto);

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }
    }
}
