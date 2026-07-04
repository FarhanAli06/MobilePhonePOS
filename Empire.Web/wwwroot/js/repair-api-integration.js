// Repair Page API Integration
// This file replaces all hardcoded arrays with API calls to load data from the database

// Global data storage
let brands = [];
let deviceCategories = [];
let deviceModels = [];
let parts = [];
let repairStatuses = [];
let paymentStatuses = [];
let paymentMethods = [];

// API Base URL
const API_BASE_URL = '/api';

// Load Brands from API
async function loadBrands() {
    try {
        const response = await fetch(`${API_BASE_URL}/brands`);
        if (!response.ok) throw new Error('Failed to load brands');
        brands = await response.json();
        console.log('Brands loaded:', brands.length);
        return brands;
    } catch (error) {
        console.error('Error loading brands:', error);
        return [];
    }
}

// Load Device Categories from API
async function loadDeviceCategories() {
    try {
        const response = await fetch(`${API_BASE_URL}/devicecategories`);
        if (!response.ok) throw new Error('Failed to load device categories');
        deviceCategories = await response.json();
        console.log('Device categories loaded:', deviceCategories.length);
        return deviceCategories;
    } catch (error) {
        console.error('Error loading device categories:', error);
        return [];
    }
}

// Load Device Models by Brand and Category
async function loadDeviceModels(brandId, categoryId) {
    try {
        const response = await fetch(`${API_BASE_URL}/devicemodels/by-brand-category?brandId=${brandId}&categoryId=${categoryId}`);
        if (!response.ok) throw new Error('Failed to load device models');
        deviceModels = await response.json();
        console.log('Device models loaded:', deviceModels.length);
        return deviceModels;
    } catch (error) {
        console.error('Error loading device models:', error);
        return [];
    }
}

// Load Parts from Inventory (filtered by brand, category, model)
async function loadParts(brandId, categoryId, modelId) {
    try {
        let url = `${API_BASE_URL}/inventory/parts?`;
        if (brandId) url += `brandId=${brandId}&`;
        if (categoryId) url += `categoryId=${categoryId}&`;
        if (modelId) url += `modelId=${modelId}`;
        
        const response = await fetch(url);
        if (!response.ok) throw new Error('Failed to load parts');
        parts = await response.json();
        console.log('Parts loaded:', parts.length);
        return parts;
    } catch (error) {
        console.error('Error loading parts:', error);
        return [];
    }
}

// Load Repair Statuses from Lookups
async function loadRepairStatuses() {
    try {
        const response = await fetch(`${API_BASE_URL}/lookups/category/RepairStatus`);
        if (!response.ok) throw new Error('Failed to load repair statuses');
        repairStatuses = await response.json();
        console.log('Repair statuses loaded:', repairStatuses.length);
        return repairStatuses;
    } catch (error) {
        console.error('Error loading repair statuses:', error);
        return [];
    }
}

// Load Payment Statuses from Lookups
async function loadPaymentStatuses() {
    try {
        const response = await fetch(`${API_BASE_URL}/lookups/category/PaymentStatus`);
        if (!response.ok) throw new Error('Failed to load payment statuses');
        paymentStatuses = await response.json();
        console.log('Payment statuses loaded:', paymentStatuses.length);
        return paymentStatuses;
    } catch (error) {
        console.error('Error loading payment statuses:', error);
        return [];
    }
}

// Load Payment Methods from Lookups
async function loadPaymentMethods() {
    try {
        const response = await fetch(`${API_BASE_URL}/lookups/category/PaymentMethod`);
        if (!response.ok) throw new Error('Failed to load payment methods');
        paymentMethods = await response.json();
        console.log('Payment methods loaded:', paymentMethods.length);
        return paymentMethods;
    } catch (error) {
        console.error('Error loading payment methods:', error);
        return [];
    }
}

// Initialize all data on page load
async function initializeRepairPageData() {
    console.log('Initializing repair page data from API...');
    
    try {
        // Load all data in parallel
        await Promise.all([
            loadBrands(),
            loadDeviceCategories(),
            loadRepairStatuses(),
            loadPaymentStatuses(),
            loadPaymentMethods()
        ]);
        
        console.log('All data loaded successfully!');
        
        // Render initial UI
        renderBrands();
        renderRepairStatuses();
        renderPaymentStatuses();
        renderPaymentMethods();
        
        return true;
    } catch (error) {
        console.error('Error initializing repair page data:', error);
        return false;
    }
}

// Render Brands
function renderBrands() {
    const brandGrid = document.getElementById('brandGrid');
    if (!brandGrid) return;
    
    brandGrid.innerHTML = '';
    
    brands.forEach(brand => {
        const brandCard = document.createElement('div');
        brandCard.className = 'brand-card';
        brandCard.setAttribute('data-brand-id', brand.id);
        brandCard.style.borderColor = brand.color || '#ccc';
        brandCard.onclick = () => selectBrand(brand.id);
        
        brandCard.innerHTML = `
            <i class="${brand.icon || 'fas fa-mobile-alt'}" style="color: ${brand.color || '#000'}"></i>
            <span>${brand.name}</span>
        `;
        
        brandGrid.appendChild(brandCard);
    });
}

