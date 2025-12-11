/**
 * Shipex Pro API Client
 * Centralized API client for all Shipex Pro backend API calls
 * Extends OASIS avatar authentication
 */

class ShipexProAPI {
    constructor() {
        // Base URL - Shipex Pro API (separate from OASIS API)
        this.baseURL = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'
            ? 'https://localhost:5005'  // Local Shipex Pro API
            : 'https://api.shipexpro.com';
        
        // OASIS API URL for avatar authentication
        this.oasisApiURL = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'
            ? 'https://localhost:5004'  // Local OASIS API (HTTPS on 5004, HTTP on 5003)
            : 'https://api.oasisweb4.com';
        
        this.token = null;
        this.merchant = null;
    }

    /**
     * Set authentication token (from OASIS avatar auth)
     */
    setAuthToken(token) {
        this.token = token;
        localStorage.setItem('shipex_token', token);
    }

    /**
     * Get authentication token
     */
    getAuthToken() {
        if (!this.token) {
            this.token = localStorage.getItem('shipex_token');
        }
        return this.token;
    }

    /**
     * Set merchant context
     */
    setMerchant(merchant) {
        this.merchant = merchant;
        localStorage.setItem('shipex_merchant', JSON.stringify(merchant));
    }

    /**
     * Get merchant context
     */
    getMerchant() {
        if (!this.merchant) {
            const stored = localStorage.getItem('shipex_merchant');
            if (stored) {
                this.merchant = JSON.parse(stored);
            }
        }
        return this.merchant;
    }

    /**
     * Clear authentication
     */
    clearAuth() {
        this.token = null;
        this.merchant = null;
        localStorage.removeItem('shipex_token');
        localStorage.removeItem('shipex_merchant');
    }

    /**
     * Get authentication headers
     */
    getAuthHeaders() {
        const headers = {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        };

        const token = this.getAuthToken();
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        return headers;
    }

