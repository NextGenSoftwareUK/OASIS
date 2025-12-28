/**
 * Wallet Management Module
 * Handles multi-chain wallet operations in the OASIS Portal
 * Follows portal module patterns (similar to bridge.js)
 */

// Wallet State
let walletState = {
    wallets: {},
    selectedWallet: null,
    loading: false,
    error: null,
    balances: {},
    transactions: []
};

// Provider type mapping for display
const PROVIDER_DISPLAY_NAMES = {
    'ZcashOASIS': 'Zcash',
    'AztecOASIS': 'Aztec',
    'SolanaOASIS': 'Solana',
    'EthereumOASIS': 'Ethereum',
    'PolygonOASIS': 'Polygon',
    'ArbitrumOASIS': 'Arbitrum',
    'BaseOASIS': 'Base',
    'OptimismOASIS': 'Optimism',
    'MidenOASIS': 'Miden',
    'StarknetOASIS': 'Starknet',
    'BNBChainOASIS': 'BNB Chain',
    'FantomOASIS': 'Fantom'
};

/**
 * Get authenticated avatar ID
 */
function getAvatarId() {
    // Try to use centralized authStore first
    if (typeof authStore !== 'undefined' && authStore.isAuthenticated()) {
        const avatar = authStore.getAvatar();
        if (avatar && (avatar.id || avatar.avatarId)) {
            return avatar.id || avatar.avatarId;
        }
    }
    
    // Fallback to direct localStorage access
    try {
        const authData = localStorage.getItem('oasis_auth');
        if (authData) {
            const auth = JSON.parse(authData);
            if (auth.avatar) {
                return auth.avatar.id || auth.avatar.avatarId;
            }
        }
    } catch (error) {
        console.error('Error getting avatar ID:', error);
    }
    
    return null;
}

/**
 * Get authenticated avatar object
 */
function getAvatar() {
    if (typeof authStore !== 'undefined' && authStore.isAuthenticated()) {
        return authStore.getAvatar();
    }
    
    try {
        const authData = localStorage.getItem('oasis_auth');
        if (authData) {
            const auth = JSON.parse(authData);
            return auth.avatar || null;
        }
    } catch (error) {
        console.error('Error getting avatar:', error);
    }
    
    return null;
}

/**
 * Load wallets for the authenticated avatar
 */
async function loadWalletsData() {
    const avatarId = getAvatarId();
    if (!avatarId) {
        walletState.error = 'Please sign in to view wallets';
        return;
    }

    walletState.loading = true;
    walletState.error = null;

    try {
        // Use oasisAPI to load wallets
        // Try the new endpoint format first: /api/wallet/avatar/{id}/wallets/{showOnlyDefault}/{decryptPrivateKeys}?providerType={providerType}
        let result = await oasisAPI.request(`/api/wallet/avatar/${avatarId}/wallets/false/false?providerType=All`);
        
        // Fallback to simpler endpoint if the above fails
        if (result.isError || !result.result) {
            result = await oasisAPI.getAvatarWallets(avatarId);
        }

        if (result.isError) {
            walletState.error = result.message || 'Failed to load wallets';
            walletState.wallets = {};
        } else {
            // Normalize wallet data
            walletState.wallets = result.result || result || {};
            
            // If result structure is different, try to extract wallets
            if (!walletState.wallets || Object.keys(walletState.wallets).length === 0) {
                // Check if wallets are in a different structure
                if (result.wallets) {
                    walletState.wallets = result.wallets;
                } else if (result.data) {
                    walletState.wallets = result.data;
                }
            }
        }
    } catch (error) {
        console.error('Error loading wallets:', error);
        walletState.error = error.message || 'Failed to load wallets';
        walletState.wallets = {};
    } finally {
        walletState.loading = false;
    }
}

/**
 * Get wallet balance (placeholder - can be enhanced)
 */
async function getWalletBalance(walletId, providerType) {
    // This can be enhanced to fetch actual balances
    return walletState.balances[walletId] || { amount: 0, currency: 'N/A' };
}

/**
 * Format number with decimals
 */
function formatNumber(num, decimals = 4) {
    if (num === null || num === undefined) return '0';
    const value = typeof num === 'string' ? parseFloat(num) : num;
    if (isNaN(value)) return '0';
    return value.toFixed(decimals).replace(/\.?0+$/, '');
}

/**
 * Shorten address for display
 */
function shortAddress(address, start = 6, end = 4) {
    if (!address) return 'N/A';
    if (address.length <= start + end) return address;
    return `${address.substring(0, start)}...${address.substring(address.length - end)}`;
}

/**
 * Render wallet dashboard
 */
