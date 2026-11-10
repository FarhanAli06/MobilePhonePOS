(function (global) {
    'use strict';

    var files = {
        apple: 'apple.svg', samsung: 'samsung.svg', google: 'google.svg',
        oneplus: 'oneplus.svg', lg: 'lg.svg', motorola: 'motorola.svg'
    };

    function key(name) {
        var value = String(name || '').trim().toLowerCase();
        if (/^apple(?:\s|$|\.|,)/.test(value)) return 'apple';
        return value.replace(/[^a-z0-9]/g, '');
    }

    function escapeHtml(value) {
        return String(value || '').replace(/[&<>"']/g, function (character) {
            return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[character];
        });
    }

    function markup(name, className) {
        var normalized = key(name);
        var file = files[normalized];
        if (!file) return escapeHtml(name);
        return '<img class="manufacturer-logo ' + escapeHtml(className || '') + '" src="/images/manufacturers/' + file + '?v=3" alt="' + escapeHtml(name) + ' logo" title="' + escapeHtml(name) + '">';
    }

    function hasLogo(name) {
        return !!files[key(name)];
    }

    function upgrade(root) {
        (root || document).querySelectorAll('.eds-brand-name,.brand-name,.brand-card > span,.eds-banner-brand').forEach(function (element) {
            if (element.querySelector('.manufacturer-logo')) return;
            var name = element.textContent.trim();
            if (!files[key(name)]) return;
            element.innerHTML = markup(name);
        });
    }

    global.EmpireManufacturerLogos = { markup: markup, hasLogo: hasLogo, upgrade: upgrade };
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', function () { upgrade(document); });
    else upgrade(document);
    new MutationObserver(function (changes) {
        changes.forEach(function (change) {
            change.addedNodes.forEach(function (node) {
                if (node.nodeType === 1) upgrade(node.matches && node.matches('.eds-brand-name,.brand-name,.brand-card > span,.eds-banner-brand') ? node.parentNode : node);
            });
        });
    }).observe(document.documentElement, { childList: true, subtree: true });
}(window));
