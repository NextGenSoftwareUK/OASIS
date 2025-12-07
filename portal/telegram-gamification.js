// Telegram Gamification UI JavaScript
// Handles all Telegram gamification display and interactions

const TELEGRAM_GROUP_URL = 'https://t.me/your_oasis_group'; // Update with actual group URL

// OASIS API client (should be defined in portal.html or separate file)
// For now, using a simple fetch wrapper
const oasisAPI = {
    baseUrl: 'https://api.oasisplatform.world',
    
    getToken() {
        const auth = localStorage.getItem('oasis_auth');
        if (auth) {
            const parsed = JSON.parse(auth);
            return parsed.token;
        }
        return null;
    },
    
    async request(endpoint, options = {}) {
        const url = endpoint.startsWith('http') ? endpoint : `${this.baseUrl}${endpoint}`;
        const headers = {
            'Content-Type': 'application/json',
            'Accept': 'application/json',
            ...options.headers
        };
        
        const token = this.getToken();
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }
        
        try {
            const response = await fetch(url, {
                ...options,
                headers
            });
            
            if (!response.ok) {
                throw new Error(`API error: ${response.status}`);
            }
            
            return await response.json();
        } catch (error) {
            console.error('API request failed:', error);
            return { isError: true, message: error.message };
        }
    }
};

/**
 * Load Telegram gamification data
 */
async function loadTelegramGamification() {
    const authData = localStorage.getItem('oasis_auth');
    if (!authData) {
        return;
    }

    try {
        const auth = JSON.parse(authData);
        const avatarId = auth.avatar?.id || auth.avatar?.avatarId;
        
        if (!avatarId) {
            return;
        }

        // Load all Telegram data in parallel
        const [stats, achievements, rewards, leaderboard] = await Promise.all([
            loadTelegramStats(avatarId),
            loadTelegramAchievements(avatarId),
            loadTelegramRewards(avatarId),
            loadTelegramLeaderboard()
        ]);

        // Render all components
        renderTelegramConnectionStatus(stats);
        renderTelegramStats(stats);
        renderRecentRewards(rewards);
        renderAchievementBadges(achievements);
        renderActivityTimeline(rewards);
        renderLeaderboard(leaderboard);
        renderRewardsBreakdown(rewards);
        renderNFTGallery(achievements);

    } catch (error) {
        console.error('Error loading Telegram gamification:', error);
        showTelegramError(error);
    }
}

/**
 * Load Telegram stats
 */
async function loadTelegramStats(avatarId) {
    try {
        const response = await oasisAPI.request(
            `/api/telegram/gamification/stats/${avatarId}`
        );
        
        if (response.isError) {
            return getDefaultStats();
        }
        
        return response.result || getDefaultStats();
    } catch (error) {
        console.error('Error loading Telegram stats:', error);
        return getDefaultStats();
    }
}

function getDefaultStats() {
    return {
        telegramLinked: false,
        telegramUsername: null,
        totalKarmaEarned: 0,
        totalTokensEarned: 0,
        nftsEarned: 0,
        achievementsCompleted: 0,
        achievementsActive: 0,
        groupsJoined: 0,
        dailyStreak: 0,
        weeklyActive: false
    };
}

/**
 * Load Telegram achievements
 */
async function loadTelegramAchievements(avatarId) {
    try {
        const response = await oasisAPI.request(
            `/api/telegram/achievements/user/${avatarId}`
        );
        
        return response.result || [];
    } catch (error) {
        console.error('Error loading achievements:', error);
        return [];
    }
}

/**
 * Load Telegram rewards
 */
async function loadTelegramRewards(avatarId) {
    try {
        const response = await oasisAPI.request(
            `/api/telegram/gamification/rewards/${avatarId}?limit=50`
        );
        
        return response.result?.rewards || [];
    } catch (error) {
        console.error('Error loading rewards:', error);
        return [];
    }
}

/**
 * Load leaderboard
 */
async function loadTelegramLeaderboard(period = 'weekly') {
    try {
        const response = await oasisAPI.request(
            `/api/telegram/gamification/leaderboard?period=${period}`
        );
        
        return response.result?.leaderboard || [];
    } catch (error) {
        console.error('Error loading leaderboard:', error);
        return [];
    }
}

/**
 * Render connection status banner
 */
function renderTelegramConnectionStatus(stats) {
    const container = document.getElementById('telegram-connection-status');
    if (!container) return;

    if (!stats.telegramLinked) {
        container.innerHTML = `
            <div class="telegram-connection-banner not-linked">
                <div class="telegram-connection-content">
                    <div class="telegram-connection-icon">📱</div>
                    <div class="telegram-connection-info">
                        <h3>Connect Your Telegram Account</h3>
                        <p>Link your Telegram account to start earning rewards for promoting OASIS!</p>
                    </div>
                    <button class="btn-primary" onclick="linkTelegramAccount()">
                        Link Telegram Account
                    </button>
                </div>
            </div>
        `;
    } else {
        container.innerHTML = `
            <div class="telegram-connection-banner linked">
                <div class="telegram-connection-content">
                    <div class="telegram-connection-icon">✅</div>
                    <div class="telegram-connection-info">
                        <h3>Telegram Connected</h3>
                        <p>Linked as <strong>@${stats.telegramUsername}</strong></p>
                    </div>
                    <div class="telegram-connection-actions">
                        <a href="${TELEGRAM_GROUP_URL}" target="_blank" class="btn-primary">
                            Join Telegram Group
                        </a>
                        <button class="btn-text" onclick="disconnectTelegram()">
                            Disconnect
                        </button>
                    </div>
                </div>
            </div>
        `;
    }
}

