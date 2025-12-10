/**
 * Custom Oracle Service
 * Allows users to create and manage custom oracle feeds from any Web2/Web3 source
 */

// Oracle State
let oracleState = {
    feeds: [],
    selectedFeed: null,
    view: 'dashboard', // 'dashboard', 'builder', 'feed-detail'
    providers: [],
    isCreating: false,
    isFetching: false,
    builderState: {
        jobs: [], // Array of jobs, each with tasks
        currentJobIndex: -1,
        isSimulating: false,
        simulationResult: null
    }
};

// Provider categories
const PROVIDER_CATEGORIES = {
    blockchain: {
        name: 'Blockchain',
        providers: [
            { id: 'EthereumOASIS', name: 'Ethereum', description: 'Ethereum mainnet and testnets' },
            { id: 'SolanaOASIS', name: 'Solana', description: 'High-performance blockchain' },
            { id: 'PolygonOASIS', name: 'Polygon', description: 'Ethereum scaling solution' },
            { id: 'ArbitrumOASIS', name: 'Arbitrum', description: 'Ethereum Layer 2' },
            { id: 'AvalancheOASIS', name: 'Avalanche', description: 'High-performance blockchain' },
            { id: 'BNBChainOASIS', name: 'BNB Chain', description: 'Binance Smart Chain' },
            { id: 'BaseOASIS', name: 'Base', description: 'Coinbase Layer 2' },
            { id: 'OptimismOASIS', name: 'Optimism', description: 'Ethereum Layer 2' },
            { id: 'FantomOASIS', name: 'Fantom', description: 'Fast and scalable' }
        ]
    },
    database: {
        name: 'Database',
        providers: [
            { id: 'MongoDBOASIS', name: 'MongoDB', description: 'Document database' },
            { id: 'SQLLiteDBOASIS', name: 'SQLite', description: 'Lightweight SQL database' },
            { id: 'Neo4jOASIS', name: 'Neo4j', description: 'Graph database' },
            { id: 'AzureCosmosDBOASIS', name: 'Azure Cosmos DB', description: 'Multi-model database' }
        ]
    },
    cloud: {
        name: 'Cloud',
        providers: [
            { id: 'AWSOASIS', name: 'AWS', description: 'Amazon Web Services' },
            { id: 'GoogleCloudOASIS', name: 'Google Cloud', description: 'Google Cloud Platform' },
            { id: 'AzureOASIS', name: 'Azure', description: 'Microsoft Azure' }
        ]
    },
    storage: {
        name: 'Storage',
        providers: [
            { id: 'IPFSOASIS', name: 'IPFS', description: 'InterPlanetary File System' },
            { id: 'PinataOASIS', name: 'Pinata', description: 'IPFS gateway service' },
            { id: 'LocalFileOASIS', name: 'Local File', description: 'Local file storage' }
        ]
    }
};

// Task types (inspired by Switchboard)
const TASK_TYPES = {
    fetch: {
        name: 'Fetch',
        description: 'Fetch data from a source',
        icon: '→',
        category: 'input'
    },
    parse: {
        name: 'Parse',
        description: 'Parse JSON or response data',
        icon: '⚙',
        category: 'transform'
    },
    transform: {
        name: 'Transform',
        description: 'Transform or calculate data',
        icon: '↻',
        category: 'transform'
    },
    aggregate: {
        name: 'Aggregate',
        description: 'Aggregate multiple values',
        icon: 'Σ',
        category: 'output'
    }
};

// Query types (for backward compatibility and simple feeds)
const QUERY_TYPES = {
    smartContract: {
        name: 'Smart Contract',
        description: 'Query smart contract state or call functions',
        fields: ['contractAddress', 'function', 'parameters']
    },
    database: {
        name: 'Database Query',
        description: 'Execute database queries',
        fields: ['query', 'collection', 'filters']
    },
    api: {
        name: 'API Endpoint',
        description: 'Query external API endpoints',
        fields: ['url', 'method', 'headers', 'body']
    },
    blockchain: {
        name: 'Blockchain Data',
        description: 'Query blockchain data (balances, transactions)',
        fields: ['address', 'dataType']
    }
};

/**
 * Check if user is authenticated
 * DISABLED: Always returns true for now
 */
function isAuthenticated() {
    // Sign in disabled - always return true
    return true;
    // try {
    //     const authData = localStorage.getItem('oasis_auth');
    //     if (!authData) return false;
    //     const auth = JSON.parse(authData);
    //     const avatarId = auth.avatar?.avatarId || auth.avatar?.id;
    //     return !!avatarId;
    // } catch (error) {
    //     return false;
    // }
}

/**
 * Get avatar ID from auth
 * DISABLED: Returns mock ID for now
 */
function getAvatarId() {
    // Sign in disabled - return mock ID
    return 'mock-avatar-id';
    // try {
    //     const authData = localStorage.getItem('oasis_auth');
    //     if (!authData) return null;
    //     const auth = JSON.parse(authData);
    //     return auth.avatar?.avatarId || auth.avatar?.id || null;
    // } catch (error) {
    //     return null;
    // }
}

/**
 * Load oracle page
 */
async function loadOracle() {
    const container = document.getElementById('oracle-content');
    if (!container) return;

    const authenticated = isAuthenticated();
    
    if (!authenticated) {
        renderLoginPrompt(container);
        return;
    }

    oracleState.view = 'dashboard';
    await fetchOracleFeeds();
    renderOracleDashboard(container);
}

/**
 * Fetch user's oracle feeds
 */
async function fetchOracleFeeds() {
    const avatarId = getAvatarId();
    if (!avatarId) return;

    oracleState.isFetching = true;
    try {
        const response = await oasisAPI.getOracleFeeds(avatarId);
        if (response && !response.isError && response.result) {
            oracleState.feeds = response.result.data || response.result || [];
        }
    } catch (error) {
        console.error('Failed to fetch oracle feeds:', error);
        oracleState.feeds = [];
    } finally {
        oracleState.isFetching = false;
    }
}

/**
 * Render oracle dashboard
 */
function renderOracleDashboard(container) {
    const stats = {
        totalFeeds: oracleState.feeds.length,
        activeFeeds: oracleState.feeds.filter(f => f.status === 'active').length,
        publicFeeds: oracleState.feeds.filter(f => f.visibility === 'public').length
    };

    container.innerHTML = `
        <div class="portal-section">
            <div class="portal-section-header">
                <div>
                    <h2 class="portal-section-title">Oracle Feeds</h2>
                    <p class="portal-section-subtitle">Monitor data from any Web2 or Web3 source</p>
                </div>
                <button class="btn-primary" onclick="openOracleBuilder()">
                    Create Feed
                </button>
            </div>
        </div>

        <div class="portal-section">
            <div class="stats-grid" style="grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); margin-bottom: 2rem;">
                <div class="stat-card">
                    <div class="stat-label">Total Feeds</div>
                    <div class="stat-value">${stats.totalFeeds}</div>
                    <div class="stat-detail">Your oracle feeds</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Active</div>
                    <div class="stat-value">${stats.activeFeeds}</div>
                    <div class="stat-detail">Currently monitoring</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Public</div>
                    <div class="stat-value">${stats.publicFeeds}</div>
                    <div class="stat-detail">Shared feeds</div>
                </div>
            </div>
        </div>

        <div class="portal-section">
            <div class="portal-card">
                <div class="portal-card-header">
                    <h3 class="portal-card-title">My Feeds</h3>
                </div>
                <div id="oracle-feeds-list">
                    ${renderFeedsList()}
                </div>
            </div>
        </div>
    `;
}

/**
 * Render feeds list
 */
function renderFeedsList() {
    if (oracleState.isFetching) {
        return '<div class="empty-state">Loading feeds...</div>';
    }

    if (oracleState.feeds.length === 0) {
        return `
            <div class="empty-state">
                <p>No oracle feeds yet</p>
                <p style="color: var(--text-tertiary); font-size: 0.875rem; margin-top: 0.5rem;">
                    Create your first feed to start monitoring data from any source
                </p>
                <button class="btn-primary" onclick="openOracleBuilder()" style="margin-top: 1.5rem;">
                    Create Feed
                </button>
            </div>
        `;
    }

    return oracleState.feeds.map(feed => `
        <div class="oracle-feed-card" onclick="viewOracleFeed('${feed.feedId || feed.id}')">
            <div class="oracle-feed-header">
                <div>
                    <h4 class="oracle-feed-name">${feed.name || 'Unnamed Feed'}</h4>
                    <p class="oracle-feed-description">${feed.description || 'No description'}</p>
                </div>
                <div class="oracle-feed-status">
                    <span class="oracle-status-badge ${feed.status || 'inactive'}">${(feed.status || 'inactive').charAt(0).toUpperCase() + (feed.status || 'inactive').slice(1)}</span>
                </div>
            </div>
            <div class="oracle-feed-details">
                <div class="oracle-feed-detail">
                    <span class="oracle-feed-detail-label">Source:</span>
                    <span class="oracle-feed-detail-value">${feed.sources?.[0]?.provider || 'Unknown'}</span>
                </div>
                <div class="oracle-feed-detail">
                    <span class="oracle-feed-detail-label">Frequency:</span>
                    <span class="oracle-feed-detail-value">${formatFrequency(feed.monitoring?.frequency)}</span>
                </div>
                ${feed.currentValue ? `
                    <div class="oracle-feed-detail">
                        <span class="oracle-feed-detail-label">Current Value:</span>
                        <span class="oracle-feed-detail-value">${formatValue(feed.currentValue)}</span>
                    </div>
                ` : ''}
            </div>
            <div class="oracle-feed-actions">
                <button class="btn-text" onclick="event.stopPropagation(); viewOracleFeed('${feed.feedId || feed.id}')">
                    View
                </button>
                <button class="btn-text" onclick="event.stopPropagation(); editOracleFeed('${feed.feedId || feed.id}')">
                    Edit
                </button>
                <button class="btn-text" onclick="event.stopPropagation(); deleteOracleFeed('${feed.feedId || feed.id}')" style="color: var(--text-tertiary);">
                    Delete
                </button>
            </div>
        </div>
    `).join('');
}

