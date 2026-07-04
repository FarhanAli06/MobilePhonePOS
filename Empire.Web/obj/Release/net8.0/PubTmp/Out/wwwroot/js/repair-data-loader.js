// repair-data-loader.js
// This file replaces hardcoded arrays in Create.cshtml with API calls through RepairsController

// Global variables to store data
let deviceCategories = [];
let brands = [];
let paymentMethods = [];
let phoneModels = {};
let partsInventory = {};
let repairStatuses = [];
let paymentStatuses = [];

// Promise that resolves once all initial data is loaded
let _readyResolve;
const _readyPromise = new Promise(resolve => { _readyResolve = resolve; });

// Load device categories from RepairsController
async function loadDeviceCategories() {
    try {
        const response = await fetch('/Repairs/GetDeviceCategories');
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        const result = await response.json();
        
        if (result.success && result.data) {
            deviceCategories = result.data.map(cat => ({
                id: cat.id,
                name: cat.name,
                icon: cat.icon || 'devices_other',
                color: cat.color || '#757575'
            }));
            console.log('✅ Loaded device categories:', deviceCategories.length);
        } else {
            console.error('❌ Failed to load device categories:', result.message);
            deviceCategories = [];
        }
        return deviceCategories;
    } catch (error) {
        console.error('❌ Error loading device categories:', error);
        deviceCategories = [];
        return deviceCategories;
    }
}

// Load brands from RepairsController
async function loadBrands() {
    try {
        const response = await fetch('/Repairs/GetBrands');
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        const result = await response.json();
        
        if (result.success && result.data) {
            brands = result.data.map(brand => ({
                id: brand.id,
                name: brand.name,
                icon: brand.icon || 'fas fa-mobile-alt',
                color: brand.color || '#000000'
            }));
            console.log('✅ Loaded brands:', brands.length);
        } else {
            console.error('❌ Failed to load brands:', result.message);
            brands = [];
        }
        return brands;
    } catch (error) {
        console.error('❌ Error loading brands:', error);
        brands = [];
        return brands;
    }
}

// Load device models from RepairsController
async function loadDeviceModels(brandId, categoryId) {
    try {
        let url = '/Repairs/GetDeviceModels?';
        if (brandId) url += `brandId=${brandId}&`;
        if (categoryId) url += `categoryId=${categoryId}`;
        
        const response = await fetch(url);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        const result = await response.json();
        
        if (result.success && result.data) {
            const models = result.data.map(model => ({
                id: model.id,
                name: model.name,
                icon: model.icon || '📱',
                color: model.color || '#757575'
            }));
            
            // Store in phoneModels object
            const brandKey = brandId || 'all';
            if (!phoneModels[brandKey]) {
                phoneModels[brandKey] = {};
            }
            const categoryKey = categoryId || 'all';
            phoneModels[brandKey][categoryKey] = models;
            
            console.log('✅ Loaded device models:', models.length, 'for brand', brandId, 'category', categoryId);
            return models;
        } else {
            console.error('❌ Failed to load device models:', result.message);
            return [];
        }
    } catch (error) {
        console.error('❌ Error loading device models:', error);
        return [];
    }
}

// Load all device models (for initial load)
async function loadAllDeviceModels() {
    try {
        const response = await fetch('/Repairs/GetDeviceModels');
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        const result = await response.json();
        
        if (result.success && result.data) {
            // Group models by brand ID
            phoneModels = {};
            result.data.forEach(model => {
                const brandId = model.id;
                if (!phoneModels[brandId]) {
                    phoneModels[brandId] = [];
                }
                phoneModels[brandId].push({
                    id: model.id,
                    name: model.name,
                    icon: model.icon || '📱',
                    color: model.color || '#757575'
                });
            });
            
            console.log('✅ Loaded all device models for brands:', Object.keys(phoneModels).length);
            return phoneModels;
        } else {
            console.error('❌ Failed to load device models:', result.message);
            phoneModels = {};
            return phoneModels;
        }
    } catch (error) {
        console.error('❌ Error loading device models:', error);
        phoneModels = {};
        return phoneModels;
    }
}

// Load parts inventory from RepairsController
async function loadInventoryParts(brandId, categoryId, modelId) {
    try {
        let url = '/Repairs/GetInventoryParts?';
        if (brandId) url += `brandId=${brandId}&`;
        if (categoryId) url += `categoryId=${categoryId}&`;
        if (modelId) url += `modelId=${modelId}`;
        
        const response = await fetch(url);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        const result = await response.json();
        
        if (result.success && result.data) {
            const parts = result.data.map(part => ({
                id: part.id,
                name: part.name,
                itemType: part.itemType,
                brand: part.brand,
                category: part.category,
                model: part.model,
                sku: part.sku,
                // stock quantity — server returns as 'stock'
                quantity: part.stock != null ? part.stock : (part.quantity || 0),
                stock: part.stock != null ? part.stock : (part.quantity || 0),
                // pricing
                costPrice: parseFloat(part.costPrice || 0),
                retailPrice: parseFloat(part.retailPrice || 0),
                wholesalePrice: parseFloat(part.wholesalePrice || 0),
                price: parseFloat(part.price || 0),
                inStock: part.inStock,
                notes: part.notes,
                description: part.description
            }));
            
            // Store in partsInventory object
            const brandKey = brandId || 'all';
            if (!partsInventory[brandKey]) {
                partsInventory[brandKey] = {};
            }
            const categoryKey = categoryId || 'all';
            if (!partsInventory[brandKey][categoryKey]) {
                partsInventory[brandKey][categoryKey] = {};
            }
            const modelKey = modelId || 'all';
            partsInventory[brandKey][categoryKey][modelKey] = parts;
            
            console.log('✅ Loaded inventory parts:', parts.length);
            return parts;
        } else {
            console.error('❌ Failed to load inventory parts:', result.message);
            return [];
        }
    } catch (error) {
        console.error('❌ Error loading inventory parts:', error);
        return [];
    }
}

