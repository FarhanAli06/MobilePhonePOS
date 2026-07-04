using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

public class Shop : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? Address { get; set; }
    
    [MaxLength(50)]
    public string? City { get; set; }
    
    [MaxLength(50)]
    public string? State { get; set; }
    
    [MaxLength(10)]
    public string? ZipCode { get; set; }
    
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [MaxLength(100)]
    public string? Email { get; set; }
    
    [MaxLength(500)]
    public string? LogoPath { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<UserShopRole> UserShopRoles { get; set; } = new List<UserShopRole>();
    public virtual ICollection<Repair> Repairs { get; set; } = new List<Repair>();
    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public virtual ICollection<Brand> Brands { get; set; } = new List<Brand>();
    public virtual ICollection<DeviceCategory> DeviceCategories { get; set; } = new List<DeviceCategory>();
    public virtual ICollection<DeviceModel> DeviceModels { get; set; } = new List<DeviceModel>();
    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
    public virtual ICollection<InventoryCategory> InventoryCategories { get; set; } = new List<InventoryCategory>();
    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
    // LookupValues are NOT navigated from Shop because ShopId = 0 (global) is allowed.
    // Use LookupValue.ShopId directly for filtering.
    public virtual ICollection<UserPagePermission> UserPagePermissions { get; set; } = new List<UserPagePermission>();
}

