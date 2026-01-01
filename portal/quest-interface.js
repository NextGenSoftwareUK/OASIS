/**
 * Quest Interface - Visual Quest Browser and Management
 */

const questInterfaceState = {
    baseUrl: window.location.origin,
    authToken: null,
    avatarId: null,
    quests: {
        available: [],
        active: [],
        completed: []
    },
    currentQuest: null,
    view: 'browser' // 'browser', 'detail', 'create'
};

/**
 * Show Quest Browser
 */
function showQuestBrowser() {
    questInterfaceState.view = 'browser';
    const container = document.getElementById('star-dashboard-content') || document.getElementById('quest-interface-container');
    if (!container) {
        // Create container if it doesn't exist
        const starTab = document.getElementById('tab-star');
        if (starTab) {
            const newContainer = document.createElement('div');
            newContainer.id = 'quest-interface-container';
            starTab.appendChild(newContainer);
            renderQuestBrowser(newContainer);
            return;
        }
        console.error('Quest interface container not found');
        return;
    }
    
    container.innerHTML = renderQuestBrowser();
    loadQuests();
}

/**
 * Render Quest Browser
 */
function renderQuestBrowser(container) {
    const html = `
        <div class="portal-section">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 2rem;">
                <div>
                    <h2 style="
                        font-size: 1.5rem;
                        font-weight: 300;
                        color: var(--text-primary);
                        margin-bottom: 0.5rem;
                    ">Quest Browser</h2>
                    <p style="
                        font-size: 0.875rem;
                        color: var(--text-secondary);
                    ">Browse, start, and track your quests</p>
                </div>
                <button class="btn-primary" onclick="showQuestCreationInterface()">
                    Create Quest
                </button>
            </div>

            <div style="display: flex; gap: 1rem; margin-bottom: 2rem; border-bottom: 1px solid var(--border-color);">
                <button 
                    class="quest-tab-btn"
                    data-quest-tab="available"
                    onclick="switchQuestTab('available')"
                    style="padding: 0.75rem 1.5rem; border: none; background: none; color: var(--text-secondary); cursor: pointer; border-bottom: 2px solid transparent; font-size: 0.875rem;"
                >
                    Available (<span id="quest-count-available">0</span>)
                </button>
                <button 
                    class="quest-tab-btn"
                    data-quest-tab="active"
                    onclick="switchQuestTab('active')"
                    style="padding: 0.75rem 1.5rem; border: none; background: none; color: var(--text-secondary); cursor: pointer; border-bottom: 2px solid transparent; font-size: 0.875rem;"
                >
                    Active (<span id="quest-count-active">0</span>)
                </button>
                <button 
                    class="quest-tab-btn"
                    data-quest-tab="completed"
                    onclick="switchQuestTab('completed')"
                    style="padding: 0.75rem 1.5rem; border: none; background: none; color: var(--text-secondary); cursor: pointer; border-bottom: 2px solid transparent; font-size: 0.875rem;"
                >
                    Completed (<span id="quest-count-completed">0</span>)
                </button>
            </div>

            <div id="quest-list-container">
                ${renderQuestList('available')}
            </div>
        </div>
    `;

    if (container) {
        container.innerHTML = html;
    } else {
        return html;
    }
}

/**
 * Switch Quest Tab
 */
function switchQuestTab(tab) {
    // Update tab buttons
    document.querySelectorAll('.quest-tab-btn').forEach(btn => {
        btn.style.color = 'var(--text-secondary)';
        btn.style.borderBottomColor = 'transparent';
    });
    const activeBtn = document.querySelector(`[data-quest-tab="${tab}"]`);
    if (activeBtn) {
        activeBtn.style.color = 'var(--text-primary)';
        activeBtn.style.borderBottomColor = 'var(--text-primary)';
    }

    // Update quest list
    const container = document.getElementById('quest-list-container');
    if (container) {
        container.innerHTML = renderQuestList(tab);
    }
}

/**
 * Render Quest List
 */
