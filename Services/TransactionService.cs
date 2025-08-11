using fraude_pix.Data;
using fraude_pix.Dtos;
using fraude_pix.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace fraude_pix.Services
{
    public class TransactionService
    {
        private readonly AppDbContext _context;
        private readonly List<string> _blacklistedPixKeys = new() { "suspeito@fraude.com", "12345678900" };

        public TransactionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TransactionModel?> CreateTransactionAsync(TransactionDto dto)
        {
            ValidateTransaction(dto);

            // 🔍 Checa se é fraude
            if (await IsFraud(dto))
            {
                var fraudLog = new FraudLog
                {
                    TransactionId = Guid.NewGuid(), // Ainda não há transação real
                    FraudReason = "Transação suspeita detectada",
                    LoggedAt = DateTime.UtcNow
                };

                _context.FraudLogs.Add(fraudLog);
                await _context.SaveChangesAsync();
                return null;
            }

            var transaction = new TransactionModel
            {
                Id = Guid.NewGuid(),
                SenderId = dto.SenderId,
                ReceiverId = dto.ReceiverID,
                Amount = dto.Amount,
                PixKey = dto.PixKey,
                Timestamp = dto.Timestamp == default ? DateTime.UtcNow : dto.Timestamp
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }

        private void ValidateTransaction(TransactionDto dto)
        {
            if (dto.Amount <= 0)
                throw new ArgumentException("O valor da transação deve ser maior que zero.");
            if (string.IsNullOrWhiteSpace(dto.PixKey))
                throw new ArgumentException("A chave Pix é obrigatória.");
        }

        private async Task<bool> IsFraud(TransactionDto dto)
        {
            var now = dto.Timestamp == default ? DateTime.UtcNow : dto.Timestamp;

            // 1️⃣ Valor acima de 10.000
            if (dto.Amount > 10000)
                return true;

            // 2️⃣ Mesmo remetente e destinatário
            if (dto.SenderId == dto.ReceiverID)
                return true;

            // 3️⃣ Chave Pix inválida
            if (!dto.PixKey.Contains("@") && dto.PixKey.Length < 8)
                return true;

            // 4️⃣ Transações antes das 06:00
            if (now.Hour < 6)
                return true;

            // 5️⃣ Palavras suspeitas na chave Pix
            var suspiciousWords = new[] { "fraude", "teste", "suspeito" };
            if (suspiciousWords.Any(word => dto.PixKey.Contains(word, StringComparison.OrdinalIgnoreCase)))
                return true;

            // 6️⃣ Transação repetida em menos de 1 minuto
            bool hasRecentDuplicate = await _context.Transactions
                .AnyAsync(t => t.PixKey == dto.PixKey &&
                               t.Amount == dto.Amount &&
                               EF.Functions.DateDiffSecond(t.Timestamp, now) <= 60);
            if (hasRecentDuplicate)
                return true;

            // 7️⃣ Mais de 5 transações do mesmo remetente em 1 minuto
            int senderCountLastMinute = await _context.Transactions
                .CountAsync(t => t.SenderId == dto.SenderId &&
                                 EF.Functions.DateDiffSecond(t.Timestamp, now) <= 60);
            if (senderCountLastMinute > 5)
                return true;

            // 8️⃣ Lista negra de chaves Pix
            if (_blacklistedPixKeys.Contains(dto.PixKey))
                return true;

            // 9️⃣ Muitos depósitos pequenos (< 50) no mesmo dia
            int smallTxToday = await _context.Transactions
                .CountAsync(t => t.SenderId == dto.SenderId &&
                                 t.Amount < 50 &&
                                 t.Timestamp.Date == now.Date);
            if (smallTxToday >= 5)
                return true;

            // 🔟 Transação acima de 5000 fora do horário comercial
            if (dto.Amount > 5000 && (now.Hour < 8 || now.Hour > 20))
                return true;

            return false;
        }

        public async Task<List<TransactionModel>> GetAllTransactionsAsync()
        {
            return await _context.Transactions.AsNoTracking().ToListAsync();
        }

        public async Task<List<FraudLog>> GetAllFraudsAsync()
        {
            return await _context.FraudLogs.AsNoTracking().ToListAsync();
        }

        public async Task<FraudLog?> GetFraudLogAsync(Guid id)
        {
            return await _context.FraudLogs.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TransactionModel?> GetTransactionByIdAsync(Guid id)
        {
            return await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<bool> DeleteTransactionAsync(Guid id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
                return false;

            if (transaction.IsFraud)
                throw new Exception("Transações marcadas como fraude não podem ser deletadas.");

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        private bool IsValidPixKey(string pixKey)
        {
            if (string.IsNullOrWhiteSpace(pixKey))
                return false;

            var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            var phoneRegex = @"^\+?[1-9]\d{7,14}$";
            var cpfCnpjRegex = @"^\d{11}$|^\d{14}$";

            return Regex.IsMatch(pixKey, emailRegex) ||
                   Regex.IsMatch(pixKey, phoneRegex) ||
                   Regex.IsMatch(pixKey, cpfCnpjRegex);
        }

        internal async Task GetFraudByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }
    }

}
