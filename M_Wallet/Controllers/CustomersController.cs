using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using M_Wallet.Data;
using M_Wallet.Shared;

namespace M_Wallet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            return await _context.Customers.OrderBy(c => c.Name).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            return customer;
        }

        [HttpPost]
        public async Task<ActionResult<Customer>> PostCustomer(Customer customer)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCustomer", new { id = customer.Id }, customer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomer(int id, Customer customer)
        {
            if (id != customer.Id)
            {
                return BadRequest();
            }

            var existingCustomer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (existingCustomer == null)
            {
                return NotFound();
            }

            if (existingCustomer.Name != customer.Name)
            {
                // Update Payments
                var payments = await _context.Payments.Where(p => p.CustomerName == existingCustomer.Name).ToListAsync();
                foreach (var payment in payments)
                {
                    payment.CustomerName = customer.Name;
                }
                
                // Update Transactions
                var transactions = await _context.Transactions.Where(t => t.CustomerName == existingCustomer.Name).ToListAsync();
                foreach (var transaction in transactions)
                {
                    transaction.CustomerName = customer.Name;
                }
            }

            _context.Entry(customer).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}/statement")]
        public async Task<ActionResult<List<StatementItem>>> GetCustomerStatement(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            var items = new List<StatementItem>();

            // Get Transactions (Invoices) - Debits
            // Match by CustomerId OR CustomerName
            var transactions = await _context.Transactions
                .Where(t => t.CustomerId == id || t.CustomerName == customer.Name)
                .ToListAsync();

            items.AddRange(transactions.Select(t => new StatementItem
            {
                Date = t.TransactionDate,
                Description = $"Invoice #{t.Id}",
                Amount = -t.TotalAmount // Money owed by customer
            }));

            // Get Payments - Credits
            // Payments currently store CustomerName string. 
            // We should probably add CustomerId to Payment too, but for now match by Name.
            // Or if we update Payment to have CustomerId later.
            // Let's assume matching by Name is the way for now since Payment entity wasn't updated.
            var payments = await _context.Payments
                .Where(p => p.CustomerName == customer.Name)
                .ToListAsync();

            items.AddRange(payments.Select(p => new StatementItem
            {
                Date = p.PaymentDate,
                Description = $"Payment #{p.Id} ({p.PaymentMethod})",
                Amount = p.Amount // Money paid by customer
            }));

            // Sort by date
            var sortedItems = items.OrderBy(i => i.Date).ToList();

            // Calculate running balance
            decimal balance = 0;
            foreach (var item in sortedItems)
            {
                balance += item.Amount;
                item.RunningBalance = balance;
            }

            return sortedItems.OrderByDescending(i => i.Date).ToList();
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncCustomers()
        {
            // 1. Find all unique customer names from Transactions and Payments
            var transactionNames = await _context.Transactions
                .Where(t => !string.IsNullOrWhiteSpace(t.CustomerName))
                .Select(t => t.CustomerName)
                .Distinct()
                .ToListAsync();

            var paymentNames = await _context.Payments
                .Where(p => !string.IsNullOrWhiteSpace(p.CustomerName))
                .Select(p => p.CustomerName)
                .Distinct()
                .ToListAsync();

            var allNames = transactionNames.Concat(paymentNames)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            int addedCount = 0;
            int linkedCount = 0;

            foreach (var name in allNames)
            {
                // Check if customer exists
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());

                if (customer == null)
                {
                    customer = new Customer
                    {
                        Name = name,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                    addedCount++;
                }

                // Link Transactions
                var unlinkedTransactions = await _context.Transactions
                    .Where(t => t.CustomerName == name && t.CustomerId == null)
                    .ToListAsync();

                if (unlinkedTransactions.Any())
                {
                    foreach (var t in unlinkedTransactions)
                    {
                        t.CustomerId = customer.Id;
                        // Ensure name casing matches the customer record
                        t.CustomerName = customer.Name; 
                    }
                    linkedCount += unlinkedTransactions.Count;
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { Added = addedCount, LinkedTransactions = linkedCount });
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }
    }
}
