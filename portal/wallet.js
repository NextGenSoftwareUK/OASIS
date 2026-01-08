/**
 * Wallet Management Module - Multi-chain wallet management interface
 * Implements comprehensive wallet functionality for the OASIS Portal
 */

const walletState = {
    baseUrl: null,
    authToken: null,
    avatarId: null,
    wallets: {},
    supportedChains: [],
    selectedWallet: null,
    loading: false,
    error: null
};

/**
 * Get API Base URL
 */
function getWalletAPIBaseURL() {
    const hostname = window.location.hostname;
    const protocol = window.location.protocol;
    
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
        return 'https://localhost:5000';
    }
    
    if (protocol === 'https:') {
        return 'https://api.oasisplatform.world';
    }
    return 'http://api.oasisplatform.world';
}

/**
 * Initialize Wallet Module
 */
function initWalletModule() {
    const authData = localStorage.getItem('oasis_auth');
    if (authData) {
        try {
            const auth = JSON.parse(authData);
            walletState.authToken = auth.token;
            walletState.avatarId = auth.avatarId || auth.avatar?.id || auth.avatar?.avatarId;
        } catch (e) {
            console.error('Error parsing auth data:', e);
        }
    }
    
    walletState.baseUrl = getWalletAPIBaseURL();
    
    loadWalletDashboard();
}

/**
 * Load Wallet Dashboard
 */
async function loadWalletDashboard() {
    const container = document.getElementById('tab-wallets');
    if (!container) return;

    // Update container to include content area
    if (!container.querySelector('#wallet-content')) {
        const contentDiv = document.createElement('div');
        contentDiv.id = 'wallet-content';
        container.appendChild(contentDiv);
    }

    const contentDiv = container.querySelector('#wallet-content');
    contentDiv.innerHTML = renderWalletDashboard();

    // Load data
    await loadSupportedChains();
    await loadWallets();
    await loadPortfolioValue();
}

/**
 * Render Wallet Dashboard
 */
function renderWalletDashboard() {
    return `
        <div class="portal-section">
            <div style="margin-bottom: 2rem;">
                <h2 style="
                    font-size: 1.5rem;
                    font-weight: 300;
                    color: var(--text-primary);
                    margin-bottom: 0.5rem;
                ">Multi-Chain Wallets</h2>
                <p style="
                    font-size: 0.875rem;
                    color: var(--text-secondary);
                ">Manage your wallets across all supported blockchains</p>
            </div>

            ${renderWalletOverview()}
            ${renderWalletActions()}
            ${renderWalletsList()}
            ${renderWalletDetails()}
        </div>
    `;
}

/**
 * Render Wallet Overview (Portfolio Stats)
 */
