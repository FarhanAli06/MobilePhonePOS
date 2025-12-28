using Microsoft.AspNetCore.Mvc;
using Empire.Application.Interfaces;
using Empire.Application.DTOs.Sale;
using Empire.Application.DTOs.Inventory;
using Empire.Application.DTOs.Device;
using Empire.Application.DTOs.Repair;
using Empire.Infrastructure.Data;
using Empire.Domain.Entities;
using Empire.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Empire.Web.Authorization;

namespace Empire.Web.Controllers
{
    [SessionAuthorize]
    public class POSController : Controller
    {
        private readonly EmpireDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly IDeviceService _deviceService;
        private readonly IRepairService _repairService;
        private readonly ICustomerService _customerService;
        private readonly ILogger<POSController> _logger;

        public POSController(
            EmpireDbContext context,
            IInventoryService inventoryService,
            IDeviceService deviceService,
            IRepairService repairService,
            ICustomerService customerService,
            ILogger<POSController> logger)
        {
            _context = context;
            _inventoryService = inventoryService;
            _deviceService = deviceService;
            _repairService = repairService;
            _customerService = customerService;
            _logger = logger;
        }

        private int GetCurrentShopId()
        {
            var shopIdString = HttpContext.Session.GetString("CurrentShopId");
            return int.TryParse(shopIdString, out int shopId) ? shopId : 0;
        }

