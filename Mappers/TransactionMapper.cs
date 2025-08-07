using fraude_pix.Dtos;
using fraude_pix.Models;

namespace fraude_pix.Mappers
{
    public class TransactionMapper
    {
        public static TransactionModel ToModel(TransactionDto dto)
        {
            return new TransactionModel
            {
                SenderId = dto.SenderId,
                ReceiverId = dto.ReceiverID,
                PixKey = dto.PixKey,
                Amount = dto.Amount,
                Timestamp = dto.Timestamp,
                Description = dto.Description
            };

        }
    }
}
