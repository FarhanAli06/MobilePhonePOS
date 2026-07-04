using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Empire.Domain.Entities;
using Empire.Domain.Common; // Added to resolve AuditableEntity errors
using Empire.Domain.Enums;   // Added for enums if needed

namespace Empire.Infrastructure.Data;

public class EmpireDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    
    public EmpireDbContext(DbContextOptions<EmpireDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Shop> Shops { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserShopRole> UserShopRoles { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<Repair> Repairs { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<InventoryAdjustment> InventoryAdjustments { get; set; }

    // Lookup Tables (now shop-scoped)
    public DbSet<Brand> Brands { get; set; }
    public DbSet<DeviceCategory> DeviceCategories { get; set; }
    public DbSet<DeviceModel> DeviceModels { get; set; }
    public DbSet<InventoryCategory> InventoryCategories { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<LookupValue> LookupValues { get; set; }
    public DbSet<Styling> Stylings { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleItem> SaleItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<RepairPart> RepairParts { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }

    // ── Repair Activity / Audit Log ───────────────────────────────────────────
    public DbSet<RepairActivity> RepairActivities { get; set; }

    // ── Repair Payment Ledger ─────────────────────────────────────────────────
    public DbSet<RepairPayment> RepairPayments { get; set; }

    // Page permission tables
    public DbSet<Page> Pages { get; set; }
    public DbSet<UserPagePermission> UserPagePermissions { get; set; }

    // ── Logging ───────────────────────────────────────────────────────────────
    public DbSet<RequestLog> RequestLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Shop ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<Shop>(entity =>
        {
            entity.HasMany(s => s.Customers).WithOne(c => c.Shop).HasForeignKey(c => c.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(s => s.Repairs).WithOne(r => r.Shop).HasForeignKey(r => r.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(s => s.Sales).WithOne(sa => sa.Shop).HasForeignKey(sa => sa.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(s => s.Inventories).WithOne(i => i.Shop).HasForeignKey(i => i.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(s => s.Brands).WithOne(b => b.Shop).HasForeignKey(b => b.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(s => s.DeviceCategories).WithOne(dc => dc.Shop).HasForeignKey(dc => dc.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(s => s.DeviceModels).WithOne(dm => dm.Shop).HasForeignKey(dm => dm.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(s => s.Categories).WithOne(c => c.Shop).HasForeignKey(c => c.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(s => s.InventoryCategories).WithOne(ic => ic.Shop).HasForeignKey(ic => ic.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(s => s.Items).WithOne(it => it.Shop).HasForeignKey(it => it.ShopId).OnDelete(DeleteBehavior.Restrict);
            // NOTE: LookupValues are NOT FK-linked to Shops because ShopId = 0 means "global".
            // The ShopId column is a plain filter column, not a FK constraint.
            entity.HasMany(s => s.UserPagePermissions).WithOne().HasForeignKey(up => up.ShopId).OnDelete(DeleteBehavior.Restrict);
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

        // ── Customer ──────────────────────────────────────────────────────────
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

        // ── Sale ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SaleNumber).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.SaleNumber).IsUnique();
            entity.HasMany(s => s.SaleItems).WithOne(si => si.Sale).HasForeignKey(si => si.SaleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(s => s.Payments).WithOne(p => p.Sale).HasForeignKey(p => p.SaleId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── SaleItem ──────────────────────────────────────────────────────────
        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(si => si.InventoryItem).WithMany(i => i.SaleItems).HasForeignKey(si => si.InventoryItemId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(si => si.Repair).WithMany().HasForeignKey(si => si.RepairId).OnDelete(DeleteBehavior.SetNull);
            entity.Property(e => e.ItemName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Note).HasMaxLength(1000);
            entity.Property(e => e.IsCustomItem);
            entity.Property(e => e.IsTaxable);
        });

        // ── Repair ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Repair>(entity =>
        {
             entity.HasOne(e => e.Company).WithMany(c => c.Repairs).HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.SetNull);
             entity.HasMany(e => e.RepairParts).WithOne(rp => rp.Repair).HasForeignKey(rp => rp.RepairId).OnDelete(DeleteBehavior.Cascade);
             entity.Property(e => e.AmountPaid).HasPrecision(18, 2).HasDefaultValue(0m);
             entity.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedBy).OnDelete(DeleteBehavior.Restrict);
             entity.HasOne(e => e.ModifiedByUser).WithMany().HasForeignKey(e => e.ModifiedBy).OnDelete(DeleteBehavior.Restrict);
        });
        
        // ── RepairPayment ─────────────────────────────────────────────────────
        modelBuilder.Entity<RepairPayment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Repair).WithMany(r => r.RepairPayments).HasForeignKey(e => e.RepairId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Reference).HasMaxLength(255);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasIndex(e => e.RepairId);
            entity.HasIndex(e => e.PaidAt);
        });

        // ── RepairActivity ────────────────────────────────────────────────────
        modelBuilder.Entity<RepairActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Repair).WithMany().HasForeignKey(e => e.RepairId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.PerformedByUser).WithMany().HasForeignKey(e => e.PerformedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.OldValues).HasMaxLength(2000);
            entity.Property(e => e.NewValues).HasMaxLength(2000);
            entity.HasIndex(e => e.RepairId);
            entity.HasIndex(e => e.PerformedAt);
        });

        // ── RepairPart ────────────────────────────────────────────────────────
        modelBuilder.Entity<RepairPart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Repair).WithMany(r => r.RepairParts).HasForeignKey(e => e.RepairId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.InventoryItem).WithMany().HasForeignKey(e => e.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Ignore(e => e.TotalPrice); // Computed property, not stored in database
        });

        // ── Device ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasOne(e => e.Shop).WithMany(s => s.Devices).HasForeignKey(e => e.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Brand).WithMany().HasForeignKey(e => e.BrandId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DeviceCategory).WithMany().HasForeignKey(e => e.DeviceCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DeviceModel).WithMany().HasForeignKey(e => e.DeviceModelId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Company).WithMany(c => c.Devices).HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.SoldToCustomer).WithMany(c => c.Devices).HasForeignKey(e => e.SoldToCustomerId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── InventoryItem ─────────────────────────────────────────────────────
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasOne(e => e.Shop).WithMany().HasForeignKey(e => e.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Brand).WithMany().HasForeignKey(e => e.BrandId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DeviceCategory).WithMany().HasForeignKey(e => e.DeviceCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DeviceModel).WithMany().HasForeignKey(e => e.DeviceModelId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InventoryCategory).WithMany().HasForeignKey(e => e.InventoryCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Category).WithMany(c => c.InventoryItems).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.SetNull);
        });

        // ── Payment ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        
        // ── StockMovement ─────────────────────────────────────────────────────
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.InventoryItem).WithMany(i => i.StockMovements).HasForeignKey(e => e.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.MovementType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.UnitCost).HasPrecision(18, 2);
            entity.Property(e => e.TotalCost).HasPrecision(18, 2);
        });
        
        // ── InventoryTransaction ──────────────────────────────────────────────
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

        // ── Brand (shop-scoped) ───────────────────────────────────────────────
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Shop).WithMany(s => s.Brands).HasForeignKey(e => e.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ShopId, e.Name }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        // ── DeviceCategory (shop-scoped) ──────────────────────────────────────
        modelBuilder.Entity<DeviceCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Shop).WithMany(s => s.DeviceCategories).HasForeignKey(e => e.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ShopId, e.Name }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        // ── DeviceModel (shop-scoped) ─────────────────────────────────────────
        modelBuilder.Entity<DeviceModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Shop).WithMany(s => s.DeviceModels).HasForeignKey(e => e.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Brand).WithMany(b => b.DeviceModels).HasForeignKey(e => e.BrandId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.DeviceCategory).WithMany(dc => dc.DeviceModels).HasForeignKey(e => e.DeviceCategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── InventoryCategory (shop-scoped) ───────────────────────────────────
        modelBuilder.Entity<InventoryCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Shop).WithMany(s => s.InventoryCategories).HasForeignKey(e => e.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ShopId, e.Name }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        // ── Item (shop-scoped) ────────────────────────────────────────────────
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasOne(e => e.Shop).WithMany(s => s.Items).HasForeignKey(e => e.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ShopId, e.Name }).IsUnique().HasFilter("[IsDeleted] = 0");
            entity.HasMany(e => e.InventoryItems).WithOne(i => i.Item).HasForeignKey(i => i.ItemId).OnDelete(DeleteBehavior.Restrict);
        });

         // ── LookupValue (shop-scoped, 0 = global) ────────────────────────
        // ShopId = 0 means the value is system-wide (global); ShopId > 0 means shop-specific.
        // There is intentionally NO foreign key from ShopId to the Shops table so that
        // global values (ShopId = 0) can exist without a matching Shop row.
        modelBuilder.Entity<LookupValue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ShopId).HasDefaultValue(0);
            entity.HasIndex(e => new { e.ShopId, e.Category, e.Value }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        // ── Category (shop-scoped) ────────────────────────────────────────────
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CategoryType).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Shop).WithMany(s => s.Categories).HasForeignKey(e => e.ShopId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.ShopId, e.Name, e.CategoryType }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        // ── Page permission tables ────────────────────────────────────────────
        modelBuilder.Entity<Page>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PageKey).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.PageKey).IsUnique();
            entity.Property(e => e.ControllerName).HasMaxLength(100);
            entity.Property(e => e.ActionName).HasMaxLength(100);
            entity.Property(e => e.Icon).HasMaxLength(100);
            entity.Property(e => e.GroupName).HasMaxLength(100);
            // Ensure DB-level DEFAULT constraints so raw SQL inserts that omit these columns still succeed
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
            entity.HasOne(e => e.ParentPage)
                  .WithMany(e => e.ChildPages)
                  .HasForeignKey(e => e.ParentPageId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserPagePermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Unique per user + page + shop combination
            entity.HasIndex(e => new { e.UserId, e.PageId, e.ShopId }).IsUnique();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Page)
                  .WithMany(e => e.UserPagePermissions)
                  .HasForeignKey(e => e.PageId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Shop)
                  .WithMany()
                  .HasForeignKey(e => e.ShopId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
        });

        // ── Styling ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Styling>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Icon).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Color).IsRequired().HasMaxLength(20);
            entity.Property(e => e.TextColor).IsRequired().HasMaxLength(20);
            entity.Property(e => e.BadgeVariant).IsRequired().HasMaxLength(30);
            entity.HasIndex(e => e.Name).IsUnique().HasFilter("[Name] <> ''");
        });

        // ── RequestLog (no FK constraints — standalone audit table) ────────────
        modelBuilder.Entity<RequestLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Source).IsRequired().HasMaxLength(10);
            entity.Property(e => e.HttpMethod).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(500);
            entity.Property(e => e.QueryString).HasMaxLength(1000);
            entity.Property(e => e.RequestBody).HasMaxLength(4096);
            entity.Property(e => e.RequestHeaders).HasMaxLength(2000);
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.ResponseBody).HasMaxLength(4096);
            entity.Property(e => e.ErrorMessage).HasMaxLength(4096);
            // Ignore computed property
            entity.Ignore(e => e.IsError);
            // Indexes for common query patterns
            entity.HasIndex(e => e.RequestedAtUtc);
            entity.HasIndex(e => e.StatusCode);
            entity.HasIndex(e => e.Source);
            entity.HasIndex(e => e.ShopId);
            entity.HasIndex(e => e.Username);
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
