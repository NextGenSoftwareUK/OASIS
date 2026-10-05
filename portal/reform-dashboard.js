// Reform Dashboard JavaScript
// Converted from React components in oasis-wallet-ui

const brandBlue = '#00AEEF';

/**
 * Build Reform Hub data structure
 */
function buildReformHubData(avatar) {
    const supporterName = avatar?.firstName || avatar?.username || 'Reform supporter';
    
    const stats = [
        {
            id: 'ground-campaign',
            label: 'Field Missions',
            value: '42',
            detail: 'Doorstep conversations logged this month',
            icon: '🚪'
        },
        {
            id: 'digital-reach',
            label: 'Digital Reach',
            value: '128K',
            detail: 'Impressions across X/Telegram in 7 days',
            icon: '📢'
        },
        {
            id: 'civic-points',
            label: 'Reform Score',
            value: '3,560 pts',
            detail: '+220 pts this week',
            icon: '⚡'
        },
        {
            id: 'nfts',
            label: 'Impact NFTs',
            value: '12 badges',
            detail: 'Unique community collectibles',
            icon: '🎖️'
        }
    ];

    const badges = [
        {
            id: 'badge-field',
            title: 'Constituency Pathfinder',
            description: 'Visited 10 priority wards with canvassing data sync.',
            status: 'unlocked',
            reward: '+250 pts & Field Ops access',
            imageUrl: '/reform/badges/badge-field.svg'
        },
        {
            id: 'badge-digital',
            title: 'Amplifier',
            description: 'Hosted a Telegram AMA and weekly X Space.',
            status: 'unlocked',
            reward: 'Exclusive merch drop',
            imageUrl: '/reform/badges/badge-digital.svg'
        },
        {
            id: 'badge-donate',
            title: 'Momentum Builder',
            description: 'Secured 5 recurring donations via dashboard links.',
            status: 'in-progress',
            reward: 'Token rebate on campaign shop',
            imageUrl: '/reform/badges/badge-donate.svg'
        },
        {
            id: 'badge-shirt',
            title: 'Top Prize: Reform Kit',
            description: 'Lead the leaderboard for 4 straight weeks.',
            status: 'in-progress',
            reward: 'Limited Reform UK football shirt',
            imageUrl: '/reform/badges/badge-shirt.svg'
        }
    ];

    const news = [
        {
            id: 'news-1',
            title: 'Reform launches regional volunteer nerve center',
            summary: 'New Midlands command hub ties canvassing, data capture, and NFT rewards under one dashboard.',
            publishedAt: '2h ago',
            channel: 'Field',
            link: 'https://reformparty.uk/news/regional-nerve-center'
        },
        {
            id: 'news-2',
            title: 'Telegram town hall with Nigel reaches 60k members',
            summary: 'Community AMA minted "Voice of Reform" badge to hosts and top contributors automatically.',
            publishedAt: '5h ago',
            channel: 'Telegram',
            link: 'https://t.me/ReformUK'
        },
        {
            id: 'news-3',
            title: 'Campaign treasury publishes transparent flow report',
            summary: 'Supporters can track tokenized donations and grant payouts via OASIS wallet.',
            publishedAt: '1d ago',
            channel: 'YouTube',
            link: 'https://youtube.com/ReformUK'
        }
    ];

    const roles = [
        {
            id: 'role-1',
            title: 'Signal Booster',
            description: 'Coordinate digital pushes and community rooms.',
            requirements: ['1K+ impressions / week', 'Host or moderate 2 community events'],
            permissions: ['Schedule broadcasts', 'Mint Amplifier NFTs'],
            achieved: true
        },
        {
            id: 'role-2',
            title: 'Constituency Captain',
            description: 'Own field operations in your district.',
            requirements: ['Complete Pathfinder badge', 'Upload turf insights weekly'],
            permissions: ['Access field analytics', 'Assign canvassing missions'],
            achieved: false
        },
        {
            id: 'role-3',
            title: 'Policy Circle',
            description: 'Unlock draft access and token-weighted voting.',
            requirements: ['Verified donor', 'Trusted score > 4000'],
            permissions: ['Early briefings', 'Submit proposals'],
            achieved: false
        }
    ];

    const feed = [
        {
            id: 'feed-1',
            source: 'X (Twitter)',
            title: 'Amplified manifesto clip',
            description: 'Retweeted the reform health plan clip.',
            timestamp: '3m ago',
            valueChange: '+25 pts'
        },
        {
            id: 'feed-2',
            source: 'Telegram',
            title: 'Hosted student caucus AMA',
            description: 'Lead a Q&A with 600 attendees.',
            timestamp: '1h ago',
            valueChange: '+60 pts'
        },
        {
            id: 'feed-3',
            source: 'YouTube',
            title: 'Comment swarm mission',
            description: 'Left top comment securing 4k likes.',
            timestamp: '3h ago',
            valueChange: '+40 pts'
        },
        {
            id: 'feed-4',
            source: 'Field',
            title: 'Registered volunteers',
            description: 'Onboarded 8 university canvassers.',
            timestamp: 'Yesterday',
            valueChange: '+120 pts'
        }
    ];

    const notifications = [
        {
            id: 'notif-1',
            title: 'Fundraising drive: Midlands HQ',
            description: 'We unlocked £35k of the £50k goal. Final push livestream tonight.',
            time: '15m ago',
            priority: 'high'
        },
        {
            id: 'notif-2',
            title: 'Telegram AMA slots',
            description: 'New weekly slots open for student moderators.',
            time: '1h ago',
            priority: 'normal'
        },
        {
            id: 'notif-3',
            title: 'Policy vote reminder',
            description: 'Review the education whitepaper draft before Sunday.',
            time: '3h ago',
            priority: 'normal'
        }
    ];

    const events = [
        {
            id: 'event-1',
            title: 'Birmingham student canvass',
            location: 'Birmingham Digbeth hub',
            time: 'Sat 10:00',
            rsvps: 32,
            status: 'open'
        },
        {
            id: 'event-2',
            title: 'Northern digital war-room',
            location: 'Discord + Telegram',
            time: 'Sun 19:30',
            rsvps: 58,
            status: 'open'
        },
        {
            id: 'event-3',
            title: 'Westminster policy lab',
            location: 'HQ briefing room',
            time: 'Next Wed',
            rsvps: 120,
            status: 'waitlist'
        }
    ];

    const leaderboard = [
        {
            id: 'leader-1',
            name: 'Amelia K.',
            region: 'Manchester North',
            avatarColor: '#DB2777',
            points: 4820,
            missionsCompleted: 38,
            providerFocus: 'Telegram',
            streak: 4
        },
        {
            id: 'leader-2',
            name: 'Lewis T.',
            region: 'Birmingham',
            avatarColor: '#14B8A6',
            points: 4510,
            missionsCompleted: 33,
            providerFocus: 'X',
            streak: 3
        },
        {
            id: 'leader-3',
            name: 'Sara P.',
            region: 'Leeds Central',
            avatarColor: '#F59E0B',
            points: 4325,
            missionsCompleted: 29,
            providerFocus: 'Telegram',
            streak: 5
        },
        {
            id: 'leader-4',
            name: 'Noah R.',
            region: 'Bristol South',
            avatarColor: '#6366F1',
            points: 4190,
            missionsCompleted: 25,
            providerFocus: 'Field',
            streak: 2
        }
    ];

    const contacts = [
        {
            id: 'contact-1',
            name: 'Student Caucus Leads',
            platform: 'Telegram',
            username: '@reform_caucus',
            priority: 'high',
            lastSeen: '5m ago',
            action: 'message'
        },
        {
            id: 'contact-2',
            name: 'Regional Captains',
            platform: 'X',
            username: '@reform-field',
            priority: 'normal',
            lastSeen: '30m ago',
            action: 'meet'
        },
        {
            id: 'contact-3',
            name: 'Fundraising Desk',
            platform: 'Telegram',
            username: '@reform_funding',
            priority: 'high',
            lastSeen: '1h ago',
            action: 'call'
        }
    ];

    const pathways = [
        {
            id: 'path-signal',
            title: 'Signal Amplifier',
            category: 'signal',
            description: 'Grow Reform\'s reach across X, Telegram, and YouTube.',
            level: 2,
            progress: 65,
            reward: 'Unlocks Boost badge + merch access',
            icon: '📡',
            steps: [
                { id: 'step1', title: 'Share 3 clips this week', description: 'Tag @ReformUK on X.', completed: true },
                { id: 'step2', title: 'Host a Telegram AMA', description: 'Schedule via Reform CRM.', completed: false },
                { id: 'step3', title: 'Maintain 4-week streak', description: 'Earn the Reform kit raffle entry.', completed: false }
            ]
        },
        {
            id: 'path-field',
            title: 'Constituency Captain',
            category: 'field',
            description: 'Lead on-the-ground missions in your ward.',
            level: 1,
            progress: 30,
            reward: 'Access to local data + micro-grants',
            icon: '🚪',
            steps: [
                { id: 'step1', title: 'Attend captain training', description: 'Scan QR at HQ briefing.', completed: true },
                { id: 'step2', title: 'Log 20 doorstep convos', description: 'Use the mobile mission form.', completed: false },
                { id: 'step3', title: 'Recruit 5 volunteers', description: 'Share your referral link.', completed: false }
            ]
        },
        {
            id: 'path-policy',
            title: 'Policy Circle',
            category: 'policy',
            description: 'Shape Reform manifestos and critique drafts.',
            level: 3,
            progress: 80,
            reward: 'Token-weighted vote + Policy Architect NFT',
            icon: '📜',
            steps: [
                { id: 'step1', title: 'Read Education Blueprint', description: 'Complete proof-of-read quiz.', completed: true },
                { id: 'step2', title: 'Submit feedback', description: 'Use quadratic voting round.', completed: true },
                { id: 'step3', title: 'Lead micro working group', description: 'Deliver a draft update.', completed: false }
            ]
        }
    ];

    const hq = [
        {
            id: 'hq-policy',
            title: 'Education Blueprint v2',
            summary: 'Updated policy briefing on academy reforms and vocational pipelines.',
            category: 'policy',
            link: 'https://reformparty.uk/policy/education',
            publishedAt: 'Today'
        },
        {
            id: 'hq-content',
            title: 'Nigel town hall highlights',
            summary: 'Cut-down clip + social pack for the student living cost session.',
            category: 'content',
            link: 'https://youtube.com/reformuk',
            publishedAt: '3h ago'
        },
        {
            id: 'hq-campaign',
            title: 'Midlands Surge Campaign',
            summary: 'Digital + field mission with unified slogan assets and budgets.',
            category: 'campaign',
            link: 'https://reformparty.uk/campaigns/midlands',
            publishedAt: 'Yesterday'
        }
    ];

    return {
        greeting: `Welcome, ${supporterName}`,
        stats,
        badges,
        news,
        roles,
        feed,
        notifications,
        events,
        leaderboard,
        contacts,
        hq,
        pathways
    };
}