/**
 * Open oracle builder
 */
function openOracleBuilder() {
    const container = document.getElementById('oracle-content');
    if (!container) return;

    oracleState.view = 'builder';
    // Reset builder state
    oracleState.builderState.jobs = [];
    oracleState.builderState.simulationResult = null;
    renderOracleBuilder(container);
}

/**
 * Render oracle builder
 */
function renderOracleBuilder(container) {
    container.innerHTML = `
        <div class="portal-section">
            <div class="portal-section-header">
                <div>
                    <h2 class="portal-section-title">Create Oracle Feed</h2>
                    <p class="portal-section-subtitle">Drag providers and build visual data pipelines</p>
                </div>
                <div style="display: flex; gap: 0.5rem;">
                    <button type="button" class="btn-text" onclick="importOracleFeedJSON()" title="Import JSON">
                        Import JSON
                    </button>
                    <button type="button" class="btn-text" onclick="exportOracleFeedJSON()" title="Export JSON">
                        Export JSON
                    </button>
                    <button class="btn-text" onclick="loadOracle()">
                        Cancel
                    </button>
                </div>
            </div>
        </div>

        <div class="oracle-visual-builder">
            <div class="oracle-builder-layout">
                <!-- Provider Palette (Left Sidebar) -->
                <div class="oracle-provider-palette">
                    <div class="oracle-palette-header">
                        <h3 class="oracle-palette-title">Providers</h3>
                        <p class="oracle-palette-subtitle">Drag to canvas</p>
                    </div>
                    <div class="oracle-palette-content">
                        ${renderProviderPalette()}
                    </div>
                </div>

                <!-- Main Canvas (Center) -->
                <div class="oracle-builder-canvas">
                    <div class="oracle-canvas-header">
                        <div>
                            <h3 class="oracle-canvas-title">Feed Builder</h3>
                            <p class="oracle-canvas-subtitle">${oracleState.builderState.jobs.length} job${oracleState.builderState.jobs.length !== 1 ? 's' : ''}</p>
                        </div>
                        <div style="display: flex; gap: 0.5rem;">
                            <button type="button" class="btn-text" onclick="simulateOracleFeed()" ${oracleState.builderState.jobs.length === 0 ? 'disabled' : ''} id="simulate-btn">
                                ${oracleState.builderState.isSimulating ? 'Simulating...' : 'Simulate'}
                            </button>
                            ${oracleState.builderState.simulationResult ? `
                                <button type="button" class="btn-text" onclick="clearSimulation()" style="color: var(--text-tertiary);">
                                    Clear Results
                                </button>
                            ` : ''}
                        </div>
                    </div>
                    <div class="oracle-canvas-workspace" id="oracle-canvas-workspace" ondrop="handleCanvasDrop(event)" ondragover="handleCanvasDragOver(event)" ondragleave="handleCanvasDragLeave(event)">
                        ${renderCanvasContent()}
                    </div>
                </div>

                <!-- Feed Configuration (Right Sidebar) -->
                <div class="oracle-feed-config">
                    <div class="oracle-config-header">
                        <h3 class="oracle-config-title">Feed Settings</h3>
                    </div>
                    <form id="oracle-feed-form" onsubmit="handleCreateOracleFeed(event)" class="oracle-config-form">
                        <div class="oracle-form-section">
                            <h3 class="oracle-form-section-title">Feed Information</h3>
                            <div class="oracle-form-field">
                                <label class="oracle-form-label">Feed Name</label>
                                <input 
                                    type="text" 
                                    id="oracle-feed-name" 
                                    class="oracle-form-input" 
                                    placeholder="e.g., My ETH Balance Monitor"
                                    required
                                />
                            </div>
                            <div class="oracle-form-field">
                                <label class="oracle-form-label">Description</label>
                                <textarea 
                                    id="oracle-feed-description" 
                                    class="oracle-form-input oracle-form-textarea" 
                                    placeholder="Describe what this feed monitors"
                                    rows="3"
                                ></textarea>
                            </div>
                            <div class="oracle-form-field">
                                <label class="oracle-form-label">Category</label>
                                <select id="oracle-feed-category" class="oracle-form-input">
                                    <option value="price">Price</option>
                                    <option value="ownership">Ownership</option>
                                    <option value="status">Status</option>
                                    <option value="custom">Custom</option>
                                </select>
                            </div>
                            <div class="oracle-form-field">
                                <label class="oracle-form-label">Visibility</label>
                                <select id="oracle-feed-visibility" class="oracle-form-input">
                                    <option value="private">Private</option>
                                    <option value="public">Public</option>
                                </select>
                            </div>
                        </div>

                        <div class="oracle-form-section">
                            <h3 class="oracle-form-section-title">Monitoring Configuration</h3>
                            <div class="oracle-form-field">
                                <label class="oracle-form-label">Frequency</label>
                                <select id="oracle-frequency" class="oracle-form-input">
                                    <option value="realtime">Real-time (WebSocket)</option>
                                    <option value="1second">Every 1 second</option>
                                    <option value="1minute" selected>Every 1 minute</option>
                                    <option value="5minutes">Every 5 minutes</option>
                                    <option value="1hour">Every 1 hour</option>
                                    <option value="daily">Daily</option>
                                </select>
                            </div>
                            <div class="oracle-form-field">
                                <label class="oracle-form-label">Trigger</label>
                                <select id="oracle-trigger" class="oracle-form-input">
                                    <option value="onChange" selected>On Change</option>
                                    <option value="onThreshold">On Threshold</option>
                                    <option value="always">Always</option>
                                </select>
                            </div>
                            <div id="oracle-threshold-fields" style="display: none;">
                                <div class="oracle-form-field">
                                    <label class="oracle-form-label">Threshold Value</label>
                                    <input 
                                        type="number" 
                                        id="oracle-threshold-value" 
                                        class="oracle-form-input" 
                                        placeholder="Enter threshold value"
                                    />
                                </div>
                                <div class="oracle-form-field">
                                    <label class="oracle-form-label">Operator</label>
                                    <select id="oracle-threshold-operator" class="oracle-form-input">
                                        <option value="greaterThan">Greater Than</option>
                                        <option value="lessThan">Less Than</option>
                                        <option value="equals">Equals</option>
                                        <option value="between">Between</option>
                                    </select>
                                </div>
                            </div>
                        </div>

                        <div class="oracle-form-actions">
                            <button type="button" class="btn-text" onclick="loadOracle()">
                                Cancel
                            </button>
                            <button type="submit" class="btn-primary" ${oracleState.isCreating || oracleState.builderState.jobs.length === 0 ? 'disabled' : ''}>
                                ${oracleState.isCreating ? 'Creating...' : 'Create Feed'}
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
        ${oracleState.builderState.simulationResult ? `
            <div class="portal-section" style="margin-top: 2rem;">
                ${renderSimulationResults()}
            </div>
        ` : ''}
    `;

    // Initialize drag and drop
    initializeDragAndDrop();

    // Show/hide threshold fields based on trigger
    setTimeout(() => {
        const triggerSelect = document.getElementById('oracle-trigger');
        if (triggerSelect) {
            triggerSelect.addEventListener('change', (e) => {
                const thresholdFields = document.getElementById('oracle-threshold-fields');
                if (thresholdFields) {
                    thresholdFields.style.display = e.target.value === 'onThreshold' ? 'block' : 'none';
                }
            });
        }
    }, 100);
}

/**
 * Render provider palette
 */
function renderProviderPalette() {
    let html = '';
    
    Object.keys(PROVIDER_CATEGORIES).forEach(category => {
        const categoryData = PROVIDER_CATEGORIES[category];
        html += `
            <div class="oracle-palette-category">
                <div class="oracle-palette-category-header">
                    <span class="oracle-palette-category-name">${categoryData.name}</span>
                </div>
                <div class="oracle-palette-providers">
                    ${categoryData.providers.map(provider => `
                        <div 
                            class="oracle-provider-card" 
                            draggable="true"
                            data-provider-id="${provider.id}"
                            data-provider-category="${category}"
                            data-provider-name="${provider.name}"
                            ondragstart="handleProviderDragStart(event)"
                            onclick="addJobFromProvider({id: '${provider.id}', category: '${category}', name: '${provider.name}'})"
                            title="Click or drag to canvas to create job"
                        >
                            <div class="oracle-provider-icon">${getProviderIcon(provider.id)}</div>
                            <div class="oracle-provider-info">
                                <div class="oracle-provider-name">${provider.name}</div>
                                <div class="oracle-provider-desc">${provider.description}</div>
                            </div>
                        </div>
                    `).join('')}
                </div>
            </div>
        `;
    });
    
    return html;
}

/**
 * Get provider logo path
 */
