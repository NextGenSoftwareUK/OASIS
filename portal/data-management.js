/**
 * Data Management Dashboard
 * Frontend interface for managing data storage across OASIS providers
 */

// State management
let dataManagementState = {
    providers: [],
    storageProviders: [],
    holons: [],
    holonsByProvider: {},
    replicationProviders: [],
    failoverProviders: [],
    currentProvider: null,
    loading: false,
    error: null
};

/**
 * Load and render data management dashboard
 */
async function loadDataManagement() {
    const container = document.getElementById('data-management-content');
    if (!container) {
        console.error('Data management container not found');
        return;
    }

    // Show loading state
    setLoadingState(true);
    container.innerHTML = renderLoadingState();

    const authData = localStorage.getItem('oasis_auth');
    
    // If no auth, load demo data instead of showing login prompt
    if (!authData) {
        console.log('No auth found - loading demo data');
        loadDemoData();
        container.innerHTML = renderDataManagement();
        attachEventListeners();
        setLoadingState(false);
        return;
    }

    try {
        const auth = JSON.parse(authData);
        const avatarId = auth.avatar?.avatarId || auth.avatar?.id;
        
        if (!avatarId) {
            // Fallback to demo data if avatar ID missing
            console.log('Avatar ID not found - loading demo data');
            loadDemoData();
            container.innerHTML = renderDataManagement();
            attachEventListeners();
            setLoadingState(false);
            return;
        }

        // Load all data in parallel
        const [
            providersResult,
            storageProvidersResult,
            holonsResult,
            replicationResult,
            failoverResult,
            currentProviderResult
        ] = await Promise.all([
            oasisAPI.getAllProviders(),
            oasisAPI.getStorageProviders(),
            oasisAPI.getHolonsByAvatar(avatarId),
            oasisAPI.getReplicationProviders(),
            oasisAPI.getFailoverProviders(),
            oasisAPI.getCurrentStorageProvider()
        ]);

        // Update state
        dataManagementState.providers = providersResult.isError ? [] : (providersResult.result || []);
        dataManagementState.storageProviders = storageProvidersResult.isError ? [] : (storageProvidersResult.result || []);
        dataManagementState.holons = holonsResult.isError ? [] : (holonsResult.result || []);
        dataManagementState.replicationProviders = replicationResult.isError ? [] : (replicationResult.result || []);
        dataManagementState.failoverProviders = failoverResult.isError ? [] : (failoverResult.result || []);
        dataManagementState.currentProvider = currentProviderResult.isError ? null : (currentProviderResult.result || null);

        // Group holons by provider
        dataManagementState.holonsByProvider = groupHolonsByProvider(dataManagementState.holons);

        // Render dashboard
        container.innerHTML = renderDataManagement();

        // Attach event listeners
        attachEventListeners();

    } catch (error) {
        console.error('Error loading data management:', error);
        // Fallback to demo data on error
        console.log('Error occurred - loading demo data instead');
        loadDemoData();
        container.innerHTML = renderDataManagement();
        attachEventListeners();
    } finally {
        setLoadingState(false);
    }
}

/**
 * Load demo/sample data for UI preview
 */
