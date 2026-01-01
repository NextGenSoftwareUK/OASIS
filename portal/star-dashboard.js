/**
 * STAR Dashboard - Comprehensive STAR functionality interface
 */

const starDashboardState = {
    baseUrl: window.location.origin,
    authToken: null,
    avatarId: null,
    stats: {
        totalNFTs: 0,
        totalGeoNFTs: 0,
        activeQuests: 0,
        activeMissions: 0,
        publishedAssets: 0,
        karma: 0,
        xp: 0
    },
    recentActivity: [],
    assets: {
        nfts: [],
        geonfts: [],
        quests: [],
        missions: [],
        oapps: []
    }
};

/**
 * Initialize STAR Dashboard
 */
function initSTARDashboard() {
    const authData = localStorage.getItem('oasis_auth');
    if (authData) {
        try {
            const auth = JSON.parse(authData);
            starDashboardState.authToken = auth.token;
            starDashboardState.avatarId = auth.avatarId || auth.avatar?.id || auth.avatar?.avatarId;
        } catch (e) {
            console.error('Error parsing auth data:', e);
        }
    }
    
    loadSTARDashboard();
}

/**
 * Load STAR Dashboard content
 */
async function loadSTARDashboard() {
    const container = document.getElementById('star-dashboard-content');
    if (!container) return;

    container.innerHTML = renderSTARDashboard();

    // Load stats and assets
    await loadSTARStats();
    await loadSTARAssets();
    await loadRecentActivity();
}

/**
 * Render STAR Dashboard
 */
function renderSTARDashboard() {
    return `
        <div class="portal-section">
            <div style="margin-bottom: 2rem;">
                <h2 style="
                    font-size: 1.5rem;
                    font-weight: 300;
                    color: var(--text-primary);
                    margin-bottom: 0.5rem;
                ">STAR Dashboard</h2>
                <p style="
                    font-size: 0.875rem;
                    color: var(--text-secondary);
                ">Manage your NFTs, GeoNFTs, Quests, Missions, and OAPPs</p>
            </div>

            ${renderSTAROverview()}
            ${renderSTARQuickActions()}
            ${renderSTARAssetsSection()}
            ${renderRecentActivitySection()}
        </div>
    `;
}

/**
 * Render STAR Overview (Stats)
 */
