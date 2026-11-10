using Empire.Web.Authorization;
using Empire.Web.Extensions;
using Empire.Web.Services;
using Empire.Web.Services.API;
using Empire.Web.Services.Brand;
using Empire.Web.Services.DeviceCategory;
using Empire.Web.Services.DeviceModel;
using Empire.Web.Services.POS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Empire.Web.DTOs.Pos;
using Empire.Web.Services.Inventory;
using Empire.Web.Services.Device;
using Empire.Web.Services.Repair;
using Empire.Web.Services.Customer;
using Empire.Web.DTOs.Device;
using Empire.Web.DTOs.Inventory;
using Empire.Web.DTOs.Sale;
using Empire.Web.DTOs.SaleItem;
using Empire.Web.DTOs.Common;
using Empire.Web.Services.Helper;
using Empire.Web.Services.POSMapping;
using Empire.Web.Services.Printing;
using System.Text;

namespace Empire.Web.Controllers
{
    [SessionAuthorize]
    public class POSController : BaseController
    {
    private readonly IPOSApiService _posApiService;
    private readonly IBrandApiService _brandApiService;
    private readonly IDeviceCategoryApiService _deviceCategoryApiService;
    private readonly IDeviceModelApiService _deviceModelApiService;
    private readonly IHelperService _helperService;
    private readonly IPOSMappingService _posMappingService;
    private readonly ILogger<POSController> _logger;

    public POSController(
        IPOSApiService posApiService,
        IBrandApiService brandApiService,
        IDeviceCategoryApiService deviceCategoryApiService,
        IDeviceModelApiService deviceModelApiService,
        IHelperService helperService,
        IPOSMappingService posMappingService,
        ILogger<POSController> logger,
        ITimezoneService timezoneService) : base(logger, timezoneService)
    {
        _posApiService = posApiService;
        _brandApiService = brandApiService;
        _deviceCategoryApiService = deviceCategoryApiService;
        _deviceModelApiService = deviceModelApiService;
        _helperService = helperService;
        _posMappingService = posMappingService;
        _logger = logger;
    }

