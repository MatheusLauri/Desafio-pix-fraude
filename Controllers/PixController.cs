using Microsoft.AspNetCore.Mvc;
using fraude_pix.Models;
using fraude_pix.Data;
using Microsoft.EntityFrameworkCore;

namespace fraude_pix.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PixController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PixController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/pix
        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] TransactionModel transaction)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            transaction.Timestamp = DateTime.UtcNow;
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTransactionById), new { id = transaction.Id }, transaction);
        }

        // GET: api/pix
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionModel>>> GetAllTransactions()
        {
            return await _context.Transactions.ToListAsync();
        }

        // GET: api/pix/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionModel>> GetTransactionById(Guid id)
        {
            var transaction = await _context.Transactions.FindAsync(id);

            if (transaction == null)
                return NotFound();

            return transaction;
        }

        // DELETE: api/pix/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(Guid id)
        {
            var transaction = await _context.Transactions.FindAsync(id);

            if (transaction == null)
                return NotFound();

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
