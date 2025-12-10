// Telegram Gamification UI JavaScript
// Handles all Telegram gamification display and interactions

const TELEGRAM_GROUP_URL = 'https://t.me/your_oasis_group'; // Update with actual group URL

/**
 * Premium SVG Icon System
 * Returns SVG icons with animations instead of emojis
 */
const TelegramIcons = {
    karma: `<svg class="telegram-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M12 2L15.09 8.26L22 9.27L17 14.14L18.18 21.02L12 17.77L5.82 21.02L7 14.14L2 9.27L8.91 8.26L12 2Z" 
              stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" 
              fill="rgba(251, 191, 36, 0.2)"/>
        <path d="M12 6V12L15 15" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
    </svg>`,
    
    tokens: `<svg class="telegram-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2" fill="rgba(34, 197, 94, 0.1)"/>
        <path d="M12 6V12L16 14" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        <circle cx="12" cy="12" r="2" fill="currentColor"/>
    </svg>`,
    
    nft: `<svg class="telegram-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect x="3" y="3" width="18" height="18" rx="2" stroke="currentColor" stroke-width="2" fill="rgba(147, 51, 234, 0.1)"/>
        <path d="M8 12H16M12 8V16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <circle cx="7" cy="7" r="1" fill="currentColor"/>
        <circle cx="17" cy="7" r="1" fill="currentColor"/>
        <circle cx="7" cy="17" r="1" fill="currentColor"/>
        <circle cx="17" cy="17" r="1" fill="currentColor"/>
    </svg>`,
    
    streak: `<svg class="telegram-icon telegram-icon-pulse" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M12 2L2 7L12 12L22 7L12 2Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" 
              fill="rgba(251, 146, 60, 0.2)"/>
        <path d="M2 17L12 22L22 17" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" 
              fill="rgba(251, 146, 60, 0.15)"/>
        <path d="M2 12L12 17L22 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" 
              fill="rgba(251, 146, 60, 0.1)"/>
    </svg>`,
    
    achievement: `<svg class="telegram-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M6 9L12 2L18 9" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        <path d="M12 2V22" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M18 9L22 20H2L6 9" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" 
              fill="rgba(96, 165, 250, 0.1)"/>
        <circle cx="12" cy="20" r="2" fill="currentColor"/>
    </svg>`,
    
    groups: `<svg class="telegram-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="9" cy="7" r="4" stroke="currentColor" stroke-width="2" fill="rgba(34, 211, 238, 0.1)"/>
        <circle cx="15" cy="7" r="4" stroke="currentColor" stroke-width="2" fill="rgba(34, 211, 238, 0.1)"/>
        <path d="M1 21C1 17 4 14 9 14C14 14 17 17 17 21" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M15 14C17.5 14 23 15.5 23 21" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
    </svg>`,
    
    telegram: `<svg class="telegram-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M12 2C6.48 2 2 6.48 2 12C2 17.52 6.48 22 12 22C17.52 22 22 17.52 22 12C22 6.48 17.52 2 12 2Z" 
              stroke="currentColor" stroke-width="2" fill="rgba(99, 102, 241, 0.1)"/>
        <path d="M8 12L16 8L13 14L11 16L8 12Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" 
              stroke-linejoin="round" fill="currentColor" fill-opacity="0.3"/>
    </svg>`,
    
    check: `<svg class="telegram-icon telegram-icon-check" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2" fill="rgba(34, 197, 94, 0.1)"/>
        <path d="M8 12L11 15L16 9" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
    </svg>`,
    
    reward: `<svg class="telegram-icon telegram-icon-bounce" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M12 2L15.09 8.26L22 9.27L17 14.14L18.18 21.02L12 17.77L5.82 21.02L7 14.14L2 9.27L8.91 8.26L12 2Z" 
              stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" 
              fill="rgba(251, 191, 36, 0.2)"/>
    </svg>`,
    
    message: `<svg class="telegram-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M21 15C21 15.5304 20.7893 16.0391 20.4142 16.4142C20.0391 16.7893 19.5304 17 19 17H7L3 21V5C3 4.46957 3.21071 3.96086 3.58579 3.58579C3.96086 3.21071 4.46957 3 5 3H19C19.5304 3 20.0391 3.21071 20.4142 3.58579C20.7893 3.96086 21 4.46957 21 5V15Z" 
              stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" 
              fill="rgba(99, 102, 241, 0.1)"/>
    </svg>`,
    
    link: `<svg class="telegram-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M10 13C10.4295 13.5741 10.9774 14.0491 11.6066 14.3929C12.2357 14.7367 12.9315 14.9411 13.6467 14.9923C14.3618 15.0435 15.0796 14.9403 15.7513 14.6897C16.4231 14.4392 17.0331 14.047 17.54 13.54L20.54 10.54C21.4508 9.59695 21.9548 8.33394 21.9434 7.02296C21.932 5.71198 21.4061 4.45792 20.4791 3.53087C19.5521 2.60382 18.298 2.07799 16.987 2.0666C15.676 2.0552 14.413 2.55918 13.47 3.47L11.75 5.18" 
              stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        <path d="M14 11C13.5705 10.4259 13.0226 9.95088 12.3934 9.60711C11.7643 9.26334 11.0685 9.05886 10.3533 9.00766C9.63816 8.95645 8.92037 9.05972 8.24869 9.31026C7.57701 9.5608 6.96693 9.95304 6.46 10.46L3.46 13.46C2.54918 14.403 2.04518 15.6661 2.0566 16.977C2.068 18.288 2.59382 19.5421 3.52087 20.4691C4.44792 21.3962 5.70198 21.922 7.01296 21.9334C8.32394 21.9448 9.58695 21.4408 10.53 20.53L12.24 18.82" 
              stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
    </svg>`,
    
    post: `<svg class="telegram-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M14 2H6C5.46957 2 4.96086 2.21071 4.58579 2.58579C4.21071 2.96086 4 3.46957 4 4V20C4 20.5304 4.21071 21.0391 4.58579 21.4142C4.96086 21.7893 5.46957 22 6 22H18C18.5304 22 19.0391 21.7893 19.4142 21.4142C19.7893 21.0391 20 20.5304 20 20V8L14 2Z" 
              stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" fill="rgba(99, 102, 241, 0.1)"/>
        <path d="M14 2V8H20" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        <path d="M16 13H8" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M16 17H8" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M10 9H9H8" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
    </svg>`,
    
    lightbulb: `<svg class="telegram-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M12 2V4" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M12 20V22" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M4.93 4.93L6.34 6.34" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M17.66 17.66L19.07 19.07" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M2 12H4" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M20 12H22" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M6.34 17.66L4.93 19.07" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M19.07 4.93L17.66 6.34" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M9 12C9 10.3431 10.3431 9 12 9C13.6569 9 15 10.3431 15 12C15 13.6569 13.6569 15 12 15C10.3431 15 9 13.6569 9 12Z" 
              stroke="currentColor" stroke-width="2" fill="rgba(251, 191, 36, 0.1)"/>
    </svg>`,
    
    code: `<svg class="telegram-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <polyline points="16 18 22 12 16 6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        <polyline points="8 6 2 12 8 18" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
    </svg>`,
    
    book: `<svg class="telegram-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M4 19.5C4 18.837 4.26339 18.2011 4.73223 17.7322C5.20107 17.2634 5.83696 17 6.5 17H20" 
              stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
        <path d="M6.5 2H20V20H6.5C5.83696 20 5.20107 19.7366 4.73223 19.2678C4.26339 18.7989 4 18.163 4 17.5V4.5C4 3.83696 4.26339 3.20107 4.73223 2.73223C5.20107 2.26339 5.83696 2 6.5 2Z" 
              stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" fill="rgba(99, 102, 241, 0.1)"/>
    </svg>`,
    
    calendar: `<svg class="telegram-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <rect x="3" y="4" width="18" height="18" rx="2" stroke="currentColor" stroke-width="2" fill="rgba(99, 102, 241, 0.1)"/>
        <path d="M16 2V6" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M8 2V6" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
        <path d="M3 10H21" stroke="currentColor" stroke-width="2"/>
    </svg>`,
    
    target: `<svg class="telegram-icon" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="2" fill="rgba(99, 102, 241, 0.1)"/>
        <circle cx="12" cy="12" r="6" stroke="currentColor" stroke-width="2" fill="rgba(99, 102, 241, 0.1)"/>
        <circle cx="12" cy="12" r="2" fill="currentColor"/>
    </svg>`
};