/**
 * Render Telegram stats grid
 */
function renderTelegramStats(stats) {
    const container = document.getElementById('telegram-stats');
    if (!container) return;

    const statsData = [
        {
            id: 'karma',
            label: 'Karma Earned',
            value: formatNumber(stats.totalKarmaEarned),
            sublabel: 'From Telegram activities',
            icon: '⭐',
            color: 'text-yellow-400'
        },
        {
            id: 'tokens',
            label: 'Tokens Earned',
            value: formatNumber(stats.totalTokensEarned, 1),
            sublabel: 'Token rewards',
            icon: '🪙',
            color: 'text-green-400'
        },
        {
            id: 'nfts',
            label: 'NFTs Earned',
            value: stats.nftsEarned,
            sublabel: 'Achievement NFTs',
            icon: '🎨',
            color: 'text-purple-400'
        },
        {
            id: 'streak',
            label: 'Daily Streak',
            value: `${stats.dailyStreak} days`,
            sublabel: stats.dailyStreak > 0 ? '🔥 Keep it up!' : 'Start your streak',
            icon: '🔥',
            color: 'text-orange-400'
        },
        {
            id: 'achievements',
            label: 'Achievements',
            value: `${stats.achievementsCompleted}/${stats.achievementsCompleted + stats.achievementsActive}`,
            sublabel: `${stats.achievementsActive} in progress`,
            icon: '🏆',
            color: 'text-blue-400'
        },
        {
            id: 'groups',
            label: 'Groups Joined',
            value: stats.groupsJoined,
            sublabel: 'Telegram groups',
            icon: '👥',
            color: 'text-cyan-400'
        }
    ];

    container.innerHTML = `
        <div class="telegram-stats-grid">
            ${statsData.map(stat => `
                <div class="telegram-stat-card">
                    <div class="telegram-stat-header">
                        <span class="telegram-stat-icon">${stat.icon}</span>
                        <p class="telegram-stat-label">${stat.label}</p>
                    </div>
                    <div class="telegram-stat-value ${stat.color}">${stat.value}</div>
                    <p class="telegram-stat-sublabel">${stat.sublabel}</p>
                </div>
            `).join('')}
        </div>
    `;
}

/**
 * Render recent rewards feed
 */
