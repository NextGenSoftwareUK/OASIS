/**
 * STAR API Configuration Helper
 * Provides centralized configuration for STAR API endpoints
 */

/**
 * Get STAR API Base URL
 * Returns the appropriate STAR API URL based on environment
 */
function getSTARAPIBaseURL() {
    const hostname = window.location.hostname;
    const protocol = window.location.protocol;
    
    // Development environment
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
        return 'http://localhost:50564'; // STAR API dev port
    }
    
    // Production environment (not live yet - will need to be configured when deployed)
    // For now, fallback to development port if on localhost-like domains
    if (protocol === 'https:') {
        // Production URL not live yet - using development for now
        return 'http://localhost:50564';
    }
    return 'http://localhost:50564';
}

/**
 * Get OASIS API Base URL (for reference)
 * Returns the appropriate OASIS API URL based on environment
 */
function getOASISAPIBaseURL() {
    const hostname = window.location.hostname;
    const protocol = window.location.protocol;
    
    // Development environment
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
        return 'https://localhost:5000'; // OASIS API dev port
    }
    
    // Production environment
    if (protocol === 'https:') {
        return 'https://api.oasisplatform.world';
    }
    return 'http://api.oasisplatform.world';
}

/**
 * Get authentication token from localStorage
 */
function getAuthToken() {
    try {
        const authData = localStorage.getItem('oasis_auth');
        if (authData) {
            const auth = JSON.parse(authData);
            return auth.token || null;
        }
    } catch (e) {
        console.error('Error parsing auth data:', e);
    }
    return null;
}

/**
 * Get avatar ID from localStorage
 */
function getAvatarId() {
    try {
        const authData = localStorage.getItem('oasis_auth');
        if (authData) {
            const auth = JSON.parse(authData);
            return auth.avatarId || auth.avatar?.id || auth.avatar?.avatarId || null;
        }
    } catch (e) {
        console.error('Error parsing auth data:', e);
    }
    return null;
}

/**
 * Get authorization headers for API requests
 */
function getAuthHeaders() {
    const token = getAuthToken();
    const headers = {
        'Content-Type': 'application/json'
    };
    
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }
    
    return headers;
}

/**
 * Make authenticated API request to STAR API
 */
async function starAPIRequest(endpoint, options = {}) {
    const baseUrl = getSTARAPIBaseURL();
    const url = `${baseUrl}${endpoint.startsWith('/') ? endpoint : '/' + endpoint}`;
    
    const defaultOptions = {
        headers: getAuthHeaders(),
        ...options
    };
    
    // Merge headers if provided
    if (options.headers) {
        defaultOptions.headers = {
            ...defaultOptions.headers,
            ...options.headers
        };
    }
    
    try {
        const response = await fetch(url, defaultOptions);
        
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({ message: `HTTP ${response.status}: ${response.statusText}` }));
            throw new Error(errorData.message || `API request failed: ${response.status} ${response.statusText}`);
        }
        
        return await response.json();
    } catch (error) {
        // Don't log connection refused errors (API not running) - they're expected in development
        // API errors (400, 401, 500, etc.) are logged but handled gracefully by callers
        if (error.message && !error.message.includes('Failed to fetch') && !error.message.includes('ERR_CONNECTION_REFUSED')) {
            // Only log non-connection errors at debug level (less noisy)
            if (error.message.includes('400') || error.message.includes('401')) {
                // 400/401 errors are usually auth/data issues - handled by callers
            } else {
                console.error('STAR API request failed:', error);
            }
        }
        throw error;
    }
}

// Export to window for global access
if (typeof window !== 'undefined') {
    window.getSTARAPIBaseURL = getSTARAPIBaseURL;
    window.getOASISAPIBaseURL = getOASISAPIBaseURL;
    window.getAuthToken = getAuthToken;
    window.getAvatarId = getAvatarId;
    window.getAuthHeaders = getAuthHeaders;
    window.starAPIRequest = starAPIRequest;
}