/**
 * Get icon by name
 */
function getIcon(name) {
    return TelegramIcons[name] || TelegramIcons.target;
}

// Use the global oasisAPI from api/oasisApi.js if available, otherwise create a fallback
// We'll initialize this after checking if oasisAPI exists
let telegramAPI;

// Initialize telegramAPI - use oasisAPI if available, otherwise create fallback with all methods
function initializeTelegramAPI() {
    if (typeof oasisAPI !== 'undefined' && oasisAPI.getTelegramAvatarByOasis) {
        // Use the real oasisAPI which has all Telegram methods
        telegramAPI = oasisAPI;
        return;
    }
    
    // Create fallback with all necessary methods
    const baseURL = window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1'
        ? 'http://localhost:5004'
        : 'https://api.oasisweb4.one';
    
    telegramAPI = {
        baseURL: baseURL,
        
        getAuthHeaders() {
            try {
                const authData = localStorage.getItem('oasis_auth');
                if (!authData) {
                    return { 'Content-Type': 'application/json' };
                }
                const auth = JSON.parse(authData);
                const headers = { 'Content-Type': 'application/json' };
                if (auth.token) {
                    headers['Authorization'] = `Bearer ${auth.token}`;
                }
                return headers;
            } catch (error) {
                return { 'Content-Type': 'application/json' };
            }
        },
        
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
                const contentType = response.headers.get('content-type');
                if (contentType && contentType.includes('application/json')) {
                    return await response.json();
                } else {
                    return {
                        isError: !response.ok,
                        message: response.statusText,
                        status: response.status
                    };
                }
            } catch (error) {
                console.error('API request failed:', error);
                return { isError: true, message: error.message };
            }
        },
        
        // Telegram API methods
        async getTelegramAvatarByOasis(oasisAvatarId) {
            return this.request(`/api/telegram/avatar/oasis/${oasisAvatarId}`);
        },
        
        async getTelegramStats(telegramUserId) {
            return this.request(`/api/telegram/stats/user/${telegramUserId}`);
        },
        
        async getTelegramRewards(telegramUserId, limit = 10) {
            return this.request(`/api/telegram/rewards/user/${telegramUserId}?limit=${limit}`);
        },
        
        async getTelegramAchievements(telegramUserId) {
            return this.request(`/api/telegram/achievements/user/${telegramUserId}`);
        },
        
        async getTelegramActivities(telegramUserId, limit = 50, type = null) {
            let endpoint = `/api/telegram/activities/user/${telegramUserId}?limit=${limit}`;
            if (type) {
                endpoint += `&type=${type}`;
            }
            return this.request(endpoint);
        },
        
        async getTelegramGroups(telegramUserId) {
            return this.request(`/api/telegram/groups/user/${telegramUserId}`);
        },
        
        async getTelegramLeaderboard(groupId = null, period = 'alltime') {
            if (groupId) {
                return this.request(`/api/telegram/groups/${groupId}/leaderboard?period=${period}`);
            } else {
                return this.request(`/api/telegram/leaderboard?period=${period}`);
            }
        },
        
        async linkTelegram(data) {
            return this.request('/api/telegram/link-avatar', {
                method: 'POST',
                body: JSON.stringify(data)
            });
        }
    };
}