function renderRecentRewards(rewards) {
    const container = document.getElementById('telegram-recent-rewards');
    if (!container) return;

    // Get most recent 10 rewards
    const recent = rewards
        .sort((a, b) => new Date(b.timestamp) - new Date(a.timestamp))
        .slice(0, 10);

    if (recent.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <p class="empty-state-text">No rewards yet. Start engaging in Telegram to earn rewards!</p>
            </div>
        `;
        return;
    }

    container.innerHTML = `
        <div class="telegram-rewards-feed">
            ${recent.map(reward => `
                <div class="telegram-reward-item">
                    <div class="telegram-reward-icon">${getRewardIcon(reward.type)}</div>
                    <div class="telegram-reward-content">
                        <div class="telegram-reward-header">
                            <p class="telegram-reward-title">${getRewardTitle(reward.type)}</p>
                            <span class="telegram-reward-time">${formatTimeAgo(reward.timestamp)}</span>
                        </div>
                        <p class="telegram-reward-description">${reward.description || 'Telegram activity'}</p>
                        <div class="telegram-reward-amounts">
                            ${reward.karmaAwarded > 0 ? `<span class="reward-karma">+${reward.karmaAwarded} karma</span>` : ''}
                            ${reward.tokensAwarded > 0 ? `<span class="reward-tokens">+${reward.tokensAwarded} tokens</span>` : ''}
                            ${reward.nftAwarded ? `<span class="reward-nft">+1 NFT</span>` : ''}
                        </div>
                    </div>
                </div>
            `).join('')}
        </div>
    `;
}

/**
 * Render achievement badges
 */
function renderAchievementBadges(achievements) {
    const container = document.getElementById('telegram-achievement-badges');
    if (!container) return;

    // Group achievements by tier
    const achievementTemplates = getAchievementTemplates();
    const userAchievements = new Map();
    
    achievements.forEach(achievement => {
        const template = achievementTemplates.find(t => t.id === achievement.type);
        if (template) {
            userAchievements.set(achievement.type, {
                ...template,
                progress: achievement.progress || 0,
                target: achievement.target || template.target,
                status: achievement.status || 'in_progress',
                completed: achievement.status === 'Completed'
            });
        }
    });

    // Add locked achievements (not yet started)
    achievementTemplates.forEach(template => {
        if (!userAchievements.has(template.id)) {
            userAchievements.set(template.id, {
                ...template,
                progress: 0,
                status: 'locked',
                completed: false
            });
        }
    });

    const achievementsArray = Array.from(userAchievements.values())
        .sort((a, b) => {
            // Sort: completed first, then by tier, then by progress
            if (a.completed !== b.completed) return a.completed ? -1 : 1;
            if (a.tier !== b.tier) return a.tier.localeCompare(b.tier);
            return b.progress - a.progress;
        });

    container.innerHTML = `
        <div class="telegram-achievements-grid">
            ${achievementsArray.map(achievement => {
                const progressPct = achievement.target > 0 
                    ? Math.min(100, (achievement.progress / achievement.target) * 100)
                    : 0;
                const statusClass = achievement.status === 'completed' ? 'completed' :
                                  achievement.status === 'locked' ? 'locked' : 'in-progress';
                
                return `
                    <div class="telegram-achievement-badge ${statusClass} tier-${achievement.tier}">
                        <div class="achievement-badge-icon">${achievement.icon}</div>
                        <div class="achievement-badge-content">
                            <p class="achievement-badge-title">${achievement.name}</p>
                            <p class="achievement-badge-description">${achievement.description}</p>
                            ${achievement.status !== 'locked' ? `
                                <div class="achievement-badge-progress">
                                    <div class="achievement-progress-bar">
                                        <div class="achievement-progress-fill" style="width: ${progressPct}%"></div>
                                    </div>
                                    <div class="achievement-progress-text">
                                        ${achievement.progress}/${achievement.target}
                                    </div>
                                </div>
                            ` : ''}
                            <div class="achievement-badge-rewards">
                                ${achievement.karmaReward > 0 ? `<span>+${achievement.karmaReward} karma</span>` : ''}
                                ${achievement.tokenReward > 0 ? `<span>+${achievement.tokenReward} tokens</span>` : ''}
                                ${achievement.nftReward ? `<span>+1 NFT</span>` : ''}
                            </div>
                        </div>
                    </div>
                `;
            }).join('')}
        </div>
    `;
}

/**
 * Render activity timeline
 */
function renderActivityTimeline(rewards) {
    const container = document.getElementById('telegram-activity-timeline');
    if (!container) return;

    // Group rewards by date
    const grouped = groupRewardsByDate(rewards);

    if (Object.keys(grouped).length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <p class="empty-state-text">No activity yet</p>
            </div>
        `;
        return;
    }

    container.innerHTML = `
        <div class="telegram-activity-timeline">
            <div class="timeline-filters">
                <button class="filter-btn active" data-filter="all">All</button>
                <button class="filter-btn" data-filter="karma">Karma</button>
                <button class="filter-btn" data-filter="tokens">Tokens</button>
                <button class="filter-btn" data-filter="nft">NFTs</button>
            </div>
            <div class="timeline-content">
                ${Object.entries(grouped).map(([date, dateRewards]) => `
                    <div class="timeline-day" data-date="${date}">
                        <div class="timeline-day-header">
                            <span class="timeline-date">${formatDate(date)}</span>
                            <span class="timeline-count">${dateRewards.length} activities</span>
                        </div>
                        <div class="timeline-items">
                            ${dateRewards.map(reward => `
                                <div class="timeline-item" data-type="${reward.type}">
                                    <div class="timeline-item-icon">${getRewardIcon(reward.type)}</div>
                                    <div class="timeline-item-content">
                                        <p class="timeline-item-title">${getRewardTitle(reward.type)}</p>
                                        <p class="timeline-item-description">${reward.description || ''}</p>
                                        <div class="timeline-item-rewards">
                                            ${reward.karmaAwarded > 0 ? `<span>+${reward.karmaAwarded} karma</span>` : ''}
                                            ${reward.tokensAwarded > 0 ? `<span>+${reward.tokensAwarded} tokens</span>` : ''}
                                            ${reward.nftAwarded ? `<span>+1 NFT</span>` : ''}
                                        </div>
                                    </div>
                                    <div class="timeline-item-time">${formatTime(reward.timestamp)}</div>
                                </div>
                            `).join('')}
                        </div>
                    </div>
                `).join('')}
            </div>
        </div>
    `;

    // Add filter functionality
    setupTimelineFilters();
}

/**
 * Render leaderboard
 */
