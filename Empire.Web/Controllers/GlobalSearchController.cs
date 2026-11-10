using Empire.Web.Authorization;
using Empire.Web.Services;
using Empire.Web.Services.Customer;
using Empire.Web.Services.Device;
using Empire.Web.Services.InventoryManagement;
using Empire.Web.Services.Repair;
using Microsoft.AspNetCore.Mvc;

namespace Empire.Web.Controllers;

[SessionAuthorizeWithShop]
public class GlobalSearchController : BaseController
{
    private readonly ICustomerApiService _customers;
    private readonly IRepairApiService _repairs;
    private readonly IDeviceApiService _devices;
    private readonly IInventoryManagementApiService _inventory;

    public GlobalSearchController(ILogger<GlobalSearchController> logger, ITimezoneService timezoneService,
        ICustomerApiService customers, IRepairApiService repairs, IDeviceApiService devices,
        IInventoryManagementApiService inventory) : base(logger, timezoneService)
    {
        _customers = customers;
        _repairs = repairs;
        _devices = devices;
        _inventory = inventory;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string term)
    {
        term = (term ?? string.Empty).Trim();
        if (term.Length < 2) return Json(new { success = true, data = Array.Empty<object>() });
        var shopId = GetCurrentShopId();
        var customerTask = _customers.SearchAsync(term, shopId);
        var repairTask = _repairs.SearchAsync(term);
        var deviceTask = _devices.GetByShopAsync(shopId);
        var inventoryTask = _inventory.GetInventoryItemsAsync(shopId, search: term);
        await Task.WhenAll(customerTask, repairTask, deviceTask, inventoryTask);

        bool Match(string? value) => !string.IsNullOrWhiteSpace(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);
        var results = new List<object>();
        results.AddRange((await customerTask).Take(6).Select(x => new { type = "Customer", title = $"{x.FirstName} {x.LastName}".Trim(), detail = x.PhoneNumber ?? x.Phone ?? x.Email, url = $"/Customers/Edit/{x.Id}" }));
        results.AddRange((await repairTask).Where(x => x.ShopId == null || x.ShopId == shopId).Take(6).Select(x => new { type = "Repair", title = x.CustomerName, detail = $"{x.DeviceBrand} {x.DeviceModel} · {x.RepairNumber}", url = $"/Repairs/Edit/{x.Id}" }));
        results.AddRange((await deviceTask).Where(x => Match(x.Brand) || Match(x.Model) || Match(x.ModelNumber) || Match(x.IMEISerialNumber)).Take(6).Select(x => new { type = "Device", title = x.DisplayName, detail = x.IMEISerialNumber, url = $"/Devices/Edit/{x.Id}" }));
        results.AddRange((await inventoryTask).Take(6).Select(x => new { type = "Inventory", title = x.Name, detail = $"{x.BrandName} {x.DeviceModelName} · {x.SKU}", url = $"/Inventories?search={Uri.EscapeDataString(term)}" }));
        return Json(new { success = true, data = results.Take(20) });
    }
}
