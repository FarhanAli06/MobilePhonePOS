/**
 * Secure Authentication Context Manager
 * Fetches user and shop context from API endpoint (token stored in HttpOnly cookie)
 * Provides global access to user context without exposing JWT token
 * 
 * Security Features:
 * - JWT token stored in HttpOnly cookie (not accessible to JavaScript)
 * - XSS protection (no token in localStorage/sessionStorage)
 * - CSRF protection via SameSite cookie attribute
 * - Automatic context refresh
 */

(function(window) {
    'use strict';

    // Get API base URL from configuration (injected by _Layout.cshtml from appsettings.json)
    // window.API_BASE_URL is set by _Layout.cshtml: ApiSettings:BaseUrl + '/api'
    const API_BASE_URL = window.API_BASE_URL || '';
    // Web-side base URL for session-based endpoints (no JWT required from browser JS)
    const WEB_BASE_URL = window.WEB_BASE_URL || window.location.origin;
    
    // Global context object
    window.AuthContext = {
        userId: null,
        userName: null,
        email: null,
        firstName: null,
        lastName: null,
        fullName: null,
        shopId: null,
        shopName: null,
        role: null,
        roleId: null,
        isAuthenticated: false,
        isLoading: false,
        error: null
    };

    // Event system for context changes
    const contextChangeListeners = [];

    /**
     * Register a listener for context changes
     */
    window.AuthContext.onChange = function(callback) {
        if (typeof callback === 'function') {
            contextChangeListeners.push(callback);
        }
    };

    /**
     * Notify all listeners of context change
     */
    function notifyContextChange() {
        contextChangeListeners.forEach(callback => {
            try {
                callback(AuthContext);
            } catch (error) {
                console.error('Error in context change listener:', error);
            }
        });
    }

    /**
     * Fetch user context from API endpoint
     * Token is automatically sent via HttpOnly cookie
     */
    async function fetchContext() {
        AuthContext.isLoading = true;
        AuthContext.error = null;

        try {
            // Use the Web-side context endpoint — reads from server-side session, no JWT needed
            const response = await fetch(`${WEB_BASE_URL}/api/auth/context`, {
                method: 'GET',
                credentials: 'include', // Important: Include cookies in request
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                if (response.status === 401) {
                    // Unauthorized - clear context
                    clearContext();
                    return false;
                }
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            
            // Check if response has success property (API wrapper format)
            const context = data.success ? data.data : data;

            // Populate context from API response
            AuthContext.userId = context.userId || null;
            AuthContext.userName = context.userName || null;
            AuthContext.email = context.email || null;
            AuthContext.firstName = context.firstName || null;
            AuthContext.lastName = context.lastName || null;
            AuthContext.fullName = context.fullName || null;
            AuthContext.shopId = context.shopId || null;
            AuthContext.shopName = context.shopName || null;
            AuthContext.role = context.role || null;
            AuthContext.roleId = context.roleId || null;
            AuthContext.isAuthenticated = true;
            AuthContext.isLoading = false;

            console.log('Auth context loaded:', {
                userId: AuthContext.userId,
                userName: AuthContext.userName,
                shopId: AuthContext.shopId,
                shopName: AuthContext.shopName,
                role: AuthContext.role
            });

            // Notify listeners
            notifyContextChange();

            return true;

        } catch (error) {
            console.error('Error fetching auth context:', error);
            AuthContext.error = error.message;
            AuthContext.isLoading = false;
            AuthContext.isAuthenticated = false;
            
            // Notify listeners of error
            notifyContextChange();
            
            return false;
        }
    }

    /**
     * Clear auth context
     */
    function clearContext() {
        AuthContext.userId = null;
        AuthContext.userName = null;
        AuthContext.email = null;
        AuthContext.firstName = null;
        AuthContext.lastName = null;
        AuthContext.fullName = null;
        AuthContext.shopId = null;
        AuthContext.shopName = null;
        AuthContext.role = null;
        AuthContext.roleId = null;
        AuthContext.isAuthenticated = false;
        AuthContext.isLoading = false;
        AuthContext.error = null;

        // Notify listeners
        notifyContextChange();
    }

    /**
     * Refresh auth context (call after login or when needed)
     */
    window.AuthContext.refresh = async function() {
        return await fetchContext();
    };

    /**
     * Clear auth context and logout
     */
    window.AuthContext.logout = async function() {
        try {
            // Call API logout endpoint to clear HttpOnly cookie
            await fetch(`${API_BASE_URL}/auth/logout`, {
                method: 'POST',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json'
                }
            });
        } catch (error) {
            console.error('Error during logout:', error);
        } finally {
            // Clear local context regardless of API call result
            clearContext();
            
            // Redirect to login page
            window.location.href = '/Home/Login';
        }
    };

    /**
     * Check if user has specific role
     */
    window.AuthContext.hasRole = function(role) {
        return AuthContext.role && AuthContext.role.toLowerCase() === role.toLowerCase();
    };

    /**
     * Check if user is admin
     */
    window.AuthContext.isAdmin = function() {
        return AuthContext.hasRole('Admin') || AuthContext.hasRole('SuperAdmin');
    };

    /**
     * Check if user is manager
     */
    window.AuthContext.isManager = function() {
        return AuthContext.hasRole('Manager') || AuthContext.isAdmin();
    };

    /**
     * Wait for context to be loaded
     * Useful for scripts that need context immediately
     */
    window.AuthContext.ready = function() {
        return new Promise((resolve) => {
            if (!AuthContext.isLoading) {
                resolve(AuthContext);
            } else {
                const checkInterval = setInterval(() => {
                    if (!AuthContext.isLoading) {
                        clearInterval(checkInterval);
                        resolve(AuthContext);
                    }
                }, 50);
            }
        });
    };

    /**
     * Initialize context on page load
     */
    async function initialize() {
        // Fetch context from API
        await fetchContext();
        
        // Auto-refresh every 5 minutes to keep context fresh
        setInterval(() => {
            if (AuthContext.isAuthenticated) {
                fetchContext();
            }
        }, 5 * 60 * 1000); // 5 minutes
    }

    // Initialize on DOMContentLoaded
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        // DOM already loaded
        initialize();
    }

})(window);