function loadDemoData() {
    // Demo providers
    dataManagementState.storageProviders = [
        {
            providerName: 'MongoDB',
            providerType: 'MongoDBOASIS',
            providerCategory: 'Database',
            providerDescription: 'Primary database storage provider',
            isProviderActivated: true,
            isProviderRegistered: true
        },
        {
            providerName: 'Ethereum',
            providerType: 'EthereumOASIS',
            providerCategory: 'Blockchain',
            providerDescription: 'Ethereum blockchain storage',
            isProviderActivated: true,
            isProviderRegistered: true
        },
        {
            providerName: 'Solana',
            providerType: 'SolanaOASIS',
            providerCategory: 'Blockchain',
            providerDescription: 'Solana blockchain storage',
            isProviderActivated: true,
            isProviderRegistered: true
        },
        {
            providerName: 'IPFS',
            providerType: 'IPFSOASIS',
            providerCategory: 'Storage',
            providerDescription: 'InterPlanetary File System',
            isProviderActivated: false,
            isProviderRegistered: true
        },
        {
            providerName: 'Neo4j',
            providerType: 'Neo4jOASIS',
            providerCategory: 'Database',
            providerDescription: 'Graph database provider',
            isProviderActivated: true,
            isProviderRegistered: true
        },
        {
            providerName: 'Local File',
            providerType: 'LocalFileOASIS',
            providerCategory: 'Storage',
            providerDescription: 'Local file system storage',
            isProviderActivated: true,
            isProviderRegistered: true
        }
    ];

    // Demo holons
    dataManagementState.holons = [
        {
            id: 'holon-001-abc123def456',
            holonId: 'holon-001-abc123def456',
            holonType: 'Profile',
            name: 'User Profile Data',
            description: 'Main user profile information',
            providerKey: 'MongoDBOASIS',
            providerType: 'MongoDBOASIS',
            createdDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
            modifiedDate: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString()
        },
        {
            id: 'holon-002-xyz789ghi012',
            holonId: 'holon-002-xyz789ghi012',
            holonType: 'NFT',
            name: 'NFT Collection Metadata',
            description: 'Collection of NFT metadata',
            providerKey: 'EthereumOASIS',
            providerType: 'EthereumOASIS',
            createdDate: new Date(Date.now() - 15 * 24 * 60 * 60 * 1000).toISOString(),
            modifiedDate: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000).toISOString()
        },
        {
            id: 'holon-003-jkl345mno678',
            holonId: 'holon-003-jkl345mno678',
            holonType: 'Transaction',
            name: 'Transaction History',
            description: 'Blockchain transaction records',
            providerKey: 'SolanaOASIS',
            providerType: 'SolanaOASIS',
            createdDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
            modifiedDate: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString()
        },
        {
            id: 'holon-004-pqr901stu234',
            holonId: 'holon-004-pqr901stu234',
            holonType: 'Settings',
            name: 'User Preferences',
            description: 'Application settings and preferences',
            providerKey: 'MongoDBOASIS',
            providerType: 'MongoDBOASIS',
            createdDate: new Date(Date.now() - 20 * 24 * 60 * 60 * 1000).toISOString(),
            modifiedDate: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString()
        },
        {
            id: 'holon-005-vwx567yza890',
            holonId: 'holon-005-vwx567yza890',
            holonType: 'Document',
            name: 'Project Documentation',
            description: 'Project documentation and files',
            providerKey: 'Neo4jOASIS',
            providerType: 'Neo4jOASIS',
            createdDate: new Date(Date.now() - 10 * 24 * 60 * 60 * 1000).toISOString(),
            modifiedDate: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000).toISOString()
        }
    ];

    // Group holons by provider
    dataManagementState.holonsByProvider = groupHolonsByProvider(dataManagementState.holons);

    // Demo replication and failover
    dataManagementState.replicationProviders = ['MongoDBOASIS', 'EthereumOASIS'];
    dataManagementState.failoverProviders = ['MongoDBOASIS'];
    dataManagementState.currentProvider = {
        providerType: 'MongoDBOASIS',
        providerName: 'MongoDB',
        isProviderActivated: true
    };
}

/**
 * Group holons by provider
 */
function groupHolonsByProvider(holons) {
    const grouped = {};
    
    holons.forEach(holon => {
        const providerKey = holon.providerKey || holon.providerType || 'unknown';
        if (!grouped[providerKey]) {
            grouped[providerKey] = [];
        }
        grouped[providerKey].push(holon);
    });
    
    return grouped;
}

/**
 * Calculate statistics
 */