function renderWalletOverview() {
    return `
        <div class="portal-section" style="margin-bottom: 2rem;">
            <h3 style="
                font-size: 1rem;
                font-weight: 500;
                color: var(--text-primary);
                margin-bottom: 1rem;
            ">Portfolio Overview</h3>
            <div class="stats-grid">
                <div class="stat-card">
                    <div class="stat-label">Total Wallets</div>
                    <div class="stat-value" id="wallet-total-count">0</div>
                    <div class="stat-detail">Across all chains</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Portfolio Value</div>
                    <div class="stat-value" id="wallet-portfolio-value">$0.00</div>
                    <div class="stat-detail" id="wallet-portfolio-detail">Loading...</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Active Chains</div>
                    <div class="stat-value" id="wallet-active-chains">0</div>
                    <div class="stat-detail">Blockchains with wallets</div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Render Wallet Actions (Create, Import, etc.)
 */
function renderWalletActions() {
    return `
        <div class="portal-section" style="margin-bottom: 2rem;">
            <div style="display: flex; gap: 1rem; flex-wrap: wrap;">
                <button class="portal-btn portal-btn-primary" onclick="showCreateWalletModal()">
                    <span>+</span> Create Wallet
                </button>
                <button class="portal-btn portal-btn-secondary" onclick="showImportWalletModal()">
                    <span>📥</span> Import Wallet
                </button>
                <button class="portal-btn portal-btn-secondary" onclick="refreshWallets()">
                    <span>🔄</span> Refresh
                </button>
            </div>
        </div>
    `;
}

/**
 * Render Wallets List
 */
function renderWalletsList() {
    return `
        <div class="portal-section" style="margin-bottom: 2rem;">
            <h3 style="
                font-size: 1rem;
                font-weight: 500;
                color: var(--text-primary);
                margin-bottom: 1rem;
            ">Your Wallets</h3>
            <div id="wallets-list-container">
                <div class="portal-card" style="padding: 2rem; text-align: center;">
                    <div class="loading-spinner" style="display: none;" id="wallets-loading">
                        <div class="spinner"></div>
                        <p>Loading wallets...</p>
                    </div>
                    <div id="wallets-list-content">
                        <p style="color: var(--text-secondary);">Loading wallets...</p>
                    </div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Render Wallet Details (Selected Wallet Info)
 */
function renderWalletDetails() {
    return `
        <div class="portal-section" id="wallet-details-section" style="display: none;">
            <h3 style="
                font-size: 1rem;
                font-weight: 500;
                color: var(--text-primary);
                margin-bottom: 1rem;
            ">Wallet Details</h3>
            <div class="portal-card" id="wallet-details-content">
                <!-- Wallet details will be loaded here -->
            </div>
        </div>
    `;
}

/**
 * Load Supported Chains
 */
async function loadSupportedChains() {
    try {
        const response = await fetch(`${walletState.baseUrl}/api/wallet/supported-chains`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`Failed to load supported chains: ${response.status}`);
        }

        const result = await response.json();
        if (result.isError) {
            throw new Error(result.message || 'Failed to load supported chains');
        }

        walletState.supportedChains = result.result || [];
        console.log('Supported chains loaded:', walletState.supportedChains);
    } catch (error) {
        console.error('Error loading supported chains:', error);
        walletState.error = error.message;
    }
}

/**
 * Load Wallets for Avatar
 */
async function loadWallets() {
    if (!walletState.avatarId) {
        console.warn('No avatar ID available');
        return;
    }

    const loadingEl = document.getElementById('wallets-loading');
    const contentEl = document.getElementById('wallets-list-content');
    
    if (loadingEl) loadingEl.style.display = 'block';
    if (contentEl) contentEl.innerHTML = '<p>Loading wallets...</p>';

    walletState.loading = true;

    try {
        const response = await fetch(`${walletState.baseUrl}/api/wallet/avatar/${walletState.avatarId}/wallets`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Authorization': `Bearer ${walletState.authToken}`
            }
        });

        if (!response.ok) {
            if (response.status === 401) {
                throw new Error('Authentication required. Please log in.');
            }
            throw new Error(`Failed to load wallets: ${response.status}`);
        }

        const result = await response.json();
        if (result.isError) {
            throw new Error(result.message || 'Failed to load wallets');
        }

        walletState.wallets = result.result || {};
        
        // Update UI
        updateWalletsList();
        updateWalletStats();

    } catch (error) {
        console.error('Error loading wallets:', error);
        walletState.error = error.message;
        if (contentEl) {
            contentEl.innerHTML = `
                <div style="color: var(--error-color, #ff4444); padding: 1rem;">
                    <p><strong>Error loading wallets:</strong></p>
                    <p>${error.message}</p>
                    <button class="portal-btn portal-btn-secondary" onclick="loadWallets()" style="margin-top: 1rem;">
                        Retry
                    </button>
                </div>
            `;
        }
    } finally {
        walletState.loading = false;
        if (loadingEl) loadingEl.style.display = 'none';
    }
}

/**
 * Update Wallets List UI
 */
