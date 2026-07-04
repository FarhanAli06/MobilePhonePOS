/**
 * Phone Formatter - Global phone number formatting utility
 * Automatically formats international phone numbers for display
 */

(function (window) {
    'use strict';

    const PhoneFormatter = {
        /**
         * Format international phone number for display
         * @param {string} phoneNumber - International phone number (e.g., "+12345678900")
         * @returns {string} Formatted phone number
         */
        format: function (phoneNumber) {
            if (!phoneNumber || phoneNumber.trim() === '') {
                return '';
            }

            // Remove all non-numeric characters except +
            let cleaned = phoneNumber.replace(/[^\d+]/g, '');

            // If it doesn't start with +, assume it's a US number
            if (!cleaned.startsWith('+')) {
                cleaned = '+1' + cleaned;
            }

            // Try to format using intl-tel-input if available
            if (window.intlTelInputUtils) {
                try {
                    return window.intlTelInputUtils.formatNumber(
                        cleaned,
                        null,
                        window.intlTelInputUtils.numberFormat.INTERNATIONAL
                    );
                } catch (e) {
                    console.warn('Error formatting phone number:', e);
                }
            }

            // Fallback formatting based on country code
            return this.fallbackFormat(cleaned);
        },

        /**
         * Fallback formatting when intl-tel-input utils not available
         * @param {string} phoneNumber - Cleaned phone number with country code
         * @returns {string} Formatted phone number
         */
        fallbackFormat: function (phoneNumber) {
            // US/Canada (+1)
            if (phoneNumber.startsWith('+1')) {
                const digits = phoneNumber.substring(2);
                if (digits.length === 10) {
                    return `+1 (${digits.substring(0, 3)}) ${digits.substring(3, 6)}-${digits.substring(6)}`;
                }
            }

            // UK (+44)
            if (phoneNumber.startsWith('+44')) {
                const digits = phoneNumber.substring(3);
                if (digits.length === 10) {
                    return `+44 ${digits.substring(0, 4)} ${digits.substring(4, 7)} ${digits.substring(7)}`;
                }
            }

            // Australia (+61)
            if (phoneNumber.startsWith('+61')) {
                const digits = phoneNumber.substring(3);
                if (digits.length === 9) {
                    return `+61 ${digits.substring(0, 1)} ${digits.substring(1, 5)} ${digits.substring(5)}`;
                }
            }

            // Default: Just add spaces every 3-4 digits after country code
            const match = phoneNumber.match(/^(\+\d{1,3})(\d+)$/);
            if (match) {
                const countryCode = match[1];
                const number = match[2];
                const formatted = number.match(/.{1,4}/g)?.join(' ') || number;
                return `${countryCode} ${formatted}`;
            }

            return phoneNumber;
        },

        /**
         * Format all phone numbers in a container
         * Looks for elements with data-phone attribute
         * @param {string|HTMLElement} container - Container selector or element
         */
        formatPhoneNumbersInContainer: function (container) {
            const containerElement = typeof container === 'string'
                ? document.querySelector(container)
                : container;

            if (!containerElement) return;

            const phoneElements = containerElement.querySelectorAll('[data-phone]');
            phoneElements.forEach(element => {
                const phoneNumber = element.getAttribute('data-phone');
                const formatted = this.format(phoneNumber);
                element.textContent = formatted;
            });
        },

        /**
         * Initialize phone formatter - format all phones on page load
         */
        init: function () {
            // Format phones when DOM is ready
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', () => {
                    this.formatPhoneNumbersInContainer(document.body);
                });
            } else {
                this.formatPhoneNumbersInContainer(document.body);
            }

            console.log('Phone Formatter Initialized');
        },

        /**
         * Detect country from phone number
         * @param {string} phoneNumber - International phone number
         * @returns {string} Country code (e.g., 'us', 'gb', 'au')
         */
        detectCountry: function (phoneNumber) {
            if (!phoneNumber) return 'us';

            const cleaned = phoneNumber.replace(/[^\d+]/g, '');

            const countryMap = {
                '+1': 'us',
                '+44': 'gb',
                '+61': 'au',
                '+91': 'in',
                '+86': 'cn',
                '+81': 'jp',
                '+49': 'de',
                '+33': 'fr',
                '+39': 'it',
                '+34': 'es',
                '+7': 'ru',
                '+55': 'br',
                '+52': 'mx',
                '+27': 'za',
                '+82': 'kr'
            };

            for (const [code, country] of Object.entries(countryMap)) {
                if (cleaned.startsWith(code)) {
                    return country;
                }
            }

            return 'us'; // Default
        }
    };

    // Expose to window
    window.PhoneFormatter = PhoneFormatter;

    // Auto-initialize
    PhoneFormatter.init();

})(window);