    /**
     * Generic request handler
     */
    async request(endpoint, options = {}) {
        // Skip API calls in demo mode - return mock success
        const token = this.getAuthToken();
        if (token && token.startsWith('demo-token-for-testing')) {
            console.log('[Demo Mode] Skipping API call:', endpoint);
            return { result: null, isError: false, message: 'Demo mode - API call skipped' };
        }

        const url = `${this.baseURL}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;
        
        const config = {
            ...options,
            headers: {
                ...this.getAuthHeaders(),
                ...(options.headers || {})
            }
        };

        try {
            const response = await fetch(url, config);
            
            // Handle non-JSON responses
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                const data = await response.json();
                
                // Handle OASISResult format
                if (data.isError) {
                    throw new Error(data.message || 'API request failed');
                }
                
                return data;
            } else {
                const text = await response.text();
                if (!response.ok) {
                    throw new Error(text || `HTTP ${response.status}`);
                }
                return { result: text, isError: false };
            }
        } catch (error) {
            console.error('API Request Error:', error);
            // Provide more helpful error messages
            if (error.message.includes('Failed to fetch') || error.message.includes('ERR_CONNECTION_REFUSED')) {
                throw new Error(`Cannot connect to API at ${this.baseURL}. Make sure the Shipex Pro API is running on port 5005.`);
            }
            throw error;
        }
    }

    // ============================================
    // OASIS Avatar Authentication
    // ============================================

    /**
     * Login with OASIS avatar
     */
    async loginAvatar(username, password) {
        try {
            const response = await fetch(`${this.oasisApiURL}/api/avatar/authenticate`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify({ username, password })
            });

            if (!response.ok) {
                const error = await response.json().catch(() => ({ message: `HTTP ${response.status}` }));
                throw new Error(error.message || 'Authentication failed');
            }

            const data = await response.json();
            
            // OASIS API structure: 
            // { result: { Result: { jwtToken: "...", ... }, isError: false, ... }, ... }
            // The nested Result (capital R) contains the IAvatar object
            const oasisResult = data.result || data;
            
            // Check for error first
            if (oasisResult.isError || oasisResult.IsError) {
                const errorMsg = oasisResult.message || oasisResult.Message || 'Authentication failed';
                throw new Error(errorMsg);
            }
            
            // Extract avatar from nested Result (capital R in C#, might be lowercase in JSON)
            const avatar = oasisResult.Result || oasisResult.result || oasisResult;
            
            // Extract token from avatar object
            // From meta-bricks example: response.data.result.Result.jwtToken
            const token = avatar?.jwtToken || avatar?.JwtToken || 
                         avatar?.token || avatar?.Token ||
                         oasisResult?.jwtToken || oasisResult?.JwtToken;
            
            // Extract avatar ID
            const avatarId = avatar?.avatarId || avatar?.AvatarId || 
                           avatar?.id || avatar?.Id;

            if (!token) {
                console.error('Authentication response structure:', JSON.stringify(data, null, 2));
                console.error('OASIS Result:', oasisResult);
                console.error('Avatar object:', avatar);
                throw new Error('No token received from authentication. Check console for response structure.');
            }

            this.setAuthToken(token);

            return {
                token,
                avatar: avatar || { avatarId, username },
                avatarId: avatarId || avatar?.avatarId || avatar?.AvatarId
            };
        } catch (error) {
            console.error('Avatar login failed:', error);
            throw error;
        }
    }

    /**
     * Register new OASIS avatar
     */
    async registerAvatar(registrationData) {
        // Call OASIS API directly for avatar registration
        const response = await fetch(`${this.oasisApiURL}/api/avatar/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            },
            body: JSON.stringify(registrationData)
        });

        if (!response.ok) {
            const error = await response.json().catch(() => ({ message: `HTTP ${response.status}` }));
            throw new Error(error.message || 'Registration failed');
        }

        const data = await response.json();

        // Extract token from response
        const token = response.token || response.jwtToken || response.result?.token;
        const avatar = response.avatar || response.result?.avatar;

        if (token) {
            this.setAuthToken(token);
        }

        return {
            token,
            avatar,
            avatarId: avatar?.avatarId || response.avatarId
        };
    }

    // ============================================
    // Merchant Operations
    // ============================================

    /**
     * Get merchant by avatar ID
     */
    async getMerchantByAvatar(avatarId) {
        // Skip API call in demo mode
        const token = this.getAuthToken();
        if (token && token.startsWith('demo-token-for-testing')) {
            return { result: this.getMerchant(), isError: false };
        }
        return this.request(`/api/shipexpro/merchant/by-avatar/${avatarId}`);
    }

    /**
     * Create merchant profile from avatar
     */
    async createMerchantFromAvatar(avatarId, merchantData) {
        // Skip API call in demo mode
        const token = this.getAuthToken();
        if (token && token.startsWith('demo-token-for-testing')) {
            return { result: this.getMerchant(), isError: false };
        }
        return this.request('/api/shipexpro/merchant/create-from-avatar', {
            method: 'POST',
            body: JSON.stringify({
                avatarId,
                ...merchantData
            })
        });
    }

    /**
     * Get merchant by ID (async API call)
     */
    async getMerchantById(merchantId) {
        // Skip API call in demo mode
        const token = this.getAuthToken();
        if (token && token.startsWith('demo-token-for-testing')) {
            return { result: this.getMerchant(), isError: false };
        }
        if (!merchantId) {
            throw new Error('Merchant ID is required');
        }
        return this.request(`/api/shipexpro/merchant/${merchantId}`);
    }

    // ============================================
    // Shipment Operations
    // ============================================

    /**
     * Get shipments by merchant ID
     */
    async getShipments(merchantId, filters = {}) {
        // Skip API call in demo mode
        const token = this.getAuthToken();
        if (token && token.startsWith('demo-token-for-testing')) {
            return { result: [], isError: false };
        }
        
        const params = new URLSearchParams({
            merchantId,
            ...(filters.status && { status: filters.status }),
            ...(filters.limit && { limit: filters.limit }),
            ...(filters.offset && { offset: filters.offset })
        });
        
        return this.request(`/api/shipexpro/shipments?${params}`);
    }

    /**
     * Get shipment by ID
     */
    async getShipment(shipmentId) {
        return this.request(`/api/shipexpro/shipments/${shipmentId}`);
    }

    /**
     * Track shipment by tracking number
     */
    async trackShipment(trackingNumber) {
        // Skip API call in demo mode - return mock data
        const token = this.getAuthToken();
        if (token && token.startsWith('demo-token-for-testing')) {
            return { result: null, isError: false };
        }
        return this.request(`/api/shipexpro/shipox/track/${encodeURIComponent(trackingNumber)}`);
    }

    // ============================================
    // Quote Operations
    // ============================================

    /**
     * Request shipping quotes
     */
    async requestQuote(rateRequest) {
        // Skip API call in demo mode - return mock data
        const token = this.getAuthToken();
        if (token && token.startsWith('demo-token-for-testing')) {
            return { result: null, isError: false };
        }
        return this.request('/api/shipexpro/shipox/quote-request', {
            method: 'POST',
            body: JSON.stringify(rateRequest)
        });
    }

    /**
     * Confirm shipment (create from quote)
     */
    async confirmShipment(orderRequest) {
        // Skip API call in demo mode - return mock data
        const token = this.getAuthToken();
        if (token && token.startsWith('demo-token-for-testing')) {
            return { result: null, isError: false };
        }
        return this.request('/api/shipexpro/shipox/confirm-shipment', {
            method: 'POST',
            body: JSON.stringify(orderRequest)
        });
    }

    // ============================================
    // Markup Operations
    // ============================================

    /**
     * Get markups
     */
    async getMarkups(merchantId = null) {
        // Skip API call in demo mode - return mock data
        const token = this.getAuthToken();
        if (token && token.startsWith('demo-token-for-testing')) {
            return { result: [], isError: false };
        }
        const params = merchantId ? `?merchantId=${merchantId}` : '';
        return this.request(`/api/shipexpro/markups${params}`);
    }

    /**
     * Get markup by ID
     */
    async getMarkup(markupId) {
        return this.request(`/api/shipexpro/markups/${markupId}`);
    }

    /**
     * Create markup
     */
    async createMarkup(markup) {
        return this.request('/api/shipexpro/markups', {
            method: 'POST',
            body: JSON.stringify(markup)
        });
    }

    /**
     * Update markup
     */
    async updateMarkup(markupId, markup) {
        return this.request(`/api/shipexpro/markups/${markupId}`, {
            method: 'PUT',
            body: JSON.stringify(markup)
        });
    }

    /**
     * Delete markup
     */
    async deleteMarkup(markupId) {
        return this.request(`/api/shipexpro/markups/${markupId}`, {
            method: 'DELETE'
        });
    }

    // ============================================
    // QuickBooks Operations
    // ============================================

    /**
     * Get QuickBooks authorization URL
     */
    async getQuickBooksAuthUrl(merchantId, state = null) {
        // Skip API call in demo mode
        const token = this.getAuthToken();
        if (token && token.startsWith('demo-token-for-testing')) {
            return { result: { authorizationUrl: 'https://appcenter.intuit.com/connect/oauth2?demo=true' }, isError: false };
        }
        const params = new URLSearchParams({ merchantId });
        if (state) params.append('state', state);
        
        return this.request(`/api/shipexpro/quickbooks/authorize?${params}`);
    }

    /**
     * Refresh QuickBooks token
     */
    async refreshQuickBooksToken(merchantId) {
        return this.request(`/api/shipexpro/quickbooks/refresh-token?merchantId=${merchantId}`, {
            method: 'POST'
        });
    }
}

// Export singleton instance
const shipexAPI = new ShipexProAPI();
