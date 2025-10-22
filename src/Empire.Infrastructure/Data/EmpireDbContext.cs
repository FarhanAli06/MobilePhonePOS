using Microsoft.EntityFrameworkCore;
using Empire.Domain.Entities;
using Empire.Domain.Common;

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
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<LookupValue> LookupValues { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleItem> SaleItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Shop entity
        modelBuilder.Entity<Shop>(entity =>
        {
            entity.HasKey(e => e.Id);
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

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure UserShopRole entity
        modelBuilder.Entity<UserShopRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserShopRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade); // When user is deleted, remove their shop roles
            entity.HasOne(e => e.Shop)
                .WithMany(s => s.UserShopRoles)
                .HasForeignKey(e => e.ShopId)
                .OnDelete(DeleteBehavior.Cascade); // When shop is deleted, remove user roles for that shop
            entity.HasIndex(e => new { e.UserId, e.ShopId });
        });

        // Configure Customer entity
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.ZipCode).HasMaxLength(10);
            entity.HasOne(e => e.Shop)
                .WithMany(s => s.Customers)
                .HasForeignKey(e => e.ShopId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent shop deletion if customers exist
            entity.HasIndex(e => new { e.ShopId, e.Phone });
        });

        // Configure Repair entity
        modelBuilder.Entity<Repair>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RepairNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Issue).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Comments).HasMaxLength(1000);
            entity.Property(e => e.Cost).HasColumnType("decimal(18,2)");
            
            entity.HasOne(e => e.Shop)
                .WithMany(s => s.Repairs)
                .HasForeignKey(e => e.ShopId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent shop deletion if repairs exist
            
            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Repairs)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent customer deletion if repairs exist
            
            // Brand relation for repair
            entity.HasOne(e => e.Brand)
                .WithMany()
                .HasForeignKey(e => e.BrandId)
                .OnDelete(DeleteBehavior.SetNull); // Allow brand deletion, set to null
            
            // Device categorization relations
            entity.HasOne(e => e.DeviceCategory)
                .WithMany()
                .HasForeignKey(e => e.DeviceCategoryId)
                .OnDelete(DeleteBehavior.SetNull); // Allow category deletion, set to null
            
            entity.HasOne(e => e.DeviceModel)
                .WithMany()
                .HasForeignKey(e => e.DeviceModelId)
                .OnDelete(DeleteBehavior.SetNull); // Allow model deletion, set to null
            
            // User tracking relations
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction); // No action on user deletion to avoid cascade conflicts
            
            entity.HasOne(e => e.ModifiedByUser)
                .WithMany()
                .HasForeignKey(e => e.ModifiedBy)
                .OnDelete(DeleteBehavior.NoAction); // No action on user deletion to avoid cascade conflicts
            
            entity.HasIndex(e => e.RepairNumber).IsUnique();
            entity.HasIndex(e => e.ShopId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.BrandId);
            entity.HasIndex(e => e.DeviceCategoryId);
            entity.HasIndex(e => e.DeviceModelId);
        });

        // Configure Inventory entity
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CostPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RetailPrice).HasColumnType("decimal(18,2)");
            
            entity.HasOne(e => e.Shop)
                .WithMany(s => s.Inventories)
                .HasForeignKey(e => e.ShopId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent shop deletion if inventory exists
            
            entity.HasOne(e => e.Device)
                .WithMany()
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => new { e.ShopId, e.DeviceType });
            entity.HasIndex(e => new { e.ShopId, e.Stock });
        });

        // Configure InventoryAdjustment entity
        modelBuilder.Entity<InventoryAdjustment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AdjustmentType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Reason).HasMaxLength(200);
            
            entity.HasOne(e => e.Inventory)
                .WithMany(i => i.Adjustments)
                .HasForeignKey(e => e.InventoryId)
                .OnDelete(DeleteBehavior.Cascade); // When inventory is deleted, remove adjustments
            
            entity.HasIndex(e => new { e.InventoryId, e.AdjustmentDate });
        });

        // Configure Brand entity
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.DisplayOrder);
        });

        // Configure DeviceCategory entity
        modelBuilder.Entity<DeviceCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.DisplayOrder);
        });

        // Configure DeviceModel entity
        modelBuilder.Entity<DeviceModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ModelNumber).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            
            entity.HasOne(e => e.Brand)
                .WithMany(b => b.DeviceModels)
                .HasForeignKey(e => e.BrandId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.DeviceCategory)
                .WithMany(c => c.DeviceModels)
                .HasForeignKey(e => e.DeviceCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => new { e.BrandId, e.DeviceCategoryId, e.Name }).IsUnique();
            entity.HasIndex(e => e.DisplayOrder);
        });

        // Configure InventoryCategory entity
        modelBuilder.Entity<InventoryCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.DisplayOrder);
        });

        // Configure InventoryItem entity
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.SKU).HasMaxLength(50);
            entity.Property(e => e.CostPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RetailPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.WholesalePrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.Shop)
                .WithMany()
                .HasForeignKey(e => e.ShopId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Brand)
                .WithMany(b => b.InventoryItems)
                .HasForeignKey(e => e.BrandId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.DeviceCategory)
                .WithMany(c => c.InventoryItems)
                .HasForeignKey(e => e.DeviceCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.DeviceModel)
                .WithMany(m => m.InventoryItems)
                .HasForeignKey(e => e.DeviceModelId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.InventoryCategory)
                .WithMany(c => c.InventoryItems)
                .HasForeignKey(e => e.InventoryCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => e.SKU).IsUnique();
            entity.HasIndex(e => new { e.ShopId, e.CurrentStock });
            entity.HasIndex(e => new { e.ShopId, e.BrandId, e.DeviceModelId });
        });

        // Configure StockMovement entity
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MovementType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(50);
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");
            
            entity.HasOne(e => e.InventoryItem)
                .WithMany(i => i.StockMovements)
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => new { e.InventoryItemId, e.CreatedDate });
            entity.HasIndex(e => e.MovementType);
        });

        // Configure LookupValue entity
        modelBuilder.Entity<LookupValue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.ColorCode).HasMaxLength(20);
            entity.HasIndex(e => new { e.Category, e.Value }).IsUnique();
            entity.HasIndex(e => new { e.Category, e.DisplayOrder });
        });

        // Update Device entity configuration for new lookup relationships
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IMEISerialNumber).HasMaxLength(50);
            entity.Property(e => e.NetworkStatus).HasMaxLength(20);
            entity.Property(e => e.ScratchesCondition).HasMaxLength(20);
            entity.Property(e => e.BuyingPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SellingPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Source).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.Shop)
                .WithMany(s => s.Devices)
                .HasForeignKey(e => e.ShopId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Brand)
                .WithMany(b => b.Devices)
                .HasForeignKey(e => e.BrandId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.DeviceCategory)
                .WithMany(c => c.Devices)
                .HasForeignKey(e => e.DeviceCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.DeviceModel)
                .WithMany(m => m.Devices)
                .HasForeignKey(e => e.DeviceModelId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => new { e.ShopId, e.BrandId, e.DeviceModelId });
            entity.HasIndex(e => e.IMEISerialNumber);
        });

        // Configure Category entity
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.CategoryType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);

            // Self-referencing relationship for parent/child categories
            entity.HasOne(e => e.ParentCategory)
                .WithMany(e => e.SubCategories)
                .HasForeignKey(e => e.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.CategoryType, e.DisplayOrder });
            entity.HasIndex(e => e.Name);
        });

        // Configure LookupValue relationship with Category
        modelBuilder.Entity<LookupValue>(entity =>
        {
            entity.HasOne(e => e.CategoryEntity)
                .WithMany(c => c.LookupValues)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Sale entity
        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);
            
            entity.HasOne(e => e.Shop)
                .WithMany()
                .HasForeignKey(e => e.ShopId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => new { e.ShopId, e.SaleDate });
            entity.HasIndex(e => e.CustomerId);
        });

        // Configure SaleItem entity
        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ItemType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OriginalPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(500);
            
            entity.HasOne(e => e.Sale)
                .WithMany(s => s.SaleItems)
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.SaleId);
            entity.HasIndex(e => new { e.ItemType, e.ItemReferenceId });
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

