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
        private readonly RabbitMqProducer _producer;
        private readonly List<string> _blacklistedPixKeys = new() { "suspeito@fraude.com", "12345678900" };
        public TransactionService(AppDbContext context, RabbitMqProducer producer)
        {
            _context = context;
            _producer = producer;
        }

        public async Task<(TransactionModel Transaction, FraudLog? FraudLog)> CreateTransactionAsync(TransactionDto dto)
        {
            var dtoErrors = ValidateTransactionDto(dto);
            if (dtoErrors.Any())
                throw new ArgumentException(string.Join(" | ", dtoErrors));

            var transaction = new TransactionModel
            {
                Id = Guid.NewGuid(),
                SenderId = dto.SenderId,
                ReceiverId = dto.ReceiverID,
                PixKey = dto.PixKey,
                Amount = dto.Amount,
                Timestamp = dto.Timestamp == default ? DateTime.UtcNow : dto.Timestamp,
                Description = dto.Description
            };

            var isFraud = await IsFraudAsync(transaction);
            transaction.IsFraud = isFraud;

            if (string.IsNullOrWhiteSpace(transaction.FraudReason) && isFraud)
                transaction.FraudReason = "Suspeita detectada por regras internas.";

            using var dbTx = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                FraudLog? fraudLog = null;
                if (isFraud)
                {
                    fraudLog = new FraudLog
                    {
                        Id = Guid.NewGuid(),
                        TransactionId = transaction.Id,
                        FraudReason = transaction.FraudReason ?? "Não informado",
                        LoggedAt = DateTime.UtcNow
                    };

                    _context.FraudLogs.Add(fraudLog);
                    await _context.SaveChangesAsync();
                }

                await dbTx.CommitAsync();

                try
                {
                    var evt = new
                    {
                        transaction.Id,
                        transaction.SenderId,
                        transaction.ReceiverId,
                        transaction.PixKey,
                        transaction.Amount,
                        transaction.Timestamp,
                        transaction.IsFraud,
                        FraudReason = transaction.FraudReason
                    };

                    await _producer.PublishAsync(evt);
                }
                catch
                {
                    // não propagar erro de publish — apenas logue se tiver logging
                }

                return (transaction, fraudLog);
            }
            catch
            {
                await dbTx.RollbackAsync();
                throw;
            }
        }

        #region Validações

        private List<string> ValidateTransactionDto(TransactionDto dto)
        {
            var errors = new List<string>();

            if (dto == null)
            {
                errors.Add("Corpo da requisição é obrigatório.");
                return errors;
            }

            if (dto.SenderId == Guid.Empty)
                errors.Add("SenderId é obrigatório.");

            if (dto.ReceiverID == Guid.Empty)
                errors.Add("ReceiverId é obrigatório.");

            if (dto.SenderId == dto.ReceiverID)
                errors.Add("SenderId e ReceiverId não podem ser iguais.");

            if (dto.Amount <= 0)
                errors.Add("Amount deve ser maior que zero.");

            if (string.IsNullOrWhiteSpace(dto.PixKey))
                errors.Add("PixKey é obrigatória.");

            if (dto.Timestamp != default && dto.Timestamp > DateTime.UtcNow.AddMinutes(1))
                errors.Add("Timestamp não pode ser no futuro.");

            if (!string.IsNullOrEmpty(dto.Description) && dto.Description.Length > 1000)
                errors.Add("Description muito longa.");

            return errors;
        }

        private async Task<bool> IsFraudAsync(TransactionModel transaction)
        {
            transaction.IsFraud = false;
            transaction.FraudReason = null;

            var now = transaction.Timestamp;

            if (transaction.Amount > 10000m)
            {
                transaction.IsFraud = true;
                transaction.FraudReason = "Valor muito alto para transação Pix.";
                return true;
            }

            if (transaction.SenderId == transaction.ReceiverId)
            {
                transaction.IsFraud = true;
                transaction.FraudReason = "SenderId e ReceiverId são iguais.";
                return true;
            }

            if (!IsValidPixKey(transaction.PixKey))
            {
                transaction.IsFraud = true;
                transaction.FraudReason = "Chave Pix inválida.";
                return true;
            }

            if (now.Hour < 6)
            {
                transaction.IsFraud = true;
                transaction.FraudReason = "Transações entre 00:00 e 06:00 são suspeitas.";
                return true;
            }

            var suspicious = new[] { "fraude", "scam", "teste", "suspeito" };
            if (suspicious.Any(w => transaction.PixKey.Contains(w, StringComparison.OrdinalIgnoreCase)))
            {
                transaction.IsFraud = true;
                transaction.FraudReason = "Chave Pix contém termos suspeitos.";
                return true;
            }

            bool hasRecentDuplicate = await _context.Transactions
                .AnyAsync(t => t.PixKey == transaction.PixKey &&
                               t.Amount == transaction.Amount &&
                               EF.Functions.DateDiffSecond(t.Timestamp, transaction.Timestamp) <= 60);
            if (hasRecentDuplicate)
            {
                transaction.IsFraud = true;
                transaction.FraudReason = "Transação duplicada detectada em curto intervalo.";
                return true;
            }

            int senderCount = await _context.Transactions
                .CountAsync(t => t.SenderId == transaction.SenderId &&
                                 EF.Functions.DateDiffSecond(t.Timestamp, transaction.Timestamp) <= 60);
            if (senderCount > 5)
            {
                transaction.IsFraud = true;
                transaction.FraudReason = "Múltiplas transações do mesmo remetente em 1 minuto.";
                return true;
            }

            if (_blacklistedPixKeys.Contains(transaction.PixKey))
            {
                transaction.IsFraud = true;
                transaction.FraudReason = "Chave Pix em blacklist.";
                return true;
            }

            int smallTxToday = await _context.Transactions
                .CountAsync(t => t.SenderId == transaction.SenderId && t.Amount < 50m && t.Timestamp.Date == now.Date);
            if (smallTxToday >= 5)
            {
                transaction.IsFraud = true;
                transaction.FraudReason = "Padrão de muitos depósitos pequenos no mesmo dia.";
                return true;
            }

            if (transaction.Amount > 5000m && (now.Hour < 8 || now.Hour > 20))
            {
                transaction.IsFraud = true;
                transaction.FraudReason = "Valor alto fora do horário comercial.";
                return true;
            }

            return false;
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

        #endregion

        #region Read Methods (já existentes)

        public async Task<List<TransactionModel>> GetAllTransactionsAsync()
            => await _context.Transactions.AsNoTracking().ToListAsync();

        public async Task<List<FraudLog>> GetAllFraudsAsync()
            => await _context.FraudLogs.AsNoTracking().ToListAsync();

        public async Task<FraudLog?> GetFraudLogAsync(Guid id)
            => await _context.FraudLogs.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

        public async Task<TransactionModel?> GetTransactionByIdAsync(Guid id)
            => await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

        public async Task<bool> DeleteTransactionAsync(Guid id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return false;
            if (transaction.IsFraud) throw new InvalidOperationException("Transações marcadas como fraude não podem ser deletadas.");
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion
    }
}