function renderLeaderboard(leaderboard) {
    const container = document.getElementById('telegram-leaderboard');
    if (!container) return;

    if (leaderboard.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <p class="empty-state-text">No leaderboard data yet</p>
            </div>
        `;
        return;
    }

    container.innerHTML = `
        <div class="telegram-leaderboard">
            <div class="leaderboard-header">
                <h3>Top Performers</h3>
                <select class="leaderboard-period" onchange="loadLeaderboardPeriod(this.value)">
                    <option value="daily">Today</option>
                    <option value="weekly" selected>This Week</option>
                    <option value="monthly">This Month</option>
                    <option value="alltime">All Time</option>
                </select>
            </div>
            <div class="leaderboard-list">
                ${leaderboard.map((entry, index) => {
                    const medal = index === 0 ? '🥇' : index === 1 ? '🥈' : index === 2 ? '🥉' : `${index + 1}.`;
                    return `
                        <div class="leaderboard-entry ${index < 3 ? 'top-three' : ''}">
                            <div class="leaderboard-rank">${medal}</div>
                            <div class="leaderboard-user">
                                <div class="leaderboard-avatar">${entry.username?.[0]?.toUpperCase() || 'U'}</div>
                                <div class="leaderboard-user-info">
                                    <p class="leaderboard-username">@${entry.username || 'user'}</p>
                                    <p class="leaderboard-achievements">${entry.achievements} achievements</p>
                                </div>
                            </div>
                            <div class="leaderboard-stats">
                                <div class="leaderboard-stat">
                                    <span class="stat-label">Karma</span>
                                    <span class="stat-value">${formatNumber(entry.karma)}</span>
                                </div>
                                <div class="leaderboard-stat">
                                    <span class="stat-label">Tokens</span>
                                    <span class="stat-value">${formatNumber(entry.tokens, 1)}</span>
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
 * Render rewards breakdown
 */
function renderRewardsBreakdown(rewards) {
    const container = document.getElementById('telegram-rewards-breakdown');
    if (!container) return;

    // Calculate breakdown by category
    const categories = {
        'Content Creation': { karma: 0, tokens: 0, count: 0 },
        'Community Engagement': { karma: 0, tokens: 0, count: 0 },
        'Marketing & Growth': { karma: 0, tokens: 0, count: 0 },
        'Technical': { karma: 0, tokens: 0, count: 0 }
    };

    rewards.forEach(reward => {
        const category = getRewardCategory(reward.type);
        if (categories[category]) {
            categories[category].karma += reward.karmaAwarded || 0;
            categories[category].tokens += reward.tokensAwarded || 0;
            categories[category].count += 1;
        }
    });

    const totalKarma = Object.values(categories).reduce((sum, cat) => sum + cat.karma, 0);
    const totalTokens = Object.values(categories).reduce((sum, cat) => sum + cat.tokens, 0);

    container.innerHTML = `
        <div class="telegram-rewards-breakdown">
            <h3>Rewards by Category</h3>
            <div class="breakdown-list">
                ${Object.entries(categories).map(([name, data]) => {
                    const karmaPct = totalKarma > 0 ? (data.karma / totalKarma * 100).toFixed(0) : 0;
                    const tokensPct = totalTokens > 0 ? (data.tokens / totalTokens * 100).toFixed(0) : 0;
                    
                    return `
                        <div class="breakdown-item">
                            <div class="breakdown-header">
                                <span class="breakdown-name">${name}</span>
                                <span class="breakdown-count">${data.count} activities</span>
                            </div>
                            <div class="breakdown-stats">
                                <div class="breakdown-stat">
                                    <span class="breakdown-label">Karma</span>
                                    <span class="breakdown-value">${formatNumber(data.karma)} (${karmaPct}%)</span>
                                </div>
                                <div class="breakdown-stat">
                                    <span class="breakdown-label">Tokens</span>
                                    <span class="breakdown-value">${formatNumber(data.tokens, 1)} (${tokensPct}%)</span>
                                </div>
                            </div>
                            <div class="breakdown-bar">
                                <div class="breakdown-bar-fill" style="width: ${karmaPct}%"></div>
                            </div>
                        </div>
                    `;
                }).join('')}
            </div>
        </div>
    `;
}

/**
 * Render NFT gallery
 */
function renderNFTGallery(achievements) {
    const container = document.getElementById('telegram-nft-gallery');
    if (!container) return;

    // Filter achievements that awarded NFTs
    const nftAchievements = achievements.filter(a => 
        a.status === 'Completed' && a.nftReward
    );

    if (nftAchievements.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <p class="empty-state-text">No NFTs earned yet. Complete achievements to earn NFTs!</p>
            </div>
        `;
        return;
    }

    container.innerHTML = `
        <div class="telegram-nft-gallery">
            <div class="nft-gallery-filters">
                <button class="filter-btn active" data-tier="all">All</button>
                <button class="filter-btn" data-tier="bronze">🥉 Bronze</button>
                <button class="filter-btn" data-tier="silver">🥈 Silver</button>
                <button class="filter-btn" data-tier="gold">🥇 Gold</button>
                <button class="filter-btn" data-tier="platinum">💎 Platinum</button>
            </div>
            <div class="nft-gallery-grid">
                ${nftAchievements.map(achievement => {
                    const tier = getAchievementTier(achievement.type);
                    return `
                        <div class="nft-gallery-card tier-${tier}" data-tier="${tier}">
                            <div class="nft-card-image">
                                <img src="${achievement.nftImageUrl || getDefaultNFTImage(tier)}" 
                                     alt="${achievement.nftTitle || achievement.type}" />
                                <div class="nft-tier-badge tier-${tier}">${getTierIcon(tier)}</div>
                            </div>
                            <div class="nft-card-info">
                                <p class="nft-card-title">${achievement.nftTitle || achievement.type}</p>
                                <p class="nft-card-description">${achievement.description || ''}</p>
                                <p class="nft-card-date">Earned ${formatDate(achievement.completedAt)}</p>
                                ${achievement.nftTokenAddress ? `
                                    <a href="https://explorer.solana.com/address/${achievement.nftTokenAddress}" 
                                       target="_blank" 
                                       class="nft-view-link">
                                        View on Solana
                                    </a>
                                ` : ''}
                            </div>
                        </div>
                    `;
                }).join('')}
            </div>
        </div>
    `;

    // Add filter functionality
    setupNFTFilters();
}

/**
 * Helper functions
 */
function getAchievementTemplates() {
    return [
        {
            id: 'mention',
            name: 'OASIS Mentioner',
            description: 'Mention OASIS 10 times',
            tier: 'bronze',
            icon: '💬',
            target: 10,
            karmaReward: 50,
            tokenReward: 0,
            nftReward: true
        },
        {
            id: 'link_share',
            name: 'Link Master',
            description: 'Share OASIS links 5 times',
            tier: 'bronze',
            icon: '🔗',
            target: 5,
            karmaReward: 30,
            tokenReward: 0.5,
            nftReward: false
        },
        {
            id: 'quality_post',
            name: 'Quality Contributor',
            description: 'Create 10 quality posts',
            tier: 'silver',
            icon: '✍️',
            target: 10,
            karmaReward: 100,
            tokenReward: 5,
            nftReward: true
        },
        {
            id: 'helpful_answer',
            name: 'Community Helper',
            description: 'Answer 20 questions',
            tier: 'silver',
            icon: '💡',
            target: 20,
            karmaReward: 75,
            tokenReward: 3,
            nftReward: true
        },
        {
            id: 'code_example',
            name: 'Code Contributor',
            description: 'Share 5 code examples',
            tier: 'gold',
            icon: '💻',
            target: 5,
            karmaReward: 150,
            tokenReward: 10,
            nftReward: true
        },
        {
            id: 'tutorial',
            name: 'Tutorial Creator',
            description: 'Create a tutorial',
            tier: 'gold',
            icon: '📚',
            target: 1,
            karmaReward: 200,
            tokenReward: 15,
            nftReward: true
        },
        {
            id: 'viral',
            name: 'Viral Creator',
            description: 'Create viral content (100+ reactions)',
            tier: 'gold',
            icon: '🔥',
            target: 1,
            karmaReward: 250,
            tokenReward: 20,
            nftReward: true
        },
        {
            id: 'invite',
            name: 'Community Builder',
            description: 'Invite 10 members',
            tier: 'platinum',
            icon: '👥',
            target: 10,
            karmaReward: 300,
            tokenReward: 25,
            nftReward: true
        },
        {
            id: 'moderator',
            name: 'Moderator',
            description: 'Become group moderator',
            tier: 'platinum',
            icon: '🛡️',
            target: 1,
            karmaReward: 500,
            tokenReward: 50,
            nftReward: true
        }
    ];
}

function getRewardIcon(type) {
    const icons = {
        'mention': '💬',
        'link_share': '🔗',
        'quality_post': '✍️',
        'helpful_answer': '💡',
        'code_example': '💻',
        'tutorial': '📚',
        'viral': '🔥',
        'invite': '👥',
        'daily_active': '📅',
        'weekly_active': '📆',
        'nft_reward': '🎨'
    };
    return icons[type] || '🎉';
}

function getRewardTitle(type) {
    const titles = {
        'mention': 'Mentioned OASIS',
        'link_share': 'Shared OASIS link',
        'quality_post': 'Created quality post',
        'helpful_answer': 'Helped community member',
        'code_example': 'Shared code example',
        'tutorial': 'Created tutorial',
        'viral': 'Created viral content',
        'invite': 'Invited new member',
        'daily_active': 'Daily active bonus',
        'weekly_active': 'Weekly active bonus',
        'nft_reward': 'Earned NFT'
    };
    return titles[type] || 'Telegram activity';
}

function getRewardCategory(type) {
    if (['mention', 'link_share', 'quality_post', 'tutorial'].includes(type)) {
        return 'Content Creation';
    }
    if (['helpful_answer', 'daily_active', 'weekly_active'].includes(type)) {
        return 'Community Engagement';
    }
    if (['invite', 'viral'].includes(type)) {
        return 'Marketing & Growth';
    }
    if (['code_example'].includes(type)) {
        return 'Technical';
    }
    return 'Content Creation';
}

function getAchievementTier(type) {
    const templates = getAchievementTemplates();
    const template = templates.find(t => t.id === type);
    return template?.tier || 'bronze';
}

function getTierIcon(tier) {
    const icons = {
        'bronze': '🥉',
        'silver': '🥈',
        'gold': '🥇',
        'platinum': '💎'
    };
    return icons[tier] || '🏆';
}

function getDefaultNFTImage(tier) {
    // Placeholder images - replace with actual NFT images
    return `https://via.placeholder.com/300x300?text=${tier.toUpperCase()}+NFT`;
}

function groupRewardsByDate(rewards) {
    const grouped = {};
    rewards.forEach(reward => {
        const date = new Date(reward.timestamp).toDateString();
        if (!grouped[date]) {
            grouped[date] = [];
        }
        grouped[date].push(reward);
    });
    return grouped;
}

function formatNumber(num, decimals = 0) {
    if (num === null || num === undefined) return '0';
    return num.toLocaleString('en-US', { 
        minimumFractionDigits: decimals, 
        maximumFractionDigits: decimals 
    });
}

function formatTimeAgo(timestamp) {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return formatDate(timestamp);
}

function formatDate(timestamp) {
    const date = new Date(timestamp);
    const today = new Date();
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    if (date.toDateString() === today.toDateString()) {
        return 'Today';
    }
    if (date.toDateString() === yesterday.toDateString()) {
        return 'Yesterday';
    }
    return date.toLocaleDateString('en-US', { 
        month: 'short', 
        day: 'numeric',
        year: date.getFullYear() !== today.getFullYear() ? 'numeric' : undefined
    });
}

function formatTime(timestamp) {
    const date = new Date(timestamp);
    return date.toLocaleTimeString('en-US', { 
        hour: 'numeric', 
        minute: '2-digit' 
    });
}

function setupTimelineFilters() {
    const filterBtns = document.querySelectorAll('.timeline-filters .filter-btn');
    const timelineItems = document.querySelectorAll('.timeline-item');

    filterBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const filter = btn.dataset.filter;
            
            // Update active button
            filterBtns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            
            // Filter items
            timelineItems.forEach(item => {
                if (filter === 'all') {
                    item.style.display = 'flex';
                } else {
                    const itemType = item.dataset.type;
                    const hasKarma = itemType.includes('karma') || item.querySelector('.reward-karma');
                    const hasTokens = itemType.includes('token') || item.querySelector('.reward-tokens');
                    const hasNFT = itemType.includes('nft') || item.querySelector('.reward-nft');
                    
                    if (filter === 'karma' && hasKarma) {
                        item.style.display = 'flex';
                    } else if (filter === 'tokens' && hasTokens) {
                        item.style.display = 'flex';
                    } else if (filter === 'nft' && hasNFT) {
                        item.style.display = 'flex';
                    } else {
                        item.style.display = 'none';
                    }
                }
            });
        });
    });
}

