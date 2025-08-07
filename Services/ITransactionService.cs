using fraude_pix.Dtos;
using fraude_pix.Models;

namespace fraude_pix.Services
{
    public interface ITransactionService
    {
        Task<TransactionModel> CreateTransactionAsync(TransactionDto dto);
    }
}