function getProviderLogo(providerId) {
    const logoMap = {
        // Blockchain providers
        'EthereumOASIS': '../oasisweb4 site/new-v2/logos/ethereum.svg',
        'SolanaOASIS': '../oasisweb4 site/new-v2/logos/solana.svg',
        'PolygonOASIS': '../oasisweb4 site/new-v2/logos/polygon.svg',
        'ArbitrumOASIS': '../UniversalAssetBridge/frontend/public/ARB.png',
        'AvalancheOASIS': '../oasisweb4 site/new-v2/logos/avalanche.svg',
        'BNBChainOASIS': '../oasisweb4 site/new-v2/logos/bnb.svg',
        'BaseOASIS': '../oasisweb4 site/new-v2/logos/base.svg',
        'OptimismOASIS': '../oasisweb4 site/new-v2/logos/optimism.svg',
        'FantomOASIS': '../oasisweb4 site/new-v2/logos/fantom.svg',
        // Database providers
        'MongoDBOASIS': '../oasisweb4 site/new-v2/logos/mongodb.png',
        'SQLLiteDBOASIS': '../oasisweb4 site/new-v2/logos/ethereum.svg', // No specific logo, use fallback
        'Neo4jOASIS': '../oasisweb4 site/new-v2/logos/ethereum.svg', // No specific logo, use fallback
        'AzureCosmosDBOASIS': '../oasisweb4 site/new-v2/logos/azure.png',
        // Cloud providers
        'AWSOASIS': '../oasisweb4 site/new-v2/logos/aws.png',
        'GoogleCloudOASIS': '../oasisweb4 site/new-v2/logos/google-cloud.png',
        'AzureOASIS': '../oasisweb4 site/new-v2/logos/azure.png',
        // Storage providers
        'IPFSOASIS': '../oasisweb4 site/new-v2/logos/ipfs.png',
        'PinataOASIS': '../oasisweb4 site/new-v2/logos/ipfs.png', // Use IPFS logo as Pinata is IPFS-based
        'LocalFileOASIS': '../oasisweb4 site/new-v2/logos/ethereum.svg' // No specific logo, use fallback
    };
    
    return logoMap[providerId] || '../oasisweb4 site/new-v2/logos/ethereum.svg';
}

/**
 * Get provider icon (returns HTML img tag or fallback)
 */
function getProviderIcon(providerId) {
    const logoPath = getProviderLogo(providerId);
    return `<img src="${logoPath}" alt="${providerId}" class="oracle-provider-logo-img" onerror="this.onerror=null; this.style.display='none'; this.nextElementSibling.style.display='flex';"><span class="oracle-provider-logo-fallback" style="display: none;">${getProviderIconFallback(providerId)}</span>`;
}

/**
 * Get provider icon fallback (text-based)
 */
function getProviderIconFallback(providerId) {
    const icons = {
        'EthereumOASIS': 'Ξ',
        'SolanaOASIS': '◎',
        'PolygonOASIS': '⬟',
        'ArbitrumOASIS': '⟠',
        'AvalancheOASIS': '🔺',
        'BNBChainOASIS': 'BNB',
        'BaseOASIS': 'BASE',
        'OptimismOASIS': 'OP',
        'FantomOASIS': 'FTM',
        'MongoDBOASIS': 'M',
        'SQLLiteDBOASIS': 'SQL',
        'Neo4jOASIS': 'N',
        'AzureCosmosDBOASIS': 'C',
        'AWSOASIS': 'AWS',
        'GoogleCloudOASIS': 'GCP',
        'AzureOASIS': 'AZ',
        'IPFSOASIS': 'IPFS',
        'PinataOASIS': 'PIN',
        'LocalFileOASIS': '📁'
    };
    return icons[providerId] || '●';
}

/**
 * Render canvas content
 */
function renderCanvasContent() {
    if (oracleState.builderState.jobs.length === 0) {
        return `
            <div class="oracle-canvas-empty">
                <div class="oracle-canvas-empty-icon">⚡</div>
                <h3 class="oracle-canvas-empty-title">Start Building</h3>
                <p class="oracle-canvas-empty-text">
                    Drag a provider from the left to create your first job
                </p>
                <p class="oracle-canvas-empty-hint">
                    Or click a provider to add it quickly
                </p>
            </div>
        `;
    }

    return oracleState.builderState.jobs.map((job, jobIndex) => `
        <div class="oracle-job-visual" data-job-index="${jobIndex}" draggable="true" ondragstart="handleJobDragStart(event, ${jobIndex})" ondragend="handleJobDragEnd(event)">
            <div class="oracle-job-visual-header">
                <div class="oracle-job-visual-provider">
                    <div class="oracle-job-provider-icon">${getProviderIcon(job.provider)}</div>
                    <div>
                        <div class="oracle-job-visual-name">${job.name || `Job ${jobIndex + 1}`}</div>
                        <div class="oracle-job-visual-provider-name">${getProviderName(job.provider)}</div>
                    </div>
                </div>
                <div class="oracle-job-visual-actions">
                    <button type="button" class="oracle-job-action-btn" onclick="editJobVisual(${jobIndex})" title="Edit">
                        ⚙
                    </button>
                    <button type="button" class="oracle-job-action-btn" onclick="removeOracleJob(${jobIndex})" title="Remove">
                        ×
                    </button>
                </div>
            </div>
            <div class="oracle-job-visual-tasks">
                ${renderVisualTaskChain(jobIndex)}
            </div>
        </div>
    `).join('');
}

/**
 * Render visual task chain
 */
function renderVisualTaskChain(jobIndex) {
    const job = oracleState.builderState.jobs[jobIndex];
    if (!job || !job.tasks || job.tasks.length === 0) {
        return `
            <div class="oracle-task-chain-empty">
                <button type="button" class="btn-text" onclick="showTaskTypeSelector(${jobIndex})" style="font-size: 0.875rem;">
                    + Add Task
                </button>
            </div>
        `;
    }

    return `
        <div class="oracle-task-chain">
            ${job.tasks.map((task, taskIndex) => `
                <div class="oracle-task-node" data-task-index="${taskIndex}">
                    <div class="oracle-task-node-content">
                        <div class="oracle-task-node-icon">${TASK_TYPES[task.type]?.icon || '•'}</div>
                        <div class="oracle-task-node-label">${TASK_TYPES[task.type]?.name || task.type}</div>
                    </div>
                    ${taskIndex < job.tasks.length - 1 ? '<div class="oracle-task-connector"></div>' : ''}
                    <div class="oracle-task-node-actions">
                        <button type="button" class="oracle-task-action-btn" onclick="insertTaskAfter(${jobIndex}, ${taskIndex})" title="Add task after">
                            +
                        </button>
                        <button type="button" class="oracle-task-action-btn" onclick="removeTaskFromJob(${jobIndex}, ${taskIndex})" title="Remove">
                            ×
                        </button>
                    </div>
                </div>
            `).join('')}
        </div>
    `;
}

/**
 * Get provider name
 */
function getProviderName(providerId) {
    for (const category of Object.values(PROVIDER_CATEGORIES)) {
        const provider = category.providers.find(p => p.id === providerId);
        if (provider) return provider.name;
    }
    return providerId;
}

/**
 * Initialize drag and drop
 */
function initializeDragAndDrop() {
    // Make provider cards draggable (already set in HTML)
    // Canvas drop handlers are in HTML
}

/**
 * Handle provider drag start
 */
function handleProviderDragStart(event) {
    const providerId = event.target.closest('.oracle-provider-card').dataset.providerId;
    const providerCategory = event.target.closest('.oracle-provider-card').dataset.providerCategory;
    const providerName = event.target.closest('.oracle-provider-card').dataset.providerName;
    
    event.dataTransfer.setData('application/oracle-provider', JSON.stringify({
        id: providerId,
        category: providerCategory,
        name: providerName
    }));
    event.dataTransfer.effectAllowed = 'copy';
    
    event.target.closest('.oracle-provider-card').style.opacity = '0.5';
}

/**
 * Handle canvas drag over
 */
function handleCanvasDragOver(event) {
    if (event.dataTransfer.types.includes('application/oracle-provider') || 
        event.dataTransfer.types.includes('application/oracle-job')) {
        event.preventDefault();
        event.dataTransfer.dropEffect = 'copy';
        event.currentTarget.classList.add('oracle-canvas-drag-over');
    }
}

/**
 * Handle canvas drag leave
 */
function handleCanvasDragLeave(event) {
    if (!event.currentTarget.contains(event.relatedTarget)) {
        event.currentTarget.classList.remove('oracle-canvas-drag-over');
    }
}

/**
 * Handle canvas drop
 */
function handleCanvasDrop(event) {
    event.preventDefault();
    event.currentTarget.classList.remove('oracle-canvas-drag-over');
    
    // Handle provider drop
    if (event.dataTransfer.types.includes('application/oracle-provider')) {
        const providerData = JSON.parse(event.dataTransfer.getData('application/oracle-provider'));
        addJobFromProvider(providerData);
    }
    
    // Handle job reorder (if implemented)
    if (event.dataTransfer.types.includes('application/oracle-job')) {
        const jobIndex = parseInt(event.dataTransfer.getData('application/oracle-job'));
        // Could implement job reordering here
    }
    
    // Reset provider card opacity
    document.querySelectorAll('.oracle-provider-card').forEach(card => {
        card.style.opacity = '1';
    });
}

/**
 * Add job from provider
 */
function addJobFromProvider(providerData) {
    oracleState.builderState.jobs.push({
        name: '',
        providerCategory: providerData.category,
        provider: providerData.id,
        tasks: [{
            type: 'fetch',
            queryType: 'smartContract',
            contractAddress: '',
            function: '',
            parameters: ''
        }]
    });
    refreshOracleBuilder();
}

