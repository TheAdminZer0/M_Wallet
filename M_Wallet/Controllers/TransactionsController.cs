using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using M_Wallet.Data;
using M_Wallet.Shared;

namespace M_Wallet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TransactionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
    {
        return await _context.Transactions
            .Include(t => t.Items)
            .ThenInclude(i => i.Product)
            .Include(t => t.PaymentAllocations)
            .ThenInclude(pa => pa.Payment)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Transaction>> GetTransaction(int id)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
            return NotFound();

        return transaction;
    }

    [HttpGet("person/{personId}")]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetPersonTransactions(int personId)
    {
        return await _context.Transactions
            .Include(t => t.Items)
            .ThenInclude(i => i.Product)
            .Where(t => t.PersonId == personId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Transaction>> CreateTransaction(Transaction transaction)
    {
        try
        {
            // If date is not provided (default), use current time. 
            // Otherwise use the provided date (converted to UTC for consistency)
            if (transaction.TransactionDate == default)
            {
                transaction.TransactionDate = DateTime.UtcNow;
            }
            else
            {
                transaction.TransactionDate = transaction.TransactionDate.ToUniversalTime();
            }
            
            var productNames = new List<string>();

            // Validate and update stock for each item
            foreach (var item in transaction.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                
                if (product == null)
                {
                    return BadRequest($"Product with ID {item.ProductId} not found");
                }

                productNames.Add(product.Name);
                
                if (product.StockQuantity < item.Quantity)
                {
                    return BadRequest($"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, Requested: {item.Quantity}");
                }
                
                // Update stock quantity
                product.StockQuantity -= item.Quantity;
                
                // Capture the cost at the time of sale
                item.UnitCost = product.CostPrice;

                // Calculate subtotal
                item.Subtotal = item.Quantity * item.UnitPrice;
            }
            
            transaction.TotalAmount = transaction.Items.Sum(i => i.Subtotal);

            // Handle Customer Creation/Linking
            if (!string.IsNullOrWhiteSpace(transaction.CustomerName))
            {
                var customerName = transaction.CustomerName.Trim();
                var existingCustomer = await _context.People
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == customerName.ToLower() && c.Role == "Customer");

                if (existingCustomer != null)
                {
                    transaction.PersonId = existingCustomer.Id;
                    // Ensure the name matches exactly the existing record to avoid case discrepancies
                    transaction.CustomerName = existingCustomer.Name; 
                }
                else
                {
                    var newCustomer = new Person
                    {
                        Name = customerName,
                        Role = "Customer",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.People.Add(newCustomer);
                    await _context.SaveChangesAsync(); // Save to generate ID
                    
                    transaction.PersonId = newCustomer.Id;
                    transaction.CustomerName = newCustomer.Name;
                }
            }

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Log the sale
            var itemDetails = string.Join(", ", productNames);
            var customerDisplay = string.IsNullOrWhiteSpace(transaction.CustomerName) ? "Walk-in" : transaction.CustomerName;
            var dateDisplay = transaction.TransactionDate.ToString("yyyy/MM/dd");

            var log = new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                Action = "Sale",
                Entity = "Transaction",
                EntityId = transaction.Id.ToString(),
                EmployeeName = transaction.EmployeeName,
                Description = $"Date: {dateDisplay} | Customer: {customerDisplay} | Items: {itemDetails} | Total: {transaction.TotalAmount:F2} LD",
                Changes = System.Text.Json.JsonSerializer.Serialize(new { 
                    Customer = transaction.CustomerName,
                    Items = transaction.Items.Select(i => new { i.ProductId, i.Quantity, i.UnitPrice }) 
                })
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            // Return a simple response without circular references
            return Ok(new 
            { 
                id = transaction.Id, 
                transactionDate = transaction.TransactionDate,
                totalAmount = transaction.TotalAmount,
                message = "Transaction completed successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(int id, [FromQuery] string? employeeName = null)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Items)
            .Include(t => t.PaymentAllocations)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
        {
            return NotFound();
        }

        // 1. Restore Stock
        foreach (var item in transaction.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.StockQuantity += item.Quantity;
            }
        }

        // 2. Remove Payment Allocations
        if (transaction.PaymentAllocations != null)
        {
            _context.PaymentAllocations.RemoveRange(transaction.PaymentAllocations);
        }

        // 3. Remove Transaction (Items will cascade delete if configured, but let's be safe)
        _context.TransactionItems.RemoveRange(transaction.Items);
        _context.Transactions.Remove(transaction);

        // Log the deletion
        var log = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            Action = "Delete",
            Entity = "Transaction",
            EntityId = transaction.Id.ToString(),
            EmployeeName = !string.IsNullOrEmpty(employeeName) ? employeeName : "System",
            Description = $"Deleted transaction #{transaction.Id} for {transaction.TotalAmount:F2} LD",
            Changes = "Stock restored"
        };
        _context.AuditLogs.Add(log);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTransaction(int id, Transaction transaction)
    {
        if (id != transaction.Id)
        {
            return BadRequest();
        }

        var existingTransaction = await _context.Transactions.FindAsync(id);
        if (existingTransaction == null)
        {
            return NotFound();
        }

        // Only allow updating the Note for now
        existingTransaction.Note = transaction.Note;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Transactions.Any(e => e.Id == id))
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
}