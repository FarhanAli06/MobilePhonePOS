using Microsoft.EntityFrameworkCore;
using Empire.Domain.Entities;
using Empire.Domain.Common; // Added to resolve AuditableEntity errors
using Empire.Domain.Enums;   // Added for enums if needed

namespace Empire.Infrastructure.Data;

public class EmpireDbContext : DbContext
{
    public EmpireDbContext(DbContextOptions<EmpireDbContext> options) : base(options)
    {
    }

    public DbSet<Shop> Shops { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserShopRole> UserShopRoles { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<Repair> Repairs { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<InventoryAdjustment> InventoryAdjustments { get; set; }

    // New Lookup Tables
    public DbSet<Brand> Brands { get; set; }
    public DbSet<DeviceCategory> DeviceCategories { get; set; }
    public DbSet<DeviceModel> DeviceModels { get; set; }
    public DbSet<InventoryCategory> InventoryCategories { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<LookupValue> LookupValues { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleItem> SaleItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<RepairPart> RepairParts { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurations for all entities

        modelBuilder.Entity<Shop>(entity =>
        {
            entity.HasMany(s => s.Customers).WithOne(c => c.Shop).HasForeignKey(c => c.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(s => s.Repairs).WithOne(r => r.Shop).HasForeignKey(r => r.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(s => s.Sales).WithOne(sa => sa.Shop).HasForeignKey(sa => sa.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(s => s.Inventories).WithOne(i => i.Shop).HasForeignKey(i => i.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.ZipCode).HasMaxLength(10);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.LogoPath).HasMaxLength(500);
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasMany(c => c.Repairs).WithOne(r => r.Customer).HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(c => c.Sales).WithOne(s => s.Customer).HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.SetNull);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.ZipCode).HasMaxLength(10);
            entity.HasOne(e => e.Shop).WithMany(s => s.Customers).HasForeignKey(e => e.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ShopId, e.Phone });
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SaleNumber).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.SaleNumber).IsUnique();
            entity.HasMany(s => s.SaleItems).WithOne(si => si.Sale).HasForeignKey(si => si.SaleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(s => s.Payments).WithOne(p => p.Sale).HasForeignKey(p => p.SaleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(si => si.InventoryItem).WithMany(i => i.SaleItems).HasForeignKey(si => si.InventoryItemId).OnDelete(DeleteBehavior.SetNull);
            entity.Property(e => e.ItemName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Note).HasMaxLength(1000);
            entity.Property(e => e.IsCustomItem);
            entity.Property(e => e.IsTaxable);
        });

        modelBuilder.Entity<Repair>(entity =>
        {
             entity.HasOne(e => e.Company).WithMany(c => c.Repairs).HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.SetNull);
             entity.HasMany(e => e.RepairParts).WithOne(rp => rp.Repair).HasForeignKey(rp => rp.RepairId).OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<RepairPart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Repair).WithMany(r => r.RepairParts).HasForeignKey(e => e.RepairId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.InventoryItem).WithMany().HasForeignKey(e => e.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Ignore(e => e.TotalPrice); // Computed property, not stored in database
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasOne(e => e.Shop).WithMany(s => s.Devices).HasForeignKey(e => e.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Brand).WithMany().HasForeignKey(e => e.BrandId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DeviceCategory).WithMany().HasForeignKey(e => e.DeviceCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DeviceModel).WithMany().HasForeignKey(e => e.DeviceModelId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Company).WithMany(c => c.Devices).HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.SoldToCustomer).WithMany(c => c.Devices).HasForeignKey(e => e.SoldToCustomerId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasOne(e => e.Shop).WithMany().HasForeignKey(e => e.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Brand).WithMany().HasForeignKey(e => e.BrandId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DeviceCategory).WithMany().HasForeignKey(e => e.DeviceCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DeviceModel).WithMany().HasForeignKey(e => e.DeviceModelId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InventoryCategory).WithMany().HasForeignKey(e => e.InventoryCategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.InventoryItem).WithMany(i => i.StockMovements).HasForeignKey(e => e.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.MovementType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.UnitCost).HasPrecision(18, 2);
            entity.Property(e => e.TotalCost).HasPrecision(18, 2);
        });
        
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.InventoryItem).WithMany(i => i.InventoryTransactions).HasForeignKey(e => e.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ReferenceType).HasMaxLength(50);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(50);
            entity.Property(e => e.UnitCost).HasPrecision(18, 2);
            entity.Property(e => e.TotalCost).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasIndex(e => e.InventoryItemId);
            entity.HasIndex(e => e.TransactionDate);
            entity.HasIndex(e => e.TransactionType);
            entity.HasIndex(e => e.ReferenceNumber);
        });
        
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Name).IsUnique().HasFilter("[IsDeleted] = 0");
            entity.HasMany(e => e.InventoryItems).WithOne(i => i.Item).HasForeignKey(i => i.ItemId).OnDelete(DeleteBehavior.Restrict);
        });

    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity && (
                e.State == EntityState.Added ||
                e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (BaseEntity)entityEntry.Entity;

            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedDate = DateTime.UtcNow;
            }
            else if (entityEntry.State == EntityState.Modified)
            {
                entity.ModifiedDate = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
