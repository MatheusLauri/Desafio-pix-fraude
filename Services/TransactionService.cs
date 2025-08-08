using fraude_pix.Data;
using fraude_pix.Dtos;
using fraude_pix.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace fraude_pix.Services
{
    public class TransactionService
    {
        private readonly AppDbContext _context;

        public TransactionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TransactionModel?> CreateTransactionAsync(TransactionDto dto)
        {
            var transaction = new TransactionModel
            {
                Id = Guid.NewGuid(),
                SenderId = dto.SenderId,
                ReceiverId = dto.ReceiverID,
                Amount = dto.Amount,
                PixKey = dto.PixKey,
                Timestamp = dto.Timestamp == default ? DateTime.UtcNow : dto.Timestamp
            };

            var errors = new List<string>();

            if (transaction.SenderId == transaction.ReceiverId)     
                errors.Add("SenderId e ReceiverId não podem ser iguais.");

            if (errors.Any())
                throw new ArgumentException(string.Join(" | ", errors));

            var validationErrors = ValidateTransaction(transaction);

            if (validationErrors.Any())
                throw new ArgumentException(string.Join(" | ", validationErrors));

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Verifica fraude
                bool isFraud = ProcessFraudValidation(transaction);

                if (isFraud)
                {
                    // Salva apenas na tabela de logs de fraude
                    _context.FraudLogs.Add(new FraudLog
                    {
                        TransactionId = transaction.Id,
                        FraudReason = transaction.FraudReason ?? "Motivo não informado",
                        LoggedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    // Salva transação normal
                    _context.Transactions.Add(transaction);
                }

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                // Se foi fraude, não retorna a transação como registrada
                return isFraud ? null : transaction;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public List<string> ValidateTransaction(TransactionModel transaction)
        {
            var errors = new List<string>();

            if (transaction.SenderId == Guid.Empty)
                errors.Add("O SenderId é obrigatório.");

            if (transaction.ReceiverId == Guid.Empty)
                errors.Add("O ReceiverId é obrigatório.");

            if (transaction.Amount <= 0)
                errors.Add("O valor da transação deve ser maior que zero.");

            if (!IsValidPixKey(transaction.PixKey))
                errors.Add("A chave Pix informada é inválida.");

            if (transaction.Timestamp == default)
                errors.Add("A data/hora da transação é obrigatória.");

            return errors;
        }

        private bool ProcessFraudValidation(TransactionModel transaction)
        {
            transaction.IsFraud = false;
            transaction.FraudReason = null;

            if (transaction.Amount > 10000)
            {
                transaction.IsFraud = true;
                transaction.FraudReason = "Valor muito alto para transação Pix.";
            }
            else if (transaction.SenderId == transaction.ReceiverId)
            {
                transaction.IsFraud = true;
                transaction.FraudReason = "SenderId e ReceiverId não podem ser iguais.";
            }
            else if (!IsValidPixKey(transaction.PixKey))
            {
                transaction.IsFraud = true;
                transaction.FraudReason = "Chave Pix inválida.";
            }
            else if (transaction.Timestamp.Hour < 6)
            {
                transaction.IsFraud = true;
                transaction.FraudReason = "Transações não permitidas entre 00:00 e 06:00.";
            }
            else if (transaction.PixKey.Contains("fraude", StringComparison.OrdinalIgnoreCase) ||
                     transaction.PixKey.Contains("scam", StringComparison.OrdinalIgnoreCase))
            {
                transaction.IsFraud = true;
                transaction.FraudReason = "Chave Pix suspeita.";
            }

            return transaction.IsFraud;
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
