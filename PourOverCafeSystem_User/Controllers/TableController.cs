using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PourOverCafeSystem_User.Database;

namespace PourOverCafeSystem_User.Controllers
{
    [Route("Table")]
    public class TableController : Controller
    {
        private readonly PourOverCoffeeDbContext _context;

        public TableController(PourOverCoffeeDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var tables = await _context.CafeTables
                .Select(t => new {
                    tableId = t.TableId,
                    status = t.Status
                }).ToListAsync();

            return Json(tables);
        }
    }
}
