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

                // The model reads this column during repair queries, so create it
                // before seeding or any other model-backed startup work begins.
                await EnsureRepairPartCostColumnAsync();
                await EnsureDeviceColorAndCarrierColumnsAsync();
                await EnsureSalesAndDeviceAuditTablesAsync();

                // Ensure RequestLogs table exists — safety net for databases that were
                // created before the RequestLogs migration was added.
                await EnsureRequestLogsTableAsync();

                // Seed initial data.
                await SeedDataAsync();

                // Keep the Apple laptop selector useful on both existing and fresh
                // installations. This is idempotent and never changes user-created models.
                await EnsureMacBookModelsAsync();

                // Ensure new RepairStatus lookup values exist in databases that were
                // seeded before Unrepairable and Abandoned were introduced.
                await EnsureRepairStatusesAsync();

                // Ensure SaleItems.RepairId column exists — added after initial schema.
                await EnsureSaleItemRepairIdColumnAsync();
                await EnsureSaleItemStatusColumnAsync();

                // Ensure IX_Stylings_Name has a WHERE filter so empty-name rows
                // don't cause duplicate key errors during seeding.
                await EnsureStylingIndexFilterAsync();
                // Ensure Repairs.AmountPaid column exists — added after initial schema.
                await EnsureAmountPaidColumnAsync();
                // Ensure InventoryItems.CategoryId column exists — added after initial schema.
                await EnsureCategoryIdColumnAsync();
                await EnsureInventoryBatchTablesAsync();
                await EnsureInventoryVariantColumnsAndChoicesAsync();
                await EnsureIPhone18ModelsAndInventoryAsync();
                await EnsureMacBookInventoryAsync();
                await EnsureGameConsoleServicesAsync();
                await EnsurePerformanceIndexesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database: {Message}", ex.Message);
                throw;
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private async Task EnsureSalesAndDeviceAuditTablesAsync()
        {
            const string sql = @"
IF COL_LENGTH('Devices','LifecycleStatus') IS NULL
    ALTER TABLE [Devices] ADD [LifecycleStatus] nvarchar(40) NOT NULL CONSTRAINT [DF_Devices_LifecycleStatus] DEFAULT N'Available';
IF COL_LENGTH('Payments','Type') IS NULL
    ALTER TABLE [Payments] ADD [Type] nvarchar(20) NOT NULL CONSTRAINT [DF_Payments_Type] DEFAULT N'Payment';
IF OBJECT_ID(N'[SaleActivities]', N'U') IS NULL
BEGIN
 CREATE TABLE [SaleActivities]([Id] int IDENTITY NOT NULL PRIMARY KEY,[SaleId] int NOT NULL,[Action] nvarchar(50) NOT NULL,[Description] nvarchar(500) NOT NULL,[OldValues] nvarchar(max) NULL,[NewValues] nvarchar(max) NULL,[PerformedByUserId] int NULL,[PerformedAt] datetime2 NOT NULL,CONSTRAINT [FK_SaleActivities_Sales] FOREIGN KEY([SaleId]) REFERENCES [Sales]([Id]) ON DELETE CASCADE,CONSTRAINT [FK_SaleActivities_Users] FOREIGN KEY([PerformedByUserId]) REFERENCES [Users]([Id]));
 CREATE INDEX [IX_SaleActivities_SaleId_PerformedAt] ON [SaleActivities]([SaleId],[PerformedAt]);
END;
IF OBJECT_ID(N'[DeviceActivities]', N'U') IS NULL
BEGIN
 CREATE TABLE [DeviceActivities]([Id] int IDENTITY NOT NULL PRIMARY KEY,[DeviceId] int NOT NULL,[Action] nvarchar(50) NOT NULL,[Description] nvarchar(500) NOT NULL,[OldValues] nvarchar(max) NULL,[NewValues] nvarchar(max) NULL,[PerformedByUserId] int NULL,[PerformedAt] datetime2 NOT NULL,CONSTRAINT [FK_DeviceActivities_Devices] FOREIGN KEY([DeviceId]) REFERENCES [Devices]([Id]) ON DELETE CASCADE,CONSTRAINT [FK_DeviceActivities_Users] FOREIGN KEY([PerformedByUserId]) REFERENCES [Users]([Id]));
 CREATE INDEX [IX_DeviceActivities_DeviceId_PerformedAt] ON [DeviceActivities]([DeviceId],[PerformedAt]);
END;
IF OBJECT_ID(N'[SaleReturns]', N'U') IS NULL
BEGIN
 CREATE TABLE [SaleReturns]([Id] int IDENTITY NOT NULL PRIMARY KEY,[SaleId] int NOT NULL,[ReturnNumber] nvarchar(30) NOT NULL,[RefundMethod] nvarchar(50) NOT NULL,[RefundAmount] decimal(18,2) NOT NULL,[Reason] nvarchar(500) NOT NULL,[Notes] nvarchar(1000) NOT NULL,[ProcessedByUserId] int NOT NULL,[ProcessedAt] datetime2 NOT NULL,CONSTRAINT [FK_SaleReturns_Sales] FOREIGN KEY([SaleId]) REFERENCES [Sales]([Id]),CONSTRAINT [FK_SaleReturns_Users] FOREIGN KEY([ProcessedByUserId]) REFERENCES [Users]([Id]));
 CREATE INDEX [IX_SaleReturns_SaleId] ON [SaleReturns]([SaleId]);
END;
IF OBJECT_ID(N'[SaleReturnItems]', N'U') IS NULL
BEGIN
 CREATE TABLE [SaleReturnItems]([Id] int IDENTITY NOT NULL PRIMARY KEY,[SaleReturnId] int NOT NULL,[SaleItemId] int NOT NULL,[Quantity] int NOT NULL,[UnitRefundAmount] decimal(18,2) NOT NULL,[TotalRefundAmount] decimal(18,2) NOT NULL,[Disposition] nvarchar(50) NOT NULL,CONSTRAINT [FK_SaleReturnItems_Returns] FOREIGN KEY([SaleReturnId]) REFERENCES [SaleReturns]([Id]) ON DELETE CASCADE,CONSTRAINT [FK_SaleReturnItems_Items] FOREIGN KEY([SaleItemId]) REFERENCES [SaleItems]([Id]));
END;";
            await _context.Database.ExecuteSqlRawAsync(sql);
        }

        private async Task EnsureMacBookModelsAsync()
        {
            var models = new (string Name, string Number, int Year)[]
            {
                ("MacBook Air 15-inch (M4, 2025) — A3241", "A3241", 2025),
                ("MacBook Air 13-inch (M4, 2025) — A3240", "A3240", 2025),
                ("MacBook Pro 16-inch (M4, 2024) — A3185", "A3185", 2024),
                ("MacBook Pro 14-inch (M4, 2024) — A3401", "A3401", 2024),
                ("MacBook Pro 14-inch (M4, 2024) — A3112", "A3112", 2024),
                ("MacBook Air 15-inch (M3, 2024) — A3114", "A3114", 2024),
                ("MacBook Air 13-inch (M3, 2024) — A3113", "A3113", 2024),
                ("MacBook Pro 16-inch (M3, 2023) — A2991", "A2991", 2023),
                ("MacBook Pro 14-inch (M3, 2023) — A2992", "A2992", 2023),
                ("MacBook Pro 14-inch (M3, 2023) — A2918", "A2918", 2023),
                ("MacBook Pro 16-inch (M2, 2023) — A2780", "A2780", 2023),
                ("MacBook Pro 14-inch (M2, 2023) — A2779", "A2779", 2023),
                ("MacBook Air 15-inch (M2, 2023) — A2941", "A2941", 2023),
                ("MacBook Air 13-inch (M2, 2022) — A2681", "A2681", 2022),
                ("MacBook Pro 16-inch (2021) — A2485", "A2485", 2021),
                ("MacBook Pro 14-inch (2021) — A2442", "A2442", 2021),
                ("MacBook Pro 13-inch (M1/M2, 2020–2022) — A2338", "A2338", 2020),
                ("MacBook Air 13-inch (M1, 2020) — A2337", "A2337", 2020),
                ("MacBook Pro 13-inch (2020) — A2251", "A2251", 2020),
                ("MacBook Pro 13-inch (2020) — A2289", "A2289", 2020),
                ("MacBook Air 13-inch (2020) — A2179", "A2179", 2020),
                ("MacBook Pro 16-inch (2019) — A2141", "A2141", 2019),
                ("MacBook Pro 13-inch (2019) — A2159", "A2159", 2019),
                ("MacBook Pro 15-inch (2018–2019) — A1990", "A1990", 2018),
                ("MacBook Pro 13-inch (2018–2019) — A1989", "A1989", 2018),
                ("MacBook Air 13-inch (2018–2019) — A1932", "A1932", 2018)
            };

            var shopIds = await _context.Shops.Select(shop => shop.Id).ToListAsync();
            foreach (var shopId in shopIds)
            {
                var appleId = await _context.Brands
                    .Where(brand => brand.ShopId == shopId && brand.Name == "Apple")
                    .Select(brand => (int?)brand.Id)
                    .FirstOrDefaultAsync();
                var laptopId = await _context.DeviceCategories
                    .Where(category => category.ShopId == shopId &&
                        (category.Name == "Laptop" || category.Name == "Computer" || category.Name == "MacBook"))
                    .Select(category => (int?)category.Id)
                    .FirstOrDefaultAsync();
                if (!appleId.HasValue || !laptopId.HasValue) continue;

                var existingNumbers = (await _context.DeviceModels
                    .Where(model => model.ShopId == shopId && model.BrandId == appleId.Value)
                    .Select(model => model.ModelNumber)
                    .ToListAsync())
                    .Where(number => !string.IsNullOrWhiteSpace(number))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var order = 1;
                foreach (var entry in models)
                {
                    if (existingNumbers.Contains(entry.Number)) { order++; continue; }
                    _context.DeviceModels.Add(new DeviceModel
                    {
                        ShopId = shopId,
                        BrandId = appleId.Value,
                        DeviceCategoryId = laptopId.Value,
                        Name = entry.Name,
                        ModelNumber = entry.Number,
                        Year = entry.Year,
                        Description = "Apple MacBook service model",
                        DisplayOrder = order++,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
            }
        }

        private async Task EnsureGameConsoleServicesAsync()
        {
            var consoleDefinitions = new[]
            {
                new { Brand = "Sony", Icon = "sports_esports", Color = "#006FCD", Model = "PlayStation 5 Disc Edition", Number = "PS5-DISC", Services = new[] { "HDMI Port Repair", "Liquid Metal / Overheating Service" } },
                new { Brand = "Sony", Icon = "sports_esports", Color = "#006FCD", Model = "PlayStation 5 Digital Edition", Number = "PS5-DIGITAL", Services = new[] { "HDMI Port Repair", "Liquid Metal / Overheating Service" } },
                new { Brand = "Sony", Icon = "sports_esports", Color = "#006FCD", Model = "PlayStation 5 Slim Disc Edition", Number = "PS5-SLIM-DISC", Services = new[] { "HDMI Port Repair", "Liquid Metal / Overheating Service" } },
                new { Brand = "Sony", Icon = "sports_esports", Color = "#006FCD", Model = "PlayStation 5 Slim Digital Edition", Number = "PS5-SLIM-DIGITAL", Services = new[] { "HDMI Port Repair", "Liquid Metal / Overheating Service" } },
                new { Brand = "Xbox", Icon = "sports_esports", Color = "#107C10", Model = "Xbox Series X", Number = "XBOX-SERIES-X", Services = new[] { "HDMI Port Repair", "Overheating / Thermal Service" } },
                new { Brand = "Xbox", Icon = "sports_esports", Color = "#107C10", Model = "Xbox Series S", Number = "XBOX-SERIES-S", Services = new[] { "HDMI Port Repair", "Overheating / Thermal Service" } }
            };

            var shopIds = await _context.Shops.AsNoTracking().Where(shop => !shop.IsDeleted).Select(shop => shop.Id).ToListAsync();
            foreach (var shopId in shopIds)
            {
                var gaming = await _context.DeviceCategories.FirstOrDefaultAsync(category => category.ShopId == shopId && category.Name == "Gaming" && !category.IsDeleted);
                if (gaming == null)
                {
                    gaming = new DeviceCategory
                    {
                        ShopId = shopId, Name = "Gaming", Description = "Game consoles", IsActive = true,
                        DisplayOrder = 5, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow
                    };
                    _context.DeviceCategories.Add(gaming);
                    await _context.SaveChangesAsync();
                }

                var repairPartsId = await _context.InventoryCategories
                    .Where(category => category.ShopId == shopId && category.Name == "Repair Parts" && !category.IsDeleted)
                    .Select(category => (int?)category.Id).FirstOrDefaultAsync();
                if (!repairPartsId.HasValue) continue;

                foreach (var brandDefinition in consoleDefinitions.GroupBy(definition => definition.Brand).Select(group => group.First()))
                {
                    var brand = await _context.Brands.FirstOrDefaultAsync(entry => entry.ShopId == shopId && entry.Name == brandDefinition.Brand && !entry.IsDeleted);
                    if (brand == null)
                    {
                        var styling = await _context.Stylings.FirstOrDefaultAsync(entry => entry.Name == brandDefinition.Brand);
                        if (styling == null)
                        {
                            styling = new Styling { Name = brandDefinition.Brand, Icon = brandDefinition.Icon, Color = brandDefinition.Color, CreatedAt = DateTime.UtcNow };
                            _context.Stylings.Add(styling);
                            await _context.SaveChangesAsync();
                        }
                        brand = new Brand
                        {
                            ShopId = shopId, Name = brandDefinition.Brand, Description = brandDefinition.Brand + " game consoles",
                            IsActive = true, DisplayOrder = brandDefinition.Brand == "Sony" ? 7 : 8, StylingId = styling.Id,
                            CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow
                        };
                        _context.Brands.Add(brand);
                        await _context.SaveChangesAsync();
                    }

                    foreach (var definition in consoleDefinitions.Where(entry => entry.Brand == brandDefinition.Brand))
                    {
                        var model = await _context.DeviceModels.FirstOrDefaultAsync(entry => entry.ShopId == shopId && entry.BrandId == brand.Id && entry.ModelNumber == definition.Number && !entry.IsDeleted);
                        if (model == null)
                        {
                            model = new DeviceModel
                            {
                                ShopId = shopId, BrandId = brand.Id, DeviceCategoryId = gaming.Id,
                                Name = definition.Model, ModelNumber = definition.Number, Description = definition.Brand + " game console",
                                IsActive = true, DisplayOrder = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow
                            };
                            _context.DeviceModels.Add(model);
                            await _context.SaveChangesAsync();
                        }

                        foreach (var serviceName in definition.Services)
                        {
                            var category = await _context.Categories.FirstOrDefaultAsync(entry => entry.ShopId == shopId && entry.Name == serviceName && entry.CategoryType == "Inventory" && !entry.IsDeleted);
                            if (category == null)
                            {
                                category = new Category
                                {
                                    ShopId = shopId, Name = serviceName, Description = serviceName,
                                    CategoryType = "Inventory", IsActive = true, DisplayOrder = 90,
                                    CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow
                                };
                                _context.Categories.Add(category);
                                await _context.SaveChangesAsync();
                            }

                            var exists = await _context.InventoryItems.AnyAsync(item => item.ShopId == shopId && item.DeviceModelId == model.Id && item.Name == serviceName && !item.IsDeleted);
                            if (exists) continue;
                            var skuStem = serviceName.StartsWith("HDMI", StringComparison.OrdinalIgnoreCase) ? "HDMI" : "THERMAL";
                            _context.InventoryItems.Add(new InventoryItem
                            {
                                ShopId = shopId, BrandId = brand.Id, DeviceCategoryId = gaming.Id, DeviceModelId = model.Id,
                                InventoryCategoryId = repairPartsId.Value, CategoryId = category.Id,
                                Name = serviceName, Description = serviceName, SKU = $"SVC-{brand.Id}-{model.Id}-{skuStem}",
                                CurrentStock = 0, ReorderPoint = 0, CostPrice = 0, RetailPrice = 0, WholesalePrice = 0,
                                EnableLowStockNotifications = false, IsActive = true,
                                Notes = "SERVICE_ONLY: selectable repair service; does not deduct a stocked part",
                                CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow
                            });
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        private async Task EnsurePerformanceIndexesAsync()
        {
            const string sql = @"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryItems_Shop_Model_Active' AND object_id = OBJECT_ID('InventoryItems'))
                    CREATE INDEX [IX_InventoryItems_Shop_Model_Active] ON [InventoryItems] ([ShopId],[DeviceModelId],[IsDeleted],[IsActive]) INCLUDE ([BrandId],[DeviceCategoryId],[InventoryCategoryId],[CurrentStock],[ReorderPoint]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_InventoryItems_Shop_SKU' AND object_id = OBJECT_ID('InventoryItems'))
                    CREATE INDEX [IX_InventoryItems_Shop_SKU] ON [InventoryItems] ([ShopId],[SKU]) WHERE [IsDeleted] = 0;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Repairs_Shop_Status_CreatedDate' AND object_id = OBJECT_ID('Repairs'))
                    CREATE INDEX [IX_Repairs_Shop_Status_CreatedDate] ON [Repairs] ([ShopId],[Status],[CreatedDate] DESC) WHERE [IsDeleted] = 0;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_Shop_Phone' AND object_id = OBJECT_ID('Customers'))
                    CREATE INDEX [IX_Customers_Shop_Phone] ON [Customers] ([ShopId],[Phone]) WHERE [IsDeleted] = 0;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DeviceModels_Shop_Brand_Category_Active' AND object_id = OBJECT_ID('DeviceModels'))
                    CREATE INDEX [IX_DeviceModels_Shop_Brand_Category_Active] ON [DeviceModels] ([ShopId],[BrandId],[DeviceCategoryId],[IsActive]) WHERE [IsDeleted] = 0;";
            await _context.Database.ExecuteSqlRawAsync(sql);
        }

        private async Task EnsureIPhone18ModelsAndInventoryAsync()
        {
            var phoneModels = new[]
            {
                new { Name = "iPhone 18 Pro Max", Year = 2026, Order = 1, BackGlassColors = new[] { "Black", "Silver", "Glacier", "Burgundy" } },
                new { Name = "iPhone 18 Pro", Year = 2026, Order = 2, BackGlassColors = new[] { "Black", "Silver", "Glacier", "Burgundy" } },
                new { Name = "iPhone 17e", Year = 2026, Order = 3, BackGlassColors = Array.Empty<string>() },
                new { Name = "iPhone Air", Year = 2025, Order = 4, BackGlassColors = new[] { "Space Black", "Cloud White", "Light Gold", "Sky Blue" } },
                new { Name = "iPhone 16e", Year = 2025, Order = 5, BackGlassColors = new[] { "Black", "White" } }
            };
            var screenQualities = new[] { "Incell", "Soft OLED", "Hard OLED", "OEM", "Original/Refurbished" };
            var simpleParts = new[] { "Battery", "Charging Port", "Camera", "Front Camera" };

            var shopIds = await _context.Shops.Where(shop => !shop.IsDeleted).Select(shop => shop.Id).ToListAsync();
            foreach (var shopId in shopIds)
            {
                var appleId = await _context.Brands.Where(brand => brand.ShopId == shopId && brand.Name == "Apple").Select(brand => (int?)brand.Id).FirstOrDefaultAsync();
                var phoneCategoryId = await _context.DeviceCategories
                    .Where(category => category.ShopId == shopId && (category.Name == "Smartphone" || category.Name == "Phone" || category.Name == "iPhone"))
                    .Select(category => (int?)category.Id).FirstOrDefaultAsync();
                var repairPartsId = await _context.InventoryCategories.Where(category => category.ShopId == shopId && category.Name == "Repair Parts")
                    .Select(category => (int?)category.Id).FirstOrDefaultAsync();
                if (!appleId.HasValue || !phoneCategoryId.HasValue || !repairPartsId.HasValue) continue;

                var partCategoryIds = await _context.Categories
                    .Where(category => category.ShopId == shopId && category.CategoryType == "Inventory" && !category.IsDeleted)
                    .ToDictionaryAsync(category => category.Name, category => category.Id, StringComparer.OrdinalIgnoreCase);

                // Remove the nonexistent iPhone 17 Plus catalog entry completely.
                // If an installation somehow has a real transaction tied to it, fail
                // closed and only hide it rather than destroying repair/sale history.
                var retired17Plus = await _context.DeviceModels
                    .FirstOrDefaultAsync(model => model.ShopId == shopId && model.BrandId == appleId.Value &&
                        model.Name == "iPhone 17 Plus" && !model.IsDeleted);
                if (retired17Plus != null)
                {
                    var legacyItems = await _context.InventoryItems
                        .Where(item => item.ShopId == shopId && item.DeviceModelId == retired17Plus.Id)
                        .ToListAsync();
                    var legacyItemIds = legacyItems.Select(item => item.Id).ToList();
                    var hasHistory = await _context.Devices.AnyAsync(device => device.DeviceModelId == retired17Plus.Id) ||
                        await _context.Repairs.AnyAsync(repair => repair.DeviceModelId == retired17Plus.Id) ||
                        legacyItems.Any(item => item.CurrentStock != 0) ||
                        (legacyItemIds.Count > 0 && (await _context.RepairParts.AnyAsync(part => legacyItemIds.Contains(part.InventoryItemId)) ||
                            await _context.SaleItems.AnyAsync(item => item.InventoryItemId.HasValue && legacyItemIds.Contains(item.InventoryItemId.Value))));

                    if (!hasHistory)
                    {
                        if (legacyItemIds.Count > 0)
                        {
                            _context.InventoryBatches.RemoveRange(await _context.InventoryBatches.Where(batch => legacyItemIds.Contains(batch.InventoryItemId)).ToListAsync());
                            _context.InventoryTransactions.RemoveRange(await _context.InventoryTransactions.Where(transaction => legacyItemIds.Contains(transaction.InventoryItemId)).ToListAsync());
                            _context.StockMovements.RemoveRange(await _context.StockMovements.Where(movement => legacyItemIds.Contains(movement.InventoryItemId)).ToListAsync());
                            _context.InventoryItems.RemoveRange(legacyItems);
                        }
                        _context.DeviceModels.Remove(retired17Plus);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        retired17Plus.IsActive = false;
                        retired17Plus.IsDeleted = true;
                        retired17Plus.ModifiedDate = DateTime.UtcNow;
                        foreach (var item in legacyItems.Where(item => item.CurrentStock == 0))
                        {
                            item.IsActive = false;
                            item.IsDeleted = true;
                            item.UpdatedDate = DateTime.UtcNow;
                            item.ModifiedDate = DateTime.UtcNow;
                        }
                    }
                }

                foreach (var modelDefinition in phoneModels)
                {
                    var modelName = modelDefinition.Name;
                    var model = await _context.DeviceModels.FirstOrDefaultAsync(entry => entry.ShopId == shopId && entry.BrandId == appleId.Value && entry.Name == modelName);
                    if (model == null)
                    {
                        model = new DeviceModel
                        {
                            ShopId = shopId,
                            BrandId = appleId.Value,
                            DeviceCategoryId = phoneCategoryId.Value,
                            Name = modelName,
                            ModelNumber = string.Empty,
                            Year = modelDefinition.Year,
                            Description = "Apple iPhone service model",
                            DisplayOrder = modelDefinition.Order,
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow
                        };
                        _context.DeviceModels.Add(model);
                        await _context.SaveChangesAsync();
                    }

                    var existing = (await _context.InventoryItems
                        .Where(item => item.ShopId == shopId && item.DeviceModelId == model.Id && !item.IsDeleted)
                        .Select(item => new { item.Name, item.Variant, item.Color })
                        .ToListAsync())
                        .Select(item => $"{item.Name}|{item.Variant}|{item.Color}")
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    void AddPart(string partName, string variant = "", string color = "")
                    {
                        if (!partCategoryIds.TryGetValue(partName, out var categoryId)) return;
                        var key = $"{partName}|{variant}|{color}";
                        if (existing.Contains(key)) return;
                        var suffix = string.Join('-', new[] { variant, color }.Where(value => !string.IsNullOrWhiteSpace(value)))
                            .ToUpperInvariant().Replace("/", "-").Replace(" ", "-");
                        _context.InventoryItems.Add(new InventoryItem
                        {
                            ShopId = shopId,
                            BrandId = appleId.Value,
                            DeviceCategoryId = phoneCategoryId.Value,
                            DeviceModelId = model.Id,
                            InventoryCategoryId = repairPartsId.Value,
                            CategoryId = categoryId,
                            Name = partName,
                            Description = $"{modelName} {partName}",
                            SKU = $"{partName.Replace(" ", "-").ToUpperInvariant()}-{model.Id}{(suffix.Length > 0 ? "-" + suffix : string.Empty)}",
                            Variant = variant,
                            Color = color,
                            CurrentStock = 0,
                            ReorderPoint = 0,
                            CostPrice = 0,
                            RetailPrice = 0,
                            WholesalePrice = 0,
                            EnableLowStockNotifications = false,
                            IsActive = true,
                            UpdatedDate = DateTime.UtcNow,
                            CreatedDate = DateTime.UtcNow
                        });
                        existing.Add(key);
                    }

                    foreach (var quality in screenQualities) AddPart("Screen", variant: quality);
                    foreach (var part in simpleParts) AddPart(part);
                    foreach (var color in modelDefinition.BackGlassColors) AddPart("Back Glass", color: color);
                    await _context.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Gives every supported MacBook model one simple Screen, Battery, and
        /// Charging Port inventory record. Phone-only screen qualities never apply
        /// to MacBooks. The method is idempotent and preserves existing stock.
        /// </summary>
        private async Task EnsureMacBookInventoryAsync()
        {
            var allowedParts = new[] { "Screen", "Battery", "Charging Port" };
            var phoneScreenVariants = new[] { "Incell", "Soft OLED", "Hard OLED", "OEM", "Original/Refurbished" };
            var shopIds = await _context.Shops.Where(shop => !shop.IsDeleted).Select(shop => shop.Id).ToListAsync();

            foreach (var shopId in shopIds)
            {
                var appleId = await _context.Brands
                    .Where(brand => brand.ShopId == shopId && brand.Name == "Apple" && !brand.IsDeleted)
                    .Select(brand => (int?)brand.Id)
                    .FirstOrDefaultAsync();
                var laptopId = await _context.DeviceCategories
                    .Where(category => category.ShopId == shopId && !category.IsDeleted &&
                        (category.Name == "Laptop" || category.Name == "Computer" || category.Name == "MacBook"))
                    .Select(category => (int?)category.Id)
                    .FirstOrDefaultAsync();
                var repairPartsId = await _context.InventoryCategories
                    .Where(category => category.ShopId == shopId && category.Name == "Repair Parts" && !category.IsDeleted)
                    .Select(category => (int?)category.Id)
                    .FirstOrDefaultAsync();
                if (!appleId.HasValue || !laptopId.HasValue || !repairPartsId.HasValue) continue;

                var categoryIds = await _context.Categories
                    .Where(category => category.ShopId == shopId && category.CategoryType == "Inventory" &&
                        !category.IsDeleted && allowedParts.Contains(category.Name))
                    .ToDictionaryAsync(category => category.Name, category => category.Id, StringComparer.OrdinalIgnoreCase);
                if (categoryIds.Count != allowedParts.Length) continue;

                var models = await _context.DeviceModels
                    .Where(model => model.ShopId == shopId && model.BrandId == appleId.Value &&
                        model.DeviceCategoryId == laptopId.Value && model.IsActive && !model.IsDeleted &&
                        model.Name.Contains("MacBook"))
                    .ToListAsync();

                foreach (var model in models)
                {
                    var items = await _context.InventoryItems
                        .Where(item => item.ShopId == shopId && item.DeviceModelId == model.Id && !item.IsDeleted)
                        .ToListAsync();

                    // Hide empty, incompatible MacBook parts that were created by the
                    // earlier generic phone-part seeding. Historical rows remain intact.
                    foreach (var incompatible in items.Where(item => !allowedParts.Contains(item.Name, StringComparer.OrdinalIgnoreCase)))
                    {
                        if (incompatible.CurrentStock == 0)
                        {
                            incompatible.IsActive = false;
                            incompatible.IsDeleted = true;
                            incompatible.UpdatedDate = DateTime.UtcNow;
                            incompatible.ModifiedDate = DateTime.UtcNow;
                        }
                    }

                    foreach (var partName in allowedParts)
                    {
                        var matches = items.Where(item => item.Name.Equals(partName, StringComparison.OrdinalIgnoreCase) && !item.IsDeleted).ToList();
                        InventoryItem? primary = matches.FirstOrDefault(item => string.IsNullOrWhiteSpace(item.Variant));

                        if (partName == "Screen" && primary == null)
                        {
                            primary = matches.FirstOrDefault(item => phoneScreenVariants.Contains(item.Variant, StringComparer.OrdinalIgnoreCase));
                            if (primary != null)
                            {
                                primary.Variant = string.Empty;
                                primary.Description = $"{model.Name} Screen Replacement";
                                primary.UpdatedDate = DateTime.UtcNow;
                                primary.ModifiedDate = DateTime.UtcNow;
                            }
                        }

                        if (primary == null)
                        {
                            primary = new InventoryItem
                            {
                                ShopId = shopId,
                                BrandId = appleId.Value,
                                DeviceCategoryId = laptopId.Value,
                                DeviceModelId = model.Id,
                                InventoryCategoryId = repairPartsId.Value,
                                CategoryId = categoryIds[partName],
                                Name = partName,
                                Description = partName == "Screen" ? $"{model.Name} Screen Replacement" :
                                    partName == "Battery" ? $"{model.Name} Battery Replacement" : $"{model.Name} Charging Port",
                                SKU = $"MAC-{model.ModelNumber}-{partName.Replace(" ", "-").ToUpperInvariant()}",
                                Variant = string.Empty,
                                Color = string.Empty,
                                CurrentStock = 0,
                                ReorderPoint = 0,
                                CostPrice = 0,
                                RetailPrice = 0,
                                WholesalePrice = 0,
                                EnableLowStockNotifications = false,
                                IsActive = true,
                                UpdatedDate = DateTime.UtcNow,
                                CreatedDate = DateTime.UtcNow
                            };
                            _context.InventoryItems.Add(primary);
                        }
                        else
                        {
                            primary.IsActive = true;
                            primary.CategoryId = categoryIds[partName];
                            primary.InventoryCategoryId = repairPartsId.Value;
                        }

                        // Remove only empty duplicates. Any row with real stock stays
                        // available so quantities and FIFO history can never be lost.
                        foreach (var duplicate in matches.Where(item => item != primary && item.CurrentStock == 0))
                        {
                            duplicate.IsActive = false;
                            duplicate.IsDeleted = true;
                            duplicate.UpdatedDate = DateTime.UtcNow;
                            duplicate.ModifiedDate = DateTime.UtcNow;
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }
        }

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

                    // Motorola is a selectable empty brand. Models are intentionally
                    // not seeded so each shop can add only the models it carries.
                    bool hasMotorola = await _context.Brands.AnyAsync(b => b.ShopId == sId && b.Name == "Motorola");
                    if (!hasMotorola)
                    {
                        var motorolaStyling = new Styling
                        {
                            Name = "Motorola",
                            Icon = "phone_android",
                            Color = "#E1000F",
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Add(motorolaStyling);
                        await _context.SaveChangesAsync();

                        _context.Brands.Add(new Brand
                        {
                            ShopId = sId,
                            Name = "Motorola",
                            Description = "Motorola devices",
                            IsActive = true,
                            DisplayOrder = 6,
                            StylingId = motorolaStyling.Id,
                            CreatedDate = DateTime.UtcNow
                        });
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Motorola brand ensured for shop {ShopId} without adding models.", sId);
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
        /// Adds durable device color and carrier fields to older installations.
        /// Lock status remains in NetworkStatus and is intentionally separate.
        /// </summary>
        private async Task EnsureDeviceColorAndCarrierColumnsAsync()
        {
            const string colorSql = @"
                IF COL_LENGTH('Devices', 'Color') IS NULL
                    ALTER TABLE [Devices] ADD [Color] nvarchar(50) NOT NULL
                        CONSTRAINT [DF_Devices_Color] DEFAULT N'';";
            const string carrierSql = @"
                IF COL_LENGTH('Devices', 'Carrier') IS NULL
                    ALTER TABLE [Devices] ADD [Carrier] nvarchar(100) NOT NULL
                        CONSTRAINT [DF_Devices_Carrier] DEFAULT N'';";
            await _context.Database.ExecuteSqlRawAsync(colorSql);
            await _context.Database.ExecuteSqlRawAsync(carrierSql);
        }

        /// <summary>
        /// Adds a durable operational status for custom quick-sale items.
        /// Existing sale items are kept as In Stock for backward compatibility.
        /// </summary>
        private async Task EnsureSaleItemStatusColumnAsync()
        {
            const string sql = @"
                IF COL_LENGTH('SaleItems', 'ItemStatus') IS NULL
                BEGIN
                    ALTER TABLE [SaleItems] ADD [ItemStatus] nvarchar(50) NOT NULL
                        CONSTRAINT [DF_SaleItems_ItemStatus] DEFAULT N'In Stock';
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
        /// Stores private cost entered for a custom repair that does not consume
        /// a standard inventory record. Safe to run on every startup.
        /// </summary>
        private async Task EnsureRepairPartCostColumnAsync()
        {
            const string sql = @"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Repairs' AND COLUMN_NAME = 'PartCost'
                )
                BEGIN
                    ALTER TABLE [Repairs] ADD [PartCost] decimal(18,2) NOT NULL DEFAULT 0;
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

        /// <summary>
        /// Adds FIFO batch tracking without changing or deleting existing inventory data.
        /// Existing positive stock is converted to one opening batch at its current cost.
        /// </summary>
        private async Task EnsureInventoryBatchTablesAsync()
        {
            // Keep the column-creation batch separate from statements that use the
            // new columns. SQL Server compiles a batch before executing it, so an
            // UPDATE that references a column added earlier in the same batch can
            // still fail with "Invalid column name" on first startup.
            const string schemaSql = @"
                IF OBJECT_ID('InventoryBatches', 'U') IS NULL
                BEGIN
                    CREATE TABLE [InventoryBatches] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [InventoryItemId] INT NOT NULL,
                        [ReceivedQuantity] INT NOT NULL,
                        [RemainingQuantity] INT NOT NULL,
                        [UnitCost] DECIMAL(18,2) NOT NULL,
                        [Vendor] NVARCHAR(100) NOT NULL DEFAULT '',
                        [ReferenceNumber] NVARCHAR(50) NOT NULL DEFAULT '',
                        [ReceivedDate] DATETIME2 NOT NULL,
                        [Notes] NVARCHAR(500) NOT NULL DEFAULT '',
                        [IsDeleted] BIT NOT NULL DEFAULT 0,
                        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [ModifiedDate] DATETIME2 NULL,
                        CONSTRAINT [FK_InventoryBatches_InventoryItems_InventoryItemId]
                            FOREIGN KEY ([InventoryItemId]) REFERENCES [InventoryItems]([Id])
                    );
                    CREATE INDEX [IX_InventoryBatches_FIFO]
                        ON [InventoryBatches] ([InventoryItemId], [ReceivedDate], [Id]);

                    INSERT INTO [InventoryBatches]
                        ([InventoryItemId],[ReceivedQuantity],[RemainingQuantity],[UnitCost],[Vendor],[ReferenceNumber],[ReceivedDate],[Notes],[IsDeleted],[CreatedDate])
                    SELECT [Id],[CurrentStock],[CurrentStock],[CostPrice],'Opening inventory','OPENING',COALESCE([CreatedDate],GETUTCDATE()),'Automatically preserved when FIFO tracking was enabled',0,GETUTCDATE()
                    FROM [InventoryItems]
                    WHERE [CurrentStock] > 0 AND [IsDeleted] = 0;
                END;

                IF COL_LENGTH('RepairParts', 'ActualUnitCost') IS NULL
                    ALTER TABLE [RepairParts] ADD [ActualUnitCost] DECIMAL(18,2) NOT NULL DEFAULT 0;

                IF COL_LENGTH('RepairParts', 'IsAllocatedFromStock') IS NULL
                    ALTER TABLE [RepairParts] ADD [IsAllocatedFromStock] BIT NOT NULL CONSTRAINT [DF_RepairParts_IsAllocatedFromStock] DEFAULT 1;

                IF COL_LENGTH('RepairParts', 'StockAllocatedDate') IS NULL
                    ALTER TABLE [RepairParts] ADD [StockAllocatedDate] DATETIME2 NULL;";

            await _context.Database.ExecuteSqlRawAsync(schemaSql);

            const string backfillSql = @"
                UPDATE [RepairParts]
                   SET [StockAllocatedDate] = COALESCE([StockAllocatedDate], [CreatedDate])
                 WHERE [IsAllocatedFromStock] = 1 AND [StockAllocatedDate] IS NULL;";

            await _context.Database.ExecuteSqlRawAsync(backfillSql);

            const string allocationTableSql = @"

                IF OBJECT_ID('RepairPartBatchAllocations', 'U') IS NULL
                BEGIN
                    CREATE TABLE [RepairPartBatchAllocations] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [RepairPartId] INT NOT NULL,
                        [InventoryBatchId] INT NOT NULL,
                        [Quantity] INT NOT NULL,
                        [UnitCost] DECIMAL(18,2) NOT NULL,
                        [IsDeleted] BIT NOT NULL DEFAULT 0,
                        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [ModifiedDate] DATETIME2 NULL,
                        CONSTRAINT [FK_RepairPartBatchAllocations_RepairParts_RepairPartId]
                            FOREIGN KEY ([RepairPartId]) REFERENCES [RepairParts]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_RepairPartBatchAllocations_InventoryBatches_InventoryBatchId]
                            FOREIGN KEY ([InventoryBatchId]) REFERENCES [InventoryBatches]([Id])
                    );
                    CREATE INDEX [IX_RepairPartBatchAllocations_RepairPartId] ON [RepairPartBatchAllocations] ([RepairPartId]);
                    CREATE INDEX [IX_RepairPartBatchAllocations_InventoryBatchId] ON [RepairPartBatchAllocations] ([InventoryBatchId]);
                END;";
            await _context.Database.ExecuteSqlRawAsync(allocationTableSql);
        }

        /// <summary>
        /// Adds inventory-only part attributes and removes pre-2016 models from the
        /// active inventory view without deleting their stock or transaction history.
        /// </summary>
        private async Task EnsureInventoryVariantColumnsAndChoicesAsync()
        {
            const string sql = @"
                IF COL_LENGTH('InventoryItems', 'Variant') IS NULL
                    ALTER TABLE [InventoryItems] ADD [Variant] NVARCHAR(50) NOT NULL CONSTRAINT [DF_InventoryItems_Variant] DEFAULT '';

                IF COL_LENGTH('InventoryItems', 'Color') IS NULL
                    ALTER TABLE [InventoryItems] ADD [Color] NVARCHAR(50) NOT NULL CONSTRAINT [DF_InventoryItems_Color] DEFAULT '';

                UPDATE ii
                   SET ii.IsActive = 0,
                       ii.UpdatedDate = GETUTCDATE(),
                       ii.ModifiedDate = GETUTCDATE()
                  FROM [InventoryItems] ii
                  INNER JOIN [DeviceModels] dm ON dm.Id = ii.DeviceModelId
                 WHERE dm.[Year] IS NOT NULL
                   AND dm.[Year] < 2016
                   AND ii.IsActive = 1
                   AND ii.IsDeleted = 0;

                DECLARE @InventoryParts TABLE ([Name] NVARCHAR(50), [Description] NVARCHAR(200), [DisplayOrder] INT);
                INSERT INTO @InventoryParts VALUES
                    ('Screen', 'Display and screen assemblies', 10),
                    ('Battery', 'Replacement batteries', 20),
                    ('Charging Port', 'Charging port assemblies', 30),
                    ('Camera', 'Rear camera assemblies', 40),
                    ('Front Camera', 'Front camera assemblies', 50),
                    ('Back Glass', 'Back glass for compatible phones', 60);

                INSERT INTO [Categories]
                    ([ShopId],[Name],[Description],[CategoryType],[IsActive],[DisplayOrder],[ParentCategoryId],[IsDeleted],[CreatedDate],[ModifiedDate])
                SELECT s.Id, p.[Name], p.[Description], 'Inventory', 1, p.[DisplayOrder], NULL, 0, GETUTCDATE(), NULL
                  FROM [Shops] s
                  CROSS JOIN @InventoryParts p
                 WHERE s.IsDeleted = 0
                   AND NOT EXISTS (
                       SELECT 1 FROM [Categories] c
                        WHERE c.ShopId = s.Id AND c.[Name] = p.[Name]
                          AND c.CategoryType = 'Inventory' AND c.IsDeleted = 0
                   );

                DECLARE @InventoryGroups TABLE ([Name] NVARCHAR(50), [Description] NVARCHAR(200), [DisplayOrder] INT);
                INSERT INTO @InventoryGroups VALUES
                    ('Repair Parts', 'Parts consumed while completing device repairs', 10),
                    ('Accessories', 'Customer accessories and add-on products', 20),
                    ('Devices', 'Phones, tablets, computers, and other devices', 30),
                    ('Tools & Supplies', 'Repair tools and shop consumable supplies', 40);

                INSERT INTO [InventoryCategories]
                    ([ShopId],[Name],[Description],[IsActive],[DisplayOrder],[IsDeleted],[CreatedDate],[ModifiedDate])
                SELECT s.Id, g.[Name], g.[Description], 1, g.[DisplayOrder], 0, GETUTCDATE(), NULL
                  FROM [Shops] s
                  CROSS JOIN @InventoryGroups g
                 WHERE s.IsDeleted = 0
                   AND NOT EXISTS (
                       SELECT 1 FROM [InventoryCategories] ic
                        WHERE ic.ShopId = s.Id AND ic.[Name] = g.[Name] AND ic.IsDeleted = 0
                   );";
            await _context.Database.ExecuteSqlRawAsync(sql);
        }
    }
}