function renderSTAROverview() {
    return `
        <div class="portal-section" style="margin-bottom: 2rem;">
            <h3 style="
                font-size: 1rem;
                font-weight: 500;
                color: var(--text-primary);
                margin-bottom: 1rem;
            ">Overview</h3>
            <div class="stats-grid">
                <div class="stat-card">
                    <div class="stat-label">Total NFTs</div>
                    <div class="stat-value" id="star-stat-nfts">${starDashboardState.stats.totalNFTs}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Total GeoNFTs</div>
                    <div class="stat-value" id="star-stat-geonfts">${starDashboardState.stats.totalGeoNFTs}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Active Quests</div>
                    <div class="stat-value" id="star-stat-quests">${starDashboardState.stats.activeQuests}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Active Missions</div>
                    <div class="stat-value" id="star-stat-missions">${starDashboardState.stats.activeMissions}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Published Assets</div>
                    <div class="stat-value" id="star-stat-published">${starDashboardState.stats.publishedAssets}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">Karma</div>
                    <div class="stat-value" id="star-stat-karma">${starDashboardState.stats.karma}</div>
                </div>
                <div class="stat-card">
                    <div class="stat-label">XP</div>
                    <div class="stat-value" id="star-stat-xp">${starDashboardState.stats.xp}</div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Render STAR Quick Actions
 */
function renderSTARQuickActions() {
    return `
        <div class="portal-section" style="margin-bottom: 2rem;">
            <h3 style="
                font-size: 1rem;
                font-weight: 500;
                color: var(--text-primary);
                margin-bottom: 1rem;
            ">Quick Actions</h3>
            <div style="
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
                gap: 1rem;
            ">
                <button
                    class="btn-secondary"
                    onclick="window.location.href='#nfts'; switchTab('nfts');"
                    style="padding: 1rem; text-align: left;"
                >
                    <div style="font-weight: 500; margin-bottom: 0.25rem;">Create NFT</div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary);">Mint a new NFT</div>
                </button>
                <button
                    class="btn-secondary"
                    onclick="if(typeof showGeoNFTPlacementInterface === 'function') { showGeoNFTPlacementInterface(); } else { console.error('showGeoNFTPlacementInterface not loaded'); }"
                    style="padding: 1rem; text-align: left;"
                >
                    <div style="font-weight: 500; margin-bottom: 0.25rem;">Place GeoNFT</div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary);">Place NFT on map</div>
                </button>
                <button
                    class="btn-secondary"
                    onclick="showQuestCreationInterface()"
                    style="padding: 1rem; text-align: left;"
                >
                    <div style="font-weight: 500; margin-bottom: 0.25rem;">Create Quest</div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary);">Design a new quest</div>
                </button>
                <button
                    class="btn-secondary"
                    onclick="showMissionCreationInterface()"
                    style="padding: 1rem; text-align: left;"
                >
                    <div style="font-weight: 500; margin-bottom: 0.25rem;">Create Mission</div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary);">Start a new mission</div>
                </button>
                <button
                    class="btn-secondary"
                    onclick="showQuestBrowser()"
                    style="padding: 1rem; text-align: left;"
                >
                    <div style="font-weight: 500; margin-bottom: 0.25rem;">Browse Quests</div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary);">View available quests</div>
                </button>
                <button
                    class="btn-secondary"
                    onclick="showMissionTracker()"
                    style="padding: 1rem; text-align: left;"
                >
                    <div style="font-weight: 500; margin-bottom: 0.25rem;">Mission Tracker</div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary);">Track your missions</div>
                </button>
            </div>
        </div>
    `;
}

/**
 * Render STAR Assets Section
 */
function renderSTARAssetsSection() {
    return `
        <div class="portal-section" style="margin-bottom: 2rem;">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">
                <h3 style="
                    font-size: 1rem;
                    font-weight: 500;
                    color: var(--text-primary);
                    margin: 0;
                ">My STAR Assets</h3>
                <div style="display: flex; gap: 0.5rem;">
                    <select id="star-assets-filter" onchange="filterSTARAssets()" class="form-select" style="font-size: 0.875rem;">
                        <option value="all">All Assets</option>
                        <option value="nfts">NFTs</option>
                        <option value="geonfts">GeoNFTs</option>
                        <option value="quests">Quests</option>
                        <option value="missions">Missions</option>
                        <option value="oapps">OAPPs</option>
                    </select>
                </div>
            </div>
            <div id="star-assets-grid" style="
                display: grid;
                grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
                gap: 1rem;
            ">
                ${renderSTARAssetsGrid()}
            </div>
        </div>
    `;
}

/**
 * Render STAR Assets Grid
 */
function renderSTARAssetsGrid() {
    const allAssets = [
        ...starDashboardState.assets.nfts.map(nft => ({ ...nft, type: 'nft' })),
        ...starDashboardState.assets.geonfts.map(geonft => ({ ...geonft, type: 'geonft' })),
        ...starDashboardState.assets.quests.map(quest => ({ ...quest, type: 'quest' })),
        ...starDashboardState.assets.missions.map(mission => ({ ...mission, type: 'mission' })),
        ...starDashboardState.assets.oapps.map(oapp => ({ ...oapp, type: 'oapp' }))
    ];

    if (allAssets.length === 0) {
        return `
            <div class="empty-state" style="grid-column: 1 / -1;">
                <p class="empty-state-text">No assets found. Create your first NFT, Quest, or Mission to get started.</p>
            </div>
        `;
    }

    return allAssets.map(asset => renderSTARAssetCard(asset)).join('');
}

/**
 * Render STAR Asset Card
 */
function renderSTARAssetCard(asset) {
    const typeLabels = {
        nft: 'NFT',
        geonft: 'GeoNFT',
        quest: 'Quest',
        mission: 'Mission',
        oapp: 'OAPP'
    };

    const typeColors = {
        nft: 'rgba(59, 130, 246, 0.1)',
        geonft: 'rgba(34, 197, 94, 0.1)',
        quest: 'rgba(168, 85, 247, 0.1)',
        mission: 'rgba(236, 72, 153, 0.1)',
        oapp: 'rgba(251, 146, 60, 0.1)'
    };

    return `
        <div class="portal-card" style="
            padding: 1rem;
            cursor: pointer;
            transition: transform 0.2s, box-shadow 0.2s;
            border-color: ${typeColors[asset.type] || 'var(--border-color)'};
        " 
        onclick="viewSTARAsset('${asset.type}', '${asset.id || ''}')"
        onmouseover="this.style.transform='translateY(-2px)'; this.style.boxShadow='0 8px 16px rgba(0,0,0,0.3)'"
        onmouseout="this.style.transform=''; this.style.boxShadow=''">
            <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 0.75rem;">
                <span style="
                    font-size: 0.75rem;
                    padding: 0.25rem 0.5rem;
                    border-radius: 4px;
                    background: ${typeColors[asset.type] || 'rgba(255,255,255,0.05)'};
                    color: var(--text-primary);
                    font-weight: 500;
                ">${typeLabels[asset.type] || 'Asset'}</span>
            </div>
            <div style="
                font-size: 0.875rem;
                font-weight: 500;
                color: var(--text-primary);
                margin-bottom: 0.5rem;
            ">${escapeHtml(asset.title || asset.name || 'Unnamed')}</div>
            <div style="
                font-size: 0.8125rem;
                color: var(--text-secondary);
                line-height: 1.5;
            ">${escapeHtml((asset.description || '').substring(0, 100))}${(asset.description || '').length > 100 ? '...' : ''}</div>
        </div>
    `;
}

/**
 * Render Recent Activity Section
 */
function renderRecentActivitySection() {
    return `
        <div class="portal-section">
            <h3 style="
                font-size: 1rem;
                font-weight: 500;
                color: var(--text-primary);
                margin-bottom: 1rem;
            ">Recent Activity</h3>
            <div id="star-recent-activity" style="display: flex; flex-direction: column; gap: 0.75rem;">
                ${starDashboardState.recentActivity.length === 0 
                    ? '<div class="empty-state"><p class="empty-state-text">No recent activity</p></div>'
                    : starDashboardState.recentActivity.map(activity => renderActivityItem(activity)).join('')
                }
            </div>
        </div>
    `;
}

/**
 * Render Activity Item
 */
function renderActivityItem(activity) {
    return `
        <div class="portal-card" style="padding: 0.75rem 1rem;">
            <div style="display: flex; align-items: center; gap: 1rem;">
                <div style="
                    width: 8px;
                    height: 8px;
                    border-radius: 50%;
                    background: ${activity.color || 'var(--text-tertiary)'};
                "></div>
                <div style="flex: 1;">
                    <div style="font-size: 0.875rem; color: var(--text-primary);">${escapeHtml(activity.message)}</div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-top: 0.25rem;">
                        ${formatTimeAgo(activity.timestamp)}
                    </div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Load STAR Stats
 */