        public IActionResult Index()
        {            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchItems(string query = "", string type = "", int? customerId = null)
        {
            try
            {
                var shopId = GetCurrentShopId();
                var results = new List<object>();

                // Search products (devices, inventory, repairs)
                var searchRequest = new POSProductSearchRequestDto
                {
                    ShopId = shopId,
                    SearchTerm = query
                };

                var products = await _posApiService.SearchProductsAsync(searchRequest);
                if (products != null)
                {
                    results.AddRange(_posMappingService.MapProductSearchResults(products));
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
        public async Task<IActionResult> GetCustomers(string search = "")
        {
            try
            {
                var shopId = GetCurrentShopId();
                var customers = await _posApiService.SearchCustomersAsync(shopId, search);

                if (customers == null)
                {
                    return Json(new { success = false, message = "Error loading customers" });
                }

                var results = _posMappingService.MapCustomerSearchResults(customers);

                return Json(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerRepairs(int customerId)
        {
            try
            {
                var shopId = GetCurrentShopId();
                var repairs = await _posApiService.SearchRepairsAsync(shopId, $"customer:{customerId}");

                if (repairs == null)
                {
                    return Json(new { success = false, message = "Error loading repairs" });
                }

                var results = _posMappingService.MapRepairSearchResults(repairs);

                return Json(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer repairs");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CompleteSale([FromBody] CreateSaleRequestDto request)
        {
            try
            {
                var shopId = GetCurrentShopId();
                var userId = GetCurrentUserId();

                // Use mapping service to convert request
                var posRequest = _posMappingService.MapToPosSaleRequest(request, shopId, userId);

                var result = await _posApiService.CreateSaleAsync(posRequest);

                if (result == null)
                {
                    return Json(new { success = false, message = "No response from sale service" });
                }

                // Resolve the actual sale ID (API returns 'id' in data object)
                var resolvedSaleId = result.Id > 0 ? result.Id : result.SaleId;
                if (resolvedSaleId <= 0)
                {
                    return Json(new { success = false, message = result.Message ?? "Error creating sale" });
                }

                // Resolve invoice number: prefer InvoiceNumber, fall back to SaleNumber
                var resolvedInvoice = !string.IsNullOrEmpty(result.InvoiceNumber)
                    ? result.InvoiceNumber
                    : result.SaleNumber;

                return Json(new
                {
                    success = true,
                    saleId = resolvedSaleId,
                    invoiceNumber = resolvedInvoice,
                    message = "Sale completed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing sale");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBrands()
        {
            try
            {
                var brands = await _brandApiService.GetAllAsync();
                
                var results = _helperService.CreateDropdownList(
                    brands ?? Enumerable.Empty<dynamic>(),
                    b => b.Id,
                    b => b.Name,
                    b => b.IsActive,
                    b => b.DisplayOrder);

                return Json(new { success = true, data = results });
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
                var categories = await _deviceCategoryApiService.GetAllAsync();
                
                var results = _helperService.CreateDropdownList(
                    categories ?? Enumerable.Empty<dynamic>(),
                    c => c.Id,
                    c => c.Name,
                    c => c.IsActive,
                    c => c.DisplayOrder);

                return Json(new { success = true, data = results });
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
                var models = await _deviceModelApiService.GetAllAsync();
                
                // Filter active models
                var filteredModels = _helperService.FilterActive(
                    models ?? Enumerable.Empty<dynamic>(),
                    m => m.IsActive);

                // Apply brand and category filters
                if (brandId.HasValue && brandId.Value > 0)
                    filteredModels = filteredModels.Where(m => m.BrandId == brandId.Value);

                if (categoryId.HasValue && categoryId.Value > 0)
                    filteredModels = filteredModels.Where(m => m.DeviceCategoryId == categoryId.Value);

                var results = _helperService.CreateDropdownList(
                    filteredModels,
                    m => m.Id,
                    m => m.Name);

                return Json(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting models");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult AddToCart([FromBody] System.Text.Json.JsonElement item)
        {
            try
            {
                // Store item in session so POS page can pick it up on load
                const string sessionKey = "PendingCartItems";
                var existing = HttpContext.Session.GetString(sessionKey);
                var list = string.IsNullOrEmpty(existing)
                    ? new System.Collections.Generic.List<System.Text.Json.JsonElement>()
                    : System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<System.Text.Json.JsonElement>>(existing)
                      ?? new System.Collections.Generic.List<System.Text.Json.JsonElement>();

                list.Add(item);
                HttpContext.Session.SetString(sessionKey,
                    System.Text.Json.JsonSerializer.Serialize(list));

                return Json(new { success = true, message = "Item added to cart" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to pending cart");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetPendingCartItems()
        {
            try
            {
                const string sessionKey = "PendingCartItems";
                var stored = HttpContext.Session.GetString(sessionKey);
                if (string.IsNullOrEmpty(stored))
                    return Json(new { success = true, items = new List<object>() });

                var items = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(stored)
                            ?? new List<System.Text.Json.JsonElement>();

                // Clear the queue so items are only loaded once
                HttpContext.Session.Remove(sessionKey);

                return Json(new { success = true, items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending cart items");
                return Json(new { success = false, items = new List<object>() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchItemsTab(string type, string keyword = null, string query = null, int? customerId = null)
        {
            try
            {
                var shopId = GetCurrentShopId();
                var searchTerm = keyword ?? query ?? "";
                var results = new List<object>();

                switch (type?.ToLower())
                {
                    case "inventory":
                        var inventoryRequest = new POSInventorySearchRequestDto
                        {
                            ShopId = shopId,
                            SearchTerm = searchTerm
                        };
                        var inventory = await _posApiService.GetInventoryAsync(inventoryRequest);
                        if (inventory != null)
                        {
                            results.AddRange(inventory.Select(i => new
                            {
                                id = $"inv_{i.Id}",
                                name = i.Name,
                                sku = i.Sku,
                                brand = i.Brand,
                                category = i.Category,
                                stock = i.Stock,
                                price = i.RetailPrice,
                                wholesalePrice = i.CostPrice,
                                status = i.Status,
                                type = "inventory"
                            }));
                        }
                        break;

                    case "repair":
                        var repairs = await _posApiService.SearchRepairsAsync(shopId, searchTerm);
                        if (repairs != null)
                        {
                            results.AddRange(repairs.Select(r => new
                            {
                                id = $"rep_{r.Id}",
                                name = r.Description,
                                repairNumber = r.RepairNumber,
                                customerName = r.CustomerName,
                                cost = r.Cost,
                                status = r.Status,
                                type = "repair"
                            }));
                        }
                        break;

                    case "device":
                        var deviceRequest = new POSDeviceSearchRequestDto
                        {
                            ShopId = shopId,
                            SearchTerm = searchTerm
                        };
                        var devices = await _posApiService.GetDevicesAsync(deviceRequest);
                        if (devices != null)
                        {
                            results.AddRange(devices.Select(d => new
                            {
                                id = d.Id,
                                name = $"{d.Brand} {d.Model}",
                                description = string.Join(" | ", new[]
                                {
                                    string.IsNullOrWhiteSpace(d.Imei) ? null : $"IMEI: {d.Imei}",
                                    string.IsNullOrWhiteSpace(d.Storage) ? null : d.Storage,
                                    string.IsNullOrWhiteSpace(d.Color) ? null : d.Color,
                                    string.IsNullOrWhiteSpace(d.Carrier) ? null : d.Carrier
                                }.Where(value => !string.IsNullOrWhiteSpace(value))),
                                brand = d.Brand,
                                category = d.Category,
                                imei = d.Imei,
                                serialNumber = d.SerialNumber,
                                price = d.SalePrice ?? 0,
                                wholesalePrice = d.PurchasePrice ?? 0,
                                status = d.Status,
                                type = "device"
                            }));
                        }
                        break;
                }

                return Json(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching items by tab");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetItemDetails(int id, string type)
        {
            try
            {
                // Item details would be fetched from the appropriate API service
                // For now, return a placeholder
                return Json(new { success = true, data = new { id, type } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item details");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLastInvoice()
        {
            try
            {
                var shopId = GetCurrentShopId();
                var lastSale = await _posApiService.GetLastSaleAsync(shopId);

                if (lastSale == null)
                {
                    return Json(new { success = false, message = "No invoices found" });
                }

                var result = new
                {
                    id = lastSale.Id,
                    invoiceNumber = lastSale.SaleNumber,
                    date = lastSale.SaleDate.ToUserTime(_timezoneService).ToString("yyyy-MM-dd HH:mm:ss"),
                    customerName = lastSale.CustomerName,
                    items = lastSale.Items.Select(i => new
                    {
                        name = i.ItemName,
                        quantity = i.Quantity,
                        unitPrice = i.UnitPrice,
                        total = i.TotalPrice
                    }),
                    payments = lastSale.Payments.Select(p => new
                    {
                        method = p.PaymentMethod,
                        amount = p.Amount,
                        reference = p.ReferenceNumber
                    }),
                    subtotal = lastSale.SubTotal,
                    tax = lastSale.TaxAmount,
                    discount = lastSale.DiscountAmount,
                    total = lastSale.TotalAmount,
                    paymentStatus = lastSale.PaymentStatus
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last invoice");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoiceData(int saleId)
        {
            try
            {
                var sale = await _posApiService.GetSaleAsync(saleId);

                if (sale == null)
                {
                    return Json(new { success = false, message = "Sale not found" });
                }

                // Return structure matching _InvoiceEditModal.cshtml expectations:
                // response.invoice.number, response.items, response.totals, response.payment, response.customer
                var invoiceData = new
                {
                    invoice = new
                    {
                        number = sale.SaleNumber ?? sale.InvoiceNumber ?? "",
                        date   = sale.SaleDate.ToUserTime(_timezoneService).ToString("yyyy-MM-dd HH:mm:ss"),
                        notes  = ""
                    },
                    customer = new
                    {
                        id    = sale.CustomerId,
                        name  = string.IsNullOrEmpty(sale.CustomerName) ? "Walk-in Customer" : sale.CustomerName,
                        email = "",
                        phone = "",
                        address = ""
                    },
                    shop = new { name = "", email = "", address = "", logo = "", website = "" },
                    items = sale.Items?.Select(i => new
                    {
                        id              = i.Id,
                        name            = i.ItemName ?? "",
                        description     = i.Description ?? "",
                        quantity        = i.Quantity,
                        unitPrice       = i.UnitPrice,
                        totalPrice      = i.TotalPrice,
                        costPrice       = i.CostPrice,
                        itemStatus      = i.ItemStatus,
                        inventoryItemId = i.InventoryItemId,
                        repairId        = i.RepairId,
                        isCustomItem    = i.IsCustomItem,
                        itemType        = i.RepairId.HasValue ? "Repair"
                            : i.InventoryItemId.HasValue ? "Inventory"
                            : i.IsCustomItem ? "Custom"
                            : "Device",
                        isTaxable       = true
                    }) ?? Enumerable.Empty<object>(),
                    payment = new
                    {
                        status  = sale.PaymentStatus ?? "Unpaid",
                        methods = sale.Payments?.Select(p => new
                        {
                            method    = p.PaymentMethod,
                            amount    = p.Amount,
                            reference = p.TransactionId ?? ""
                        }) ?? Enumerable.Empty<object>()
                    },
                    totals = new
                    {
                        subtotal    = sale.SubTotal,
                        tax         = sale.TaxAmount,
                        discount    = sale.DiscountAmount,
                        total       = sale.TotalAmount,
                        totalAmount = sale.TotalAmount
                    }
                };

                return Json(new { success = true, invoice = invoiceData.invoice, customer = invoiceData.customer, shop = invoiceData.shop, items = invoiceData.items, payment = invoiceData.payment, totals = invoiceData.totals });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoice data");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PrintReceipt(int saleId)
        {
            try
            {
                var sale = await _posApiService.GetSaleAsync(saleId);
                if (sale == null) return Json(new { success = false, message = "Sale not found" });

                var line = new StringBuilder();
                line.AppendLine($"Receipt: {sale.SaleNumber ?? sale.InvoiceNumber}");
                line.AppendLine($"Date: {sale.SaleDate.ToUserTime(_timezoneService):MM/dd/yyyy h:mm tt}");
                line.AppendLine($"Customer: {(string.IsNullOrWhiteSpace(sale.CustomerName) ? "Walk-in Customer" : sale.CustomerName)}");
                line.AppendLine("------------------------------------------");
                foreach (var item in sale.Items ?? [])
                {
                    line.AppendLine(item.ItemName ?? "Item");
                    line.AppendLine($"  {item.Quantity} x {item.UnitPrice:C2}     {item.TotalPrice:C2}");
                }
                line.AppendLine("------------------------------------------");
                line.AppendLine($"Subtotal:                    {sale.SubTotal:C2}");
                if (sale.TaxAmount != 0) line.AppendLine($"Tax:                         {sale.TaxAmount:C2}");
                if (sale.DiscountAmount != 0) line.AppendLine($"Discount:                   -{sale.DiscountAmount:C2}");
                line.AppendLine($"TOTAL:                       {sale.TotalAmount:C2}");
                var payments = sale.Payments?.Select(p => p.PaymentMethod).Where(p => !string.IsNullOrWhiteSpace(p));
                line.AppendLine($"Payment: {string.Join(", ", payments ?? [])}");

                var number = sale.SaleNumber ?? sale.InvoiceNumber ?? saleId.ToString();
                RawReceiptPrinter.Print(line.ToString(), number, logoPath: Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "empire-original-logo.png"));
                return Json(new { success = true, printer = RawReceiptPrinter.ReceiptPrinterName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing receipt {SaleId}", saleId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult PrintReceiptTest(int copies = 1)
        {
            try
            {
                var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                RawReceiptPrinter.Print("THERMAL PRINTER TEST\nPOS-80C connection successful\n80mm receipt route (not DYMO)\n", $"TEST-{stamp}", Math.Clamp(copies, 1, 3), Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "empire-original-logo.png"));
                return Json(new { success = true, printer = RawReceiptPrinter.ReceiptPrinterName, copies = Math.Clamp(copies, 1, 3) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing POS-80C test receipt");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult PrintCartReceipt([FromBody] PrintCartReceiptRequestDto request)
        {
            try
            {
                if (request.Items.Count == 0)
                    return Json(new { success = false, message = "Cart is empty" });

                var number = $"CART-{DateTime.Now:yyyyMMdd-HHmmss}";
                var line = new StringBuilder();
                line.AppendLine($"Receipt: {number}");
                line.AppendLine($"Date: {DateTime.Now:MM/dd/yyyy h:mm tt}");
                line.AppendLine($"Customer: {(string.IsNullOrWhiteSpace(request.CustomerName) ? "Walk-in Customer" : request.CustomerName)}");
                line.AppendLine("------------------------------------------");
                foreach (var item in request.Items)
                {
                    var quantity = Math.Max(1, item.Quantity);
                    line.AppendLine(item.Name ?? "Item");
                    line.AppendLine($"  {quantity} x {item.Price:C2}     {(quantity * item.Price):C2}");
                }
                line.AppendLine("------------------------------------------");
                line.AppendLine($"TOTAL:                       {request.Total:C2}");
                line.AppendLine("CURRENT CART / PAYMENT PENDING");

                RawReceiptPrinter.Print(line.ToString(), number, logoPath: Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "empire-original-logo.png"));
                return Json(new { success = true, printer = RawReceiptPrinter.ReceiptPrinterName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing current POS cart receipt");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchInventory(int? brandId = null, int? deviceCategoryId = null, 
            int? modelId = null, string status = null, string keyword = null)
        {
            try
            {
                var shopId = GetCurrentShopId();
                var request = new POSInventorySearchRequestDto
                {
                    ShopId = shopId,
                    SearchTerm = keyword,
                    BrandId = brandId,
                    CategoryId = deviceCategoryId,
                    Status = status
                };

                var inventory = await _posApiService.GetInventoryAsync(request);

                if (inventory == null)
                {
                    return Json(new { success = false, message = "Error loading inventory" });
                }

                var results = inventory.Select(i => new
                {
                    id = i.Id,
                    name = i.Name,
                    sku = i.Sku,
                    brand = i.Brand,
                    category = i.Category,
                    stock = i.Stock,
                    costPrice = i.CostPrice,
                    retailPrice = i.RetailPrice,
                    status = i.Status
                });

                return Json(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching inventory");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchDevices(int? brandId = null, int? deviceCategoryId = null, 
            int? modelId = null, string status = null, string keyword = null)
        {
            try
            {
                var shopId = GetCurrentShopId();
                var request = new POSDeviceSearchRequestDto
                {
                    ShopId = shopId,
                    SearchTerm = keyword,
                    BrandId = brandId,
                    CategoryId = deviceCategoryId,
                    ModelId = modelId,
                    Status = status
                };

                var devices = await _posApiService.GetDevicesAsync(request);

                if (devices == null)
                {
                    return Json(new { success = false, message = "Error loading devices" });
                }

                var results = devices.Select(d => new
                {
                    id           = d.Id,
                    name         = $"{d.BrandName} {d.ModelName}{(string.IsNullOrEmpty(d.Storage) ? "" : " " + d.Storage + "GB")}".Trim(),
                    brandName    = d.BrandName,
                    categoryName = d.CategoryName,
                    modelName    = d.ModelName,
                    imei         = d.Imei,
                    network      = d.Network,
                    storage      = d.Storage,
                    condition    = d.Condition,
                    scratches    = d.Scratches,
                    batteryHealth = d.BatteryHealth,
                    stock        = d.Stock,
                    buyingPrice  = d.BuyingPrice,
                    sellingPrice = d.SellingPrice,
                    isAvailable  = d.IsAvailable
                });

                return Json(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching devices");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchSaleByNumber(string saleNumber)
        {
            try
            {
                var shopId = GetCurrentShopId();
                var request = new POSSalesSearchRequestDto
                {
                    ShopId = shopId,
                    SearchTerm = saleNumber
                };

                var sales = await _posApiService.GetSalesAsync(request);

                if (sales == null || !sales.Any())
                {
                    return Json(new { success = false, message = "Sale not found" });
                }

                var sale = sales.First();
                var saleDetails = await _posApiService.GetSaleAsync(sale.Id);

                if (saleDetails == null)
                {
                    return Json(new { success = false, message = "Sale details not found" });
                }

                var result = new
                {
                    id = saleDetails.Id,
                    invoiceNumber = saleDetails.SaleNumber,
                    date = saleDetails.SaleDate,
                    customerName = saleDetails.CustomerName,
                    items = saleDetails.Items,
                    payments = saleDetails.Payments,
                    subtotal = saleDetails.SubTotal,
                    tax = saleDetails.TaxAmount,
                    discount = saleDetails.DiscountAmount,
                    total = saleDetails.TotalAmount,
                    paymentStatus = saleDetails.PaymentStatus
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching sale by number");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSale([FromBody] UpdateSaleRequestDto request)
        {
            try
            {
                request.Items ??= new List<UpdateSaleItemRequestDto>();
                request.Payments ??= new List<UpdatePaymentRequestDto>();
                if (request.Items.Count == 0)
                    return Json(new { success = false, message = "Sale must contain at least one item." });

                var subtotal = request.SubTotal ?? request.Items.Sum(i =>
                    i.TotalPrice ?? (i.UnitPrice * i.Quantity));
                var tax = request.TaxAmount ?? 0m;
                var discount = request.DiscountAmount ?? 0m;
                var total = request.TotalAmount ?? (subtotal + tax - discount);

                var posRequest = new POSUpdateSaleRequestDto
                {
                    CustomerId = request.CustomerId,
                    Items = request.Items.Select(i => new POSSaleItemRequestDto
                    {
                        ItemType        = !string.IsNullOrWhiteSpace(i.ItemType)
                            ? i.ItemType
                            : i.RepairId.HasValue ? "Repair"
                            : i.InventoryItemId.HasValue ? "Inventory"
                            : i.IsCustomItem ? "Custom"
                            : "Device",
                        ItemReferenceId = i.ItemReferenceId,
                        InventoryItemId = i.InventoryItemId,
                        RepairId        = i.RepairId,
                        ItemName        = i.ItemName,
                        Description     = i.Description,
                        Quantity        = i.Quantity,
                        UnitPrice       = i.UnitPrice,
                        TotalPrice      = i.TotalPrice ?? (i.UnitPrice * i.Quantity),
                        DiscountAmount  = i.DiscountAmount ?? 0m,
                        Cost            = i.CostPrice,
                        IsCustomItem    = i.IsCustomItem,
                        IsTaxable       = i.IsTaxable,
                        ItemStatus      = string.IsNullOrWhiteSpace(i.ItemStatus) ? "In Stock" : i.ItemStatus
                    }).ToList(),
                    Payments = request.Payments.Select(p => new POSPaymentRequestDto
                    {
                        PaymentMethod = p.PaymentMethod,
                        Amount = p.Amount,
                        // The API contract calls this TransactionId. Preserve the
                        // existing reference while allowing a payment-method-only edit.
                        TransactionId = p.TransactionId ?? p.ReferenceNumber,
                        ReferenceNumber = p.ReferenceNumber
                    }).ToList(),
                    SubTotal = subtotal,
                    TaxAmount = tax,
                    DiscountAmount = discount,
                    TotalAmount = total,
                    PaymentStatus = request.PaymentStatus
                    ,ChangeReason = request.ChangeReason
                };

                var result = await _posApiService.UpdateSaleAsync(request.SaleId, posRequest);

                if (result == null || !result.Success)
                {
                    return Json(new { success = false, message = result?.Message ?? "Error updating sale" });
                }

                return Json(new
                {
                    success = true,
                    saleId = result.SaleId,
                    invoiceNumber = result.SaleNumber,
                    message = "Sale updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sale");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSaleHistory(int saleId)
        {
            var history = await _posApiService.GetSaleHistoryAsync(saleId);
            return Json(new { success = history != null, data = history ?? new List<SaleHistoryDto>() });
        }

        [HttpGet]
        public async Task<IActionResult> GetSaleReturns(int saleId)
        {
            var returns = await _posApiService.GetSaleReturnsAsync(saleId);
            return Json(new { success = returns != null, data = returns ?? new List<SaleReturnHistoryDto>() });
        }

        [HttpPost]
        public async Task<IActionResult> ProcessReturn(int saleId, [FromBody] ProcessSaleReturnDto request)
        {
            var result = await _posApiService.ProcessReturnAsync(saleId, request);
            return Json(new { success = result != null, data = result, message = result == null ? "Return could not be processed" : "Return processed successfully" });
        }
    }
}