// Load payment methods from RepairsController
async function loadPaymentMethods() {
    try {
        const response = await fetch('/Repairs/GetPaymentMethods');
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        const result = await response.json();
        
        if (result.success && result.data) {
            paymentMethods = result.data.map(method => ({
                id: method.id,
                name: method.name,
                icon: method.icon || '💰',
                color: method.color || '#757575',
                description: method.name
            }));
            console.log('✅ Loaded payment methods:', paymentMethods.length);
        } else {
            console.error('❌ Failed to load payment methods:', result.message);
            paymentMethods = [];
        }
        return paymentMethods;
    } catch (error) {
        console.error('❌ Error loading payment methods:', error);
        paymentMethods = [];
        return paymentMethods;
    }
}

// Load repair statuses from RepairsController
async function loadRepairStatuses() {
    try {
        const response = await fetch('/Repairs/GetRepairStatuses');
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        const result = await response.json();
        
        if (result.success && result.data) {
            repairStatuses = result.data.map(status => ({
                id: status.id,
                name: status.name,
                icon: status.icon || 'schedule',
                color: status.color || '#FF9800',
                description: status.name
            }));
            console.log('✅ Loaded repair statuses:', repairStatuses.length);
        } else {
            console.error('❌ Failed to load repair statuses:', result.message);
            repairStatuses = [];
        }
        return repairStatuses;
    } catch (error) {
        console.error('❌ Error loading repair statuses:', error);
        repairStatuses = [];
        return repairStatuses;
    }
}

// Load payment statuses from RepairsController
async function loadPaymentStatuses() {
    try {
        const response = await fetch('/Repairs/GetPaymentStatuses');
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        const result = await response.json();
        
        if (result.success && result.data) {
            paymentStatuses = result.data.map(status => ({
                id: status.id,
                name: status.name,
                icon: status.icon || 'payments',
                color: status.color || '#757575',
                description: status.name
            }));
            console.log('✅ Loaded payment statuses:', paymentStatuses.length);
        } else {
            console.error('❌ Failed to load payment statuses:', result.message);
            paymentStatuses = [];
        }
        return paymentStatuses;
    } catch (error) {
        console.error('❌ Error loading payment statuses:', error);
        paymentStatuses = [];
        return paymentStatuses;
    }
}

// Initialize all data on page load
async function initializeRepairPageData() {
    console.log('🔄 Loading all data from database through RepairsController...');
    
    try {
        await Promise.all([
            loadDeviceCategories(),
            loadBrands(),
            loadPaymentMethods(),
            loadRepairStatuses(),
            loadPaymentStatuses()
        ]);
        
        console.log('✅ All data loaded successfully from database!');
        console.log('📊 Summary:');
        console.log('  - Device Categories:', deviceCategories.length);
        console.log('  - Brands:', brands.length);
        console.log('  - Payment Methods:', paymentMethods.length);
        console.log('  - Repair Statuses:', repairStatuses.length);
        console.log('  - Payment Statuses:', paymentStatuses.length);
        
        // Resolve the ready promise so waiting pages know data is available
        _readyResolve();

        // Trigger any UI updates if needed
        if (typeof renderDeviceCategories === 'function') renderDeviceCategories();
        if (typeof renderBrands === 'function') renderBrands();
        if (typeof renderPaymentMethods === 'function') renderPaymentMethods();
        
    } catch (error) {
        console.error('❌ Error initializing repair page data:', error);
        // Resolve anyway so pages don't hang forever
        _readyResolve();
    }
}

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeRepairPageData);
} else {
    // DOM is already ready
    initializeRepairPageData();
}

// Export for use in other scripts.
// Use Object.defineProperty with getters so consumers always read the
// current value of the module-level variables (not a stale copy captured
// at script-load time before the async fetches have completed).
window.repairData = {
    get deviceCategories() { return deviceCategories; },
    get brands()           { return brands; },
    get phoneModels()      { return phoneModels; },
    get partsInventory()   { return partsInventory; },
    get paymentMethods()   { return paymentMethods; },
    get repairStatuses()   { return repairStatuses; },
    get paymentStatuses()  { return paymentStatuses; },
    loadDeviceModels,
    loadInventoryParts,
    reload: initializeRepairPageData,
    // Promise that resolves once the initial data load is complete.
    // Usage: await window.repairData.ready
    ready: _readyPromise
};