function renderQuestList(tab) {
    const quests = questInterfaceState.quests[tab] || [];

    if (quests.length === 0) {
        return `
            <div class="empty-state">
                <p class="empty-state-text">
                    ${tab === 'available' ? 'No available quests. Create one to get started!' : 
                      tab === 'active' ? 'No active quests. Start a quest to begin your journey!' : 
                      'No completed quests yet.'}
                </p>
            </div>
        `;
    }

    return quests.map(quest => renderQuestCard(quest, tab)).join('');
}

/**
 * Render Quest Card
 */
function renderQuestCard(quest, status) {
    const statusColors = {
        available: 'rgba(59, 130, 246, 0.1)',
        active: 'rgba(168, 85, 247, 0.1)',
        completed: 'rgba(34, 197, 94, 0.1)'
    };

    const statusLabels = {
        available: 'Available',
        active: 'In Progress',
        completed: 'Completed'
    };

    const progress = quest.progress || 0;

    return `
        <div class="portal-card" style="
            padding: 1.5rem;
            margin-bottom: 1rem;
            cursor: pointer;
            border-left: 4px solid ${statusColors[status] || 'var(--border-color)'};
            transition: transform 0.2s, box-shadow 0.2s;
        " 
        onclick="showQuestDetail('${quest.id || ''}')"
        onmouseover="this.style.transform='translateX(4px)'; this.style.boxShadow='0 8px 16px rgba(0,0,0,0.3)'"
        onmouseout="this.style.transform=''; this.style.boxShadow=''">
            <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 1rem;">
                <div style="flex: 1;">
                    <div style="display: flex; align-items: center; gap: 0.75rem; margin-bottom: 0.5rem;">
                        <h3 style="
                            font-size: 1.125rem;
                            font-weight: 500;
                            color: var(--text-primary);
                            margin: 0;
                        ">${escapeHtml(quest.name || 'Unnamed Quest')}</h3>
                        <span style="
                            font-size: 0.75rem;
                            padding: 0.25rem 0.5rem;
                            border-radius: 4px;
                            background: ${statusColors[status] || 'rgba(255,255,255,0.05)'};
                            color: var(--text-primary);
                            font-weight: 500;
                        ">${statusLabels[status]}</span>
                    </div>
                    <p style="
                        font-size: 0.875rem;
                        color: var(--text-secondary);
                        line-height: 1.6;
                        margin: 0;
                    ">${escapeHtml((quest.description || '').substring(0, 150))}${(quest.description || '').length > 150 ? '...' : ''}</p>
                </div>
            </div>

            ${status === 'active' ? renderQuestProgress(quest) : ''}

            <div style="display: flex; gap: 0.75rem; margin-top: 1rem; flex-wrap: wrap;">
                <div style="
                    font-size: 0.75rem;
                    color: var(--text-tertiary);
                    display: flex;
                    align-items: center;
                    gap: 0.5rem;
                ">
                    <span>Type:</span>
                    <span style="color: var(--text-secondary); font-weight: 500;">${quest.questType || 'MainQuest'}</span>
                </div>
                <div style="
                    font-size: 0.75rem;
                    color: var(--text-tertiary);
                    display: flex;
                    align-items: center;
                    gap: 0.5rem;
                ">
                    <span>Difficulty:</span>
                    <span style="color: var(--text-secondary); font-weight: 500;">${quest.difficulty || 'Easy'}</span>
                </div>
                ${quest.rewardXP ? `
                <div style="
                    font-size: 0.75rem;
                    color: var(--text-tertiary);
                    display: flex;
                    align-items: center;
                    gap: 0.5rem;
                ">
                    <span>XP Reward:</span>
                    <span style="color: var(--text-secondary); font-weight: 500;">${quest.rewardXP}</span>
                </div>
                ` : ''}
                ${quest.rewardKarma ? `
                <div style="
                    font-size: 0.75rem;
                    color: var(--text-tertiary);
                    display: flex;
                    align-items: center;
                    gap: 0.5rem;
                ">
                    <span>Karma Reward:</span>
                    <span style="color: var(--text-secondary); font-weight: 500;">${quest.rewardKarma}</span>
                </div>
                ` : ''}
            </div>

            <div style="display: flex; gap: 0.75rem; margin-top: 1rem;">
                ${status === 'available' ? `
                <button class="btn-primary" style="font-size: 0.875rem; padding: 0.5rem 1rem;" onclick="event.stopPropagation(); startQuest('${quest.id || ''}')">
                    Start Quest
                </button>
                ` : status === 'active' ? `
                <button class="btn-secondary" style="font-size: 0.875rem; padding: 0.5rem 1rem;" onclick="event.stopPropagation(); showQuestDetail('${quest.id || ''}')">
                    View Progress
                </button>
                ` : `
                <button class="btn-secondary" style="font-size: 0.875rem; padding: 0.5rem 1rem;" onclick="event.stopPropagation(); showQuestDetail('${quest.id || ''}')">
                    View Details
                </button>
                `}
            </div>
        </div>
    `;
}