function updateWalletsList() {
    const contentEl = document.getElementById('wallets-list-content');
    if (!contentEl) return;

    const wallets = walletState.wallets;
    const walletCount = Object.values(wallets).reduce((sum, list) => sum + (list?.length || 0), 0);

    if (walletCount === 0) {
        contentEl.innerHTML = `
            <div style="text-align: center; padding: 2rem;">
                <p style="color: var(--text-secondary); margin-bottom: 1rem;">
                    No wallets found. Create your first wallet to get started.
                </p>
                <button class="portal-btn portal-btn-primary" onclick="showCreateWalletModal()">
                    Create Wallet
                </button>
            </div>
        `;
        return;
    }

    let html = '<div style="display: grid; gap: 1rem;">';
    
    // Group wallets by provider type
    for (const [providerType, walletList] of Object.entries(wallets)) {
        if (!walletList || walletList.length === 0) continue;

        html += `
            <div class="portal-card" style="padding: 1.5rem;">
                <h4 style="
                    font-size: 0.875rem;
                    font-weight: 600;
                    color: var(--text-primary);
                    margin-bottom: 1rem;
                    text-transform: uppercase;
                    letter-spacing: 0.05em;
                ">${providerType}</h4>
                <div style="display: grid; gap: 1rem;">
        `;

        for (const wallet of walletList) {
            const walletId = wallet.id || wallet.walletId;
            const address = wallet.address || wallet.publicKey || 'N/A';
            const balance = wallet.balance || '0';
            
            html += `
                <div class="wallet-item" style="
                    padding: 1rem;
                    border: 1px solid var(--border-color, rgba(255,255,255,0.1));
                    border-radius: 8px;
                    cursor: pointer;
                    transition: all 0.2s;
                " onclick="selectWallet('${walletId}', '${providerType}')" 
                   onmouseover="this.style.borderColor='var(--accent-color, #00d4ff)'"
                   onmouseout="this.style.borderColor='var(--border-color, rgba(255,255,255,0.1))'">
                    <div style="display: flex; justify-content: space-between; align-items: start;">
                        <div style="flex: 1;">
                            <div style="font-weight: 500; color: var(--text-primary); margin-bottom: 0.5rem;">
                                ${wallet.name || 'Unnamed Wallet'}
                            </div>
                            <div style="font-size: 0.75rem; color: var(--text-secondary); font-family: monospace; word-break: break-all;">
                                ${address}
                            </div>
                        </div>
                        <div style="text-align: right;">
                            <div style="font-weight: 600; color: var(--text-primary); margin-bottom: 0.25rem;">
                                ${formatBalance(balance)}
                            </div>
                            <button class="portal-btn portal-btn-small" onclick="event.stopPropagation(); sendToken('${walletId}', '${providerType}')">
                                Send
                            </button>
                        </div>
                    </div>
                </div>
            `;
        }

        html += `
                </div>
            </div>
        `;
    }

    html += '</div>';
    contentEl.innerHTML = html;
}

/**
 * Update Wallet Stats
 */
function updateWalletStats() {
    const wallets = walletState.wallets;
    const totalCount = Object.values(wallets).reduce((sum, list) => sum + (list?.length || 0), 0);
    const activeChains = Object.keys(wallets).length;

    const countEl = document.getElementById('wallet-total-count');
    const chainsEl = document.getElementById('wallet-active-chains');
    
    if (countEl) countEl.textContent = totalCount;
    if (chainsEl) chainsEl.textContent = activeChains;
    
    // Also update dashboard stat if it exists
    const dashboardStatEl = document.getElementById('statWallets');
    if (dashboardStatEl) {
        dashboardStatEl.textContent = totalCount;
    }
}

/**
 * Get Wallet Count for Dashboard
 * This function can be called from loadPortalData to update dashboard stats
 */
async function getWalletCount() {
    if (!walletState.avatarId) return 0;
    
    try {
        const response = await fetch(`${walletState.baseUrl}/api/wallet/avatar/${walletState.avatarId}/wallets`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Authorization': `Bearer ${walletState.authToken}`
            }
        });

        if (!response.ok) {
            return 0;
        }

        const result = await response.json();
        if (result.isError) {
            return 0;
        }

        const wallets = result.result || {};
        const totalCount = Object.values(wallets).reduce((sum, list) => sum + (list?.length || 0), 0);
        return totalCount;
    } catch (error) {
        console.error('Error getting wallet count:', error);
        return 0;
    }
}

/**
 * Load Portfolio Value
 */
