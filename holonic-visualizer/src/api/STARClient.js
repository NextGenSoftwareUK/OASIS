/**
 * STAR API Client
 * Handles authentication and API requests to the STAR API (WEB5)
 * Uses JWT tokens from OASIS API for authentication
 */
export class STARClient {
    constructor(config = {}) {
        // STAR API runs on port 50564 by default
        const defaultUrl = import.meta.env.VITE_STAR_API_URL || 'http://localhost:50564';
        this.baseUrl = config.baseUrl || defaultUrl;
        this.username = config.username || import.meta.env.VITE_STAR_USERNAME || 'OASIS_ADMIN';
        this.password = config.password || import.meta.env.VITE_STAR_PASSWORD || 'Uppermall1!';
        
        // OASIS API URL for JWT authentication
        const oasisUrl = import.meta.env.VITE_OASIS_API_URL || 'https://localhost:5004';
        this.oasisBaseUrl = config.oasisBaseUrl || oasisUrl;
        
        this.jwtToken = null;
        this.avatarId = null;
        this.isAuthenticated = false;
    }

    /**
     * Authenticate with OASIS API to get JWT token
     * STAR API uses JWT tokens from OASIS API for authentication
     * @returns {Promise<object>} Authentication result with JWT token
     */
    async authenticate() {
        try {
            const url = `${this.oasisBaseUrl}/api/avatar/authenticate`;
            
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    username: this.username,
                    password: this.password
                })
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`OASIS authentication failed: ${response.status} ${response.statusText} - ${errorText}`);
            }

            const data = await response.json();
            
            // Extract JWT token from nested response structure
            if (data.result && data.result.result && data.result.result.jwtToken) {
                this.jwtToken = data.result.result.jwtToken;
                this.avatarId = data.result.result.id;
                this.isAuthenticated = true;
                return data;
            } else if (data.result && data.result.jwtToken) {
                this.jwtToken = data.result.jwtToken;
                this.avatarId = data.result.id;
                this.isAuthenticated = true;
                return data;
            } else {
                throw new Error('JWT token not found in authentication response');
            }
        } catch (error) {
            console.error('OASIS API authentication error:', error);
            this.isAuthenticated = false;
            this.jwtToken = null;
            throw new Error(`Failed to authenticate: ${error.message}`);
        }
    }

    /**
     * Beam in (ignite STAR and authenticate)
     * @returns {Promise<object>} Beam-in result
     */
    async beamIn() {
        try {
            // First ensure we have a JWT token
            if (!this.jwtToken) {
                await this.authenticate();
            }
            
            // Check if STAR is ignited
            const status = await this.getStatus();
            if (!status.isIgnited) {
                // Ignite STAR
                const igniteUrl = `${this.baseUrl}/api/star/ignite`;
                const igniteResponse = await fetch(igniteUrl, {
                    method: 'POST',
                    headers: { 
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${this.jwtToken}`
                    },
                    body: JSON.stringify({
                        UserName: this.username,
                        Password: this.password
                    })
                });
                
                if (!igniteResponse.ok) {
                    console.warn('STAR ignition failed, continuing anyway');
                }
            }
            
            // Beam in (authenticate with STAR)
            const url = `${this.baseUrl}/api/star/beam-in`;
            const response = await fetch(url, {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.jwtToken}`
                },
                body: JSON.stringify({
                    username: this.username,
                    password: this.password
                })
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`Beam-in failed: ${response.status} ${response.statusText} - ${errorText}`);
            }

            const data = await response.json();
            
            if (data.success) {
                this.isAuthenticated = true;
                if (data.avatarId) {
                    this.avatarId = data.avatarId;
                }
                return data;
            } else {
                throw new Error(data.message || 'Beam-in failed');
            }
        } catch (error) {
            console.error('STAR API beam-in error:', error);
            this.isAuthenticated = false;
            throw new Error(`Failed to beam in: ${error.message}`);
        }
    }

    /**
     * Ensure we're authenticated (authenticate and beam in if needed)
     * @returns {Promise<void>}
     */
    async ensureAuthenticated() {
        if (!this.isAuthenticated || !this.jwtToken) {
            await this.authenticate();
            await this.beamIn();
        }
    }

    /**
     * Make an authenticated API request to STAR API
     * STAR API uses JWT tokens from OASIS API
     * @param {string} endpoint - API endpoint path
     * @param {object} options - Fetch options
     * @returns {Promise<object>} Response data
     */
    async request(endpoint, options = {}) {
        // Ensure we're authenticated first
        await this.ensureAuthenticated();
        
        const url = `${this.baseUrl}${endpoint}`;
        
        try {
            const response = await fetch(url, {
                ...options,
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.jwtToken}`,
                    ...options.headers
                }
            });

            if (!response.ok) {
                if (response.status === 401) {
                    // Re-authenticate and retry
                    this.isAuthenticated = false;
                    this.jwtToken = null;
                    await this.authenticate();
                    await this.beamIn();
                    return this.request(endpoint, options);
                }
                
                const errorText = await response.text();
                throw new Error(`API request failed: ${response.status} ${response.statusText} - ${errorText}`);
            }

            const data = await response.json();
            
            // STAR API returns OASISResult<T> format:
            // { Result: [...], IsError: false, Message: "..." }
            // or { result: {...}, isError: false, message: "..." }
            return data;
        } catch (error) {
            console.error(`STAR API request error for ${endpoint}:`, error);
            throw error;
        }
    }

    /**
     * Get all holons from STAR API
     * @returns {Promise<Array>} Array of holon objects
     */
    async getAllHolons() {
        try {
            const data = await this.request('/api/holons', {
                method: 'GET'
            });
            
            // Handle OASISResult format
            if (data.Result && Array.isArray(data.Result)) {
                console.log(`getAllHolons returned ${data.Result.length} holons from data.Result`);
                return data.Result;
            }
            if (data.result && Array.isArray(data.result)) {
                console.log(`getAllHolons returned ${data.result.length} holons from data.result`);
                return data.result;
            }
            if (Array.isArray(data)) {
                console.log(`getAllHolons returned ${data.length} holons from data`);
                return data;
            }
            
            // Check for errors
            if (data.IsError || data.isError) {
                console.error('STAR API error:', data.Message || data.message);
                return [];
            }
            
            console.warn('getAllHolons - no array found in response:', data);
            return [];
        } catch (error) {
            console.error('Failed to fetch holons from STAR API:', error);
            return [];
        }
    }

    /**
     * Get all OAPPs from STAR API
     * @returns {Promise<Array>} Array of OAPP objects
     */
    async getAllOAPPs() {
        try {
            const data = await this.request('/api/oapps', {
                method: 'GET'
            });
            
            // Handle OASISResult format
            if (data.Result && Array.isArray(data.Result)) {
                console.log(`getAllOAPPs returned ${data.Result.length} OAPPs from data.Result`);
                return data.Result;
            }
            if (data.result && Array.isArray(data.result)) {
                console.log(`getAllOAPPs returned ${data.result.length} OAPPs from data.result`);
                return data.result;
            }
            if (Array.isArray(data)) {
                console.log(`getAllOAPPs returned ${data.length} OAPPs from data`);
                return data;
            }
            
            // Check for errors
            if (data.IsError || data.isError) {
                console.error('STAR API error:', data.Message || data.message);
                return [];
            }
            
            console.warn('getAllOAPPs - no array found in response:', data);
            return [];
        } catch (error) {
            console.error('Failed to fetch OAPPs from STAR API:', error);
            return [];
        }
    }

    /**
     * Get holons for a specific avatar
     * @param {string} avatarId - Avatar ID
     * @returns {Promise<Array>} Array of holons
     */
    async getHolonsForAvatar(avatarId) {
        try {
            const data = await this.request(`/api/holons/load-all-for-avatar`, {
                method: 'GET'
            });
            
            // Handle OASISResult format
            if (data.Result && Array.isArray(data.Result)) {
                return data.Result;
            }
            if (data.result && Array.isArray(data.result)) {
                return data.result;
            }
            return [];
        } catch (error) {
            console.error('Failed to get holons for avatar:', error);
            return [];
        }
    }

    /**
     * Search holons by query
     * @param {string} query - Search query
     * @returns {Promise<Array>} Array of matching holons
     */
    async searchHolons(query) {
        try {
            const data = await this.request(`/api/holons/search?query=${encodeURIComponent(query)}`, {
                method: 'GET'
            });
            
            if (data.Result && Array.isArray(data.Result)) {
                return data.Result;
            }
            if (data.result && Array.isArray(data.result)) {
                return data.result;
            }
            return [];
        } catch (error) {
            console.error('Failed to search holons:', error);
            return [];
        }
    }

    /**
     * Search OAPPs by query
     * @param {string} query - Search query
     * @returns {Promise<Array>} Array of matching OAPPs
     */
    async searchOAPPs(query) {
        try {
            const data = await this.request(`/api/oapps/search?searchTerm=${encodeURIComponent(query)}`, {
                method: 'GET'
            });
            
            if (data.Result && Array.isArray(data.Result)) {
                return data.Result;
            }
            if (data.result && Array.isArray(data.result)) {
                return data.result;
            }
            return [];
        } catch (error) {
            console.error('Failed to search OAPPs:', error);
            return [];
        }
    }

    /**
     * Check STAR API status
     * @returns {Promise<object>} Status information
     */
    async getStatus() {
        try {
            // Status endpoint doesn't require authentication
            const url = `${this.baseUrl}/api/star/status`;
            const response = await fetch(url, {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' }
            });

            if (!response.ok) {
                return { isIgnited: false, status: 'error' };
            }

            const data = await response.json();
            return data;
        } catch (error) {
            console.error('Failed to get STAR status:', error);
            return { isIgnited: false, status: 'error' };
        }
    }
}
