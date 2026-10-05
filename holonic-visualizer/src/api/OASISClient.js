/**
 * OASIS API Client
 * Handles authentication and API requests to the OASIS API
 */
export class OASISClient {
    constructor(config = {}) {
        // Use local API by default, fallback to environment variable or remote
        // Use HTTP for localhost to avoid HTTP/2 protocol errors in browser
        const defaultUrl = import.meta.env.VITE_OASIS_API_URL || 'http://localhost:5003';
        this.baseUrl = config.baseUrl || defaultUrl;
        this.username = config.username || import.meta.env.VITE_OASIS_USERNAME || 'OASIS_ADMIN';
        this.password = config.password || import.meta.env.VITE_OASIS_PASSWORD || 'Uppermall1!';
        this.token = null;
        this.tokenExpiry = null;
    }

    /**
     * Authenticate with OASIS API
     * @returns {Promise<string>} JWT token
     */
    async authenticate() {
        try {
            // Ensure we're using HTTP for localhost (not HTTPS) to avoid HTTP/2 protocol errors
            let url = `${this.baseUrl}/api/avatar/authenticate`;
            if (url.includes('localhost') && url.startsWith('https://')) {
                url = url.replace('https://', 'http://');
                url = url.replace(':5004', ':5003');
                console.warn(`Switched to HTTP for localhost authentication: ${url}`);
            }
            
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    username: this.username,
                    password: this.password
                })
            });

            // Get text first to handle HTTP/2 errors
            let text = '';
            try {
                text = await response.text();
            } catch (textError) {
                // If we can't read text but status is OK, it might be HTTP/2 protocol error
                if (response.ok || response.status === 200) {
                    throw new Error('HTTP/2 protocol error - could not read authentication response');
                }
                throw new Error(`Authentication failed: ${response.status} ${response.statusText}`);
            }
            
            let data;
            try {
                data = text ? JSON.parse(text) : {};
            } catch (e) {
                throw new Error(`Authentication failed: Invalid JSON response - ${text.substring(0, 100)}`);
            }

            if (!response.ok && response.status !== 200) {
                throw new Error(`Authentication failed: ${response.status} ${response.statusText} - ${data.message || ''}`);
            }
            
            // Handle nested result structure from OASIS API
            const jwtToken = data?.result?.result?.jwtToken || 
                           data?.result?.jwtToken || 
                           data?.jwtToken;
            
            if (!jwtToken) {
                console.error('Authentication response:', data);
                throw new Error('Authentication failed: No token received');
            }

            this.token = jwtToken;
            // Token expires in 15 minutes, but refresh 30 seconds early
            this.tokenExpiry = Date.now() + (15 * 60 * 1000);
            return jwtToken;
        } catch (error) {
            console.error('Authentication error:', error);
            throw new Error(`Failed to authenticate: ${error.message}`);
        }
    }

    /**
     * Get a valid token, refreshing if necessary
     * @returns {Promise<string>} JWT token
     */
    async getValidToken() {
        const bufferTime = 30 * 1000; // 30 seconds buffer
        if (!this.token || !this.tokenExpiry || Date.now() >= (this.tokenExpiry - bufferTime)) {
            await this.authenticate();
        }
        return this.token;
    }

    /**
     * Make an authenticated API request
     * @param {string} endpoint - API endpoint path
     * @param {object} options - Fetch options
     * @returns {Promise<object>} Response data
     */
    async request(endpoint, options = {}) {
        const token = await this.getValidToken();
        
        // Ensure we're using HTTP for localhost (not HTTPS) to avoid HTTP/2 protocol errors
        let url = `${this.baseUrl}${endpoint}`;
        if (url.includes('localhost') && url.startsWith('https://')) {
            url = url.replace('https://', 'http://');
            // Also update port if it's 5004 (HTTPS) to 5003 (HTTP)
            url = url.replace(':5004', ':5003');
            console.warn(`Switched to HTTP for localhost: ${url}`);
        }
        
        try {
            const response = await fetch(url, {
                ...options,
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json',
                    ...options.headers
                }
            });

            // Read response text - handle chunked encoding and large responses
            let text = '';
            try {
                text = await response.text();
            } catch (textError) {
                // Handle ERR_INCOMPLETE_CHUNKED_ENCODING and other read errors
                const isChunkedError = textError.message.includes('ERR_INCOMPLETE_CHUNKED_ENCODING') || 
                                     textError.message.includes('chunked') ||
                                     textError.name === 'TypeError';
                
                if (!response.ok && response.status !== 200) {
                    if (response.status === 401) {
                        await this.authenticate();
                        return this.request(endpoint, options);
                    }
                    throw new Error(`API request failed: ${response.status} ${response.statusText}`);
                }
                
                // For chunked encoding errors with 200 status, the response might be too large
                // Return a helpful error message
                if (isChunkedError && (response.ok || response.status === 200)) {
                    console.warn(`Response too large or incomplete for ${endpoint}. Status: ${response.status}, Error:`, textError.message);
                    // Return empty structure with a helpful message
                    return { 
                        result: { result: [] }, 
                        isError: false, 
                        message: 'Response too large - try requesting specific holon types or use pagination',
                        errorType: 'INCOMPLETE_CHUNKED_ENCODING'
                    };
                }
                
                // Other errors
                console.warn(`Could not read response text for ${endpoint}, but status was ${response.status}. Error:`, textError.message);
                return { result: { result: [] }, isError: false, message: `Failed to read response: ${textError.message}` };
            }

            // Try to parse as JSON
            let data;
            try {
                data = text ? JSON.parse(text) : {};
            } catch (parseError) {
                console.warn('JSON parse failed for', endpoint, 'Response preview:', text.substring(0, 200));
                // If response was OK but JSON parse failed, return empty structure
                if (response.ok || response.status === 200) {
                    return { result: { result: [] }, isError: false, message: 'Failed to parse response' };
                }
                throw new Error(`Failed to parse response: ${parseError.message}`);
            }

            // Check if response indicates an error
            if (!response.ok && response.status !== 200) {
                if (response.status === 401) {
                    await this.authenticate();
                    return this.request(endpoint, options);
                }
                // For 400 errors, include the actual error message from the API
                const errorMessage = data?.message || data?.Message || data?.result?.message || data?.result?.Message || response.statusText;
                const errorDetails = data?.innerMessages || data?.InnerMessages || data?.result?.innerMessages || [];
                // ASP.NET Core validation errors are in data.errors
                const validationErrors = data?.errors || {};
                const validationErrorText = Object.keys(validationErrors).length > 0 
                    ? JSON.stringify(validationErrors, null, 2)
                    : 'No validation errors found';
                
                console.error(`API Error (${response.status}):`, {
                    message: errorMessage,
                    details: errorDetails,
                    validationErrors: validationErrorText,
                    fullResponse: JSON.stringify(data, null, 2)
                });
                throw new Error(`API request failed: ${response.status} ${response.statusText} - ${errorMessage}\nValidation Errors: ${validationErrorText}`);
            }

            return data;
        } catch (error) {
            // Handle network errors, HTTP/2 protocol errors, etc.
            // HTTP/2 protocol errors can occur with HTTPS on localhost but the request might still succeed
            // The browser shows ERR_HTTP2_PROTOCOL_ERROR but curl shows it works
            if (error.message.includes('Failed to fetch') || error.message.includes('ERR_HTTP2') || error.name === 'TypeError') {
                console.warn(`Network/protocol error for ${endpoint}:`, error.message);
                // Return empty result structure instead of throwing
                // This allows the UI to show "no data" instead of crashing
                return { result: { result: [] }, isError: false, message: `Network error: ${error.message}` };
            }
            throw error;
        }
    }

    /**
     * Get all holons from OASIS with custom options (always uses POST)
     * @param {string|number} holonType - Filter by holon type (e.g., 'Avatar', 'OAPP', 74, 3, 'All')
     * @param {object} options - Request options (LoadChildren, Recursive, etc.)
     * @returns {Promise<Array>} Array of holon objects
     */
    async getAllHolonsWithOptions(holonType = 'All', options = {}) {
        try {
            const data = await this.request('/api/data/load-all-holons', {
                method: 'POST',
                body: JSON.stringify({
                    HolonType: holonType || 'All',
                    LoadChildren: options.LoadChildren !== undefined ? options.LoadChildren : false,
                    Recursive: options.Recursive !== undefined ? options.Recursive : false,
                    MaxChildDepth: options.MaxChildDepth !== undefined ? options.MaxChildDepth : 0,
                    ContinueOnError: options.ContinueOnError !== undefined ? options.ContinueOnError : true,
                    Version: options.Version !== undefined ? options.Version : 0,
                    ProviderKey: options.ProviderKey || 'MongoOASIS'
                })
            });
            
            // Handle nested response structure
            if (data.result?.result && Array.isArray(data.result.result)) {
                console.log(`getAllHolonsWithOptions('${holonType}') returned ${data.result.result.length} holons from data.result.result`);
                return data.result.result;
            }
            if (data.result && Array.isArray(data.result)) {
                console.log(`getAllHolonsWithOptions('${holonType}') returned ${data.result.length} holons from data.result`);
                return data.result;
            }
            if (Array.isArray(data)) {
                console.log(`getAllHolonsWithOptions('${holonType}') returned ${data.length} holons from data`);
                return data;
            }
            console.warn(`getAllHolonsWithOptions('${holonType}') - no array found in response:`, data);
            return [];
        } catch (error) {
            console.error(`Failed to fetch holons (type: ${holonType}):`, error);
            return [];
        }
    }

    /**
     * Get all holons from OASIS
     * @param {string} holonType - Optional: filter by holon type (e.g., 'Avatar', 'OAPP', 'All')
     * @returns {Promise<Array>} Array of holon objects
     */
    async getAllHolons(holonType = 'All') {
        try {
            // Use the correct endpoint: /api/data/load-all-holons
            // Can use GET with holonType parameter or POST with request body
            // Handle both string and integer enum values
            let endpoint;
            if (holonType && holonType !== 'All') {
                // If it's a number (enum value), use it directly; otherwise encode the string
                const typeParam = typeof holonType === 'number' ? holonType : encodeURIComponent(holonType);
                endpoint = `/api/data/load-all-holons/${typeParam}`;
            } else {
                endpoint = '/api/data/load-all-holons/All';
            }
            
            // Try GET first (simpler)
            try {
                const data = await this.request(endpoint, {
                    method: 'GET'
                });
                // Check if this is an error response (chunked encoding issue)
                if (data.errorType === 'INCOMPLETE_CHUNKED_ENCODING') {
                    // Return the error object so caller knows it's a size issue, not empty data
                    return data;
                }
                
                // Handle nested response structure: data.result.result is the array
                if (data.result?.result && Array.isArray(data.result.result)) {
                    console.log(`getAllHolons('${holonType}') returned ${data.result.result.length} holons from data.result.result`);
                    return data.result.result;
                }
                if (data.result && Array.isArray(data.result)) {
                    console.log(`getAllHolons('${holonType}') returned ${data.result.length} holons from data.result`);
                    return data.result;
                }
                if (Array.isArray(data)) {
                    console.log(`getAllHolons('${holonType}') returned ${data.length} holons from data`);
                    return data;
                }
                // If no array found, log the structure and return empty array
                console.warn(`getAllHolons('${holonType}') - no array found in response:`, data);
                return [];
            } catch (getError) {
                console.warn('GET request failed, trying POST:', getError);
                // Fallback to POST with request body
                try {
                    const data = await this.request('/api/data/load-all-holons', {
                        method: 'POST',
                        body: JSON.stringify({
                            HolonType: holonType || 'All',  // Can be string or integer enum value
                            LoadChildren: false,
                            Recursive: false,
                            MaxChildDepth: 0,
                            ContinueOnError: true,
                            Version: 0,
                            ProviderKey: 'MongoOASIS'
                        })
                    });
                    // Handle nested response structure
                    if (data.result?.result && Array.isArray(data.result.result)) {
                        return data.result.result;
                    }
                    if (data.result && Array.isArray(data.result)) {
                        return data.result;
                    }
                    return data.result || data || [];
                } catch (postError) {
                    console.error('Both GET and POST failed:', postError);
                    return [];
                }
            }
        } catch (error) {
            console.error('Failed to fetch holons:', error);
            return [];
        }
    }

    /**
     * Get all avatars (holons with HolonType = Avatar)
     * @returns {Promise<Array>} Array of avatar holons
     */
    async getAllAvatars() {
        // Use getAllHolons with Avatar type filter
        return this.getAllHolons('Avatar');
    }

    /**
     * Get all OAPPs from OASIS
     * OAPPs are holons with HolonType = OAPP
     * @returns {Promise<Array>} Array of OAPP objects
     */
    async getAllOAPPs() {
        // OAPPs are holons with HolonType = OAPP
        return this.getAllHolons('OAPP');
    }

    /**
     * Search holons by criteria
     * @param {object} criteria - Search criteria
     * @returns {Promise<Array>} Array of matching holons
     */
    async searchHolons(criteria) {
        try {
            const data = await this.request('/api/holons/search', {
                method: 'POST',
                body: JSON.stringify(criteria)
            });
            return data.result || data || [];
        } catch (error) {
            console.error('Failed to search holons:', error);
            return [];
        }
    }

    /**
     * Register a new avatar
     * @param {object} avatarData - Registration data
     * @returns {Promise<object>} Created avatar
     */
    async registerAvatar(avatarData) {
        try {
            const data = await this.request('/api/avatar/register', {
                method: 'POST',
                body: JSON.stringify(avatarData)
            });
            return data.result || data;
        } catch (error) {
            console.error('Failed to register avatar:', error);
            throw error;
        }
    }

    /**
     * Get holons for a specific avatar
     * @param {string} avatarId - Avatar ID
     * @returns {Promise<Array>} Array of holons created by the avatar
     */
    async getHolonsForAvatar(avatarId) {
        try {
            // Try to search for holons by createdByAvatarId
            // First, get all holons and filter client-side (more reliable)
            const allHolons = await this.getAllHolons('All');
            return allHolons.filter(h => {
                const id = h.id || h.Id || h._id;
                const createdBy = h.createdByAvatarId || h.CreatedByAvatarId || 
                                 h.metadata?.createdByAvatarId || h.MetaData?.createdByAvatarId ||
                                 h.metaData?.createdByAvatarId;
                return createdBy === avatarId || id === avatarId;
            });
        } catch (error) {
            console.error('Failed to get holons for avatar:', error);
            return [];
        }
    }

    /**
     * Get current authenticated avatar info
     * @returns {Promise<object>} Avatar information
     */
    async getCurrentAvatar() {
        try {
            const data = await this.request('/api/avatar/get-logged-in-avatar', {
                method: 'GET'
            });
            return data.result || data;
        } catch (error) {
            console.error('Failed to get current avatar:', error);
            return null;
        }
    }
}