/**
 * Handle job drag start
 */
function handleJobDragStart(event, jobIndex) {
    event.dataTransfer.setData('application/oracle-job', jobIndex.toString());
    event.dataTransfer.effectAllowed = 'move';
    event.currentTarget.style.opacity = '0.5';
}

/**
 * Handle job drag end
 */
function handleJobDragEnd(event) {
    event.currentTarget.style.opacity = '1';
}

/**
 * Insert task after another task
 */
function insertTaskAfter(jobIndex, afterTaskIndex) {
    showTaskTypeSelector(jobIndex, afterTaskIndex + 1);
}

/**
 * Edit job visual (opens edit panel)
 */
function editJobVisual(jobIndex) {
    // Toggle job edit mode
    const jobCard = document.querySelector(`.oracle-job-visual[data-job-index="${jobIndex}"]`);
    if (jobCard) {
        const isExpanded = jobCard.classList.contains('oracle-job-expanded');
        if (isExpanded) {
            jobCard.classList.remove('oracle-job-expanded');
        } else {
            // Expand to show full configuration
            jobCard.classList.add('oracle-job-expanded');
            showJobEditPanel(jobIndex);
        }
    }
}

/**
 * Show job edit panel
 */
function showJobEditPanel(jobIndex) {
    const job = oracleState.builderState.jobs[jobIndex];
    if (!job) return;

    const jobCard = document.querySelector(`.oracle-job-visual[data-job-index="${jobIndex}"]`);
    if (!jobCard) return;

    const existingPanel = jobCard.querySelector('.oracle-job-edit-panel');
    if (existingPanel) {
        existingPanel.remove();
        return;
    }

    const panel = document.createElement('div');
    panel.className = 'oracle-job-edit-panel';
    panel.innerHTML = `
        <div class="oracle-job-edit-content">
            <div class="oracle-form-field">
                <label class="oracle-form-label">Job Name</label>
                <input 
                    type="text" 
                    class="oracle-form-input" 
                    value="${job.name || ''}"
                    onchange="updateJobName(${jobIndex}, this.value); refreshOracleBuilder();"
                    placeholder="Optional job name"
                />
            </div>
            <div class="oracle-form-field">
                <label class="oracle-form-label">Provider</label>
                <select class="oracle-form-input" onchange="updateJobProvider(${jobIndex}, this.value); refreshOracleBuilder();">
                    ${renderProviderOptionsForJob(job.providerCategory || 'blockchain', job.provider)}
                </select>
            </div>
            <div class="oracle-job-tasks-config">
                <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.75rem;">
                    <label class="oracle-form-label" style="margin: 0;">Tasks</label>
                    <button type="button" class="btn-text" onclick="showTaskTypeSelector(${jobIndex})" style="font-size: 0.75rem;">
                        + Add Task
                    </button>
                </div>
                ${renderTasksForJob(jobIndex)}
            </div>
        </div>
    `;
    
    const tasksContainer = jobCard.querySelector('.oracle-job-visual-tasks');
    if (tasksContainer) {
        tasksContainer.appendChild(panel);
    }
}

/**
 * Render jobs list (for non-visual mode, kept for compatibility)
 */
function renderJobsList() {
    if (oracleState.builderState.jobs.length === 0) {
        return '';
    }

    return oracleState.builderState.jobs.map((job, jobIndex) => `
        <div class="oracle-job-card" data-job-index="${jobIndex}">
            <div class="oracle-job-header">
                <div style="display: flex; align-items: center; gap: 0.75rem;">
                    <span style="color: var(--text-secondary); font-size: 0.875rem; font-weight: 500;">Job ${jobIndex + 1}</span>
                    <input 
                        type="text" 
                        class="oracle-form-input" 
                        style="flex: 1; max-width: 300px; padding: 0.5rem; font-size: 0.875rem;"
                        placeholder="Job name (optional)"
                        value="${job.name || ''}"
                        onchange="updateJobName(${jobIndex}, this.value)"
                    />
                </div>
                <div style="display: flex; gap: 0.5rem; align-items: center;">
                    ${jobIndex > 0 ? `
                        <button type="button" class="btn-text" onclick="moveJob(${jobIndex}, -1)" style="font-size: 0.75rem; padding: 0.25rem 0.5rem;" title="Move up">
                            ↑
                        </button>
                    ` : ''}
                    ${jobIndex < oracleState.builderState.jobs.length - 1 ? `
                        <button type="button" class="btn-text" onclick="moveJob(${jobIndex}, 1)" style="font-size: 0.75rem; padding: 0.25rem 0.5rem;" title="Move down">
                            ↓
                        </button>
                    ` : ''}
                    <button type="button" class="btn-text" onclick="removeOracleJob(${jobIndex})" style="color: var(--text-tertiary); font-size: 0.75rem; padding: 0.25rem 0.5rem;" title="Remove job">
                        ×
                    </button>
                </div>
            </div>
            <div class="oracle-job-content">
                <div class="oracle-job-source">
                    <div class="oracle-form-field" style="margin-bottom: 1rem;">
                        <label class="oracle-form-label">Provider Category</label>
                        <select class="oracle-form-input oracle-job-provider-category" onchange="updateJobProviderOptions(${jobIndex})">
                            ${Object.keys(PROVIDER_CATEGORIES).map(cat => `
                                <option value="${cat}" ${job.providerCategory === cat ? 'selected' : ''}>${PROVIDER_CATEGORIES[cat].name}</option>
                            `).join('')}
                        </select>
                    </div>
                    <div class="oracle-form-field" style="margin-bottom: 1rem;">
                        <label class="oracle-form-label">Provider</label>
                        <select class="oracle-form-input oracle-job-provider" onchange="updateJobProvider(${jobIndex}, this.value)">
                            ${renderProviderOptionsForJob(job.providerCategory || 'blockchain', job.provider)}
                        </select>
                    </div>
                </div>
                <div class="oracle-job-tasks">
                    <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.75rem;">
                        <label class="oracle-form-label" style="margin: 0;">Tasks</label>
                        <button type="button" class="btn-text" onclick="addTaskToJob(${jobIndex})" style="font-size: 0.75rem; padding: 0.25rem 0.5rem;">
                            + Add Task
                        </button>
                    </div>
                    <div class="oracle-tasks-list" id="oracle-tasks-${jobIndex}">
                        ${renderTasksForJob(jobIndex)}
                    </div>
                </div>
            </div>
        </div>
    `).join('');
}

/**
 * Render tasks for a job
 */
function renderTasksForJob(jobIndex) {
    const job = oracleState.builderState.jobs[jobIndex];
    if (!job || !job.tasks || job.tasks.length === 0) {
        return `
            <div class="empty-state" style="padding: 1rem; font-size: 0.875rem; color: var(--text-tertiary);">
                No tasks yet. Add a task to start building your data pipeline.
            </div>
        `;
    }

    return job.tasks.map((task, taskIndex) => `
        <div class="oracle-task-card" data-task-index="${taskIndex}">
            <div class="oracle-task-header">
                <div style="display: flex; align-items: center; gap: 0.5rem;">
                    <span style="color: var(--text-secondary); font-size: 0.75rem;">${TASK_TYPES[task.type]?.icon || '•'}</span>
                    <span style="font-size: 0.875rem; font-weight: 500;">${TASK_TYPES[task.type]?.name || task.type}</span>
                </div>
                <div style="display: flex; gap: 0.5rem;">
                    ${taskIndex > 0 ? `
                        <button type="button" class="btn-text" onclick="moveTask(${jobIndex}, ${taskIndex}, -1)" style="font-size: 0.75rem; padding: 0.25rem;" title="Move up">
                            ↑
                        </button>
                    ` : ''}
                    ${taskIndex < job.tasks.length - 1 ? `
                        <button type="button" class="btn-text" onclick="moveTask(${jobIndex}, ${taskIndex}, 1)" style="font-size: 0.75rem; padding: 0.25rem;" title="Move down">
                            ↓
                        </button>
                    ` : ''}
                    <button type="button" class="btn-text" onclick="removeTaskFromJob(${jobIndex}, ${taskIndex})" style="color: var(--text-tertiary); font-size: 0.75rem; padding: 0.25rem;" title="Remove task">
                        ×
                    </button>
                </div>
            </div>
            <div class="oracle-task-content">
                ${renderTaskFields(jobIndex, taskIndex, task)}
            </div>
        </div>
    `).join('');
}

/**
 * Render task fields based on task type
 */
