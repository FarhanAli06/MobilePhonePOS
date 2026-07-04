/**
 * Timezone Helper - Automatic timezone detection and conversion
 * Detects user's timezone and converts UTC dates to local time
 */

(function (window) {
    'use strict';

    const TimezoneHelper = {
        /**
         * Get user's timezone (e.g., "America/Los_Angeles")
         */
        getUserTimezone: function () {
            return Intl.DateTimeFormat().resolvedOptions().timeZone;
        },

        /**
         * Get user's timezone offset in minutes (e.g., -480 for PST)
         */
        getTimezoneOffset: function () {
            return new Date().getTimezoneOffset();
        },

        /**
         * Convert UTC date string to local Date object
         * @param {string} utcDateString - UTC date string from server (e.g., "2026-01-05T19:27:33Z")
         * @returns {Date} Local Date object
         */
        utcToLocal: function (utcDateString) {
            if (!utcDateString) return null;

            // Ensure the string is treated as UTC
            let dateStr = utcDateString;
            if (!dateStr.endsWith('Z') && !dateStr.includes('+') && !dateStr.includes('T')) {
                // If it's just a date without time, add time and Z
                dateStr = dateStr + 'T00:00:00Z';
            } else if (!dateStr.endsWith('Z') && dateStr.includes('T')) {
                // If it has time but no Z, add Z
                dateStr = dateStr + 'Z';
            }

            return new Date(dateStr);
        },

        /**
         * Format date to local string
         * @param {string|Date} date - UTC date string or Date object
         * @param {object} options - Intl.DateTimeFormat options
         * @returns {string} Formatted date string
         */
        formatLocal: function (date, options) {
            if (!date) return '';

            const localDate = typeof date === 'string' ? this.utcToLocal(date) : date;
            if (!localDate || isNaN(localDate.getTime())) return '';

            const defaultOptions = {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                hour12: true
            };

            const formatOptions = { ...defaultOptions, ...options };
            return new Intl.DateTimeFormat('en-US', formatOptions).format(localDate);
        },

        /**
         * Format date to short local string (MM/DD/YYYY)
         * @param {string|Date} date - UTC date string or Date object
         * @returns {string} Formatted date string
         */
        formatShortDate: function (date) {
            return this.formatLocal(date, {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit'
            });
        },

        /**
         * Format date to long local string (MM/DD/YYYY hh:mm:ss AM/PM)
         * @param {string|Date} date - UTC date string or Date object
         * @returns {string} Formatted date string
         */
        formatLongDate: function (date) {
            return this.formatLocal(date, {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                hour12: true
            });
        },

        /**
         * Format date to time only (hh:mm:ss AM/PM)
         * @param {string|Date} date - UTC date string or Date object
         * @returns {string} Formatted time string
         */
        formatTime: function (date) {
            return this.formatLocal(date, {
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit',
                hour12: true
            });
        },

        /**
         * Convert local date to UTC ISO string for sending to server
         * @param {Date} localDate - Local Date object
         * @returns {string} UTC ISO string
         */
        localToUtc: function (localDate) {
            if (!localDate) return null;
            return localDate.toISOString();
        },

        /**
         * Get current local date/time as Date object
         * @returns {Date} Current local date
         */
        now: function () {
            return new Date();
        },

        /**
         * Get current UTC date/time as ISO string
         * @returns {string} Current UTC ISO string
         */
        nowUtc: function () {
            return new Date().toISOString();
        },

        /**
         * Convert all dates in a container to local timezone
         * Looks for elements with data-utc-date attribute
         * @param {string|HTMLElement} container - Container selector or element
         */
        convertDatesInContainer: function (container) {
            const containerElement = typeof container === 'string' 
                ? document.querySelector(container) 
                : container;

            if (!containerElement) return;

            const dateElements = containerElement.querySelectorAll('[data-utc-date]');
            dateElements.forEach(element => {
                const utcDate = element.getAttribute('data-utc-date');
                const format = element.getAttribute('data-date-format') || 'long';

                let formattedDate;
                switch (format) {
                    case 'short':
                        formattedDate = this.formatShortDate(utcDate);
                        break;
                    case 'time':
                        formattedDate = this.formatTime(utcDate);
                        break;
                    case 'long':
                    default:
                        formattedDate = this.formatLongDate(utcDate);
                        break;
                }

                element.textContent = formattedDate;
            });
        },

        /**
         * Initialize timezone helper - convert all dates on page load
         */
        init: function () {
            // Convert dates when DOM is ready
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', () => {
                    this.convertDatesInContainer(document.body);
                });
            } else {
                this.convertDatesInContainer(document.body);
            }

            // Log timezone info for debugging
            console.log('Timezone Helper Initialized');
            console.log('User Timezone:', this.getUserTimezone());
            console.log('Timezone Offset:', this.getTimezoneOffset(), 'minutes');
        }
    };

    // Expose to window
    window.TimezoneHelper = TimezoneHelper;

    // Auto-initialize
    TimezoneHelper.init();

})(window);
