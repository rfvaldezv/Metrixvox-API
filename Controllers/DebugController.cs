using MetrixVox.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MetrixVox.Api.Controllers
{
    [ApiController]
    [Route("api/debug")]
    [AllowAnonymous]
    public class DebugController : ControllerBase
    {
        private readonly TenantDbContext _context;

        public DebugController(TenantDbContext context)
        {
            _context = context;
        }

        [HttpGet("db-info")]
        public async Task<IActionResult> GetDbInfo()
        {
            var dbName = await _context.Database
                .SqlQueryRaw<string>("SELECT DB_NAME() AS Value")
                .FirstOrDefaultAsync();

            var serverName = await _context.Database
                .SqlQueryRaw<string>("SELECT @@SERVERNAME AS Value")
                .FirstOrDefaultAsync();

            var columns = await _context.Database
                .SqlQueryRaw<string>(
                    "SELECT COLUMN_NAME AS Value FROM INFORMATION_SCHEMA.COLUMNS " +
                    "WHERE TABLE_NAME = 'SurveyQuestions' " +
                    "AND COLUMN_NAME IN ('Weight', 'OptionsJson')")
                .ToListAsync();

            return Ok(new
            {
                DatabaseName = dbName,
                ServerName = serverName,
                HasWeightAndOptions = columns.Any(),
                Columns = columns,
            });
        }
    }
}