// Initialize immediately
initializeTelegramAPI();

// Re-initialize if oasisAPI becomes available later
if (typeof window !== 'undefined') {
    window.addEventListener('load', () => {
        if (typeof oasisAPI !== 'undefined' && oasisAPI.getTelegramAvatarByOasis && telegramAPI !== oasisAPI) {
            console.log('oasisAPI loaded, switching to use it');
            telegramAPI = oasisAPI;
        }
    });
}

/**
 * Load Telegram gamification data
 */
async function loadTelegramGamification() {
    console.log('Loading Telegram gamification...');
    
    // Ensure API is initialized
    if (!telegramAPI) {
        initializeTelegramAPI();
    }
    
    // Always render empty states first so user sees something
    const defaultStats = getDefaultStats();
    renderTelegramConnectionStatus({ telegramLinked: false });
    renderTelegramStats(defaultStats);
    renderRecentRewards([]);
    renderAchievementBadges([]);
    renderActivityTimeline([]);
    renderConversations([]);
    renderLeaderboard([]);
    renderRewardsBreakdown([]);
    renderQuests([]);
    renderNFTGallery([]);
    
    const authData = localStorage.getItem('oasis_auth');
    if (!authData) {
        console.log('No auth data found, showing login prompt');
        showLoginPrompt();
        return;
    }

    try {
        const auth = JSON.parse(authData);
        const avatarId = auth.avatar?.id || auth.avatar?.avatarId;
        
        if (!avatarId) {
            console.log('No avatar ID found');
            showLoginPrompt();
            return;
        }

        console.log('Checking Telegram link for avatar:', avatarId);
        
        // Check if Telegram is linked
        const telegramLink = await telegramAPI.getTelegramAvatarByOasis(avatarId);
        console.log('Telegram link response:', telegramLink);
        
        if (telegramLink.isError || !telegramLink.result) {
            console.log('Telegram not linked, showing connection banner');
            // Connection banner already rendered above
            return;
        }
        
        const telegramUserId = telegramLink.result.telegramUserId;
        const telegramUser = telegramLink.result;
        
        console.log('Telegram linked! Loading data for user:', telegramUserId);

        // Load all Telegram data in parallel
        const [stats, rewards, achievements, activities, groups, leaderboard] = await Promise.all([
            telegramAPI.getTelegramStats(telegramUserId).catch(e => {
                console.error('Error loading stats:', e);
                return { isError: true, result: null };
            }),
            telegramAPI.getTelegramRewards(telegramUserId, 10).catch(e => {
                console.error('Error loading rewards:', e);
                return { isError: true, result: [] };
            }),
            telegramAPI.getTelegramAchievements(telegramUserId).catch(e => {
                console.error('Error loading achievements:', e);
                return { isError: true, result: [] };
            }),
            telegramAPI.getTelegramActivities(telegramUserId, 50).catch(e => {
                console.error('Error loading activities:', e);
                return { isError: true, result: [] };
            }),
            telegramAPI.getTelegramGroups(telegramUserId).catch(e => {
                console.error('Error loading groups:', e);
                return { isError: true, result: [] };
            }),
            telegramAPI.getTelegramLeaderboard(null, 'alltime').catch(e => {
                console.error('Error loading leaderboard:', e);
                return { isError: true, result: [] };
            })
        ]);
        
        console.log('Data loaded:', { stats, rewards, achievements, activities, groups, leaderboard });

        // Process and render all components
        const statsData = stats.isError ? getDefaultStats() : {
            ...getDefaultStats(),
            ...stats.result,
            telegramLinked: true,
            telegramUsername: telegramUser.telegramUsername
        };

        renderTelegramConnectionStatus(statsData);
        renderTelegramStats(statsData);
        renderRecentRewards(rewards.isError ? [] : rewards.result || []);
        renderAchievementBadges(achievements.isError ? [] : achievements.result || []);
        renderActivityTimeline(activities.isError ? [] : activities.result || []);
        renderConversations(groups.isError ? [] : groups.result || []);
        renderLeaderboard(leaderboard.isError ? [] : leaderboard.result || []);
        renderRewardsBreakdown(rewards.isError ? [] : rewards.result || []);
        renderQuests(achievements.isError ? [] : achievements.result.filter(a => a.status === 'Active') || []);
        renderNFTGallery(achievements.isError ? [] : achievements.result.filter(a => a.status === 'Completed' && a.nftReward) || []);

    } catch (error) {
        console.error('Error loading Telegram gamification:', error);
        // Still render empty states so user can see the interface
        renderTelegramConnectionStatus({ telegramLinked: false });
        renderTelegramStats(getDefaultStats());
        renderRecentRewards([]);
        renderAchievementBadges([]);
        renderActivityTimeline([]);
        renderConversations([]);
        renderLeaderboard([]);
        renderRewardsBreakdown([]);
        renderQuests([]);
        renderNFTGallery([]);
        showTelegramError(error);
    }
}

