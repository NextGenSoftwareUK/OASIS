/**
 * OASIS API Client
 * Centralized API client for all OASIS backend API calls
 */

const oasisAPI = {
    // Base URL configuration
    // Using local API when on localhost, otherwise remote API
    // Use HTTPS if page is served over HTTPS to avoid mixed content errors
    baseURL: (() => {
        const hostname = window.location.hostname;
        const protocol = window.location.protocol;
        
        if (hostname === 'localhost' || hostname === '127.0.0.1') {
            return 'https://localhost:5004';  // Local API runs on HTTPS port 5004
        }
        
        // Use HTTPS if the page is served over HTTPS, otherwise HTTP
        // This prevents mixed content errors
        if (protocol === 'https:') {
            return 'https://api.oasisweb4.com';
        }
        return 'http://api.oasisweb4.com';
    })(),

    /**
     * Get authentication headers
     * Uses centralized authStore if available, otherwise falls back to localStorage
     */
    getAuthHeaders() {
        const headers = {
            'Content-Type': 'application/json'
        };

        // Try to use centralized authStore first (from api/auth.js)
        if (typeof authStore !== 'undefined' && authStore.getAuthHeader()) {
            headers['Authorization'] = authStore.getAuthHeader();
            return headers;
        }

        // Fallback to direct localStorage access
        try {
            const authData = localStorage.getItem('oasis_auth');
            if (!authData) {
                return headers;
            }

            const auth = JSON.parse(authData);
            if (auth.token) {
                headers['Authorization'] = `Bearer ${auth.token}`;
            }
        } catch (error) {
            console.error('Error getting auth headers:', error);
        }

        return headers;
    },

    /**
     * Generic request handler
     */
    async request(endpoint, options = {}) {
        const url = `${this.baseURL}${endpoint}`;
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
                return data;
            } else {
                return {
                    isError: !response.ok,
                    message: response.statusText,
                    status: response.status
                };
            }
        } catch (error) {
            console.error('API Request Error:', error);
            return {
                isError: true,
                message: error.message || 'Network error',
                error: error
            };
        }
    },

    // ============================================
    // Provider API Methods
    // ============================================

    /**
     * Get all registered providers
     */
    async getAllProviders() {
        return this.request('/api/provider/get-all-registered-providers');
    },

    /**
     * Get all registered storage providers
     */
    async getStorageProviders() {
        return this.request('/api/provider/get-all-registered-storage-providers');
    },

    /**
     * Get current storage provider
     */
    async getCurrentStorageProvider() {
        return this.request('/api/provider/get-current-storage-provider');
    },

    /**
     * Set current storage provider
     */
    async setCurrentStorageProvider(providerType) {
        return this.request(`/api/provider/set-current-storage-provider/${encodeURIComponent(providerType)}`, {
            method: 'POST',
            body: JSON.stringify({ providerType })
        });
    },

    /**
     * Get providers with auto-replication enabled
     */
    async getReplicationProviders() {
        return this.request('/api/provider/get-providers-that-are-auto-replicating');
    },

    /**
     * Get providers with auto-failover enabled
     */
    async getFailoverProviders() {
        return this.request('/api/provider/get-providers-that-have-auto-fail-over-enabled');
    },

    /**
     * Activate a provider (if endpoint exists)
     */
    async activateProvider(providerType) {
        // Note: This endpoint may need to be confirmed with actual API
        return this.request(`/api/provider/activate/${encodeURIComponent(providerType)}`, {
            method: 'POST'
        });
    },

    /**
     * Deactivate a provider (if endpoint exists)
     */
    async deactivateProvider(providerType) {
        // Note: This endpoint may need to be confirmed with actual API
        return this.request(`/api/provider/deactivate/${encodeURIComponent(providerType)}`, {
            method: 'POST'
        });
    },

    /**
     * Set auto-replication for all providers
     * @param {boolean} autoReplicate - Enable or disable auto-replication
     */
    async setAutoReplicateForAllProviders(autoReplicate) {
        return this.request(`/api/provider/set-auto-replicate-for-all-providers/${autoReplicate}`, {
            method: 'POST'
        });
    },

    /**
     * Set auto-replication for a list of providers
     * @param {boolean} autoReplicate - Enable or disable auto-replication
     * @param {string[]} providerTypes - Array of provider types
     */
    async setAutoReplicateForProviders(autoReplicate, providerTypes) {
        const providerTypesString = Array.isArray(providerTypes) ? providerTypes.join(',') : providerTypes;
        return this.request(`/api/provider/set-auto-replicate-for-list-of-providers/${autoReplicate}/${encodeURIComponent(providerTypesString)}`, {
            method: 'POST'
        });
    },

    /**
     * Set auto-failover for all providers
     * @param {boolean} addToFailOverList - Enable or disable auto-failover
     */
    async setAutoFailOverForAllProviders(addToFailOverList) {
        return this.request(`/api/provider/set-auto-fail-over-for-all-providers/${addToFailOverList}`, {
            method: 'POST'
        });
    },

    // ============================================
    // Data/Holon API Methods
    // ============================================

    /**
     * Get holons by avatar ID
     * @param {string} avatarId - Avatar ID
     * @param {object} filters - Optional filters {holonType, providerType}
     */
    async getHolonsByAvatar(avatarId, filters = {}) {
        let endpoint = `/api/data/avatar/${encodeURIComponent(avatarId)}/holons`;
        
        const params = new URLSearchParams();
        if (filters.holonType) params.append('holonType', filters.holonType);
        if (filters.providerType) params.append('providerType', filters.providerType);
        
        if (params.toString()) {
            endpoint += `?${params.toString()}`;
        }
        
        return this.request(endpoint);
    },

    /**
     * Get holon by ID
     * @param {string} holonId - Holon ID
     * @param {string} providerType - Optional provider type
     */
    async getHolon(holonId, providerType = null) {
        let endpoint = `/api/data/holon/${encodeURIComponent(holonId)}`;
        
        if (providerType) {
            endpoint += `?providerType=${encodeURIComponent(providerType)}`;
        }
        
        return this.request(endpoint);
    },

    /**
     * Save holon
     * @param {object} holonData - Holon data to save
     */
    async saveHolon(holonData) {
        return this.request('/api/data/save-holon', {
            method: 'POST',
            body: JSON.stringify(holonData)
        });
    },

    /**
     * Delete holon
     * @param {string} holonId - Holon ID
     * @param {string} providerType - Optional provider type
     */
    async deleteHolon(holonId, providerType = null) {
        let endpoint = `/api/data/delete-holon/${encodeURIComponent(holonId)}`;
        
        if (providerType) {
            endpoint += `?providerType=${encodeURIComponent(providerType)}`;
        }
        
        return this.request(endpoint, {
            method: 'DELETE'
        });
    },

    // ============================================
    // Avatar Authentication API Methods
    // ============================================

    /**
     * Login with username/email and password
     * @param {string} username - Username or email
     * @param {string} password - Password
     * @returns {Promise<object>} Authentication response with avatar and token
     */
    async login(username, password) {
        try {
            const response = await fetch(`${this.baseURL}/api/avatar/authenticate`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json',
                },
                body: JSON.stringify({
                    username: username,
                    password: password
                })
            });

            if (!response.ok) {
                let errorMessage = `HTTP ${response.status}`;
                try {
                    const errorData = await response.json();
                    errorMessage = errorData.message || errorData.error || errorMessage;
                } catch (e) {
                    const text = await response.text().catch(() => '');
                    errorMessage = text || errorMessage;
                }
                throw new Error(errorMessage);
            }

            const data = await response.json();
            
            // Handle nested result structure (API returns: data.result.result.jwtToken)
            const result = data.result?.result || data.result || data;
            const avatar = result.avatar || data.result?.avatar || data.avatar;
            const token = result.jwtToken || data.result?.jwtToken || data.jwtToken || data.token;
            const refreshToken = result.refreshToken || data.result?.refreshToken || data.refreshToken;

            if (!token) {
                throw new Error(data.result?.message || data.message || 'No token received from authentication service');
            }

            // Normalize avatar structure
            const normalizedAvatar = {
                ...(avatar || result),
                id: avatar?.avatarId || avatar?.id || result?.avatarId || result?.id,
                avatarId: avatar?.avatarId || avatar?.id || result?.avatarId || result?.id,
                username: avatar?.username || result?.username,
                email: avatar?.email || result?.email,
                firstName: avatar?.firstName || result?.firstName,
                lastName: avatar?.lastName || result?.lastName
            };

            // Store auth data
            const authData = {
                avatar: normalizedAvatar,
                token: token,
                refreshToken: refreshToken || null,
                timestamp: Date.now()
            };

            localStorage.setItem('oasis_auth', JSON.stringify(authData));

            return {
                avatar: normalizedAvatar,
                jwtToken: token,
                refreshToken: refreshToken,
                expiresIn: undefined
            };
        } catch (error) {
            console.error('Avatar login failed:', error);
            throw error instanceof Error ? error : new Error('Unable to authenticate avatar');
        }
    },

    /**
     * Register a new avatar
     * @param {object} registrationData - Registration data
     * @param {string} registrationData.username - Username
     * @param {string} registrationData.email - Email
     * @param {string} registrationData.password - Password
     * @param {string} registrationData.confirmPassword - Password confirmation
     * @param {string} [registrationData.firstName] - First name
     * @param {string} [registrationData.lastName] - Last name
     * @param {string} [registrationData.title] - Title (Mr, Mrs, etc.)
     * @param {string} [registrationData.avatarType] - Avatar type (default: 'User')
     * @param {boolean} [registrationData.acceptTerms] - Terms acceptance (required)
     * @param {boolean} [registrationData.privacyMode] - Privacy mode flag
     * @returns {Promise<object>} Authentication response with avatar and token
     */
    async register(registrationData) {
        try {
            // Set defaults
            const payload = {
                username: registrationData.username,
                email: registrationData.email,
                password: registrationData.password,
                confirmPassword: registrationData.confirmPassword || registrationData.password,
                firstName: registrationData.firstName || 'User',
                lastName: registrationData.lastName || 'User',
                title: registrationData.title || 'Mr',
                avatarType: registrationData.avatarType || 'User',
                acceptTerms: registrationData.acceptTerms !== undefined ? registrationData.acceptTerms : true,
                privacyMode: registrationData.privacyMode || false
            };

            const response = await fetch(`${this.baseURL}/api/avatar/register`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json',
                },
                body: JSON.stringify(payload)
            });

            if (!response.ok) {
                let errorMessage = `HTTP ${response.status}`;
                try {
                    const errorData = await response.json();
                    errorMessage = errorData.message || errorData.error || errorMessage;
                } catch (e) {
                    const text = await response.text().catch(() => '');
                    errorMessage = text || errorMessage;
                }
                throw new Error(errorMessage);
            }

            const data = await response.json();

            // Handle nested result structure
            const avatar = data.result?.avatar || data.result?.result?.avatar || data.avatar || data.result;
            const token = data.result?.jwtToken || data.result?.result?.jwtToken || data.jwtToken || data.token;
            const refreshToken = data.result?.refreshToken || data.refreshToken;
            const verificationToken = data.result?.verificationToken || data.result?.result?.verificationToken || data.verificationToken;

            if (!token) {
                throw new Error(data.message || 'No token received from registration service');
            }

            // Normalize avatar structure
            const normalizedAvatar = {
                ...avatar,
                id: avatar?.avatarId || avatar?.id,
                avatarId: avatar?.avatarId || avatar?.id,
                verificationToken: verificationToken
            };

            // Store auth data
            const authData = {
                avatar: normalizedAvatar,
                token: token,
                refreshToken: refreshToken || null,
                timestamp: Date.now()
            };

            localStorage.setItem('oasis_auth', JSON.stringify(authData));

            // If privacy mode and verification token exists, try to auto-verify
            if (registrationData.privacyMode && verificationToken) {
                try {
                    await this.verifyEmail(verificationToken);
                    console.log('Privacy mode: Avatar auto-verified');
                } catch (verifyError) {
                    console.warn('Privacy mode: Auto-verification failed (optional)', verifyError);
                    // Don't throw - registration succeeded
                }
            }

            return {
                avatar: normalizedAvatar,
                jwtToken: token,
                refreshToken: refreshToken,
                expiresIn: undefined
            };
        } catch (error) {
            console.error('Avatar registration failed:', error);
            throw error instanceof Error ? error : new Error('Unable to register avatar');
        }
    },

    /**
     * Verify email using verification token
     * @param {string} token - Verification token
     * @returns {Promise<object>} Verification result
     */
    async verifyEmail(token) {
        try {
            const response = await this.request(`/api/avatar/verify-email?token=${encodeURIComponent(token)}`, {
                method: 'GET'
            });

            if (response.isError) {
                throw new Error(response.message || 'Email verification failed');
            }

            return {
                isError: false,
                message: response.message || 'Email verified successfully',
                result: response.result || response
            };
        } catch (error) {
            return {
                isError: true,
                message: error instanceof Error ? error.message : 'Email verification failed'
            };
        }
    },

    /**
     * Get avatar by ID
     * @param {string} avatarId - Avatar ID
     * @returns {Promise<object>} Avatar profile
     */
    async getAvatar(avatarId) {
        return this.request(`/api/avatar/${encodeURIComponent(avatarId)}`);
    },

    /**
     * Get avatar by username
     * @param {string} username - Username
     * @returns {Promise<object>} Avatar profile
     */
    async getAvatarByUsername(username) {
        return this.request(`/api/avatar/username/${encodeURIComponent(username)}`);
    },

    /**
     * Logout - clear authentication data
     */
    logout() {
        localStorage.removeItem('oasis_auth');
        // Reload page to reset all state
        window.location.reload();
    },

    // ============================================
    // Helper Methods
    // ============================================

    /**
     * Get avatar ID from localStorage
     */
    getAvatarId() {
        try {
            const authData = localStorage.getItem('oasis_auth');
            if (!authData) return null;
            
            const auth = JSON.parse(authData);
            const avatar = auth.avatar;
            
            if (!avatar) return null;
            
            return avatar.avatarId || avatar.id || null;
        } catch (error) {
            console.error('Error getting avatar ID:', error);
            return null;
        }
    },

    /**
     * Get avatar data from localStorage
     */
    getAvatar() {
        try {
            const authData = localStorage.getItem('oasis_auth');
            if (!authData) return null;
            
            const auth = JSON.parse(authData);
            return auth.avatar || null;
        } catch (error) {
            console.error('Error getting avatar:', error);
            return null;
        }
    },

    /**
     * Check if user is authenticated
     */
    isAuthenticated() {
        const authData = localStorage.getItem('oasis_auth');
        if (!authData) return false;
        
        try {
            const auth = JSON.parse(authData);
            return !!(auth.token && auth.avatar);
        } catch (error) {
            return false;
        }
    },

    // ============================================
    // Telegram API Methods
    // ============================================

    /**
     * Link Telegram account to OASIS avatar
     * @param {object} data - { telegramUserId, telegramUsername, oasisAvatarId, verificationCode? }
     */
    async linkTelegram(data) {
        return this.request('/api/telegram/link-avatar', {
            method: 'POST',
            body: JSON.stringify(data)
        });
    },

    /**
     * Get avatar by Telegram user ID
     * @param {number} telegramUserId - Telegram user ID
     */
    async getTelegramAvatarByTelegram(telegramUserId) {
        return this.request(`/api/telegram/avatar/telegram/${telegramUserId}`);
    },

    /**
     * Get avatar by OASIS avatar ID
     * @param {string} oasisAvatarId - OASIS avatar ID
     */
    async getTelegramAvatarByOasis(oasisAvatarId) {
        return this.request(`/api/telegram/avatar/oasis/${oasisAvatarId}`);
    },

    /**
     * Get user's Telegram groups
     * @param {number} telegramUserId - Telegram user ID
     */
    async getTelegramGroups(telegramUserId) {
        return this.request(`/api/telegram/groups/user/${telegramUserId}`);
    },

    /**
     * Get Telegram group details
     * @param {string} groupId - Group ID
     */
    async getTelegramGroup(groupId) {
        return this.request(`/api/telegram/groups/${groupId}`);
    },

    /**
     * Join a Telegram group
     * @param {object} data - { groupId, telegramUserId }
     */
    async joinTelegramGroup(data) {
        return this.request('/api/telegram/groups/join', {
            method: 'POST',
            body: JSON.stringify(data)
        });
    },

    /**
     * Get user achievements
     * @param {number} telegramUserId - Telegram user ID
     */
    async getTelegramAchievements(telegramUserId) {
        return this.request(`/api/telegram/achievements/user/${telegramUserId}`);
    },

    /**
     * Get user rewards
     * @param {number} telegramUserId - Telegram user ID
     * @param {number} limit - Limit of results (default: 10)
     */
    async getTelegramRewards(telegramUserId, limit = 10) {
        return this.request(`/api/telegram/rewards/user/${telegramUserId}?limit=${limit}`);
    },

    /**
     * Get user check-ins
     * @param {number} telegramUserId - Telegram user ID
     * @param {number} limit - Limit of results (default: 10)
     */
    async getTelegramCheckins(telegramUserId, limit = 10) {
        return this.request(`/api/telegram/achievements/checkin/user/${telegramUserId}?limit=${limit}`);
    },

    /**
     * Get user activities
     * @param {number} telegramUserId - Telegram user ID
     * @param {number} limit - Limit of results (default: 50)
     * @param {string} type - Optional activity type filter
     */
    async getTelegramActivities(telegramUserId, limit = 50, type = null) {
        let endpoint = `/api/telegram/activities/user/${telegramUserId}?limit=${limit}`;
        if (type) {
            endpoint += `&type=${type}`;
        }
        return this.request(endpoint);
    },

    /**
     * Get group leaderboard
     * @param {string|null} groupId - Group ID (null for global leaderboard)
     * @param {string} period - Period: 'daily', 'weekly', 'monthly', 'alltime'
     */
    async getTelegramLeaderboard(groupId = null, period = 'alltime') {
        if (groupId) {
            return this.request(`/api/telegram/groups/${groupId}/leaderboard?period=${period}`);
        } else {
            return this.request(`/api/telegram/leaderboard?period=${period}`);
        }
    },

    /**
     * Get user statistics
     * @param {number} telegramUserId - Telegram user ID
     */
    async getTelegramStats(telegramUserId) {
        return this.request(`/api/telegram/stats/user/${telegramUserId}`);
    },

    // ============================================
    // Developer API Methods
    // ============================================

    /**
     * Get API keys for an avatar
     * @param {string} avatarId - Avatar ID
     */
    async getAPIKeys(avatarId) {
        return this.request(`/api/developer/api-keys?avatarId=${encodeURIComponent(avatarId)}`);
    },

    /**
     * Create a new API key
     * @param {object} data - { avatarId, name, permissions[], expiresIn? }
     */
    async createAPIKey(data) {
        return this.request('/api/developer/api-keys', {
            method: 'POST',
            body: JSON.stringify(data)
        });
    },

    /**
     * Revoke an API key
     * @param {string} keyId - API key ID
     */
    async revokeAPIKey(keyId) {
        return this.request(`/api/developer/api-keys/${encodeURIComponent(keyId)}`, {
            method: 'DELETE'
        });
    },

    /**
     * Get API usage statistics
     * @param {string} avatarId - Avatar ID
     * @param {string} period - Period: 'daily', 'weekly', 'monthly', 'alltime'
     */
    async getUsage(avatarId, period = 'monthly') {
        return this.request(`/api/developer/usage?avatarId=${encodeURIComponent(avatarId)}&period=${encodeURIComponent(period)}`);
    },

    /**
     * Get usage history
     * @param {string} avatarId - Avatar ID
     * @param {number} days - Number of days (default: 30)
     */
    async getUsageHistory(avatarId, days = 30) {
        return this.request(`/api/developer/usage/history?avatarId=${encodeURIComponent(avatarId)}&days=${days}`);
    },

    /**
     * Get all OAPPs
     * @param {string} category - Optional category filter
     * @param {string} status - Optional status filter
     */
    async getAllOAPPs(category = null, status = null) {
        const params = new URLSearchParams();
        if (category) params.append('category', category);
        if (status) params.append('status', status);
        const query = params.toString();
        return this.request(`/api/oapp${query ? '?' + query : ''}`);
    },

    /**
     * Get installed OAPPs for an avatar
     * @param {string} avatarId - Avatar ID
     */
    async getInstalledOAPPs(avatarId) {
        return this.request(`/api/oapp/avatar/${encodeURIComponent(avatarId)}/installed`);
    },

    /**
     * Install an OAPP
     * @param {string} avatarId - Avatar ID
     * @param {string} oappId - OAPP ID
     * @param {string} version - Optional OAPP version
     */
    async installOAPP(avatarId, oappId, version = null) {
        const payload = { avatarId, oappId };
        if (version) payload.oappVersion = version;
        return this.request('/api/oapp/install', {
            method: 'POST',
            body: JSON.stringify(payload)
        });
    },

    /**
     * Uninstall an OAPP
     * @param {string} avatarId - Avatar ID
     * @param {string} oappId - OAPP ID
     */
    async uninstallOAPP(avatarId, oappId) {
        return this.request('/api/oapp/uninstall', {
            method: 'POST',
            body: JSON.stringify({ avatarId, oappId })
        });
    },

    /**
     * Get OAPP details
     * @param {string} oappId - OAPP ID
     */
    async getOAPPDetails(oappId) {
        return this.request(`/api/oapp/${encodeURIComponent(oappId)}`);
    },

    /**
     * Get available SDKs
     */
    async getSDKs() {
        return this.request('/api/developer/sdks');
    },

    /**
     * Get code examples
     * @param {string} category - Optional category filter
     * @param {string} language - Optional language filter
     */
    async getCodeExamples(category = null, language = null) {
        const params = new URLSearchParams();
        if (category) params.append('category', category);
        if (language) params.append('language', language);
        const query = params.toString();
        return this.request(`/api/developer/examples${query ? '?' + query : ''}`);
    },

    /**
     * Get webhooks for an avatar
     * @param {string} avatarId - Avatar ID
     */
    async getWebhooks(avatarId) {
        return this.request(`/api/developer/webhooks?avatarId=${encodeURIComponent(avatarId)}`);
    },

    /**
     * Create a webhook
     * @param {object} data - { avatarId, url, events[], secret? }
     */
    async createWebhook(data) {
        return this.request('/api/developer/webhooks', {
            method: 'POST',
            body: JSON.stringify(data)
        });
    },

    /**
     * Delete a webhook
     * @param {string} webhookId - Webhook ID
     */
    async deleteWebhook(webhookId) {
        return this.request(`/api/developer/webhooks/${encodeURIComponent(webhookId)}`, {
            method: 'DELETE'
        });
    },

    /**
     * Get API logs
     * @param {string} avatarId - Avatar ID
     * @param {number} limit - Limit of results (default: 100)
     * @param {string} level - Optional log level filter: 'info', 'warning', 'error'
     */
    async getAPILogs(avatarId, limit = 100, level = null) {
        const params = new URLSearchParams({ limit: limit.toString() });
        if (level) params.append('level', level);
        return this.request(`/api/developer/logs?avatarId=${encodeURIComponent(avatarId)}&${params.toString()}`);
    },

    // ============================================
    // NFT API Methods
    // ============================================

    /**
     * Get NFTs for an avatar
     * @param {string} avatarId - Avatar ID
     * @param {string} providerType - Optional provider type filter
     */
    async getAvatarNFTs(avatarId, providerType = null) {
        let endpoint = `/api/nft/avatar/${encodeURIComponent(avatarId)}/nfts`;
        if (providerType) {
            endpoint += `?providerType=${encodeURIComponent(providerType)}`;
        }
        return this.request(endpoint);
    },

    /**
     * Get NFT by ID
     * @param {string} nftId - NFT ID
     */
    async getNFT(nftId) {
        return this.request(`/api/nft/${encodeURIComponent(nftId)}`);
    },

    /**
     * Mint NFT
     * @param {object} nftData - NFT data to mint
     * @param {string} nftData.avatarId - Avatar ID
     * @param {string} nftData.name - NFT name
     * @param {string} nftData.description - NFT description
     * @param {string} nftData.imageUrl - NFT image URL
     * @param {object} nftData.metadata - NFT metadata
     * @param {string} nftData.providerType - Provider type (e.g., 'SolanaOASIS', 'EthereumOASIS')
     */
    async mintNFT(nftData) {
        return this.request('/api/nft/mint', {
            method: 'POST',
            body: JSON.stringify(nftData)
        });
    },

    // ============================================
    // Wallet API Methods
    // ============================================

    /**
     * Get all wallets for an avatar
     * @param {string} avatarId - Avatar ID
     */
    async getAvatarWallets(avatarId) {
        return this.request(`/api/wallet/avatar/${encodeURIComponent(avatarId)}/wallets`);
    },

    /**
     * Get wallets for a specific chain
     * @param {string} avatarId - Avatar ID
     * @param {string} chain - Chain name (e.g., 'Solana', 'Ethereum')
     */
    async getWalletsByChain(avatarId, chain) {
        return this.request(`/api/wallet/avatar/${encodeURIComponent(avatarId)}/wallets/chain/${encodeURIComponent(chain)}`);
    },

    /**
     * Get default wallet for an avatar
     * @param {string} avatarId - Avatar ID
     */
    async getDefaultWallet(avatarId) {
        return this.request(`/api/wallet/avatar/${encodeURIComponent(avatarId)}/default-wallet`);
    },

    /**
     * Create a new wallet
     * @param {string} avatarId - Avatar ID
     * @param {object} walletData - Wallet creation data
     * @param {string} walletData.providerType - Provider type (e.g., 'SolanaOASIS', 'EthereumOASIS')
     * @param {string} [walletData.name] - Wallet name
     */
    async createWallet(avatarId, walletData) {
        return this.request(`/api/wallet/avatar/${encodeURIComponent(avatarId)}/wallets`, {
            method: 'POST',
            body: JSON.stringify(walletData)
        });
    },

    /**
     * Get wallet balance
     * @param {string} walletId - Wallet ID
     */
    async getWalletBalance(walletId) {
        return this.request(`/api/wallet/${encodeURIComponent(walletId)}/balance`);
    },

    /**
     * Transfer tokens
     * @param {object} transferData - Transfer data
     * @param {string} transferData.fromWalletId - Source wallet ID
     * @param {string} transferData.toAddress - Destination address
     * @param {number} transferData.amount - Amount to transfer
     * @param {string} [transferData.memo] - Optional memo
     */
    async transferTokens(transferData) {
        return this.request('/api/wallet/transfer', {
            method: 'POST',
            body: JSON.stringify(transferData)
        });
    },

    /**
     * Get supported chains
     */
    async getSupportedChains() {
        return this.request('/api/wallet/supported-chains');
    },

    /**
     * Get wallet tokens
     * @param {string} avatarId - Avatar ID
     * @param {string} walletId - Wallet ID
     */
    async getWalletTokens(avatarId, walletId) {
        return this.request(`/api/wallet/avatar/${encodeURIComponent(avatarId)}/wallet/${encodeURIComponent(walletId)}/tokens`);
    },

    // ============================================
    // Bridge API Methods
    // ============================================

    /**
     * Get exchange rate for bridge
     * @param {string} fromToken - Source token
     * @param {string} toToken - Destination token
     */
    async getBridgeExchangeRate(fromToken, toToken) {
        return this.request(`/api/bridge/exchange-rate?fromToken=${encodeURIComponent(fromToken)}&toToken=${encodeURIComponent(toToken)}`);
    },

    /**
     * Create bridge order
     * @param {object} orderData - Order data
     * @param {string} orderData.avatarId - Avatar ID
     * @param {string} orderData.fromToken - Source token
     * @param {string} orderData.fromNetwork - Source network
     * @param {string} orderData.toToken - Destination token
     * @param {string} orderData.toNetwork - Destination network
     * @param {number} orderData.fromAmount - Amount to send
     * @param {string} [orderData.fromWalletId] - Source wallet ID
     * @param {string} [orderData.toWalletId] - Destination wallet ID
     */
    async createBridgeOrder(orderData) {
        return this.request('/api/bridge/create-order', {
            method: 'POST',
            body: JSON.stringify(orderData)
        });
    },

    /**
     * Get bridge transaction status
     * @param {string} orderId - Order ID
     */
    async getBridgeTransactionStatus(orderId) {
        return this.request(`/api/bridge/order/${encodeURIComponent(orderId)}/status`);
    },

    /**
     * Get bridge transactions for an avatar
     * @param {string} avatarId - Avatar ID
     */
    async getBridgeTransactions(avatarId) {
        return this.request(`/api/bridge/avatar/${encodeURIComponent(avatarId)}/transactions`);
    },

    // ============================================
    // Oracle API Methods
    // ============================================

    /**
     * Create custom oracle feed
     * @param {object} feedData - Feed configuration
     */
    async createOracleFeed(feedData) {
        return this.request('/api/oracle/feed', {
            method: 'POST',
            body: JSON.stringify(feedData)
        });
    },

    /**
     * Get oracle feed by ID
     * @param {string} feedId - Feed ID
     */
    async getOracleFeed(feedId) {
        return this.request(`/api/oracle/feed/${encodeURIComponent(feedId)}`);
    },

    /**
     * Get all oracle feeds for avatar
     * @param {string} avatarId - Avatar ID
     */
    async getOracleFeeds(avatarId) {
        return this.request(`/api/oracle/feeds?avatarId=${encodeURIComponent(avatarId)}`);
    },

    /**
     * Update oracle feed
     * @param {string} feedId - Feed ID
     * @param {object} updateData - Update data
     */
    async updateOracleFeed(feedId, updateData) {
        return this.request(`/api/oracle/feed/${encodeURIComponent(feedId)}`, {
            method: 'PUT',
            body: JSON.stringify(updateData)
        });
    },

    /**
     * Delete oracle feed
     * @param {string} feedId - Feed ID
     */
    async deleteOracleFeed(feedId) {
        return this.request(`/api/oracle/feed/${encodeURIComponent(feedId)}`, {
            method: 'DELETE'
        });
    },

    /**
     * Get public oracle feeds
     * @param {object} filters - Filter options
     */
    async getPublicOracleFeeds(filters = {}) {
        const params = new URLSearchParams();
        if (filters.category) params.append('category', filters.category);
        if (filters.limit) params.append('limit', filters.limit);
        if (filters.offset) params.append('offset', filters.offset);
        return this.request(`/api/oracle/feeds/public?${params.toString()}`);
    },

    /**
     * Subscribe to oracle feed
     * @param {string} feedId - Feed ID
     */
    async subscribeToOracleFeed(feedId) {
        return this.request(`/api/oracle/feed/${encodeURIComponent(feedId)}/subscribe`, {
            method: 'POST'
        });
    },

    /**
     * Unsubscribe from oracle feed
     * @param {string} feedId - Feed ID
     */
    async unsubscribeFromOracleFeed(feedId) {
        return this.request(`/api/oracle/feed/${encodeURIComponent(feedId)}/unsubscribe`, {
            method: 'POST'
        });
    },

    /**
     * Get oracle feed data
     * @param {string} feedId - Feed ID
     * @param {object} options - Query options
     */
    async getOracleFeedData(feedId, options = {}) {
        const params = new URLSearchParams();
        if (options.history) params.append('history', 'true');
        if (options.limit) params.append('limit', options.limit);
        return this.request(`/api/oracle/feed/${encodeURIComponent(feedId)}/data?${params.toString()}`);
    },

    /**
     * Get available providers for oracle
     */
    async getOracleProviders() {
        return this.request('/api/oracle/providers');
    }
};

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = oasisAPI;
}