        private int GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdString, out int userId) ? userId : 0;
        }

        public IActionResult Index()
        {
            ViewBag.CurrentShopId = GetCurrentShopId();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchItems(string query = "", string type = "", int? customerId = null)
        {
            try
            {
                var shopId = GetCurrentShopId();
                if (shopId == 0)
                {
                    _logger.LogWarning("SearchItems called with invalid shop session");
                    return Json(new { success = false, message = "Invalid shop session. Please refresh the page and try again." });
                }
                
                var results = new List<POSSearchResult>();

                // Search Devices
                if (string.IsNullOrEmpty(type) || type == "device")
                {
                    var devices = await _context.Devices
                        .Include(d => d.Brand)
                        .Include(d => d.DeviceCategory)
                        .Include(d => d.DeviceModel)
                        .Where(d => d.ShopId == shopId && 
                                   d.IsAvailableForSale && 
                                   !d.IsSold &&
                                   (string.IsNullOrEmpty(query) ||
                                    d.Brand.Name.Contains(query) ||
                                    d.DeviceModel.Name.Contains(query) ||
                                    d.IMEISerialNumber.Contains(query)))
                        .Take(50)
                        .ToListAsync();

                    foreach (var device in devices)
                    {
                        results.Add(new POSSearchResult
                        {
                            Id = $"dev_{device.Id}",
                            Type = "Device",
                            Name = $"{device.Brand?.Name} {device.DeviceModel?.Name}",
                            Description = $"{device.DeviceCategory?.Name} - {device.NetworkStatus}",
                            Brand = device.Brand?.Name ?? "",
                            Category = device.DeviceCategory?.Name ?? "",
                            SKU = device.IMEISerialNumber ?? "",
                            Price = device.SellingPrice ?? 0,
                            CostPrice = device.BuyingPrice,
                            Stock = 1,
                            IMEI = device.IMEISerialNumber,
                            NetworkStatus = device.NetworkStatus,
                            IsAvailable = true
                        });
                    }
                }

                // Search Inventory - Check both tables
                if (string.IsNullOrEmpty(type) || type == "inventory")
                {
                    // Try InventoryItem first (new structure)
                    var inventoryItems = await _context.InventoryItems
                        .Include(i => i.Brand)
                        .Include(i => i.InventoryCategory)
                        .Where(i => i.ShopId == shopId && 
                                   i.CurrentStock > 0 &&
                                   i.IsActive &&
                                   (string.IsNullOrEmpty(query) ||
                                    i.Name.Contains(query) ||
                                    i.Description.Contains(query) ||
                                    i.SKU.Contains(query)))
                        .Take(50)
                        .ToListAsync();

                    foreach (var item in inventoryItems)
                    {
                        results.Add(new POSSearchResult
                        {
                            Id = $"inv_{item.Id}",
                            Type = "Inventory",
                            Name = item.Name,
                            Description = item.Description,
                            Brand = item.Brand?.Name ?? "Generic",
                            Category = item.InventoryCategory?.Name ?? "",
                            SKU = item.SKU,
                            Price = item.RetailPrice,
                            CostPrice = item.CostPrice,
                            Stock = item.CurrentStock,
                            IsAvailable = item.CurrentStock > 0
                        });
                    }
                    
                    // Fallback to old Inventory table if no items found
                    if (inventoryItems.Count == 0)
                    {
                        var inventory = await _context.Inventories
                            .Where(i => i.ShopId == shopId && 
                                       i.Stock > 0 &&
                                       (string.IsNullOrEmpty(query) ||
                                        i.Name.Contains(query) ||
                                        i.Category.Contains(query)))
                            .Take(50)
                            .ToListAsync();

                        foreach (var item in inventory)
                        {
                            results.Add(new POSSearchResult
                            {
                                Id = $"inv_{item.Id}",
                                Type = "Inventory",
                                Name = item.Name,
                                Description = item.Description ?? item.Category,
                                Brand = "Generic",
                                Category = item.Category,
                                SKU = $"INV-{item.Id}",
                                Price = item.RetailPrice,
                                CostPrice = item.CostPrice,
                                Stock = item.Stock,
                                IsAvailable = item.Stock > 0
                            });
                        }
                    }
                }

                // Search Repairs - UPDATED LOGIC
                // Show ALL repairs for selected customer, not just "Complete" status
                if (string.IsNullOrEmpty(type) || type == "repair")
                {
                    var repairQuery = _context.Repairs
                        .Include(r => r.Customer)
                        .Include(r => r.Brand)
                        .Include(r => r.DeviceModel)
                        .Where(r => r.ShopId == shopId);

                    // If customer is selected, show ALL their repairs (not just Complete)
                    if (customerId.HasValue && customerId.Value > 0)
                    {
                        repairQuery = repairQuery.Where(r => r.CustomerId == customerId.Value);
                    }
                    else
                    {
                        // If no customer selected, show only Complete and Unpaid repairs
                        repairQuery = repairQuery.Where(r => r.Status == "Complete" && r.PaymentStatus != "Paid");
                    }

                    // Apply search filter
                    if (!string.IsNullOrEmpty(query))
                    {
                        repairQuery = repairQuery.Where(r =>
                            r.RepairNumber.Contains(query) ||
                            r.Customer.FirstName.Contains(query) ||
                            r.Customer.LastName.Contains(query));
                    }

                    var repairs = await repairQuery.Take(50).ToListAsync();

                    foreach (var repair in repairs)
                    {
                        results.Add(new POSSearchResult
                        {
                            Id = $"rep_{repair.Id}",
                            Type = "Repair",
                            Name = $"Repair #{repair.RepairNumber}",
                            Description = $"{repair.Brand?.Name} {repair.DeviceModel?.Name} - Status: {repair.Status}",
                            Brand = repair.Brand?.Name ?? "",
                            Category = "Repair Service",
                            SKU = repair.RepairNumber,
                            Price = repair.Cost,
                            CostPrice = null,
                            Stock = 1,
                            RepairNumber = repair.RepairNumber,
                            RepairId = repair.Id,
                            CustomerName = $"{repair.Customer?.FirstName} {repair.Customer?.LastName}",
                            IsAvailable = true
                        });
                    }
                }

                return Json(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching POS items");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers(string search = "")
        {
            try
            {
                var shopId = GetCurrentShopId();
                var customers = await _context.Customers
                    .Where(c => c.ShopId == shopId &&
                               (string.IsNullOrEmpty(search) ||
                                c.FirstName.Contains(search) ||
                                c.LastName.Contains(search) ||
                                c.Phone.Contains(search) ||
                                c.Email.Contains(search)))
                    .Select(c => new
                    {
                        id = c.Id,
                        name = $"{c.FirstName} {c.LastName}",
                        phone = c.Phone,
                        email = c.Email,
                        text = $"{c.FirstName} {c.LastName} - {c.Phone}"
                    })
                    .Take(50)
                    .ToListAsync();

                // Return in Select2 format
                return Json(new { results = customers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // NEW METHOD: Get customer repairs
        [HttpGet]
        public async Task<IActionResult> GetCustomerRepairs(int customerId)
        {
            try
            {
                var shopId = GetCurrentShopId();
                var repairs = await _context.Repairs
                    .Include(r => r.Brand)
                    .Include(r => r.DeviceModel)
                    .Include(r => r.Customer)
                    .Where(r => r.ShopId == shopId && 
                               r.CustomerId == customerId && 
                               r.PaymentStatus != "Paid")  // Only show unpaid and partial repairs
                    .OrderByDescending(r => r.CreatedDate)
                    .ToListAsync();

                var results = repairs.Select(repair => new POSSearchResult
                {
                    Id = $"rep_{repair.Id}",
                    Type = "Repair",
                    Name = $"Repair #{repair.RepairNumber}",
                    Description = $"{repair.Brand?.Name} {repair.DeviceModel?.Name} - Status: {repair.Status}",
                    Brand = repair.Brand?.Name ?? "",
                    Category = "Repair Service",
                    SKU = repair.RepairNumber,
                    Price = repair.Cost,
                    CostPrice = null,
                    Stock = 1,
                    RepairNumber = repair.RepairNumber,
                    RepairId = repair.Id,
                    CustomerName = $"{repair.Customer?.FirstName} {repair.Customer?.LastName}",
                    IsAvailable = true
                }).ToList();

                return Json(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer repairs");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CompleteSale([FromBody] CreateSaleRequest request)
        {
            try
            {
                // Use execution strategy to handle transactions with retry logic
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                var shopId = GetCurrentShopId();
                var userId = GetCurrentUserId();

                // Generate invoice number
                var invoiceNumber = await GenerateInvoiceNumber(shopId);

                // Determine payment status from request
                var paymentStatus = request.PaymentStatus ?? "Unpaid";

                // Create sale
                var sale = new Sale
                {
                    ShopId = shopId,
                    CustomerId = request.CustomerId,
                    SaleNumber = invoiceNumber,
                    SubTotal = request.SubTotal,
                    TaxAmount = request.TaxAmount,
                    DiscountAmount = request.DiscountAmount,
                    TotalAmount = request.TotalAmount,
                    PaymentStatus = paymentStatus,
                    Notes = request.Notes,
                    SaleDate = DateTime.UtcNow,
                    CreatedByUserId = userId,
                    CreatedDateUtc = DateTime.UtcNow
                };

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                // Track repair items for status update
                var repairItems = new List<int>();

                // Process each item
                foreach (var item in request.Items)
                {
                    // Check for custom item flag
                    if (item.ItemType == "Custom")
                    {
                        item.IsCustomItem = true;
                        item.InventoryItemId = null;
                    }
                    
                    // Track repair items
                    if (item.ItemType == "Repair" && item.RepairId.HasValue)
                    {
                        repairItems.Add(item.RepairId.Value);
                    }

                    // Pre-load entities to get cost price and avoid duplicate queries
                    decimal costPrice = 0;
                    Device? deviceEntity = null;
                    InventoryItem? inventoryEntity = null;
                    
                    if (item.ItemType == "Device" && item.ItemReferenceId.HasValue)
                    {
                        deviceEntity = await _context.Devices.FindAsync(item.ItemReferenceId.Value);
                        if (deviceEntity != null)
                        {
                            costPrice = deviceEntity.BuyingPrice ?? 0; // Use buying price as cost for devices
                        }
                    }
                    else if (item.ItemType == "Inventory" && item.ItemReferenceId.HasValue)
                    {
                        inventoryEntity = await _context.InventoryItems.FindAsync(item.ItemReferenceId.Value);
                        if (inventoryEntity != null)
                        {
                            costPrice = inventoryEntity.CostPrice;
                        }
                    }
                    // For custom items and repairs, cost remains 0 unless specified

                    var saleItem = new SaleItem
                    {
                        SaleId = sale.Id,
                        InventoryItemId = item.InventoryItemId,
                        ItemName = item.ItemName,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice,
                        DiscountAmount = item.DiscountAmount,
                        CostPrice = costPrice,
                        Note = item.Note,
                        IsCustomItem = item.IsCustomItem,
                        IsTaxable = item.IsTaxable,
                        CreatedDateUtc = DateTime.UtcNow
                    };

                    _context.SaleItems.Add(saleItem);

                    // Update item status based on type (reuse pre-loaded entities)
                    if (item.ItemType == "Device" && deviceEntity != null)
                    {
                        deviceEntity.IsSold = true;
                        deviceEntity.SoldDate = DateTime.UtcNow;
                        deviceEntity.SoldToCustomerId = request.CustomerId;
                        deviceEntity.ModifiedDate = DateTime.UtcNow;
                    }
                    else if (item.ItemType == "Inventory" && inventoryEntity != null)
                    {
                        // Use pre-loaded inventory entity
                        var previousStock = inventoryEntity.CurrentStock;
                        inventoryEntity.CurrentStock -= item.Quantity;
                        inventoryEntity.UpdatedDate = DateTime.UtcNow;

                        // Create stock movement record
                        var stockMovement = new StockMovement
                        {
                            InventoryItemId = inventoryEntity.Id,
                            CreatedByUserId = userId,
                            MovementType = "OUT",
                            Quantity = -item.Quantity,
                            PreviousStock = previousStock,
                            NewStock = inventoryEntity.CurrentStock,
                            Reason = "Sale",
                            ReferenceNumber = invoiceNumber,
                            UnitCost = inventoryEntity.CostPrice,
                            TotalCost = inventoryEntity.CostPrice * item.Quantity,
                            MovementDate = DateTime.UtcNow,
                            Notes = $"Sold via Invoice #{invoiceNumber}",
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.StockMovements.Add(stockMovement);
                    }
                }

                // Process Payments
                decimal totalPaid = 0;
                foreach (var payment in request.Payments)
                {
                    var newPayment = new Payment
                    {
                        SaleId = sale.Id,
                        Amount = payment.Amount,
                        PaymentMethod = payment.PaymentMethod,
                        TransactionId = payment.TransactionId,
                        UserId = userId,
                        CreatedDateUtc = DateTime.UtcNow
                    };
                    _context.Payments.Add(newPayment);
                    totalPaid += payment.Amount;
                }

                // Update Payment Status based on total paid
                if (totalPaid >= sale.TotalAmount)
                {
                    sale.PaymentStatus = "Paid";
                }
                else if (totalPaid > 0)
                {
                    sale.PaymentStatus = "Partial";
                }
                else
                {
                    sale.PaymentStatus = "Unpaid";
                }

                // Update repair statuses if payment is complete
                if (sale.PaymentStatus == "Paid" && repairItems.Any())
                {
                    foreach (var repairId in repairItems)
                    {
                        var repair = await _context.Repairs.FindAsync(repairId);
                        if (repair != null)
                        {
                            repair.PaymentStatus = "Paid";
                            repair.Status = "Picked Up"; // Update status to Picked Up when paid
                            repair.ModifiedDate = DateTime.UtcNow;
                        }
                    }
                }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return Json(new { 
                            success = true, 
                            message = "Sale completed successfully",
                            invoiceNumber = invoiceNumber,
                            saleId = sale.Id,
                            paymentStatus = sale.PaymentStatus
                        });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw; // Re-throw to be caught by outer catch
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing sale");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<string> GenerateInvoiceNumber(int shopId)
        {
            var today = DateTime.UtcNow;
            var prefix = $"INV-{today:yyyyMMdd}";
            
            var lastInvoice = await _context.Sales
                .Where(s => s.ShopId == shopId && s.SaleNumber.StartsWith(prefix))
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastInvoice != null)
            {
                var lastNumberStr = lastInvoice.SaleNumber.Substring(prefix.Length + 1);
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}-{nextNumber:D4}";
        }

        [HttpGet]
        public async Task<IActionResult> GetBrands()
        {
            try
            {
                var brands = await _context.Brands
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.DisplayOrder)
                    .ThenBy(b => b.Name)
                    .Select(b => new { id = b.Id, name = b.Name })
                    .ToListAsync();

                return Json(new { success = true, data = brands });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting brands");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.DeviceCategories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .Select(c => new { id = c.Id, name = c.Name })
                    .ToListAsync();

                return Json(new { success = true, data = categories });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetModels(int? brandId = null, int? categoryId = null)
        {
            try
            {
                var query = _context.DeviceModels.Where(m => m.IsActive);

                if (brandId.HasValue && brandId.Value > 0)
                {
                    query = query.Where(m => m.BrandId == brandId.Value);
                }

                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    query = query.Where(m => m.DeviceCategoryId == categoryId.Value);
                }

                var models = await query
                    .OrderBy(m => m.DisplayOrder)
                    .ThenBy(m => m.Name)
                    .Select(m => new { id = m.Id, name = m.Name })
                    .ToListAsync();

                return Json(new { success = true, data = models });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting models");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult AddToCart([FromBody] dynamic item)
        {
            try
            {
                // Store cart items in session for cross-page functionality
                var cartJson = HttpContext.Session.GetString("PendingCartItems");
                List<object> cartItems;
                
                if (string.IsNullOrEmpty(cartJson))
                {
                    cartItems = new List<object>();
                }
                else
                {
                    cartItems = System.Text.Json.JsonSerializer.Deserialize<List<object>>(cartJson) ?? new List<object>();
                }
                
                // Add the new item
                cartItems.Add(item);
                
                // Save back to session
                var updatedJson = System.Text.Json.JsonSerializer.Serialize(cartItems);
                HttpContext.Session.SetString("PendingCartItems", updatedJson);
                
                _logger.LogInformation("Item added to session cart. Total items: {Count}", cartItems.Count);
                
                return Json(new { success = true, message = "Item added to cart" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddToCart");
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        [HttpGet]
        public IActionResult GetPendingCartItems()
        {
            try
            {
                var cartJson = HttpContext.Session.GetString("PendingCartItems");
                
                if (string.IsNullOrEmpty(cartJson))
                {
                    return Json(new { success = true, items = new List<object>() });
                }
                
                var cartItems = System.Text.Json.JsonSerializer.Deserialize<List<object>>(cartJson);
                
                // Clear the session after retrieving
                HttpContext.Session.Remove("PendingCartItems");
                
                return Json(new { success = true, items = cartItems });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending cart items");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchItemsTab(string type, string keyword = null, string query = null, int? customerId = null)
        {
            try
            {
                var shopId = GetCurrentShopId();
                if (shopId == 0)
                {
                    _logger.LogWarning("SearchItemsTab called with invalid shop session");
                    return Json(new { success = false, message = "Invalid shop session. Please refresh the page and try again." });
                }

                var results = new List<object>();

                // Support both 'keyword' and 'query' parameters
                var searchTerm = !string.IsNullOrWhiteSpace(keyword) ? keyword : query;
                
                // If no search term provided, return all items for the current filter type
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = ""; // Empty string to get all items
                }

                searchTerm = searchTerm.ToLower();

                // Search Inventory
                if (string.IsNullOrEmpty(type) || type == "Inventory" || type == "inventory")
                {
                    var inventoryQuery = _context.InventoryItems
                        .Include(i => i.Brand)
                        .Include(i => i.DeviceCategory)
                        .Include(i => i.DeviceModel)
                        .Where(i => i.ShopId == shopId && 
                                   i.IsActive && 
                                   i.CurrentStock > 0 &&
                                   (string.IsNullOrEmpty(searchTerm) ||
                                    i.Name.ToLower().Contains(searchTerm) || 
                                    i.Description.ToLower().Contains(searchTerm) ||
                                    i.SKU.ToLower().Contains(searchTerm) ||
                                    (i.Brand != null && i.Brand.Name.ToLower().Contains(searchTerm)) ||
                                    (i.DeviceCategory != null && i.DeviceCategory.Name.ToLower().Contains(searchTerm))))
                        .Select(i => new
                        {
                            Id = i.Id,
                            Name = i.Name,
                            Description = i.Description + (i.Brand != null ? " (" + i.Brand.Name + ")" : ""),
                            Price = i.RetailPrice,
                            Stock = i.CurrentStock,
                            Brand = i.Brand != null ? i.Brand.Name : "",
                            Category = i.DeviceCategory != null ? i.DeviceCategory.Name : "",
                            Type = "Inventory"
                        })
                        .Take(20)
                        .ToListAsync();
                    var inventory = await inventoryQuery;
                    results.AddRange(inventory);
                }

                // Search Repairs
                if (string.IsNullOrEmpty(type) || type == "Repair")
                {
                    var repairs = await _context.Repairs
                        .Include(r => r.Customer)
                        .Include(r => r.Brand)
                        .Include(r => r.DeviceModel)
                        .Where(r => r.ShopId == shopId &&
                                   (string.IsNullOrEmpty(searchTerm) ||
                                    (r.Description != null && r.Description.ToLower().Contains(searchTerm)) ||
                                    (r.RepairNumber != null && r.RepairNumber.ToLower().Contains(searchTerm)) ||
                                    (r.Customer != null && (r.Customer.FirstName + " " + r.Customer.LastName).ToLower().Contains(searchTerm))))
                        .Select(r => new
                        {
                            id = r.Id,
                            name = r.Brand != null && r.DeviceModel != null ? r.Brand.Name + " " + r.DeviceModel.Name + " Repair" : "Repair",
                            description = r.Description ?? "",
                            price = r.Cost,
                            sku = r.RepairNumber,
                            type = "Repair"
                        })
                        .Take(20)
                        .ToListAsync();
                    results.AddRange(repairs);
                }
                
                // Search Sales by Invoice Number
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var sales = await _context.Sales
                        .Include(s => s.Customer)
                        .Include(s => s.SaleItems)
                        .Where(s => s.ShopId == shopId &&
                                   s.SaleNumber.ToLower().Contains(searchTerm))
                        .Select(s => new
                        {
                            id = s.Id,
                            name = "Invoice #" + s.SaleNumber,
                            description = s.Customer != null ? s.Customer.FirstName + " " + s.Customer.LastName : "Walk-in" + " - " + s.SaleDate.ToString("MMM dd, yyyy"),
                            price = s.TotalAmount,
                            sku = s.SaleNumber,
                            type = "Sale"
                        })
                        .Take(10)
                        .ToListAsync();
                    results.AddRange(sales);
                }

                // Search Devices
                if (string.IsNullOrEmpty(type) || type == "Device")
                {
                    var devices = await _context.Devices
                        .Include(d => d.DeviceModel)
                        .Include(d => d.Brand)
                        .Include(d => d.SoldToCustomer)
                        .Where(d => d.ShopId == shopId &&
                                   (string.IsNullOrEmpty(searchTerm) ||
                                    d.DeviceModel.Name.ToLower().Contains(searchTerm) ||
                                    d.Brand.Name.ToLower().Contains(searchTerm) ||
                                    (d.IMEISerialNumber != null && d.IMEISerialNumber.ToLower().Contains(searchTerm)) ||
                                    (d.SoldToCustomer != null && (d.SoldToCustomer.FirstName + " " + d.SoldToCustomer.LastName).ToLower().Contains(searchTerm))))
                        .Select(d => new
                        {
                            id = d.Id,
                            name = d.Brand.Name + " " + d.DeviceModel.Name,
                            description = d.IMEISerialNumber + " - " + (d.SoldToCustomer != null ? d.SoldToCustomer.FirstName + " " + d.SoldToCustomer.LastName : "Available"),
                            price = d.SellingPrice ?? 0,
                            sku = d.IMEISerialNumber,
                            type = "Device"
                        })
                        .Take(20)
                        .ToListAsync();
                    results.AddRange(devices);
                }

                return Json(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching items");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetItemDetails(int id, string type)
        {
            try
            {
                var shopId = GetCurrentShopId();
                // Allow getting item details without shop session for client-side cart
                // Session will be validated when completing the sale
                
                object item = null;

                switch (type)
                {
                    case "Inventory":
                        var inventoryQuery = _context.InventoryItems
                            .Include(i => i.Brand)
                            .Where(i => i.Id == id);
                        
                        // Filter by shop if session exists
                        if (shopId > 0)
                        {
                            inventoryQuery = inventoryQuery.Where(i => i.ShopId == shopId);
                        }
                        
                        var inventory = await inventoryQuery.FirstOrDefaultAsync();
                        if (inventory != null)
                        {
                            item = new
                            {
                                id = inventory.Id,
                                name = inventory.Name,
                                description = inventory.Description,
                                price = inventory.RetailPrice,
                                type = "Inventory"
                            };
                        }
                        break;

                    case "Repair":
                        var repairQuery = _context.Repairs
                            .Include(r => r.Brand)
                            .Include(r => r.DeviceModel)
                            .Where(r => r.Id == id);
                        
                        // Filter by shop if session exists
                        if (shopId > 0)
                        {
                            repairQuery = repairQuery.Where(r => r.ShopId == shopId);
                        }
                        
                        var repair = await repairQuery.FirstOrDefaultAsync();
                        if (repair != null)
                        {
                            item = new
                            {
                                id = repair.Id,
                                name = repair.Brand != null && repair.DeviceModel != null ? repair.Brand.Name + " " + repair.DeviceModel.Name + " Repair" : "Repair",
                                description = repair.Description ?? "",
                                price = repair.Cost,
                                type = "Repair"
                            };
                        }
                        break;

                    case "Device":
                        var deviceQuery = _context.Devices
                            .Include(d => d.DeviceModel)
                            .Include(d => d.Brand)
                            .Include(d => d.SoldToCustomer)
                            .Where(d => d.Id == id);
                        
                        // Filter by shop if session exists
                        if (shopId > 0)
                        {
                            deviceQuery = deviceQuery.Where(d => d.ShopId == shopId);
                        }
                        
                        var device = await deviceQuery.FirstOrDefaultAsync();
                        if (device != null)
                        {
                            item = new
                            {
                                id = device.Id,
                                name = device.Brand.Name + " " + device.DeviceModel.Name,
                                description = device.IMEISerialNumber + " - " + (device.SoldToCustomer != null ? device.SoldToCustomer.FirstName + " " + device.SoldToCustomer.LastName : "Available"),
                                price = device.SellingPrice ?? 0,
                                type = "Device"
                            };
                        }
                        break;
                }

                if (item != null)
                {
                    return Json(new { success = true, data = item });
                }
                else
                {
                    return Json(new { success = false, message = "Item not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item details");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get the last invoice (most recent sale)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLastInvoice()
        {
            try
            {
                var shopId = GetCurrentShopId();
                
                // Get the most recent sale
                var lastSaleQuery = _context.Sales
                    .Where(s => s.ShopId == shopId)
                    .OrderByDescending(s => s.CreatedDateUtc);
                
                var lastSale = await lastSaleQuery.FirstOrDefaultAsync();
                
                if (lastSale == null)
                {
                    return Json(new { success = false, message = "No sales found" });
                }
                
                return Json(new { success = true, saleId = lastSale.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last invoice");
                return Json(new { success = false, message = "Error loading last invoice" });
            }
        }
        
        /// <summary>
        /// Get complete invoice data for printing
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetInvoiceData(int saleId)
        {
            try
            {
                var shopId = GetCurrentShopId();
                
                // Get sale with all related data
                var saleQuery = _context.Sales
                    .Include(s => s.Customer)
                    .Include(s => s.Shop)
                    .Include(s => s.SaleItems)
                    .Include(s => s.Payments)
                    .Include(s => s.CreatedByUser)
                    .Where(s => s.Id == saleId);
                
                // Filter by shop if session exists
                if (shopId > 0)
                {
                    saleQuery = saleQuery.Where(s => s.ShopId == shopId);
                }
                
                var sale = await saleQuery.FirstOrDefaultAsync();
                
                if (sale == null)
                {
                    return Json(new { success = false, message = "Sale not found" });
                }
                
                // Calculate payment details
                var totalPaid = sale.Payments.Sum(p => p.Amount);
                var changeAmount = totalPaid > sale.TotalAmount ? totalPaid - sale.TotalAmount : 0;
                
                // Format payment methods
                var paymentMethods = sale.Payments
                    .Select(p => new
                    {
                        method = p.PaymentMethod,
                        amount = p.Amount
                    })
                    .ToList();
                
                var invoiceData = new
                {
                    success = true,
                    // Shop details
                    shop = new
                    {
                        name = sale.Shop.Name,
                        address = sale.Shop.Address,
                        city = sale.Shop.City,
                        state = sale.Shop.State,
                        zipCode = sale.Shop.ZipCode,
                        phone = sale.Shop.Phone,
                        email = sale.Shop.Email,
                        logoPath = sale.Shop.LogoPath
                    },
                    // Invoice details
                    invoice = new
                    {
                        number = sale.SaleNumber,
                        date = sale.SaleDate,
                        createdBy = sale.CreatedByUser?.Username ?? "System"
                    },
                    // Customer details
                    customer = new
                    {
                        name = sale.Customer != null ? $"{sale.Customer.FirstName} {sale.Customer.LastName}" : "Walk-in Customer",
                        phone = sale.Customer?.Phone ?? "",
                        email = sale.Customer?.Email ?? "",
                        address = sale.Customer?.Address ?? ""
                    },
                    // Items
                    items = sale.SaleItems.Select(item => new
                    {
                        name = item.ItemName,
                        description = item.Description,
                        quantity = item.Quantity,
                        unitPrice = item.UnitPrice,
                        totalPrice = item.TotalPrice,
                        discount = item.DiscountAmount
                    }).ToList(),
                    // Totals
                    totals = new
                    {
                        subtotal = sale.SubTotal,
                        tax = sale.TaxAmount,
                        discount = sale.DiscountAmount,
                        total = sale.TotalAmount
                    },
                    // Payment details
                    payment = new
                    {
                        status = sale.PaymentStatus,
                        methods = paymentMethods,
                        totalPaid = totalPaid,
                        change = changeAmount,
                        balance = sale.TotalAmount - totalPaid
                    }
                };
                
                return Json(invoiceData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoice data for sale {SaleId}", saleId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Search inventory items by filters
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchInventory(int? brandId = null, int? deviceCategoryId = null, 
            int? modelId = null, int? inventoryCategoryId = null)
        {
            try
            {
                var shopId = GetCurrentShopId();
                
                // Build query
                var query = _context.InventoryItems
                    .Include(i => i.Brand)
                    .Include(i => i.DeviceModel)
                    .Include(i => i.DeviceCategory)
                    .Include(i => i.InventoryCategory)
                    .Include(i => i.Item)
                    .AsQueryable();
                
                // Filter by shop if session exists
                if (shopId > 0)
                {
                    query = query.Where(i => i.ShopId == shopId);
                }
                
                // Only show active items
                query = query.Where(i => i.IsActive);
                
                // Apply filters
                if (brandId.HasValue && brandId.Value > 0)
                {
                    query = query.Where(i => i.BrandId == brandId.Value);
                }
                
                if (deviceCategoryId.HasValue && deviceCategoryId.Value > 0)
                {
                    query = query.Where(i => i.DeviceCategoryId == deviceCategoryId.Value);
                }
                
                if (modelId.HasValue && modelId.Value > 0)
                {
                    query = query.Where(i => i.DeviceModelId == modelId.Value);
                }
                
                if (inventoryCategoryId.HasValue && inventoryCategoryId.Value > 0)
                {
                    query = query.Where(i => i.InventoryCategoryId == inventoryCategoryId.Value);
                }
                
                // Only show items with stock > 0
                query = query.Where(i => i.CurrentStock > 0);
                
                var items = await query
                    .Select(i => new
                    {
                        id = i.Id,
                        name = i.Name,
                        description = i.Description,
                        sku = i.SKU,
                        price = i.RetailPrice,
                        stock = i.CurrentStock,
                        brandName = i.Brand != null ? i.Brand.Name : null,
                        modelName = i.DeviceModel != null ? i.DeviceModel.Name : null,
                        categoryName = i.InventoryCategory != null ? i.InventoryCategory.Name : null
                    })
                    .Take(50) // Limit results to 50 items
                    .ToListAsync();
                
                return Json(new { success = true, data = items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching inventory");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Search devices by filters with OR condition
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchDevices(int? brandId = null, int? deviceCategoryId = null, 
            int? modelId = null, string network = null, string gb = null, string scratches = null, string imei = null)
        {
            try
            {
                var shopId = GetCurrentShopId();
                
                // Build query
                var query = _context.Devices
                    .Include(d => d.Brand)
                    .Include(d => d.DeviceModel)
                    .Include(d => d.DeviceCategory)
                    .AsQueryable();
                
                // Filter by shop if session exists
                if (shopId > 0)
                {
                    query = query.Where(d => d.ShopId == shopId);
                }
                
                // Only show available devices (not sold)
                query = query.Where(d => d.IsAvailableForSale && !d.IsSold);
                
                // OR condition: Apply filters if any are provided
                bool hasFilters = false;
                IQueryable<Device> filteredQuery = null;
                
                // Brand filter
                if (brandId.HasValue && brandId.Value > 0)
                {
                    var brandQuery = query.Where(d => d.BrandId == brandId.Value);
                    filteredQuery = hasFilters ? filteredQuery.Union(brandQuery) : brandQuery;
                    hasFilters = true;
                }
                
                // Device Category filter
                if (deviceCategoryId.HasValue && deviceCategoryId.Value > 0)
                {
                    var categoryQuery = query.Where(d => d.DeviceCategoryId == deviceCategoryId.Value);
                    filteredQuery = hasFilters ? filteredQuery.Union(categoryQuery) : categoryQuery;
                    hasFilters = true;
                }
                
                // Model filter
                if (modelId.HasValue && modelId.Value > 0)
                {
                    var modelQuery = query.Where(d => d.DeviceModelId == modelId.Value);
                    filteredQuery = hasFilters ? filteredQuery.Union(modelQuery) : modelQuery;
                    hasFilters = true;
                }
                
                // Network filter
                if (!string.IsNullOrEmpty(network))
                {
                    var networkQuery = query.Where(d => d.NetworkStatus == network);
                    filteredQuery = hasFilters ? filteredQuery.Union(networkQuery) : networkQuery;
                    hasFilters = true;
                }
                
                // GB (Storage) filter
                if (!string.IsNullOrEmpty(gb))
                {
                    var gbQuery = query.Where(d => d.GB == gb);
                    filteredQuery = hasFilters ? filteredQuery.Union(gbQuery) : gbQuery;
                    hasFilters = true;
                }
                
                // Scratches filter
                if (!string.IsNullOrEmpty(scratches))
                {
                    var scratchesQuery = query.Where(d => d.ScratchesCondition == scratches);
                    filteredQuery = hasFilters ? filteredQuery.Union(scratchesQuery) : scratchesQuery;
                    hasFilters = true;
                }
                
                // IMEI filter (exact match)
                if (!string.IsNullOrEmpty(imei))
                {
                    var imeiQuery = query.Where(d => d.IMEISerialNumber == imei);
                    filteredQuery = hasFilters ? filteredQuery.Union(imeiQuery) : imeiQuery;
                    hasFilters = true;
                }
                
                // Use filtered query if filters were applied, otherwise use base query
                var finalQuery = hasFilters ? filteredQuery : query;
                
                var devices = await finalQuery
                    .Select(d => new
                    {
                        id = d.Id,
                        name = d.Brand.Name + " " + d.DeviceModel.Name + (d.GB != null ? " " + d.GB + "GB" : ""),
                        imei = d.IMEISerialNumber,
                        brandName = d.Brand != null ? d.Brand.Name : null,
                        modelName = d.DeviceModel != null ? d.DeviceModel.Name : null,
                        categoryName = d.DeviceCategory != null ? d.DeviceCategory.Name : null,
                        network = d.NetworkStatus,
                        gb = d.GB,
                        scratches = d.ScratchesCondition,
                        condition = d.ScratchesCondition,
                        buyingPrice = d.BuyingPrice,
                        sellingPrice = d.SellingPrice,
                        stock = 1 // Devices are unique items
                    })
                    .Take(50) // Limit results to 50 items
                    .ToListAsync();
                
                return Json(new { success = true, data = devices });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching devices");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Search for a sale by sale number
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchSaleByNumber(string saleNumber)
        {
            try
            {
                var shopId = GetCurrentShopId();
                
                var query = _context.Sales
                    .Include(s => s.Customer)
                    .Include(s => s.SaleItems)
                    .Include(s => s.Payments)
                    .Where(s => s.SaleNumber.Contains(saleNumber));
                
                // Filter by shop if session exists
                if (shopId > 0)
                {
                    query = query.Where(s => s.ShopId == shopId);
                }
                
                var sale = await query.FirstOrDefaultAsync();
                
                if (sale == null)
                {
                    return Json(new { success = false, message = "Invoice not found" });
                }
                
                var saleData = new
                {
                    id = sale.Id,
                    saleNumber = sale.SaleNumber,
                    saleDate = sale.SaleDate,
                    customerName = sale.Customer != null ? $"{sale.Customer.FirstName} {sale.Customer.LastName}" : "Walk-in Customer",
                    customerId = sale.CustomerId,
                    totalAmount = sale.TotalAmount,
                    paymentStatus = sale.PaymentStatus,
                    itemCount = sale.SaleItems.Count
                };
                
                return Json(new { success = true, sale = saleData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for sale by number {SaleNumber}", saleNumber);
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing sale/invoice
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateSale([FromBody] UpdateSaleRequest request)
        {
            try
            {
                var shopId = GetCurrentShopId();
                if (shopId == 0)
                {
                    return Json(new { success = false, message = "Invalid shop session" });
                }

                // Get the existing sale
                var sale = await _context.Sales
                    .Include(s => s.SaleItems)
                    .Include(s => s.Payments)
                    .Where(s => s.Id == request.SaleId && s.ShopId == shopId)
                    .FirstOrDefaultAsync();

                if (sale == null)
                {
                    return Json(new { success = false, message = "Sale not found" });
                }

                // Update customer
                if (request.CustomerId.HasValue)
                {
                    sale.CustomerId = request.CustomerId.Value;
                }

                // Remove old sale items
                _context.SaleItems.RemoveRange(sale.SaleItems);

                // Add updated sale items
                foreach (var item in request.Items)
                {
                    var saleItem = new SaleItem
                    {
                        SaleId = sale.Id,
                        ItemName = item.ItemName,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.Quantity * item.UnitPrice,
                        DiscountAmount = item.DiscountAmount ?? 0,
                        CostPrice = item.CostPrice ?? 0,
                        InventoryItemId = item.InventoryItemId,
                        IsCustomItem = item.IsCustomItem
                    };
                    _context.SaleItems.Add(saleItem);
                }

                // Recalculate totals
                sale.SubTotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);
                sale.DiscountAmount = request.Items.Sum(i => i.DiscountAmount ?? 0);
                sale.TaxAmount = request.TaxAmount ?? 0;
                sale.TotalAmount = sale.SubTotal - sale.DiscountAmount + sale.TaxAmount;

                // Update payment status if provided
                if (!string.IsNullOrEmpty(request.PaymentStatus))
                {
                    sale.PaymentStatus = request.PaymentStatus;
                }

                // Update payments if provided
                if (request.Payments != null && request.Payments.Any())
                {
                    // Remove old payments
                    _context.Payments.RemoveRange(sale.Payments);

                    // Add new payments
                    foreach (var payment in request.Payments)
                    {
                        _context.Payments.Add(new Payment
                        {
                            SaleId = sale.Id,
                            Amount = payment.Amount,
                            PaymentMethod = payment.PaymentMethod,
                            TransactionId = payment.TransactionId,
                            UserId = GetCurrentUserId()
                            // CreatedDateUtc is set automatically by AuditableEntity
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Invoice updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sale {SaleId}", request.SaleId);
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    // Request models for UpdateSale
    public class UpdateSaleRequest
    {
        public int SaleId { get; set; }
        public int? CustomerId { get; set; }
        public List<UpdateSaleItemRequest> Items { get; set; }
        public decimal? TaxAmount { get; set; }
        public string PaymentStatus { get; set; }
        public List<UpdatePaymentRequest> Payments { get; set; }
    }

    public class UpdateSaleItemRequest
    {
        public string ItemName { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? CostPrice { get; set; }
        public int? InventoryItemId { get; set; }
        public bool IsCustomItem { get; set; }
    }

    public class UpdatePaymentRequest
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionId { get; set; }
    }
}
