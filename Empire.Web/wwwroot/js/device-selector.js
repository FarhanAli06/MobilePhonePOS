/**
 * device-selector.js
 * ─────────────────────────────────────────────────────────────────────────────
 * Generic cascading Brand → Category → Model (→ Parts) selector.
 * Exposed as window.EmpireDeviceSelector (singleton).
 *
 * ── Quick-start (JS-only, no Razor partial needed) ───────────────────────────
 *
 *   EmpireDeviceSelector.create('containerId', {
 *       selectorId:      'mySelector',
 *       showParts:       false,
 *       showIssueInput:  false,
 *       hiddenBrand:     'hiddenBrandInputId',
 *       hiddenCategory:  'hiddenCategoryInputId',
 *       hiddenModel:     'hiddenModelInputId',
 *       onModelSelected: function(detail) { ... },
 *       onPartToggled:   function(detail) { ... },
 *   });
 *
 * ── Razor partial usage ───────────────────────────────────────────────────────
 *
 *   @await Html.PartialAsync("_DeviceSelector", new ViewDataDictionary(ViewData) {
 *       { "SelectorId", "mySelector" }, { "ShowParts", false }
 *   })
 *   <script>EmpireDeviceSelector.init('mySelector');</script>
 *
 * ─────────────────────────────────────────────────────────────────────────────
 */