/**
 * Render Reform Hero section
 */
function renderReformHero(avatar, data) {
    const container = document.getElementById('reform-hero');
    if (!container) return;

    const displayName = avatar?.firstName 
        ? `${avatar.firstName} ${avatar.lastName || ''}`.trim()
        : avatar?.username || 'Supporter';

    container.innerHTML = `
        <div class="reform-hero-section">
            <div class="reform-hero-gradient"></div>
            <div class="reform-hero-content">
                <div class="reform-hero-left">
                    <div class="reform-hero-header">
                        <div class="reform-logo-container">
                            <img src="/reform/reform-logo-gold.png" alt="Reform UK" class="reform-logo" />
                        </div>
                        <div>
                            <p class="reform-hero-label">Signal Hub</p>
                            <p class="reform-hero-subtitle">Unified Reform identity · Powered by OASIS providers</p>
                        </div>
                    </div>
                    <p class="reform-hero-greeting">${data.greeting}</p>
                    <div class="reform-hero-name">
                        ${displayName}
                        <span class="reform-sparkle">✨</span>
                    </div>
                    <p class="reform-hero-description">
                        Track your Reform-wide impact, unlock community NFTs, and access roles that give you
                        more say in strategy, content, and funding.
                    </p>
                    <div class="reform-hero-badges">
                        <span class="reform-badge-item">
                            <span class="reform-badge-icon">🛡️</span>
                            Trusted supporter
                        </span>
                        <span class="reform-badge-item">
                            Region: ${avatar?.region || 'Nationwide'}
                        </span>
                    </div>
                </div>
                <div class="reform-hero-actions">
                    <button class="btn-primary reform-btn-primary">Launch campaign hub →</button>
                    <button class="btn-secondary reform-btn-secondary">Share impact card</button>
                </div>
                <div class="reform-hero-stats">
                    <div class="reform-stats-grid">
                        ${data.stats.map(stat => `
                            <div class="reform-stat-card">
                                <div class="reform-stat-icon">${stat.icon}</div>
                                <p class="reform-stat-label">${stat.label}</p>
                                <p class="reform-stat-value">${stat.value}</p>
                                <p class="reform-stat-detail">${stat.detail}</p>
                            </div>
                        `).join('')}
                    </div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Render Reform Pathways
 */
function renderReformPathways(pathways) {
    const container = document.getElementById('reform-pathways');
    if (!container) return;

    const categoryLabels = {
        signal: 'Signal Amplifier',
        field: 'Field Ops',
        policy: 'Policy Circle',
        fundraising: 'Fundraising',
        student: 'Youth',
        data: 'Insights'
    };

    let currentFilter = 'all';
    let collapsed = false;

    function render() {
        const filtered = currentFilter === 'all' 
            ? pathways 
            : pathways.filter(p => p.category === currentFilter);

        container.innerHTML = `
            <div class="reform-pathways-section">
                <div class="reform-pathways-header">
                    <div>
                        <p class="reform-section-label">Pathways</p>
                        <div class="reform-pathways-title-row">
                            <h2 class="reform-section-title">Choose your Reform mission</h2>
                            <button class="reform-toggle-btn" onclick="togglePathwaysCollapse()">
                                ${collapsed ? 'Show' : 'Hide'}
                            </button>
                        </div>
                        <p class="reform-section-subtitle">
                            Track your progress across digital, field, policy, and fundraising ladders.
                        </p>
                    </div>
                    ${!collapsed ? `
                        <div class="reform-pathways-filters">
                            <button class="reform-filter-btn ${currentFilter === 'all' ? 'active' : ''}" 
                                    onclick="setPathwayFilter('all')">All</button>
                            ${Object.entries(categoryLabels).map(([key, label]) => `
                                <button class="reform-filter-btn ${currentFilter === key ? 'active' : ''}" 
                                        onclick="setPathwayFilter('${key}')">${label}</button>
                            `).join('')}
                        </div>
                    ` : ''}
                </div>
                ${!collapsed ? `
                    <div class="reform-pathways-grid">
                        ${filtered.map(pathway => `
                            <div class="reform-pathway-card">
                                <div class="reform-pathway-header">
                                    <span class="reform-pathway-icon">${pathway.icon}</span>
                                    <div>
                                        <p class="reform-pathway-title">${pathway.title}</p>
                                        <p class="reform-pathway-description">${pathway.description}</p>
                                    </div>
                                </div>
                                <div class="reform-pathway-progress">
                                    <div class="reform-progress-bar">
                                        <div class="reform-progress-fill" style="width: ${pathway.progress}%"></div>
                                    </div>
                                    <p class="reform-progress-text">
                                        Level ${pathway.level} • ${pathway.progress}% complete
                                    </p>
                                </div>
                                <ul class="reform-pathway-steps">
                                    ${pathway.steps.map(step => `
                                        <li class="reform-pathway-step">
                                            <span class="reform-step-dot ${step.completed ? 'completed' : ''}"></span>
                                            <div>
                                                <p class="${step.completed ? 'completed' : ''}">${step.title}</p>
                                                <p class="reform-step-description">${step.description}</p>
                                            </div>
                                        </li>
                                    `).join('')}
                                </ul>
                                <div class="reform-pathway-reward">
                                    Reward: ${pathway.reward}
                                </div>
                            </div>
                        `).join('')}
                    </div>
                ` : ''}
            </div>
        `;
    }

    window.setPathwayFilter = (filter) => {
        currentFilter = filter;
        render();
    };

    window.togglePathwaysCollapse = () => {
        collapsed = !collapsed;
        render();
    };

    render();
}

/**
 * Render Reform Badges
 */
function renderReformBadges(badges) {
    const container = document.getElementById('reform-badges');
    if (!container) return;

    container.innerHTML = `
        <div class="reform-badges-section">
            <div class="reform-section-header">
                <div>
                    <p class="reform-section-label">Badges & tokens</p>
                    <h2 class="reform-section-title">Gamify your Reform journey</h2>
                </div>
                <button class="reform-link-btn">View all rewards</button>
            </div>
            <div class="reform-badges-grid">
                ${badges.map(badge => `
                    <div class="reform-badge-card">
                        <div class="reform-badge-image">
                            <img src="${badge.imageUrl}" alt="${badge.title}" />
                        </div>
                        <div class="reform-badge-info">
                            <div class="reform-badge-header">
                                <p class="reform-badge-title">${badge.title}</p>
                                <span class="reform-badge-status ${badge.status}">
                                    ${badge.status === 'unlocked' ? 'Unlocked' : 'In progress'}
                                </span>
                            </div>
                            <p class="reform-badge-description">${badge.description}</p>
                            <p class="reform-badge-reward">${badge.reward}</p>
                        </div>
                    </div>
                `).join('')}
            </div>
        </div>
    `;
}

/**
 * Render Reform Social Feed
 */
function renderReformSocialFeed(feed) {
    const container = document.getElementById('reform-social-feed');
    if (!container) return;

    container.innerHTML = `
        <div class="reform-social-feed-section">
            <div class="reform-section-header">
                <div>
                    <p class="reform-section-label">Cross-platform feed</p>
                    <h2 class="reform-section-title">Your Reform signal</h2>
                </div>
                <button class="reform-link-btn">View missions</button>
            </div>
            <div class="reform-feed-list">
                ${feed.map(item => `
                    <div class="reform-feed-item">
                        <div class="reform-feed-icon">${item.source[0]}</div>
                        <div class="reform-feed-content">
                            <div class="reform-feed-header">
                                <p class="reform-feed-title">${item.title}</p>
                                <span class="reform-feed-time">${item.timestamp}</span>
                            </div>
                            <p class="reform-feed-description">${item.description}</p>
                            <div class="reform-feed-meta">
                                <span class="reform-feed-source">${item.source}</span>
                                ${item.valueChange ? `<span class="reform-feed-points">${item.valueChange}</span>` : ''}
                            </div>
                        </div>
                    </div>
                `).join('')}
            </div>
        </div>
    `;
}

/**
 * Render Reform Leaderboard
 */
function renderReformLeaderboard(leaders) {
    const container = document.getElementById('reform-leaderboard');
    if (!container) return;

    container.innerHTML = `
        <div class="reform-leaderboard-section">
            <div class="reform-section-header">
                <div>
                    <p class="reform-section-label">Leaderboard</p>
                    <h2 class="reform-section-title">Signal champions</h2>
                </div>
                <button class="reform-link-btn">View full board</button>
            </div>
            <div class="reform-leaderboard-list">
                ${leaders.map((leader, index) => `
                    <div class="reform-leaderboard-entry">
                        <div class="reform-leaderboard-user">
                            <span class="reform-leaderboard-rank">${index + 1}</span>
                            <div class="reform-leaderboard-avatar" style="background-color: ${leader.avatarColor}">
                                ${leader.name[0]}
                            </div>
                            <div>
                                <p class="reform-leaderboard-name">${leader.name}</p>
                                <p class="reform-leaderboard-region">${leader.region}</p>
                            </div>
                        </div>
                        <div class="reform-leaderboard-platform">
                            ${leader.providerFocus}
                        </div>
                        <div class="reform-leaderboard-stats">
                            <p class="reform-leaderboard-points">${leader.points.toLocaleString()} pts</p>
                            <p class="reform-leaderboard-detail">
                                ${leader.missionsCompleted} missions • ${leader.streak} week streak
                            </p>
                        </div>
                    </div>
                `).join('')}
            </div>
        </div>
    `;
}

/**
 * Render Reform Roles
 */
function renderReformRoles(roles) {
    const container = document.getElementById('reform-roles');
    if (!container) return;

    container.innerHTML = `
        <div class="reform-roles-section">
            <div class="reform-section-header">
                <div>
                    <p class="reform-section-label">Role progression</p>
                    <h2 class="reform-section-title">Unlock new permissions</h2>
                </div>
                <button class="reform-link-btn">View requirements</button>
            </div>
            <div class="reform-roles-list">
                ${roles.map(role => `
                    <div class="reform-role-card">
                        <div class="reform-role-header">
                            <div>
                                <p class="reform-role-title">${role.title}</p>
                                <p class="reform-role-description">${role.description}</p>
                            </div>
                            <span class="reform-role-status ${role.achieved ? 'achieved' : 'locked'}">
                                ${role.achieved ? '✓ Active' : '○ Locked'}
                            </span>
                        </div>
                        <div class="reform-role-details">
                            <div>
                                <p class="reform-role-detail-label">Requirements</p>
                                <ul class="reform-role-requirements">
                                    ${role.requirements.map(req => `
                                        <li>
                                            <span class="reform-requirement-dot ${role.achieved ? 'achieved' : ''}"></span>
                                            ${req}
                                        </li>
                                    `).join('')}
                                </ul>
                            </div>
                            <div>
                                <p class="reform-role-detail-label">Permissions</p>
                                <ul class="reform-role-permissions">
                                    ${role.permissions.map(perm => `
                                        <li>${perm}</li>
                                    `).join('')}
                                </ul>
                            </div>
                        </div>
                    </div>
                `).join('')}
            </div>
        </div>
    `;
}

/**
 * Render Reform News
 */
function renderReformNews(news) {
    const container = document.getElementById('reform-news');
    if (!container) return;

    const channelColors = {
        Field: 'bg-emerald-500/20 text-emerald-300',
        Telegram: 'bg-purple-500/20 text-purple-200',
        YouTube: 'bg-red-500/20 text-red-200',
        X: 'bg-gray-200/20 text-white'
    };

    container.innerHTML = `
        <div class="reform-news-section">
            <div class="reform-section-header">
                <div>
                    <p class="reform-section-label">Movement intel</p>
                    <h2 class="reform-section-title">Latest content & updates</h2>
                </div>
                <button class="reform-link-btn">View newsroom</button>
            </div>
            <div class="reform-news-list">
                ${news.map(item => `
                    <a href="${item.link}" target="_blank" class="reform-news-item">
                        <div class="reform-news-channel ${channelColors[item.channel] || ''}">
                            ${item.channel}
                        </div>
                        <div class="reform-news-content">
                            <div class="reform-news-header">
                                <p class="reform-news-title">${item.title}</p>
                                <span class="reform-news-arrow">→</span>
                            </div>
                            <p class="reform-news-summary">${item.summary}</p>
                            <p class="reform-news-time">${item.publishedAt}</p>
                        </div>
                    </a>
                `).join('')}
            </div>
        </div>
    `;
}

/**
 * Render Reform Events
 */
function renderReformEvents(events) {
    const container = document.getElementById('reform-events');
    if (!container) return;

    container.innerHTML = `
        <div class="reform-events-section">
            <div class="reform-section-header">
                <div>
                    <p class="reform-section-label">Local missions</p>
                    <h2 class="reform-section-title">Upcoming events</h2>
                </div>
                <button class="reform-link-btn">See calendar</button>
            </div>
            <div class="reform-events-list">
                ${events.map(event => `
                    <div class="reform-event-card">
                        <div class="reform-event-header">
                            <p class="reform-event-title">${event.title}</p>
                            <span class="reform-event-status ${event.status}">
                                ${event.status}
                            </span>
                        </div>
                        <div class="reform-event-details">
                            <span class="reform-event-location">📍 ${event.location}</span>
                            <span>${event.time}</span>
                            <span class="reform-event-rsvps">👥 ${event.rsvps} RSVPs</span>
                        </div>
                    </div>
                `).join('')}
            </div>
        </div>
    `;
}

/**
 * Render Reform Notifications
 */
function renderReformNotifications(notifications, variant = 'inline') {
    const containerId = variant === 'floating' ? 'reform-notifications-floating' : 'reform-notifications';
    const container = document.getElementById(containerId);
    if (!container) return;

    if (variant === 'floating') {
        container.innerHTML = `
            <div class="reform-notifications-floating">
                ${notifications.slice(0, 3).map(notif => `
                    <div class="reform-notification-card ${notif.priority === 'high' ? 'high-priority' : ''}">
                        <div class="reform-notification-header">
                            <div class="reform-notification-type">
                                🔔 ${notif.priority === 'high' ? 'Urgent' : 'Update'}
                            </div>
                            <span class="reform-notification-time">${notif.time}</span>
                        </div>
                        <p class="reform-notification-title">${notif.title}</p>
                        <p class="reform-notification-description">${notif.description}</p>
                    </div>
                `).join('')}
            </div>
        `;
    } else {
        container.innerHTML = `
            <div class="reform-notifications-section">
                <div class="reform-section-header">
                    <div>
                        <p class="reform-section-label">Notifications</p>
                        <h2 class="reform-section-title">Stay in sync</h2>
                    </div>
                </div>
                <div class="reform-notifications-list">
                    ${notifications.map(notif => `
                        <div class="reform-notification-card ${notif.priority === 'high' ? 'high-priority' : ''}">
                            <div class="reform-notification-header">
                                <p class="reform-notification-title">${notif.title}</p>
                                <span class="reform-notification-time">${notif.time}</span>
                            </div>
                            <p class="reform-notification-description">${notif.description}</p>
                        </div>
                    `).join('')}
                </div>
            </div>
        `;
    }
}

/**
 * Render Reform Contacts
 */
function renderReformContacts(contacts) {
    const container = document.getElementById('reform-contacts');
    if (!container) return;

    container.innerHTML = `
        <div class="reform-contacts-section">
            <div class="reform-section-header">
                <div>
                    <p class="reform-section-label">Quick contacts</p>
                    <h2 class="reform-section-title">Key connections</h2>
                </div>
            </div>
            <div class="reform-contacts-list">
                ${contacts.map(contact => `
                    <div class="reform-contact-card ${contact.priority === 'high' ? 'high-priority' : ''}">
                        <div class="reform-contact-info">
                            <p class="reform-contact-name">${contact.name}</p>
                            <p class="reform-contact-platform">${contact.platform} • ${contact.username}</p>
                        </div>
                        <div class="reform-contact-meta">
                            <span class="reform-contact-last-seen">${contact.lastSeen}</span>
                            <button class="reform-contact-action">${contact.action}</button>
                        </div>
                    </div>
                `).join('')}
            </div>
        </div>
    `;
}

/**
 * Render Reform HQ Highlights
 */
function renderReformHqHighlights(hq) {
    const container = document.getElementById('reform-hq-highlights');
    if (!container) return;

    container.innerHTML = `
        <div class="reform-hq-section">
            <div class="reform-section-header">
                <div>
                    <p class="reform-section-label">HQ Updates</p>
                    <h2 class="reform-section-title">From headquarters</h2>
                </div>
            </div>
            <div class="reform-hq-list">
                ${hq.map(item => `
                    <a href="${item.link}" target="_blank" class="reform-hq-item">
                        <div class="reform-hq-category">${item.category}</div>
                        <p class="reform-hq-title">${item.title}</p>
                        <p class="reform-hq-summary">${item.summary}</p>
                        <p class="reform-hq-time">${item.publishedAt}</p>
                    </a>
                `).join('')}
            </div>
        </div>
    `;
}

/**
 * Load Reform Dashboard
 */
async function loadReformDashboard() {
    const authData = localStorage.getItem('oasis_auth');
    if (!authData) {
        return;
    }

    try {
        const auth = JSON.parse(authData);
        const avatar = auth.avatar || {};

        // Build data
        const data = buildReformHubData(avatar);

        // Render all components
        renderReformHero(avatar, data);
        renderReformPathways(data.pathways);
        renderReformSocialFeed(data.feed);
        renderReformBadges(data.badges);
        renderReformLeaderboard(data.leaderboard);
        renderReformRoles(data.roles);
        renderReformNews(data.news);
        renderReformEvents(data.events);
        renderReformNotifications(data.notifications, 'floating');
        renderReformNotifications(data.notifications, 'inline');
        renderReformContacts(data.contacts);
        renderReformHqHighlights(data.hq);

    } catch (error) {
        console.error('Error loading Reform dashboard:', error);
    }
}
