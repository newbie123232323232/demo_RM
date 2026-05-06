using Microsoft.EntityFrameworkCore;
using RevenueModule.Api.Models;

namespace RevenueModule.Api.Data;

public sealed class RevenueDbContext(DbContextOptions<RevenueDbContext> options) : DbContext(options)
{
    public DbSet<Buyer> Buyers => Set<Buyer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillLine> BillLines => Set<BillLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Buyer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable(x => x.HasCheckConstraint("CK_Buyers_VipPoint_NonNegative", "\"VipPoint\" >= 0"));
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.VipPoint).HasDefaultValue(0);

            entity.HasData(
                new Buyer { Id = 1, Name = "Buyer A", VipPoint = 3 },
                new Buyer { Id = 2, Name = "Buyer B", VipPoint = 7 },
                new Buyer { Id = 3, Name = "Buyer C", VipPoint = 12 }
            );
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToTable(x => x.HasCheckConstraint("CK_Products_UnitPriceVnd_NonNegative", "\"UnitPriceVnd\" >= 0"));
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.UnitPriceVnd).IsRequired();

            entity.HasData(
                new Product { Id = 1, Name = "So tay mini", UnitPriceVnd = 45000 },
                new Product { Id = 2, Name = "Binh giu nhiet", UnitPriceVnd = 320000 },
                new Product { Id = 3, Name = "Tai nghe co day", UnitPriceVnd = 290000 },
                new Product { Id = 4, Name = "Ghe cong thai hoc", UnitPriceVnd = 2150000 },
                new Product { Id = 5, Name = "Man hinh 27 inch", UnitPriceVnd = 4790000 }
            );
        });

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(16).IsRequired();
            entity.Property(x => x.PendingAtUtc).IsRequired();
            entity.Property(x => x.Subtotal).IsRequired();
            entity.Property(x => x.VoucherThuongAmount).IsRequired();
            entity.Property(x => x.VoucherType).HasMaxLength(16).IsRequired();
            entity.Property(x => x.VoucherValue).HasColumnType("numeric(18,2)").HasDefaultValue(0);
            entity.Property(x => x.BaseBeforeVip).IsRequired();
            entity.Property(x => x.VipDiscountVnd).IsRequired();
            entity.Property(x => x.Payable).IsRequired();
            entity.Property(x => x.VipPointEarned).HasDefaultValue(0);
            entity.ToTable(x => x.HasCheckConstraint("CK_Bills_Payable_NonNegative", "\"Payable\" >= 0"));
            entity.HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.PendingAtUtc);
            entity.HasIndex(x => x.CompletedAtUtc);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.Payable);
            entity.HasIndex(x => new { x.BuyerId, x.PendingAtUtc });
        });

        modelBuilder.Entity<BillLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Qty).IsRequired();
            entity.Property(x => x.UnitPriceVnd).IsRequired();
            entity.Property(x => x.LineTotalVnd).IsRequired();
            entity.ToTable(x => x.HasCheckConstraint("CK_BillLines_Qty_Positive", "\"Qty\" > 0"));
            entity.ToTable(x => x.HasCheckConstraint("CK_BillLines_UnitPrice_NonNegative", "\"UnitPriceVnd\" >= 0"));
            entity.HasOne(x => x.Bill).WithMany(x => x.Lines).HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.ProductId);
            entity.HasIndex(x => new { x.ProductId, x.BillId });
        });
    }
}