function setupNFTFilters() {
    const filterBtns = document.querySelectorAll('.nft-gallery-filters .filter-btn');
    const nftCards = document.querySelectorAll('.nft-gallery-card');

    filterBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            const tier = btn.dataset.tier;
            
            // Update active button
            filterBtns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            
            // Filter cards
            nftCards.forEach(card => {
                if (tier === 'all') {
                    card.style.display = 'block';
                } else {
                    card.style.display = card.dataset.tier === tier ? 'block' : 'none';
                }
            });
        });
    });
}

async function loadLeaderboardPeriod(period) {
    const leaderboard = await loadTelegramLeaderboard(period);
    renderLeaderboard(leaderboard);
}

/**
 * Link Telegram account
 */
async function linkTelegramAccount() {
    try {
        const authData = localStorage.getItem('oasis_auth');
        if (!authData) {
            alert('Please log in first');
            return;
        }

        const auth = JSON.parse(authData);
        const avatarId = auth.avatar?.id || auth.avatar?.avatarId;

        // Get verification code
        const codeResponse = await oasisAPI.request(
            `/api/telegram/link-code/${avatarId}`
        );

        if (codeResponse.isError) {
            alert('Error generating link code: ' + codeResponse.message);
            return;
        }

        const code = codeResponse.result.verificationCode;
        const instructions = codeResponse.result.instructions;

        // Show instructions modal
        showLinkInstructionsModal(code, instructions);

    } catch (error) {
        console.error('Error linking Telegram:', error);
        alert('Error linking Telegram account');
    }
}

