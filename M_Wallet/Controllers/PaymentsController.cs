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
                .ThenInclude(pa => pa.Transaction!)
                    .ThenInclude(t => t.Items)
                        .ThenInclude(i => i.Product)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment(Payment payment)
    {
        if (payment.Amount <= 0)
        {
            return BadRequest("Payment amount must be greater than zero.");
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
                
                allocation.Payment = payment;
            }
        }

        _context.Payments.Add(payment);
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