function renderTaskFields(jobIndex, taskIndex, task) {
    const taskType = task.type || 'fetch';
    let fields = '';

    if (taskType === 'fetch') {
        fields = `
            <div class="oracle-form-field">
                <label class="oracle-form-label">Query Type</label>
                <select class="oracle-form-input oracle-task-query-type" onchange="updateTaskQueryType(${jobIndex}, ${taskIndex}, this.value)">
                    ${Object.keys(QUERY_TYPES).map(type => `
                        <option value="${type}" ${task.queryType === type ? 'selected' : ''}>${QUERY_TYPES[type].name}</option>
                    `).join('')}
                </select>
            </div>
            <div class="oracle-task-query-fields" id="task-query-fields-${jobIndex}-${taskIndex}">
                ${renderTaskQueryFields(jobIndex, taskIndex, task.queryType || 'smartContract', task)}
            </div>
        `;
    } else if (taskType === 'parse') {
        fields = `
            <div class="oracle-form-field">
                <label class="oracle-form-label">JSON Path</label>
                <input 
                    type="text" 
                    class="oracle-form-input oracle-task-json-path" 
                    placeholder="$.data.price or $.result[0].value"
                    value="${task.jsonPath || ''}"
                    onchange="updateTaskField(${jobIndex}, ${taskIndex}, 'jsonPath', this.value)"
                />
                <p style="color: var(--text-tertiary); font-size: 0.75rem; margin-top: 0.25rem;">
                    Use JSONPath syntax to extract data from the previous task's output
                </p>
            </div>
        `;
    } else if (taskType === 'transform') {
        fields = `
            <div class="oracle-form-field">
                <label class="oracle-form-label">Transform Expression</label>
                <textarea 
                    class="oracle-form-input oracle-form-textarea oracle-task-transform" 
                    placeholder="value * 100 or value.toFixed(2)"
                    rows="2"
                    onchange="updateTaskField(${jobIndex}, ${taskIndex}, 'transform', this.value)"
                >${task.transform || ''}</textarea>
                <p style="color: var(--text-tertiary); font-size: 0.75rem; margin-top: 0.25rem;">
                    JavaScript expression. Use 'value' to refer to the input from previous task.
                </p>
            </div>
        `;
    } else if (taskType === 'aggregate') {
        fields = `
            <div class="oracle-form-field">
                <label class="oracle-form-label">Aggregation Method</label>
                <select class="oracle-form-input oracle-task-aggregate" onchange="updateTaskField(${jobIndex}, ${taskIndex}, 'aggregateMethod', this.value)">
                    <option value="average" ${task.aggregateMethod === 'average' ? 'selected' : ''}>Average</option>
                    <option value="sum" ${task.aggregateMethod === 'sum' ? 'selected' : ''}>Sum</option>
                    <option value="min" ${task.aggregateMethod === 'min' ? 'selected' : ''}>Minimum</option>
                    <option value="max" ${task.aggregateMethod === 'max' ? 'selected' : ''}>Maximum</option>
                    <option value="median" ${task.aggregateMethod === 'median' ? 'selected' : ''}>Median</option>
                </select>
            </div>
        `;
    }

    return fields;
}

/**
 * Render task query fields
 */
function renderTaskQueryFields(jobIndex, taskIndex, queryType, task) {
    let fields = '';
    
    if (queryType === 'smartContract') {
        fields = `
            <div class="oracle-form-field">
                <label class="oracle-form-label">Contract Address</label>
                <input 
                    type="text" 
                    class="oracle-form-input oracle-task-contract-address" 
                    placeholder="0x..."
                    value="${task.contractAddress || ''}"
                    onchange="updateTaskField(${jobIndex}, ${taskIndex}, 'contractAddress', this.value)"
                />
            </div>
            <div class="oracle-form-field">
                <label class="oracle-form-label">Function</label>
                <input 
                    type="text" 
                    class="oracle-form-input oracle-task-function" 
                    placeholder="balanceOf"
                    value="${task.function || ''}"
                    onchange="updateTaskField(${jobIndex}, ${taskIndex}, 'function', this.value)"
                />
            </div>
            <div class="oracle-form-field">
                <label class="oracle-form-label">Parameters (JSON)</label>
                <textarea 
                    class="oracle-form-input oracle-form-textarea oracle-task-parameters" 
                    placeholder='{"address": "0x..."}'
                    rows="2"
                    onchange="updateTaskField(${jobIndex}, ${taskIndex}, 'parameters', this.value)"
                >${task.parameters ? (typeof task.parameters === 'string' ? task.parameters : JSON.stringify(task.parameters)) : ''}</textarea>
            </div>
        `;
    } else if (queryType === 'api') {
        fields = `
            <div class="oracle-form-field">
                <label class="oracle-form-label">API URL</label>
                <input 
                    type="url" 
                    class="oracle-form-input oracle-task-api-url" 
                    placeholder="https://api.example.com/data"
                    value="${task.url || ''}"
                    onchange="updateTaskField(${jobIndex}, ${taskIndex}, 'url', this.value)"
                />
            </div>
            <div class="oracle-form-field">
                <label class="oracle-form-label">HTTP Method</label>
                <select class="oracle-form-input oracle-task-api-method" onchange="updateTaskField(${jobIndex}, ${taskIndex}, 'method', this.value)">
                    <option value="GET" ${task.method === 'GET' ? 'selected' : ''}>GET</option>
                    <option value="POST" ${task.method === 'POST' ? 'selected' : ''}>POST</option>
                    <option value="PUT" ${task.method === 'PUT' ? 'selected' : ''}>PUT</option>
                </select>
            </div>
        `;
    } else if (queryType === 'database') {
        fields = `
            <div class="oracle-form-field">
                <label class="oracle-form-label">Query</label>
                <textarea 
                    class="oracle-form-input oracle-form-textarea oracle-task-query" 
                    placeholder="SELECT * FROM collection WHERE..."
                    rows="3"
                    onchange="updateTaskField(${jobIndex}, ${taskIndex}, 'query', this.value)"
                >${task.query || ''}</textarea>
            </div>
        `;
    } else if (queryType === 'blockchain') {
        fields = `
            <div class="oracle-form-field">
                <label class="oracle-form-label">Address</label>
                <input 
                    type="text" 
                    class="oracle-form-input oracle-task-address" 
                    placeholder="0x... or address"
                    value="${task.address || ''}"
                    onchange="updateTaskField(${jobIndex}, ${taskIndex}, 'address', this.value)"
                />
            </div>
            <div class="oracle-form-field">
                <label class="oracle-form-label">Data Type</label>
                <select class="oracle-form-input oracle-task-data-type" onchange="updateTaskField(${jobIndex}, ${taskIndex}, 'dataType', this.value)">
                    <option value="balance" ${task.dataType === 'balance' ? 'selected' : ''}>Balance</option>
                    <option value="transactionCount" ${task.dataType === 'transactionCount' ? 'selected' : ''}>Transaction Count</option>
                    <option value="code" ${task.dataType === 'code' ? 'selected' : ''}>Contract Code</option>
                </select>
            </div>
        `;
    }

    return fields;
}

/**
 * Render provider options
 */
function renderProviderOptions(category) {
    const providers = PROVIDER_CATEGORIES[category]?.providers || [];
    return providers.map(provider => `
        <option value="${provider.id}">${provider.name}</option>
    `).join('');
}

/**
 * Render provider options for a job
 */
function renderProviderOptionsForJob(category, selectedProvider) {
    const providers = PROVIDER_CATEGORIES[category]?.providers || [];
    return providers.map(provider => `
        <option value="${provider.id}" ${provider.id === selectedProvider ? 'selected' : ''}>${provider.name}</option>
    `).join('');
}

/**
 * Add a new job to the builder
 */
function addOracleJob() {
    oracleState.builderState.jobs.push({
        name: '',
        providerCategory: 'blockchain',
        provider: 'EthereumOASIS',
        tasks: [{
            type: 'fetch',
            queryType: 'smartContract',
            contractAddress: '',
            function: '',
            parameters: ''
        }]
    });
    refreshOracleBuilder();
}

/**
 * Remove a job
 */
function removeOracleJob(jobIndex) {
    oracleState.builderState.jobs.splice(jobIndex, 1);
    refreshOracleBuilder();
}

/**
 * Move a job up or down
 */
function moveJob(jobIndex, direction) {
    if (direction < 0 && jobIndex === 0) return;
    if (direction > 0 && jobIndex === oracleState.builderState.jobs.length - 1) return;
    
    const newIndex = jobIndex + direction;
    const job = oracleState.builderState.jobs.splice(jobIndex, 1)[0];
    oracleState.builderState.jobs.splice(newIndex, 0, job);
    refreshOracleBuilder();
}

/**
 * Update job name
 */
function updateJobName(jobIndex, name) {
    if (oracleState.builderState.jobs[jobIndex]) {
        oracleState.builderState.jobs[jobIndex].name = name;
    }
}

/**
 * Update job provider category
 */
function updateJobProviderOptions(jobIndex) {
    const select = event.target;
    const category = select.value;
    if (oracleState.builderState.jobs[jobIndex]) {
        oracleState.builderState.jobs[jobIndex].providerCategory = category;
        const firstProvider = PROVIDER_CATEGORIES[category]?.providers?.[0]?.id;
        if (firstProvider) {
            oracleState.builderState.jobs[jobIndex].provider = firstProvider;
        }
        refreshOracleBuilder();
    }
}

/**
 * Update job provider
 */
function updateJobProvider(jobIndex, provider) {
    if (oracleState.builderState.jobs[jobIndex]) {
        oracleState.builderState.jobs[jobIndex].provider = provider;
    }
}

/**
 * Add task to job
 */
function addTaskToJob(jobIndex, taskType = null) {
    if (!oracleState.builderState.jobs[jobIndex]) return;
    if (!oracleState.builderState.jobs[jobIndex].tasks) {
        oracleState.builderState.jobs[jobIndex].tasks = [];
    }
    
    // If taskType not provided, show selection UI
    if (!taskType) {
        showTaskTypeSelector(jobIndex);
        return;
    }
    
    const newTask = {
        type: taskType
    };
    
    if (taskType === 'fetch') {
        newTask.queryType = 'smartContract';
    } else if (taskType === 'parse') {
        newTask.jsonPath = '';
    } else if (taskType === 'transform') {
        newTask.transform = '';
    } else if (taskType === 'aggregate') {
        newTask.aggregateMethod = 'average';
    }
    
    oracleState.builderState.jobs[jobIndex].tasks.push(newTask);
    refreshOracleBuilder();
}