async function loadSTARStats() {
    try {
        // Load NFT count
        const nftResponse = await fetch(`${starDashboardState.baseUrl}/api/nft/load-all-nfts-for_avatar/${starDashboardState.avatarId}`, {
            headers: {
                'Authorization': starDashboardState.authToken ? `Bearer ${starDashboardState.authToken}` : ''
            }
        });
        if (nftResponse.ok) {
            const nftData = await nftResponse.json();
            starDashboardState.stats.totalNFTs = nftData.result?.length || 0;
        }

        // Update UI
        updateSTARStatsDisplay();
    } catch (error) {
        console.error('Error loading STAR stats:', error);
    }
}

/**
 * Load STAR Assets
 */
async function loadSTARAssets() {
    try {
        // Load NFTs
        const nftResponse = await fetch(`${starDashboardState.baseUrl}/api/nft/load-all-nfts-for_avatar/${starDashboardState.avatarId}`, {
            headers: {
                'Authorization': starDashboardState.authToken ? `Bearer ${starDashboardState.authToken}` : ''
            }
        });
        if (nftResponse.ok) {
            const nftData = await nftResponse.json();
            starDashboardState.assets.nfts = (nftData.result || []).slice(0, 10); // Limit to 10 for display
        }

        // Update UI
        updateSTARAssetsDisplay();
    } catch (error) {
        console.error('Error loading STAR assets:', error);
    }
}

