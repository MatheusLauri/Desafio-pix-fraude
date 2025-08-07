using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace fraude_pix.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PixController : ControllerBase
    {
        [HttpGet]
        public IActionResult PixTransaction()
        {
            var obj = new
            {
                Id = Guid.NewGuid(),
                Amount = 100.00m,
                Currency = "BRL",
                Timestamp = DateTime.UtcNow
            };

            return Ok(obj);
        }

    }
}