/**
 * Show task type selector
 */
function showTaskTypeSelector(jobIndex, insertIndex = null) {
    const container = document.getElementById('oracle-content');
    if (!container) return;

    const modal = document.createElement('div');
    modal.className = 'oracle-task-selector-modal';
    modal.innerHTML = `
        <div class="oracle-task-selector-overlay" onclick="closeTaskTypeSelector()"></div>
        <div class="oracle-task-selector-content">
            <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 1.5rem;">
                <h3 style="margin: 0; font-size: 1.125rem; font-weight: 500;">Select Task Type</h3>
                <button class="btn-text" onclick="closeTaskTypeSelector()" style="font-size: 1.25rem; padding: 0.25rem;">×</button>
            </div>
            <div class="oracle-task-types-grid">
                ${Object.keys(TASK_TYPES).map(type => {
                    const taskType = TASK_TYPES[type];
                    return `
                        <button 
                            class="oracle-task-type-card" 
                            onclick="selectTaskType(${jobIndex}, '${type}', ${insertIndex !== null ? insertIndex : 'null'})"
                        >
                            <div style="font-size: 1.5rem; margin-bottom: 0.5rem;">${taskType.icon}</div>
                            <div style="font-weight: 500; margin-bottom: 0.25rem;">${taskType.name}</div>
                            <div style="font-size: 0.75rem; color: var(--text-secondary);">${taskType.description}</div>
                        </button>
                    `;
                }).join('')}
            </div>
        </div>
    `;
    document.body.appendChild(modal);
}

/**
 * Select task type
 */
function selectTaskType(jobIndex, taskType, insertIndex) {
    closeTaskTypeSelector();
    if (insertIndex !== null && insertIndex !== undefined) {
        insertTaskAtPosition(jobIndex, taskType, insertIndex);
    } else {
        addTaskToJob(jobIndex, taskType);
    }
}

/**
 * Insert task at specific position
 */
function insertTaskAtPosition(jobIndex, taskType, position) {
    const job = oracleState.builderState.jobs[jobIndex];
    if (!job) return;
    if (!job.tasks) job.tasks = [];
    
    const newTask = {
        type: taskType
    };
    
    if (taskType === 'fetch') {
        newTask.queryType = 'smartContract';
    } else if (taskType === 'parse') {
        newTask.jsonPath = '';
    } else if (taskType === 'transform') {
        newTask.transform = '';
    } else if (taskType === 'aggregate') {
        newTask.aggregateMethod = 'average';
    }
    
    job.tasks.splice(position, 0, newTask);
    refreshOracleBuilder();
}

/**
 * Close task type selector
 */
function closeTaskTypeSelector() {
    const modal = document.querySelector('.oracle-task-selector-modal');
    if (modal) {
        modal.remove();
    }
}

/**
 * Remove task from job
 */
function removeTaskFromJob(jobIndex, taskIndex) {
    if (oracleState.builderState.jobs[jobIndex]?.tasks) {
        oracleState.builderState.jobs[jobIndex].tasks.splice(taskIndex, 1);
        refreshOracleBuilder();
    }
}

/**
 * Move task up or down
 */
function moveTask(jobIndex, taskIndex, direction) {
    const job = oracleState.builderState.jobs[jobIndex];
    if (!job || !job.tasks) return;
    
    if (direction < 0 && taskIndex === 0) return;
    if (direction > 0 && taskIndex === job.tasks.length - 1) return;
    
    const newIndex = taskIndex + direction;
    const task = job.tasks.splice(taskIndex, 1)[0];
    job.tasks.splice(newIndex, 0, task);
    refreshOracleBuilder();
}

/**
 * Update task field
 */
function updateTaskField(jobIndex, taskIndex, field, value) {
    const job = oracleState.builderState.jobs[jobIndex];
    if (job && job.tasks && job.tasks[taskIndex]) {
        job.tasks[taskIndex][field] = value;
    }
}

/**
 * Update task query type
 */
function updateTaskQueryType(jobIndex, taskIndex, queryType) {
    const job = oracleState.builderState.jobs[jobIndex];
    if (job && job.tasks && job.tasks[taskIndex]) {
        job.tasks[taskIndex].queryType = queryType;
        // Clear old query fields
        delete job.tasks[taskIndex].contractAddress;
        delete job.tasks[taskIndex].function;
        delete job.tasks[taskIndex].parameters;
        delete job.tasks[taskIndex].url;
        delete job.tasks[taskIndex].method;
        delete job.tasks[taskIndex].query;
        delete job.tasks[taskIndex].address;
        delete job.tasks[taskIndex].dataType;
        refreshOracleBuilder();
    }
}

/**
 * Refresh oracle builder UI
 */
function refreshOracleBuilder() {
    const container = document.getElementById('oracle-content');
    if (container && oracleState.view === 'builder') {
        renderOracleBuilder(container);
    }
}

/**
 * Simulate oracle feed
 */
async function simulateOracleFeed() {
    if (oracleState.builderState.jobs.length === 0) {
        alert('Please add at least one job before simulating');
        return;
    }

    oracleState.builderState.isSimulating = true;
    refreshOracleBuilder();

    try {
        // Build feed configuration
        const feedConfig = buildFeedConfigFromBuilder();
        
        // Simulate execution
        const simulationResult = {
            success: true,
            jobs: [],
            totalLatency: 0,
            timestamp: new Date().toISOString()
        };

        // Simulate each job
        for (let i = 0; i < feedConfig.jobs.length; i++) {
            const job = feedConfig.jobs[i];
            const jobResult = {
                jobIndex: i,
                jobName: job.name || `Job ${i + 1}`,
                success: true,
                latency: Math.random() * 200 + 50, // 50-250ms
                tasks: [],
                result: null
            };

            // Simulate task chain
            let taskResult = null;
            for (let j = 0; j < job.tasks.length; j++) {
                const task = job.tasks[j];
                const taskResultItem = {
                    taskIndex: j,
                    taskType: task.type,
                    success: true,
                    latency: Math.random() * 100 + 20, // 20-120ms
                    output: simulateTaskOutput(task, taskResult)
                };
                jobResult.tasks.push(taskResultItem);
                taskResult = taskResultItem.output;
            }

            jobResult.result = taskResult;
            simulationResult.jobs.push(jobResult);
            simulationResult.totalLatency += jobResult.latency;
        }

        oracleState.builderState.simulationResult = simulationResult;
    } catch (error) {
        console.error('Simulation error:', error);
        oracleState.builderState.simulationResult = {
            success: false,
            error: error.message,
            timestamp: new Date().toISOString()
        };
    } finally {
        oracleState.builderState.isSimulating = false;
        refreshOracleBuilder();
    }
}

/**
 * Simulate task output
 */
function simulateTaskOutput(task, previousOutput) {
    if (task.type === 'fetch') {
        if (task.queryType === 'api') {
            return { price: 3421.50, symbol: 'ETH', timestamp: Date.now() };
        } else if (task.queryType === 'smartContract') {
            return '1500000000000000000'; // 1.5 ETH in wei
        } else if (task.queryType === 'blockchain') {
            return '1.5';
        }
        return { data: 'mock data' };
    } else if (task.type === 'parse') {
        if (previousOutput && task.jsonPath) {
            // Simple JSONPath simulation
            if (task.jsonPath.includes('price')) return 3421.50;
            if (task.jsonPath.includes('symbol')) return 'ETH';
            return previousOutput;
        }
        return previousOutput;
    } else if (task.type === 'transform') {
        if (previousOutput && task.transform) {
            try {
                const value = typeof previousOutput === 'object' ? previousOutput.price || previousOutput : parseFloat(previousOutput);
                // Simple transform simulation
                if (task.transform.includes('*')) {
                    const multiplier = parseFloat(task.transform.match(/\* (\d+)/)?.[1] || '1');
                    return value * multiplier;
                }
                return value;
            } catch (e) {
                return previousOutput;
            }
        }
        return previousOutput;
    } else if (task.type === 'aggregate') {
        return 3421.50; // Simulated aggregate result
    }
    return previousOutput || 'mock result';
}

/**
 * Render simulation results
 */
