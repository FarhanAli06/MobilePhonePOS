using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Empire.Web.Authorization;
using Empire.Web.Services.Inventory;
using Empire.Web.Services.Device;
using Empire.Web.Services.Customer;
using Empire.Web.DTOs.Inventory;
using Empire.Web.Services.API;
using Empire.Web.Services.Brand;
using Empire.Web.Services.DeviceModel;
using Empire.Web.DTOs.Customer;
using Empire.Web.Services;
using Empire.Web.Models;

namespace Empire.Web.Controllers
{
    [SessionAuthorize]
    public class SaleController : BaseController
    {
        private readonly IInventoryApiService _inventoryService;
        private readonly IDeviceApiService _deviceService;
        private readonly ICustomerApiService _customerService;
        private readonly IMapper _mapper;
        private readonly IBrandApiService _brandApi;
        private readonly IInventoryCategoryApiService _inventoryCategoryApi;
        private readonly IServiceProvider _serviceProvider;

        public SaleController(
            IInventoryApiService inventoryService,
            IDeviceApiService deviceService,
            ICustomerApiService customerService,
            IMapper mapper,
            IBrandApiService brandApi,
            IInventoryCategoryApiService inventoryCategoryApi,
            IServiceProvider serviceProvider,
            ITimezoneService timezoneService,
            ILogger<SaleController> logger)
            : base(logger, timezoneService)
        {
            _inventoryService = inventoryService;
            _deviceService = deviceService;
            _customerService = customerService;
            _mapper = mapper;
            _brandApi = brandApi;
            _inventoryCategoryApi = inventoryCategoryApi;
            _serviceProvider = serviceProvider;
        }

        public IActionResult Index()
        {            return View();
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
                    var inventoryItems = await _inventoryService.GetInventoryAsync(shopId);
                    
                    // Apply client-side filtering
                    if (!string.IsNullOrEmpty(search))
                    {
                        inventoryItems = inventoryItems.Where(i => 
                            i.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            (i.SKU != null && i.SKU.Contains(search, StringComparison.OrdinalIgnoreCase)));
                    }
                    if (categoryId > 0)
                    {
                        inventoryItems = inventoryItems.Where(i => i.InventoryCategoryId == categoryId);
                    }
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
                                brand = item.Brand ?? "Generic",
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
                    var devices = await _deviceService.GetDevicesAsync(shopId);
                    
                    // Apply client-side filtering
                    if (!string.IsNullOrEmpty(search))
                    {
                        devices = devices.Where(d => 
                            d.Model.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            (d.IMEISerialNumber != null && d.IMEISerialNumber.Contains(search, StringComparison.OrdinalIgnoreCase)));
                    }
                    if (brandId > 0)
                    {
                        devices = devices.Where(d => d.Brand.Equals(brandId.ToString(), StringComparison.OrdinalIgnoreCase));
                    }
                    // Filter for available devices
                    devices = devices.Where(d => d.IsAvailableForSale);
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
                    var customers = await _customerService.GetCustomersAsync(shopId);
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
                
                var brandEntities = await _brandApi.GetAllAsync();
                _logger.LogInformation($"Retrieved {brandEntities?.Count ?? 0} brands from API");
                
                var brands = brandEntities?
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.DisplayOrder)
                    .ThenBy(b => b.Name)
                    .Select(b => new
                    {
                        id = b.Id,
                        name = b.Name
                    })
                    .ToList<object>() ?? new List<object>();

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
                
                var categoryEntities = await _inventoryCategoryApi.GetAllAsync();
                _logger.LogInformation($"Retrieved {categoryEntities?.Count ?? 0} categories from API");
                
                var categories = categoryEntities?
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.Name
                    })
                    .ToList<object>() ?? new List<object>();

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
                    invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}"
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
                
                // Map request to CreateCustomerRequestDto using AutoMapper
                var createRequest = _mapper.Map<CreateCustomerRequestDto>(request);
                createRequest.ShopId = shopId;

                var customer = await _customerService.CreateAsync(createRequest);
                
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
                    // Get device categories using API service
                    var categoryEntities = await _inventoryCategoryApi.GetAllAsync();
                    
                    _logger.LogInformation($"Retrieved {categoryEntities.Count()} device categories from API");
                    
                    categories = categoryEntities
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.DisplayOrder)
                        .ThenBy(c => c.Name)
                        .Select(c => new
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
                    // Get device models using service provider to resolve DeviceModelApiService
                    using var scope = _serviceProvider.CreateScope();
                    var modelService = scope.ServiceProvider.GetRequiredService<IDeviceModelApiService>();
                    
                    var allModels = await modelService.GetAllAsync();
                    
                    // Filter by brandId and categoryId on client side
                    var filteredModels = allModels
                        .Where(m => m.IsActive && m.BrandId == brandId && m.DeviceCategoryId == categoryId)
                        .OrderBy(m => m.DisplayOrder)
                        .ThenBy(m => m.Name)
                        .ToList();
                    
                    _logger.LogInformation($"Retrieved {filteredModels.Count} device models from API");
                    
                    models = filteredModels.Select(m => new
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

