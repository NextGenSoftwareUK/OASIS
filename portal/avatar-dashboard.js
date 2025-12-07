// Avatar Dashboard JavaScript
// Adapted from oasis-wallet-ui avatar dashboard

/**
 * Build avatar insights data structure
 */
function buildAvatarInsights(avatar) {
    const name = formatAvatarName(avatar);
    
    const stats = [
        {
            id: 'karma',
            label: 'Karma Score',
            value: avatar?.karma || 1280,
            sublabel: 'Across all integrations',
            trend: {
                direction: 'up',
                value: '+12 this week'
            }
        },
        {
            id: 'rides',
            label: 'TimoRides Trips',
            value: '248',
            sublabel: 'Safe driver streak: 12',
            trend: {
                direction: 'up',
                value: '+4 today'
            }
        },
        {
            id: 'messages',
            label: 'Community Impact',
            value: '3,420',
            sublabel: 'Telegram contributions',
            trend: {
                direction: 'up',
                value: '+320 this week'
            }
        },
        {
            id: 'rewards',
            label: 'Rewards Earned',
            value: '18',
            sublabel: 'NFTs + perks',
            trend: {
                direction: 'flat',
                value: '3 pending'
            }
        }
    ];

    const activities = [
        {
            id: 'act-1',
            source: 'TimoRides',
            title: 'Driver Genesis NFT unlocked',
            description: 'Completed 200 safe rides on Lagos corridor',
            timestamp: '2h ago',
            valueChange: '+1 NFT',
            providerType: 'SolanaOASIS'
        },
        {
            id: 'act-2',
            source: 'Telegram',
            title: 'Moderator kudos',
            description: 'Hosted Oasis Africa AMA with 1.2K attendees',
            timestamp: '8h ago',
            valueChange: '+45 karma',
            providerType: 'EthereumOASIS'
        },
        {
            id: 'act-3',
            source: 'Wallet',
            title: 'Bridge swap complete',
            description: 'Swapped 120 qUSDC → SOL via Universal Asset Bridge',
            timestamp: '1d ago',
            valueChange: '+0.4 SOL',
            providerType: 'PolygonOASIS'
        },
        {
            id: 'act-4',
            source: 'TimoRides',
            title: 'Peak-hour streak',
            description: '5-star rides during surge window',
            timestamp: '2d ago',
            valueChange: '+18 karma',
            providerType: 'SolanaOASIS'
        }
    ];

    const rewards = [
        {
            id: 'reward-1',
            title: 'Driver Genesis Badge',
            source: 'TimoRides',
            description: 'Commemorative NFT for early Lagos pilots',
            status: 'claimed',
            imageUrl: 'https://assets.coingecko.com/coins/images/325/large/Tether-logo.png',
            chain: 'SolanaOASIS'
        },
        {
            id: 'reward-2',
            title: 'Community Catalyst',
            source: 'Telegram',
            description: 'Special role for moderating 3+ AMAs',
            status: 'available',
            imageUrl: 'https://assets.coingecko.com/coins/images/6319/large/USDC.png',
            chain: 'EthereumOASIS'
        },
        {
            id: 'reward-3',
            title: 'Bridge Pioneer Pack',
            source: 'Universal Asset Bridge',
            description: 'Fee rebate for cross-chain swaps',
            status: 'locked',
            imageUrl: 'https://assets.coingecko.com/coins/images/12504/large/solana.png',
            chain: 'PolygonOASIS'
        }
    ];

    const missions = [
        {
            id: 'mission-1',
            title: 'Safe Driver Sprint',
            description: 'Complete 15 rides with 5⭐ ratings this week.',
            progress: 11,
            target: 15,
            rewardSummary: 'Earn "Quartz Driver" NFT + 120 karma',
            status: 'active'
        },
        {
            id: 'mission-2',
            title: 'Community Anchor',
            description: 'Host two Telegram office hours sessions.',
            progress: 2,
            target: 2,
            rewardSummary: 'Unlock exclusive merch drop',
            status: 'completed'
        },
        {
            id: 'mission-3',
            title: 'Bridge Pathfinder',
            description: 'Process $5k volume via Universal Asset Bridge.',
            progress: 3200,
            target: 5000,
            rewardSummary: '0.5% swap rebate + collectible',
            status: 'active'
        }
    ];

    return {
        greeting: `Welcome back, ${name}`,
        stats,
        activities,
        rewards,
        missions
    };
}

function formatAvatarName(avatar) {
    if (!avatar) return 'Explorer';
    if (avatar.firstName) {
        return `${avatar.firstName} ${avatar.lastName || ''}`.trim();
    }
    return avatar.username || 'Explorer';
}

/**
 * Render Avatar Dashboard Hero
 */