(function (global) {
    'use strict';

    // ── Internal state per selector instance ─────────────────────────────────
    var _states = {};
    var _cache  = {
        brands:     null,
        categories: null,
        models:     {}
    };

    // ── Helpers ───────────────────────────────────────────────────────────────
    function _el(id) { return document.getElementById(id); }
    function _escapeHtml(value) {
        return String(value || '').replace(/[&<>"']/g, function(character) {
            return ({ '&':'&amp;', '<':'&lt;', '>':'&gt;', '"':'&quot;', "'":'&#39;' })[character];
        });
    }

    function _brandMarkup(sid, name) {
        return global.EmpireManufacturerLogos ? global.EmpireManufacturerLogos.markup(name) : _escapeHtml(name);
    }

    function _partIcon(name) {
        var n = (name || '').toLowerCase();
        if (n.includes('screen') || n.includes('display') || n.includes('lcd')) return '📱';
        if (n.includes('battery'))                                               return '🔋';
        if (n.includes('camera'))                                                return '📷';
        if (n.includes('charging') || n.includes('port') || n.includes('usb'))  return '🔌';
        if (n.includes('speaker') || n.includes('audio'))                       return '🔊';
        if (n.includes('back') || n.includes('glass') || n.includes('cover'))   return '🪟';
        if (n.includes('button') || n.includes('power') || n.includes('volume'))return '🔘';
        if (n.includes('motherboard') || n.includes('board'))                   return '🖥️';
        return '🔧';
    }

    function _repairLabel(name) {
        var n = (name || '').toLowerCase();
        if (n.includes('screen') || n.includes('display') || n.includes('lcd')) return 'Screen Replacement';
        if (n.includes('battery')) return 'Battery Replacement';
        if (n.includes('camera')) return 'Camera Repair';
        if (n.includes('charging') || n.includes('port') || n.includes('usb')) return 'Charging Port Repair';
        if (n.includes('home') && n.includes('button')) return 'Home Button Repair';
        if (n.includes('back') || n.includes('glass') || n.includes('cover')) return 'Back Glass Repair';
        if (n.includes('speaker') || n.includes('audio')) return 'Speaker Repair';
        return name || 'Repair';
    }

    function _screenQuality(part) {
        var explicit = String(part.variant || '').trim();
        if (explicit && !/^unspecified/i.test(explicit)) return explicit;
        var text = [part.name || '', part.description || '', part.notes || '', part.sku || ''].join(' ').toLowerCase();
        if (/soft\s*oled/.test(text)) return 'Soft OLED';
        if (/hard\s*oled/.test(text)) return 'Hard OLED';
        if (/incell|in-cell/.test(text)) return 'Incell';
        if (/original\s*\/\s*refurb|original.*refurb|refurb.*original/.test(text)) return 'Original/Refurbished';
        if (/\boem\b/.test(text)) return 'OEM';
        if (/\boriginal\b/.test(text)) return 'Original';
        return 'Unspecified quality';
    }

    var _iphoneBackGlassColors = {
        'iphone 8':['Silver','Space Gray','Gold','(PRODUCT)RED'], 'iphone 8 plus':['Silver','Space Gray','Gold','(PRODUCT)RED'],
        'iphone x':['Silver','Space Gray'], 'iphone xr':['Black','White','Blue','Yellow','Coral','(PRODUCT)RED'],
        'iphone xs':['Silver','Space Gray','Gold'], 'iphone xs max':['Silver','Space Gray','Gold'],
        'iphone 11':['Black','Green','Yellow','Purple','White','(PRODUCT)RED'], 'iphone 11 pro':['Gold','Space Gray','Silver','Midnight Green'], 'iphone 11 pro max':['Gold','Space Gray','Silver','Midnight Green'],
        'iphone se 2':['Black','White','(PRODUCT)RED'], 'iphone se (2nd generation)':['Black','White','(PRODUCT)RED'],
        'iphone 12':['Black','White','(PRODUCT)RED','Green','Blue','Purple'], 'iphone 12 mini':['Black','White','(PRODUCT)RED','Green','Blue','Purple'], 'iphone 12 pro':['Silver','Graphite','Gold','Pacific Blue'], 'iphone 12 pro max':['Silver','Graphite','Gold','Pacific Blue'],
        'iphone 13':['Pink','Blue','Midnight','Starlight','(PRODUCT)RED','Green'], 'iphone 13 mini':['Pink','Blue','Midnight','Starlight','(PRODUCT)RED','Green'], 'iphone 13 pro':['Sierra Blue','Silver','Gold','Graphite','Alpine Green'], 'iphone 13 pro max':['Sierra Blue','Silver','Gold','Graphite','Alpine Green'],
        'iphone se 3':['Midnight','Starlight','(PRODUCT)RED'], 'iphone se (3rd generation)':['Midnight','Starlight','(PRODUCT)RED'],
        'iphone 14':['Blue','Purple','Midnight','Starlight','(PRODUCT)RED','Yellow'], 'iphone 14 plus':['Blue','Purple','Midnight','Starlight','(PRODUCT)RED','Yellow'], 'iphone 14 pro':['Space Black','Silver','Gold','Deep Purple'], 'iphone 14 pro max':['Space Black','Silver','Gold','Deep Purple'],
        'iphone 15':['Black','Blue','Green','Yellow','Pink'], 'iphone 15 plus':['Black','Blue','Green','Yellow','Pink'], 'iphone 15 pro':['Black Titanium','White Titanium','Blue Titanium','Natural Titanium'], 'iphone 15 pro max':['Black Titanium','White Titanium','Blue Titanium','Natural Titanium'],
        'iphone 16':['Black','White','Pink','Teal','Ultramarine'], 'iphone 16 plus':['Black','White','Pink','Teal','Ultramarine'], 'iphone 16 pro':['Black Titanium','White Titanium','Natural Titanium','Desert Titanium'], 'iphone 16 pro max':['Black Titanium','White Titanium','Natural Titanium','Desert Titanium'], 'iphone 16e':['Black','White'],
        'iphone 17':['Black','White','Mist Blue','Sage','Lavender'], 'iphone 17 pro':['Silver','Cosmic Orange','Deep Blue'], 'iphone 17 pro max':['Silver','Cosmic Orange','Deep Blue'], 'iphone 17e':['Black','White','Soft Pink'], 'iphone air':['Space Black','Cloud White','Light Gold','Sky Blue'],
        'iphone 18 pro':['Black','Silver','Glacier','Burgundy'], 'iphone 18 pro max':['Black','Silver','Glacier','Burgundy']
    };

    function _officialColorsFor(modelName) {
        var key = String(modelName || '').toLowerCase().replace(/\s+/g, ' ').trim();
        return _iphoneBackGlassColors[key] || [];
    }

    function _partColor(part, modelName) {
        var explicit = String(part.color || part.variant || '').trim();
        if (explicit) return explicit;
        var name = String(part.name || '');
        return _officialColorsFor(modelName).find(function(color) {
            return name.toLowerCase().includes(color.toLowerCase());
        }) || '';
    }

    function _colorSwatch(color) {
        var map={'black':'#202124','white':'#f7f7f5','silver':'#d7d8d4','space gray':'#66676b','gold':'#d4b98c','(product)red':'#b7192a','blue':'#4d7092','yellow':'#f4df72','coral':'#f47b65','green':'#708b78','purple':'#81738e','midnight green':'#4e5a55','graphite':'#5d5b57','pacific blue':'#536b7b','pink':'#e8c5c0','midnight':'#232a31','starlight':'#eee7d9','sierra blue':'#9bb5ce','alpine green':'#576856','space black':'#343336','deep purple':'#554e5f','black titanium':'#4a4845','white titanium':'#ddd9cf','blue titanium':'#5a6570','natural titanium':'#8f877c','teal':'#5f8f91','ultramarine':'#4c5f9a','desert titanium':'#b39c88','mist blue':'#adc6d5','sage':'#9ca88c','lavender':'#b8acc9','cosmic orange':'#d86d2d','deep blue':'#394a69','soft pink':'#e9c6cb','cloud white':'#efefec','light gold':'#d9c69f','sky blue':'#9dbbd0'};
        return map[String(color || '').toLowerCase()] || (/^#[0-9a-f]{3,8}$/i.test(color) ? color : '#d7d8dc');
    }

    function _categoryLabel(brandName, categoryName) {
        var brand = (brandName || '').toLowerCase();
        var category = (categoryName || '').toLowerCase();
        if (brand === 'apple') {
            if (category.includes('phone')) return 'iPhone';
            if (category.includes('tablet')) return 'iPad';
            if (category.includes('laptop') || category.includes('computer')) return 'MacBook';
        }
        if (brand === 'samsung') {
            if (category.includes('phone')) return 'Galaxy Phones';
            if (category.includes('tablet')) return 'Galaxy Tab';
        }
        return categoryName;
    }

    function _categoryIconClass(categoryName, configuredIcon) {
        var text = ((categoryName || '') + ' ' + (configuredIcon || '')).toLowerCase();
        if (text.includes('tablet') || text.includes('ipad')) return 'fas fa-tablet-alt';
        if (text.includes('laptop') || text.includes('computer') || text.includes('macbook')) return 'fas fa-laptop';
        if (text.includes('watch')) return 'fas fa-clock';
        if (text.includes('console') || text.includes('game')) return 'fas fa-gamepad';
        if (text.includes('phone') || text.includes('smartphone') || text.includes('iphone')) return 'fas fa-mobile-alt';
        return 'fas fa-microchip';
    }

    function _modelFamily(brandName, modelName) {
        var brand = (brandName || '').toLowerCase();
        var model = (modelName || '').toLowerCase();
        if (brand === 'samsung') {
            if (/galaxy\s*s(?:\d|\s|$)/.test(model)) return 'Galaxy S Series';
            if (/galaxy\s*a(?:\d|\s|$)/.test(model)) return 'Galaxy A Series';
            if (model.includes('note')) return 'Galaxy Note Series';
            if (/galaxy\s*z\b/.test(model) || model.includes('fold') || model.includes('flip')) return 'Galaxy Z Series';
            if (model.includes('tab')) return 'Galaxy Tab Series';
        }
        if (brand === 'google') {
            if (model.includes('fold')) return 'Pixel Fold';
            if (model.includes('tablet')) return 'Pixel Tablet';
            if (model.includes('pixel')) return 'Pixel Phones';
        }
        if (brand === 'motorola') {
            if (model.includes('razr')) return 'Razr Series';
            if (model.includes('edge')) return 'Edge Series';
            if (/moto\s*g\b/.test(model)) return 'Moto G Series';
        }
        if (brand === 'oneplus') {
            if (model.includes('nord')) return 'Nord Series';
            if (model.includes('open')) return 'Open Series';
            return 'OnePlus Number Series';
        }
        if (brand === 'lg') {
            if (model.includes('stylo')) return 'Stylo Series';
            if (/lg\s*v\b/.test(model)) return 'V Series';
            if (/lg\s*g\b/.test(model)) return 'G Series';
        }
        return 'All Models';
    }

    function _dispatch(eventName, detail) {
        document.dispatchEvent(new CustomEvent(eventName, { detail: detail, bubbles: true }));
    }

    function _writeHidden(id, value) {
        var el = _el(id);
        if (el) el.value = (value != null) ? value : '';
    }

    // Each shop browser remembers its own tile order.  The scope includes the
    // selected brand/category/model so rearranging one grid never reshuffles a
    // different device or repair list.
    function _orderKey(kind, st, suffix) {
        return 'empire.tileOrder.v1.' + kind + '.' + [st.brand || 0, st.category || 0, st.model || 0, suffix || ''].join('.');
    }

    function _applySavedOrder(items, key, identity) {
        var saved = [];
        try { saved = JSON.parse(global.localStorage.getItem(key) || '[]'); } catch (_) { saved = []; }
        if (!saved.length) return items.slice();
        var rank = {};
        saved.forEach(function(id, index) { rank[String(id)] = index; });
        return items.map(function(item, index) { return { item: item, index: index }; })
            .sort(function(a, b) {
                var ar = rank[String(identity(a.item))];
                var br = rank[String(identity(b.item))];
                ar = ar == null ? Number.MAX_SAFE_INTEGER : ar;
                br = br == null ? Number.MAX_SAFE_INTEGER : br;
                return ar === br ? a.index - b.index : ar - br;
            }).map(function(entry) { return entry.item; });
    }

    function _enableDragOrder(grid, key) {
        if (!grid) return;
        var dragging = null;
        grid.querySelectorAll('[data-order-id]').forEach(function(card) {
            card.draggable = true;
            card.title = 'Drag to rearrange';
            card.addEventListener('dragstart', function(event) {
                dragging = card;
                grid.dataset.wasDragged = 'true';
                card.classList.add('eds-dragging');
                event.dataTransfer.effectAllowed = 'move';
                event.dataTransfer.setData('text/plain', card.dataset.orderId || '');
            });
            card.addEventListener('dragover', function(event) {
                event.preventDefault();
                if (!dragging || dragging === card) return;
                var box = card.getBoundingClientRect();
                var after = event.clientY > box.top + box.height / 2 || event.clientX > box.left + box.width / 2;
                grid.insertBefore(dragging, after ? card.nextSibling : card);
            });
            card.addEventListener('dragend', function() {
                card.classList.remove('eds-dragging');
                dragging = null;
                var order = Array.from(grid.querySelectorAll('[data-order-id]')).map(function(item) { return item.dataset.orderId; });
                global.localStorage.setItem(key, JSON.stringify(order));
                global.setTimeout(function(){ grid.dataset.wasDragged = ''; }, 80);
            });
            card.addEventListener('click', function(event) {
                if (grid.dataset.wasDragged === 'true') { event.preventDefault(); event.stopImmediatePropagation(); }
            }, true);
        });
    }

    function _state(sid) {
        if (!_states[sid]) {
            _states[sid] = {
                brand: null, brandName: '',
                category: null, categoryName: '',
                model: null, modelName: '',
                issue: '',
                availableParts: [],
                availableModels: [],
                modelFamilies: {},
                selectedParts: [],
                selectedScreenQuality: '',
                options: {}
            };
        }
        return _states[sid];
    }

    // ── Data loading ──────────────────────────────────────────────────────────
    function _loadBrands(callback) {
        if (_cache.brands) { callback(_cache.brands); return; }
        fetch('/LookupManagement/GetBrands')
            .then(function(r) { return r.json(); })
            .then(function(res) {
                _cache.brands = (res.success && res.data)
                    ? res.data.map(function(b) {
                        return { id: b.id, name: b.name, color: b.color || '#757575', icon: b.icon || 'fas fa-mobile-alt' };
                    })
                    : [];
                callback(_cache.brands);
            })
            .catch(function() { callback([]); });
    }

    function _loadCategories(callback) {
        if (_cache.categories) { callback(_cache.categories); return; }
        fetch('/LookupManagement/GetDeviceCategories')
            .then(function(r) { return r.json(); })
            .then(function(res) {
                _cache.categories = (res.success && res.data)
                    ? res.data.map(function(c) {
                        return { id: c.id, name: c.name, icon: c.icon || 'devices_other', color: c.color || '#757575' };
                    })
                    : [];
                callback(_cache.categories);
            })
            .catch(function() { callback([]); });
    }

    function _loadModels(brandId, categoryId, callback) {
        var key = (brandId || 'x') + '_' + (categoryId || 'x');
        if (_cache.models[key]) { callback(_cache.models[key]); return; }
        var url = '/Repairs/GetDeviceModels?';
        if (brandId)    url += 'brandId='    + brandId    + '&';
        if (categoryId) url += 'categoryId=' + categoryId + '&';
        fetch(url)
            .then(function(r) { return r.json(); })
            .then(function(res) {
                var models = (res.success && res.data)
                    ? res.data.map(function(m) {
                        return { id: m.id, name: m.name, icon: m.icon || '📱', color: m.color || '#757575' };
                    })
                    : [];
                _cache.models[key] = models;
                callback(models);
            })
            .catch(function() { callback([]); });
    }

    function _loadParts(brandId, categoryId, modelId, callback) {
        var url = '/Repairs/GetInventoryParts?';
        if (brandId)    url += 'brandId='    + brandId    + '&';
        if (categoryId) url += 'categoryId=' + categoryId + '&';
        if (modelId)    url += 'modelId='    + modelId    + '&';
        fetch(url)
            .then(function(r) { return r.json(); })
            .then(function(res) {
                callback((res.success && res.data) ? res.data : []);
            })
            .catch(function() { callback([]); });
    }

    // ── Rendering ─────────────────────────────────────────────────────────────
    function _renderBrands(sid, brands) {
        var grid = _el(sid + '-brands');
        if (!grid) return;
        if (!brands.length) {
            grid.innerHTML = '<p class="eds-empty">No brands found. <a href="/LookupManagement">Add brands</a></p>';
            return;
        }
        var key = _orderKey('brands', _state(sid));
        brands = _applySavedOrder(brands, key, function(b) { return b.id; });
        grid.innerHTML = brands.map(function(b) {
            return '<div class="eds-brand-card" data-id="' + b.id + '" data-order-id="' + b.id + '" onclick="EmpireDeviceSelector.selectBrand(\'' + sid + '\',' + b.id + ',\'' + (b.name || '').replace(/'/g, "\\'") + '\')">'
                + '<div class="eds-brand-logo">' + _brandMarkup(sid, b.name) + '</div>'
                + '</div>';
        }).join('');
        _enableDragOrder(grid, key);
    }

    function _renderCategories(sid, categories) {
        var grid = _el(sid + '-categories');
        if (!grid) return;
        if (!categories.length) {
            grid.innerHTML = '<p class="eds-empty">No categories found.</p>';
            return;
        }
        var st = _state(sid);
        var key = _orderKey('categories', st);
        categories = _applySavedOrder(categories, key, function(c) { return c.id; });
        grid.innerHTML = categories.map(function(c) {
            return '<div class="eds-category-card" data-id="' + c.id + '" data-order-id="' + c.id + '" onclick="EmpireDeviceSelector.selectCategory(\'' + sid + '\',' + c.id + ',\'' + (c.name || '').replace(/'/g, "\\'") + '\')">'
                + '<i class="' + _categoryIconClass(c.name, c.icon) + ' eds-category-icon" aria-hidden="true"></i>'
                + '<div class="eds-category-name">' + _categoryLabel(st.brandName, c.name) + '</div>'
                + '</div>';
        }).join('');
        _enableDragOrder(grid, key);
    }

    function _renderModelChoices(sid, models) {
        var st = _state(sid);
        st.availableModels = models;
        st.modelFamilies = {};
        if (st.options.groupModels === false) {
            var directTitle = _el(sid + '-step3-title');
            if (directTitle) directTitle.innerHTML = '<i class="fas fa-mobile-alt me-2"></i>Select Model';
            _renderModels(sid, models);
            return;
        }
        models.forEach(function(model) {
            var family = _modelFamily(st.brandName, model.name);
            (st.modelFamilies[family] = st.modelFamilies[family] || []).push(model);
        });
        var familyKey = _orderKey('families', st);
        var families = _applySavedOrder(Object.keys(st.modelFamilies), familyKey, function(family) { return family; });
        var title = _el(sid + '-step3-title');
        if (families.length <= 1) {
            if (title) title.innerHTML = '<i class="fas fa-mobile-alt me-2"></i>Select Model';
            _renderModels(sid, models);
            return;
        }
        if (title) title.innerHTML = '<i class="fas fa-layer-group me-2"></i>Select Series';
        var grid = _el(sid + '-models');
        grid.innerHTML = families.map(function(family) {
            return '<div class="eds-model-card eds-family-card" data-order-id="' + family + '" onclick="EmpireDeviceSelector.selectFamily(\'' + sid + '\',\'' + family.replace(/'/g, "\\'") + '\')">'
                + '<div class="eds-model-icon"><i class="fas fa-layer-group"></i></div><div class="eds-model-name">' + family + '</div></div>';
        }).join('');
        _enableDragOrder(grid, familyKey);
    }

    function _renderModels(sid, models) {
        var grid = _el(sid + '-models');
        if (!grid) return;
        if (!models.length) {
            var canCreate = !!_state(sid).options.allowCreate;
            grid.innerHTML = '<div class="eds-empty">'
                + '<i class="fas fa-mobile-alt fa-2x mb-2"></i><p>No models found.</p>'
                + (canCreate ? '<p class="mb-0">Use Add New Model below to save one for future use.</p>'
                    : '<button type="button" class="btn btn-sm btn-outline-secondary mt-1" onclick="EmpireDeviceSelector.manualModel(\'' + sid + '\')"><i class="fas fa-plus me-1"></i>Add Manually</button>')
                + '</div>';
            return;
        }
        var st = _state(sid);
        var key = _orderKey('models', st);
        models = _applySavedOrder(models, key, function(m) { return m.id; });
        grid.innerHTML = models.map(function(m) {
            return '<div class="eds-model-card" data-id="' + m.id + '" data-order-id="' + m.id + '" onclick="EmpireDeviceSelector.selectModel(\'' + sid + '\',' + m.id + ',\'' + (m.name || '').replace(/'/g, "\\'") + '\')">'
                + '<div class="eds-model-icon" aria-hidden="true">📱</div>'
                + '<div class="eds-model-name">' + m.name + '</div>'
                + '</div>';
        }).join('');
        _enableDragOrder(grid, key);
    }

    function _renderParts(sid, parts) {
        var grid = _el(sid + '-parts');
        if (!grid) return;
        var st = _state(sid);
        st.availableParts = parts.map(function(part) {
            var color = _partColor(part, st.modelName);
            var inventoryName = part.name || '';
            // Screen inventory is not always named with the word "screen" (for
            // example, "Incell" or "Soft OLED"). Treat those quality names as
            // screen parts so every quality is grouped into one screen card.
            var isScreen = /screen|display|lcd|incell|oled/i.test([inventoryName, part.variant || '', part.description || ''].join(' '));
            var quality = isScreen ? _screenQuality(part) : '';
            return Object.assign({}, part, { inventoryName: inventoryName, name: _repairLabel(inventoryName) + (quality ? ' — ' + quality : ''), variant: quality || part.variant, displayColor: color, price: 0, isScreenPart: isScreen });
        });
        var isMacBook = /^apple$/i.test(st.brandName || '') && /laptop|computer|macbook/i.test((st.categoryName || '') + ' ' + (st.modelName || ''));
        if (isMacBook) {
            if (!st.availableParts.some(function(p) { return /battery/i.test(p.inventoryName || p.name || ''); })) {
                st.availableParts.push({ id:-9001, name:'Battery Replacement', inventoryName:'Battery Replacement', price:0, stock:0, inStock:true, custom:true });
            }
            if (!st.availableParts.some(function(p) { return /(?:macos|factory).*reset|reset.*(?:macos|factory)/i.test(p.inventoryName || p.name || ''); })) {
                st.availableParts.push({ id:-9002, name:'macOS Reset', inventoryName:'macOS Reset', price:0, stock:0, inStock:true, custom:true });
            }
        }
        var key = _orderKey('parts', st);
        st.availableParts = _applySavedOrder(st.availableParts, key, function(p) { return p.id; });
        var screenParts = st.availableParts.filter(function(p) { return p.isScreenPart; });
        var screenCard = '';
        {
            // Screen-quality choices only apply to phones. MacBooks, iPads and
            // other devices get one straightforward Screen Replacement service.
            var isPhone = /phone|smartphone|iphone/i.test((st.categoryName || '') + ' ' + (st.modelName || ''));
            var selectedScreen = st.selectedParts.find(function(p) { return p.isScreenPart; });
            var selectedQuality = st.selectedScreenQuality || (selectedScreen && selectedScreen.variant) || '';
            var hasInventoryMatch = !!(selectedScreen && !selectedScreen.custom);
            if (isPhone) {
                var standardQualities = /^apple$/i.test(st.brandName || '')
                    ? ['Incell', 'Soft OLED', 'Hard OLED', 'OEM', 'Original/Refurbished']
                    : ['Aftermarket', 'Original'];
                var qualities = standardQualities.slice();
                screenParts.forEach(function(p) {
                    var quality = String(p.variant || '').trim();
                    if (quality && !qualities.some(function(existing) { return existing.toLowerCase() === quality.toLowerCase(); })) qualities.push(quality);
                });
                screenCard = '<div class="eds-part-card eds-repair-card eds-screen-card' + (selectedQuality ? ' selected' : '') + '" data-order-id="screen" data-selected-id="' + (hasInventoryMatch ? selectedScreen.id : '') + '">'
                    + '<div class="eds-part-icon" aria-hidden="true">' + _partIcon('screen') + '</div>'
                    + '<div class="eds-repair-card-body"><div class="eds-part-name">Screen Replacement</div>'
                    + '<div class="eds-screen-options" role="radiogroup" aria-label="Screen quality">'
                    + qualities.map(function(quality) {
                        var matching = screenParts.filter(function(p) { return String(p.variant || '').trim().toLowerCase() === quality.toLowerCase(); });
                        var stock = matching.reduce(function(total, p) { return total + Math.max(0, parseInt(p.stock || 0)); }, 0);
                        var isSelected = selectedQuality.toLowerCase() === quality.toLowerCase();
                        return '<button type="button" role="radio" aria-checked="' + !!isSelected + '" class="eds-screen-option' + (isSelected ? ' selected' : '') + '" data-quality="' + _escapeHtml(quality) + '" onclick="EmpireDeviceSelector.selectScreenQuality(\'' + sid + '\',this.dataset.quality)">'
                            + '<span class="eds-screen-dot" aria-hidden="true"></span><span>' + _escapeHtml(quality) + '</span><small>' + (stock > 0 ? stock + ' in stock' : matching.length ? '0 in stock' : 'Not in inventory') + '</small></button>';
                    }).join('') + '</div>'
                    + '<div class="eds-screen-stock-note"' + (selectedQuality && !hasInventoryMatch ? '' : ' hidden') + '>'
                    + (selectedScreen && selectedScreen.inventoryItemId ? 'This quality is connected to inventory but has 0 in stock. Receive stock before using the part; no stock will be deducted now.' : 'This quality is not in inventory; it will be recorded, but no screen stock will be deducted.') + '</div>'
                    + '<label class="eds-repair-price-label" onclick="event.stopPropagation()">Price $<input class="eds-repair-price-input" type="number" min="0" step="0.01" placeholder="Select a screen first" aria-label="Screen Replacement price"'
                    + (selectedQuality ? ' value="' + (selectedScreen.price || '') + '"' : ' disabled')
                    + ' oninput="EmpireDeviceSelector.updateScreenPrice(\'' + sid + '\',this)"></label>'
                    + '</div></div>';
            } else {
                screenCard = '<div class="eds-part-card eds-repair-card eds-screen-card' + (selectedScreen ? ' selected' : '') + '" data-order-id="screen" data-selected-id="' + (hasInventoryMatch ? selectedScreen.id : '') + '" onclick="EmpireDeviceSelector.selectSimpleScreen(\'' + sid + '\')">'
                    + '<div class="eds-part-icon" aria-hidden="true">' + _partIcon('screen') + '</div>'
                    + '<div class="eds-repair-card-body"><div class="eds-part-name">Screen Replacement</div>'
                    + '<label class="eds-repair-price-label" onclick="event.stopPropagation()">Price $<input class="eds-repair-price-input" type="number" min="0" step="0.01" placeholder="Enter price" aria-label="Screen Replacement price" value="' + (selectedScreen ? selectedScreen.price || '' : '') + '" oninput="EmpireDeviceSelector.updateScreenPrice(\'' + sid + '\',this)"></label>'
                    + '</div></div>';
            }
        }
        grid.innerHTML = screenCard + st.availableParts.filter(function(p) { return !p.isScreenPart; }).map(function(p) {
            var selected = st.selectedParts.some(function(x) { return x.id === p.id; });
            var stock = parseInt(p.stock || 0);
            var isBackGlass = /back\s*(glass|cover)|rear\s*glass/i.test(p.inventoryName || '');
            var officialColors = _officialColorsFor(st.modelName);
            var colorIsOfficial = !officialColors.length || !p.displayColor || officialColors.some(function(color){ return color.toLowerCase() === p.displayColor.toLowerCase(); });
            if (isBackGlass && !colorIsOfficial) return '';
            var colorLine = isBackGlass && p.displayColor
                ? '<div class="eds-part-color"><span class="eds-color-dot" style="background:' + _colorSwatch(p.displayColor) + '"></span>' + p.displayColor + '</div>' : '';
            return '<div class="eds-part-card eds-repair-card' + (selected ? ' selected' : '') + '" data-id="' + p.id + '" data-order-id="' + p.id + '" onclick="EmpireDeviceSelector.togglePartById(\'' + sid + '\',' + p.id + ')">'
                + '<div class="eds-part-icon" aria-hidden="true">' + _partIcon(p.inventoryName) + '</div>'
                + '<div class="eds-repair-card-body"><div class="eds-part-name">' + _escapeHtml(p.name) + '</div>' + colorLine
                + '<label class="eds-repair-price-label" onclick="event.stopPropagation()">Price $<input class="eds-repair-price-input" type="number" min="0" step="0.01" placeholder="Enter price" aria-label="' + _escapeHtml(p.name) + ' price" onclick="event.stopPropagation()" oninput="EmpireDeviceSelector.updatePartPrice(\'' + sid + '\',' + p.id + ',this.value)"></label>'
                + (p.custom ? '' : '<div class="eds-part-stock ' + (p.inStock ? 'in-stock' : 'out-stock') + '">'
                + (p.inStock ? stock + ' in stock' : 'Out of stock') + '</div>')
                + '</div></div>';
        }).join('');
        _enableDragOrder(grid, key);
    }

    function _updateBreadcrumb(sid) {
        var st  = _state(sid);
        var bc  = _el(sid + '-breadcrumb');
        if (!bc) return;
        var hasSel = st.brand || st.category || st.model;
        bc.style.display = hasSel ? 'flex' : 'none';
        var cb = _el(sid + '-crumb-brand');
        var cc = _el(sid + '-crumb-category');
        var cm = _el(sid + '-crumb-model');
        if (cb) {
            if (/^apple(?:\s|$|\.|,)/i.test(st.brandName || '')) cb.innerHTML = _brandMarkup(sid, st.brandName);
            else cb.textContent = st.brandName || '';
        }
        if (cc) cc.textContent = st.categoryName || '';
        if (cm) cm.textContent = st.modelName    || '';
        var seps = bc.querySelectorAll('.eds-crumb-sep');
        if (seps[0]) seps[0].style.display = (st.brand    && st.category) ? '' : 'none';
        if (seps[1]) seps[1].style.display = (st.category && st.model)    ? '' : 'none';
    }

    // ── HTML builder ──────────────────────────────────────────────────────────
    function _buildHtml(sid, showParts, showIssue, allowCreate) {
        var html = '<div class="empire-device-selector" id="' + sid + '-wrapper">'
            + '<div class="eds-step eds-step--active" id="' + sid + '-step1">'
            +   '<h6 class="eds-step-title"><i class="fas fa-tag me-2"></i>Select Brand</h6>'
            +   '<div class="eds-brand-grid" id="' + sid + '-brands"><div class="eds-loading"><i class="fas fa-spinner fa-spin me-2"></i>Loading brands…</div></div>'
            +   (allowCreate ? '<button type="button" class="eds-create-lookup-btn" onclick="EmpireDeviceSelector.requestCreate(\'' + sid + '\',\'brand\')"><i class="fas fa-plus"></i>Add New Brand</button>' : '')
            + '</div>'
            + '<div class="eds-step" id="' + sid + '-step2">'
            +   '<div class="eds-step-header">'
            +   '<button type="button" class="eds-back-btn" onclick="EmpireDeviceSelector.goToStep(\'' + sid + '\',1)"><i class="fas fa-arrow-left"></i></button>'
            +   '<h6 class="eds-step-title"><i class="fas fa-th-large me-2"></i>Select Device Type</h6></div>'
            +   '<div class="eds-category-grid" id="' + sid + '-categories"><div class="eds-loading"><i class="fas fa-spinner fa-spin me-2"></i>Loading…</div></div>'
            +   (allowCreate ? '<button type="button" class="eds-create-lookup-btn" onclick="EmpireDeviceSelector.requestCreate(\'' + sid + '\',\'category\')"><i class="fas fa-plus"></i>Add New Device Type</button>' : '')
            + '</div>'
            + '<div class="eds-step" id="' + sid + '-step3">'
            +   '<div class="eds-step-header">'
            +   '<button type="button" class="eds-back-btn" onclick="EmpireDeviceSelector.goToStep(\'' + sid + '\',2)"><i class="fas fa-arrow-left"></i></button>'
            +   '<h6 class="eds-step-title" id="' + sid + '-step3-title"><i class="fas fa-mobile-alt me-2"></i>Select Model</h6></div>'
            +   '<div class="eds-model-grid" id="' + sid + '-models"><div class="eds-loading"><i class="fas fa-spinner fa-spin me-2"></i>Loading…</div></div>'
            +   (allowCreate ? '<button type="button" class="eds-create-lookup-btn" onclick="EmpireDeviceSelector.requestCreate(\'' + sid + '\',\'model\')"><i class="fas fa-plus"></i>Add New Model</button>' : '')
            + '</div>';

        if (showParts) {
            html += '<div class="eds-step" id="' + sid + '-step4">'
                +   '<div class="eds-step-header">'
                +   '<button type="button" class="eds-back-btn" onclick="EmpireDeviceSelector.goToStep(\'' + sid + '\',3)"><i class="fas fa-arrow-left"></i></button>'
                +   '<h6 class="eds-step-title"><i class="fas fa-tools me-2"></i>Select Parts</h6></div>';
            if (showIssue) {
                html += '<div class="mb-3"><label class="form-label fw-semibold">Device Issue / Problem</label>'
                    + '<input type="text" class="form-control" id="' + sid + '-issue" placeholder="Describe the issue…"'
                    + ' oninput="EmpireDeviceSelector.updateIssue(\'' + sid + '\',this.value)" /></div>';
            }
            html += '<div class="eds-selected-banner" id="' + sid + '-banner" style="display:none;"></div>'
                +   '<div class="eds-parts-grid" id="' + sid + '-parts"><div class="eds-loading"><i class="fas fa-spinner fa-spin me-2"></i>Loading parts…</div></div>'
                +   '<button type="button" class="eds-add-service-btn" onclick="openAddRepairType(\'' + sid + '\')"><i class="fas fa-plus"></i> Add New Repair Type</button>'
                +   '<div id="' + sid + '-selected-parts"></div>'
                + '</div>';
        }

        html += '<div class="eds-breadcrumb mt-2" id="' + sid + '-breadcrumb" style="display:none;">'
            + '<span class="eds-crumb" id="' + sid + '-crumb-brand"></span>'
            + '<span class="eds-crumb-sep"><i class="fas fa-chevron-right"></i></span>'
            + '<span class="eds-crumb" id="' + sid + '-crumb-category"></span>'
            + '<span class="eds-crumb-sep"><i class="fas fa-chevron-right"></i></span>'
            + '<span class="eds-crumb" id="' + sid + '-crumb-model"></span>'
            + '<button type="button" class="eds-reset-btn ms-2" onclick="EmpireDeviceSelector.reset(\'' + sid + '\')" title="Reset"><i class="fas fa-times-circle"></i></button>'
            + '</div></div>';

        return html;
    }

    // ── Public API ────────────────────────────────────────────────────────────
    var EDS = {

        /**
         * Initialise a selector rendered via the Razor _DeviceSelector partial.
         * Reads options from data-* attributes on the wrapper div.
         */
        init: function(sid) {
            var wrapper = _el(sid + '-wrapper');
            if (!wrapper) { console.warn('EmpireDeviceSelector: wrapper #' + sid + '-wrapper not found'); return; }
            var st = _state(sid);
            st.options = {
                showParts:      wrapper.dataset.showParts      === 'true',
                showIssue:      wrapper.dataset.showIssue      === 'true',
                hiddenBrand:    wrapper.dataset.hiddenBrand    || '',
                hiddenCategory: wrapper.dataset.hiddenCategory || '',
                hiddenModel:    wrapper.dataset.hiddenModel    || '',
                onModelSelected: null,
                onPartToggled:   null
            };
            _loadBrands(function(brands) { _renderBrands(sid, brands); });
        },

        /**
         * Create a selector entirely from JavaScript (no Razor partial needed).
         * Injects HTML into the element with id=containerId.
         */
        create: function(containerId, opts) {
            opts = opts || {};
            var sid       = opts.selectorId || containerId;
            var container = _el(containerId);
            if (!container) { console.warn('EmpireDeviceSelector.create: container #' + containerId + ' not found'); return; }

            var showParts = opts.showParts      || false;
            var showIssue = opts.showIssueInput || false;
            if (opts.freshData) {
                _cache.brands = null;
                _cache.categories = null;
                _cache.models = {};
            }

            var allowCreate = opts.allowCreate || false;
            container.innerHTML = _buildHtml(sid, showParts, showIssue, allowCreate);

            var st = _state(sid);
            // Reset state for a fresh create (e.g. modal re-open)
            st.brand = null; st.brandName = '';
            st.category = null; st.categoryName = '';
            st.model = null; st.modelName = '';
            st.issue = ''; st.selectedParts = []; st.selectedScreenQuality = '';
            st.options = {
                showParts:       showParts,
                showIssue:       showIssue,
                allowCreate:     allowCreate,
                groupModels:     opts.groupModels !== false,
                hiddenBrand:     opts.hiddenBrand    || '',
                hiddenCategory:  opts.hiddenCategory || '',
                hiddenModel:     opts.hiddenModel    || '',
                onModelSelected: opts.onModelSelected || null,
                onPartToggled:   opts.onPartToggled   || null,
                onCreateRequest: opts.onCreateRequest || null
            };
            _loadBrands(function(brands) { _renderBrands(sid, brands); });
        },

        requestCreate: function(sid, kind) {
            var st = _state(sid);
            var detail = {
                selectorId: sid,
                kind: kind,
                brandId: st.brand,
                brandName: st.brandName,
                categoryId: st.category,
                categoryName: st.categoryName
            };
            if (st.options.onCreateRequest) st.options.onCreateRequest(detail);
            _dispatch('eds:createRequested', detail);
        },

        refreshAfterCreate: function(sid, kind, item) {
            var st = _state(sid);
            var selectedId = parseInt(item && (item.id || item.Id), 10);
            var selectedName = item && (item.name || item.Name);
            if (!selectedId || !selectedName) return;

            if (kind === 'brand') {
                _cache.brands = null;
                _loadBrands(function(brands) {
                    _renderBrands(sid, brands);
                    var match = brands.find(function(brand) { return parseInt(brand.id) === selectedId; });
                    if (match) EDS.selectBrand(sid, match.id, match.name);
                });
                return;
            }

            if (kind === 'category') {
                _cache.categories = null;
                _loadCategories(function(categories) {
                    _renderCategories(sid, categories);
                    var match = categories.find(function(category) { return parseInt(category.id) === selectedId; });
                    if (match) EDS.selectCategory(sid, match.id, match.name);
                });
                return;
            }

            if (kind === 'model') {
                var key = (st.brand || 'x') + '_' + (st.category || 'x');
                delete _cache.models[key];
                _loadModels(st.brand, st.category, function(models) {
                    _renderModelChoices(sid, models);
                    var match = models.find(function(model) { return parseInt(model.id) === selectedId; });
                    if (match) EDS.selectModel(sid, match.id, match.name);
                });
            }
        },

        // ── Step navigation ───────────────────────────────────────────────────
        goToStep: function(sid, step) {
            [1, 2, 3, 4].forEach(function(i) {
                var el = _el(sid + '-step' + i);
                if (el) el.classList.toggle('eds-step--active', i === step);
            });
        },

        // ── Selection handlers ────────────────────────────────────────────────
        selectBrand: function(sid, brandId, brandName) {
            var st = _state(sid);
            st.brand = brandId; st.brandName = brandName;
            st.category = null; st.categoryName = '';
            st.model = null; st.modelName = ''; st.selectedParts = []; st.selectedScreenQuality = '';

            var grid = _el(sid + '-brands');
            if (grid) grid.querySelectorAll('.eds-brand-card').forEach(function(c) {
                c.classList.toggle('active', parseInt(c.dataset.id) === brandId);
            });

            _writeHidden(st.options.hiddenBrand,    brandId);
            _writeHidden(st.options.hiddenCategory, '');
            _writeHidden(st.options.hiddenModel,    '');
            _updateBreadcrumb(sid);

            var catGrid = _el(sid + '-categories');
            if (catGrid) catGrid.innerHTML = '<div class="eds-loading"><i class="fas fa-spinner fa-spin me-2"></i>Loading categories…</div>';
            EDS.goToStep(sid, 2);
            _loadCategories(function(cats) { _renderCategories(sid, cats); });
        },

        selectCategory: function(sid, categoryId, categoryName) {
            var st = _state(sid);
            st.category = categoryId; st.categoryName = categoryName;
            st.model = null; st.modelName = ''; st.selectedParts = []; st.selectedScreenQuality = '';

            var grid = _el(sid + '-categories');
            if (grid) grid.querySelectorAll('.eds-category-card').forEach(function(c) {
                c.classList.toggle('active', parseInt(c.dataset.id) === categoryId);
            });

            _writeHidden(st.options.hiddenCategory, categoryId);
            _writeHidden(st.options.hiddenModel,    '');
            _updateBreadcrumb(sid);

            var modGrid = _el(sid + '-models');
            if (modGrid) modGrid.innerHTML = '<div class="eds-loading"><i class="fas fa-spinner fa-spin me-2"></i>Loading models…</div>';
            EDS.goToStep(sid, 3);
            _loadModels(st.brand, categoryId, function(models) { _renderModelChoices(sid, models); });
        },

        selectFamily: function(sid, family) {
            var st = _state(sid);
            var title = _el(sid + '-step3-title');
            if (title) title.innerHTML = '<i class="fas fa-mobile-alt"></i>Select Model';
            _renderModels(sid, st.modelFamilies[family] || []);
        },

        selectModel: function(sid, modelId, modelName) {
            var st = _state(sid);
            st.model = modelId; st.modelName = modelName;
            st.selectedParts = []; st.selectedScreenQuality = '';

            var grid = _el(sid + '-models');
            if (grid) grid.querySelectorAll('.eds-model-card').forEach(function(c) {
                c.classList.toggle('active', parseInt(c.dataset.id) === modelId);
            });

            _writeHidden(st.options.hiddenModel, modelId);
            _updateBreadcrumb(sid);

            var detail = {
                selectorId:   sid,
                brandId:      st.brand,
                brandName:    st.brandName,
                categoryId:   st.category,
                categoryName: st.categoryName,
                modelId:      modelId,
                modelName:    modelName
            };
            _dispatch('eds:modelSelected', detail);
            if (st.options.onModelSelected) st.options.onModelSelected(detail);

            if (st.options.showParts) {
                var banner = _el(sid + '-banner');
                if (banner) {
                    banner.style.display = 'flex';
                    banner.innerHTML = '<div class="eds-banner-icon"><i class="fas fa-mobile-alt"></i></div>'
                        + '<div><div class="eds-banner-brand">' + _brandMarkup(sid, st.brandName || '') + '</div>'
                        + '<div class="eds-banner-model">' + modelName + '</div></div>';
                }
                var partsGrid = _el(sid + '-parts');
                if (partsGrid) partsGrid.innerHTML = '<div class="eds-loading"><i class="fas fa-spinner fa-spin me-2"></i>Loading parts…</div>';
                EDS.goToStep(sid, 4);
                _loadParts(st.brand, st.category, modelId, function(parts) { _renderParts(sid, parts); });
            }
        },

        togglePart: function(sid, part) {
            var st  = _state(sid);
            var idx = st.selectedParts.findIndex(function(x) { return x.id === part.id; });
            if (idx < 0 && part.isScreenPart && parseInt(part.stock || 0) <= 0) return;
            if (idx >= 0) {
                st.selectedParts.splice(idx, 1);
            } else {
                st.selectedParts.push(part);
            }
            var partsGrid = _el(sid + '-parts');
            if (partsGrid) {
                partsGrid.querySelectorAll('.eds-part-card').forEach(function(c) {
                    c.classList.toggle('selected', st.selectedParts.some(function(x) { return x.id === parseInt(c.dataset.id); }));
                });
            }
            var detail = { selectorId: sid, part: part, selectedParts: st.selectedParts.slice() };
            _dispatch('eds:partToggled', detail);
            if (st.options.onPartToggled) st.options.onPartToggled(detail);

            var issueInput = _el(sid + '-issue');
            if (issueInput) {
                issueInput.value = st.selectedParts.map(function(selectedPart) { return selectedPart.name; }).join(', ');
                EDS.updateIssue(sid, issueInput.value);
            }
        },

        togglePartById: function(sid, partId) {
            var part = _state(sid).availableParts.find(function(candidate) { return parseInt(candidate.id) === parseInt(partId); });
            if (part) EDS.togglePart(sid, part);
        },

        selectScreenQuality: function(sid, quality) {
            var st = _state(sid);
            quality = String(quality || '').trim();
            if (!quality) return;
            var previous = st.selectedParts.find(function(selected) { return selected.isScreenPart; });
            var matches = st.availableParts.filter(function(candidate) {
                return candidate.isScreenPart && String(candidate.variant || '').trim().toLowerCase() === quality.toLowerCase();
            });
            var part = matches.find(function(candidate) { return parseInt(candidate.stock || 0) > 0; });
            if (!part) part = { id: 'screen-quality-' + quality.toLowerCase().replace(/[^a-z0-9]+/g, '-'), inventoryItemId: matches.length ? matches[0].id : null, name: 'Screen Replacement — ' + quality, variant: quality, price: 0, isScreenPart: true, custom: true };
            if (previous && previous.id === part.id) part.price = previous.price || 0;
            st.selectedParts = st.selectedParts.filter(function(selected) { return !selected.isScreenPart; });
            st.selectedParts.push(part);
            st.selectedScreenQuality = quality;
            var detail = { selectorId: sid, part: part, selectedParts: st.selectedParts.slice() };
            _dispatch('eds:partToggled', detail);
            if (st.options.onPartToggled) st.options.onPartToggled(detail);
            var card = _el(sid + '-parts').querySelector('.eds-screen-card');
            if (card) {
                card.classList.add('selected');
                card.dataset.selectedId = part.custom ? '' : part.id;
                card.querySelectorAll('.eds-screen-option').forEach(function(option) {
                    var selected = option.dataset.quality.toLowerCase() === quality.toLowerCase();
                    option.classList.toggle('selected', selected);
                    option.setAttribute('aria-checked', selected ? 'true' : 'false');
                });
                var note = card.querySelector('.eds-screen-stock-note');
                if (note) {
                    note.hidden = !part.custom;
                    if (part.custom) note.textContent = part.inventoryItemId
                        ? 'This quality is connected to inventory but has 0 in stock. Receive stock before using the part; no stock will be deducted now.'
                        : 'This quality is not in inventory; it will be recorded, but no screen stock will be deducted.';
                }
                var priceInput = card.querySelector('.eds-repair-price-input');
                priceInput.disabled = false;
                priceInput.placeholder = 'Enter price';
                priceInput.value = part.price || '';
            }
            var issueInput = _el(sid + '-issue');
            if (issueInput) { issueInput.value = st.selectedParts.map(function(selected) { return selected.name; }).join(', '); EDS.updateIssue(sid, issueInput.value); }
        },

        selectSimpleScreen: function(sid) {
            var st = _state(sid);
            var previous = st.selectedParts.find(function(part) { return part.isScreenPart; });
            var part = st.availableParts.find(function(candidate) { return candidate.isScreenPart && parseInt(candidate.stock || 0) > 0; })
                || st.availableParts.find(function(candidate) { return candidate.isScreenPart; })
                || { id:'screen-service', name:'Screen Replacement', price:0, isScreenPart:true, custom:true };
            if (previous && previous.price) part.price = previous.price;
            st.selectedParts = st.selectedParts.filter(function(selected) { return !selected.isScreenPart; });
            st.selectedParts.push(part);
            st.selectedScreenQuality = '';
            var card = _el(sid + '-parts').querySelector('.eds-screen-card');
            if (card) {
                card.classList.add('selected');
                card.dataset.selectedId = part.custom ? '' : part.id;
            }
            var issueInput = _el(sid + '-issue');
            if (issueInput) { issueInput.value = st.selectedParts.map(function(selectedPart) { return selectedPart.name; }).join(', '); EDS.updateIssue(sid, issueInput.value); }
            var detail = { selectorId:sid, part:part, selectedParts:st.selectedParts.slice() };
            _dispatch('eds:partToggled', detail);
            if (st.options.onPartToggled) st.options.onPartToggled(detail);
        },

        updateScreenPrice: function(sid, input) {
            var card = input.closest('.eds-screen-card');
            if (!card) return;
            if (card.dataset.selectedId) { EDS.updatePartPrice(sid, parseInt(card.dataset.selectedId), input.value); return; }
            var st = _state(sid);
            var selected = st.selectedParts.find(function(part) { return part.isScreenPart && part.custom; });
            if (selected) {
                selected.price = Math.max(0, parseFloat(input.value) || 0);
                var detail = { selectorId: sid, part: selected, selectedParts: st.selectedParts.slice() };
                _dispatch('eds:partToggled', detail);
                if (st.options.onPartToggled) st.options.onPartToggled(detail);
            }
        },

        updatePartPrice: function(sid, partId, value) {
            var st = _state(sid);
            var price = Math.max(0, parseFloat(value) || 0);
            var available = st.availableParts.find(function(part) { return parseInt(part.id) === parseInt(partId); });
            if (available) available.price = price;
            var selected = st.selectedParts.find(function(part) { return parseInt(part.id) === parseInt(partId); });
            if (selected) {
                selected.price = price;
                var detail = { selectorId: sid, part: selected, selectedParts: st.selectedParts.slice() };
                _dispatch('eds:partToggled', detail);
                if (st.options.onPartToggled) st.options.onPartToggled(detail);
            }
        },

        reloadParts: function(sid) {
            var st = _state(sid);
            var grid = _el(sid + '-parts');
            if (grid) grid.innerHTML = '<div class="eds-loading"><i class="fas fa-spinner fa-spin me-2"></i>Loading repair types…</div>';
            _loadParts(st.brand, st.category, st.model, function(parts) { _renderParts(sid, parts); });
        },

        updateIssue: function(sid, value) {
            _state(sid).issue = value;
            _dispatch('eds:issueUpdated', { selectorId: sid, issue: value });
        },

        manualModel: function(sid) {
            var name = prompt('Enter model name:');
            if (!name) return;
            EDS.selectModel(sid, 0, name.trim());
        },

        reset: function(sid) {
            var st = _state(sid);
            st.brand = null; st.brandName = '';
            st.category = null; st.categoryName = '';
            st.model = null; st.modelName = '';
            st.selectedParts = [];
            st.selectedScreenQuality = '';
            st.availableParts = [];
            st.availableModels = [];
            st.modelFamilies = {};
            _writeHidden(st.options.hiddenBrand,    '');
            _writeHidden(st.options.hiddenCategory, '');
            _writeHidden(st.options.hiddenModel,    '');
            _updateBreadcrumb(sid);
            EDS.goToStep(sid, 1);
            var grid = _el(sid + '-brands');
            if (grid) grid.querySelectorAll('.eds-brand-card').forEach(function(c) { c.classList.remove('active'); });
            _dispatch('eds:reset', { selectorId: sid });
        },

        /** Return current state snapshot for a selector */
        getState: function(sid) {
            var st = _state(sid);
            return {
                brandId:       st.brand,
                brandName:     st.brandName,
                categoryId:    st.category,
                categoryName:  st.categoryName,
                modelId:       st.model,
                modelName:     st.modelName,
                issue:         st.issue,
                selectedParts: st.selectedParts.slice()
            };
        },

        /** Pre-select brand/category/model programmatically (e.g. when editing an existing record) */
        setValue: function(sid, brandId, categoryId, modelId) {
            _loadBrands(function(brands) {
                var b = brands.find(function(x) { return x.id === brandId; });
                if (!b) return;
                _state(sid).brand = brandId; _state(sid).brandName = b.name;
                _writeHidden(_state(sid).options.hiddenBrand, brandId);
                _loadCategories(function(cats) {
                    var c = cats.find(function(x) { return x.id === categoryId; });
                    if (!c) { _updateBreadcrumb(sid); return; }
                    _state(sid).category = categoryId; _state(sid).categoryName = c.name;
                    _writeHidden(_state(sid).options.hiddenCategory, categoryId);
                    _loadModels(brandId, categoryId, function(models) {
                        var m = models.find(function(x) { return x.id === modelId; });
                        if (m) {
                            _state(sid).model = modelId; _state(sid).modelName = m.name;
                            _writeHidden(_state(sid).options.hiddenModel, modelId);
                        }
                        _updateBreadcrumb(sid);
                    });
                });
            });
        },

        /** Invalidate the brand/category/model cache (call after adding new lookup items) */
        clearCache: function() {
            _cache.brands     = null;
            _cache.categories = null;
            _cache.models     = {};
        }
    };

    global.EmpireDeviceSelector = EDS;

    /* Shared 2.25in x 1.25in DYMO label printing.  Kept here because this
       script is already loaded on both Devices and Repair pages. */
    function labelText(value) {
        return String(value == null ? '' : value).replace(/[&<>"']/g, function (ch) {
            return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[ch];
        });
    }

    function printDymoLabel(data, trigger) {
        var originalHtml = trigger ? trigger.innerHTML : '';
        if (trigger) {
            trigger.disabled = true;
            trigger.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Printing...';
        }
        return fetch('/Dymo/PrintLabel', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        }).then(function (response) {
            if (response.ok) return response.json();
            return response.json().catch(function () { return {}; }).then(function (result) {
                throw new Error(result.message || 'The DYMO label could not be printed.');
            });
        }).then(function () {
            if (trigger) {
                trigger.innerHTML = '<i class="fas fa-check me-1"></i>Printed';
                global.setTimeout(function () {
                    trigger.disabled = false;
                    trigger.innerHTML = originalHtml;
                }, 1800);
            }
            return true;
        }).catch(function (error) {
            if (trigger) {
                trigger.disabled = false;
                trigger.innerHTML = originalHtml;
            }
            global.alert(error.message || 'The DYMO label could not be printed.');
            return false;
        });
    }

    global.EmpireLabelPrinter = { print: printDymoLabel };

    // Keep the Devices page usable when the optional DataTables Responsive
    // extension is not present.  The page's original setDeviceView() used to
    // throw before its Add/Available/Sold/Filter click handlers were bound.
    // This wrapper is installed during DOMContentLoaded's capture phase, before
    // jQuery runs the page-ready callbacks.
    function stabilizeDevicesPage() {
        if (!/^\/Devices\/?$/i.test(global.location.pathname)) return;
        var original = global.setDeviceView;
        if (typeof original !== 'function' || original.__empireSafeView) return;

        function safeSetDeviceView(view) {
            try {
                return original(view);
            } catch (error) {
                var isGrid = view === 'grid';
                var tableView = document.getElementById('devicesTableView');
                var gridView = document.getElementById('devicesGridView');
                var tableButton = document.getElementById('deviceTableViewBtn');
                var gridButton = document.getElementById('deviceGridViewBtn');
                if (tableView) tableView.style.display = isGrid ? 'none' : '';
                if (gridView) gridView.style.display = isGrid ? 'grid' : 'none';
                if (tableButton) {
                    tableButton.classList.toggle('active', !isGrid);
                    tableButton.setAttribute('aria-pressed', String(!isGrid));
                }
                if (gridButton) {
                    gridButton.classList.toggle('active', isGrid);
                    gridButton.setAttribute('aria-pressed', String(isGrid));
                }
                try { global.localStorage.setItem('empire-device-view', view); } catch (_) { }
                if (global.console && console.warn) console.warn('Devices view loaded without the optional responsive table extension.');
            }
        }

        safeSetDeviceView.__empireSafeView = true;
        global.setDeviceView = safeSetDeviceView;
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', stabilizeDevicesPage, true);
    } else {
        stabilizeDevicesPage();
    }

    // Use the shared printer for every present or dynamically loaded Device row.
    document.addEventListener('click', function (event) {
        var button = event.target.closest && event.target.closest('.print-device-btn');
        if (!button) return;
        event.preventDefault();
        event.stopImmediatePropagation();
        var condition = [];
        if (button.dataset.battery) condition.push('Battery: ' + button.dataset.battery + '%');
        if (button.dataset.scratches) condition.push('Scratches: ' + button.dataset.scratches);
        printDymoLabel({
            labelType: 'device',
            primary: button.dataset.model || 'Device',
            code: button.dataset.imei || 'N/A',
            barcode: button.dataset.imei || 'N/A',
            model: button.dataset.model || 'Device',
            storage: button.dataset.storage || 'N/A',
            sellingPrice: button.dataset.sellingPrice || null,
            batteryHealth: button.dataset.battery || null,
            carrier: button.dataset.carrier || null,
            networkStatus: button.dataset.networkStatus || null,
            notes: button.dataset.notes || ''
        }, button);
    }, true);

    function addRepairLabelButton() {
        if (!/^\/Repairs\/Edit\//i.test(global.location.pathname) || document.getElementById('printRepairLabelBtn')) return;
        var actions = document.querySelector('.page-header .d-flex.gap-2');
        if (!actions) return;
        var button = document.createElement('button');
        button.type = 'button';
        button.id = 'printRepairLabelBtn';
        button.className = 'btn btn-sm btn-outline-secondary';
        button.innerHTML = '<i class="fas fa-tag me-1"></i>Print Label';
        button.addEventListener('click', function () {
            var selected = EDS.getState('editRepairDevice') || {};
            var repairNumber = global.location.pathname.split('/').filter(Boolean).pop() || 'N/A';
            var labelData = document.getElementById('repairLabelData');
            printDymoLabel({
                customerName: labelData ? labelData.dataset.customerName : '',
                phone: labelData ? labelData.dataset.customerPhone : '',
                model: selected.modelName || 'Repair Device',
                repairType: labelData ? labelData.dataset.repairType : 'Repair',
                date: labelData ? labelData.dataset.repairDate : '',
                barcode: repairNumber
            }, button);
        });
        actions.insertBefore(button, actions.firstChild);
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', addRepairLabelButton);
    else addRepairLabelButton();

    /* Live compatibility fixes. These are kept in the shared script so an
       already-running local POS receives the corrections without replacing a
       locked application binary. The compiled views contain the same behavior. */
    function installDeviceDetailsView() {
        if (!/^\/Devices\/?$/i.test(global.location.pathname)) return;

        if (!document.getElementById('deviceDetailsModal')) {
            var modal = document.createElement('div');
            modal.className = 'modal fade';
            modal.id = 'deviceDetailsModal';
            modal.tabIndex = -1;
            modal.innerHTML = '<div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable"><div class="modal-content border-0 shadow-lg" style="border-radius:16px;overflow:hidden">'
                + '<div class="modal-header text-white" style="background:linear-gradient(135deg,#111827,#2563eb)"><div><h5 class="modal-title mb-1"><i class="fas fa-mobile-alt me-2"></i>Device Details</h5><small class="opacity-75" id="deviceDetailsSubtitle">Complete device information</small></div><button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button></div>'
                + '<div class="modal-body" id="deviceDetailsModalBody"></div><div class="modal-footer bg-light"><button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button><button type="button" class="btn btn-primary" id="deviceDetailsEditButton"><i class="fas fa-edit me-1"></i>Edit</button></div>'
                + '</div></div>';
            document.body.appendChild(modal);
        }

        function detailValue(device, camelName, pascalName) {
            return device[camelName] !== undefined ? device[camelName] : device[pascalName];
        }
        function safeText(value) {
            return labelText(value == null || value === '' ? '\u2014' : value);
        }

        global.viewDevice = function (id) {
            var body = document.getElementById('deviceDetailsModalBody');
            var subtitle = document.getElementById('deviceDetailsSubtitle');
            body.innerHTML = '<div class="text-center py-5 text-muted"><span class="spinner-border spinner-border-sm me-2"></span>Loading device\u2026</div>';
            subtitle.textContent = 'Complete device information';
            document.getElementById('deviceDetailsEditButton').onclick = function () {
                global.bootstrap.Modal.getOrCreateInstance(document.getElementById('deviceDetailsModal')).hide();
                if (typeof global.editDevice === 'function') global.editDevice(id);
            };
            global.bootstrap.Modal.getOrCreateInstance(document.getElementById('deviceDetailsModal')).show();

            fetch('/Devices/GetDetails?id=' + encodeURIComponent(id), { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(function (response) { return response.json(); })
                .then(function (response) {
                    if (!response || response.success === false || !response.device) throw new Error(response && response.message ? response.message : 'Device details could not be loaded.');
                    var d = response.device;
                    var brand = detailValue(d, 'brand', 'Brand') || detailValue(d, 'brandName', 'BrandName');
                    var model = detailValue(d, 'model', 'Model') || detailValue(d, 'modelName', 'ModelName') || detailValue(d, 'displayName', 'DisplayName');
                    var battery = detailValue(d, 'batteryHealthPercentage', 'BatteryHealthPercentage');
                    var status = detailValue(d, 'isSold', 'IsSold') ? 'Sold' : (detailValue(d, 'isAvailableForSale', 'IsAvailableForSale') ? 'Available for Sale' : 'Not for Sale');
                    subtitle.textContent = [brand, model].filter(Boolean).join(' ') || ('Device #' + id);
                    var fields = [
                        ['Brand', brand], ['Model', model], ['Model Number', detailValue(d, 'modelNumber', 'ModelNumber')], ['Category', detailValue(d, 'category', 'Category') || detailValue(d, 'categoryName', 'CategoryName')],
                        ['Storage', detailValue(d, 'gb', 'GB')], ['Color', detailValue(d, 'color', 'Color')], ['Carrier', detailValue(d, 'carrier', 'Carrier')], ['Lock Status', detailValue(d, 'networkStatus', 'NetworkStatus')],
                        ['IMEI / Serial', detailValue(d, 'imeiSerialNumber', 'IMEISerialNumber') || detailValue(d, 'imei', 'IMEI')], ['Battery Health', battery == null ? '\u2014' : battery + '%'],
                        ['Condition', detailValue(d, 'scratchesCondition', 'ScratchesCondition') || detailValue(d, 'condition', 'Condition')], ['Status', status],
                        ['Cost Price', '$' + Number(detailValue(d, 'buyingPrice', 'BuyingPrice') || detailValue(d, 'purchasePrice', 'PurchasePrice') || 0).toFixed(2)], ['Selling Price', '$' + Number(detailValue(d, 'sellingPrice', 'SellingPrice') || 0).toFixed(2)],
                        ['Source', detailValue(d, 'source', 'Source')], ['Notes', detailValue(d, 'notes', 'Notes')]
                    ];
                    body.innerHTML = '<div class="row g-3">' + fields.map(function (field) { return '<div class="col-md-6"><div class="h-100 border rounded-3 p-3 bg-light"><div class="small text-muted text-uppercase fw-semibold mb-1">' + safeText(field[0]) + '</div><div class="fw-semibold text-dark text-break">' + safeText(field[1]) + '</div></div></div>'; }).join('') + '</div>';
                })
                .catch(function (error) { body.innerHTML = '<div class="alert alert-danger mb-0">' + safeText(error.message || 'Device details could not be loaded.') + '</div>'; });
        };
    }

    function installRepairSavePrintAndBackorderPrompt() {
        if (!/^\/Repairs\/Create\/?$/i.test(global.location.pathname)) return;

        var printButton = document.getElementById('confirmAndPrintButton');
        var saveButton = document.getElementById('confirmSaveButton');
        if (printButton && !saveButton) {
            var saveButton = document.createElement('button');
            saveButton.type = 'button';
            saveButton.className = 'btn btn-primary';
            saveButton.id = 'confirmSaveButton';
            saveButton.style.justifyContent = 'center';
            saveButton.innerHTML = '<i class="material-icons">save</i> Save';
            saveButton.onclick = function () { if (typeof global.saveRepair === 'function') global.saveRepair(saveButton); };
            printButton.innerHTML = '<i class="material-icons">print</i> Print';
            printButton.style.justifyContent = 'center';
            var buttonRow = document.createElement('div');
            buttonRow.style.cssText = 'display:grid;grid-template-columns:1fr 1fr;gap:10px;';
            printButton.parentNode.insertBefore(buttonRow, printButton);
            buttonRow.appendChild(saveButton);
            buttonRow.appendChild(printButton);
        }

        // Legacy compiled pages used a combined Print action which silently
        // saved first.  Keep the two actions genuinely separate: Save records
        // the repair and enables Print; Print never creates or updates a repair.
        if (printButton && typeof global.printSavedRepair !== 'function') {
            var legacyCombinedPrint = global.confirmAndPrintLabel;
            printButton.id = 'confirmPrintButton';
            printButton.disabled = true;
            var note = printButton.closest('.intake-summary') && printButton.closest('.intake-summary').querySelector('.intake-print-note');
            if (note) note.innerHTML = '<i class="material-icons" style="font-size:14px;vertical-align:middle;">info</i> Save records the repair without printing. Print becomes available after the repair is saved.';

            global.saveRepairSeparate = async function (button) {
                if (typeof global.validateForm === 'function' && !global.validateForm()) return;
                var originalHtml = button ? button.innerHTML : '';
                if (button) {
                    button.disabled = true;
                    button.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Saving…';
                }
                try {
                    var result = await global.postRepairToServer(global.buildRepairPayload(false));
                    if (!result || !result.success) throw new Error((result && result.message) || 'The repair could not be saved.');
                    global.confirmedRepairResult = result;
                    document.querySelectorAll('#confirmSaveButton, #footerSaveButton').forEach(function (item) {
                        item.disabled = true;
                        item.innerHTML = '<i class="fas fa-check me-1"></i>Saved';
                    });
                    document.querySelectorAll('#confirmPrintButton, #footerPrintButton').forEach(function (item) { item.disabled = false; });
                    if (typeof global.showNotification === 'function') global.showNotification('Repair #' + (result.repairNumber || result.repairId) + ' saved successfully!', 'success');
                } catch (error) {
                    if (button) { button.disabled = false; button.innerHTML = originalHtml; }
                    if (typeof global.showNotification === 'function') global.showNotification('Save failed: ' + error.message, 'error');
                }
            };

            global.printSavedRepair = function (button) {
                if (!global.confirmedRepairResult || !global.confirmedRepairResult.repairId) {
                    if (typeof global.showNotification === 'function') global.showNotification('Save the repair first, then use Print.', 'error');
                    return;
                }
                return legacyCombinedPrint(button);
            };
            saveButton = document.getElementById('confirmSaveButton');
            if (saveButton) saveButton.onclick = function () { global.saveRepairSeparate(saveButton); };
            printButton.onclick = function () { global.printSavedRepair(printButton); };
        }

        if (typeof global.addPart === 'function' && !global.addPart.__empireBackorderConfirm && global.addPart.toString().indexOf('is out of stock. Do you still want') < 0) {
            var originalAddPart = global.addPart;
            global.addPart = function (deviceId, partId, partName, price, maxStock, serviceOnly, allowBackorder) {
                if (allowBackorder && !global.confirm(partName + ' is out of stock. Do you still want to add it to this repair?\n\nThe repair will be placed in Waiting for Parts and cannot be completed until the part is received and assigned.')) return;
                return originalAddPart.apply(this, arguments);
            };
            global.addPart.__empireBackorderConfirm = true;
        }
    }

    function installSaleUpdateCompatibility() {
        if (!global.jQuery || !global.jQuery.ajaxPrefilter) return;
        global.jQuery.ajaxPrefilter(function (options) {
            if (!options || !/\/POS\/UpdateSale(?:\?|$)/i.test(options.url || '') || typeof options.data !== 'string') return;
            try {
                var payload = JSON.parse(options.data);
                (payload.Items || payload.items || []).forEach(function (item) {
                    var repairId = item.RepairId || item.repairId;
                    var inventoryId = item.InventoryItemId || item.inventoryItemId;
                    var custom = item.IsCustomItem === true || item.isCustomItem === true;
                    if (!(item.ItemType || item.itemType)) item.ItemType = repairId ? 'Repair' : (inventoryId ? 'Inventory' : (custom ? 'Custom' : 'Device'));
                    if (item.IsTaxable === undefined && item.isTaxable === undefined) item.IsTaxable = true;
                    if (!(item.ItemStatus || item.itemStatus)) item.ItemStatus = 'In Stock';
                });
                (payload.Payments || payload.payments || []).forEach(function (payment) {
                    var reference = payment.TransactionId || payment.transactionId || payment.ReferenceNumber || payment.referenceNumber || '';
                    payment.TransactionId = reference;
                    payment.ReferenceNumber = reference;
                });
                options.data = JSON.stringify(payload);
            } catch (_) { }
        });
    }

    global.addEventListener('load', function () {
        installDeviceDetailsView();
        installRepairSavePrintAndBackorderPrompt();
        installSaleUpdateCompatibility();
    });

}(window));