/**
 * Render Quest Progress
 */
function renderQuestProgress(quest) {
    const progress = quest.progress || 0;
    const objectives = quest.objectives || [];
    const completedObjectives = objectives.filter(obj => obj.completed).length;
    const totalObjectives = objectives.length || 1;

    return `
        <div style="margin-top: 1rem;">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem;">
                <span style="font-size: 0.75rem; color: var(--text-tertiary);">Progress</span>
                <span style="font-size: 0.75rem; color: var(--text-secondary); font-weight: 500;">
                    ${completedObjectives} / ${totalObjectives} objectives
                </span>
            </div>
            <div style="
                width: 100%;
                height: 8px;
                background: rgba(255, 255, 255, 0.05);
                border-radius: 4px;
                overflow: hidden;
            ">
                <div style="
                    width: ${progress}%;
                    height: 100%;
                    background: linear-gradient(90deg, rgba(168, 85, 247, 0.8), rgba(236, 72, 153, 0.8));
                    transition: width 0.3s;
                "></div>
            </div>
            ${objectives.length > 0 ? `
            <div style="margin-top: 0.75rem; display: flex; flex-direction: column; gap: 0.5rem;">
                ${objectives.slice(0, 3).map(obj => `
                <div style="display: flex; align-items: center; gap: 0.5rem; font-size: 0.8125rem;">
                    <span style="color: ${obj.completed ? 'rgba(34, 197, 94, 1)' : 'var(--text-tertiary)'};">
                        ${obj.completed ? '✓' : '○'}
                    </span>
                    <span style="color: var(--text-secondary); ${obj.completed ? 'text-decoration: line-through; opacity: 0.6;' : ''}">
                        ${escapeHtml(obj.description || obj.text || 'Objective')}
                    </span>
                </div>
                `).join('')}
                ${objectives.length > 3 ? `
                <div style="font-size: 0.75rem; color: var(--text-tertiary); padding-left: 1.5rem;">
                    +${objectives.length - 3} more objectives
                </div>
                ` : ''}
            </div>
            ` : ''}
        </div>
    `;
}

/**
 * Show Quest Detail
 */
function showQuestDetail(questId) {
    questInterfaceState.currentQuest = questInterfaceState.quests.active.find(q => q.id === questId) ||
                                        questInterfaceState.quests.available.find(q => q.id === questId) ||
                                        questInterfaceState.quests.completed.find(q => q.id === questId);

    const container = document.getElementById('star-dashboard-content') || document.getElementById('quest-interface-container');
    if (!container) return;

    container.innerHTML = renderQuestDetail();
    loadQuestDetail(questId);
}

/**
 * Render Quest Detail
 */
