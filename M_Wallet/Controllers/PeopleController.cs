using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using M_Wallet.Data;
using M_Wallet.Shared;
using M_Wallet.Shared.Models;

namespace M_Wallet.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PeopleController : ControllerBase
{
    private readonly AppDbContext _context;

    public PeopleController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Person>>> GetPeople([FromQuery] string? role = null)
    {
        var query = _context.People.AsQueryable();
        
        if (!string.IsNullOrEmpty(role))
        {
            if (role == "Employee")
            {
                // Include Admin when asking for Employees
                query = query.Where(p => p.Role == "Employee" || p.Role == "Admin");
            }
            else
            {
                query = query.Where(p => p.Role == role);
            }
        }
        
        var people = await query.OrderBy(p => p.Name).ToListAsync();

        var personIds = people.Select(p => p.Id).ToList();

        var transactions = await _context.Transactions
            .Where(t => t.PersonId.HasValue && personIds.Contains(t.PersonId.Value))
            .GroupBy(t => t.PersonId)
            .Select(g => new { PersonId = g.Key, Total = g.Sum(t => t.TotalAmount), LastDate = g.Max(t => t.TransactionDate) })
            .ToListAsync();

        var payments = await _context.Payments
            .Where(p => p.PersonId.HasValue && personIds.Contains(p.PersonId.Value))
            .GroupBy(p => p.PersonId)
            .Select(g => new { PersonId = g.Key, Total = g.Sum(p => p.Amount), LastDate = g.Max(p => p.PaymentDate) })
            .ToListAsync();

        foreach (var person in people)
        {
            var t = transactions.FirstOrDefault(x => x.PersonId == person.Id);
            var p = payments.FirstOrDefault(x => x.PersonId == person.Id);

            decimal totalTransactions = t?.Total ?? 0;
            decimal totalPayments = p?.Total ?? 0;
            
            person.Balance = totalPayments - totalTransactions;
            
            DateTime? lastT = t?.LastDate;
            DateTime? lastP = p?.LastDate;

            if (lastT.HasValue && lastP.HasValue)
            {
                person.LastTransactionDate = lastT > lastP ? lastT : lastP;
            }
            else if (lastT.HasValue)
            {
                person.LastTransactionDate = lastT;
            }
            else if (lastP.HasValue)
            {
                person.LastTransactionDate = lastP;
            }
        }
        
        return people;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Person>> GetPerson(int id)
    {
        var person = await _context.People.FindAsync(id);

        if (person == null)
        {
            return NotFound();
        }

        return person;
    }

    [HttpPost]
    public async Task<ActionResult<Person>> CreatePerson(Person person)
    {
        // Check for duplicate username if provided
        if (!string.IsNullOrEmpty(person.Username))
        {
            if (await _context.People.AnyAsync(p => p.Username == person.Username))
            {
                return BadRequest("Username already exists.");
            }
        }

        person.CreatedAt = DateTime.UtcNow;
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPerson), new { id = person.Id }, person);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePerson(int id, Person person)
    {
        if (id != person.Id)
        {
            return BadRequest();
        }

        _context.Entry(person).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PersonExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpPut("{id}/preferences")]
    public async Task<IActionResult> UpdatePreferences(int id, [FromBody] string preferences)
    {
        var person = await _context.People.FindAsync(id);
        if (person == null)
        {
            return NotFound();
        }

        person.Preferences = preferences;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePerson(int id)
    {
        var person = await _context.People.FindAsync(id);
        if (person == null)
        {
            return NotFound();
        }

        _context.People.Remove(person);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<Person>> Login(LoginRequest request)
    {
        var person = await _context.People
            .FirstOrDefaultAsync(p => p.Username == request.Username && p.Password == request.Password && p.IsActive);

        if (person == null)
        {
            // Try passcode login
            person = await _context.People
                .FirstOrDefaultAsync(p => p.Passcode == request.Username && p.IsActive);
        }

        if (person == null)
        {
            return Unauthorized("Invalid credentials");
        }

        // Prevent Customers from logging in
        if (person.Role == "Customer")
        {
            return Unauthorized("Customers are not allowed to log in.");
        }

        return person;
    }

    [HttpGet("verify/{pattern}")]
    public async Task<ActionResult<Person>> VerifyPattern(string pattern)
    {
        var person = await _context.People
            .FirstOrDefaultAsync(p => p.Passcode == pattern && p.IsActive);

        if (person == null)
        {
            return NotFound();
        }

        return person;
    }

    [HttpGet("{id}/statement")]
    public async Task<ActionResult<IEnumerable<StatementItem>>> GetStatement(int id)
    {
        var transactions = await _context.Transactions
            .Where(t => t.PersonId == id)
            .Select(t => new StatementItem
            {
                Date = t.TransactionDate,
                Description = $"Transaction #{t.Id}",
                Amount = -t.TotalAmount, // Debit
                Type = "Transaction"
            })
            .ToListAsync();

        var payments = await _context.Payments
            .Where(p => p.PersonId == id)
            .Select(p => new StatementItem
            {
                Date = p.PaymentDate,
                Description = $"Payment #{p.Id} ({p.PaymentMethod})",
                Amount = p.Amount, // Credit
                Type = "Payment"
            })
            .ToListAsync();

        var items = transactions.Concat(payments)
            .OrderBy(i => i.Date)
            .ToList();

        decimal balance = 0;
        foreach (var item in items)
        {
            balance += item.Amount;
            item.RunningBalance = balance;
        }

        // Reverse for display (newest first)
        return items.OrderByDescending(i => i.Date).ToList();
    }

    private bool PersonExists(int id)
    {
        return _context.People.Any(e => e.Id == id);
    }
}
