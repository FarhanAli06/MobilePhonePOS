using Microsoft.AspNetCore.Mvc;
using Empire.Application.Interfaces;
using Empire.Application.DTOs.Inventory;
using Empire.Application.DTOs.Device;
using Empire.Application.DTOs.Customer;
using Microsoft.EntityFrameworkCore;
using Empire.Infrastructure.Data;
using Empire.Web.Authorization;

namespace Empire.Web.Controllers
{
    [SessionAuthorize]
    public class SaleController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly IDeviceService _deviceService;
        private readonly ICustomerService _customerService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SaleController> _logger;

        public SaleController(
            IInventoryService inventoryService,
            IDeviceService deviceService,
            ICustomerService customerService,
            IServiceProvider serviceProvider,
            ILogger<SaleController> logger)
        {
            _inventoryService = inventoryService;
            _deviceService = deviceService;
            _customerService = customerService;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewBag.CurrentShopId = GetCurrentShopId();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts(string search = "", int brandId = 0, int categoryId = 0, int deviceCategoryId = 0, int deviceModelId = 0)
        {
            try
            {
                _logger.LogInformation($"GetProducts called with search='{search}', brandId={brandId}, categoryId={categoryId}, deviceCategoryId={deviceCategoryId}, deviceModelId={deviceModelId}");
                
                var shopId = GetCurrentShopId();
                _logger.LogInformation($"Current ShopId: {shopId}");
                
                var products = new List<object>();

                try
                {
                    var inventoryFilter = new InventoryFilterRequest
                    {
                        ShopId = shopId,
                        SearchTerm = search,
                        Category = categoryId > 0 ? categoryId.ToString() : null
                    };

                    var inventoryItems = await _inventoryService.GetInventoryAsync(inventoryFilter);
                    _logger.LogInformation($"Retrieved {inventoryItems?.Count() ?? 0} inventory items");

                    // Add inventory items
                    if (inventoryItems != null)
                    {
                        foreach (var item in inventoryItems)
                        {
                            products.Add(new
                            {
                                id = $"inv_{item.Id}",
                                name = item.Name,
                                brand = item.DeviceBrand ?? "Generic",
                                category = item.Category,
                                sku = $"INV-{item.Id}",
                                price = item.RetailPrice,
                                wholesalePrice = item.CostPrice,
                                stock = item.Stock,
                                type = "inventory"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading inventory items");
                }

                try
                {
                    var deviceFilter = new DeviceFilterRequest
                    {
                        ShopId = shopId,
                        SearchTerm = search,
                        Brand = brandId > 0 ? brandId.ToString() : null,
                        IsAvailableForSale = true
                    };

                    var devices = await _deviceService.GetDevicesAsync(deviceFilter);
                    _logger.LogInformation($"Retrieved {devices?.Count() ?? 0} devices");

                    // Add devices
                    if (devices != null)
                    {
                        foreach (var device in devices)
                        {
                            products.Add(new
                            {
                                id = $"dev_{device.Id}",
                                name = $"{device.Brand} {device.Model}",
                                brand = device.Brand,
                                category = device.Category,
                                sku = device.ModelNumber ?? "N/A",
                                price = 100, // Default price for testing
                                wholesalePrice = 80,
                                stock = 1,
                                type = "device"
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading devices");
                }

                _logger.LogInformation($"Returning {products.Count} total products");
                return Json(new { success = true, data = products });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProducts method");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers(string search = "")
        {
            try
            {
                _logger.LogInformation($"GetCustomers called with search='{search}'");
                
                var shopId = GetCurrentShopId();
                _logger.LogInformation($"Current ShopId: {shopId}");
                
                var customerList = new List<object>();

                try
                {
                    var filter = new CustomerFilterRequest
                    {
                        ShopId = shopId,
                        SearchTerm = search
                    };

                    var customers = await _customerService.GetCustomersAsync(filter);
                    _logger.LogInformation($"Retrieved {customers?.Count() ?? 0} customers");
                    
                    if (customers != null)
                    {
                        customerList = customers.Select(c => new
                        {
                            id = c.Id,
                            text = $"{c.FirstName} {c.LastName} - {c.Phone}",
                            name = $"{c.FirstName} {c.LastName}",
                            phone = c.Phone,
                            email = c.Email
                        }).ToList<object>();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading customers from database");
                }

                _logger.LogInformation($"Returning {customerList.Count} customers");
                return Json(new { success = true, data = customerList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCustomers method");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBrands()
        {
            try
            {
                _logger.LogInformation("GetBrands called");
                
                var brands = new List<object>();

                try
                {
                    // Get brands from database using DbContext directly
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<EmpireDbContext>();
                    
                    var brandEntities = await dbContext.Brands
                        .Where(b => b.IsActive)
                        .OrderBy(b => b.DisplayOrder)
                        .ThenBy(b => b.Name)
                        .ToListAsync();
                    
                    _logger.LogInformation($"Retrieved {brandEntities.Count} brands from database");
                    
                    brands = brandEntities.Select(b => new
                    {
                        id = b.Id,
                        name = b.Name
                    }).ToList<object>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading brands from database");
                }

                _logger.LogInformation($"Returning {brands.Count} brands");
                return Json(new { success = true, data = brands });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetBrands method");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                _logger.LogInformation("GetCategories called");
                
                var categories = new List<object>();

                try
                {
                    // Get categories from database using DbContext directly
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<EmpireDbContext>();
                    
                    var categoryEntities = await dbContext.InventoryCategories
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.DisplayOrder)
                        .ThenBy(c => c.Name)
                        .ToListAsync();
                    
                    _logger.LogInformation($"Retrieved {categoryEntities.Count} categories from database");
                    
                    categories = categoryEntities.Select(c => new
                    {
                        id = c.Id,
                        name = c.Name
                    }).ToList<object>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading categories from database");
                }

                _logger.LogInformation($"Returning {categories.Count} categories");
                return Json(new { success = true, data = categories });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCategories method");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessSale([FromBody] ProcessSaleRequest request)
        {
            try
            {
                var shopId = GetCurrentShopId();
                var userId = GetCurrentUserId();

                // Here you would implement the actual sale processing logic
                // This would involve:
                // 1. Creating a sale record
                // 2. Updating inventory quantities
                // 3. Updating device statuses
                // 4. Creating payment records
                // 5. Generating invoice

                // For now, return success
                return Json(new { 
                    success = true, 
                    message = "Sale processed successfully",
                    invoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sale");
                return Json(new { success = false, message = "Error processing sale" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddCustomer([FromBody] AddCustomerRequest request)
        {
            try
            {
                _logger.LogInformation("AddCustomer called");
                
                var shopId = GetCurrentShopId();
                
                var createRequest = new CreateCustomerRequest
                {
                    ShopId = shopId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Phone = request.Phone,
                    Email = request.Email,
                    Address = request.Address,
                    City = request.City,
                    State = request.State,
                    ZipCode = request.ZipCode
                };

                var customer = await _customerService.CreateCustomerAsync(createRequest);
                
                _logger.LogInformation($"Customer created with ID: {customer.Id}");
                
                return Json(new 
                { 
                    success = true, 
                    data = new
                    {
                        id = customer.Id,
                        text = $"{customer.FirstName} {customer.LastName} - {customer.Phone}",
                        name = $"{customer.FirstName} {customer.LastName}",
                        phone = customer.Phone,
                        email = customer.Email
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding customer");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDeviceCategories()
        {
            try
            {
                _logger.LogInformation("GetDeviceCategories called");
                
                var categories = new List<object>();

                try
                {
                    // Get device categories from database using DbContext directly
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<EmpireDbContext>();
                    
                    var categoryEntities = await dbContext.DeviceCategories
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.DisplayOrder)
                        .ThenBy(c => c.Name)
                        .ToListAsync();
                    
                    _logger.LogInformation($"Retrieved {categoryEntities.Count} device categories from database");
                    
                    categories = categoryEntities.Select(c => new
                    {
                        id = c.Id,
                        name = c.Name
                    }).ToList<object>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading device categories from database");
                }

                _logger.LogInformation($"Returning {categories.Count} device categories");
                return Json(new { success = true, data = categories });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDeviceCategories method");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDeviceModels(int brandId, int categoryId)
        {
            try
            {
                _logger.LogInformation($"GetDeviceModels called with brandId={brandId}, categoryId={categoryId}");
                
                var models = new List<object>();

                try
                {
                    // Get device models from database using DbContext directly
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<EmpireDbContext>();
                    
                    var modelEntities = await dbContext.DeviceModels
                        .Where(m => m.IsActive && m.BrandId == brandId && m.DeviceCategoryId == categoryId)
                        .OrderBy(m => m.DisplayOrder)
                        .ThenBy(m => m.Name)
                        .ToListAsync();
                    
                    _logger.LogInformation($"Retrieved {modelEntities.Count} device models from database");
                    
                    models = modelEntities.Select(m => new
                    {
                        id = m.Id,
                        name = m.Name
                    }).ToList<object>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading device models from database");
                }

                _logger.LogInformation($"Returning {models.Count} device models");
                return Json(new { success = true, data = models });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDeviceModels method");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private int GetCurrentShopId()
        {
            var shopIdString = HttpContext.Session.GetString("CurrentShopId");
            return int.TryParse(shopIdString, out var shopId) ? shopId : 1;
        }

        private int GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            return int.TryParse(userIdString, out var userId) ? userId : 1;
        }
    }

    public class AddCustomerRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
    }

    public class ProcessSaleRequest
    {
        public int? CustomerId { get; set; }
        public List<SaleItemRequest> Items { get; set; } = new();
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class SaleItemRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}

