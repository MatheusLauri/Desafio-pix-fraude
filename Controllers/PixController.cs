using fraude_pix.Dtos;
using fraude_pix.Models;
using fraude_pix.Services;
using Microsoft.AspNetCore.Mvc;

namespace fraude_pix.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PixController : ControllerBase
    {
        private readonly TransactionService _transactionService;

        public PixController(TransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] TransactionDto dto)
        {
            if (dto == null)
                return BadRequest(new { errors = new[] { "O corpo da requisição não pode ser nulo." } });

            try
            {
                var (transaction, fraudLog) = await _transactionService.CreateTransactionAsync(dto);

                var message = transaction.IsFraud
                    ? "Transação criada e marcada como fraude."
                    : "Transação criada com sucesso.";

                return CreatedAtAction(nameof(GetTransactionById), new { id = transaction.Id }, new
                {
                    message,
                    transaction,
                    fraud = fraudLog == null ? null : new { fraudLog.Id, fraudLog.FraudReason, fraudLog.LoggedAt }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { errors = ex.Message.Split(" | ") });
            }
            catch (Exception ex)
            {
                // ideal: registrar em um logger antes de devolver
                return StatusCode(500, new { message = "Erro interno no servidor", details = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTransactions()
            => Ok(await _transactionService.GetAllTransactionsAsync());

        [HttpGet("fraudes")]
        public async Task<IActionResult> GetAllFrauds()
            => Ok(await _transactionService.GetAllFraudsAsync());

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetTransactionById(Guid id)
        {
            var tx = await _transactionService.GetTransactionByIdAsync(id);
            return tx == null ? NotFound(new { message = "Transação não encontrada." }) : Ok(tx);
        }

        [HttpGet("fraudes/{id:guid}")]
        public async Task<IActionResult> GetFraudById(Guid id)
        {
            var fraud = await _transactionService.GetFraudLogAsync(id);
            return fraud == null ? NotFound(new { message = "Registro de fraude não encontrado." }) : Ok(fraud);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTransaction(Guid id)
        {
            try
            {
                var ok = await _transactionService.DeleteTransactionAsync(id);
                return ok ? NoContent() : NotFound(new { message = "Transação não encontrada." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno", details = ex.Message });
            }
        }
    }
}
    