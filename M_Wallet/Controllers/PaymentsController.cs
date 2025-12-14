using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using M_Wallet.Data;
using M_Wallet.Shared;

namespace M_Wallet.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
    {
        return await _context.Payments
            .Include(p => p.Allocations)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment(Payment payment)
    {
        if (payment.Amount == 0)
        {
            return BadRequest("Payment amount cannot be zero.");
        }

        // Ensure UTC date for PostgreSQL
        if (payment.PaymentDate.Kind != DateTimeKind.Utc)
        {
            payment.PaymentDate = payment.PaymentDate.ToUniversalTime();
        }

        // If allocations are provided, validate them
        if (payment.Allocations != null && payment.Allocations.Any())
        {
            var totalAllocated = payment.Allocations.Sum(a => a.Amount);
            if (totalAllocated > payment.Amount)
            {
                return BadRequest("Total allocated amount cannot exceed payment amount.");
            }

            foreach (var allocation in payment.Allocations)
            {
                var transaction = await _context.Transactions.FindAsync(allocation.TransactionId);
                if (transaction == null)
                {
                    return BadRequest($"Transaction {allocation.TransactionId} not found.");
                }
                
                // Link payment to the same person as the transaction if not already linked
                if (payment.PersonId == null && transaction.PersonId != null)
                {
                    payment.PersonId = transaction.PersonId;
                }

                allocation.Payment = payment;
            }
        }

        // If PersonId is still null but we have a CustomerName, try to find the customer
        if (payment.PersonId == null && !string.IsNullOrWhiteSpace(payment.CustomerName))
        {
            var customerInput = payment.CustomerName.Trim();
            Person? existingCustomer = null;

            // Check if input is a 10-digit phone number
            bool isPhoneNumber = System.Text.RegularExpressions.Regex.IsMatch(customerInput, @"^\d{10}$");

            if (isPhoneNumber)
            {
                existingCustomer = await _context.People
                    .FirstOrDefaultAsync(c => c.PhoneNumber == customerInput && c.Role == "Customer");
            }

            if (existingCustomer == null)
            {
                existingCustomer = await _context.People
                    .FirstOrDefaultAsync(c => c.Name != null && c.Name.ToLower() == customerInput.ToLower() && c.Role == "Customer");
            }

            if (existingCustomer != null)
            {
                payment.PersonId = existingCustomer.Id;
            }
        }

        // Auto-allocate if it's a deposit (positive amount) and we have a PersonId but no manual allocations
        if (payment.Amount > 0 && payment.PersonId.HasValue && (payment.Allocations == null || !payment.Allocations.Any()))
        {
            var unpaidTransactions = await _context.Transactions
                .Include(t => t.PaymentAllocations)
                .Where(t => t.PersonId == payment.PersonId)
                .OrderBy(t => t.TransactionDate)
                .ToListAsync();

            decimal remainingPayment = payment.Amount;
            payment.Allocations ??= new List<PaymentAllocation>();

            foreach (var transaction in unpaidTransactions)
            {
                if (remainingPayment <= 0) break;

                var paidAmount = transaction.PaymentAllocations.Sum(pa => pa.Amount);
                var balanceDue = transaction.TotalAmount - paidAmount;

                if (balanceDue > 0)
                {
                    var allocateAmount = Math.Min(remainingPayment, balanceDue);
                    
                    payment.Allocations.Add(new PaymentAllocation
                    {
                        TransactionId = transaction.Id,
                        Amount = allocateAmount
                    });

                    remainingPayment -= allocateAmount;
                }
            }
        }

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Log the payment
        var log = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            Action = "Payment",
            Entity = "Payment",
            EntityId = payment.Id.ToString(),
            EmployeeName = payment.EmployeeName ?? "Unknown",
            Description = $"Received payment of {payment.Amount:F2} LD via {payment.PaymentMethod}",
            Changes = System.Text.Json.JsonSerializer.Serialize(new { 
                Customer = payment.CustomerName,
                Allocations = (payment.Allocations ?? new List<PaymentAllocation>()).Select(a => new { a.TransactionId, a.Amount }) 
            })
        };
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPayments), new { id = payment.Id }, payment);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePayment(int id)
    {
        var payment = await _context.Payments
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
        {
            return NotFound();
        }

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
