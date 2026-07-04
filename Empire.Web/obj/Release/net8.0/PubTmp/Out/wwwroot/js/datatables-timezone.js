/**
 * DataTables Timezone Plugin
 * Automatically converts UTC dates to local timezone in DataTables
 */

(function ($) {
    'use strict';

    // Custom DataTables render function for UTC dates
    $.fn.dataTable.render.utcDate = function (format) {
        return function (data, type, row) {
            if (!data) return '';

            // For sorting and filtering, use the original data
            if (type === 'sort' || type === 'type') {
                return data;
            }

            // For display, convert to local timezone
            if (type === 'display') {
                if (typeof TimezoneHelper !== 'undefined') {
                    switch (format) {
                        case 'short':
                            return TimezoneHelper.formatShortDate(data);
                        case 'time':
                            return TimezoneHelper.formatTime(data);
                        case 'long':
                        default:
                            return TimezoneHelper.formatLongDate(data);
                    }
                }
                // Fallback if TimezoneHelper not loaded
                return new Date(data).toLocaleString();
            }

            return data;
        };
    };

    // Helper function to add timezone conversion to DataTable columns
    $.fn.dataTable.ext.addTimezoneConversion = function (columnDefs) {
        if (!columnDefs) return [];

        return columnDefs.map(function (colDef) {
            // If column is marked as date column, add timezone conversion
            if (colDef.isDate) {
                colDef.render = $.fn.dataTable.render.utcDate(colDef.dateFormat || 'long');
            }
            return colDef;
        });
    };

})(jQuery);