async function loadPortfolioValue() {
    if (!walletState.avatarId) return;

    try {
        const response = await fetch(`${walletState.baseUrl}/api/wallet/avatar/${walletState.avatarId}/portfolio/value`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Authorization': `Bearer ${walletState.authToken}`
            }
        });

        if (!response.ok) {
            throw new Error(`Failed to load portfolio value: ${response.status}`);
        }

        const result = await response.json();
        if (result.isError) {
            throw new Error(result.message || 'Failed to load portfolio value');
        }

        const portfolio = result.result;
        const valueEl = document.getElementById('wallet-portfolio-value');
        const detailEl = document.getElementById('wallet-portfolio-detail');

        if (valueEl && portfolio?.totalValueUSD) {
            valueEl.textContent = `$${formatNumber(portfolio.totalValueUSD)}`;
        }
        
        if (detailEl && portfolio?.lastUpdated) {
            detailEl.textContent = `Updated ${formatDate(portfolio.lastUpdated)}`;
        }

    } catch (error) {
        console.error('Error loading portfolio value:', error);
        const valueEl = document.getElementById('wallet-portfolio-value');
        if (valueEl) valueEl.textContent = 'N/A';
    }
}

/**
 * Select Wallet
 */
function selectWallet(walletId, providerType) {
    walletState.selectedWallet = { walletId, providerType };
    loadWalletDetails(walletId, providerType);
}

/**
 * Load Wallet Details
 */
async function loadWalletDetails(walletId, providerType) {
    const sectionEl = document.getElementById('wallet-details-section');
    const contentEl = document.getElementById('wallet-details-content');
    
    if (!sectionEl || !contentEl) return;

    sectionEl.style.display = 'block';
    contentEl.innerHTML = '<p>Loading wallet details...</p>';

    try {
        // Load wallet tokens
        const tokensResponse = await fetch(`${walletState.baseUrl}/api/wallet/avatar/${walletState.avatarId}/wallet/${walletId}/tokens`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Authorization': `Bearer ${walletState.authToken}`
            }
        });

        // Load wallet analytics
        const analyticsResponse = await fetch(`${walletState.baseUrl}/api/wallet/avatar/${walletState.avatarId}/wallet/${walletId}/analytics`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Authorization': `Bearer ${walletState.authToken}`
            }
        });

        let tokens = [];
        let analytics = null;

        if (tokensResponse.ok) {
            const tokensResult = await tokensResponse.json();
            if (!tokensResult.isError) {
                tokens = tokensResult.result || [];
            }
        }

        if (analyticsResponse.ok) {
            const analyticsResult = await analyticsResponse.json();
            if (!analyticsResult.isError) {
                analytics = analyticsResult.result;
            }
        }

        // Find wallet in state
        const wallet = findWalletById(walletId, providerType);

        contentEl.innerHTML = renderWalletDetailsContent(wallet, tokens, analytics);

    } catch (error) {
        console.error('Error loading wallet details:', error);
        contentEl.innerHTML = `
            <div style="color: var(--error-color, #ff4444); padding: 1rem;">
                <p><strong>Error loading wallet details:</strong></p>
                <p>${error.message}</p>
            </div>
        `;
    }
}

/**
 * Find Wallet by ID
 */
function findWalletById(walletId, providerType) {
    const wallets = walletState.wallets[providerType] || [];
    return wallets.find(w => (w.id || w.walletId) === walletId);
}

/**
 * Render Wallet Details Content
 */
function renderWalletDetailsContent(wallet, tokens, analytics) {
    const address = wallet?.address || wallet?.publicKey || 'N/A';
    
    return `
        <div style="padding: 1.5rem;">
            <div style="margin-bottom: 2rem;">
                <h4 style="font-size: 1.125rem; font-weight: 500; color: var(--text-primary); margin-bottom: 0.5rem;">
                    ${wallet?.name || 'Wallet Details'}
                </h4>
                <div style="font-size: 0.75rem; color: var(--text-secondary); font-family: monospace; word-break: break-all;">
                    ${address}
                </div>
            </div>

            ${tokens.length > 0 ? renderWalletTokens(tokens) : '<p style="color: var(--text-secondary);">No tokens found</p>'}

            ${analytics ? renderWalletAnalytics(analytics) : ''}
        </div>
    `;
}

/**
 * Render Wallet Tokens
 */