function renderSimulationResults() {
    const result = oracleState.builderState.simulationResult;
    if (!result) return '';

    if (!result.success) {
        return `
            <div class="portal-section" style="margin-top: 1.5rem;">
                <div class="portal-card" style="border-color: rgba(239, 68, 68, 0.3);">
                    <h3 class="portal-card-title" style="color: rgba(239, 68, 68, 1);">Simulation Failed</h3>
                    <p style="color: var(--text-secondary);">${result.error || 'Unknown error'}</p>
                </div>
            </div>
        `;
    }

    return `
        <div class="portal-section" style="margin-top: 1.5rem;">
            <div class="portal-card">
                <div class="portal-card-header">
                    <h3 class="portal-card-title">Simulation Results</h3>
                    <span style="color: var(--text-secondary); font-size: 0.875rem;">
                        Total Latency: ${result.totalLatency.toFixed(0)}ms
                    </span>
                </div>
                <div class="oracle-simulation-results">
                    ${result.jobs.map((job, index) => `
                        <div class="oracle-simulation-job">
                            <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.75rem;">
                                <div>
                                    <strong>${job.jobName}</strong>
                                    <span style="color: var(--text-secondary); font-size: 0.875rem; margin-left: 0.5rem;">
                                        ${job.latency.toFixed(0)}ms
                                    </span>
                                </div>
                                <span class="oracle-status-badge ${job.success ? 'active' : 'inactive'}">
                                    ${job.success ? 'Success' : 'Failed'}
                                </span>
                            </div>
                            <div class="oracle-simulation-tasks">
                                ${job.tasks.map((task, taskIndex) => `
                                    <div class="oracle-simulation-task">
                                        <div style="display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.25rem;">
                                            <span style="color: var(--text-secondary);">${TASK_TYPES[task.taskType]?.icon || '•'}</span>
                                            <span style="font-size: 0.875rem;">${TASK_TYPES[task.taskType]?.name || task.taskType}</span>
                                            <span style="color: var(--text-tertiary); font-size: 0.75rem;">${task.latency.toFixed(0)}ms</span>
                                        </div>
                                        <code style="font-size: 0.75rem; color: var(--text-secondary); background: rgba(0, 0, 0, 0.3); padding: 0.25rem 0.5rem; border-radius: 0.25rem; display: block; margin-top: 0.25rem;">
                                            ${JSON.stringify(task.output)}
                                        </code>
                                    </div>
                                `).join('')}
                            </div>
                            <div style="margin-top: 0.75rem; padding-top: 0.75rem; border-top: 1px solid rgba(255, 255, 255, 0.1);">
                                <strong style="font-size: 0.875rem;">Final Result:</strong>
                                <code style="font-size: 0.875rem; color: var(--text-primary); background: rgba(0, 0, 0, 0.3); padding: 0.25rem 0.5rem; border-radius: 0.25rem; margin-left: 0.5rem;">
                                    ${JSON.stringify(job.result)}
                                </code>
                            </div>
                        </div>
                    `).join('')}
                </div>
            </div>
        </div>
    `;
}

/**
 * Clear simulation results
 */
function clearSimulation() {
    oracleState.builderState.simulationResult = null;
    refreshOracleBuilder();
}

/**
 * Build feed config from builder state
 */
function buildFeedConfigFromBuilder() {
    return {
        name: document.getElementById('oracle-feed-name')?.value || '',
        description: document.getElementById('oracle-feed-description')?.value || '',
        category: document.getElementById('oracle-feed-category')?.value || 'custom',
        visibility: document.getElementById('oracle-feed-visibility')?.value || 'private',
        jobs: oracleState.builderState.jobs.map(job => ({
            name: job.name,
            provider: job.provider,
            providerCategory: job.providerCategory,
            tasks: job.tasks || []
        })),
        monitoring: {
            frequency: document.getElementById('oracle-frequency')?.value || '1minute',
            trigger: document.getElementById('oracle-trigger')?.value || 'onChange',
            conditions: getTriggerConditions()
        },
        aggregation: {
            method: 'weightedAverage',
            consensusThreshold: 0.8
        }
    };
}

/**
 * Export feed as JSON
 */
