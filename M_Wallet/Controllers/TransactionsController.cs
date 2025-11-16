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

    [HttpGet("customer/{customerName}")]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetCustomerTransactions(string customerName)
    {
        return await _context.Transactions
            .Include(t => t.Items)
            .ThenInclude(i => i.Product)
            .Where(t => t.CustomerName == customerName)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Transaction>> CreateTransaction(Transaction transaction)
    {
        try
        {
            transaction.TransactionDate = DateTime.UtcNow;
            
            // Validate and update stock for each item
            foreach (var item in transaction.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                
                if (product == null)
                {
                    return BadRequest($"Product with ID {item.ProductId} not found");
                }
                
                if (product.StockQuantity < item.Quantity)
                {
                    return BadRequest($"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, Requested: {item.Quantity}");
                }
                
                // Update stock quantity
                product.StockQuantity -= item.Quantity;
                
                // Calculate subtotal
                item.Subtotal = item.Quantity * item.UnitPrice;
            }
            
            transaction.TotalAmount = transaction.Items.Sum(i => i.Subtotal);

            _context.Transactions.Add(transaction);
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
}