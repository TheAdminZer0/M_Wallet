using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using M_Wallet.Data;
using M_Wallet.Shared;

namespace M_Wallet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly AppDbContext _context;

    public LogsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetLogs()
    {
        return await _context.AuditLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(1000) // Limit to last 1000 logs for performance
            .ToListAsync();
    }
}