function calculateStatistics() {
    const { holons, storageProviders, holonsByProvider } = dataManagementState;
    
    const activeProviders = storageProviders.filter(p => 
        p.isProviderActivated !== false && p.isProviderRegistered !== false
    ).length;
    
    const totalHolons = holons.length;
    const providersWithData = Object.keys(holonsByProvider).length;
    
    // Calculate total storage (if size data is available)
    let totalStorage = 0;
    holons.forEach(holon => {
        if (holon.size) {
            totalStorage += parseFloat(holon.size) || 0;
        }
    });
    
    return {
        totalHolons,
        activeProviders,
        providersWithData,
        totalStorage
    };
}

/**
 * Render main data management interface
 */
function renderDataManagement() {
    const stats = calculateStatistics();
    
    return `
        ${renderHeader(stats)}
        ${renderProviderGrid()}
        ${renderHolonsList()}
        ${renderSettingsPanel()}
    `;
}

/**
 * Render header section with statistics
 */
function renderHeader(stats) {
    return `
        <div class="data-header">
            <div class="data-header-content">
                <h2 class="data-title">Data Storage Management</h2>
                <p class="data-subtitle">Control where your data is stored across providers</p>
                <div class="data-stats-grid">
                    <div class="data-stat-card">
                        <div class="data-stat-label">Total Holons</div>
                        <div class="data-stat-value">${stats.totalHolons}</div>
                        <div class="data-stat-detail">Data modules stored</div>
                    </div>
                    <div class="data-stat-card">
                        <div class="data-stat-label">Active Providers</div>
                        <div class="data-stat-value">${stats.activeProviders}</div>
                        <div class="data-stat-detail">Storage providers enabled</div>
                    </div>
                    <div class="data-stat-card">
                        <div class="data-stat-label">Providers with Data</div>
                        <div class="data-stat-value">${stats.providersWithData}</div>
                        <div class="data-stat-detail">Currently storing data</div>
                    </div>
                    <div class="data-stat-card">
                        <div class="data-stat-label">Replication</div>
                        <div class="data-stat-value">${dataManagementState.replicationProviders.length}</div>
                        <div class="data-stat-detail">Auto-replicating providers</div>
                    </div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Render provider grid
 */
function renderProviderGrid() {
    const { storageProviders, holonsByProvider, currentProvider } = dataManagementState;
    
    if (storageProviders.length === 0) {
        return `
            <div class="data-section">
                <div class="empty-state">
                    <p class="empty-state-text">No storage providers found</p>
                </div>
            </div>
        `;
    }

    // Group providers by category
    const providersByCategory = groupProvidersByCategory(storageProviders);
    
    let html = '<div class="data-section"><h3 class="data-section-title">Storage Providers</h3>';
    
    Object.keys(providersByCategory).forEach(category => {
        html += `<div class="provider-category"><h4 class="provider-category-title">${category}</h4>`;
        html += '<div class="provider-grid">';
        
        providersByCategory[category].forEach(provider => {
            const holonCount = holonsByProvider[provider.providerType]?.length || 0;
            const isActive = provider.isProviderActivated !== false;
            const isCurrent = currentProvider?.providerType === provider.providerType;
            
            html += renderProviderCard(provider, holonCount, isActive, isCurrent);
        });
        
        html += '</div></div>';
    });
    
    html += '</div>';
    return html;
}

/**
 * Group providers by category
 */
function groupProvidersByCategory(providers) {
    const grouped = {};
    
    providers.forEach(provider => {
        const category = provider.providerCategory || 'Other';
        if (!grouped[category]) {
            grouped[category] = [];
        }
        grouped[category].push(provider);
    });
    
    return grouped;
}

/**
 * Render provider card
 */
function renderProviderCard(provider, holonCount, isActive, isCurrent) {
    const providerType = provider.providerType || provider.providerName;
    const statusClass = isActive ? 'active' : 'inactive';
    const statusText = isActive ? 'Active' : 'Inactive';
    const currentBadge = isCurrent ? '<span class="provider-badge current">Current</span>' : '';
    
    return `
        <div class="provider-card ${statusClass}" data-provider-type="${providerType}">
            <div class="provider-card-header">
                <div class="provider-icon">
                    ${getProviderIcon(providerType)}
                </div>
                <div class="provider-info">
                    <div class="provider-name">${provider.providerName || providerType}</div>
                    <div class="provider-type">${providerType}</div>
                </div>
                ${currentBadge}
            </div>
            <div class="provider-card-body">
                <div class="provider-status">
                    <span class="provider-status-indicator ${statusClass}"></span>
                    ${statusText}
                </div>
                <div class="provider-stats">
                    <div class="provider-stat">
                        <span class="provider-stat-label">Holons:</span>
                        <span class="provider-stat-value">${holonCount}</span>
                    </div>
                </div>
                ${provider.providerDescription ? `<div class="provider-description">${provider.providerDescription}</div>` : ''}
            </div>
            <div class="provider-card-footer">
                <label class="provider-toggle">
                    <input type="checkbox" ${isActive ? 'checked' : ''} 
                           onchange="toggleProvider('${providerType}', this.checked)">
                    <span class="provider-toggle-slider"></span>
                </label>
                <button class="provider-btn" onclick="viewProviderDetails('${providerType}')">Details</button>
            </div>
        </div>
    `;
}

/**
 * Get provider logo path
 * Maps provider types to logo file names in the logos directory
 */
function getProviderLogoPath(providerType) {
    const logoMap = {
        // Blockchain Providers
        'EthereumOASIS': 'ethereum.svg',
        'SolanaOASIS': 'solana.svg',
        'PolygonOASIS': 'polygon.svg',
        'ArbitrumOASIS': 'arbitrum.svg', // Try SVG first, fallback to PNG in getProviderIcon
        'BaseOASIS': 'base.svg',
        'AvalancheOASIS': 'avalanche.svg',
        'BNBOASIS': 'bnb.svg',
        'OptimismOASIS': 'optimism.svg', // Try SVG first, fallback to PNG
        'FantomOASIS': 'fantom.svg', // Try SVG first, fallback to PNG
        'RadixOASIS': 'radix.svg',
        
        // Storage Providers
        'IPFSOASIS': 'ipfs.png',
        'ArweaveOASIS': 'arweave.png',
        'PinataOASIS': 'ipfs.png', // Pinata uses IPFS, so use IPFS logo
        
        // Database Providers
        'MongoDBOASIS': 'mongodb.png',
        'Neo4jOASIS': 'neo4j.webp', // Logo located in portal folder
        'SQLLiteDBOASIS': 'mongodb.png', // placeholder
        'SQLServerDBOASIS': 'azure.png', // SQL Server is Microsoft/Azure
        'AzureCosmosDBOASIS': 'azure.png',
        
        // Network Providers
        'HoloOASIS': 'holochain.png',
        'ThreeFoldOASIS': 'threefold.png',
        
        // Cloud Providers
        'AWSOASIS': 'aws.png',
        'AzureOASIS': 'azure.png',
        'GoogleCloudOASIS': 'google-cloud.png',
        
        // Privacy/Zero-Knowledge
        'AztecOASIS': 'aztec.png',
        'MidenOASIS': 'miden.png',
        'ZcashOASIS': 'zcash.png',
        'SolidOASIS': 'solid.png',
        
        // Local/File
        'LocalFileOASIS': 'mongodb.png' // placeholder
    };
    
    // Try to get logo from map
    let logoFile = logoMap[providerType];
    
    // Handle providers that might have SVG or PNG
    if (!logoFile) {
        // Try common fallbacks
        if (providerType.includes('Arbitrum')) {
            logoFile = 'arbitrum.png'; // Fallback to PNG if SVG not found
        } else if (providerType.includes('Optimism')) {
            logoFile = 'optimism.png';
        } else if (providerType.includes('Fantom')) {
            logoFile = 'fantom.png';
        } else if (providerType.includes('Base')) {
            logoFile = 'base.png';
        } else if (providerType.includes('Neo4j')) {
            // Try Neo4j logo, fallback to generic database icon
            logoFile = 'neo4j.png';
        }
    }
    
    if (logoFile) {
        // Check if it's a local file in portal directory (like neo4j.webp)
        if (logoFile.endsWith('.webp') && providerType === 'Neo4jOASIS') {
            return `neo4j.webp`; // Relative to portal directory
        }
        // Otherwise use logos directory
        return `../oasisweb4 site/new-v2/logos/${logoFile}`;
    }
    
    // Default fallback - use mongodb.png as generic database icon
    return `../oasisweb4 site/new-v2/logos/mongodb.png`;
}

/**
 * Get provider icon HTML (returns img tag with fallback handling)
 */
function getProviderIcon(providerType) {
    const logoPath = getProviderLogoPath(providerType);
    
    // Handle webp files (like neo4j.webp in portal folder)
    if (logoPath.endsWith('.webp')) {
        return `
            <img src="${logoPath}" alt="${providerType}" class="provider-logo-img" 
                 onerror="this.style.display='none'; this.nextElementSibling.style.display='flex';" />
            <div class="provider-icon-fallback" style="display: none; align-items: center; justify-content: center; width: 100%; height: 100%; font-size: 1.5rem; color: var(--text-secondary);">📊</div>
        `;
    }
    
    // Try SVG first, then PNG if SVG fails
    const basePath = logoPath.replace(/\.(svg|png)$/, '');
    return `
        <img src="${basePath}.svg" alt="${providerType}" class="provider-logo-img" 
             onerror="this.onerror=null; this.src='${basePath}.png'; this.onerror=function(){this.style.display='none'; this.nextElementSibling.style.display='flex';}" />
        <div class="provider-icon-fallback" style="display: none; align-items: center; justify-content: center; width: 100%; height: 100%; font-size: 1.5rem; color: var(--text-secondary);">📊</div>
    `;
}

/**
 * Render holons list
 */
function renderHolonsList() {
    const { holons } = dataManagementState;
    
    if (holons.length === 0) {
        return `
            <div class="data-section">
                <h3 class="data-section-title">Data Holons</h3>
                <div class="empty-state">
                    <p class="empty-state-text">No holons found. Your data will appear here once stored.</p>
                </div>
            </div>
        `;
    }

    let html = `
        <div class="data-section">
            <div class="data-section-header">
                <h3 class="data-section-title">Data Holons</h3>
                <div class="data-section-actions">
                    <input type="text" class="data-search" id="holonSearch" 
                           placeholder="Search holons..." onkeyup="filterHolons()">
                    <button class="data-btn" onclick="exportHolons()">Export</button>
                </div>
            </div>
            <div class="holons-table-container">
                <table class="holons-table">
                    <thead>
                        <tr>
                            <th>Holon ID</th>
                            <th>Type</th>
                            <th>Name</th>
                            <th>Provider</th>
                            <th>Created</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody id="holonsTableBody">
    `;
    
    holons.forEach(holon => {
        html += renderHolonRow(holon);
    });
    
    html += `
                    </tbody>
                </table>
            </div>
        </div>
    `;
    
    return html;
}

/**
 * Render holon table row
 */
function renderHolonRow(holon) {
    const id = holon.id || holon.holonId || '-';
    const shortId = id.length > 12 ? `${id.substring(0, 6)}...${id.substring(id.length - 6)}` : id;
    const type = holon.holonType || '-';
    const name = holon.name || holon.description || '-';
    const provider = holon.providerKey || holon.providerType || '-';
    const created = holon.createdDate ? new Date(holon.createdDate).toLocaleDateString() : '-';
    
    return `
        <tr class="holon-row" data-holon-id="${id}">
            <td>
                <code class="holon-id">${shortId}</code>
                <button class="holon-copy-btn" onclick="copyHolonId('${id}')" title="Copy ID">📋</button>
            </td>
            <td><span class="holon-type-badge">${type}</span></td>
            <td>${name}</td>
            <td><span class="provider-badge">${provider}</span></td>
            <td>${created}</td>
            <td>
                <div class="holon-actions">
                    <button class="holon-action-btn" onclick="viewHolon('${id}', '${provider}')">View</button>
                    <button class="holon-action-btn" onclick="migrateHolon('${id}', '${provider}')">Migrate</button>
                    <button class="holon-action-btn danger" onclick="deleteHolon('${id}', '${provider}')">Delete</button>
                </div>
            </td>
        </tr>
    `;
}

/**
 * Render settings panel
 */
function renderSettingsPanel() {
    const { replicationProviders, failoverProviders, currentProvider } = dataManagementState;
    
    return `
        <div class="data-section">
            <div class="settings-panel">
                <h3 class="data-section-title">Settings</h3>
                <div class="settings-group">
                    <label class="settings-label">
                        <input type="checkbox" id="autoReplication" 
                               ${replicationProviders.length > 0 ? 'checked' : ''}>
                        Enable Auto-Replication
                    </label>
                    <p class="settings-description">Automatically replicate data across selected providers</p>
                </div>
                <div class="settings-group">
                    <label class="settings-label">
                        <input type="checkbox" id="autoFailover" 
                               ${failoverProviders.length > 0 ? 'checked' : ''}>
                        Enable Auto-Failover
                    </label>
                    <p class="settings-description">Automatically switch to backup provider on failure</p>
                </div>
                <div class="settings-group">
                    <label class="settings-label">Default Storage Provider</label>
                    <select class="settings-select" id="defaultProvider">
                        ${renderProviderOptions(currentProvider?.providerType)}
                    </select>
                </div>
                <div class="settings-actions">
                    <button class="data-btn" onclick="saveSettings()">Save Settings</button>
                </div>
            </div>
        </div>
    `;
}

/**
 * Render provider options for select
 */
function renderProviderOptions(currentProviderType) {
    const { storageProviders } = dataManagementState;
    let html = '<option value="">Select provider...</option>';
    
    storageProviders.forEach(provider => {
        const providerType = provider.providerType || provider.providerName;
        const selected = providerType === currentProviderType ? 'selected' : '';
        html += `<option value="${providerType}" ${selected}>${provider.providerName || providerType}</option>`;
    });
    
    return html;
}

/**
 * Render loading state
 */
function renderLoadingState() {
    return `
        <div class="data-loading">
            <div class="data-loading-spinner"></div>
            <p>Loading data management...</p>
        </div>
    `;
}

/**
 * Render error state
 */
function renderErrorState(message) {
    return `
        <div class="data-error">
            <div class="data-error-icon">⚠️</div>
            <h3>Error Loading Data</h3>
            <p>${message}</p>
            <button class="data-btn" onclick="loadDataManagement()">Retry</button>
        </div>
    `;
}

/**
 * Set loading state
 */
function setLoadingState(loading) {
    dataManagementState.loading = loading;
}

/**
 * Show login prompt
 */
function showLoginPrompt() {
    const container = document.getElementById('data-management-content');
    if (container) {
        container.innerHTML = `
            <div class="data-error">
                <div class="data-error-icon">🔒</div>
                <h3>Authentication Required</h3>
                <p>Please log in to manage your data storage</p>
                <button class="data-btn" onclick="window.location.reload()">Go to Login</button>
            </div>
        `;
    }
}

/**
 * Attach event listeners
 */
function attachEventListeners() {
    // Search functionality is handled inline
    // Other event listeners can be added here
}

// ============================================
// Provider Management Functions
// ============================================

/**
 * Toggle provider enable/disable
 */
async function toggleProvider(providerType, enabled) {
    try {
        if (enabled) {
            // Enable provider
            const result = await oasisAPI.activateProvider(providerType);
            if (result.isError) {
                alert(`Failed to activate provider: ${result.message}`);
                return;
            }
            
            // Optionally set as default
            if (confirm('Set as default storage provider?')) {
                await oasisAPI.setCurrentStorageProvider(providerType);
            }
        } else {
            // Check if provider has data
            const holonsInProvider = dataManagementState.holonsByProvider[providerType] || [];
            if (holonsInProvider.length > 0) {
                if (!confirm(`This provider has ${holonsInProvider.length} holons. Disabling will prevent new data but won't delete existing data. Continue?`)) {
                    // Reset toggle
                    const checkbox = document.querySelector(`input[onchange*="${providerType}"]`);
                    if (checkbox) checkbox.checked = true;
                    return;
                }
            }
            
            // Disable provider
            const result = await oasisAPI.deactivateProvider(providerType);
            if (result.isError) {
                alert(`Failed to deactivate provider: ${result.message}`);
                return;
            }
        }
        
        // Refresh data
        await loadDataManagement();
        
    } catch (error) {
        console.error('Error toggling provider:', error);
        alert(`Failed to update provider: ${error.message}`);
    }
}