function renderWalletTokens(tokens) {
    return `
        <div style="margin-bottom: 2rem;">
            <h5 style="font-size: 0.875rem; font-weight: 500; color: var(--text-primary); margin-bottom: 1rem;">
                Tokens
            </h5>
            <div style="display: grid; gap: 0.75rem;">
                ${tokens.map(token => `
                    <div style="
                        padding: 1rem;
                        border: 1px solid var(--border-color, rgba(255,255,255,0.1));
                        border-radius: 8px;
                        display: flex;
                        justify-content: space-between;
                        align-items: center;
                    ">
                        <div>
                            <div style="font-weight: 500; color: var(--text-primary);">
                                ${token.symbol || 'N/A'}
                            </div>
                            <div style="font-size: 0.75rem; color: var(--text-secondary);">
                                ${token.name || ''}
                            </div>
                        </div>
                        <div style="text-align: right;">
                            <div style="font-weight: 600; color: var(--text-primary);">
                                ${formatBalance(token.amount || '0')}
                            </div>
                            <div style="font-size: 0.75rem; color: var(--text-secondary);">
                                $${formatNumber(token.usdValue || 0)}
                            </div>
                        </div>
                    </div>
                `).join('')}
            </div>
        </div>
    `;
}

/**
 * Render Wallet Analytics
 */
function renderWalletAnalytics(analytics) {
    return `
        <div>
            <h5 style="font-size: 0.875rem; font-weight: 500; color: var(--text-primary); margin-bottom: 1rem;">
                Analytics
            </h5>
            <div class="stats-grid" style="grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));">
                <div class="stat-card">
                    <div class="stat-label">Total Transactions</div>
                    <div class="stat-value">${analytics.totalTransactions || 0}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Total Volume</div>
                    <div class="stat-value">$${formatNumber(analytics.totalVolume || 0)}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Avg Transaction</div>
                    <div class="stat-value">$${formatNumber(analytics.averageTransaction || 0)}</div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Show Create Wallet Modal
 */
function showCreateWalletModal() {
    // Simple prompt for now - can be enhanced with a proper modal
    const providerType = prompt('Enter provider type (e.g., Ethereum, Solana, Bitcoin):');
    if (!providerType) return;

    const name = prompt('Enter wallet name:');
    if (!name) return;

    createWallet(providerType, name);
}

/**
 * Create Wallet
 */
async function createWallet(providerType, name, description = '') {
    if (!walletState.avatarId) {
        alert('Please log in to create a wallet');
        return;
    }

    try {
        // Map provider type string to ProviderType enum value
        // This is a simplified mapping - adjust based on actual ProviderType enum
        const providerTypeEnum = providerType.toUpperCase().replace(/\s+/g, '');

        const response = await fetch(`${walletState.baseUrl}/api/wallet/avatar/${walletState.avatarId}/create-wallet`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Authorization': `Bearer ${walletState.authToken}`
            },
            body: JSON.stringify({
                name: name,
                description: description,
                walletProviderType: providerTypeEnum,
                generateKeyPair: true,
                isDefaultWallet: false
            })
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({ message: `HTTP ${response.status}` }));
            throw new Error(errorData.message || errorData.error || `Failed to create wallet: ${response.status}`);
        }

        const result = await response.json();
        if (result.isError) {
            throw new Error(result.message || 'Failed to create wallet');
        }

        alert('Wallet created successfully!');
        await loadWallets();

    } catch (error) {
        console.error('Error creating wallet:', error);
        alert(`Error creating wallet: ${error.message}`);
    }
}

/**
 * Show Import Wallet Modal
 */
function showImportWalletModal() {
    const keyType = prompt('Import type:\n1. Private Key\n2. Public Key\n\nEnter 1 or 2:');
    if (!keyType) return;

    const key = prompt('Enter the key:');
    if (!key) return;

    const providerType = prompt('Enter provider type (e.g., Ethereum, Solana):');
    if (!providerType) return;

    importWallet(keyType === '1' ? 'private' : 'public', key, providerType);
}

/**
 * Import Wallet
 */
async function importWallet(keyType, key, providerType) {
    if (!walletState.avatarId) {
        alert('Please log in to import a wallet');
        return;
    }

    try {
        const endpoint = keyType === 'private' 
            ? `avatar/${walletState.avatarId}/import/private-key`
            : `avatar/${walletState.avatarId}/import/public-key`;

        const providerTypeEnum = providerType.toUpperCase().replace(/\s+/g, '');

        const response = await fetch(`${walletState.baseUrl}/api/wallet/${endpoint}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Authorization': `Bearer ${walletState.authToken}`
            },
            body: JSON.stringify({
                key: key,
                providerToImportTo: providerTypeEnum
            })
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({ message: `HTTP ${response.status}` }));
            throw new Error(errorData.message || errorData.error || `Failed to import wallet: ${response.status}`);
        }

        const result = await response.json();
        if (result.isError) {
            throw new Error(result.message || 'Failed to import wallet');
        }

        alert('Wallet imported successfully!');
        await loadWallets();

    } catch (error) {
        console.error('Error importing wallet:', error);
        alert(`Error importing wallet: ${error.message}`);
    }
}