function renderAvatarDashboardHero(avatar, greeting) {
    const trustLabel = avatar?.trustLevel || 'gold';
    const initials = (avatar?.firstName?.[0] || '') + (avatar?.lastName?.[0] || '') || avatar?.username?.[0]?.toUpperCase() || 'A';
    const username = avatar?.username || 'avatar';
    const avatarId = avatar?.avatarId || avatar?.id || '';

    return `
        <section class="avatar-dashboard-hero">
            <div class="avatar-hero-content">
                <div class="avatar-hero-left">
                    <div class="avatar-hero-icon">${initials}</div>
                    <div class="avatar-hero-info">
                        <p class="avatar-hero-greeting">${greeting}</p>
                        <div class="avatar-hero-name">
                            @${username}
                            <button class="avatar-copy-btn" onclick="copyToClipboard('avatar-username')" title="Copy username">
                                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                                    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                                </svg>
                            </button>
                        </div>
                        <div class="avatar-hero-meta">
                            <span class="avatar-trust-badge">
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path>
                                </svg>
                                ${trustLabel} tier
                            </span>
                            ${avatar?.email ? `<span>${avatar.email}</span>` : ''}
                            ${avatarId ? `<button class="avatar-id-link" onclick="copyToClipboard('avatar-id-display')">${formatAddress(avatarId, 6)}</button>` : ''}
                        </div>
                    </div>
                </div>
                <div class="avatar-hero-actions">
                    <button class="btn-text avatar-action-btn" onclick="copyToClipboard('avatar-token')">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                            <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                        </svg>
                        Copy token
                    </button>
                    <button class="btn-primary avatar-action-btn">
                        Manage avatar
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <line x1="7" y1="17" x2="17" y2="7"></line>
                            <polyline points="7 7 17 7 17 17"></polyline>
                        </svg>
                    </button>
                    <button class="btn-text avatar-action-btn" onclick="handleLogout()">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
                            <polyline points="16 17 21 12 16 7"></polyline>
                            <line x1="21" y1="12" x2="9" y2="12"></line>
                        </svg>
                        Sign out
                    </button>
                </div>
            </div>
            <span id="avatar-username" style="display: none;">${username}</span>
            <span id="avatar-id-display" style="display: none;">${avatarId}</span>
            <span id="avatar-token" style="display: none;">${localStorage.getItem('oasis_auth') ? JSON.parse(localStorage.getItem('oasis_auth')).token : ''}</span>
        </section>
    `;
}

/**
 * Render Stats Grid
 */
function renderStatsGrid(stats) {
    const trendColors = {
        up: 'text-emerald-400',
        down: 'text-red-400',
        flat: 'text-gray-400'
    };

    return `
        <div class="avatar-stats-grid">
            ${stats.map(stat => `
                <div class="avatar-stat-card">
                    <p class="avatar-stat-label">${stat.label}</p>
                    <div class="avatar-stat-value">${stat.value}</div>
                    ${stat.sublabel ? `<p class="avatar-stat-sublabel">${stat.sublabel}</p>` : ''}
                    ${stat.trend ? `
                        <p class="avatar-stat-trend ${trendColors[stat.trend.direction]}">
                            ${stat.trend.value}
                        </p>
                    ` : ''}
                </div>
            `).join('')}
        </div>
    `;
}

/**
 * Render Activity Feed
 */
function renderActivityFeed(activities) {
    const providerLogos = {
        'SolanaOASIS': 'https://assets.coingecko.com/coins/images/4128/large/solana.png',
        'EthereumOASIS': 'https://assets.coingecko.com/coins/images/279/large/ethereum.png',
        'PolygonOASIS': 'https://assets.coingecko.com/coins/images/4713/large/matic-token-icon.png'
    };

    return `
        <div class="avatar-activity-feed">
            <div class="avatar-section-header">
                <div>
                    <p class="avatar-section-label">Recent activity</p>
                    <h2 class="avatar-section-title">Cross-network highlights</h2>
                </div>
                <button class="avatar-section-link">View all</button>
            </div>
            <div class="avatar-activity-list">
                ${activities.map(activity => {
                    const logoUrl = providerLogos[activity.providerType] || '';
                    return `
                        <div class="avatar-activity-item">
                            <div class="avatar-activity-icon">
                                ${logoUrl ? `<img src="${logoUrl}" alt="${activity.providerType}" />` : `<span>${activity.source[0]}</span>`}
                            </div>
                            <div class="avatar-activity-content">
                                <div class="avatar-activity-header">
                                    <p class="avatar-activity-title">${activity.title}</p>
                                    <span class="avatar-activity-time">• ${activity.timestamp}</span>
                                </div>
                                <p class="avatar-activity-description">${activity.description}</p>
                                <div class="avatar-activity-meta">
                                    <span class="avatar-activity-source">${activity.source}</span>
                                    ${activity.valueChange ? `<span class="avatar-activity-change">${activity.valueChange}</span>` : ''}
                                </div>
                            </div>
                        </div>
                    `;
                }).join('')}
            </div>
        </div>
    `;
}