/**
 * View provider details
 */
function viewProviderDetails(providerType) {
    const provider = dataManagementState.storageProviders.find(p => 
        (p.providerType || p.providerName) === providerType
    );
    
    if (!provider) {
        alert('Provider not found');
        return;
    }
    
    const holons = dataManagementState.holonsByProvider[providerType] || [];
    
    alert(`Provider: ${provider.providerName || providerType}\n\n` +
          `Type: ${provider.providerType}\n` +
          `Category: ${provider.providerCategory || 'N/A'}\n` +
          `Status: ${provider.isProviderActivated ? 'Active' : 'Inactive'}\n` +
          `Holons: ${holons.length}\n\n` +
          `${provider.providerDescription || 'No description available'}`);
}

// ============================================
// Holon Management Functions
// ============================================

/**
 * View holon details
 */
async function viewHolon(holonId, providerType) {
    try {
        const result = await oasisAPI.getHolon(holonId, providerType);
        
        if (result.isError) {
            alert(`Error loading holon: ${result.message}`);
            return;
        }
        
        const holon = result.result;
        const dataPreview = JSON.stringify(holon.data || {}, null, 2);
        
        // Create modal or side panel
        showHolonDetailsModal(holon, dataPreview);
        
    } catch (error) {
        console.error('Error viewing holon:', error);
        alert(`Failed to load holon: ${error.message}`);
    }
}