function showLinkInstructionsModal(code, instructions) {
    // Create modal
    const modal = document.createElement('div');
    modal.className = 'modal';
    modal.innerHTML = `
        <div class="modal-overlay" onclick="this.closest('.modal').remove()"></div>
        <div class="modal-content telegram-link-modal">
            <button class="modal-close" onclick="this.closest('.modal').remove()">×</button>
            <h2>Link Telegram Account</h2>
            <div class="telegram-link-instructions">
                <p>1. Open Telegram and find the OASIS bot</p>
                <p>2. Send the following command:</p>
                <div class="telegram-link-code">
                    <code>/link {code}</code>
                    <button onclick="copyToClipboard('telegram-link-code-text')">Copy</button>
                </div>
                <p id="telegram-link-code-text" style="display: none;">/link ${code}</p>
                <p>3. The bot will confirm the link</p>
                <p>4. Refresh this page to see your rewards</p>
            </div>
            <button class="btn-primary" onclick="this.closest('.modal').remove()">Got it</button>
        </div>
    `;
    document.body.appendChild(modal);
}

async function disconnectTelegram() {
    if (!confirm('Are you sure you want to disconnect your Telegram account?')) {
        return;
    }

    try {
        const authData = localStorage.getItem('oasis_auth');
        const auth = JSON.parse(authData);
        const avatarId = auth.avatar?.id || auth.avatar?.avatarId;

        const response = await oasisAPI.request(
            `/api/telegram/disconnect/${avatarId}`,
            { method: 'POST' }
        );

        if (response.isError) {
            alert('Error disconnecting: ' + response.message);
            return;
        }

        // Reload Telegram section
        await loadTelegramGamification();

    } catch (error) {
        console.error('Error disconnecting Telegram:', error);
        alert('Error disconnecting Telegram account');
    }
}

function showTelegramError(error) {
    const container = document.getElementById('telegram-gamification-content');
    if (container) {
        container.innerHTML = `
            <div class="telegram-error-state">
                <p class="error-message">Error loading Telegram data: ${error.message}</p>
                <button class="btn-primary" onclick="loadTelegramGamification()">Retry</button>
            </div>
        `;
    }
}

// ============================================
// DYNAMIC ENHANCEMENTS - Real-time & Fun Features
// ============================================

/**
 * Telegram Notification Manager
 * Handles floating notifications for real-time updates
 */
class TelegramNotificationManager {
    constructor() {
        this.notifications = [];
        this.maxVisible = 3;
        this.container = null;
        this.init();
    }

    init() {
        // Create floating notification container
        if (!document.getElementById('telegram-notifications-floating')) {
            const container = document.createElement('div');
            container.id = 'telegram-notifications-floating';
            container.className = 'telegram-notifications-floating';
            document.body.appendChild(container);
            this.container = container;
        } else {
            this.container = document.getElementById('telegram-notifications-floating');
        }
    }