function renderQuestDetail() {
    const quest = questInterfaceState.currentQuest || {};

    return `
        <div class="portal-section">
            <div style="margin-bottom: 2rem;">
                <button 
                    class="btn-text" 
                    onclick="showQuestBrowser()"
                    style="margin-bottom: 1rem; font-size: 0.875rem;"
                >
                    ← Back to Quests
                </button>
                <h2 style="
                    font-size: 1.5rem;
                    font-weight: 300;
                    color: var(--text-primary);
                    margin-bottom: 0.5rem;
                ">${escapeHtml(quest.name || 'Quest Details')}</h2>
            </div>

            <div style="display: grid; grid-template-columns: 2fr 1fr; gap: 2rem;">
                <div>
                    ${renderQuestDetailMain(quest)}
                </div>
                <div>
                    ${renderQuestDetailSidebar(quest)}
                </div>
            </div>
        </div>
    `;
}

/**
 * Render Quest Detail Main
 */
function renderQuestDetailMain(quest) {
    const objectives = quest.objectives || [];
    const progress = quest.progress || 0;

    return `
        <div class="portal-card" style="margin-bottom: 1.5rem; padding: 2rem;">
            <h3 style="font-size: 1rem; font-weight: 500; color: var(--text-primary); margin-bottom: 1rem;">Description</h3>
            <p style="
                font-size: 0.875rem;
                color: var(--text-secondary);
                line-height: 1.8;
                margin-bottom: 2rem;
            ">${escapeHtml(quest.description || 'No description provided.')}</p>

            <h3 style="font-size: 1rem; font-weight: 500; color: var(--text-primary); margin-bottom: 1rem;">Objectives</h3>
            <div style="display: flex; flex-direction: column; gap: 1rem;">
                ${objectives.length > 0 ? objectives.map((obj, index) => `
                <div style="
                    padding: 1rem;
                    background: rgba(255, 255, 255, 0.02);
                    border: 1px solid var(--border-color);
                    border-radius: 0.5rem;
                    ${obj.completed ? 'opacity: 0.6;' : ''}
                ">
                    <div style="display: flex; align-items: start; gap: 1rem;">
                        <div style="
                            width: 24px;
                            height: 24px;
                            border-radius: 50%;
                            background: ${obj.completed ? 'rgba(34, 197, 94, 0.2)' : 'rgba(255, 255, 255, 0.05)'};
                            border: 2px solid ${obj.completed ? 'rgba(34, 197, 94, 1)' : 'var(--border-color)'};
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            flex-shrink: 0;
                            margin-top: 2px;
                        ">
                            ${obj.completed ? '✓' : index + 1}
                        </div>
                        <div style="flex: 1;">
                            <div style="
                                font-size: 0.875rem;
                                font-weight: 500;
                                color: var(--text-primary);
                                margin-bottom: 0.25rem;
                                ${obj.completed ? 'text-decoration: line-through;' : ''}
                            ">${escapeHtml(obj.description || obj.text || `Objective ${index + 1}`)}</div>
                            ${obj.progress !== undefined ? `
                            <div style="margin-top: 0.5rem;">
                                <div style="
                                    width: 100%;
                                    height: 4px;
                                    background: rgba(255, 255, 255, 0.05);
                                    border-radius: 2px;
                                    overflow: hidden;
                                ">
                                    <div style="
                                        width: ${obj.progress || 0}%;
                                        height: 100%;
                                        background: rgba(168, 85, 247, 0.8);
                                    "></div>
                                </div>
                            </div>
                            ` : ''}
                        </div>
                    </div>
                </div>
                `).join('') : `
                <div class="empty-state">
                    <p class="empty-state-text">No objectives defined for this quest.</p>
                </div>
                `}
            </div>
        </div>
    `;
}

/**
 * Render Quest Detail Sidebar
 */