/**
 * Show holon details modal
 */
function showHolonDetailsModal(holon, dataPreview) {
    const modal = document.createElement('div');
    modal.className = 'holon-details-modal';
    modal.innerHTML = `
        <div class="modal-overlay" onclick="closeHolonDetailsModal()"></div>
        <div class="modal-content">
            <button class="modal-close" onclick="closeHolonDetailsModal()">×</button>
            <h2>Holon Details</h2>
            <div class="holon-details">
                <div class="holon-detail-item">
                    <label>ID:</label>
                    <code>${holon.id || holon.holonId}</code>
                </div>
                <div class="holon-detail-item">
                    <label>Type:</label>
                    <span>${holon.holonType || 'N/A'}</span>
                </div>
                <div class="holon-detail-item">
                    <label>Name:</label>
                    <span>${holon.name || 'N/A'}</span>
                </div>
                <div class="holon-detail-item">
                    <label>Provider:</label>
                    <span>${holon.providerKey || holon.providerType || 'N/A'}</span>
                </div>
                <div class="holon-detail-item">
                    <label>Created:</label>
                    <span>${holon.createdDate ? new Date(holon.createdDate).toLocaleString() : 'N/A'}</span>
                </div>
                <div class="holon-detail-item">
                    <label>Modified:</label>
                    <span>${holon.modifiedDate ? new Date(holon.modifiedDate).toLocaleString() : 'N/A'}</span>
                </div>
                <div class="holon-detail-item full-width">
                    <label>Data:</label>
                    <pre class="holon-data-preview">${dataPreview}</pre>
                </div>
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
    
    // Store reference for closing
    window.currentHolonModal = modal;
}

/**
 * Close holon details modal
 */
function closeHolonDetailsModal() {
    if (window.currentHolonModal) {
        window.currentHolonModal.remove();
        window.currentHolonModal = null;
    }
}

/**
 * Migrate holon
 */
async function migrateHolon(holonId, fromProvider) {
    const { storageProviders } = dataManagementState;
    
    // Show provider selection
    const providerOptions = storageProviders
        .filter(p => (p.providerType || p.providerName) !== fromProvider)
        .map(p => `<option value="${p.providerType || p.providerName}">${p.providerName || p.providerType}</option>`)
        .join('');
    
    if (!providerOptions) {
        alert('No other providers available for migration');
        return;
    }
    
    const toProvider = prompt(`Select destination provider:\n\n${storageProviders
        .filter(p => (p.providerType || p.providerName) !== fromProvider)
        .map((p, i) => `${i + 1}. ${p.providerName || p.providerType}`)
        .join('\n')}\n\nEnter provider name:`, '');
    
    if (!toProvider) return;
    
    try {
        // Show progress
        showMigrationProgress(holonId, 'Starting migration...');
        
        // Load holon from source
        const holonResult = await oasisAPI.getHolon(holonId, fromProvider);
        if (holonResult.isError) {
            throw new Error(holonResult.message);
        }
        
        showMigrationProgress(holonId, 'Saving to destination...');
        
        // Save to destination
        const saveResult = await oasisAPI.saveHolon({
            ...holonResult.result,
            providerType: toProvider
        });
        
        if (saveResult.isError) {
            throw new Error(saveResult.message);
        }
        
        showMigrationProgress(holonId, 'Verifying...');
        
        // Verify
        const verifyResult = await oasisAPI.getHolon(saveResult.result.id, toProvider);
        if (verifyResult.isError) {
            throw new Error('Migration verification failed');
        }
        
        showMigrationProgress(holonId, 'Migration complete!');
        
        // Refresh
        setTimeout(() => {
            loadDataManagement();
        }, 1000);
        
    } catch (error) {
        console.error('Error migrating holon:', error);
        alert(`Migration failed: ${error.message}`);
    }
}

/**
 * Show migration progress
 */
function showMigrationProgress(holonId, message) {
    // Simple alert for now, can be enhanced with modal
    console.log(`Migration [${holonId}]: ${message}`);
}

/**
 * Delete holon
 */
async function deleteHolon(holonId, providerType) {
    if (!confirm(`Are you sure you want to delete this holon? This action cannot be undone.`)) {
        return;
    }
    
    try {
        const result = await oasisAPI.deleteHolon(holonId, providerType);
        
        if (result.isError) {
            alert(`Failed to delete holon: ${result.message}`);
            return;
        }
        
        alert('Holon deleted successfully');
        await loadDataManagement();
        
    } catch (error) {
        console.error('Error deleting holon:', error);
        alert(`Failed to delete holon: ${error.message}`);
    }
}

/**
 * Copy holon ID to clipboard
 */
function copyHolonId(holonId) {
    navigator.clipboard.writeText(holonId).then(() => {
        // Show feedback
        const btn = event.target;
        const original = btn.textContent;
        btn.textContent = '✓';
        setTimeout(() => {
            btn.textContent = original;
        }, 2000);
    });
}

/**
 * Filter holons
 */
function filterHolons() {
    const searchTerm = document.getElementById('holonSearch')?.value.toLowerCase() || '';
    const rows = document.querySelectorAll('.holon-row');
    
    rows.forEach(row => {
        const text = row.textContent.toLowerCase();
        row.style.display = text.includes(searchTerm) ? '' : 'none';
    });
}

/**
 * Export holons
 */
function exportHolons() {
    const { holons } = dataManagementState;
    const dataStr = JSON.stringify(holons, null, 2);
    const dataBlob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(dataBlob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `oasis-holons-${new Date().toISOString().split('T')[0]}.json`;
    link.click();
    URL.revokeObjectURL(url);
}

/**
 * Save settings
 */
async function saveSettings() {
    // This would save replication/failover settings
    // Implementation depends on available API endpoints
    alert('Settings saved (implementation pending API endpoints)');
}