function renderWalletDashboard() {
    const wallets = walletState.wallets || {};
    const walletCount = Object.values(wallets).reduce((sum, walletList) => sum + (walletList?.length || 0), 0);

    if (walletState.loading) {
        return `
            <div class="portal-section">
                <div class="portal-section-header">
                    <div>
                        <h2 class="portal-section-title">Multi-Chain Wallets</h2>
                        <p class="portal-section-subtitle">Manage your wallets across all supported blockchains</p>
                    </div>
                </div>
                <div class="portal-card" style="text-align: center; padding: 3rem;">
                    <p style="color: var(--text-secondary);">Loading wallets...</p>
                </div>
            </div>
        `;
    }

    if (walletState.error) {
        return `
            <div class="portal-section">
                <div class="portal-section-header">
                    <div>
                        <h2 class="portal-section-title">Multi-Chain Wallets</h2>
                        <p class="portal-section-subtitle">Manage your wallets across all supported blockchains</p>
                    </div>
                </div>
                <div class="portal-card" style="text-align: center; padding: 3rem;">
                    <p style="color: var(--text-secondary); margin-bottom: 1rem;">${walletState.error}</p>
                    <button class="btn-primary" onclick="loadWallets()">Retry</button>
                </div>
            </div>
        `;
    }

    if (walletCount === 0) {
        return `
            <div class="portal-section">
                <div class="portal-section-header">
                    <div>
                        <h2 class="portal-section-title">Multi-Chain Wallets</h2>
                        <p class="portal-section-subtitle">Manage your wallets across all supported blockchains</p>
                    </div>
                    <button class="btn-primary" onclick="showCreateWalletModal()">
                        Create Wallet
                    </button>
                </div>
                <div class="portal-card" style="text-align: center; padding: 3rem;">
                    <p style="color: var(--text-secondary); margin-bottom: 1rem;">No wallets found</p>
                    <p style="color: var(--text-tertiary); font-size: 0.875rem; margin-bottom: 2rem;">
                        Create a unified wallet to manage assets across multiple blockchains
                    </p>
                    <button class="btn-primary" onclick="showCreateWalletModal()">
                        Create Unified Wallet
                    </button>
                </div>
            </div>
        `;
    }

    // Render wallet list grouped by provider
    let walletListHTML = '';
    
    Object.entries(wallets).forEach(([providerType, walletList]) => {
        if (!walletList || walletList.length === 0) return;
        
        const displayName = PROVIDER_DISPLAY_NAMES[providerType] || providerType.replace('OASIS', '');
        
        walletList.forEach((wallet, index) => {
            const walletId = wallet.walletId || wallet.id || `wallet-${providerType}-${index}`;
            const address = wallet.walletAddress || wallet.address || 'N/A';
            const balance = wallet.balance || 0;
            const isDefault = wallet.isDefaultWallet || false;
            
            walletListHTML += `
                <div class="wallet-item" data-wallet-id="${walletId}" data-provider="${providerType}">
                    <div class="wallet-item-main">
                        <div class="wallet-item-info">
                            <div class="wallet-item-header">
                                <span class="wallet-item-provider">${displayName}</span>
                                ${isDefault ? '<span class="wallet-item-badge">Default</span>' : ''}
                            </div>
                            <div class="wallet-item-address">${shortAddress(address)}</div>
                            <div class="wallet-item-balance">${formatNumber(balance)} ${displayName}</div>
                        </div>
                        <div class="wallet-item-actions">
                            <button class="btn-text wallet-action-btn" onclick="viewWalletDetails('${walletId}', '${providerType}')">
                                View
                            </button>
                            <button class="btn-text wallet-action-btn" onclick="copyWalletAddress('${address}')">
                                Copy
                            </button>
                        </div>
                    </div>
                </div>
            `;
        });
    });

    return `
        <div class="portal-section">
            <div class="portal-section-header">
                <div>
                    <h2 class="portal-section-title">Multi-Chain Wallets</h2>
                    <p class="portal-section-subtitle">${walletCount} wallet${walletCount !== 1 ? 's' : ''} across ${Object.keys(wallets).length} blockchain${Object.keys(wallets).length !== 1 ? 's' : ''}</p>
                </div>
                <button class="btn-primary" onclick="showCreateWalletModal()">
                    Create Wallet
                </button>
            </div>
            
            <div class="portal-card">
                <div class="portal-card-header">
                    <div class="portal-card-title">Your Wallets</div>
                </div>
                <div class="wallet-list">
                    ${walletListHTML || '<div class="empty-state"><p class="empty-state-text">No wallets found</p></div>'}
                </div>
            </div>
        </div>
    `;
}

/**
 * Load wallets and render dashboard
 */
async function loadWallets() {
    const container = document.getElementById('wallet-content') || document.getElementById('tab-wallets');
    if (!container) {
        console.error('Wallet container not found');
        return;
    }

    // Show loading state
    container.innerHTML = renderWalletDashboard();
    
    // Load wallet data
    await loadWalletsData();
    
    // Render with data
    container.innerHTML = renderWalletDashboard();
    attachWalletEventListeners();
}

/**
 * Attach event listeners for wallet interactions
 */
function attachWalletEventListeners() {
    // Event listeners are attached via onclick handlers in the HTML
    // Additional event listeners can be added here if needed
}

/**
 * Show create wallet modal
 */
function showCreateWalletModal() {
    // TODO: Implement wallet creation modal
    alert('Wallet creation will be implemented next. For now, use the zypherpunk-wallet-ui to create wallets.');
}

/**
 * View wallet details
 */
function viewWalletDetails(walletId, providerType) {
    // TODO: Implement wallet details view
    const wallets = walletState.wallets || {};
    const providerWallets = wallets[providerType] || [];
    const wallet = providerWallets.find(w => (w.walletId || w.id) === walletId);
    
    if (wallet) {
        console.log('Viewing wallet:', wallet);
        // Can show a modal or navigate to details view
        alert(`Wallet Details:\n\nProvider: ${PROVIDER_DISPLAY_NAMES[providerType] || providerType}\nAddress: ${wallet.walletAddress || wallet.address || 'N/A'}\nBalance: ${wallet.balance || 0}`);
    }
}

/**
 * Copy wallet address to clipboard
 */
async function copyWalletAddress(address) {
    try {
        await navigator.clipboard.writeText(address);
        // Show toast notification if available
        if (typeof showToast === 'function') {
            showToast('Address copied to clipboard', 'success');
        } else {
            alert('Address copied to clipboard');
        }
    } catch (error) {
        console.error('Failed to copy address:', error);
        alert('Failed to copy address');
    }
}

// Make functions available globally for onclick handlers
window.loadWallets = loadWallets;
window.showCreateWalletModal = showCreateWalletModal;
window.viewWalletDetails = viewWalletDetails;
window.copyWalletAddress = copyWalletAddress;

