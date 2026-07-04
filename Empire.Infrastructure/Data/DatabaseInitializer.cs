using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Empire.Domain.Entities;
using Empire.Application.Interfaces;

namespace Empire.Infrastructure.Data
{
    /// <summary>
    /// Service responsible for database initialization, migrations, and seeding.
    /// </summary>
    public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly EmpireDbContext _context;
        private readonly IPasswordHashService _passwordHashService;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(
            EmpireDbContext context,
            IPasswordHashService passwordHashService,
            ILogger<DatabaseInitializer> logger)
        {
            _context = context;
            _passwordHashService = passwordHashService;
            _logger = logger;
        }

        /// <summary>
        /// Initialize the database by applying migrations and seeding initial data.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // Apply all pending migrations.
                // MigrateAsync creates the database (if it does not exist), creates the
                // __EFMigrationsHistory table, and runs every pending migration in order.
                // We must NOT call EnsureCreatedAsync before this — it would create all
                // tables directly from the EF model without recording any migration history,
                // which causes "Invalid object name 'Roles'" and similar errors because
                // subsequent MigrateAsync calls see the migration as pending but the tables
                // already exist (or were created in the wrong order).
                _logger.LogInformation("Applying database migrations...");
                await _context.Database.MigrateAsync();
                _logger.LogInformation("Database migrations applied successfully.");

                // Ensure RequestLogs table exists — safety net for databases that were
                // created before the RequestLogs migration was added.
                await EnsureRequestLogsTableAsync();

                // Seed initial data.
                await SeedDataAsync();

                // Ensure new RepairStatus lookup values exist in databases that were
                // seeded before Unrepairable and Abandoned were introduced.
                await EnsureRepairStatusesAsync();

                // Ensure SaleItems.RepairId column exists — added after initial schema.
                await EnsureSaleItemRepairIdColumnAsync();