    show(notification) {
        const id = `notif-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
        const notif = {
            id,
            ...notification,
            timestamp: Date.now()
        };

        this.notifications.unshift(notif);
        if (this.notifications.length > this.maxVisible) {
            this.notifications = this.notifications.slice(0, this.maxVisible);
        }

        this.render();
        this.animateIn(id);

        // Auto-dismiss after 5 seconds (or custom duration)
        const duration = notification.duration || 5000;
        setTimeout(() => {
            this.dismiss(id);
        }, duration);

        // Play sound for achievements
        if (notification.type === 'achievement' || notification.priority === 'high') {
            this.playSound('achievement');
        }
    }

    dismiss(id) {
        const element = document.getElementById(id);
        if (element) {
            this.animateOut(id, () => {
                this.notifications = this.notifications.filter(n => n.id !== id);
                this.render();
            });
        }
    }

    render() {
        if (!this.container) return;

        this.container.innerHTML = this.notifications.map(notif => `
            <div id="${notif.id}" class="telegram-notification-card ${notif.priority || 'normal'}-priority" 
                 onclick="expandNotification('${notif.id}')">
                <div class="telegram-notification-header">
                    <div class="telegram-notification-icon">${this.getIcon(notif.type)}</div>
                    <div class="telegram-notification-type">${this.getTypeLabel(notif.type)}</div>
                    <button class="telegram-notification-close" onclick="event.stopPropagation(); dismissNotification('${notif.id}')">×</button>
                </div>
                <p class="telegram-notification-title">${notif.title}</p>
                <p class="telegram-notification-description">${notif.description || ''}</p>
                ${notif.rewards ? `
                    <div class="telegram-notification-rewards">
                        ${notif.rewards.karma ? `<span class="reward-karma">+${notif.rewards.karma} karma</span>` : ''}
                        ${notif.rewards.tokens ? `<span class="reward-tokens">+${notif.rewards.tokens} tokens</span>` : ''}
                        ${notif.rewards.nft ? `<span class="reward-nft">+1 NFT</span>` : ''}
                    </div>
                ` : ''}
            </div>
        `).join('');
    }

    getIcon(type) {
        const icons = {
            reward: '🎉',
            achievement: '🏆',
            nft: '🎨',
            streak: '🔥',
            leaderboard: '📈',
            challenge: '🎯',
            milestone: '⭐'
        };
        return icons[type] || '🔔';
    }

    getTypeLabel(type) {
        const labels = {
            reward: 'Reward Earned',
            achievement: 'Achievement Unlocked',
            nft: 'NFT Received',
            streak: 'Streak Milestone',
            leaderboard: 'Leaderboard Update',
            challenge: 'New Challenge',
            milestone: 'Milestone Reached'
        };
        return labels[type] || 'Update';
    }

    animateIn(id) {
        const element = document.getElementById(id);
        if (element) {
            element.style.animation = 'slideInRight 0.3s ease-out';
        }
    }

    animateOut(id, callback) {
        const element = document.getElementById(id);
        if (element) {
            element.style.animation = 'slideOutRight 0.3s ease-in';
            setTimeout(callback, 300);
        }
    }

    playSound(type) {
        // Optional: Play sound effect
        // const audio = new Audio(`/sounds/${type}.mp3`);
        // audio.play().catch(() => {}); // Ignore errors
    }
}

// Global notification manager instance
let telegramNotificationManager = null;

function initTelegramNotifications() {
    if (!telegramNotificationManager) {
        telegramNotificationManager = new TelegramNotificationManager();
    }
}

function showTelegramNotification(notification) {
    if (!telegramNotificationManager) {
        initTelegramNotifications();
    }
    telegramNotificationManager.show(notification);
}

function dismissNotification(id) {
    if (telegramNotificationManager) {
        telegramNotificationManager.dismiss(id);
    }
}

function expandNotification(id) {
    // Show full details in modal or expand inline
    const notification = telegramNotificationManager?.notifications.find(n => n.id === id);
    if (notification) {
        // Could open a modal with full details
        console.log('Expand notification:', notification);
    }
}

/**
 * Real-time Update Checker
 * Polls for new updates and shows notifications
 */
let lastUpdateCheck = null;
let updateCheckInterval = null;

function startTelegramUpdateChecker() {
    // Check every 10 seconds when tab is visible
    if (updateCheckInterval) {
        clearInterval(updateCheckInterval);
    }

    updateCheckInterval = setInterval(async () => {
        if (document.visibilityState === 'visible') {
            await checkForTelegramUpdates();
        }
    }, 10000);

    // Initial check
    checkForTelegramUpdates();
}

function stopTelegramUpdateChecker() {
    if (updateCheckInterval) {
        clearInterval(updateCheckInterval);
        updateCheckInterval = null;
    }
}

async function checkForTelegramUpdates() {
    const authData = localStorage.getItem('oasis_auth');
    if (!authData) return;

    try {
        const auth = JSON.parse(authData);
        const avatarId = auth.avatar?.id || auth.avatar?.avatarId;
        if (!avatarId) return;

        // Check for new rewards since last check
        const response = await oasisAPI.request(
            `/api/telegram/gamification/updates/${avatarId}?since=${lastUpdateCheck || Date.now() - 60000}`
        );

        if (response.isError || !response.result) return;

        const updates = response.result.updates || [];
        
        updates.forEach(update => {
            handleTelegramUpdate(update);
        });

        lastUpdateCheck = Date.now();

    } catch (error) {
        console.error('Error checking for updates:', error);
    }
}

function handleTelegramUpdate(update) {
    // Show notification based on update type
    switch (update.type) {
        case 'reward':
            showTelegramNotification({
                type: 'reward',
                title: `You earned ${update.karma || 0} karma!`,
                description: update.description || 'Great job!',
                rewards: {
                    karma: update.karma,
                    tokens: update.tokens,
                    nft: update.nft
                },
                priority: 'normal'
            });
            break;

        case 'achievement':
            showTelegramNotification({
                type: 'achievement',
                title: `Achievement Unlocked: ${update.achievementName}`,
                description: update.description || 'Congratulations!',
                rewards: update.rewards,
                priority: 'high',
                duration: 8000 // Show longer for achievements
            });
            // Trigger celebration animation
            celebrateAchievement(update);
            break;

        case 'nft':
            showTelegramNotification({
                type: 'nft',
                title: `New NFT: ${update.nftName}`,
                description: 'Check your NFT gallery!',
                priority: 'high',
                duration: 8000
            });
            break;

        case 'streak':
            showTelegramNotification({
                type: 'streak',
                title: `🔥 ${update.days}-day streak!`,
                description: 'Keep it up!',
                priority: 'normal'
            });
            break;

        case 'leaderboard':
            showTelegramNotification({
                type: 'leaderboard',
                title: `📈 You moved up ${update.positionChange} spots!`,
                description: `You're now #${update.newPosition}`,
                priority: 'normal'
            });
            break;
    }

    // Refresh the dashboard
    loadTelegramGamification();
}

