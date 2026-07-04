/**
 * Timezone Interceptor
 * Automatically sends user's timezone information with every AJAX request
 * This allows the server to convert UTC times to user's local timezone
 */

(function () {
    'use strict';

    // Get user's timezone information
    function getTimezoneInfo() {
        try {
            // Get timezone ID (e.g., "America/Los_Angeles")
            const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
            
            // Get timezone offset in minutes (e.g., -480 for PST)
            const offset = new Date().getTimezoneOffset();
            
            return {
                timezone: timezone,
                offset: offset
            };
        } catch (e) {
            console.error('Error getting timezone info:', e);
            return {
                timezone: 'UTC',
                offset: 0
            };
        }
    }

    // Store timezone info
    const timezoneInfo = getTimezoneInfo();
    
    console.log('Timezone Interceptor Initialized');
    console.log('Timezone:', timezoneInfo.timezone);
    console.log('Offset:', timezoneInfo.offset, 'minutes');

    // Intercept jQuery AJAX requests
    if (typeof jQuery !== 'undefined') {
        jQuery.ajaxSetup({
            beforeSend: function(xhr) {
                xhr.setRequestHeader('X-Timezone', timezoneInfo.timezone);
                xhr.setRequestHeader('X-Timezone-Offset', timezoneInfo.offset);
            }
        });
    }

    // Intercept fetch requests
    const originalFetch = window.fetch;
    window.fetch = function(url, options) {
        options = options || {};
        options.headers = options.headers || {};
        
        // Add timezone headers
        if (options.headers instanceof Headers) {
            options.headers.append('X-Timezone', timezoneInfo.timezone);
            options.headers.append('X-Timezone-Offset', timezoneInfo.offset);
        } else {
            options.headers['X-Timezone'] = timezoneInfo.timezone;
            options.headers['X-Timezone-Offset'] = timezoneInfo.offset;
        }
        
        return originalFetch(url, options);
    };

    // Intercept XMLHttpRequest
    const originalOpen = XMLHttpRequest.prototype.open;
    const originalSend = XMLHttpRequest.prototype.send;
    
    XMLHttpRequest.prototype.open = function() {
        this._url = arguments[1];
        return originalOpen.apply(this, arguments);
    };
    
    XMLHttpRequest.prototype.send = function() {
        this.setRequestHeader('X-Timezone', timezoneInfo.timezone);
        this.setRequestHeader('X-Timezone-Offset', timezoneInfo.offset);
        return originalSend.apply(this, arguments);
    };

    // Set cookie for server-side access on page loads
    document.cookie = "UserTimezone=" + timezoneInfo.timezone + "; path=/; max-age=31536000"; // 1 year
    document.cookie = "UserTimezoneOffset=" + timezoneInfo.offset + "; path=/; max-age=31536000";

    // Expose globally
    window.UserTimezone = timezoneInfo;

})();