function showLoginPrompt() {
    const container = document.getElementById('telegram-gamification-content');
    if (container) {
        // Show login prompt but keep the structure visible
        const connectionStatus = document.getElementById('telegram-connection-status');
        if (connectionStatus) {
            connectionStatus.innerHTML = `
                <div class="telegram-connection-banner not-linked">
                    <div class="telegram-connection-content">
                        <div class="telegram-connection-icon">🔒</div>
                        <div class="telegram-connection-info">
                            <h3>Please Log In</h3>
                            <p>Log in to view your Telegram gamification data.</p>
                        </div>
                        <button class="btn-primary" onclick="document.getElementById('loginModal').style.display = 'flex';">Log In</button>
                    </div>
                </div>
            `;
        }
        // Still render empty states for all sections
        renderTelegramStats(getDefaultStats());
        renderRecentRewards([]);
        renderAchievementBadges([]);
        renderActivityTimeline([]);
        renderConversations([]);
        renderLeaderboard([]);
        renderRewardsBreakdown([]);
        renderQuests([]);
        renderNFTGallery([]);
    }
}

/**
 * Load Telegram stats (now handled in main load function)
 * Kept for backward compatibility
 */
async function loadTelegramStats(telegramUserId) {
    try {
        const response = await telegramAPI.getTelegramStats(telegramUserId);
        
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
        totalKarma: 0,
        totalTokens: 0,
        totalNFTs: 0,
        dailyStreak: 0,
        longestStreak: 0,
        achievementsCompleted: 0,
        groupsJoined: 0,
        totalCheckins: 0,
        // Legacy field names for compatibility
        totalKarmaEarned: 0,
        totalTokensEarned: 0,
        nftsEarned: 0,
        achievementsActive: 0,
        weeklyActive: false
    };
}

/**
 * Load Telegram achievements
 */
async function loadTelegramAchievements(telegramUserId) {
    try {
        const response = await telegramAPI.getTelegramAchievements(telegramUserId);
        return response.result || [];
    } catch (error) {
        console.error('Error loading achievements:', error);
        return [];
    }
}

/**
 * Load Telegram rewards
 */
async function loadTelegramRewards(telegramUserId, limit = 50) {
    try {
        const response = await telegramAPI.getTelegramRewards(telegramUserId, limit);
        return response.result || [];
    } catch (error) {
        console.error('Error loading rewards:', error);
        return [];
    }
}

/**
 * Load leaderboard
 */