/**
 * Load Recent Activity
 */
async function loadRecentActivity() {
    // Placeholder - would load from activity log
    starDashboardState.recentActivity = [];
    updateRecentActivityDisplay();
}

/**
 * Update STAR Stats Display
 */
function updateSTARStatsDisplay() {
    const nftsEl = document.getElementById('star-stat-nfts');
    if (nftsEl) nftsEl.textContent = starDashboardState.stats.totalNFTs;
    const geonftsEl = document.getElementById('star-stat-geonfts');
    if (geonftsEl) geonftsEl.textContent = starDashboardState.stats.totalGeoNFTs;
    const questsEl = document.getElementById('star-stat-quests');
    if (questsEl) questsEl.textContent = starDashboardState.stats.activeQuests;
    const missionsEl = document.getElementById('star-stat-missions');
    if (missionsEl) missionsEl.textContent = starDashboardState.stats.activeMissions;
    const publishedEl = document.getElementById('star-stat-published');
    if (publishedEl) publishedEl.textContent = starDashboardState.stats.publishedAssets;
    const karmaEl = document.getElementById('star-stat-karma');
    if (karmaEl) karmaEl.textContent = starDashboardState.stats.karma;
    const xpEl = document.getElementById('star-stat-xp');
    if (xpEl) xpEl.textContent = starDashboardState.stats.xp;
}

/**
 * Update STAR Assets Display
 */
function updateSTARAssetsDisplay() {
    const container = document.getElementById('star-assets-grid');
    if (container) {
        container.innerHTML = renderSTARAssetsGrid();
    }
}

/**
 * Update Recent Activity Display
 */
function updateRecentActivityDisplay() {
    const container = document.getElementById('star-recent-activity');
    if (container) {
        container.innerHTML = starDashboardState.recentActivity.length === 0 
            ? '<div class="empty-state"><p class="empty-state-text">No recent activity</p></div>'
            : starDashboardState.recentActivity.map(activity => renderActivityItem(activity)).join('');
    }
}

/**
 * Filter STAR Assets
 */
function filterSTARAssets() {
    const filter = document.getElementById('star-assets-filter')?.value || 'all';
    // Filter logic would go here
    updateSTARAssetsDisplay();
}

/**
 * View STAR Asset
 */
function viewSTARAsset(type, id) {
    if (type === 'quest') {
        showQuestDetail(id);
    } else if (type === 'mission') {
        showMissionDetail(id);
    } else if (type === 'geonft') {
        showGeoNFTDetail(id);
    } else {
        // Navigate to appropriate section
        console.log(`View ${type} with id ${id}`);
    }
}

/**
 * Helper Functions
 */
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function formatTimeAgo(timestamp) {
    if (!timestamp) return '';
    const date = new Date(timestamp);
    const now = new Date();
    const diff = now - date;
    const seconds = Math.floor(diff / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (days > 0) return `${days} day${days > 1 ? 's' : ''} ago`;
    if (hours > 0) return `${hours} hour${hours > 1 ? 's' : ''} ago`;
    if (minutes > 0) return `${minutes} minute${minutes > 1 ? 's' : ''} ago`;
    return 'Just now';
}

// Export functions to window
if (typeof window !== 'undefined') {
    window.initSTARDashboard = initSTARDashboard;
    window.loadSTARDashboard = loadSTARDashboard;
    window.filterSTARAssets = filterSTARAssets;
    window.viewSTARAsset = viewSTARAsset;
    window.showGeoNFTPlacementInterface = showGeoNFTPlacementInterface;
    window.showQuestCreationInterface = showQuestCreationInterface;
    window.showMissionCreationInterface = showMissionCreationInterface;
    window.showQuestBrowser = showQuestBrowser;
    window.showMissionTracker = showMissionTracker;
    window.showQuestDetail = showQuestDetail;
    window.showMissionDetail = showMissionDetail;
    window.showGeoNFTDetail = showGeoNFTDetail;
}