/**
 * Render Missions Panel
 */
function renderMissionsPanel(missions) {
    return `
        <div class="avatar-missions-panel">
            <div class="avatar-section-header">
                <div>
                    <p class="avatar-section-label">Active quests</p>
                    <h2 class="avatar-section-title">Keep leveling up</h2>
                </div>
                <button class="avatar-section-link">View missions</button>
            </div>
            <div class="avatar-missions-list">
                ${missions.map(mission => {
                    const progressPct = Math.min(100, Math.round((mission.progress / mission.target) * 100));
                    const statusClass = mission.status === 'completed' ? 'text-emerald-400' : 
                                      mission.status === 'active' ? 'text-purple-300' : 'text-gray-500';
                    return `
                        <div class="avatar-mission-item">
                            <div class="avatar-mission-header">
                                <div>
                                    <p class="avatar-mission-title">${mission.title}</p>
                                    <p class="avatar-mission-description">${mission.description}</p>
                                </div>
                                <span class="avatar-mission-status ${statusClass}">${mission.status}</span>
                            </div>
                            <div class="avatar-mission-progress">
                                <div class="avatar-mission-progress-info">
                                    <span>${mission.progress}/${mission.target}</span>
                                    <span>${mission.rewardSummary}</span>
                                </div>
                                <div class="avatar-mission-progress-bar">
                                    <div class="avatar-mission-progress-fill" style="width: ${progressPct}%"></div>
                                </div>
                            </div>
                        </div>
                    `;
                }).join('')}
            </div>
        </div>
    `;
}

/**
 * Render Rewards Grid
 */
function renderRewardsGrid(rewards) {
    const statusStyles = {
        claimed: 'text-emerald-300 bg-emerald-500/10 border border-emerald-500/20',
        available: 'text-purple-300 bg-purple-500/10 border border-purple-500/20',
        locked: 'text-gray-400 bg-gray-700/40 border border-gray-600/50'
    };

    return `
        <div class="avatar-rewards-grid">
            <div class="avatar-section-header">
                <div>
                    <p class="avatar-section-label">Rewards vault</p>
                    <h2 class="avatar-section-title">NFTs & perks earned</h2>
                </div>
                <button class="avatar-section-link" onclick="switchTab('nfts'); return false;">Go to wallet</button>
            </div>
            <div class="avatar-rewards-list">
                ${rewards.map(reward => {
                    const statusClass = statusStyles[reward.status] || statusStyles.locked;
                    return `
                        <div class="avatar-reward-item">
                            <div class="avatar-reward-image">
                                <img src="${reward.imageUrl}" alt="${reward.title}" />
                            </div>
                            <div class="avatar-reward-info">
                                <div class="avatar-reward-header">
                                    <p class="avatar-reward-title">${reward.title}</p>
                                    <span class="avatar-reward-status ${statusClass}">${reward.status}</span>
                                </div>
                                <p class="avatar-reward-description">${reward.description}</p>
                                <p class="avatar-reward-source">Source: ${reward.source}</p>
                            </div>
                        </div>
                    `;
                }).join('')}
            </div>
        </div>
    `;
}

/**
 * Load and render avatar dashboard
 */
function loadAvatarDashboard() {
    const authData = localStorage.getItem('oasis_auth');
    if (!authData) {
        return;
    }

    try {
        const auth = JSON.parse(authData);
        const avatar = auth.avatar;
        
        if (!avatar) {
            return;
        }

        const insights = buildAvatarInsights(avatar);
        
        // Render dashboard components
        const dashboardContainer = document.getElementById('avatar-dashboard-content');
        if (dashboardContainer) {
            dashboardContainer.innerHTML = `
                ${renderAvatarDashboardHero(avatar, insights.greeting)}
                ${renderStatsGrid(insights.stats)}
                <div class="avatar-dashboard-grid">
                    <div class="avatar-dashboard-main">
                        ${renderActivityFeed(insights.activities)}
                    </div>
                    <div class="avatar-dashboard-sidebar">
                        ${renderMissionsPanel(insights.missions)}
                    </div>
                </div>
                ${renderRewardsGrid(insights.rewards)}
            `;
        }
    } catch (error) {
        console.error('Error loading avatar dashboard:', error);
    }
}

function handleLogout() {
    localStorage.removeItem('oasis_auth');
    window.location.reload();
}

function formatAddress(address, chars = 6) {
    if (!address) return '';
    if (address.length <= chars * 2) return address;
    return `${address.substring(0, chars)}...${address.substring(address.length - chars)}`;
}
