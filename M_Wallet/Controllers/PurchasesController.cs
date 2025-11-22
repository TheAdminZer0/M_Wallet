using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using M_Wallet.Data;
using M_Wallet.Shared;

namespace M_Wallet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchasesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PurchasesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Purchase>>> GetPurchases()
        {
            return await _context.Purchases
                .Include(p => p.Items)
                .ThenInclude(i => i.Product)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Purchase>> CreatePurchase(Purchase purchase)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                purchase.PurchaseDate = DateTime.UtcNow;

                // Prevent EF from trying to create/update products passed in the payload
                foreach (var item in purchase.Items)
                {
                    item.Product = null;
                }

                _context.Purchases.Add(purchase);

                foreach (var item in purchase.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                    {
                        throw new Exception($"Product with ID {item.ProductId} not found.");
                    }

                    // Weighted Average Cost Calculation
                    // New Cost = ((Current Stock * Current Cost) + (New Stock * New Cost)) / Total Stock
                    
                    decimal currentTotalValue = product.StockQuantity * product.CostPrice;
                    decimal newStockValue = item.Quantity * item.UnitCost;
                    int newTotalQuantity = product.StockQuantity + item.Quantity;

                    if (newTotalQuantity > 0)
                    {
                        product.CostPrice = (currentTotalValue + newStockValue) / newTotalQuantity;
                    }
                    else
                    {
                        // Should not happen for purchases, but safe guard
                        product.CostPrice = item.UnitCost;
                    }

                    // Update Stock
                    product.StockQuantity += item.Quantity;
                    
                    // Ensure product is active if we are buying stock for it
                    if (!product.IsActive)
                    {
                        product.IsActive = true;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetPurchases), new { id = purchase.Id }, purchase);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(ex.Message);
            }
        }
    }
}
