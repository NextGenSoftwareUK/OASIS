/**
 * Universal Asset Bridge Frontend
 * Handles cross-chain token swaps and bridge transactions
 */

// Bridge State
let bridgeState = {
    selectedFrom: { network: 'Solana', token: 'SOL' },
    selectedTo: { network: 'Ethereum', token: 'ETH' },
    fromAmount: null,
    toAmount: null,
    exchangeRate: null,
    isFetchingRate: false,
    transactions: [],
    isSubmitting: false,
    wallets: [],
    selectedFromWallet: null,
    selectedToWallet: null,
    walletBalances: {}
};

// Supported tokens and networks with logo paths
// Using logos from logos/ directory (symlinked to oasisweb4 site/new-v2/logos/)
const CRYPTO_OPTIONS = [
    { token: 'SOL', name: 'Solana', network: 'Solana', logo: 'logos/solana.svg', description: 'Fast and low-cost blockchain' },
    { token: 'ETH', name: 'Ethereum', network: 'Ethereum', logo: 'logos/ethereum.svg', description: 'Largest smart contract platform' },
    { token: 'MATIC', name: 'Polygon', network: 'Polygon', logo: 'logos/polygon.svg', description: 'Ethereum scaling solution' },
    { token: 'BASE', name: 'Base', network: 'Base', logo: 'logos/base.png', description: 'Coinbase Layer 2' },
    { token: 'ARB', name: 'Arbitrum', network: 'Arbitrum', logo: 'logos/arbitrum.png', description: 'Ethereum Layer 2' },
    { token: 'OP', name: 'Optimism', network: 'Optimism', logo: 'logos/optimism.svg', description: 'Ethereum Layer 2' },
    { token: 'BNB', name: 'BNB Chain', network: 'BNB Chain', logo: 'logos/bnb.svg', description: 'Binance Smart Chain' },
    { token: 'AVAX', name: 'Avalanche', network: 'Avalanche', logo: 'logos/avalanche.svg', description: 'High-performance blockchain' },
    { token: 'FTM', name: 'Fantom', network: 'Fantom', logo: 'logos/fantom.svg', description: 'Fast and scalable' },
    { token: 'XRD', name: 'Radix', network: 'Radix', logo: 'logos/radix.svg', description: 'DeFi-focused blockchain' }
];

// Mock exchange rates (fallback)
const MOCK_RATES = {
    'SOL-ETH': 0.05,
    'ETH-SOL': 20,
    'SOL-MATIC': 2.5,
    'MATIC-SOL': 0.4,
    'ETH-MATIC': 50,
    'MATIC-ETH': 0.02,
    'SOL-BASE': 0.05,
    'BASE-SOL': 20,
    'ETH-BASE': 1,
    'BASE-ETH': 1,
    'SOL-ARB': 0.05,
    'ARB-SOL': 20,
    'ETH-ARB': 1,
    'ARB-ETH': 1,
    'SOL-OP': 0.05,
    'OP-SOL': 20,
    'SOL-BNB': 0.2,
    'BNB-SOL': 5,
    'SOL-AVAX': 0.4,
    'AVAX-SOL': 2.5,
    'SOL-FTM': 20,
    'FTM-SOL': 0.05,
    'SOL-XRD': 50,
    'XRD-SOL': 0.02
};

/**
 * Get authenticated avatar object
 */
