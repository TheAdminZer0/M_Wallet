using Microsoft.EntityFrameworkCore;
using M_Wallet.Shared;

namespace M_Wallet.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<ProductBarcode> ProductBarcodes { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<TransactionItem> TransactionItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.CostPrice).HasPrecision(18, 2);
            entity.Property(p => p.Price).HasPrecision(18, 2);
            
            entity.HasMany(p => p.Barcodes)
                .WithOne(pb => pb.Product)
                .HasForeignKey(pb => pb.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductBarcode>(entity =>
        {
            entity.Property(pb => pb.Barcode).HasMaxLength(64).IsRequired();
            entity.HasIndex(pb => pb.Barcode);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(t => t.TotalAmount).HasPrecision(18, 2);
            entity.HasMany(t => t.Items)
                .WithOne(ti => ti.Transaction)
                .HasForeignKey(ti => ti.TransactionId);
        });

        modelBuilder.Entity<TransactionItem>(entity =>
        {
            entity.Property(ti => ti.UnitPrice).HasPrecision(18, 2);
            entity.Property(ti => ti.Subtotal).HasPrecision(18, 2);
        });
    }
}