async function loadTelegramLeaderboard(period = 'alltime', groupId = null) {
    try {
        const response = await telegramAPI.getTelegramLeaderboard(groupId, period);
        return response.result || [];
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
                    <div class="telegram-connection-icon">${getIcon('telegram')}</div>
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
                    <div class="telegram-connection-icon">${getIcon('check')}</div>
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
    if (!container) {
        console.error('telegram-stats container not found!');
        return;
    }
    console.log('Rendering stats:', stats);

    const totalKarma = stats.totalKarma || stats.totalKarmaEarned || 0;
    const totalTokens = stats.totalTokens || stats.totalTokensEarned || 0;
    const totalNFTs = stats.totalNFTs || stats.nftsEarned || 0;
    const achievementsActive = stats.achievementsActive || 0;
    
    const statsData = [
        {
            id: 'karma',
            label: 'Karma Earned',
            value: formatNumber(totalKarma),
            sublabel: 'From Telegram activities',
            icon: getIcon('karma'),
            color: 'text-yellow-400'
        },
        {
            id: 'tokens',
            label: 'Tokens Earned',
            value: formatNumber(totalTokens, 1),
            sublabel: 'Token rewards',
            icon: getIcon('tokens'),
            color: 'text-green-400'
        },
        {
            id: 'nfts',
            label: 'NFTs Earned',
            value: totalNFTs,
            sublabel: 'Achievement NFTs',
            icon: getIcon('nft'),
            color: 'text-purple-400'
        },
        {
            id: 'streak',
            label: 'Daily Streak',
            value: `${stats.dailyStreak || 0} days`,
            sublabel: stats.dailyStreak > 0 ? 'Keep it up!' : 'Start your streak',
            icon: getIcon('streak'),
            color: 'text-orange-400'
        },
        {
            id: 'achievements',
            label: 'Achievements',
            value: `${stats.achievementsCompleted || 0}/${(stats.achievementsCompleted || 0) + achievementsActive}`,
            sublabel: `${achievementsActive} in progress`,
            icon: getIcon('achievement'),
            color: 'text-blue-400'
        },
        {
            id: 'groups',
            label: 'Groups Joined',
            value: stats.groupsJoined || 0,
            sublabel: 'Telegram groups',
            icon: getIcon('groups'),
            color: 'text-cyan-400'
        }
    ];

    container.innerHTML = `
        <div class="telegram-stats-grid">
            ${statsData.map(stat => `
                <div class="telegram-stat-card">
                    <div class="telegram-stat-header">
                        <div class="telegram-stat-icon">${stat.icon}</div>
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
    if (!container) {
        console.error('telegram-recent-rewards container not found!');
        return;
    }
    console.log('Rendering rewards:', rewards);

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
                        <p class="telegram-reward-description">${reward.reason || reward.description || 'Telegram activity'}</p>
                        <div class="telegram-reward-amounts">
                            ${reward.amount && reward.type === 'karma' ? `<span class="reward-karma">+${reward.amount} karma</span>` : ''}
                            ${reward.amount && reward.type === 'token' ? `<span class="reward-tokens">+${reward.amount} tokens</span>` : ''}
                            ${reward.type === 'nft' ? `<span class="reward-nft">+1 NFT</span>` : ''}
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
                                        <p class="timeline-item-description">${reward.reason || reward.description || ''}</p>
                                        <div class="timeline-item-rewards">
                                            ${reward.amount && reward.type === 'karma' ? `<span>+${reward.amount} karma</span>` : ''}
                                            ${reward.amount && reward.type === 'token' ? `<span>+${reward.amount} tokens</span>` : ''}
                                            ${reward.type === 'nft' ? `<span>+1 NFT</span>` : ''}
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
 * Render conversations/groups list
 */
function renderConversations(groups) {
    const container = document.getElementById('telegram-conversations');
    if (!container) return;

    if (groups.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <p class="empty-state-text">No active conversations. Join Telegram groups to start earning rewards!</p>
            </div>
        `;
        return;
    }

    container.innerHTML = `
        <div class="conversations-list">
            ${groups.map(group => {
                const memberCount = group.memberIds?.length || group.memberCount || 0;
                const isAdmin = group.adminIds?.includes(group.telegramUserId) || group.yourRole === 'admin';
                const role = isAdmin ? 'Admin' : 'Member';
                const lastActivity = group.lastActivity ? formatTimeAgo(group.lastActivity) : 'No recent activity';
                
                return `
                    <div class="conversation-item" onclick="viewGroupDetails('${group.id}')">
                        <div class="conversation-icon">👥</div>
                        <div class="conversation-content">
                            <div class="conversation-header">
                                <h3 class="conversation-name">${group.name || 'Unnamed Group'}</h3>
                                <span class="conversation-role">${role}</span>
                            </div>
                            <p class="conversation-meta">
                                ${memberCount} members • Last activity: ${lastActivity}
                            </p>
                            ${group.yourStats ? `
                                <div class="conversation-stats">
                                    <span class="conversation-stat">
                                        <span class="conversation-stat-icon">${getIcon('karma')}</span>
                                        ${group.yourStats.karma || 0} karma
                                    </span>
                                    <span class="conversation-stat">
                                        <span class="conversation-stat-icon">${getIcon('calendar')}</span>
                                        ${group.yourStats.checkins || 0} check-ins
                                    </span>
                                    <span class="conversation-stat">
                                        <span class="conversation-stat-icon">${getIcon('achievement')}</span>
                                        ${group.yourStats.achievements || 0} achievements
                                    </span>
                                </div>
                            ` : ''}
                        </div>
                        <div class="conversation-arrow">→</div>
                    </div>
                `;
            }).join('')}
        </div>
    `;
}

/**
 * Render quests/challenges
 */
function renderQuests(achievements) {
    const container = document.getElementById('telegram-quests');
    if (!container) return;

    // Filter active achievements as quests
    const activeQuests = achievements.filter(a => a.status === 'Active' || a.status === 'active');
    const completedQuests = achievements.filter(a => a.status === 'Completed' || a.status === 'completed');

    if (activeQuests.length === 0 && completedQuests.length === 0) {
        container.innerHTML = `
            <div class="empty-state">
                <p class="empty-state-text">No quests available. Complete Telegram activities to unlock quests!</p>
            </div>
        `;
        return;
    }

    container.innerHTML = `
        <div class="quests-container">
            ${activeQuests.length > 0 ? `
                <div class="quests-section">
                    <h3 class="quests-section-title">Active Quests</h3>
                    <div class="quests-list">
                        ${activeQuests.map(quest => {
                            const progress = quest.progress || 0;
                            const goal = quest.goal || quest.target || 1;
                            const progressPct = goal > 0 ? Math.min(100, (progress / goal) * 100) : 0;
                            
                            return `
                                <div class="quest-item active">
                                    <div class="quest-header">
                                        <div class="quest-icon">${getQuestIcon(quest.type || quest.id)}</div>
                                        <div class="quest-info">
                                            <h4 class="quest-name">${quest.name || quest.description || 'Quest'}</h4>
                                            <p class="quest-description">${quest.description || ''}</p>
                                        </div>
                                    </div>
                                    <div class="quest-progress">
                                        <div class="quest-progress-bar">
                                            <div class="quest-progress-fill" style="width: ${progressPct}%"></div>
                                        </div>
                                        <div class="quest-progress-text">
                                            ${progress} / ${goal} (${Math.round(progressPct)}%)
                                        </div>
                                    </div>
                                    <div class="quest-rewards">
                                        ${quest.karmaReward > 0 ? `<span class="reward-karma">+${quest.karmaReward} karma</span>` : ''}
                                        ${quest.tokenReward > 0 ? `<span class="reward-tokens">+${quest.tokenReward} tokens</span>` : ''}
                                        ${quest.nftReward ? `<span class="reward-nft">+1 NFT</span>` : ''}
                                    </div>
                                </div>
                            `;
                        }).join('')}
                    </div>
                </div>
            ` : ''}
            
            ${completedQuests.length > 0 ? `
                <div class="quests-section">
                    <h3 class="quests-section-title">Completed Quests</h3>
                    <div class="quests-list">
                        ${completedQuests.slice(0, 5).map(quest => `
                            <div class="quest-item completed">
                                <div class="quest-header">
                                    <div class="quest-icon">${getIcon('check')}</div>
                                    <div class="quest-info">
                                        <h4 class="quest-name">${quest.name || quest.description || 'Quest'}</h4>
                                        <p class="quest-description">Completed ${quest.completedAt ? formatTimeAgo(quest.completedAt) : 'recently'}</p>
                                    </div>
                                </div>
                            </div>
                        `).join('')}
                    </div>
                </div>
            ` : ''}
        </div>
    `;
}

function getQuestIcon(questType) {
    const icons = {
        'checkin': getIcon('calendar'),
        'streak': getIcon('streak'),
        'mention': getIcon('message'),
        'share': getIcon('link'),
        'post': getIcon('post'),
        'help': getIcon('lightbulb'),
        'code': getIcon('code'),
        'tutorial': getIcon('book'),
        'viral': getIcon('streak'),
        'invite': getIcon('groups')
    };
    return icons[questType] || getIcon('target');
}

function viewGroupDetails(groupId) {
    // TODO: Implement group details view
    console.log('View group details:', groupId);
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
            icon: getIcon('message'),
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
            icon: getIcon('link'),
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
            icon: getIcon('post'),
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
            icon: getIcon('lightbulb'),
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
            icon: getIcon('code'),
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
            icon: getIcon('book'),
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
            icon: getIcon('streak'),
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
            icon: getIcon('groups'),
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
            icon: getIcon('achievement'),
            target: 1,
            karmaReward: 500,
            tokenReward: 50,
            nftReward: true
        }
    ];
}

function getRewardIcon(type) {
    const icons = {
        'mention': getIcon('message'),
        'link_share': getIcon('link'),
        'quality_post': getIcon('post'),
        'helpful_answer': getIcon('lightbulb'),
        'code_example': getIcon('code'),
        'tutorial': getIcon('book'),
        'viral': getIcon('streak'),
        'invite': getIcon('groups'),
        'daily_active': getIcon('calendar'),
        'weekly_active': getIcon('calendar'),
        'nft_reward': getIcon('nft')
    };
    return icons[type] || getIcon('reward');
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

// Make viewGroupDetails globally available
window.viewGroupDetails = viewGroupDetails;

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

        if (!avatarId) {
            alert('Avatar ID not found. Please log in again.');
            return;
        }

        // Show instructions modal with manual linking process
        // The user needs to get their Telegram user ID and verification code from the bot
        showLinkInstructionsModal(null, null);

    } catch (error) {
        console.error('Error linking Telegram:', error);
        alert('Error linking Telegram account');
    }
}

let linkStatusPollInterval = null;

function showLinkInstructionsModal(code, instructions) {
    // Close any existing modal
    const existingModal = document.querySelector('.telegram-link-modal')?.closest('.modal');
    if (existingModal) {
        existingModal.remove();
    }

    const authData = localStorage.getItem('oasis_auth');
    const auth = JSON.parse(authData);
    const avatarId = auth.avatar?.id || auth.avatar?.avatarId;

    // Create modal
    const modal = document.createElement('div');
    modal.className = 'modal';
    modal.innerHTML = `
        <div class="modal-overlay" onclick="closeTelegramLinkModal()"></div>
        <div class="modal-content telegram-link-modal">
            <button class="modal-close" onclick="closeTelegramLinkModal()">×</button>
            
            <div class="telegram-link-header">
                <div class="telegram-link-icon">📱</div>
                <h2>Link Your Telegram Account</h2>
                <p>Follow these steps to connect your Telegram account</p>
            </div>
            
            <div class="telegram-link-instructions">
                <p><strong>Step 1:</strong> Open Telegram and search for <strong>@OASISBot</strong> (or your OASIS bot name)</p>
                <p><strong>Step 2:</strong> Send the <code>/start</code> command to the bot</p>
                <p><strong>Step 3:</strong> Follow the bot's instructions to link your account</p>
                <p><strong>Step 4:</strong> The bot will provide a verification code</p>
                <p><strong>Step 5:</strong> Enter the verification code below</p>
            </div>
            
            <div class="telegram-link-code">
                <label for="verification-code-input">Verification Code:</label>
                <input 
                    type="text" 
                    id="verification-code-input" 
                    placeholder="Enter code from Telegram bot"
                    class="auth-input"
                />
            </div>
            
            <div class="telegram-link-actions">
                <button class="btn-secondary" onclick="closeTelegramLinkModal()">Cancel</button>
                <button class="btn-primary" onclick="submitTelegramLink()">Link Account</button>
            </div>
            
            <div class="telegram-link-help">
                <p>💡 <strong>Tip:</strong> If you don't have the verification code, contact the bot first using <code>/start</code></p>
            </div>
        </div>
    `;
    document.body.appendChild(modal);
}

async function submitTelegramLink() {
    const codeInput = document.getElementById('verification-code-input');
    const verificationCode = codeInput?.value?.trim();
    
    if (!verificationCode) {
        alert('Please enter the verification code from the Telegram bot');
        return;
    }

    try {
        const authData = localStorage.getItem('oasis_auth');
        const auth = JSON.parse(authData);
        const avatarId = auth.avatar?.id || auth.avatar?.avatarId;

        if (!avatarId) {
            alert('Avatar ID not found. Please log in again.');
            return;
        }

        // Note: This assumes the API endpoint accepts verificationCode
        // The actual implementation may vary based on how the bot provides the linking data
        const result = await telegramAPI.linkTelegram({
            oasisAvatarId: avatarId,
            verificationCode: verificationCode
        });

        if (result.isError) {
            alert('Error linking account: ' + (result.message || 'Unknown error'));
            return;
        }

        // Success - close modal and refresh
        closeTelegramLinkModal();
        await loadTelegramGamification();
        
        // Refresh message section if on dashboard
        if (typeof loadTelegramMessageSection === 'function') {
            loadTelegramMessageSection();
        }

    } catch (error) {
        console.error('Error linking Telegram:', error);
        alert('Error linking Telegram account: ' + error.message);
    }
}

// Make submitTelegramLink globally available
window.submitTelegramLink = submitTelegramLink;

function startTelegramLinkStatusPolling() {
    // Clear any existing polling
    if (linkStatusPollInterval) {
        clearInterval(linkStatusPollInterval);
    }
    
    // Check every 3 seconds
    linkStatusPollInterval = setInterval(async () => {
        const linked = await checkTelegramLinkStatus();
        if (linked) {
            clearInterval(linkStatusPollInterval);
            linkStatusPollInterval = null;
            showTelegramLinkSuccess();
            // Refresh dashboard after a short delay
            setTimeout(() => {
                loadTelegramGamification();
            }, 1000);
        }
    }, 3000);
    
    // Stop after 10 minutes (code expires)
    setTimeout(() => {
        if (linkStatusPollInterval) {
            clearInterval(linkStatusPollInterval);
            linkStatusPollInterval = null;
            showTelegramLinkExpired();
        }
    }, 600000);
}

async function checkTelegramLinkStatus() {
    const authData = localStorage.getItem('oasis_auth');
    if (!authData) return false;
    
    try {
        const auth = JSON.parse(authData);
        const avatarId = auth.avatar?.id || auth.avatar?.avatarId;
        
        if (!avatarId) return false;
        
        const response = await telegramAPI.getTelegramAvatarByOasis(avatarId);
        
        if (response.isError || !response.result) {
            return false;
        }
        
        return true; // If we got a result, it's linked
    } catch (error) {
        console.error('Error checking link status:', error);
        return false;
    }
}

function copyTelegramLinkCommand() {
    const commandText = document.getElementById('link-command-text');
    if (!commandText) return;
    
    const command = commandText.textContent;
    navigator.clipboard.writeText(command).then(() => {
        const btn = event.target.closest('.copy-btn');
        if (btn) {
            const originalHTML = btn.innerHTML;
            btn.innerHTML = '<span class="copy-icon">✓</span> Copied!';
            btn.style.background = 'rgba(34, 197, 94, 0.2)';
            btn.style.color = 'rgba(34, 197, 94, 1)';
            
            setTimeout(() => {
                btn.innerHTML = originalHTML;
                btn.style.background = '';
                btn.style.color = '';
            }, 2000);
        }
    }).catch(err => {
        console.error('Failed to copy:', err);
        alert('Failed to copy. Please copy manually: ' + command);
    });
}

function closeTelegramLinkModal() {
    // Stop polling
    if (linkStatusPollInterval) {
        clearInterval(linkStatusPollInterval);
        linkStatusPollInterval = null;
    }
    
    const modal = document.querySelector('.telegram-link-modal')?.closest('.modal');
    if (modal) {
        modal.remove();
    }
}

function showTelegramLinkSuccess() {
    const modal = document.querySelector('.telegram-link-modal')?.closest('.modal');
    if (modal) {
        const modalContent = modal.querySelector('.modal-content');
        modalContent.innerHTML = `
            <div class="link-success-content">
                <div class="success-icon">✅</div>
                <h2>Successfully Linked!</h2>
                <p>Your Telegram account is now connected to your OASIS avatar.</p>
                <p class="success-message">You can now earn rewards for your Telegram activity!</p>
                <button class="btn-primary" onclick="closeTelegramLinkModal(); loadTelegramGamification(); if(typeof loadTelegramMessageSection === 'function') { loadTelegramMessageSection(); }">
                    View Telegram Dashboard
                </button>
            </div>
        `;
        modalContent.classList.add('success');
    }
    
    // Also refresh message section if on dashboard
    if (typeof loadTelegramMessageSection === 'function') {
        setTimeout(() => {
            loadTelegramMessageSection();
        }, 1000);
    }
}

function showTelegramLinkExpired() {
    const statusIndicator = document.getElementById('link-status-indicator');
    if (statusIndicator) {
        statusIndicator.innerHTML = `
            <div class="status-error">⏰</div>
            <span style="color: rgba(239, 68, 68, 1);">Code expired. Please generate a new one.</span>
        `;
    }
}

// Make functions globally available
window.closeTelegramLinkModal = closeTelegramLinkModal;
window.copyTelegramLinkCommand = copyTelegramLinkCommand;
window.checkTelegramLinkStatus = checkTelegramLinkStatus;

async function disconnectTelegram() {
    if (!confirm('Are you sure you want to disconnect your Telegram account?')) {
        return;
    }

    try {
        const authData = localStorage.getItem('oasis_auth');
        const auth = JSON.parse(authData);
        const avatarId = auth.avatar?.id || auth.avatar?.avatarId;

        // Note: This endpoint may need to be confirmed with the actual API
        const response = await telegramAPI.request(
            `/api/telegram/disconnect/${avatarId}`,
            { method: 'POST' }
        );

        if (response.isError) {
            alert('Error disconnecting: ' + (response.message || 'Unknown error'));
            return;
        }

        // Reload Telegram section
        await loadTelegramGamification();
        
        // Reload message section if on dashboard
        if (typeof loadTelegramMessageSection === 'function') {
            loadTelegramMessageSection();
        }

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
        // Note: This endpoint may need to be implemented or use activities endpoint
        const telegramLink = await telegramAPI.getTelegramAvatarByOasis(avatarId);
        if (telegramLink.isError || !telegramLink.result) return;
        
        const telegramUserId = telegramLink.result.telegramUserId;
        const response = await telegramAPI.getTelegramActivities(
            telegramUserId, 
            10, 
            null
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
        
        // Refresh message section if on dashboard
        if (typeof loadTelegramMessageSection === 'function') {
            loadTelegramMessageSection();
        }
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
