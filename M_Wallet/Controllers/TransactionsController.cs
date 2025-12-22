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
            .Include(t => t.Driver)
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

    [HttpGet("driver/{driverId}")]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetDriverTransactions(int driverId, [FromQuery] TransactionStatus? status = null)
    {
        var query = _context.Transactions
            .Include(t => t.Items)
            .ThenInclude(i => i.Product)
            .Include(t => t.Driver)
            .Where(t => t.DriverId == driverId && t.IsDelivery);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

        return transactions;
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

            // Set initial status
            if (transaction.IsDelivery)
            {
                transaction.Status = TransactionStatus.Pending;
            }
            else
            {
                transaction.Status = TransactionStatus.Completed;
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
            
            transaction.TotalAmount = transaction.Items.Sum(i => i.Subtotal) - transaction.Discount;
            if (transaction.TotalAmount < 0) transaction.TotalAmount = 0;

            // Handle Customer Creation/Linking
            if (transaction.PersonId.HasValue)
            {
                // Client already identified the person
                var person = await _context.People.FindAsync(transaction.PersonId.Value);
                if (person != null)
                {
                    transaction.CustomerName = person.Name;
                }
            }
            else if (!string.IsNullOrWhiteSpace(transaction.CustomerName) || !string.IsNullOrWhiteSpace(transaction.CustomerPhone))
            {
                var customerName = transaction.CustomerName?.Trim();
                var customerPhone = transaction.CustomerPhone?.Trim();
                
                Person? existingCustomer = null;

                // Try to find by Phone first if available
                if (!string.IsNullOrWhiteSpace(customerPhone))
                {
                    existingCustomer = await _context.People
                        .FirstOrDefaultAsync(c => c.PhoneNumber == customerPhone && c.Role == "Customer");
                }
                
                // Try to find by Name if not found and name available
                if (existingCustomer == null && !string.IsNullOrWhiteSpace(customerName))
                {
                    existingCustomer = await _context.People
                        .FirstOrDefaultAsync(c => c.Name != null && c.Name.ToLower() == customerName.ToLower() && c.Role == "Customer");
                }

                if (existingCustomer != null)
                {
                    transaction.PersonId = existingCustomer.Id;
                    transaction.CustomerName = existingCustomer.Name; 
                }
                else
                {
                    // Create new customer
                    var newCustomer = new Person
                    {
                        Name = customerName,
                        Role = "Customer",
                        PhoneNumber = customerPhone,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.People.Add(newCustomer);
                    await _context.SaveChangesAsync(); // Save to generate ID
                    
                    transaction.PersonId = newCustomer.Id;
                    transaction.CustomerName = newCustomer.Name;
                }
            }

            // Handle Driver Creation/Linking
            if (transaction.IsDelivery)
            {
                if (transaction.DriverId.HasValue)
                {
                    // Driver already selected, nothing to do
                }
                else if (!string.IsNullOrWhiteSpace(transaction.DriverName) || !string.IsNullOrWhiteSpace(transaction.DriverPhone))
                {
                    var driverName = transaction.DriverName?.Trim();
                    var driverPhone = transaction.DriverPhone?.Trim();
                    
                    Person? existingDriver = null;

                    // Try to find by Phone first if available
                    if (!string.IsNullOrWhiteSpace(driverPhone))
                    {
                        existingDriver = await _context.People
                            .FirstOrDefaultAsync(d => d.PhoneNumber == driverPhone && d.Role == "Driver");
                    }
                    
                    // Try to find by Name if not found and name available
                    if (existingDriver == null && !string.IsNullOrWhiteSpace(driverName))
                    {
                        existingDriver = await _context.People
                            .FirstOrDefaultAsync(d => d.Name != null && d.Name.ToLower() == driverName.ToLower() && d.Role == "Driver");
                    }

                    if (existingDriver != null)
                    {
                        transaction.DriverId = existingDriver.Id;
                    }
                    else
                    {
                        // Create new driver
                        var newDriver = new Person
                        {
                            Name = driverName,
                            Role = "Driver",
                            PhoneNumber = driverPhone,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.People.Add(newDriver);
                        await _context.SaveChangesAsync(); // Save to generate ID
                        
                        transaction.DriverId = newDriver.Id;
                    }
                }
            }

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // AUTO-ALLOCATION: If customer has positive balance (credit), automatically apply it to this order
            if (transaction.PersonId.HasValue && transaction.TotalAmount > 0)
            {
                // Get customer's payments and their unallocated amounts
                var customerPayments = await _context.Payments
                    .Include(p => p.Allocations)
                    .Where(p => p.PersonId == transaction.PersonId && p.Amount > 0) // Only positive payments
                    .OrderBy(p => p.PaymentDate) // FIFO: oldest first
                    .ToListAsync();
                
                var orderBalanceDue = transaction.TotalAmount;
                var autoAllocations = new List<string>();
                
                foreach (var payment in customerPayments)
                {
                    if (orderBalanceDue <= 0) break;
                    
                    var paymentAllocated = payment.Allocations?.Sum(a => a.Amount) ?? 0;
                    var paymentUnallocated = payment.Amount - paymentAllocated;
                    
                    if (paymentUnallocated > 0)
                    {
                        var allocationAmount = Math.Min(paymentUnallocated, orderBalanceDue);
                        
                        var newAllocation = new PaymentAllocation
                        {
                            PaymentId = payment.Id,
                            TransactionId = transaction.Id,
                            Amount = allocationAmount
                        };
                        _context.PaymentAllocations.Add(newAllocation);
                        
                        orderBalanceDue -= allocationAmount;
                        autoAllocations.Add($"Auto-allocated {allocationAmount:F2} LD from payment #{payment.Id}");
                    }
                }
                
                if (autoAllocations.Any())
                {
                    await _context.SaveChangesAsync();
                }
            }

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

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] TransactionStatus newStatus)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
        {
            return NotFound();
        }

        var oldStatus = transaction.Status;
        if (oldStatus == newStatus)
        {
            return NoContent();
        }

        // Handle Stock Logic
        if (newStatus == TransactionStatus.Canceled && oldStatus != TransactionStatus.Canceled)
        {
            // Restore stock
            foreach (var item in transaction.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                }
            }
        }
        else if (oldStatus == TransactionStatus.Canceled && newStatus != TransactionStatus.Canceled)
        {
            // Deduct stock (Un-cancel)
            foreach (var item in transaction.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity -= item.Quantity;
                }
            }
        }

        transaction.Status = newStatus;
        
        // Log the status change
        var log = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            Action = "UpdateStatus",
            Entity = "Transaction",
            EntityId = transaction.Id.ToString(),
            EmployeeName = "System", 
            Description = $"Updated transaction #{transaction.Id} status from {oldStatus} to {newStatus}",
            Changes = $"Status: {oldStatus} -> {newStatus}"
        };
        _context.AuditLogs.Add(log);

        await _context.SaveChangesAsync();

        return NoContent();
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
    public async Task<IActionResult> UpdateTransaction(int id, Transaction transaction, [FromHeader(Name = "X-Authorized-By")] string? authorizedBy = null)
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

        // Track changes for audit log
        var changes = new List<string>();

        // Update Note
        if (existingTransaction.Note != transaction.Note)
        {
            changes.Add($"Note: '{existingTransaction.Note ?? "(empty)"}' -> '{transaction.Note ?? "(empty)"}'");
            existingTransaction.Note = transaction.Note;
        }
        
        // Update customer
        if (transaction.PersonId != existingTransaction.PersonId)
        {
            var oldCustomer = existingTransaction.CustomerName ?? "Walk-in";
            
            if (transaction.PersonId.HasValue)
            {
                var person = await _context.People.FindAsync(transaction.PersonId.Value);
                if (person != null)
                {
                    existingTransaction.PersonId = person.Id;
                    existingTransaction.CustomerName = person.Name;
                    changes.Add($"Customer: '{oldCustomer}' -> '{person.Name}'");
                    
                    // Also update payments linked to this transaction
                    // Only update payment's PersonId if ALL allocations for that payment 
                    // belong to transactions with the same customer (or this transaction)
                    var paymentAllocations = await _context.PaymentAllocations
                        .Include(pa => pa.Payment)
                        .Where(pa => pa.TransactionId == id)
                        .ToListAsync();
                    
                    foreach (var allocation in paymentAllocations)
                    {
                        if (allocation.Payment != null)
                        {
                            // Check if this payment is shared with other transactions
                            var otherAllocations = await _context.PaymentAllocations
                                .Include(pa => pa.Transaction)
                                .Where(pa => pa.PaymentId == allocation.PaymentId && pa.TransactionId != id)
                                .ToListAsync();
                            
                            // Only update if payment is exclusively for this transaction
                            // OR all other allocations are for the same customer
                            var canUpdate = otherAllocations.Count == 0 || 
                                otherAllocations.All(oa => oa.Transaction?.PersonId == person.Id);
                            
                            if (canUpdate)
                            {
                                allocation.Payment.PersonId = person.Id;
                                allocation.Payment.CustomerName = person.Name;
                            }
                            // If payment is shared with other customers, leave PersonId as is
                            // The allocations still correctly track where the money goes
                        }
                    }
                    
                    // AUTO-ALLOCATION: If customer has positive balance and this order has balance due,
                    // automatically allocate from customer's excess payments
                    var existingPaidAmount = await _context.PaymentAllocations
                        .Where(pa => pa.TransactionId == id)
                        .SumAsync(pa => pa.Amount);
                    
                    var orderBalanceDue = existingTransaction.TotalAmount - existingPaidAmount;
                    
                    if (orderBalanceDue > 0)
                    {
                        // Get customer's payments and their total allocated amounts
                        var customerPayments = await _context.Payments
                            .Include(p => p.Allocations)
                            .Where(p => p.PersonId == person.Id && p.Amount > 0) // Only positive payments
                            .OrderBy(p => p.PaymentDate) // FIFO: oldest first
                            .ToListAsync();
                        
                        foreach (var payment in customerPayments)
                        {
                            if (orderBalanceDue <= 0) break;
                            
                            var paymentAllocated = payment.Allocations?.Sum(a => a.Amount) ?? 0;
                            var paymentUnallocated = payment.Amount - paymentAllocated;
                            
                            if (paymentUnallocated > 0)
                            {
                                var allocationAmount = Math.Min(paymentUnallocated, orderBalanceDue);
                                
                                var newAllocation = new PaymentAllocation
                                {
                                    PaymentId = payment.Id,
                                    TransactionId = id,
                                    Amount = allocationAmount
                                };
                                _context.PaymentAllocations.Add(newAllocation);
                                
                                orderBalanceDue -= allocationAmount;
                                changes.Add($"Auto-allocated {allocationAmount:F2} LD from payment #{payment.Id}");
                            }
                        }
                    }
                }
            }
            else
            {
                existingTransaction.PersonId = null;
                existingTransaction.CustomerName = null;
                changes.Add($"Customer: '{oldCustomer}' -> 'Walk-in'");
                
                // Also clear customer from payments linked to this transaction
                // Only if the payment is exclusively for this transaction
                var paymentAllocations = await _context.PaymentAllocations
                    .Include(pa => pa.Payment)
                    .Where(pa => pa.TransactionId == id)
                    .ToListAsync();
                
                foreach (var allocation in paymentAllocations)
                {
                    if (allocation.Payment != null)
                    {
                        // Check if this payment is shared with other transactions
                        var otherAllocations = await _context.PaymentAllocations
                            .Where(pa => pa.PaymentId == allocation.PaymentId && pa.TransactionId != id)
                            .ToListAsync();
                        
                        // Only clear if payment is exclusively for this transaction
                        if (otherAllocations.Count == 0)
                        {
                            allocation.Payment.PersonId = null;
                            allocation.Payment.CustomerName = null;
                        }
                    }
                }
            }
        }

        // Update driver
        if (transaction.DriverId != existingTransaction.DriverId)
        {
            if (transaction.DriverId.HasValue)
            {
                var driver = await _context.People.FindAsync(transaction.DriverId.Value);
                if (driver != null)
                {
                    changes.Add($"Driver: '{existingTransaction.Driver?.Name ?? "None"}' -> '{driver.Name}'");
                    existingTransaction.DriverId = driver.Id;
                }
            }
            else
            {
                changes.Add($"Driver: '{existingTransaction.Driver?.Name ?? "None"}' -> 'None'");
                existingTransaction.DriverId = null;
            }
        }

        // Update employee (admin only - validated on client)
        if (!string.IsNullOrEmpty(transaction.EmployeeName) && transaction.EmployeeName != existingTransaction.EmployeeName)
        {
            changes.Add($"Employee: '{existingTransaction.EmployeeName}' -> '{transaction.EmployeeName}'");
            existingTransaction.EmployeeName = transaction.EmployeeName;
        }

        // Log changes if any - use authorizedBy header for audit
        if (changes.Any())
        {
            var log = new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                Action = "Update",
                Entity = "Transaction",
                EntityId = transaction.Id.ToString(),
                EmployeeName = authorizedBy ?? transaction.EmployeeName ?? existingTransaction.EmployeeName,
                Description = $"Updated transaction #{transaction.Id}",
                Changes = string.Join("; ", changes)
            };
            _context.AuditLogs.Add(log);
        }

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

    /// <summary>
    /// Process a refund for a transaction. Creates a negative payment to offset existing payments
    /// and restores stock for all items.
    /// </summary>
    [HttpPost("{id}/refund")]
    public async Task<ActionResult> RefundTransaction(int id, [FromBody] RefundRequest request)
    {
        using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var transaction = await _context.Transactions
                .Include(t => t.Items)
                .Include(t => t.PaymentAllocations)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                return NotFound();
            }

            if (transaction.Status == TransactionStatus.Refunded)
            {
                return BadRequest("Transaction is already refunded.");
            }

            if (transaction.Status == TransactionStatus.Canceled)
            {
                return BadRequest("Cannot refund a canceled transaction.");
            }

            // Calculate total already paid
            var totalPaid = transaction.PaymentAllocations?.Sum(pa => pa.Amount) ?? 0;

            // Create a negative payment (refund) to offset the payments
            if (totalPaid > 0)
            {
                var refundPayment = new Payment
                {
                    PaymentDate = DateTime.UtcNow,
                    Amount = -totalPaid, // Negative amount for refund
                    PaymentMethod = request.PaymentMethod ?? "Cash",
                    Reference = $"Refund for Order #{transaction.Id}" + (!string.IsNullOrEmpty(request.Reason) ? $" - {request.Reason}" : ""),
                    PersonId = transaction.PersonId,
                    CustomerName = transaction.CustomerName,
                    EmployeeName = request.EmployeeName ?? "System",
                    Allocations = new List<PaymentAllocation>
                    {
                        new PaymentAllocation
                        {
                            TransactionId = transaction.Id,
                            Amount = -totalPaid // Negative allocation
                        }
                    }
                };

                _context.Payments.Add(refundPayment);
            }

            // Restore stock for all items
            foreach (var item in transaction.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                }
            }

            // Update transaction status
            transaction.Status = TransactionStatus.Refunded;

            // Log the refund
            var log = new AuditLog
            {
                Timestamp = DateTime.UtcNow,
                Action = "Refund",
                Entity = "Transaction",
                EntityId = transaction.Id.ToString(),
                EmployeeName = request.EmployeeName ?? "System",
                Description = $"Refunded transaction #{transaction.Id} for {totalPaid:F2} LD",
                Changes = System.Text.Json.JsonSerializer.Serialize(new
                {
                    RefundedAmount = totalPaid,
                    Reason = request.Reason,
                    ItemsRestored = transaction.Items.Select(i => new { i.ProductId, i.Quantity })
                })
            };
            _context.AuditLogs.Add(log);

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return Ok(new { RefundedAmount = totalPaid, Message = "Transaction refunded successfully." });
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            return StatusCode(500, $"Error processing refund: {ex.Message}");
        }
    }
}

public class RefundRequest
{
    public string? PaymentMethod { get; set; }
    public string? Reason { get; set; }
    public string? EmployeeName { get; set; }
}