                // Ensure IX_Stylings_Name has a WHERE filter so empty-name rows
                // don't cause duplicate key errors during seeding.
                await EnsureStylingIndexFilterAsync();
                // Ensure Repairs.AmountPaid column exists — added after initial schema.
                await EnsureAmountPaidColumnAsync();
                // Ensure InventoryItems.CategoryId column exists — added after initial schema.
                await EnsureCategoryIdColumnAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database: {Message}", ex.Message);
                throw;
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the given table exists in the current database.
        /// </summary>
        private async Task<bool> TableExistsAsync(string tableName)
        {
            try
            {
                var conn = _context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES " +
                    "WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME = @tableName";

                var param = cmd.CreateParameter();
                param.ParameterName = "@tableName";
                param.Value = tableName;
                cmd.Parameters.Add(param);

                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Removes all rows from __EFMigrationsHistory so that MigrateAsync
        /// will treat every migration as pending and re-apply them.
        /// </summary>
        private async Task ClearMigrationHistoryAsync()
        {
            try
            {
                // __EFMigrationsHistory may not exist yet on a brand-new DB.
                bool historyExists = await TableExistsAsync("__EFMigrationsHistory");
                if (historyExists)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM [__EFMigrationsHistory]");
                    _logger.LogInformation("Cleared __EFMigrationsHistory.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not clear __EFMigrationsHistory: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Ensures the RequestLogs table exists. This is a safety net for databases that were
        /// created with EnsureCreatedAsync before the AddRequestLogs migration was added.
        /// If MigrateAsync already created the table this method is a no-op.
        /// </summary>
        private async Task EnsureRequestLogsTableAsync()
        {
            try
            {
                bool exists = await TableExistsAsync("RequestLogs");
                if (!exists)
                {
                    _logger.LogWarning("RequestLogs table not found — creating it manually...");
                    await _context.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RequestLogs')
                        BEGIN
                            CREATE TABLE [RequestLogs] (
                                [Id]                  BIGINT IDENTITY(1,1) NOT NULL,
                                [Source]              NVARCHAR(10)   NOT NULL DEFAULT 'API',
                                [HttpMethod]          NVARCHAR(10)   NOT NULL,
                                [Path]                NVARCHAR(500)  NOT NULL,
                                [QueryString]         NVARCHAR(1000) NULL,
                                [RequestBody]         NVARCHAR(4000) NULL,
                                [RequestHeaders]      NVARCHAR(2000) NULL,
                                [UserId]              NVARCHAR(100)  NULL,
                                [Username]            NVARCHAR(100)  NULL,
                                [ShopId]              INT            NULL,
                                [IpAddress]           NVARCHAR(50)   NULL,
                                [UserAgent]           NVARCHAR(500)  NULL,
                                [StatusCode]          INT            NOT NULL,
                                [ResponseBody]        NVARCHAR(4000) NULL,
                                [ElapsedMilliseconds] BIGINT         NOT NULL,
                                [RequestedAtUtc]      DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                                [ErrorMessage]        NVARCHAR(4000) NULL,
                                CONSTRAINT [PK_RequestLogs] PRIMARY KEY ([Id])
                            );
                            CREATE INDEX [IX_RequestLogs_RequestedAtUtc] ON [RequestLogs] ([RequestedAtUtc]);
                            CREATE INDEX [IX_RequestLogs_StatusCode]     ON [RequestLogs] ([StatusCode]);
                            CREATE INDEX [IX_RequestLogs_Source]         ON [RequestLogs] ([Source]);
                            CREATE INDEX [IX_RequestLogs_ShopId]         ON [RequestLogs] ([ShopId]);
                            CREATE INDEX [IX_RequestLogs_Username]       ON [RequestLogs] ([Username]);
                        END");
                    _logger.LogInformation("RequestLogs table created successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not ensure RequestLogs table: {Message}", ex.Message);
            }
        }

        // ─── Seeding ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Seed all initial data: Roles, LookupValues, superadmin User, default Shop, and UserShopRole.
        /// All steps are idempotent — safe to run on an already-seeded database.
        /// </summary>
        private async Task SeedDataAsync()
        {
            try
            {
                // ── 1. Roles ──────────────────────────────────────────────────────
                if (!await _context.Roles.AnyAsync())
                {
                    _logger.LogInformation("Seeding default roles...");
                    _context.Roles.AddRange(
                        new Role { Name = "SuperAdmin",  Description = "Super Administrator with full access", IsActive = true, DisplayOrder = 1, CreatedAt = DateTime.UtcNow },
                        new Role { Name = "Manager",     Description = "Shop Manager",                        IsActive = true, DisplayOrder = 2, CreatedAt = DateTime.UtcNow },
                        new Role { Name = "Technician",  Description = "Repair Technician",                   IsActive = true, DisplayOrder = 3, CreatedAt = DateTime.UtcNow }
                    );
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Default roles seeded.");
                }

                // ── 2. LookupValues ───────────────────────────────────────────────
                // Global lookup values use ShopId = 0 (no FK to Shops — see EmpireDbContext).
                // These are system-wide defaults available to all shops.
                if (!await _context.LookupValues.AnyAsync())
                {
                    _logger.LogInformation("Seeding default lookup values...");
                    _context.LookupValues.AddRange(
                        // RepairStatus
                        new LookupValue { ShopId = 0, Category = "RepairStatus",       Value = "InProgress",      Description = "Repair is in progress",          IsActive = true, DisplayOrder = 1, ColorCode = "#FFA500", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "RepairStatus",       Value = "Pending",         Description = "Waiting to be started",          IsActive = true, DisplayOrder = 2, ColorCode = "#808080", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "RepairStatus",       Value = "Completed",       Description = "Repair completed",               IsActive = true, DisplayOrder = 3, ColorCode = "#008000", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "RepairStatus",       Value = "Cancelled",       Description = "Repair cancelled",               IsActive = true, DisplayOrder = 4, ColorCode = "#FF0000", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "RepairStatus",       Value = "WaitingForParts", Description = "Waiting for parts",              IsActive = true, DisplayOrder = 5, ColorCode = "#0000FF", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "RepairStatus",       Value = "Diagnose",        Description = "Under diagnosis",                IsActive = true, DisplayOrder = 6, ColorCode = "#800080", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "RepairStatus",       Value = "Unrepairable",    Description = "Device cannot be repaired",      IsActive = true, DisplayOrder = 7, ColorCode = "#8B0000", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "RepairStatus",       Value = "Abandoned",       Description = "Customer abandoned the device",  IsActive = true, DisplayOrder = 8, ColorCode = "#696969", CreatedDate = DateTime.UtcNow },
                        // PaymentStatus
                        new LookupValue { ShopId = 0, Category = "PaymentStatus",      Value = "Unpaid",          Description = "Not yet paid",                   IsActive = true, DisplayOrder = 1, ColorCode = "#FF0000", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "PaymentStatus",      Value = "Partial",         Description = "Partially paid",                 IsActive = true, DisplayOrder = 2, ColorCode = "#FFA500", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "PaymentStatus",      Value = "Paid",            Description = "Fully paid",                     IsActive = true, DisplayOrder = 3, ColorCode = "#008000", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "PaymentStatus",      Value = "Refunded",        Description = "Payment refunded",               IsActive = true, DisplayOrder = 4, ColorCode = "#808080", CreatedDate = DateTime.UtcNow },
                        // NetworkStatus
                        new LookupValue { ShopId = 0, Category = "NetworkStatus",      Value = "Unlocked",        Description = "Device is unlocked",             IsActive = true, DisplayOrder = 1, ColorCode = "#008000", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "NetworkStatus",      Value = "Locked",          Description = "Device is carrier locked",       IsActive = true, DisplayOrder = 2, ColorCode = "#FF0000", CreatedDate = DateTime.UtcNow },
                        // ScratchesCondition
                        new LookupValue { ShopId = 0, Category = "ScratchesCondition", Value = "Excellent",       Description = "No scratches",                   IsActive = true, DisplayOrder = 1, ColorCode = "#008000", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "ScratchesCondition", Value = "Good",            Description = "Minor scratches",                IsActive = true, DisplayOrder = 2, ColorCode = "#90EE90", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "ScratchesCondition", Value = "Fair",            Description = "Visible scratches",              IsActive = true, DisplayOrder = 3, ColorCode = "#FFA500", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "ScratchesCondition", Value = "Poor",            Description = "Heavy scratches or damage",      IsActive = true, DisplayOrder = 4, ColorCode = "#FF0000", CreatedDate = DateTime.UtcNow },
                        // PhoneStatus
                        new LookupValue { ShopId = 0, Category = "PhoneStatus",        Value = "Available",       Description = "Available for sale",             IsActive = true, DisplayOrder = 1, ColorCode = "#008000", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "PhoneStatus",        Value = "Sold",            Description = "Device has been sold",           IsActive = true, DisplayOrder = 2, ColorCode = "#808080", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "PhoneStatus",        Value = "NotForSale",      Description = "Not available for sale",         IsActive = true, DisplayOrder = 3, ColorCode = "#FF0000", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "PhoneStatus",        Value = "ForParts",        Description = "For parts only",                 IsActive = true, DisplayOrder = 4, ColorCode = "#FFA500", CreatedDate = DateTime.UtcNow },
                        // PaymentMethod
                        new LookupValue { ShopId = 0, Category = "PaymentMethod",      Value = "Cash",            Description = "Cash payment",                   IsActive = true, DisplayOrder = 1, ColorCode = "#008000", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "PaymentMethod",      Value = "Card",            Description = "Credit/Debit card",              IsActive = true, DisplayOrder = 2, ColorCode = "#0000FF", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "PaymentMethod",      Value = "Zelle",           Description = "Zelle transfer",                 IsActive = true, DisplayOrder = 3, ColorCode = "#6A0DAD", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "PaymentMethod",      Value = "Venmo",           Description = "Venmo transfer",                 IsActive = true, DisplayOrder = 4, ColorCode = "#008CFF", CreatedDate = DateTime.UtcNow },
                        new LookupValue { ShopId = 0, Category = "PaymentMethod",      Value = "Check",           Description = "Check payment",                  IsActive = true, DisplayOrder = 5, ColorCode = "#808080", CreatedDate = DateTime.UtcNow }
                    );
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Default lookup values seeded.");
                }

                // ── 3. Superadmin user ────────────────────────────────────────────
                User superAdmin;
                if (!await _context.Users.AnyAsync(u => u.Username == "superadmin"))
                {
                    _logger.LogInformation("Creating super admin user...");
                    superAdmin = new User
                    {
                        Username     = "superadmin",
                        Email        = "superadmin@empiresolution.com",
                        FirstName    = "Super",
                        LastName     = "Admin",
                        PasswordHash = _passwordHashService.HashPassword("Admin@123"),
                        IsActive     = true,
                        CreatedDate  = DateTime.UtcNow
                    };
                    _context.Users.Add(superAdmin);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Super admin user created (Id={Id}).", superAdmin.Id);
                }
                else
                {
                    superAdmin = await _context.Users.FirstAsync(u => u.Username == "superadmin");
                    _logger.LogInformation("Super admin user already exists (Id={Id}), skipping.", superAdmin.Id);
                }

                // ── 4. Default shop ───────────────────────────────────────────────
                if (!await _context.Shops.AnyAsync())
                {
                    _logger.LogInformation("Creating default shop...");
                    var defaultShop = new Shop
                    {
                        Name        = "Empire Solution",
                        Address     = "123 Main Street",
                        City        = "New York",
                        State       = "NY",
                        ZipCode     = "10001",
                        Phone       = "555-0100",
                        Email       = "shop@empiresolution.com",
                        IsActive    = true,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.Shops.Add(defaultShop);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Default shop created (Id={Id}).", defaultShop.Id);

                    // Assign superadmin to the default shop with SuperAdmin role
                    var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
                    if (superAdminRole != null)
                    {
                        _context.UserShopRoles.Add(new UserShopRole
                        {
                            UserId       = superAdmin.Id,
                            ShopId       = defaultShop.Id,
                            RoleId       = superAdminRole.Id,
                            IsActive     = true,
                            AssignedDate = DateTime.UtcNow
                        });
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("SuperAdmin assigned to default shop.");
                    }
                }
                else
                {
                    // Ensure the superadmin has at least one UserShopRole even if the shop was
                    // created manually or by a previous run.
                    bool hasShopRole = await _context.UserShopRoles
                        .AnyAsync(r => r.UserId == superAdmin.Id);

                    if (!hasShopRole)
                    {
                        var firstShop      = await _context.Shops.FirstAsync();
                        var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");

                        if (superAdminRole != null)
                        {
                            _context.UserShopRoles.Add(new UserShopRole
                            {
                                UserId       = superAdmin.Id,
                                ShopId       = firstShop.Id,
                                RoleId       = superAdminRole.Id,
                                IsActive     = true,
                                AssignedDate = DateTime.UtcNow
                            });
                            await _context.SaveChangesAsync();
                            _logger.LogInformation(
                                "Assigned superadmin to existing shop (Id={ShopId}).", firstShop.Id);
                        }
                    }
                }

                // ── 5. Pages & SuperAdmin page permissions ────────────────────────
                // Use raw SQL IF NOT EXISTS INSERT to guarantee idempotency regardless
                // of EF change-tracker state or partial previous runs.
                await UpsertPageAsync(null, "dashboard",        "Dashboard",        "Home",                  "Dashboard",    "fas fa-tachometer-alt",      1,  "Main Menu");
                await UpsertPageAsync(null, "repairs",           "Repairs",           "Repairs",               "Index",        "fas fa-tools",               2,  "Main Menu");
                await UpsertPageAsync(null, "pos",               "POS",               "POS",                   "Index",        "fas fa-cash-register",       3,  "Main Menu");
                await UpsertPageAsync(null, "customers",         "Customers",         "Customers",             "Index",        "fas fa-users",               4,  "Main Menu");
                await UpsertPageAsync(null, "devices",           "Devices",           "Devices",               "Index",        "fas fa-mobile-alt",          5,  "Main Menu");
                await UpsertPageAsync(null, "inventory",         "Inventory",         null,                    null,           "fas fa-boxes",               6,  "Main Menu");
                await UpsertPageAsync(null, "reports",           "Reports",           null,                    null,           "fas fa-chart-bar",           9,  "Main Menu");
                await UpsertPageAsync(null, "lookup_management", "Lookup Management", null,                    null,           "fas fa-cogs",                13, "Administration");
                await UpsertPageAsync(null, "shops",             "Shops",             "Shop",                  "Index",        "fas fa-store",               16, "Administration");
                await UpsertPageAsync(null, "users",             "Users",             "Users",                 "Index",        "fas fa-user-cog",            17, "Administration");
                await UpsertPageAsync(null, "page_permissions",  "Page Permissions",  "PagePermissions",       "Index",        "fas fa-shield-alt",          18, "Administration");

                // Child pages — resolve parent IDs by PageKey from DB.
                var inventoryParentId      = await _context.Pages.Where(x => x.PageKey == "inventory")          .Select(x => (int?)x.Id).FirstOrDefaultAsync();
                var reportsParentId        = await _context.Pages.Where(x => x.PageKey == "reports")            .Select(x => (int?)x.Id).FirstOrDefaultAsync();
                var lookupMgmtParentId     = await _context.Pages.Where(x => x.PageKey == "lookup_management")  .Select(x => (int?)x.Id).FirstOrDefaultAsync();

                await UpsertPageAsync(inventoryParentId,  "inventory_items",  "Inventory Items", "Inventories",           "Index",        "fas fa-box",                 7,  "Main Menu");
                await UpsertPageAsync(inventoryParentId,  "stock_movements",  "Stock Movements", "InventoryTransactions", "Index",        "fas fa-exchange-alt",        8,  "Main Menu");
                await UpsertPageAsync(reportsParentId,    "reports_sales",    "Sales Report",    "Reports",               "SalesReport",  "fas fa-file-invoice-dollar", 10, "Main Menu");
                await UpsertPageAsync(reportsParentId,    "reports_repairs",  "Repair Report",   "Reports",               "RepairReport", "fas fa-file-medical",        11, "Main Menu");
                await UpsertPageAsync(reportsParentId,    "reports_profit",   "Profit Report",   "Reports",               "ProfitReport", "fas fa-dollar-sign",         12, "Main Menu");
                await UpsertPageAsync(lookupMgmtParentId, "general_lookups",  "General Lookups", "LookupManagement",      "Index",        "fas fa-list",                14, "Administration");
                await UpsertPageAsync(lookupMgmtParentId, "companies",        "Companies",       "Company",               "Index",        "fas fa-building",            15, "Administration");
                await UpsertPageAsync(lookupMgmtParentId, "request_logs",     "Request Logs",    "Logs",                  "Index",        "fas fa-stream",              19, "Administration");

                _logger.LogInformation("Default pages ensured.");

                // ── 5c. Grant SuperAdmin access to ALL pages for ALL shops ────────
                // SuperAdmin permissions are scoped per shop (ShopId), just like any other user.
                // This ensures the DB is the single source of truth for page visibility.
                var allPageIds = await _context.Pages.Select(p => p.Id).ToListAsync();
                var allShopIds = await _context.Shops.Select(s => s.Id).ToListAsync();

                foreach (var shopId in allShopIds)
                {
                    foreach (var pageId in allPageIds)
                    {
                        bool alreadyGranted = await _context.UserPagePermissions
                            .AnyAsync(pp => pp.UserId == superAdmin.Id
                                         && pp.PageId  == pageId
                                         && pp.ShopId  == shopId);

                        if (!alreadyGranted)
                        {
                            _context.UserPagePermissions.Add(new UserPagePermission
                            {
                                UserId          = superAdmin.Id,
                                ShopId          = shopId,
                                PageId          = pageId,
                                IsGranted       = true,
                                GrantedByUserId = superAdmin.Id,
                                GrantedDate     = DateTime.UtcNow,
                                CreatedDate     = DateTime.UtcNow
                            });
                        }
                    }
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("SuperAdmin page permissions ensured for all shops.");

                // ── 6. Default Brands per shop ────────────────────────────────────────────
                // Seed a starter set of brands for each shop that has none yet.
                var defaultBrands = new[]
                {
                    new { Name = "Apple",   Icon = "phone_iphone",  Color = "#555555", Order = 1 },
                    new { Name = "Samsung", Icon = "smartphone",    Color = "#1428A0", Order = 2 },
                    new { Name = "Google",  Icon = "devices",       Color = "#4285F4", Order = 3 },
                    new { Name = "OnePlus", Icon = "phone_android", Color = "#F5010C", Order = 4 },
                    new { Name = "LG",      Icon = "phone_android", Color = "#A50034", Order = 5 },
                    new { Name = "Motorola",Icon = "phone_android", Color = "#E1000F", Order = 6 },
                };

                var shopIdsForBrands = await _context.Shops.Select(s => s.Id).ToListAsync();
                foreach (var sId in shopIdsForBrands)
                {
                    bool hasBrands = await _context.Brands.AnyAsync(b => b.ShopId == sId);
                    if (!hasBrands)
                    {
                        foreach (var b in defaultBrands)
                        {
                            var styling = new Styling { Name = b.Name, Icon = b.Icon, Color = b.Color, CreatedAt = DateTime.UtcNow };
                            _context.Add(styling);
                            await _context.SaveChangesAsync();

                            _context.Brands.Add(new Brand
                            {
                                ShopId       = sId,
                                Name         = b.Name,
                                Description  = b.Name + " devices",
                                IsActive     = true,
                                DisplayOrder = b.Order,
                                StylingId    = styling.Id,
                                CreatedDate  = DateTime.UtcNow
                            });
                        }
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Default brands seeded for shop {ShopId}.", sId);
                    }
                }

                // ── 7. Default Device Categories per shop ──────────────────────────────────
                var defaultCategories = new[]
                {
                    new { Name = "Smartphone",  Icon = "smartphone",    Color = "#4CAF50", Order = 1 },
                    new { Name = "Tablet",       Icon = "tablet_mac",    Color = "#2196F3", Order = 2 },
                    new { Name = "Laptop",       Icon = "laptop",        Color = "#9C27B0", Order = 3 },
                    new { Name = "Smartwatch",   Icon = "watch",         Color = "#FF9800", Order = 4 },
                    new { Name = "Gaming",       Icon = "sports_esports",Color = "#F44336", Order = 5 },
                };

                foreach (var sId in shopIdsForBrands)
                {
                    bool hasCategories = await _context.DeviceCategories.AnyAsync(c => c.ShopId == sId);
                    if (!hasCategories)
                    {
                        foreach (var c in defaultCategories)
                        {
                            var styling = new Styling { Name = c.Name, Icon = c.Icon, Color = c.Color, CreatedAt = DateTime.UtcNow };
                            _context.Add(styling);
                            await _context.SaveChangesAsync();

                            _context.DeviceCategories.Add(new DeviceCategory
                            {
                                ShopId       = sId,
                                Name         = c.Name,
                                Description  = c.Name + " devices",
                                IsActive     = true,
                                DisplayOrder = c.Order,
                                StylingId    = styling.Id,
                                CreatedDate  = DateTime.UtcNow
                            });
                        }
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Default device categories seeded for shop {ShopId}.", sId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding data: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Ensures the two new RepairStatus lookup values exist in existing databases
        /// that were seeded before Unrepairable and Abandoned were added.
        /// Uses raw SQL IF NOT EXISTS so it is safe to run on every startup.
        /// </summary>
        private async Task EnsureRepairStatusesAsync()
        {
            var newStatuses = new[]
            {
                (Value: "Unrepairable", Description: "Device cannot be repaired",     ColorCode: "#8B0000", DisplayOrder: 7),
                (Value: "Abandoned",    Description: "Customer abandoned the device",  ColorCode: "#696969", DisplayOrder: 8),
            };
            foreach (var s in newStatuses)
            {
                var sql = @"
                    IF NOT EXISTS (SELECT 1 FROM [LookupValues] WHERE [Category] = 'RepairStatus' AND [Value] = @value)
                    BEGIN
                        INSERT INTO [LookupValues]
                            ([ShopId],[Category],[Value],[Description],[IsActive],[DisplayOrder],[ColorCode],[IsDeleted],[CreatedDate])
                        VALUES
                            (0, 'RepairStatus', @value, @description, 1, @displayOrder, @colorCode, 0, GETUTCDATE())
                    END";
                await _context.Database.ExecuteSqlRawAsync(sql,
                    new Microsoft.Data.SqlClient.SqlParameter("@value",        s.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@description",  s.Description),
                    new Microsoft.Data.SqlClient.SqlParameter("@colorCode",    s.ColorCode),
                    new Microsoft.Data.SqlClient.SqlParameter("@displayOrder", s.DisplayOrder));
            }
            _logger.LogInformation("Ensured Unrepairable and Abandoned RepairStatus lookup values.");
        }

        /// <summary>
        /// Inserts a Page row only if no row with the given PageKey already exists.
        /// Uses raw SQL so it is atomic and completely bypasses the EF change tracker,
        /// preventing duplicate-key errors on partial re-runs or concurrent startups.
        /// </summary>
        private async Task UpsertPageAsync(
            int?   parentPageId,
            string pageKey,
            string name,
            string? controllerName,
            string? actionName,
            string icon,
            int    displayOrder,
            string groupName)
        {
            var sql = @"
                IF NOT EXISTS (SELECT 1 FROM [Pages] WHERE [PageKey] = @pageKey)
                BEGIN
                    INSERT INTO [Pages]
                        ([Name],[PageKey],[ControllerName],[ActionName],[Icon],[ParentPageId],[DisplayOrder],[GroupName],[IsActive],[IsDeleted],[CreatedDate])
                    VALUES
                        (@name, @pageKey, @controllerName, @actionName, @icon, @parentPageId, @displayOrder, @groupName, 1, 0, GETUTCDATE())
                END";

            await _context.Database.ExecuteSqlRawAsync(sql,
                new Microsoft.Data.SqlClient.SqlParameter("@pageKey",        pageKey),
                new Microsoft.Data.SqlClient.SqlParameter("@name",           name),
                new Microsoft.Data.SqlClient.SqlParameter("@controllerName", (object?)controllerName ?? DBNull.Value),
                new Microsoft.Data.SqlClient.SqlParameter("@actionName",     (object?)actionName     ?? DBNull.Value),
                new Microsoft.Data.SqlClient.SqlParameter("@icon",           icon),
                new Microsoft.Data.SqlClient.SqlParameter("@parentPageId",   (object?)parentPageId   ?? DBNull.Value),
                new Microsoft.Data.SqlClient.SqlParameter("@displayOrder",   displayOrder),
                new Microsoft.Data.SqlClient.SqlParameter("@groupName",      groupName));
        }

        /// <summary>
        /// Ensures the RepairId column exists on SaleItems for databases created before
        /// the AddRepairIdToSaleItems migration was added.
        /// </summary>
        private async Task EnsureSaleItemRepairIdColumnAsync()
        {
            const string sql = @"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'SaleItems' AND COLUMN_NAME = 'RepairId'
                )
                BEGIN
                    ALTER TABLE SaleItems ADD RepairId INT NULL;
                    CREATE INDEX IX_SaleItems_RepairId ON SaleItems (RepairId);
                    ALTER TABLE SaleItems ADD CONSTRAINT FK_SaleItems_Repairs_RepairId
                        FOREIGN KEY (RepairId) REFERENCES Repairs(Id) ON DELETE SET NULL;
                END";
            await _context.Database.ExecuteSqlRawAsync(sql);
        }

        /// <summary>
        /// Recreates IX_Stylings_Name with a WHERE [Name] &lt;&gt; '' filter so that
        /// Styling rows without a name (legacy data) do not trigger duplicate key
        /// violations when multiple empty-name rows exist.
        /// Safe to run on every startup — checks the index filter before acting.
        /// </summary>
        private async Task EnsureStylingIndexFilterAsync()
        {
            const string sql = @"
                -- Only recreate if the index exists WITHOUT a filter
                IF EXISTS (
                    SELECT 1 FROM sys.indexes i
                    JOIN sys.objects o ON i.object_id = o.object_id
                    WHERE o.name = 'Stylings'
                      AND i.name = 'IX_Stylings_Name'
                      AND i.has_filter = 0
                )
                BEGIN
                    DROP INDEX [IX_Stylings_Name] ON [Stylings];
                    CREATE UNIQUE INDEX [IX_Stylings_Name] ON [Stylings] ([Name])
                    WHERE [Name] <> '';
                END";
            await _context.Database.ExecuteSqlRawAsync(sql);
        }

        /// <summary>
        /// Ensures the Repairs.AmountPaid column exists for databases created before
        /// the AddAmountPaidToRepairs migration was added. Safe to run on every startup.
        /// </summary>
        private async Task EnsureAmountPaidColumnAsync()
        {
            const string sql = @"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Repairs' AND COLUMN_NAME = 'AmountPaid'
                )
                BEGIN
                    ALTER TABLE [Repairs] ADD [AmountPaid] decimal(18,2) NOT NULL DEFAULT 0;
                END";
            await _context.Database.ExecuteSqlRawAsync(sql);
        }

        /// <summary>
        /// Ensures the InventoryItems.CategoryId column exists for databases created before
        /// the AddCategoryIdToInventoryItems migration was added. Safe to run on every startup.
        /// </summary>
        private async Task EnsureCategoryIdColumnAsync()
        {
            const string sql = @"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'InventoryItems' AND COLUMN_NAME = 'CategoryId'
                )
                BEGIN
                    ALTER TABLE [InventoryItems] ADD [CategoryId] INT NULL;
                    IF OBJECT_ID('FK_InventoryItems_Categories_CategoryId', 'F') IS NULL
                    BEGIN
                        ALTER TABLE [InventoryItems] ADD CONSTRAINT [FK_InventoryItems_Categories_CategoryId]
                            FOREIGN KEY ([CategoryId]) REFERENCES [Categories]([Id]) ON DELETE SET NULL;
                    END
                END";
            await _context.Database.ExecuteSqlRawAsync(sql);
        }
    }
}