// Render Device Categories (after brand selection)
function renderDeviceCategories(brandId) {
    const categoryGrid = document.getElementById('categoryGrid');
    if (!categoryGrid) return;
    
    categoryGrid.innerHTML = '';
    
    // Filter categories by brand if needed
    const filteredCategories = deviceCategories; // Can add filtering logic here
    
    filteredCategories.forEach(category => {
        const categoryCard = document.createElement('div');
        categoryCard.className = 'category-card';
        categoryCard.setAttribute('data-category-id', category.id);
        categoryCard.style.borderColor = category.color || '#ccc';
        categoryCard.onclick = () => selectCategory(category.id, brandId);
        
        categoryCard.innerHTML = `
            <i class="material-icons" style="color: ${category.color || '#000'}">${category.icon || 'devices'}</i>
            <span>${category.name}</span>
        `;
        
        categoryGrid.appendChild(categoryCard);
    });
}

// Render Device Models (after category selection)
async function renderDeviceModels(brandId, categoryId) {
    const modelGrid = document.getElementById('modelGrid');
    if (!modelGrid) return;
    
    modelGrid.innerHTML = '<div class="loading">Loading models...</div>';
    
    // Load models for selected brand and category
    await loadDeviceModels(brandId, categoryId);
    
    modelGrid.innerHTML = '';
    
    deviceModels.forEach(model => {
        const modelCard = document.createElement('div');
        modelCard.className = 'model-card';
        modelCard.setAttribute('data-model-id', model.id);
        modelCard.style.borderColor = model.color || '#ccc';
        modelCard.onclick = () => selectModel(model.id, brandId, categoryId);
        
        modelCard.innerHTML = `
            <i class="material-icons" style="color: ${model.color || '#000'}">${model.icon || 'phone_iphone'}</i>
            <div>
                <strong>${model.name}</strong>
                ${model.year ? `<small>${model.year}</small>` : ''}
            </div>
        `;
        
        modelGrid.appendChild(modelCard);
    });
}

// Render Parts (after model selection)
async function renderParts(brandId, categoryId, modelId) {
    const partsGrid = document.getElementById('partsGrid');
    if (!partsGrid) return;
    
    partsGrid.innerHTML = '<div class="loading">Loading parts...</div>';
    
    // Load parts for selected brand, category, and model
    await loadParts(brandId, categoryId, modelId);
    
    partsGrid.innerHTML = '';
    
    parts.forEach(part => {
        const partCard = document.createElement('div');
        partCard.className = 'part-card';
        partCard.setAttribute('data-part-id', part.id);
        
        // Stock indicator
        let stockClass = 'stock-out';
        let stockText = 'Out of Stock';
        if (part.quantity > 10) {
            stockClass = 'stock-in';
            stockText = 'In Stock';
        } else if (part.quantity > 0) {
            stockClass = 'stock-low';
            stockText = 'Low Stock';
        }
        
        partCard.onclick = () => togglePartSelection(part.id);
        
        partCard.innerHTML = `
            <div class="part-info">
                <strong>${part.name}</strong>
                <span class="part-price">$${part.price.toFixed(2)}</span>
            </div>
            <span class="stock-badge ${stockClass}">${stockText} (${part.quantity})</span>
        `;
        
        partsGrid.appendChild(partCard);
    });
}

// Render Repair Statuses
function renderRepairStatuses() {
    const statusGrid = document.getElementById('repairStatusGrid');
    if (!statusGrid) return;
    
    statusGrid.innerHTML = '';
    
    repairStatuses.forEach(status => {
        const statusCard = document.createElement('div');
        statusCard.className = 'status-card';
        statusCard.setAttribute('data-status', status.value);
        statusCard.style.borderColor = status.color || status.colorCode || '#ccc';
        statusCard.onclick = () => selectRepairStatus(status.value);
        
        statusCard.innerHTML = `
            <i class="material-icons" style="color: ${status.color || status.colorCode || '#000'}">${status.icon || 'info'}</i>
            <span>${status.value}</span>
        `;
        
        statusGrid.appendChild(statusCard);
    });
}

// Render Payment Statuses
function renderPaymentStatuses() {
    const paymentStatusGrid = document.getElementById('paymentStatusGrid');
    if (!paymentStatusGrid) return;
    
    paymentStatusGrid.innerHTML = '';
    
    paymentStatuses.forEach(status => {
        const statusCard = document.createElement('div');
        statusCard.className = 'payment-status-card';
        statusCard.setAttribute('data-status', status.value);
        statusCard.style.borderColor = status.color || status.colorCode || '#ccc';
        statusCard.onclick = () => selectPaymentStatus(status.value);
        
        statusCard.innerHTML = `
            <i class="material-icons" style="color: ${status.color || status.colorCode || '#000'}">${status.icon || 'payment'}</i>
            <span>${status.value}</span>
        `;
        
        paymentStatusGrid.appendChild(statusCard);
    });
}

// Render Payment Methods
function renderPaymentMethods() {
    const methodsGrid = document.getElementById('paymentMethodsGrid');
    if (!methodsGrid) return;
    
    methodsGrid.innerHTML = '';
    
    paymentMethods.forEach(method => {
        const methodCard = document.createElement('div');
        methodCard.className = 'payment-method-card';
        methodCard.setAttribute('data-method-id', method.id);
        methodCard.onclick = () => selectPaymentMethod(method.id);
        
        methodCard.innerHTML = `
            <i class="material-icons">${method.icon || 'payment'}</i>
            <span>${method.value}</span>
        `;
        
        methodsGrid.appendChild(methodCard);
    });
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    console.log('Repair page loaded, initializing API data...');
    initializeRepairPageData();
});