function getAvatar() {
    // Try to use centralized authStore first
    if (typeof authStore !== 'undefined' && authStore.isAuthenticated()) {
        return authStore.getAvatar();
    }
    
    // Fallback to direct localStorage access
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
 * Check if user is authenticated
 */
function isAuthenticated() {
    // Try to use centralized authStore first
    if (typeof authStore !== 'undefined') {
        return authStore.isAuthenticated();
    }
    
    // Fallback to direct localStorage access
    try {
        const authData = localStorage.getItem('oasis_auth');
        if (authData) {
            const auth = JSON.parse(authData);
            return !!(auth.token && auth.avatar);
        }
    } catch (error) {
        console.error('Error checking authentication:', error);
    }
    
    return false;
}

/**
 * Get avatar ID from auth
 * Uses centralized authStore if available, otherwise falls back to localStorage
 */
function getAvatarId() {
    // Try to use centralized authStore first (from api/auth.js)
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
    // Try to use centralized authStore first
    if (typeof authStore !== 'undefined' && authStore.isAuthenticated()) {
        return authStore.getAvatar();
    }
    
    // Fallback to direct localStorage access
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
 * Check if user is authenticated
 */
function isAuthenticated() {
    // Try to use centralized authStore first
    if (typeof authStore !== 'undefined') {
        return authStore.isAuthenticated();
    }
    
    // Fallback to direct localStorage access
    try {
        const authData = localStorage.getItem('oasis_auth');
        if (authData) {
            const auth = JSON.parse(authData);
            return !!(auth.token && auth.avatar);
        }
    } catch (error) {
        console.error('Error checking authentication:', error);
    }
    
    return false;
}

/**
 * Get exchange rate from API or fallback
 */
async function getExchangeRate(fromToken, toToken) {
    if (fromToken === toToken) return 1;

    try {
        // Try OASIS API first
        if (typeof oasisAPI !== 'undefined' && oasisAPI.request) {
            const response = await oasisAPI.request(
                `/api/bridge/exchange-rate?fromToken=${encodeURIComponent(fromToken)}&toToken=${encodeURIComponent(toToken)}`
            );
            if (!response.isError && response.result?.rate) {
                return response.result.rate;
            }
        }
    } catch (error) {
        console.warn('API exchange rate failed, using fallback:', error);
    }

    // Fallback to CoinGecko
    try {
        const fromCoinId = getCoinGeckoId(fromToken);
        const toCoinId = getCoinGeckoId(toToken);
        
        if (fromCoinId && toCoinId) {
            const response = await fetch(
                `https://api.coingecko.com/api/v3/simple/price?ids=${fromCoinId},${toCoinId}&vs_currencies=usd`
            );
            if (response.ok) {
                const data = await response.json();
                const fromPrice = data[fromCoinId]?.usd;
                const toPrice = data[toCoinId]?.usd;
                if (fromPrice && toPrice) {
                    return fromPrice / toPrice;
                }
            }
        }
    } catch (error) {
        console.warn('CoinGecko API failed, using mock rate:', error);
    }

    // Final fallback to mock rates
    const key = `${fromToken}-${toToken}`;
    return MOCK_RATES[key] || 1;
}

function getCoinGeckoId(token) {
    const ids = {
        'SOL': 'solana',
        'ETH': 'ethereum',
        'MATIC': 'matic-network',
        'BASE': 'base',
        'ARB': 'arbitrum',
        'OP': 'optimism',
        'BNB': 'binancecoin',
        'AVAX': 'avalanche-2',
        'FTM': 'fantom',
        'XRD': 'radix'
    };
    return ids[token.toUpperCase()];
}

/**
 * Load bridge data
 */
async function loadBridge() {
    const container = document.getElementById('bridge-content');
    if (!container) return;

    const authenticated = isAuthenticated();

    // Show loading state
    container.innerHTML = `
        <div class="portal-section">
            <div class="empty-state">
                <p class="empty-state-text">Loading bridge...</p>
            </div>
        </div>
    `;

    try {
        // Load wallets and transactions if authenticated
        let wallets = [];
        let transactions = [];
        
        if (authenticated) {
            const avatarId = getAvatarId();
            if (avatarId && typeof oasisAPI !== 'undefined') {
                // Load wallets
                try {
                    const walletsResult = await oasisAPI.getAvatarWallets(avatarId).catch(() => ({ isError: true, result: [] }));
                    wallets = walletsResult.result || [];
                    
                    // Auto-select wallets for from/to networks
                    if (wallets.length > 0) {
                        const fromWallet = wallets.find(w => 
                            w.providerType?.toLowerCase().includes(bridgeState.selectedFrom.network.toLowerCase()) ||
                            w.chain?.toLowerCase() === bridgeState.selectedFrom.network.toLowerCase()
                        );
                        const toWallet = wallets.find(w => 
                            w.providerType?.toLowerCase().includes(bridgeState.selectedTo.network.toLowerCase()) ||
                            w.chain?.toLowerCase() === bridgeState.selectedTo.network.toLowerCase()
                        );
                        
                        bridgeState.selectedFromWallet = fromWallet || null;
                        bridgeState.selectedToWallet = toWallet || null;
                        
                        // Load balances for selected wallets
                        if (fromWallet) {
                            await loadWalletBalance(fromWallet.id, 'from');
                        }
                        if (toWallet) {
                            await loadWalletBalance(toWallet.id, 'to');
                        }
                    }
                } catch (error) {
                    console.warn('Error loading wallets:', error);
                }
                
                // Load bridge transactions
                try {
                    const transactionsResult = await oasisAPI.getBridgeTransactions(avatarId).catch(() => ({ isError: true, result: [] }));
                    transactions = transactionsResult.result || [];
                } catch (error) {
                    console.warn('Error loading transactions:', error);
                }
            }
        }

        bridgeState.wallets = wallets;
        bridgeState.transactions = transactions;

        // Load initial exchange rate
        await updateExchangeRate();

        // Render the page
        renderBridgePage(authenticated);
    } catch (error) {
        console.error('Error loading bridge:', error);
        renderBridgePage(authenticated);
    }
}

/**
 * Load wallet balance
 */
async function loadWalletBalance(walletId, direction) {
    if (!walletId || typeof oasisAPI === 'undefined') return;
    
    try {
        const result = await oasisAPI.getWalletBalance(walletId);
        if (!result.isError && result.result) {
            bridgeState.walletBalances[walletId] = result.result;
        }
    } catch (error) {
        console.warn(`Error loading balance for wallet ${walletId}:`, error);
    }
}

/**
 * Update exchange rate
 */
async function updateExchangeRate() {
    const { selectedFrom, selectedTo } = bridgeState;
    if (!selectedFrom.token || !selectedTo.token) return;

    bridgeState.isFetchingRate = true;
    updateRateDisplay();

    try {
        const rate = await getExchangeRate(selectedFrom.token, selectedTo.token);
        bridgeState.exchangeRate = rate;
        
        // Update to amount if from amount is set
        if (bridgeState.fromAmount) {
            bridgeState.toAmount = bridgeState.fromAmount * rate;
            updateAmounts();
        }
    } catch (error) {
        console.error('Error fetching exchange rate:', error);
        bridgeState.exchangeRate = MOCK_RATES[`${selectedFrom.token}-${selectedTo.token}`] || 1;
    } finally {
        bridgeState.isFetchingRate = false;
        updateRateDisplay();
    }
}

/**
 * Update rate display
 */
function updateRateDisplay() {
    const rateEl = document.getElementById('bridge-exchange-rate');
    if (!rateEl) return;

    if (bridgeState.isFetchingRate) {
        rateEl.innerHTML = '<span class="loading-spinner"></span> Loading rate...';
        return;
    }

    if (bridgeState.exchangeRate) {
        const { selectedFrom, selectedTo } = bridgeState;
        rateEl.textContent = `1 ${selectedFrom.token} = ${formatNumber(bridgeState.exchangeRate)} ${selectedTo.token}`;
    } else {
        rateEl.textContent = 'Rate unavailable';
    }
}

/**
 * Update amount displays
 */
function updateAmounts() {
    const fromAmountEl = document.getElementById('bridge-from-amount');
    const toAmountEl = document.getElementById('bridge-to-amount');
    
    if (fromAmountEl) {
        fromAmountEl.value = bridgeState.fromAmount || '';
    }
    if (toAmountEl) {
        toAmountEl.value = bridgeState.toAmount ? formatNumber(bridgeState.toAmount, 6) : '';
    }
}

/**
 * Render the bridge page
 */
function renderBridgePage(authenticated) {
    const container = document.getElementById('bridge-content');
    if (!container) return;

    container.innerHTML = `
        <!-- Bridge Overview -->
        <div class="portal-section">
            <div class="bridge-overview">
                <div class="bridge-status-card">
                    <div class="bridge-status-header">
                        <h3 class="bridge-status-title">Bridge Status</h3>
                        <span class="bridge-status-badge active">Live</span>
                    </div>
                    <div class="bridge-status-info">
                        <div class="bridge-status-item">
                            <span class="bridge-status-label">Active Chains</span>
                            <span class="bridge-status-value">10</span>
                        </div>
                        <div class="bridge-status-item">
                            <span class="bridge-status-label">Network</span>
                            <span class="bridge-status-value">Testnet</span>
                        </div>
                        <div class="bridge-status-chains">
                            ${CRYPTO_OPTIONS.map(option => `
                                <span class="bridge-chain-tag">
                                    <img src="${getTokenLogo(option.token)}" alt="${option.token}" class="bridge-chain-tag-icon" onerror="this.onerror=null; this.style.display='none'; const fallback = this.nextElementSibling; if(fallback) fallback.style.display='inline';">
                                    <span class="bridge-chain-tag-icon-fallback" style="display: none;">${getTokenIcon(option.token)}</span>
                                    <span class="bridge-chain-tag-text">${option.token}</span>
                                </span>
                            `).join('')}
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Swap Form -->
        <div class="portal-section">
            <div class="portal-section-header">
                <div>
                    <h2 class="portal-section-title">Cross-Chain Swap</h2>
                    <p class="portal-section-subtitle">Swap tokens seamlessly across any blockchain</p>
                </div>
            </div>

            <div class="portal-card">
                <div class="bridge-swap-form">
                    <!-- From Token -->
                    <div class="bridge-swap-input-group">
                        <label class="bridge-swap-label">From</label>
                        <div class="bridge-swap-input-container">
                            <input 
                                type="number" 
                                id="bridge-from-amount"
                                class="bridge-swap-input"
                                placeholder="0.00"
                                step="0.00001"
                                oninput="handleFromAmountChange(this.value)"
                            />
                            <button 
                                class="bridge-token-selector"
                                onclick="openTokenModal('from')"
                            >
                                <img src="${getTokenLogo(bridgeState.selectedFrom.token)}" alt="${bridgeState.selectedFrom.token}" class="bridge-token-icon" onerror="this.onerror=null; this.style.display='none'; const fallback = this.nextElementSibling; if(fallback) fallback.style.display='inline';">
                                <span class="bridge-token-icon-fallback" style="display: none;">${getTokenIcon(bridgeState.selectedFrom.token)}</span>
                                <span class="bridge-token-symbol">${bridgeState.selectedFrom.token}</span>
                                <span class="bridge-token-chevron">▼</span>
                            </button>
                        </div>
                        <div class="bridge-swap-network">
                            <span>${bridgeState.selectedFrom.network}</span>
                        </div>
                        ${authenticated ? renderWalletSelector('from') : ''}
                    </div>

                    <!-- Swap Button -->
                    <div class="bridge-swap-button-container">
                        <button class="bridge-swap-button" onclick="swapTokens()" title="Swap tokens">
                            <span>⇅</span>
                        </button>
                    </div>

                    <!-- To Token -->
                    <div class="bridge-swap-input-group">
                        <label class="bridge-swap-label">To</label>
                        <div class="bridge-swap-input-container">
                            <input 
                                type="number" 
                                id="bridge-to-amount"
                                class="bridge-swap-input"
                                placeholder="0.00"
                                step="0.00001"
                                readonly
                            />
                            <button 
                                class="bridge-token-selector"
                                onclick="openTokenModal('to')"
                            >
                                <img src="${getTokenLogo(bridgeState.selectedTo.token)}" alt="${bridgeState.selectedTo.token}" class="bridge-token-icon" onerror="this.onerror=null; this.style.display='none'; const fallback = this.nextElementSibling; if(fallback) fallback.style.display='inline';">
                                <span class="bridge-token-icon-fallback" style="display: none;">${getTokenIcon(bridgeState.selectedTo.token)}</span>
                                <span class="bridge-token-symbol">${bridgeState.selectedTo.token}</span>
                                <span class="bridge-token-chevron">▼</span>
                            </button>
                        </div>
                        <div class="bridge-swap-network">
                            <span>${bridgeState.selectedTo.network}</span>
                        </div>
                        ${authenticated ? renderWalletSelector('to') : ''}
                    </div>

                    <!-- Exchange Rate -->
                    <div class="bridge-exchange-rate-container">
                        <span id="bridge-exchange-rate">Loading rate...</span>
                    </div>

                    <!-- Submit Button -->
                    <button 
                        class="btn-primary bridge-submit-btn" 
                        onclick="submitSwap()"
                        id="bridge-submit-btn"
                    >
                        <span>Initiate Swap</span>
                    </button>
                </div>
            </div>
        </div>

        <!-- Transaction History -->
        <div class="portal-section">
            <div class="portal-section-header">
                <div>
                    <h2 class="portal-section-title">Transaction History</h2>
                    <p class="portal-section-subtitle">View your cross-chain bridge transactions</p>
                </div>
                ${authenticated ? `
                    <button class="btn-secondary" onclick="refreshTransactions()">
                        <span>Refresh</span>
                    </button>
                ` : ''}
            </div>

            <div class="portal-card">
                ${authenticated ? renderTransactionsList() : renderLoginPrompt()}
            </div>
        </div>
    `;

    // Update rate display after render
    updateRateDisplay();
    updateAmounts();
}

/**
 * Render transactions list
 */
function renderTransactionsList() {
    if (bridgeState.transactions.length === 0) {
        return `
            <div class="empty-state">
                <div class="empty-state-icon">🌉</div>
                <p class="empty-state-text">No bridge transactions yet</p>
                <p class="empty-state-subtext">Your cross-chain swaps will appear here</p>
            </div>
        `;
    }

    return `
        <div class="bridge-transactions-list">
            ${bridgeState.transactions.map(tx => `
                <div class="bridge-transaction-item">
                    <div class="bridge-transaction-header">
                        <div class="bridge-transaction-tokens">
                            <span class="bridge-transaction-from">${tx.fromAmount} ${tx.fromToken}</span>
                            <span class="bridge-transaction-arrow">→</span>
                            <span class="bridge-transaction-to">${tx.toAmount} ${tx.toToken}</span>
                        </div>
                        <span class="bridge-transaction-status ${tx.status}">${tx.status}</span>
                    </div>
                    <div class="bridge-transaction-details">
                        <span class="bridge-transaction-date">${formatDate(tx.timestamp)}</span>
                        ${tx.txHash ? `
                            <a href="${getExplorerUrl(tx.network, tx.txHash)}" target="_blank" class="bridge-transaction-link">
                                View on Explorer
                            </a>
                        ` : ''}
                    </div>
                </div>
            `).join('')}
        </div>
    `;
}

/**
 * Render login prompt
 */
function renderLoginPrompt() {
    return `
        <div class="empty-state">
            <div class="empty-state-icon">🔒</div>
            <p class="empty-state-text">Please log in to view your bridge transactions</p>
            <button class="btn-primary" onclick="openLoginModal()" style="margin-top: 1rem;">
                Log In
            </button>
        </div>
    `;
}

/**
 * Render wallet selector for from/to
 */
function renderWalletSelector(direction) {
    const network = direction === 'from' ? bridgeState.selectedFrom.network : bridgeState.selectedTo.network;
    const selectedWallet = direction === 'from' ? bridgeState.selectedFromWallet : bridgeState.selectedToWallet;
    
    // Find wallets for this network
    const networkWallets = bridgeState.wallets.filter(w => {
        const providerType = (w.providerType || w.chain || '').toLowerCase();
        const networkLower = network.toLowerCase();
        return providerType.includes(networkLower) || 
               providerType.includes('solana') && networkLower === 'solana' ||
               providerType.includes('ethereum') && networkLower === 'ethereum' ||
               providerType.includes('polygon') && networkLower === 'polygon';
    });
    
    if (networkWallets.length === 0) {
        return `
            <div class="bridge-wallet-selector">
                <div class="bridge-wallet-status">
                    <span class="bridge-wallet-status-label">Wallet:</span>
                    <span class="bridge-wallet-status-value">No wallet found</span>
                </div>
                <button class="btn-secondary bridge-wallet-connect-btn" onclick="openWalletModal('${direction}')">
                    <span>Create Wallet</span>
                </button>
            </div>
        `;
    }
    
    const balance = selectedWallet && bridgeState.walletBalances[selectedWallet.id] 
        ? formatNumber(bridgeState.walletBalances[selectedWallet.id].balance?.amount || 0, 4)
        : '...';
    
    return `
        <div class="bridge-wallet-selector">
            <div class="bridge-wallet-status">
                <span class="bridge-wallet-status-label">Wallet:</span>
                <span class="bridge-wallet-status-value">
                    ${selectedWallet ? (selectedWallet.name || shortAddress(selectedWallet.address || selectedWallet.walletAddress)) : 'Select wallet'}
                </span>
                ${selectedWallet ? `
                    <span class="bridge-wallet-balance">Balance: ${balance} ${direction === 'from' ? bridgeState.selectedFrom.token : bridgeState.selectedTo.token}</span>
                ` : ''}
            </div>
            <button class="btn-secondary bridge-wallet-select-btn" onclick="openWalletModal('${direction}')">
                <span>${selectedWallet ? 'Change' : 'Select'} Wallet</span>
            </button>
        </div>
    `;
}

/**
 * Shorten address for display
 */
function shortAddress(address) {
    if (!address) return 'N/A';
    if (address.length <= 10) return address;
    return `${address.substring(0, 6)}...${address.substring(address.length - 4)}`;
}

/**
 * Open wallet selection modal
 */
function openWalletModal(direction) {
    const network = direction === 'from' ? bridgeState.selectedFrom.network : bridgeState.selectedTo.network;
    const networkWallets = bridgeState.wallets.filter(w => {
        const providerType = (w.providerType || w.chain || '').toLowerCase();
        const networkLower = network.toLowerCase();
        return providerType.includes(networkLower) || 
               providerType.includes('solana') && networkLower === 'solana' ||
               providerType.includes('ethereum') && networkLower === 'ethereum' ||
               providerType.includes('polygon') && networkLower === 'polygon';
    });
    
    const selectedWallet = direction === 'from' ? bridgeState.selectedFromWallet : bridgeState.selectedToWallet;
    const selectedWalletId = selectedWallet?.id || selectedWallet?.walletId;
    
    // Remove existing modal if any
    const existingModal = document.getElementById('bridge-wallet-modal');
    if (existingModal) {
        existingModal.remove();
    }
    
    const modal = document.createElement('div');
    modal.id = 'bridge-wallet-modal';
    modal.className = 'bridge-token-modal';
    modal.style.display = 'flex';
    
    modal.innerHTML = `
        <div class="bridge-token-modal-overlay" onclick="closeWalletModal()"></div>
        <div class="bridge-token-modal-content">
            <button class="bridge-token-modal-close" onclick="closeWalletModal()">×</button>
            
            <div class="bridge-token-modal-header">
                <h3 class="bridge-token-modal-title">Select ${network} Wallet</h3>
                <p class="bridge-token-modal-subtitle">Choose a wallet to ${direction === 'from' ? 'send from' : 'receive to'}</p>
            </div>
            
            <div class="bridge-token-modal-body">
                ${networkWallets.length === 0 ? `
                    <div class="empty-state">
                        <p class="empty-state-text">No ${network} wallets found</p>
                        <p class="empty-state-subtext">Create a wallet in the Wallets section first</p>
                        <button class="btn-primary" onclick="closeWalletModal(); switchTab('wallets');" style="margin-top: 1rem;">
                            Go to Wallets
                        </button>
                    </div>
                ` : `
                    <div class="bridge-token-list">
                        ${networkWallets.map(wallet => {
                            const isSelected = (wallet.id || wallet.walletId) === selectedWalletId;
                            const balance = bridgeState.walletBalances[wallet.id || wallet.walletId];
                            const balanceAmount = balance ? formatNumber(balance.balance?.amount || 0, 4) : '...';
                            const address = wallet.address || wallet.walletAddress || 'N/A';
                            
                            return `
                                <button 
                                    class="bridge-token-option ${isSelected ? 'selected' : ''}"
                                    onclick="selectWallet('${direction}', '${wallet.id || wallet.walletId}')"
                                >
                                    <div class="bridge-token-option-info" style="flex: 1;">
                                        <div class="bridge-token-option-symbol">${wallet.name || 'Unnamed Wallet'}</div>
                                        <div class="bridge-token-option-name">${shortAddress(address)}</div>
                                        ${balance ? `
                                            <div class="bridge-wallet-option-balance">${balanceAmount} ${direction === 'from' ? bridgeState.selectedFrom.token : bridgeState.selectedTo.token}</div>
                                        ` : ''}
                                    </div>
                                    ${isSelected ? '<span class="bridge-token-option-check">✓</span>' : ''}
                                </button>
                            `;
                        }).join('')}
                    </div>
                `}
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
    document.body.style.overflow = 'hidden';
}

/**
 * Close wallet modal
 */
function closeWalletModal() {
    const modal = document.getElementById('bridge-wallet-modal');
    if (modal) {
        modal.remove();
    }
    document.body.style.overflow = '';
}

/**
 * Select a wallet
 */
async function selectWallet(direction, walletId) {
    const wallet = bridgeState.wallets.find(w => (w.id || w.walletId) === walletId);
    if (!wallet) return;
    
    if (direction === 'from') {
        bridgeState.selectedFromWallet = wallet;
        await loadWalletBalance(walletId, 'from');
    } else {
        bridgeState.selectedToWallet = wallet;
        await loadWalletBalance(walletId, 'to');
    }
    
    closeWalletModal();
    renderBridgePage(isAuthenticated());
}

/**
 * Handle from amount change
 */
function handleFromAmountChange(value) {
    const amount = parseFloat(value) || null;
    bridgeState.fromAmount = amount;
    
    if (amount && bridgeState.exchangeRate) {
        bridgeState.toAmount = amount * bridgeState.exchangeRate;
        updateAmounts();
    } else {
        bridgeState.toAmount = null;
        updateAmounts();
    }
}

/**
 * Swap tokens (reverse from/to)
 */
function swapTokens() {
    const temp = bridgeState.selectedFrom;
    bridgeState.selectedFrom = bridgeState.selectedTo;
    bridgeState.selectedTo = temp;
    
    // Swap amounts
    const tempAmount = bridgeState.fromAmount;
    bridgeState.fromAmount = bridgeState.toAmount;
    bridgeState.toAmount = tempAmount;
    
    // Recalculate rate
    updateExchangeRate();
    
    // Re-render
    renderBridgePage(isAuthenticated());
}

// Token modal state
let tokenModalState = {
    direction: null, // 'from' or 'to'
    isOpen: false
};

/**
 * Open token selection modal
 */
function openTokenModal(direction) {
    tokenModalState.direction = direction;
    tokenModalState.isOpen = true;
    renderTokenModal();
}

/**
 * Close token selection modal
 */
function closeTokenModal() {
    tokenModalState.isOpen = false;
    tokenModalState.direction = null;
    const modal = document.getElementById('bridge-token-modal');
    if (modal) {
        modal.style.display = 'none';
    }
    document.body.style.overflow = '';
}

/**
 * Select a token
 */
function selectToken(token) {
    const option = CRYPTO_OPTIONS.find(opt => opt.token === token);
    if (!option) return;
    
    if (tokenModalState.direction === 'from') {
        bridgeState.selectedFrom = { network: option.network, token: option.token };
    } else {
        bridgeState.selectedTo = { network: option.network, token: option.token };
    }
    
    closeTokenModal();
    updateExchangeRate();
    renderBridgePage(isAuthenticated());
}

/**
 * Render token selection modal
 */
function renderTokenModal() {
    // Remove existing modal if any
    const existingModal = document.getElementById('bridge-token-modal');
    if (existingModal) {
        existingModal.remove();
    }
    
    const currentToken = tokenModalState.direction === 'from' 
        ? bridgeState.selectedFrom.token 
        : bridgeState.selectedTo.token;
    
    const modal = document.createElement('div');
    modal.id = 'bridge-token-modal';
    modal.className = 'bridge-token-modal';
    modal.style.display = 'flex';
    
    modal.innerHTML = `
        <div class="bridge-token-modal-overlay" onclick="closeTokenModal()"></div>
        <div class="bridge-token-modal-content">
            <button class="bridge-token-modal-close" onclick="closeTokenModal()">×</button>
            
            <div class="bridge-token-modal-header">
                <h3 class="bridge-token-modal-title">Select Token</h3>
                <p class="bridge-token-modal-subtitle">Choose a token to ${tokenModalState.direction === 'from' ? 'send' : 'receive'}</p>
            </div>
            
            <div class="bridge-token-modal-body">
                <div class="bridge-token-list">
                    ${CRYPTO_OPTIONS.map(option => {
                        const isSelected = option.token === currentToken;
                        return `
                            <button 
                                class="bridge-token-option ${isSelected ? 'selected' : ''}"
                                onclick="selectToken('${option.token}')"
                            >
                                <img 
                                    src="${getTokenLogo(option.token)}" 
                                    alt="${option.token}" 
                                    class="bridge-token-option-icon"
                                    onerror="this.onerror=null; this.style.display='none'; const fallback = this.nextElementSibling; if(fallback) fallback.style.display='inline';"
                                >
                                <span class="bridge-token-option-icon-fallback" style="display: none;">${getTokenIcon(option.token)}</span>
                                <div class="bridge-token-option-info">
                                    <div class="bridge-token-option-symbol">${option.token}</div>
                                    <div class="bridge-token-option-name">${option.name}</div>
                                </div>
                                ${isSelected ? '<span class="bridge-token-option-check">✓</span>' : ''}
                            </button>
                        `;
                    }).join('')}
                </div>
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
    document.body.style.overflow = 'hidden';
}

// Make functions globally available
window.closeTokenModal = closeTokenModal;
window.selectToken = selectToken;

/**
 * Submit swap
 */
async function submitSwap() {
    const { selectedFrom, selectedTo, fromAmount, toAmount } = bridgeState;
    
    if (!fromAmount || fromAmount <= 0) {
        alert('Please enter a valid amount');
        return;
    }
    
    if (selectedFrom.token === selectedTo.token) {
        alert('Please select different tokens');
        return;
    }
    
    if (!isAuthenticated()) {
        alert('Please log in to initiate a swap');
        openLoginModal();
        return;
    }
    
    const submitBtn = document.getElementById('bridge-submit-btn');
    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span>Processing...</span>';
    }
    
    bridgeState.isSubmitting = true;
    
    try {
        const avatarId = getAvatarId();
        
        // Check wallet selection
        if (!bridgeState.selectedFromWallet) {
            alert(`Please select a ${selectedFrom.network} wallet to send from`);
            openWalletModal('from');
            return;
        }
        
        if (!bridgeState.selectedToWallet) {
            alert(`Please select a ${selectedTo.network} wallet to receive to`);
            openWalletModal('to');
            return;
        }
        
        // Check balance
        const fromBalance = bridgeState.walletBalances[bridgeState.selectedFromWallet.id || bridgeState.selectedFromWallet.walletId];
        if (fromBalance && fromBalance.balance) {
            const availableBalance = fromBalance.balance.amount || 0;
            if (fromAmount > availableBalance) {
                alert(`Insufficient balance. Available: ${formatNumber(availableBalance, 4)} ${selectedFrom.token}`);
                return;
            }
        }
        
        const swapData = {
            avatarId: avatarId,
            fromToken: selectedFrom.token,
            fromNetwork: selectedFrom.network,
            toToken: selectedTo.token,
            toNetwork: selectedTo.network,
            fromAmount: fromAmount,
            toAmount: toAmount,
            exchangeRate: bridgeState.exchangeRate,
            fromWalletId: bridgeState.selectedFromWallet.id || bridgeState.selectedFromWallet.walletId,
            toWalletId: bridgeState.selectedToWallet.id || bridgeState.selectedToWallet.walletId
        };
        
        // Create swap order
        let result;
        if (typeof oasisAPI !== 'undefined' && oasisAPI.request) {
            result = await oasisAPI.request('/api/bridge/create-order', {
                method: 'POST',
                body: JSON.stringify(swapData)
            });
        } else {
            // Fallback: simulate success
            result = {
                isError: false,
                result: {
                    orderId: `order_${Date.now()}`,
                    status: 'pending',
                    ...swapData
                }
            };
        }
        
        if (result.isError) {
            throw new Error(result.message || 'Swap failed');
        }
        
        // Add to transactions
        bridgeState.transactions.unshift({
            ...swapData,
            status: 'pending',
            timestamp: new Date().toISOString(),
            txHash: result.result?.txHash
        });
        
        // Reset form
        bridgeState.fromAmount = null;
        bridgeState.toAmount = null;
        updateAmounts();
        
        // Show success
        alert('Swap initiated successfully! Check transaction history for status.');
        
        // Re-render
        renderBridgePage(true);
        
    } catch (error) {
        console.error('Swap error:', error);
        alert('Error initiating swap: ' + (error.message || 'Unknown error'));
    } finally {
        bridgeState.isSubmitting = false;
        if (submitBtn) {
            submitBtn.disabled = false;
            submitBtn.innerHTML = '<span>Initiate Swap</span>';
        }
    }
}

/**
 * Refresh transactions
 */
async function refreshTransactions() {
    await loadBridge();
}

/**
 * Get token logo path
 * Uses oasisweb4 site logos
 */
function getTokenLogo(token) {
    const option = CRYPTO_OPTIONS.find(opt => opt.token === token);
    if (option?.logo) {
        return option.logo;
    }
    
    // Fallback logo mapping
    const fallbackLogos = {
        'SOL': 'logos/solana.svg',
        'ETH': 'logos/ethereum.svg',
        'MATIC': 'logos/polygon.svg',
        'BASE': 'logos/base.png',
        'ARB': 'logos/arbitrum.png',
        'OP': 'logos/optimism.svg',
        'BNB': 'logos/bnb.svg',
        'AVAX': 'logos/avalanche.svg',
        'FTM': 'logos/fantom.svg',
        'XRD': 'logos/radix.svg'
    };
    
    return fallbackLogos[token] || 'logos/ethereum.svg';
}

/**
 * Get token icon (fallback emoji)
 */
function getTokenIcon(token) {
    const icons = {
        'SOL': '🚀',
        'ETH': '💎',
        'MATIC': '🔷',
        'BASE': '📦',
        'ARB': '⚡',
        'OP': '🔆',
        'BNB': '🟡',
        'AVAX': '❄️',
        'FTM': '👻',
        'XRD': '🔷'
    };
    return icons[token] || '💎';
}

/**
 * Get blockchain explorer URL
 */
function getExplorerUrl(network, txHash) {
    const explorers = {
        'Solana': `https://solscan.io/tx/${txHash}`,
        'Ethereum': `https://etherscan.io/tx/${txHash}`,
        'Polygon': `https://polygonscan.com/tx/${txHash}`,
        'Base': `https://basescan.org/tx/${txHash}`,
        'Arbitrum': `https://arbiscan.io/tx/${txHash}`,
        'Optimism': `https://optimistic.etherscan.io/tx/${txHash}`,
        'BNB Chain': `https://bscscan.com/tx/${txHash}`,
        'Avalanche': `https://snowtrace.io/tx/${txHash}`,
        'Fantom': `https://ftmscan.com/tx/${txHash}`,
        'Radix': `https://dashboard.radixdlt.com/transaction/${txHash}`
    };
    return explorers[network] || '#';
}

/**
 * Format number
 */
function formatNumber(num, decimals = 4) {
    if (!num) return '0';
    return parseFloat(num).toFixed(decimals);
}

/**
 * Format date
 */
function formatDate(dateString) {
    if (!dateString) return 'Unknown';
    try {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', { 
            year: 'numeric', 
            month: 'short', 
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    } catch (error) {
        return dateString;
    }
}

// Make functions globally available
window.handleFromAmountChange = handleFromAmountChange;
window.swapTokens = swapTokens;
window.openTokenModal = openTokenModal;
window.submitSwap = submitSwap;
window.refreshTransactions = refreshTransactions;
window.openWalletModal = openWalletModal;
window.closeWalletModal = closeWalletModal;
window.selectWallet = selectWallet;
