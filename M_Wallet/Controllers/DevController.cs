using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using M_Wallet.Data;
using M_Wallet.Shared;

namespace M_Wallet.Controllers;

/// <summary>
/// Developer tools controller for testing and data manipulation.
/// WARNING: These endpoints should be disabled in production!
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DevController : ControllerBase
{
    private readonly AppDbContext _context;
    private static readonly Random _random = new();

    private static readonly string[] ProductNames = new[]
    {
        "Coca Cola 330ml", "Pepsi 500ml", "Water Bottle 1L", "Milk 1L", "Bread Loaf",
        "Chips Packet", "Chocolate Bar", "Coffee 200g", "Tea Box", "Sugar 1kg",
        "Rice 2kg", "Pasta 500g", "Tomato Sauce", "Olive Oil 1L", "Butter 200g",
        "Cheese Block", "Eggs 12pc", "Yogurt 500g", "Juice 1L", "Biscuits Pack"
    };

    private static readonly string[] CustomerNames = new[]
    {
        "Ahmed Ali", "Mohammed Hassan", "Omar Salem", "Khalid Mahmoud", "Youssef Ibrahim",
        "Fatima Ahmed", "Sara Mohammed", "Layla Omar", "Nour Khalid", "Mona Youssef"
    };

    public DevController(AppDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // DELETE OPERATIONS
    // =====================================================

    [HttpDelete("transactions")]
    public async Task<ActionResult> DeleteAllTransactions()
    {
        // Must delete allocations first due to FK constraints
        await _context.PaymentAllocations.ExecuteDeleteAsync();
        await _context.TransactionItems.ExecuteDeleteAsync();
        await _context.Transactions.ExecuteDeleteAsync();
        
        return Ok(new { message = "All transactions deleted" });
    }

    [HttpDelete("payments")]
    public async Task<ActionResult> DeleteAllPayments()
    {
        // Must delete allocations first due to FK constraints
        await _context.PaymentAllocations.ExecuteDeleteAsync();
        await _context.Payments.ExecuteDeleteAsync();
        
        return Ok(new { message = "All payments deleted" });
    }

    [HttpDelete("products")]
    public async Task<ActionResult> DeleteAllProducts()
    {
        // Must delete related data first due to FK constraints
        await _context.PaymentAllocations.ExecuteDeleteAsync();
        await _context.TransactionItems.ExecuteDeleteAsync();
        await _context.Transactions.ExecuteDeleteAsync();
        await _context.PurchaseItems.ExecuteDeleteAsync();
        await _context.Purchases.ExecuteDeleteAsync();
        await _context.ProductBarcodes.ExecuteDeleteAsync();
        await _context.Products.ExecuteDeleteAsync();
        
        return Ok(new { message = "All products deleted" });
    }

    [HttpDelete("audits")]
    public async Task<ActionResult> DeleteAllAudits()
    {
        // Remove audit logs
        // If AuditLogs DbSet does not exist, this will fail at compile time.
        // Audit logs should be removed first to avoid FK issues if any.
        await _context.AuditLogs.ExecuteDeleteAsync();

        return Ok(new { message = "All audit logs deleted" });
    }

    // =====================================================
    // SEED OPERATIONS
    // =====================================================

    [HttpPost("seed/products")]
    public async Task<ActionResult> SeedProducts([FromQuery] int count = 10)
    {
        var products = new List<Product>();
        var usedNames = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            string name;
            do
            {
                name = ProductNames[_random.Next(ProductNames.Length)] + $" #{_random.Next(1000, 9999)}";
            } while (usedNames.Contains(name));
            
            usedNames.Add(name);

            products.Add(new Product
            {
                Name = name,
                CostPrice = _random.Next(1, 20),
                Price = _random.Next(5, 50),
                StockQuantity = _random.Next(10, 200),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"{count} products created", products = products.Select(p => p.Name) });
    }

    [HttpPost("seed/orders")]
    public async Task<ActionResult> SeedOrders([FromQuery] int count = 10)
    {
        var products = await _context.Products.Where(p => p.IsActive).ToListAsync();
        if (!products.Any())
        {
            return BadRequest("No products available. Create products first.");
        }

        var transactions = new List<Transaction>();
        var payments = new List<Payment>();

        for (int i = 0; i < count; i++)
        {
            var itemCount = _random.Next(1, 5);
            var items = new List<TransactionItem>();
            
            for (int j = 0; j < itemCount; j++)
            {
                var product = products[_random.Next(products.Count)];
                var qty = _random.Next(1, 5);
                items.Add(new TransactionItem
                {
                    ProductId = product.Id,
                    Quantity = qty,
                    UnitPrice = product.Price,
                    UnitCost = product.CostPrice,
                    Subtotal = product.Price * qty
                });
            }

            var total = items.Sum(x => x.Subtotal);
            var isPaid = _random.Next(100) < 70; // 70% paid
            var isPartial = !isPaid && _random.Next(100) < 50; // 50% of unpaid are partial

            var transaction = new Transaction
            {
                TransactionDate = DateTime.UtcNow.AddDays(-_random.Next(0, 30)).AddHours(-_random.Next(0, 24)),
                CustomerName = CustomerNames[_random.Next(CustomerNames.Length)],
                EmployeeName = "Dev Seed",
                TotalAmount = total,
                Status = TransactionStatus.Completed,
                Items = items
            };

            transactions.Add(transaction);

            // Create payment if applicable
            if (isPaid || isPartial)
            {
                var paymentAmount = isPaid ? total : Math.Round(total * (decimal)(_random.Next(20, 80) / 100.0), 2);
                var payment = new Payment
                {
                    PaymentDate = transaction.TransactionDate.AddMinutes(1),
                    Amount = paymentAmount,
                    PaymentMethod = _random.Next(3) switch { 0 => "Cash", 1 => "Card", _ => "Transfer" },
                    CustomerName = transaction.CustomerName,
                    EmployeeName = "Dev Seed"
                };
                payments.Add(payment);
            }
        }

        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();

        // Now create payment allocations
        for (int i = 0; i < payments.Count; i++)
        {
            payments[i].Allocations = new List<PaymentAllocation>
            {
                new PaymentAllocation
                {
                    TransactionId = transactions[i].Id,
                    Amount = payments[i].Amount
                }
            };
        }

        _context.Payments.AddRange(payments);
        await _context.SaveChangesAsync();

        return Ok(new { 
            message = $"{count} orders created with {payments.Count} payments",
            totalOrders = count,
            paidOrders = payments.Count(p => transactions.Any(t => t.TotalAmount <= p.Amount))
        });
    }
}
