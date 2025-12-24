using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using M_Wallet.Data;
using M_Wallet.Shared;
using M_Wallet.Shared.Models;
using System.Text.Json;

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
            .Where(t => t.PersonId.HasValue && personIds.Contains(t.PersonId.Value) && t.Status != TransactionStatus.Canceled)
            .Select(t => new 
            { 
                t.PersonId, 
                t.TotalAmount, 
                t.TransactionDate,
                Profit = t.Items.Sum(i => (i.UnitPrice - i.UnitCost) * i.Quantity) - t.Discount
            })
            .GroupBy(t => t.PersonId)
            .Select(g => new 
            { 
                PersonId = g.Key, 
                Total = g.Sum(t => t.TotalAmount), 
                TotalProfit = g.Sum(t => t.Profit),
                LastDate = g.Max(t => t.TransactionDate) 
            })
            .ToListAsync();

        var driverStats = await _context.Transactions
            .Where(t => t.DriverId.HasValue && personIds.Contains(t.DriverId.Value))
            .GroupBy(t => t.DriverId)
            .Select(g => new 
            { 
                DriverId = g.Key, 
                Pending = g.Count(t => t.Status == TransactionStatus.Pending),
                Completed = g.Count(t => t.Status == TransactionStatus.Completed),
                Canceled = g.Count(t => t.Status == TransactionStatus.Canceled)
            })
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
            var d = driverStats.FirstOrDefault(x => x.DriverId == person.Id);

            decimal totalTransactions = t?.Total ?? 0;
            decimal totalPayments = p?.Total ?? 0;
            
            person.Balance = totalPayments - totalTransactions;
            person.TotalProfit = t?.TotalProfit ?? 0;
            person.TotalSpent = totalTransactions;

            if (d != null)
            {
                person.PendingDeliveries = d.Pending;
                person.CompletedDeliveries = d.Completed;
                person.CanceledDeliveries = d.Canceled;
            }
            
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

        // Calculate Balance
        var totalTransactions = await _context.Transactions
            .Where(t => t.PersonId == id && t.Status != TransactionStatus.Canceled)
            .SumAsync(t => t.TotalAmount);

        var totalPayments = await _context.Payments
            .Where(p => p.PersonId == id)
            .SumAsync(p => p.Amount);

        person.Balance = totalPayments - totalTransactions;

        // Calculate Stats
        person.TotalSpent = totalTransactions;
        person.TotalProfit = await _context.Transactions
            .Where(t => t.PersonId == id && t.Status != TransactionStatus.Canceled)
            .SelectMany(t => t.Items)
            .SumAsync(i => (i.UnitPrice - i.UnitCost) * i.Quantity); // Note: This ignores transaction discount for simplicity, or we should sum (Profit - Discount) per transaction

        // Correct Profit Calculation including discounts
        var transactions = await _context.Transactions
            .Where(t => t.PersonId == id && t.Status != TransactionStatus.Canceled)
            .Include(t => t.Items)
            .ToListAsync();
            
        person.TotalProfit = transactions.Sum(t => t.Items.Sum(i => (i.UnitPrice - i.UnitCost) * i.Quantity) - t.Discount);

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
    public async Task<IActionResult> UpdatePreferences(int id, [FromBody] UserPreferences preferences)
    {
        var person = await _context.People.FindAsync(id);
        if (person == null)
        {
            return NotFound();
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null // Use PascalCase to match C# properties
        };
        person.Preferences = JsonSerializer.Serialize(preferences, options);
        
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
                Description = $"Order #{t.Id}",
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
            .ThenBy(i => i.Type == "Transaction" ? 0 : 1) // Ensure Transaction comes before Payment if dates are equal
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
