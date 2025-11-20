using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using M_Wallet.Data;
using M_Wallet.Shared;

namespace M_Wallet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        var products = await _context.Products
            .Include(p => p.Barcodes)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var salesStats = await _context.TransactionItems
            .Where(ti => ti.Transaction!.TransactionDate >= thirtyDaysAgo)
            .GroupBy(ti => ti.ProductId)
            .Select(g => new { ProductId = g.Key, SoldCount = g.Sum(ti => ti.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.SoldCount);

        var allTimeStats = await _context.TransactionItems
            .GroupBy(ti => ti.ProductId)
            .Select(g => new { ProductId = g.Key, SoldCount = g.Sum(ti => ti.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.SoldCount);

        foreach (var product in products)
        {
            if (salesStats.TryGetValue(product.Id, out int count30))
            {
                product.SoldLast30Days = count30;
            }
            if (allTimeStats.TryGetValue(product.Id, out int countTotal))
            {
                product.TotalSoldQuantity = countTotal;
            }
        }

        return products;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.Barcodes)
            .FirstOrDefaultAsync(p => p.Id == id);
        
        if (product == null)
            return NotFound();
        
        return product;
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.Barcodes = NormalizeBarcodes(product.Barcodes);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, Product product)
    {
        if (id != product.Id)
            return BadRequest();

        var existingProduct = await _context.Products
            .Include(p => p.Barcodes)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (existingProduct is null)
            return NotFound();

        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.CostPrice = product.CostPrice;
        existingProduct.Price = product.Price;
        existingProduct.StockQuantity = product.StockQuantity;
        existingProduct.ImageUrl = product.ImageUrl;
        existingProduct.IsActive = product.IsActive;
        existingProduct.IsPinned = product.IsPinned;

        var sanitizedBarcodes = NormalizeBarcodes(product.Barcodes);

        foreach (var existing in existingProduct.Barcodes.ToList())
        {
            var match = sanitizedBarcodes.FirstOrDefault(b => b.Id == existing.Id);
            if (match is null)
            {
                existingProduct.Barcodes.Remove(existing);
            }
            else
            {
                existing.Barcode = match.Barcode;
                sanitizedBarcodes.Remove(match);
            }
        }

        foreach (var barcode in sanitizedBarcodes)
        {
            existingProduct.Barcodes.Add(new ProductBarcode
            {
                Barcode = barcode.Barcode,
                CreatedAt = DateTime.UtcNow
            });
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ProductExists(id))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        product.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> ProductExists(int id)
    {
        return await _context.Products.AnyAsync(e => e.Id == id);
    }

    private static List<ProductBarcode> NormalizeBarcodes(IEnumerable<ProductBarcode>? barcodes)
    {
        var results = new List<ProductBarcode>();
        if (barcodes is null)
        {
            return results;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var barcode in barcodes)
        {
            if (barcode is null || string.IsNullOrWhiteSpace(barcode.Barcode))
            {
                continue;
            }

            var trimmed = barcode.Barcode.Trim();
            if (!seen.Add(trimmed))
            {
                continue;
            }

            results.Add(new ProductBarcode
            {
                Id = barcode.Id,
                Barcode = trimmed,
                CreatedAt = barcode.CreatedAt == default ? DateTime.UtcNow : barcode.CreatedAt
            });
        }

        return results;
    }
}