/**
 * Achievement Celebration
 * Shows confetti and animations when achievement is unlocked
 */
function celebrateAchievement(achievement) {
    // Show confetti animation
    showConfetti();

    // Pulse the achievement badge
    const badgeElement = document.querySelector(`[data-achievement-id="${achievement.id}"]`);
    if (badgeElement) {
        badgeElement.classList.add('achievement-unlocked');
        setTimeout(() => {
            badgeElement.classList.remove('achievement-unlocked');
        }, 2000);
    }

    // Show achievement modal (optional)
    // showAchievementModal(achievement);
}

/**
 * Confetti Animation
 */
function showConfetti() {
    const confettiContainer = document.createElement('div');
    confettiContainer.className = 'confetti-container';
    confettiContainer.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        pointer-events: none;
        z-index: 9999;
    `;
    document.body.appendChild(confettiContainer);

    // Create confetti particles
    const colors = ['#FBBF24', '#22C55E', '#9333EA', '#3B82F6', '#EF4444'];
    const particleCount = 50;

    for (let i = 0; i < particleCount; i++) {
        const particle = document.createElement('div');
        particle.style.cssText = `
            position: absolute;
            width: 10px;
            height: 10px;
            background: ${colors[Math.floor(Math.random() * colors.length)]};
            left: ${Math.random() * 100}%;
            top: -10px;
            border-radius: 50%;
            animation: confetti-fall ${1 + Math.random() * 2}s linear forwards;
            animation-delay: ${Math.random() * 0.5}s;
        `;
        confettiContainer.appendChild(particle);
    }

    // Remove after animation
    setTimeout(() => {
        confettiContainer.remove();
    }, 3000);
}

/**
 * Animate Progress Bar
 */
function animateProgressBar(element, targetPercentage) {
    if (!element) return;
    
    const progressFill = element.querySelector('.achievement-progress-fill');
    if (!progressFill) return;

    // Reset to 0
    progressFill.style.width = '0%';
    
    // Animate to target
    setTimeout(() => {
        progressFill.style.transition = 'width 1s ease-out';
        progressFill.style.width = `${targetPercentage}%`;
    }, 100);

    // Check for milestones
    if (targetPercentage >= 100) {
        setTimeout(() => {
            progressFill.parentElement.classList.add('progress-complete');
        }, 1100);
    }
}

/**
 * Animate Number Count-up
 */
function animateValue(element, start, end, duration = 1000) {
    if (!element) return;
    
    const startTime = Date.now();
    const range = end - start;

    function update() {
        const now = Date.now();
        const elapsed = now - startTime;
        const progress = Math.min(elapsed / duration, 1);
        
        // Easing function
        const easeOutQuart = 1 - Math.pow(1 - progress, 4);
        const current = Math.floor(start + range * easeOutQuart);
        
        element.textContent = formatNumber(current);
        
        if (progress < 1) {
            requestAnimationFrame(update);
        } else {
            element.textContent = formatNumber(end);
        }
    }

    update();
}

/**
 * Update Stats with Animation
 */
function updateTelegramStatsAnimated(newStats) {
    const statsContainer = document.getElementById('telegram-stats');
    if (!statsContainer) return;

    // Animate each stat value
    const statCards = statsContainer.querySelectorAll('.telegram-stat-value');
    statCards.forEach(card => {
        const currentText = card.textContent;
        const currentValue = parseFloat(currentText.replace(/[^0-9.]/g, '')) || 0;
        
        // Find corresponding new value
        const statId = card.closest('.telegram-stat-card')?.dataset?.statId;
        const newValue = newStats[statId] || currentValue;
        
        if (newValue !== currentValue) {
            animateValue(card, currentValue, newValue);
        }
    });
}

/**
 * Highlight New Activity Item
 */
function highlightNewActivity(itemId) {
    const item = document.querySelector(`[data-activity-id="${itemId}"]`);
    if (item) {
        item.classList.add('activity-new');
        setTimeout(() => {
            item.classList.remove('activity-new');
        }, 3000);
    }
}

/**
 * Initialize Dynamic Features
 */
function initTelegramDynamicFeatures() {
    // Initialize notification manager
    initTelegramNotifications();

    // Start update checker
    startTelegramUpdateChecker();

    // Animate progress bars on load
    setTimeout(() => {
        document.querySelectorAll('.achievement-progress-bar').forEach(bar => {
            const fill = bar.querySelector('.achievement-progress-fill');
            if (fill) {
                const width = fill.style.width || '0%';
                fill.style.width = '0%';
                setTimeout(() => {
                    fill.style.width = width;
                }, 100);
            }
        });
    }, 500);

    // Stop checker when tab is hidden
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'hidden') {
            stopTelegramUpdateChecker();
        } else {
            startTelegramUpdateChecker();
        }
    });
}

// Initialize when Telegram tab is loaded
if (typeof loadTelegramGamification === 'function') {
    const originalLoad = loadTelegramGamification;
    loadTelegramGamification = async function() {
        await originalLoad();
        initTelegramDynamicFeatures();
    };
}