/**
 * Send Token
 */
async function sendToken(walletId, providerType) {
    // Find the wallet to get its address
    const wallet = findWalletById(walletId, providerType);
    if (!wallet) {
        alert('Wallet not found');
        return;
    }

    const fromAddress = wallet.address || wallet.publicKey;
    if (!fromAddress) {
        alert('Wallet address not found');
        return;
    }

    const toAddress = prompt('Enter recipient address:');
    if (!toAddress) return;

    const amount = prompt('Enter amount:');
    if (!amount || isNaN(parseFloat(amount))) {
        alert('Invalid amount');
        return;
    }

    const memoText = prompt('Enter memo (optional):') || '';

    try {
        // Map provider type string to ProviderType enum value
        // The API should accept the provider type as a string or number
        const providerTypeValue = providerType; // Send as-is, API should handle conversion

        const response = await fetch(`${walletState.baseUrl}/api/wallet/send_token`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Authorization': `Bearer ${walletState.authToken}`
            },
            body: JSON.stringify({
                fromWalletAddress: fromAddress,
                toWalletAddress: toAddress,
                amount: parseFloat(amount),
                memoText: memoText,
                fromProvider: providerTypeValue,
                toProvider: providerTypeValue
            })
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({ message: `HTTP ${response.status}` }));
            throw new Error(errorData.message || errorData.error || `Failed to send token: ${response.status}`);
        }

        const result = await response.json();
        if (result.isError) {
            throw new Error(result.message || 'Failed to send token');
        }

        alert('Token sent successfully!');
        await loadWallets();
        if (walletState.selectedWallet) {
            await loadWalletDetails(walletState.selectedWallet.walletId, walletState.selectedWallet.providerType);
        }

    } catch (error) {
        console.error('Error sending token:', error);
        alert(`Error sending token: ${error.message}`);
    }
}

/**
 * Refresh Wallets
 */
async function refreshWallets() {
    await loadWallets();
    await loadPortfolioValue();
}

/**
 * Format Balance
 */
function formatBalance(balance) {
    if (!balance || balance === '0') return '0';
    const num = parseFloat(balance);
    if (isNaN(num)) return balance;
    return num.toLocaleString('en-US', { maximumFractionDigits: 6 });
}

/**
 * Format Number
 */
function formatNumber(num) {
    if (!num) return '0';
    const n = parseFloat(num);
    if (isNaN(n)) return num.toString();
    return n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

/**
 * Format Date
 */
function formatDate(dateString) {
    if (!dateString) return 'N/A';
    try {
        const date = new Date(dateString);
        return date.toLocaleString();
    } catch (e) {
        return dateString;
    }
}

// Initialize when tab is switched
if (typeof switchTab === 'function') {
    const originalSwitchTab = switchTab;
    window.switchTab = function(tab) {
        originalSwitchTab(tab);
        if (tab === 'wallets') {
            initWalletModule();
        }
    };
} else {
    // Fallback: initialize on page load if wallets tab is active
    document.addEventListener('DOMContentLoaded', () => {
        const walletsTab = document.getElementById('tab-wallets');
        if (walletsTab && walletsTab.style.display !== 'none') {
            initWalletModule();
        }
    });
}

