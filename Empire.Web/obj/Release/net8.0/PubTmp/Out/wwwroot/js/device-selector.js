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

    function _dispatch(eventName, detail) {
        document.dispatchEvent(new CustomEvent(eventName, { detail: detail, bubbles: true }));
    }

    function _writeHidden(id, value) {
        var el = _el(id);
        if (el) el.value = (value != null) ? value : '';
    }

    function _state(sid) {
        if (!_states[sid]) {
            _states[sid] = {
                brand: null, brandName: '',
                category: null, categoryName: '',
                model: null, modelName: '',
                issue: '',
                selectedParts: [],
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
        grid.innerHTML = brands.map(function(b) {
            return '<div class="eds-brand-card" data-id="' + b.id + '" onclick="EmpireDeviceSelector.selectBrand(\'' + sid + '\',' + b.id + ',\'' + (b.name || '').replace(/'/g, "\\'") + '\')">'
                + '<div class="eds-brand-icon" style="background:' + b.color + '">'
                + '<i class="' + (b.icon || 'fas fa-mobile-alt') + '"></i></div>'
                + '<div class="eds-brand-name">' + b.name + '</div>'
                + '</div>';
        }).join('');
    }

    function _renderCategories(sid, categories) {
        var grid = _el(sid + '-categories');
        if (!grid) return;
        if (!categories.length) {
            grid.innerHTML = '<p class="eds-empty">No categories found.</p>';
            return;
        }
        grid.innerHTML = categories.map(function(c) {
            return '<div class="eds-category-card" data-id="' + c.id + '" onclick="EmpireDeviceSelector.selectCategory(\'' + sid + '\',' + c.id + ',\'' + (c.name || '').replace(/'/g, "\\'") + '\')">'
                + '<i class="material-icons eds-category-icon">' + (c.icon || 'devices_other') + '</i>'
                + '<div class="eds-category-name">' + c.name + '</div>'
                + '</div>';
        }).join('');
    }

    function _renderModels(sid, models) {
        var grid = _el(sid + '-models');
        if (!grid) return;
        if (!models.length) {
            grid.innerHTML = '<div class="eds-empty">'
                + '<i class="fas fa-mobile-alt fa-2x mb-2"></i><p>No models found.</p>'
                + '<button type="button" class="btn btn-sm btn-outline-secondary mt-1" onclick="EmpireDeviceSelector.manualModel(\'' + sid + '\')">'
                + '<i class="fas fa-plus me-1"></i>Add Manually</button></div>';
            return;
        }
        grid.innerHTML = models.map(function(m) {
            return '<div class="eds-model-card" data-id="' + m.id + '" onclick="EmpireDeviceSelector.selectModel(\'' + sid + '\',' + m.id + ',\'' + (m.name || '').replace(/'/g, "\\'") + '\')">'
                + '<div class="eds-model-icon">' + (m.icon || '📱') + '</div>'
                + '<div class="eds-model-name">' + m.name + '</div>'
                + '</div>';
        }).join('');
    }

    function _renderParts(sid, parts) {
        var grid = _el(sid + '-parts');
        if (!grid) return;
        var st = _state(sid);
        if (!parts.length) {
            grid.innerHTML = '<p class="eds-empty">No parts found for this selection.</p>';
            return;
        }
        grid.innerHTML = parts.map(function(p) {
            var selected = st.selectedParts.some(function(x) { return x.id === p.id; });
            return '<div class="eds-part-card' + (selected ? ' selected' : '') + '" data-id="' + p.id + '" onclick="EmpireDeviceSelector.togglePart(\'' + sid + '\',' + JSON.stringify(p).replace(/"/g, '&quot;') + ')">'
                + '<div class="eds-part-icon">' + _partIcon(p.name) + '</div>'
                + '<div class="eds-part-name">' + p.name + '</div>'
                + '<div class="eds-part-price">$' + parseFloat(p.price || 0).toFixed(2) + '</div>'
                + '<div class="eds-part-stock ' + (p.inStock ? 'in-stock' : 'out-stock') + '">'
                + (p.inStock ? '<i class="fas fa-check-circle me-1"></i>In Stock' : '<i class="fas fa-times-circle me-1"></i>Out of Stock')
                + '</div></div>';
        }).join('');
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
        if (cb) cb.textContent = st.brandName    || '';
        if (cc) cc.textContent = st.categoryName || '';
        if (cm) cm.textContent = st.modelName    || '';
        var seps = bc.querySelectorAll('.eds-crumb-sep');
        if (seps[0]) seps[0].style.display = (st.brand    && st.category) ? '' : 'none';
        if (seps[1]) seps[1].style.display = (st.category && st.model)    ? '' : 'none';
    }

    // ── HTML builder ──────────────────────────────────────────────────────────
    function _buildHtml(sid, showParts, showIssue) {
        var html = '<div class="empire-device-selector" id="' + sid + '-wrapper">'
            + '<div class="eds-step eds-step--active" id="' + sid + '-step1">'
            +   '<h6 class="eds-step-title"><i class="fas fa-tag me-2"></i>Select Brand</h6>'
            +   '<div class="eds-brand-grid" id="' + sid + '-brands"><div class="eds-loading"><i class="fas fa-spinner fa-spin me-2"></i>Loading brands…</div></div>'
            + '</div>'
            + '<div class="eds-step" id="' + sid + '-step2">'
            +   '<div class="eds-step-header">'
            +   '<button type="button" class="eds-back-btn" onclick="EmpireDeviceSelector.goToStep(\'' + sid + '\',1)"><i class="fas fa-arrow-left"></i></button>'
            +   '<h6 class="eds-step-title"><i class="fas fa-th-large me-2"></i>Select Device Type</h6></div>'
            +   '<div class="eds-category-grid" id="' + sid + '-categories"><div class="eds-loading"><i class="fas fa-spinner fa-spin me-2"></i>Loading…</div></div>'
            + '</div>'
            + '<div class="eds-step" id="' + sid + '-step3">'
            +   '<div class="eds-step-header">'
            +   '<button type="button" class="eds-back-btn" onclick="EmpireDeviceSelector.goToStep(\'' + sid + '\',2)"><i class="fas fa-arrow-left"></i></button>'
            +   '<h6 class="eds-step-title"><i class="fas fa-mobile-alt me-2"></i>Select Model</h6></div>'
            +   '<div class="eds-model-grid" id="' + sid + '-models"><div class="eds-loading"><i class="fas fa-spinner fa-spin me-2"></i>Loading…</div></div>'
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

            container.innerHTML = _buildHtml(sid, showParts, showIssue);

            var st = _state(sid);
            // Reset state for a fresh create (e.g. modal re-open)
            st.brand = null; st.brandName = '';
            st.category = null; st.categoryName = '';
            st.model = null; st.modelName = '';
            st.issue = ''; st.selectedParts = [];
            st.options = {
                showParts:       showParts,
                showIssue:       showIssue,
                hiddenBrand:     opts.hiddenBrand    || '',
                hiddenCategory:  opts.hiddenCategory || '',
                hiddenModel:     opts.hiddenModel    || '',
                onModelSelected: opts.onModelSelected || null,
                onPartToggled:   opts.onPartToggled   || null
            };
            _loadBrands(function(brands) { _renderBrands(sid, brands); });
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
            st.model = null; st.modelName = ''; st.selectedParts = [];

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
            st.model = null; st.modelName = ''; st.selectedParts = [];

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
            _loadModels(st.brand, categoryId, function(models) { _renderModels(sid, models); });
        },

        selectModel: function(sid, modelId, modelName) {
            var st = _state(sid);
            st.model = modelId; st.modelName = modelName;
            st.selectedParts = [];

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
                        + '<div><div class="eds-banner-brand">' + (st.brandName || '') + '</div>'
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

}(window));