function renderQuestDetailSidebar(quest) {
    return `
        <div class="portal-card" style="padding: 1.5rem; margin-bottom: 1.5rem;">
            <h3 style="font-size: 1rem; font-weight: 500; color: var(--text-primary); margin-bottom: 1rem;">Quest Info</h3>
            <div style="display: flex; flex-direction: column; gap: 1rem;">
                <div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.25rem;">Type</div>
                    <div style="font-size: 0.875rem; color: var(--text-primary); font-weight: 500;">${quest.questType || 'MainQuest'}</div>
                </div>
                <div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.25rem;">Difficulty</div>
                    <div style="font-size: 0.875rem; color: var(--text-primary); font-weight: 500;">${quest.difficulty || 'Easy'}</div>
                </div>
                ${quest.rewardXP ? `
                <div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.25rem;">XP Reward</div>
                    <div style="font-size: 0.875rem; color: var(--text-primary); font-weight: 500;">${quest.rewardXP}</div>
                </div>
                ` : ''}
                ${quest.rewardKarma ? `
                <div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.25rem;">Karma Reward</div>
                    <div style="font-size: 0.875rem; color: var(--text-primary); font-weight: 500;">${quest.rewardKarma}</div>
                </div>
                ` : ''}
            </div>
        </div>

        <div class="portal-card" style="padding: 1.5rem;">
            <button class="btn-primary" style="width: 100%;" onclick="startQuest('${quest.id || ''}')">
                Start Quest
            </button>
        </div>
    `;
}

/**
 * Load Quests
 */
async function loadQuests() {
    try {
        const authData = localStorage.getItem('oasis_auth');
        if (authData) {
            const auth = JSON.parse(authData);
            questInterfaceState.authToken = auth.token;
            questInterfaceState.avatarId = auth.avatarId || auth.avatar?.id;
        }

        // Placeholder - would load from API
        questInterfaceState.quests = {
            available: [],
            active: [],
            completed: []
        };

        updateQuestCounts();
    } catch (error) {
        console.error('Error loading quests:', error);
    }
}

/**
 * Load Quest Detail
 */
async function loadQuestDetail(questId) {
    try {
        // Placeholder - would load from API
        // const response = await fetch(`${questInterfaceState.baseUrl}/api/quests/${questId}`, {
        //     headers: {
        //         'Authorization': questInterfaceState.authToken ? `Bearer ${questInterfaceState.authToken}` : ''
        //     }
        // });
        // const data = await response.json();
        // questInterfaceState.currentQuest = data.result;
    } catch (error) {
        console.error('Error loading quest detail:', error);
    }
}

/**
 * Start Quest
 */
async function startQuest(questId) {
    try {
        const response = await fetch(`${questInterfaceState.baseUrl}/api/quests/${questId}/start`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': questInterfaceState.authToken ? `Bearer ${questInterfaceState.authToken}` : ''
            },
            body: JSON.stringify({
                avatarId: questInterfaceState.avatarId
            })
        });

        if (response.ok) {
            // Reload quests
            await loadQuests();
            showQuestBrowser();
        } else {
            throw new Error('Failed to start quest');
        }
    } catch (error) {
        console.error('Error starting quest:', error);
        alert('Failed to start quest: ' + error.message);
    }
}

/**
 * Show Quest Creation Interface
 * Note: Actual implementation is in quest-builder.js
 * This is just a placeholder - quest-builder.js will override it
 */
function showQuestCreationInterface() {
    // This will be overridden by quest-builder.js
    console.log('Quest creation interface - should be handled by quest-builder.js');
}

/**
 * Update Quest Counts
 */
function updateQuestCounts() {
    const availableEl = document.getElementById('quest-count-available');
    if (availableEl) availableEl.textContent = questInterfaceState.quests.available.length;
    const activeEl = document.getElementById('quest-count-active');
    if (activeEl) activeEl.textContent = questInterfaceState.quests.active.length;
    const completedEl = document.getElementById('quest-count-completed');
    if (completedEl) completedEl.textContent = questInterfaceState.quests.completed.length;
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

// Export functions to window
if (typeof window !== 'undefined') {
    window.showQuestBrowser = showQuestBrowser;
    window.switchQuestTab = switchQuestTab;
    window.showQuestDetail = showQuestDetail;
    window.startQuest = startQuest;
    window.showQuestCreationInterface = showQuestCreationInterface;
}