function exportOracleFeedJSON() {
    const feedConfig = buildFeedConfigFromBuilder();
    const json = JSON.stringify(feedConfig, null, 2);
    const blob = new Blob([json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `oracle-feed-${Date.now()}.json`;
    a.click();
    URL.revokeObjectURL(url);
}

/**
 * Import feed from JSON
 */
function importOracleFeedJSON() {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.json';
    input.onchange = (e) => {
        const file = e.target.files[0];
        if (!file) return;
        
        const reader = new FileReader();
        reader.onload = (event) => {
            try {
                const feedConfig = JSON.parse(event.target.result);
                
                // Populate form fields
                if (feedConfig.name) {
                    const nameInput = document.getElementById('oracle-feed-name');
                    if (nameInput) nameInput.value = feedConfig.name;
                }
                if (feedConfig.description) {
                    const descInput = document.getElementById('oracle-feed-description');
                    if (descInput) descInput.value = feedConfig.description;
                }
                if (feedConfig.category) {
                    const catSelect = document.getElementById('oracle-feed-category');
                    if (catSelect) catSelect.value = feedConfig.category;
                }
                if (feedConfig.visibility) {
                    const visSelect = document.getElementById('oracle-feed-visibility');
                    if (visSelect) visSelect.value = feedConfig.visibility;
                }
                if (feedConfig.monitoring) {
                    if (feedConfig.monitoring.frequency) {
                        const freqSelect = document.getElementById('oracle-frequency');
                        if (freqSelect) freqSelect.value = feedConfig.monitoring.frequency;
                    }
                    if (feedConfig.monitoring.trigger) {
                        const triggerSelect = document.getElementById('oracle-trigger');
                        if (triggerSelect) triggerSelect.value = feedConfig.monitoring.trigger;
                    }
                }
                
                // Load jobs
                if (feedConfig.jobs && Array.isArray(feedConfig.jobs)) {
                    oracleState.builderState.jobs = feedConfig.jobs;
                } else if (feedConfig.sources && Array.isArray(feedConfig.sources)) {
                    // Convert old format to new format
                    oracleState.builderState.jobs = feedConfig.sources.map(source => ({
                        name: '',
                        provider: source.provider,
                        providerCategory: 'blockchain',
                        tasks: [{
                            type: 'fetch',
                            queryType: source.type,
                            ...source
                        }]
                    }));
                }
                
                refreshOracleBuilder();
                alert('Feed configuration imported successfully');
            } catch (error) {
                console.error('Import error:', error);
                alert('Failed to import feed configuration: ' + error.message);
            }
        };
        reader.readAsText(file);
    };
    input.click();
}

/**
 * Update provider options when category changes
 */
function updateProviderOptions() {
    const category = document.getElementById('oracle-provider-category').value;
    const providerSelect = document.getElementById('oracle-provider');
    providerSelect.innerHTML = renderProviderOptions(category);
}

/**
 * Render query fields based on query type
 */
function renderQueryFields(queryType) {
    const type = QUERY_TYPES[queryType];
    if (!type) return '';

    let fields = '';
    
    if (queryType === 'smartContract') {
        fields = `
            <div class="oracle-form-field">
                <label class="oracle-form-label">Contract Address</label>
                <input 
                    type="text" 
                    id="oracle-contract-address" 
                    class="oracle-form-input" 
                    placeholder="0x..."
                />
            </div>
            <div class="oracle-form-field">
                <label class="oracle-form-label">Function</label>
                <input 
                    type="text" 
                    id="oracle-function" 
                    class="oracle-form-input" 
                    placeholder="balanceOf"
                />
            </div>
            <div class="oracle-form-field">
                <label class="oracle-form-label">Parameters (JSON)</label>
                <textarea 
                    id="oracle-parameters" 
                    class="oracle-form-input oracle-form-textarea" 
                    placeholder='{"address": "0x..."}'
                    rows="3"
                ></textarea>
            </div>
        `;
    } else if (queryType === 'database') {
        fields = `
            <div class="oracle-form-field">
                <label class="oracle-form-label">Query</label>
                <textarea 
                    id="oracle-query" 
                    class="oracle-form-input oracle-form-textarea" 
                    placeholder="SELECT * FROM collection WHERE..."
                    rows="4"
                ></textarea>
            </div>
        `;
    } else if (queryType === 'api') {
        fields = `
            <div class="oracle-form-field">
                <label class="oracle-form-label">API URL</label>
                <input 
                    type="url" 
                    id="oracle-api-url" 
                    class="oracle-form-input" 
                    placeholder="https://api.example.com/data"
                />
            </div>
            <div class="oracle-form-field">
                <label class="oracle-form-label">HTTP Method</label>
                <select id="oracle-api-method" class="oracle-form-input">
                    <option value="GET" selected>GET</option>
                    <option value="POST">POST</option>
                    <option value="PUT">PUT</option>
                </select>
            </div>
        `;
    } else if (queryType === 'blockchain') {
        fields = `
            <div class="oracle-form-field">
                <label class="oracle-form-label">Address</label>
                <input 
                    type="text" 
                    id="oracle-address" 
                    class="oracle-form-input" 
                    placeholder="0x... or address"
                />
            </div>
            <div class="oracle-form-field">
                <label class="oracle-form-label">Data Type</label>
                <select id="oracle-data-type" class="oracle-form-input">
                    <option value="balance">Balance</option>
                    <option value="transactionCount">Transaction Count</option>
                    <option value="code">Contract Code</option>
                </select>
            </div>
        `;
    }

    return fields;
}

/**
 * Update query fields when query type changes
 */
function updateQueryFields() {
    const queryType = document.getElementById('oracle-query-type').value;
    const fieldsContainer = document.getElementById('oracle-query-fields');
    fieldsContainer.innerHTML = renderQueryFields(queryType);
}

/**
 * Handle create oracle feed
 */
async function handleCreateOracleFeed(event) {
    event.preventDefault();
    
    if (oracleState.isCreating) return;

    if (oracleState.builderState.jobs.length === 0) {
        alert('Please add at least one job before creating the feed');
        return;
    }

    const avatarId = getAvatarId();
    if (!avatarId) {
        alert('Please log in to create oracle feeds');
        return;
    }

    oracleState.isCreating = true;

    try {
        const feedData = buildFeedConfigFromBuilder();

        const response = await oasisAPI.createOracleFeed(feedData);
        
        if (response && !response.isError) {
            // Reset builder state
            oracleState.builderState.jobs = [];
            oracleState.builderState.simulationResult = null;
            await fetchOracleFeeds();
            loadOracle();
        } else {
            alert(response?.message || 'Failed to create oracle feed');
        }
    } catch (error) {
        console.error('Error creating oracle feed:', error);
        alert('Failed to create oracle feed. Please try again.');
    } finally {
        oracleState.isCreating = false;
    }
}

/**
 * Get query data based on query type
 */
function getQueryData() {
    const queryType = document.getElementById('oracle-query-type').value;
    const data = {};

    if (queryType === 'smartContract') {
        data.contractAddress = document.getElementById('oracle-contract-address').value;
        data.function = document.getElementById('oracle-function').value;
        const params = document.getElementById('oracle-parameters').value;
        if (params) {
            try {
                data.parameters = JSON.parse(params);
            } catch (e) {
                data.parameters = params;
            }
        }
    } else if (queryType === 'database') {
        data.query = document.getElementById('oracle-query').value;
    } else if (queryType === 'api') {
        data.url = document.getElementById('oracle-api-url').value;
        data.method = document.getElementById('oracle-api-method').value;
    } else if (queryType === 'blockchain') {
        data.address = document.getElementById('oracle-address').value;
        data.dataType = document.getElementById('oracle-data-type').value;
    }

    return data;
}

/**
 * Get trigger conditions
 */
function getTriggerConditions() {
    const trigger = document.getElementById('oracle-trigger').value;
    if (trigger === 'onThreshold') {
        return [{
            operator: document.getElementById('oracle-threshold-operator').value,
            value: parseFloat(document.getElementById('oracle-threshold-value').value)
        }];
    }
    return [];
}

/**
 * View oracle feed
 */
async function viewOracleFeed(feedId) {
    const container = document.getElementById('oracle-content');
    if (!container) return;

    oracleState.view = 'feed-detail';
    
    try {
        const response = await oasisAPI.getOracleFeed(feedId);
        if (response && !response.isError) {
            oracleState.selectedFeed = response.result;
            renderOracleFeedDetail(container);
        } else {
            alert('Failed to load oracle feed');
            loadOracle();
        }
    } catch (error) {
        console.error('Error loading oracle feed:', error);
        alert('Failed to load oracle feed');
        loadOracle();
    }
}

/**
 * Render oracle feed detail
 */
function renderOracleFeedDetail(container) {
    const feed = oracleState.selectedFeed;
    if (!feed) return;

    container.innerHTML = `
        <div class="portal-section">
            <div class="portal-section-header">
                <div>
                    <button class="btn-text" onclick="loadOracle()" style="margin-bottom: 0.5rem;">
                        ← Back to Feeds
                    </button>
                    <h2 class="portal-section-title">${feed.name || 'Unnamed Feed'}</h2>
                    <p class="portal-section-subtitle">${feed.description || 'No description'}</p>
                </div>
                <div>
                    <button class="btn-text" onclick="editOracleFeed('${feed.feedId || feed.id}')" style="margin-right: 1rem;">
                        Edit
                    </button>
                    <button class="btn-primary" onclick="deleteOracleFeed('${feed.feedId || feed.id}')">
                        Delete
                    </button>
                </div>
            </div>
        </div>

        <div class="portal-section">
            <div class="portal-card">
                <div class="portal-card-header">
                    <h3 class="portal-card-title">Current Value</h3>
                </div>
                <div class="oracle-feed-value-display">
                    ${feed.currentValue ? `
                        <div class="oracle-value-large">${formatValue(feed.currentValue)}</div>
                        <div class="oracle-value-timestamp">Last updated: ${formatDate(feed.currentValue.timestamp)}</div>
                    ` : `
                        <div class="empty-state">No data available yet</div>
                    `}
                </div>
            </div>
        </div>

        <div class="portal-section">
            <div class="portal-card">
                <div class="portal-card-header">
                    <h3 class="portal-card-title">Configuration</h3>
                </div>
                <div class="oracle-feed-config">
                    <div class="oracle-config-item">
                        <span class="oracle-config-label">Status:</span>
                        <span class="oracle-status-badge ${feed.status || 'inactive'}">${(feed.status || 'inactive').charAt(0).toUpperCase() + (feed.status || 'inactive').slice(1)}</span>
                    </div>
                    <div class="oracle-config-item">
                        <span class="oracle-config-label">Provider:</span>
                        <span class="oracle-config-value">${feed.sources?.[0]?.provider || 'Unknown'}</span>
                    </div>
                    <div class="oracle-config-item">
                        <span class="oracle-config-label">Frequency:</span>
                        <span class="oracle-config-value">${formatFrequency(feed.monitoring?.frequency)}</span>
                    </div>
                    <div class="oracle-config-item">
                        <span class="oracle-config-label">Trigger:</span>
                        <span class="oracle-config-value">${formatTrigger(feed.monitoring?.trigger)}</span>
                    </div>
                    <div class="oracle-config-item">
                        <span class="oracle-config-label">Visibility:</span>
                        <span class="oracle-config-value">${(feed.visibility || 'private').charAt(0).toUpperCase() + (feed.visibility || 'private').slice(1)}</span>
                    </div>
                    ${feed.apiEndpoint ? `
                        <div class="oracle-config-item">
                            <span class="oracle-config-label">API Endpoint:</span>
                            <code class="oracle-config-code">${feed.apiEndpoint}</code>
                        </div>
                    ` : ''}
                </div>
            </div>
        </div>
    `;
}

/**
 * Edit oracle feed
 */
function editOracleFeed(feedId) {
    // TODO: Implement edit functionality
    alert('Edit functionality coming soon');
}

/**
 * Delete oracle feed
 */
async function deleteOracleFeed(feedId) {
    if (!confirm('Are you sure you want to delete this oracle feed?')) {
        return;
    }

    try {
        const response = await oasisAPI.deleteOracleFeed(feedId);
        if (response && !response.isError) {
            await fetchOracleFeeds();
            if (oracleState.view === 'feed-detail') {
                loadOracle();
            } else {
                const container = document.getElementById('oracle-content');
                if (container) {
                    renderOracleDashboard(container);
                }
            }
        } else {
            alert('Failed to delete oracle feed');
        }
    } catch (error) {
        console.error('Error deleting oracle feed:', error);
        alert('Failed to delete oracle feed');
    }
}

/**
 * Render login prompt
 * DISABLED: Sign in is disabled, this should not be called
 */
function renderLoginPrompt(container) {
    // Sign in disabled - just show empty state
    container.innerHTML = `
        <div class="portal-section">
            <div class="portal-card">
                <div class="empty-state" style="padding: 4rem 2rem;">
                    <h3 style="margin-bottom: 1rem;">Oracle Feeds</h3>
                    <p style="color: var(--text-secondary); margin-bottom: 2rem;">
                        Create and manage custom oracle feeds
                    </p>
                </div>
            </div>
        </div>
    `;
}

/**
 * Format frequency
 */
function formatFrequency(frequency) {
    if (!frequency) return 'Not set';
    const map = {
        'realtime': 'Real-time',
        '1second': 'Every 1 second',
        '1minute': 'Every 1 minute',
        '5minutes': 'Every 5 minutes',
        '1hour': 'Every 1 hour',
        'daily': 'Daily'
    };
    return map[frequency] || frequency;
}

/**
 * Format trigger
 */
function formatTrigger(trigger) {
    if (!trigger) return 'Not set';
    const map = {
        'onChange': 'On Change',
        'onThreshold': 'On Threshold',
        'always': 'Always'
    };
    return map[trigger] || trigger;
}

/**
 * Format value
 */
function formatValue(value) {
    if (typeof value === 'object' && value.value !== undefined) {
        return value.value + (value.unit ? ' ' + value.unit : '');
    }
    return String(value);
}

/**
 * Format date
 */
function formatDate(dateString) {
    if (!dateString) return 'Unknown';
    const date = new Date(dateString);
    return date.toLocaleString();
}

// Export functions globally
window.loadOracle = loadOracle;
window.openOracleBuilder = openOracleBuilder;
window.viewOracleFeed = viewOracleFeed;
window.editOracleFeed = editOracleFeed;
window.deleteOracleFeed = deleteOracleFeed;
window.handleCreateOracleFeed = handleCreateOracleFeed;
window.updateProviderOptions = updateProviderOptions;
window.updateQueryFields = updateQueryFields;
window.addOracleJob = addOracleJob;
window.removeOracleJob = removeOracleJob;
window.moveJob = moveJob;
window.updateJobName = updateJobName;
window.updateJobProviderOptions = updateJobProviderOptions;
window.updateJobProvider = updateJobProvider;
window.addTaskToJob = addTaskToJob;
window.removeTaskFromJob = removeTaskFromJob;
window.moveTask = moveTask;
window.updateTaskField = updateTaskField;
window.selectTaskType = selectTaskType;
window.closeTaskTypeSelector = closeTaskTypeSelector;
window.updateTaskQueryType = updateTaskQueryType;
window.simulateOracleFeed = simulateOracleFeed;
window.clearSimulation = clearSimulation;
window.exportOracleFeedJSON = exportOracleFeedJSON;
window.importOracleFeedJSON = importOracleFeedJSON;
window.handleProviderDragStart = handleProviderDragStart;
window.handleCanvasDragOver = handleCanvasDragOver;
window.handleCanvasDragLeave = handleCanvasDragLeave;
window.handleCanvasDrop = handleCanvasDrop;
window.handleJobDragStart = handleJobDragStart;
window.handleJobDragEnd = handleJobDragEnd;
window.insertTaskAfter = insertTaskAfter;
window.insertTaskAtPosition = insertTaskAtPosition;
window.editJobVisual = editJobVisual;
window.addJobFromProvider = addJobFromProvider;
