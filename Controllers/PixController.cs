using fraude_pix.Dtos;
using fraude_pix.Models;
using fraude_pix.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

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
                var transaction = await _transactionService.CreateTransactionAsync(dto);
                if (transaction == null)
                {
                    return BadRequest(new
                    {
                        message = "Transação suspeita de fraude e não permitida."
                    });
                }

                return Ok(new
                {
                    message = "Transação criada com sucesso!",
                    transaction
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { errors = ex.Message.Split(" | ") });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro interno no servidor", details = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionModel>>> GetAllTransactions()
        {
            var transactions = await _transactionService.GetAllTransactionsAsync();
            return Ok(transactions);
        }

        [HttpGet("fraudes")]
        public async Task<ActionResult<IEnumerable<FraudLog>>> GetAllFrauds()
        {
            var frauds = await _transactionService.GetAllFraudsAsync();
            return Ok(frauds);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransactionById(Guid id)
        {
            var transaction = await _transactionService.GetTransactionByIdAsync(id);
            if (transaction == null)
                return NotFound(new { message = "Transação não encontrada." });

            return Ok(transaction);
        }

        [HttpGet("fraudes/{id}")]
        public async Task<IActionResult> GetFraudById(Guid id)
        {
            var fraud = await _transactionService.GetFraudLogAsync(id);
            if (fraud == null)
                return NotFound(new { message = "Registro de fraude não encontrado." });
            return Ok(fraud);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(Guid id)
        {
            try
            {
                var success = await _transactionService.DeleteTransactionAsync(id);
                if (!success)
                    return NotFound(new { message = "Transação não encontrada." });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
