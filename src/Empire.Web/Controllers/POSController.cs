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
            return HttpContext.Session.GetInt32("ShopId") ?? 0;
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        public IActionResult Index()
        {
            ViewBag.CurrentShopId = GetCurrentShopId();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchItems(string query = "", string type = "")
        {
            try
            {
                var shopId = GetCurrentShopId();
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

                // Search Inventory
                if (string.IsNullOrEmpty(type) || type == "inventory")
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

                // Search Repairs (Completed and Unpaid)
                if (string.IsNullOrEmpty(type) || type == "repair")
                {
                    var repairs = await _context.Repairs
                        .Include(r => r.Customer)
                        .Include(r => r.Brand)
                        .Include(r => r.DeviceModel)
                        .Where(r => r.ShopId == shopId && 
                                   r.Status == "Complete" &&
                                   r.PaymentStatus != "Paid" &&
                                   (string.IsNullOrEmpty(query) ||
                                    r.RepairNumber.Contains(query) ||
                                    r.Customer.FirstName.Contains(query) ||
                                    r.Customer.LastName.Contains(query) ||
                                    r.Issue.Contains(query)))
                        .Take(50)
                        .ToListAsync();

                    foreach (var repair in repairs)
                    {
                        results.Add(new POSSearchResult
                        {
                            Id = $"rep_{repair.Id}",
                            Type = "Repair",
                            Name = $"Repair #{repair.RepairNumber}",
                            Description = $"{repair.Issue} - {repair.Brand?.Name} {repair.DeviceModel?.Name}",
                            Brand = repair.Brand?.Name ?? "",
                            Category = "Repair Service",
                            SKU = repair.RepairNumber,
                            Price = repair.Cost,
                            CostPrice = null,
                            Stock = 1,
                            RepairNumber = repair.RepairNumber,
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

                return Json(new { success = true, data = customers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CompleteSale([FromBody] CreateSaleRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var shopId = GetCurrentShopId();
                var userId = GetCurrentUserId();

                // Generate invoice number
                var invoiceNumber = await GenerateInvoiceNumber(shopId);

                // Create sale
                var sale = new Sale
                {
                    ShopId = shopId,
                    CustomerId = request.CustomerId,
                    InvoiceNumber = invoiceNumber,
                    SubTotal = request.SubTotal,
                    TaxAmount = request.TaxAmount,
                    DiscountAmount = request.DiscountAmount,
                    TotalAmount = request.TotalAmount,
                    PaymentMethod = request.PaymentMethod,
                    PaymentStatus = "Paid",
                    Notes = request.Notes,
                    SaleDate = DateTime.UtcNow,
                    CreatedByUserId = userId,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                // Process each item
                foreach (var item in request.Items)
                {
                    var saleItem = new SaleItem
                    {
                        SaleId = sale.Id,
                        ItemType = item.ItemType,
                        ItemReferenceId = item.ItemReferenceId,
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        OriginalPrice = item.OriginalPrice,
                        SubTotal = item.UnitPrice * item.Quantity,
                        DiscountAmount = item.DiscountAmount,
                        TotalAmount = (item.UnitPrice * item.Quantity) - item.DiscountAmount,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.SaleItems.Add(saleItem);

                    // Update item status based on type
                    if (item.ItemType == "Device" && item.ItemReferenceId.HasValue)
                    {
                        var device = await _context.Devices.FindAsync(item.ItemReferenceId.Value);
                        if (device != null)
                        {
                            device.IsSold = true;
                            device.SoldDate = DateTime.UtcNow;
                            device.SoldToCustomerId = request.CustomerId;
                            // SalePrice stored in SaleItem, not on Device entity
                            device.ModifiedDate = DateTime.UtcNow;
                        }
                    }
                    else if (item.ItemType == "Inventory" && item.ItemReferenceId.HasValue)
                    {
                        var inventory = await _context.Inventories.FindAsync(item.ItemReferenceId.Value);
                        if (inventory != null)
                        {
                            inventory.Stock -= item.Quantity;
                            inventory.ModifiedDate = DateTime.UtcNow;

                            // Create inventory adjustment
                            var adjustment = new InventoryAdjustment
                            {
                                InventoryId = inventory.Id,
                                AdjustmentType = "Sale",
                                Quantity = -item.Quantity,
                                Reason = $"Sold via Invoice #{invoiceNumber}",
                                AdjustmentDate = DateTime.UtcNow
                            };
                            _context.InventoryAdjustments.Add(adjustment);
                        }
                    }
                    else if (item.ItemType == "Repair" && item.ItemReferenceId.HasValue)
                    {
                        var repair = await _context.Repairs.FindAsync(item.ItemReferenceId.Value);
                        if (repair != null)
                        {
                            repair.PaymentStatus = "Paid";
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
                    saleId = sale.Id
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error completing sale");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<string> GenerateInvoiceNumber(int shopId)
        {
            var today = DateTime.UtcNow;
            var prefix = $"INV-{today:yyyyMMdd}";
            
            var lastInvoice = await _context.Sales
                .Where(s => s.ShopId == shopId && s.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastInvoice != null)
            {
                var lastNumberStr = lastInvoice.InvoiceNumber.Substring(prefix.Length + 1);
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
    }
}

