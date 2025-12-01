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
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentAllocation> PaymentAllocations { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<PurchaseItem> PurchaseItems { get; set; }
    public DbSet<Person> People { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Person>(entity =>
        {
            entity.Property(p => p.Name).IsRequired();
            entity.HasIndex(p => p.Name);
        });

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

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.Property(p => p.TotalAmount).HasPrecision(18, 2);
            entity.HasMany(p => p.Items)
                .WithOne(pi => pi.Purchase)
                .HasForeignKey(pi => pi.PurchaseId);
        });

        modelBuilder.Entity<PurchaseItem>(entity =>
        {
            entity.Property(pi => pi.UnitCost).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Person>().HasData(
            new Person 
            { 
                Id = 1, 
                Name = "Aziz", 
                Passcode = "630125874", 
                Role = "Admin", 
                Username = "aziz",
                Password = "123", // In a real app, hash this!
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Person
            {
                Id = 2,
                Name = "POS Terminal",
                Role = "System",
                Username = "pos",
                Password = "pos",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}