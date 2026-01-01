/**
 * Mission Interface - Visual Mission Tracker
 */

const missionInterfaceState = {
    baseUrl: window.location.origin,
    authToken: null,
    avatarId: null,
    missions: [],
    currentMission: null,
    view: 'tracker' // 'tracker', 'detail', 'create'
};

/**
 * Show Mission Tracker
 */
function showMissionTracker() {
    missionInterfaceState.view = 'tracker';
    const container = document.getElementById('star-dashboard-content') || document.getElementById('mission-interface-container');
    if (!container) {
        const starTab = document.getElementById('tab-star');
        if (starTab) {
            const newContainer = document.createElement('div');
            newContainer.id = 'mission-interface-container';
            starTab.appendChild(newContainer);
            container = newContainer;
        } else {
            console.error('Mission interface container not found');
            return;
        }
    }
    
    container.innerHTML = renderMissionTracker();
    loadMissions();
}

/**
 * Render Mission Tracker
 */
function renderMissionTracker() {
    return `
        <div class="portal-section">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 2rem;">
                <div>
                    <h2 style="
                        font-size: 1.5rem;
                        font-weight: 300;
                        color: var(--text-primary);
                        margin-bottom: 0.5rem;
                    ">Mission Tracker</h2>
                    <p style="
                        font-size: 0.875rem;
                        color: var(--text-secondary);
                    ">Track your story progression and mission objectives</p>
                </div>
                <button class="btn-primary" onclick="showMissionCreationInterface()">
                    Create Mission
                </button>
            </div>

            <div id="mission-list-container">
                ${renderMissionList()}
            </div>
        </div>
    `;
}

/**
 * Render Mission List
 */
function renderMissionList() {
    const missions = missionInterfaceState.missions || [];

    if (missions.length === 0) {
        return `
            <div class="empty-state">
                <p class="empty-state-text">No missions available. Create one to get started!</p>
            </div>
        `;
    }

    return missions.map(mission => renderMissionCard(mission)).join('');
}

/**
 * Render Mission Card with Progress Visualization
 */
function renderMissionCard(mission) {
    const chapters = mission.chapters || [];
    const hasChapters = chapters.length > 0;
    
    let quests, completedQuests, totalQuests;
    if (hasChapters) {
        quests = chapters.flatMap(ch => ch.quests || []);
    } else {
        quests = mission.quests || [];
    }
    completedQuests = quests.filter(q => q.status === 'completed' || q.completed).length;
    totalQuests = quests.length || 1;
    const progress = totalQuests > 0 ? (completedQuests / totalQuests) * 100 : 0;

    return `
        <div class="portal-card" style="
            padding: 1.5rem;
            margin-bottom: 1.5rem;
            cursor: pointer;
            border-left: 4px solid rgba(236, 72, 153, 0.3);
            transition: transform 0.2s, box-shadow 0.2s;
        " 
        onclick="showMissionDetail('${mission.id || ''}')"
        onmouseover="this.style.transform='translateX(4px)'; this.style.boxShadow='0 8px 16px rgba(0,0,0,0.3)'"
        onmouseout="this.style.transform=''; this.style.boxShadow=''">
            <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 1rem;">
                <div style="flex: 1;">
                    <h3 style="
                        font-size: 1.125rem;
                        font-weight: 500;
                        color: var(--text-primary);
                        margin: 0 0 0.5rem 0;
                    ">${escapeHtml(mission.name || 'Unnamed Mission')}</h3>
                    <p style="
                        font-size: 0.875rem;
                        color: var(--text-secondary);
                        line-height: 1.6;
                        margin: 0;
                    ">${escapeHtml((mission.description || '').substring(0, 150))}${(mission.description || '').length > 150 ? '...' : ''}</p>
                </div>
            </div>

            ${renderMissionProgressVisualization(mission, progress, completedQuests, totalQuests)}

            <div style="
                display: flex;
                gap: 0.75rem;
                margin-top: 1rem;
                flex-wrap: wrap;
                font-family: 'Courier New', monospace;
                font-size: 0.6875rem;
                color: var(--text-tertiary);
            ">
                <div style="
                    padding: 0.5rem;
                    background: rgba(0, 0, 0, 0.2);
                    border: 1px solid var(--border-color);
                    border-radius: 0.25rem;
                ">
                    TYPE: <span style="color: var(--text-secondary);">${mission.missionType || 'Easy'}</span>
                </div>
                <div style="
                    padding: 0.5rem;
                    background: rgba(0, 0, 0, 0.2);
                    border: 1px solid var(--border-color);
                    border-radius: 0.25rem;
                ">
                    QUESTS: <span style="color: var(--text-secondary);">[${completedQuests}/${totalQuests}]</span>
                </div>
            </div>
        </div>
    `;
}

/**
 * Generate ASCII progress bar
 */
function generateASCIIProgressBar(progress, width = 40) {
    const filled = Math.round((progress / 100) * width);
    const empty = width - filled;
    const bar = '█'.repeat(filled) + '░'.repeat(empty);
    return bar;
}

/**
 * Render Mission Progress Visualization
 */
function renderMissionProgressVisualization(mission, progress, completedQuests, totalQuests) {
    const quests = mission.quests || [];
    const chapters = mission.chapters || [];
    const hasChapters = chapters.length > 0;
    const asciiBar = generateASCIIProgressBar(progress, 30);

    return `
        <div style="margin-top: 1rem;">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem;">
                <span style="font-size: 0.75rem; color: var(--text-tertiary); text-transform: uppercase; letter-spacing: 0.05em;">Story Progress</span>
                <span style="font-size: 0.875rem; color: var(--text-primary); font-weight: 500; font-family: monospace;">
                    ${Math.round(progress)}%
                </span>
            </div>
            
            <!-- ASCII Progress Bar -->
            <div style="
                font-family: 'Courier New', monospace;
                font-size: 0.75rem;
                color: var(--text-primary);
                background: rgba(0, 0, 0, 0.3);
                padding: 0.5rem;
                border: 1px solid var(--border-color);
                border-radius: 0.25rem;
                margin-bottom: 1rem;
                letter-spacing: 0.05em;
                line-height: 1.2;
            ">
                <div style="color: rgba(236, 72, 153, 0.9);">${asciiBar}</div>
                <div style="font-size: 0.625rem; color: var(--text-tertiary); margin-top: 0.25rem; text-align: right;">
                    [${completedQuests}/${totalQuests}]
                </div>
            </div>

            <!-- Quest Timeline Visualization -->
            ${hasChapters ? renderChapterTimeline(chapters) : (quests.length > 0 ? renderQuestTimeline(quests) : '')}
        </div>
    `;
}

/**
 * Show Mission Detail
 */
function showMissionDetail(missionId) {
    missionInterfaceState.currentMission = missionInterfaceState.missions.find(m => m.id === missionId);

    const container = document.getElementById('star-dashboard-content') || document.getElementById('mission-interface-container');
    if (!container) return;

    container.innerHTML = renderMissionDetail();
    loadMissionDetail(missionId);
}

/**
 * Render Mission Detail
 */
function renderMissionDetail() {
    const mission = missionInterfaceState.currentMission || {};
    const quests = mission.quests || [];
    const completedQuests = quests.filter(q => q.status === 'completed' || q.completed).length;
    const totalQuests = quests.length || 1;
    const progress = totalQuests > 0 ? (completedQuests / totalQuests) * 100 : 0;

    return `
        <div class="portal-section">
            <div style="margin-bottom: 2rem;">
                <button 
                    class="btn-text" 
                    onclick="showMissionTracker()"
                    style="margin-bottom: 1rem; font-size: 0.875rem;"
                >
                    ← Back to Missions
                </button>
                <h2 style="
                    font-size: 1.5rem;
                    font-weight: 300;
                    color: var(--text-primary);
                    margin-bottom: 0.5rem;
                ">${escapeHtml(mission.name || 'Mission Details')}</h2>
            </div>

            <div style="display: grid; grid-template-columns: 2fr 1fr; gap: 2rem;">
                <div>
                    ${renderMissionDetailMain(mission, quests, progress, completedQuests, totalQuests)}
                </div>
                <div>
                    ${renderMissionDetailSidebar(mission)}
                </div>
            </div>
        </div>
    `;
}

/**
 * Render Mission Detail Main
 */
function renderMissionDetailMain(mission, quests, progress, completedQuests, totalQuests) {
    const chapters = mission.chapters || [];
    const hasChapters = chapters.length > 0;
    
    return `
        <div class="portal-card" style="margin-bottom: 1.5rem; padding: 2rem;">
            <h3 style="font-size: 1rem; font-weight: 500; color: var(--text-primary); margin-bottom: 1rem;">Description</h3>
            <p style="
                font-size: 0.875rem;
                color: var(--text-secondary);
                line-height: 1.8;
                margin-bottom: 2rem;
            ">${escapeHtml(mission.description || 'No description provided.')}</p>

            ${hasChapters ? renderChaptersDetail(chapters) : renderQuestsDetail(quests, completedQuests, totalQuests)}
        </div>
    `;
}

/**
 * Render Chapters Detail
 */
function renderChaptersDetail(chapters) {
    return `
        <h3 style="font-size: 1rem; font-weight: 500; color: var(--text-primary); margin-bottom: 1rem;">
            Chapters
        </h3>
        <div style="display: flex; flex-direction: column; gap: 1.5rem;">
            ${chapters.map((chapter, chapterIndex) => {
                const chapterQuests = chapter.quests || [];
                const completedChapterQuests = chapterQuests.filter(q => q.status === 'completed' || q.completed).length;
                const totalChapterQuests = chapterQuests.length || 1;
                const chapterProgress = totalChapterQuests > 0 ? (completedChapterQuests / totalChapterQuests) * 100 : 0;
                
                return `
                    <div style="
                        padding: 1.5rem;
                        background: rgba(0, 0, 0, 0.3);
                        border: 1px solid var(--border-color);
                        border-radius: 0.5rem;
                        border-left: 4px solid rgba(236, 72, 153, 0.5);
                    ">
                        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">
                            <h4 style="
                                font-size: 0.9375rem;
                                font-weight: 500;
                                color: var(--text-primary);
                                margin: 0;
                            ">${escapeHtml(chapter.name || `${chapter.chapterDisplayName || 'Chapter'} ${chapterIndex + 1}`)}</h4>
                            <div style="
                                font-family: 'Courier New', monospace;
                                font-size: 0.75rem;
                                color: var(--text-secondary);
                            ">
                                [${completedChapterQuests}/${totalChapterQuests}]
                            </div>
                        </div>
                        
                        <div style="
                            font-family: 'Courier New', monospace;
                            font-size: 0.6875rem;
                            color: rgba(236, 72, 153, 0.9);
                            margin-bottom: 1rem;
                            letter-spacing: 0.05em;
                        ">
                            ${generateASCIIProgressBar(chapterProgress, 30)}
                        </div>
                        
                        <div style="display: flex; flex-direction: column; gap: 0.75rem;">
                            ${chapterQuests.length > 0 ? chapterQuests.map((quest, questIndex) => {
                                const isCompleted = quest.completed || quest.status === 'completed';
                                const isActive = quest.status === 'active';
                                const status = isCompleted ? '[OK]' : isActive ? '[>>]' : '[--]';
                                const statusColor = isCompleted ? 'rgba(34, 197, 94, 0.8)' : isActive ? 'rgba(168, 85, 247, 0.8)' : 'var(--text-tertiary)';
                                
                                return `
                                    <div style="
                                        padding: 0.75rem;
                                        background: rgba(255, 255, 255, 0.02);
                                        border: 1px solid var(--border-color);
                                        border-radius: 0.25rem;
                                        border-left: 2px solid ${statusColor};
                                    ">
                                        <div style="display: flex; align-items: start; gap: 0.75rem;">
                                            <span style="
                                                font-family: 'Courier New', monospace;
                                                font-size: 0.6875rem;
                                                color: ${statusColor};
                                                font-weight: 500;
                                                min-width: 35px;
                                            ">${status}</span>
                                            <div style="flex: 1;">
                                                <div style="
                                                    font-size: 0.8125rem;
                                                    font-weight: 500;
                                                    color: var(--text-primary);
                                                    margin-bottom: 0.25rem;
                                                    ${isCompleted ? 'text-decoration: line-through; opacity: 0.7;' : ''}
                                                ">${escapeHtml(quest.name || `Quest ${questIndex + 1}`)}</div>
                                                ${quest.description ? `
                                                <div style="
                                                    font-size: 0.75rem;
                                                    color: var(--text-secondary);
                                                    line-height: 1.4;
                                                ">${escapeHtml(quest.description)}</div>
                                                ` : ''}
                                            </div>
                                        </div>
                                    </div>
                                `;
                            }).join('') : `
                                <div style="
                                    font-size: 0.75rem;
                                    color: var(--text-tertiary);
                                    font-style: italic;
                                ">No quests in this chapter</div>
                            `}
                        </div>
                    </div>
                `;
            }).join('')}
        </div>
    `;
}

/**
 * Render Quests Detail
 */
function renderQuestsDetail(quests, completedQuests, totalQuests) {
    return `
        <h3 style="font-size: 1rem; font-weight: 500; color: var(--text-primary); margin-bottom: 1rem;">
            Quests (${completedQuests} / ${totalQuests} completed)
        </h3>
        <div style="display: flex; flex-direction: column; gap: 1rem;">
            ${quests.length > 0 ? quests.map((quest, index) => {
                const isCompleted = quest.completed || quest.status === 'completed';
                const isActive = quest.status === 'active';
                const status = isCompleted ? '[OK]' : isActive ? '[>>]' : '[--]';
                const statusColor = isCompleted ? 'rgba(34, 197, 94, 0.8)' : isActive ? 'rgba(168, 85, 247, 0.8)' : 'var(--text-tertiary)';
                
                return `
                    <div style="
                        padding: 1rem;
                        background: rgba(255, 255, 255, 0.02);
                        border: 1px solid var(--border-color);
                        border-radius: 0.5rem;
                        border-left: 4px solid ${statusColor};
                        ${isCompleted ? 'opacity: 0.7;' : ''}
                    ">
                        <div style="display: flex; align-items: start; gap: 1rem;">
                            <span style="
                                font-family: 'Courier New', monospace;
                                font-size: 0.75rem;
                                color: ${statusColor};
                                font-weight: 500;
                                min-width: 40px;
                            ">${status}</span>
                            <div style="flex: 1;">
                                <div style="
                                    font-size: 0.875rem;
                                    font-weight: 500;
                                    color: var(--text-primary);
                                    margin-bottom: 0.25rem;
                                    ${isCompleted ? 'text-decoration: line-through;' : ''}
                                ">${escapeHtml(quest.name || `Quest ${index + 1}`)}</div>
                                ${quest.description ? `
                                <div style="
                                    font-size: 0.8125rem;
                                    color: var(--text-secondary);
                                    line-height: 1.5;
                                ">${escapeHtml(quest.description)}</div>
                                ` : ''}
                            </div>
                        </div>
                    </div>
                `;
            }).join('') : `
                <div class="empty-state">
                    <p class="empty-state-text">No quests defined for this mission.</p>
                </div>
            `}
        </div>
    `;
}

/**
 * Render Mission Detail Sidebar
 */
function renderMissionDetailSidebar(mission) {
    const quests = mission.quests || [];
    const chapters = mission.chapters || [];
    const hasChapters = chapters.length > 0;
    
    let completedQuests, totalQuests, progress;
    if (hasChapters) {
        const allChapterQuests = chapters.flatMap(ch => ch.quests || []);
        completedQuests = allChapterQuests.filter(q => q.status === 'completed' || q.completed).length;
        totalQuests = allChapterQuests.length || 1;
    } else {
        completedQuests = quests.filter(q => q.status === 'completed' || q.completed).length;
        totalQuests = quests.length || 1;
    }
    progress = totalQuests > 0 ? (completedQuests / totalQuests) * 100 : 0;
    const asciiBar = generateASCIIProgressBar(progress, 25);

    return `
        <div class="portal-card" style="padding: 1.5rem; margin-bottom: 1.5rem;">
            <h3 style="font-size: 1rem; font-weight: 500; color: var(--text-primary); margin-bottom: 1rem;">Mission Info</h3>
            <div style="display: flex; flex-direction: column; gap: 1.5rem;">
                <div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem; text-transform: uppercase; letter-spacing: 0.05em;">Type</div>
                    <div style="
                        font-family: 'Courier New', monospace;
                        font-size: 0.875rem;
                        color: var(--text-primary);
                        font-weight: 500;
                        padding: 0.5rem;
                        background: rgba(0, 0, 0, 0.3);
                        border: 1px solid var(--border-color);
                        border-radius: 0.25rem;
                    ">${mission.missionType || 'Easy'}</div>
                </div>
                <div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem; text-transform: uppercase; letter-spacing: 0.05em;">Progress</div>
                    <div style="
                        font-family: 'Courier New', monospace;
                        font-size: 0.6875rem;
                        color: rgba(236, 72, 153, 0.9);
                        padding: 0.5rem;
                        background: rgba(0, 0, 0, 0.3);
                        border: 1px solid var(--border-color);
                        border-radius: 0.25rem;
                        letter-spacing: 0.05em;
                        margin-bottom: 0.25rem;
                    ">${asciiBar}</div>
                    <div style="
                        font-family: 'Courier New', monospace;
                        font-size: 0.75rem;
                        color: var(--text-secondary);
                        text-align: right;
                    ">${Math.round(progress)}% [${completedQuests}/${totalQuests}]</div>
                </div>
                ${hasChapters ? `
                <div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem; text-transform: uppercase; letter-spacing: 0.05em;">Chapters</div>
                    <div style="
                        font-family: 'Courier New', monospace;
                        font-size: 0.875rem;
                        color: var(--text-primary);
                        font-weight: 500;
                        padding: 0.5rem;
                        background: rgba(0, 0, 0, 0.3);
                        border: 1px solid var(--border-color);
                        border-radius: 0.25rem;
                    ">${chapters.length}</div>
                </div>
                ` : ''}
                <div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem; text-transform: uppercase; letter-spacing: 0.05em;">Rewards</div>
                    <div style="display: flex; flex-direction: column; gap: 0.5rem;">
                        <div style="
                            font-family: 'Courier New', monospace;
                            font-size: 0.75rem;
                            color: var(--text-secondary);
                        ">
                            XP: ${mission.rewardXP || 0}
                        </div>
                        <div style="
                            font-family: 'Courier New', monospace;
                            font-size: 0.75rem;
                            color: var(--text-secondary);
                        ">
                            Karma: ${mission.rewardKarma || 0}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Load Missions
 */
async function loadMissions() {
    try {
        const authData = localStorage.getItem('oasis_auth');
        if (authData) {
            const auth = JSON.parse(authData);
            missionInterfaceState.authToken = auth.token;
            missionInterfaceState.avatarId = auth.avatarId || auth.avatar?.id;
        }

        // Placeholder - would load from API
        missionInterfaceState.missions = [];

        updateMissionList();
    } catch (error) {
        console.error('Error loading missions:', error);
    }
}

/**
 * Load Mission Detail
 */
async function loadMissionDetail(missionId) {
    try {
        // Placeholder - would load from API
    } catch (error) {
        console.error('Error loading mission detail:', error);
    }
}

/**
 * Update Mission List
 */
function updateMissionList() {
    const container = document.getElementById('mission-list-container');
    if (container) {
        container.innerHTML = renderMissionList();
    }
}

/**
 * Render Chapter Timeline
 */
function renderChapterTimeline(chapters) {
    return `
        <div style="margin-top: 1rem;">
            <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em;">Chapter Timeline</div>
            <div style="
                font-family: 'Courier New', monospace;
                font-size: 0.6875rem;
                color: var(--text-secondary);
                background: rgba(0, 0, 0, 0.2);
                padding: 0.75rem;
                border: 1px solid var(--border-color);
                border-radius: 0.25rem;
                line-height: 1.6;
            ">
                ${chapters.map((chapter, index) => {
                    const chapterQuests = chapter.quests || [];
                    const completedChapterQuests = chapterQuests.filter(q => q.status === 'completed' || q.completed).length;
                    const totalChapterQuests = chapterQuests.length || 1;
                    const chapterProgress = totalChapterQuests > 0 ? (completedChapterQuests / totalChapterQuests) * 100 : 0;
                    const status = chapterProgress === 100 ? 'COMPLETE' : chapterProgress > 0 ? 'ACTIVE' : 'PENDING';
                    const statusColor = chapterProgress === 100 ? 'rgba(34, 197, 94, 0.8)' : chapterProgress > 0 ? 'rgba(168, 85, 247, 0.8)' : 'var(--text-tertiary)';
                    
                    return `
                        <div style="margin-bottom: ${index < chapters.length - 1 ? '0.75rem' : '0'};">
                            <div style="display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.25rem;">
                                <span style="color: ${statusColor}; font-weight: 500;">[${status}]</span>
                                <span style="color: var(--text-primary);">${escapeHtml(chapter.name || `${chapter.chapterDisplayName || 'Chapter'} ${index + 1}`)}</span>
                                <span style="color: var(--text-tertiary); margin-left: auto;">${completedChapterQuests}/${totalChapterQuests}</span>
                            </div>
                            <div style="color: ${statusColor}; font-size: 0.625rem; margin-left: 1.5rem;">
                                ${generateASCIIProgressBar(chapterProgress, 20)}
                            </div>
                        </div>
                    `;
                }).join('')}
            </div>
        </div>
    `;
}

/**
 * Render Quest Timeline
 */
function renderQuestTimeline(quests) {
    return `
        <div style="margin-top: 1rem;">
            <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em;">Quest Timeline</div>
            <div style="
                font-family: 'Courier New', monospace;
                font-size: 0.6875rem;
                color: var(--text-secondary);
                background: rgba(0, 0, 0, 0.2);
                padding: 0.75rem;
                border: 1px solid var(--border-color);
                border-radius: 0.25rem;
                line-height: 1.6;
            ">
                ${quests.map((quest, index) => {
                    const isCompleted = quest.completed || quest.status === 'completed';
                    const isActive = quest.status === 'active';
                    const status = isCompleted ? '[OK]' : isActive ? '[>>]' : '[--]';
                    const statusColor = isCompleted ? 'rgba(34, 197, 94, 0.8)' : isActive ? 'rgba(168, 85, 247, 0.8)' : 'var(--text-tertiary)';
                    
                    return `
                        <div style="display: flex; align-items: center; gap: 0.5rem; margin-bottom: ${index < quests.length - 1 ? '0.25rem' : '0'};">
                            <span style="color: ${statusColor}; font-weight: 500; min-width: 40px;">${status}</span>
                            <span style="color: var(--text-primary);">${escapeHtml(quest.name || `Quest ${index + 1}`)}</span>
                        </div>
                    `;
                }).join('')}
            </div>
        </div>
    `;
}

/**
 * Show Mission Creation Interface
 */
function showMissionCreationInterface() {
    missionInterfaceState.view = 'create';
    const container = document.getElementById('star-dashboard-content') || document.getElementById('mission-interface-container');
    if (!container) {
        const starTab = document.getElementById('tab-star');
        if (starTab) {
            const newContainer = document.createElement('div');
            newContainer.id = 'mission-interface-container';
            starTab.innerHTML = '';
            starTab.appendChild(newContainer);
            container = newContainer;
        } else {
            console.error('Mission interface container not found');
            return;
        }
    }
    
    container.innerHTML = renderMissionBuilder();
    attachMissionBuilderListeners();
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

/**
 * Mission Builder State
 */
const missionBuilderState = {
    currentStep: 1,
    totalSteps: 3,
    mission: {
        name: '',
        description: '',
        missionType: 'Easy',
        rewardXP: 0,
        rewardKarma: 0,
        useChapters: false,
        chapters: [],
        quests: []
    },
    availableQuests: []
};

/**
 * Render Mission Builder
 */
function renderMissionBuilder() {
    return `
        <div class="portal-section">
            <div style="margin-bottom: 2rem;">
                <button 
                    class="btn-text" 
                    onclick="showMissionTracker()"
                    style="margin-bottom: 1rem; font-size: 0.875rem;"
                >
                    ← Back to Missions
                </button>
                <h2 style="
                    font-size: 1.5rem;
                    font-weight: 300;
                    color: var(--text-primary);
                    margin-bottom: 0.5rem;
                ">Mission Builder</h2>
                <p style="
                    font-size: 0.875rem;
                    color: var(--text-secondary);
                ">Create a new mission with chapters and quests</p>
            </div>

            ${renderMissionBuilderSteps()}
            ${renderMissionBuilderContent()}
        </div>
    `;
}

/**
 * Render Mission Builder Steps
 */
function renderMissionBuilderSteps() {
    const steps = [
        { num: 1, name: 'Basic Info' },
        { num: 2, name: 'Organization' },
        { num: 3, name: 'Rewards' }
    ];
    
    return `
        <div style="
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 2rem;
            padding: 1rem;
            background: rgba(0, 0, 0, 0.3);
            border: 1px solid var(--border-color);
            border-radius: 0.5rem;
        ">
            ${steps.map((step, index) => {
                const isActive = missionBuilderState.currentStep === step.num;
                const isPast = missionBuilderState.currentStep > step.num;
                
                return `
                    <div style="
                        display: flex;
                        align-items: center;
                        gap: 0.5rem;
                        flex: 1;
                    ">
                        <div style="
                            width: 32px;
                            height: 32px;
                            border-radius: 50%;
                            background: ${isActive ? 'rgba(236, 72, 153, 0.2)' : isPast ? 'rgba(34, 197, 94, 0.2)' : 'rgba(255, 255, 255, 0.05)'};
                            border: 2px solid ${isActive ? 'rgba(236, 72, 153, 1)' : isPast ? 'rgba(34, 197, 94, 1)' : 'var(--border-color)'};
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            font-family: 'Courier New', monospace;
                            font-size: 0.75rem;
                            font-weight: 500;
                            color: ${isActive ? 'rgba(236, 72, 153, 1)' : isPast ? 'rgba(34, 197, 94, 1)' : 'var(--text-secondary)'};
                        ">
                            ${isPast ? 'OK' : step.num}
                        </div>
                        <div>
                            <div style="
                                font-size: 0.75rem;
                                color: var(--text-tertiary);
                                text-transform: uppercase;
                                letter-spacing: 0.05em;
                            ">Step ${step.num}</div>
                            <div style="
                                font-size: 0.875rem;
                                color: ${isActive ? 'var(--text-primary)' : 'var(--text-secondary)'};
                                font-weight: ${isActive ? '500' : '400'};
                            ">${step.name}</div>
                        </div>
                        ${index < steps.length - 1 ? `
                        <div style="
                            flex: 1;
                            height: 2px;
                            background: ${isPast ? 'rgba(34, 197, 94, 0.3)' : 'var(--border-color)'};
                            margin: 0 1rem;
                        "></div>
                        ` : ''}
                    </div>
                `;
            }).join('')}
        </div>
    `;
}

/**
 * Render Mission Builder Content
 */
function renderMissionBuilderContent() {
    switch (missionBuilderState.currentStep) {
        case 1:
            return renderMissionBasicInfoStep();
        case 2:
            return renderMissionOrganizationStep();
        case 3:
            return renderMissionRewardsStep();
        default:
            return '';
    }
}

/**
 * Render Mission Basic Info Step
 */
function renderMissionBasicInfoStep() {
    return `
        <div class="portal-card" style="padding: 2rem;">
            <h3 style="font-size: 1.125rem; font-weight: 500; color: var(--text-primary); margin-bottom: 1.5rem;">Basic Information</h3>
            
            <div style="display: flex; flex-direction: column; gap: 1.5rem;">
                <div>
                    <label style="
                        display: block;
                        font-size: 0.75rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                        color: var(--text-tertiary);
                        margin-bottom: 0.5rem;
                    ">Mission Name *</label>
                    <input
                        type="text"
                        id="mission-name-input"
                        value="${escapeHtml(missionBuilderState.mission.name)}"
                        placeholder="Enter mission name"
                        style="
                            width: 100%;
                            padding: 0.75rem;
                            background: rgba(0, 0, 0, 0.3);
                            border: 1px solid var(--border-color);
                            border-radius: 0.5rem;
                            color: var(--text-primary);
                            font-family: inherit;
                            font-size: 0.875rem;
                        "
                    />
                </div>
                
                <div>
                    <label style="
                        display: block;
                        font-size: 0.75rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                        color: var(--text-tertiary);
                        margin-bottom: 0.5rem;
                    ">Description *</label>
                    <textarea
                        id="mission-description-input"
                        rows="4"
                        placeholder="Describe the mission..."
                        style="
                            width: 100%;
                            padding: 0.75rem;
                            background: rgba(0, 0, 0, 0.3);
                            border: 1px solid var(--border-color);
                            border-radius: 0.5rem;
                            color: var(--text-primary);
                            font-family: inherit;
                            font-size: 0.875rem;
                            resize: vertical;
                        "
                    >${escapeHtml(missionBuilderState.mission.description)}</textarea>
                </div>
                
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 1rem;">
                    <div>
                        <label style="
                            display: block;
                            font-size: 0.75rem;
                            text-transform: uppercase;
                            letter-spacing: 0.05em;
                            color: var(--text-tertiary);
                            margin-bottom: 0.5rem;
                        ">Mission Type</label>
                        <select
                            id="mission-type-select"
                            style="
                                width: 100%;
                                padding: 0.75rem;
                                background: rgba(0, 0, 0, 0.3);
                                border: 1px solid var(--border-color);
                                border-radius: 0.5rem;
                                color: var(--text-primary);
                                font-family: inherit;
                                font-size: 0.875rem;
                            "
                        >
                            <option value="Easy" ${missionBuilderState.mission.missionType === 'Easy' ? 'selected' : ''}>Easy</option>
                            <option value="Medium" ${missionBuilderState.mission.missionType === 'Medium' ? 'selected' : ''}>Medium</option>
                            <option value="Hard" ${missionBuilderState.mission.missionType === 'Hard' ? 'selected' : ''}>Hard</option>
                            <option value="Expert" ${missionBuilderState.mission.missionType === 'Expert' ? 'selected' : ''}>Expert</option>
                        </select>
                    </div>
                </div>
            </div>
            
            ${renderMissionBuilderNavigation()}
        </div>
    `;
}

/**
 * Render Mission Organization Step
 */
function renderMissionOrganizationStep() {
    return `
        <div class="portal-card" style="padding: 2rem;">
            <h3 style="font-size: 1.125rem; font-weight: 500; color: var(--text-primary); margin-bottom: 1.5rem;">Organization</h3>
            
            <div style="display: flex; flex-direction: column; gap: 1.5rem;">
                <div>
                    <label style="
                        display: block;
                        font-size: 0.75rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                        color: var(--text-tertiary);
                        margin-bottom: 1rem;
                    ">Quest Organization</label>
                    
                    <div style="display: flex; flex-direction: column; gap: 0.75rem;">
                        <label style="
                            display: flex;
                            align-items: center;
                            gap: 0.75rem;
                            padding: 1rem;
                            background: rgba(0, 0, 0, 0.2);
                            border: 1px solid var(--border-color);
                            border-radius: 0.5rem;
                            cursor: pointer;
                        ">
                            <input
                                type="radio"
                                name="mission-organization"
                                value="flat"
                                ${!missionBuilderState.mission.useChapters ? 'checked' : ''}
                                onchange="missionBuilderState.mission.useChapters = false; updateMissionBuilderContent();"
                                style="cursor: pointer;"
                            />
                            <div>
                                <div style="font-size: 0.875rem; color: var(--text-primary); font-weight: 500;">Flat List</div>
                                <div style="font-size: 0.75rem; color: var(--text-secondary);">Simple list of quests (no chapters)</div>
                            </div>
                        </label>
                        
                        <label style="
                            display: flex;
                            align-items: center;
                            gap: 0.75rem;
                            padding: 1rem;
                            background: rgba(0, 0, 0, 0.2);
                            border: 1px solid var(--border-color);
                            border-radius: 0.5rem;
                            cursor: pointer;
                        ">
                            <input
                                type="radio"
                                name="mission-organization"
                                value="chapters"
                                ${missionBuilderState.mission.useChapters ? 'checked' : ''}
                                onchange="missionBuilderState.mission.useChapters = true; updateMissionBuilderContent();"
                                style="cursor: pointer;"
                            />
                            <div>
                                <div style="font-size: 0.875rem; color: var(--text-primary); font-weight: 500;">Chapters</div>
                                <div style="font-size: 0.75rem; color: var(--text-secondary);">Organize quests into chapters (recommended for large missions)</div>
                            </div>
                        </label>
                    </div>
                </div>
                
                ${missionBuilderState.mission.useChapters ? `
                <div>
                    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">
                        <label style="
                            font-size: 0.75rem;
                            text-transform: uppercase;
                            letter-spacing: 0.05em;
                            color: var(--text-tertiary);
                        ">Chapters</label>
                        <button
                            type="button"
                            onclick="addMissionChapter()"
                            class="btn-secondary"
                            style="font-size: 0.75rem; padding: 0.5rem 1rem;"
                        >
                            + Add Chapter
                        </button>
                    </div>
                    <div id="mission-chapters-container" style="display: flex; flex-direction: column; gap: 1rem;">
                        ${renderMissionChapters()}
                    </div>
                </div>
                ` : `
                <div>
                    <label style="
                        display: block;
                        font-size: 0.75rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                        color: var(--text-tertiary);
                        margin-bottom: 1rem;
                    ">Select Quests</label>
                    <div style="
                        padding: 1rem;
                        background: rgba(0, 0, 0, 0.2);
                        border: 1px solid var(--border-color);
                        border-radius: 0.5rem;
                        font-size: 0.875rem;
                        color: var(--text-secondary);
                    ">
                        Quest selection will be available after quests are created.
                    </div>
                </div>
                `}
            </div>
            
            ${renderMissionBuilderNavigation()}
        </div>
    `;
}

/**
 * Render Mission Chapters
 */
function renderMissionChapters() {
    const chapters = missionBuilderState.mission.chapters || [];
    if (chapters.length === 0) {
        return `
            <div style="
                padding: 2rem;
                text-align: center;
                color: var(--text-tertiary);
                font-size: 0.875rem;
            ">
                No chapters yet. Click "+ Add Chapter" to get started.
            </div>
        `;
    }
    
    return chapters.map((chapter, index) => `
        <div style="
            padding: 1rem;
            background: rgba(0, 0, 0, 0.3);
            border: 1px solid var(--border-color);
            border-radius: 0.5rem;
        ">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.75rem;">
                <input
                    type="text"
                    value="${escapeHtml(chapter.name || `Chapter ${index + 1}`)}"
                    onchange="missionBuilderState.mission.chapters[${index}].name = this.value;"
                    placeholder="Chapter name"
                    style="
                        flex: 1;
                        padding: 0.5rem;
                        background: rgba(0, 0, 0, 0.3);
                        border: 1px solid var(--border-color);
                        border-radius: 0.25rem;
                        color: var(--text-primary);
                        font-family: inherit;
                        font-size: 0.875rem;
                        margin-right: 0.5rem;
                    "
                />
                <button
                    type="button"
                    onclick="removeMissionChapter(${index})"
                    style="
                        padding: 0.5rem;
                        background: rgba(239, 68, 68, 0.2);
                        border: 1px solid rgba(239, 68, 68, 0.3);
                        border-radius: 0.25rem;
                        color: rgba(239, 68, 68, 1);
                        cursor: pointer;
                        font-size: 0.75rem;
                    "
                >
                    Remove
                </button>
            </div>
            <div style="font-size: 0.75rem; color: var(--text-secondary);">
                Quest selection will be available here.
            </div>
        </div>
    `).join('');
}

/**
 * Render Mission Rewards Step
 */
function renderMissionRewardsStep() {
    return `
        <div class="portal-card" style="padding: 2rem;">
            <h3 style="font-size: 1.125rem; font-weight: 500; color: var(--text-primary); margin-bottom: 1.5rem;">Completion Rewards</h3>
            
            <div style="display: flex; flex-direction: column; gap: 1.5rem;">
                <div>
                    <label style="
                        display: block;
                        font-size: 0.75rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                        color: var(--text-tertiary);
                        margin-bottom: 0.5rem;
                    ">Experience Points (XP)</label>
                    <input
                        type="number"
                        id="mission-xp-input"
                        value="${missionBuilderState.mission.rewardXP}"
                        min="0"
                        placeholder="0"
                        style="
                            width: 100%;
                            padding: 0.75rem;
                            background: rgba(0, 0, 0, 0.3);
                            border: 1px solid var(--border-color);
                            border-radius: 0.5rem;
                            color: var(--text-primary);
                            font-family: 'Courier New', monospace;
                            font-size: 0.875rem;
                        "
                    />
                </div>
                
                <div>
                    <label style="
                        display: block;
                        font-size: 0.75rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                        color: var(--text-tertiary);
                        margin-bottom: 0.5rem;
                    ">Karma Points</label>
                    <input
                        type="number"
                        id="mission-karma-input"
                        value="${missionBuilderState.mission.rewardKarma}"
                        min="0"
                        placeholder="0"
                        style="
                            width: 100%;
                            padding: 0.75rem;
                            background: rgba(0, 0, 0, 0.3);
                            border: 1px solid var(--border-color);
                            border-radius: 0.5rem;
                            color: var(--text-primary);
                            font-family: 'Courier New', monospace;
                            font-size: 0.875rem;
                        "
                    />
                </div>
            </div>
            
            ${renderMissionBuilderNavigation()}
        </div>
    `;
}

/**
 * Render Mission Builder Navigation
 */
function renderMissionBuilderNavigation() {
    return `
        <div style="
            display: flex;
            justify-content: space-between;
            margin-top: 2rem;
            padding-top: 1.5rem;
            border-top: 1px solid var(--border-color);
        ">
            <button
                type="button"
                onclick="previousMissionBuilderStep()"
                ${missionBuilderState.currentStep === 1 ? 'disabled' : ''}
                class="btn-secondary"
                style="${missionBuilderState.currentStep === 1 ? 'opacity: 0.5; cursor: not-allowed;' : ''}"
            >
                ← Previous
            </button>
            ${missionBuilderState.currentStep < missionBuilderState.totalSteps ? `
            <button
                type="button"
                onclick="nextMissionBuilderStep()"
                class="btn-primary"
            >
                Next →
            </button>
            ` : `
            <button
                type="button"
                onclick="createMission()"
                class="btn-primary"
            >
                Create Mission
            </button>
            `}
        </div>
    `;
}

/**
 * Attach Mission Builder Listeners
 */
function attachMissionBuilderListeners() {
    const nameInput = document.getElementById('mission-name-input');
    if (nameInput) {
        nameInput.addEventListener('input', (e) => {
            missionBuilderState.mission.name = e.target.value;
        });
    }
    
    const descInput = document.getElementById('mission-description-input');
    if (descInput) {
        descInput.addEventListener('input', (e) => {
            missionBuilderState.mission.description = e.target.value;
        });
    }
    
    const typeSelect = document.getElementById('mission-type-select');
    if (typeSelect) {
        typeSelect.addEventListener('change', (e) => {
            missionBuilderState.mission.missionType = e.target.value;
        });
    }
    
    const xpInput = document.getElementById('mission-xp-input');
    if (xpInput) {
        xpInput.addEventListener('input', (e) => {
            missionBuilderState.mission.rewardXP = parseInt(e.target.value) || 0;
        });
    }
    
    const karmaInput = document.getElementById('mission-karma-input');
    if (karmaInput) {
        karmaInput.addEventListener('input', (e) => {
            missionBuilderState.mission.rewardKarma = parseInt(e.target.value) || 0;
        });
    }
}

/**
 * Update Mission Builder Content
 */
function updateMissionBuilderContent() {
    const container = document.getElementById('star-dashboard-content') || document.getElementById('mission-interface-container');
    if (container) {
        const content = container.querySelector('.portal-card');
        if (content) {
            content.innerHTML = renderMissionBuilderContent();
            attachMissionBuilderListeners();
        }
    }
}

/**
 * Next Mission Builder Step
 */
function nextMissionBuilderStep() {
    if (missionBuilderState.currentStep < missionBuilderState.totalSteps) {
        if (validateMissionBuilderStep()) {
            missionBuilderState.currentStep++;
            const container = document.getElementById('star-dashboard-content') || document.getElementById('mission-interface-container');
            if (container) {
                container.innerHTML = renderMissionBuilder();
                attachMissionBuilderListeners();
            }
        }
    }
}

/**
 * Previous Mission Builder Step
 */
function previousMissionBuilderStep() {
    if (missionBuilderState.currentStep > 1) {
        missionBuilderState.currentStep--;
        const container = document.getElementById('star-dashboard-content') || document.getElementById('mission-interface-container');
        if (container) {
            container.innerHTML = renderMissionBuilder();
            attachMissionBuilderListeners();
        }
    }
}

/**
 * Validate Mission Builder Step
 */
function validateMissionBuilderStep() {
    if (missionBuilderState.currentStep === 1) {
        if (!missionBuilderState.mission.name || !missionBuilderState.mission.description) {
            alert('Please fill in all required fields (Name and Description)');
            return false;
        }
    }
    return true;
}

/**
 * Add Mission Chapter
 */
function addMissionChapter() {
    if (!missionBuilderState.mission.chapters) {
        missionBuilderState.mission.chapters = [];
    }
    missionBuilderState.mission.chapters.push({
        name: '',
        chapterDisplayName: 'Chapter',
        quests: []
    });
    updateMissionBuilderContent();
}

/**
 * Remove Mission Chapter
 */
function removeMissionChapter(index) {
    if (missionBuilderState.mission.chapters && missionBuilderState.mission.chapters.length > index) {
        missionBuilderState.mission.chapters.splice(index, 1);
        updateMissionBuilderContent();
    }
}

/**
 * Create Mission
 */
async function createMission() {
    if (!validateMissionBuilderStep()) {
        return;
    }
    
    try {
        const authData = localStorage.getItem('oasis_auth');
        if (!authData) {
            alert('Please log in to create a mission');
            return;
        }
        
        const auth = JSON.parse(authData);
        const baseUrl = missionInterfaceState.baseUrl || window.location.origin;
        
        const missionData = {
            name: missionBuilderState.mission.name,
            description: missionBuilderState.mission.description,
            missionType: missionBuilderState.mission.missionType,
            rewardXP: missionBuilderState.mission.rewardXP,
            rewardKarma: missionBuilderState.mission.rewardKarma,
            chapters: missionBuilderState.mission.useChapters ? missionBuilderState.mission.chapters : null,
            quests: missionBuilderState.mission.useChapters ? [] : missionBuilderState.mission.quests
        };
        
        const response = await fetch(`${baseUrl}/api/missions`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${auth.token}`
            },
            body: JSON.stringify(missionData)
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Failed to create mission');
        }
        
        const result = await response.json();
        alert('Mission created successfully!');
        showMissionTracker();
    } catch (error) {
        console.error('Error creating mission:', error);
        alert(`Failed to create mission: ${error.message}`);
    }
}

// Export functions to window
if (typeof window !== 'undefined') {
    window.showMissionTracker = showMissionTracker;
    window.showMissionDetail = showMissionDetail;
    window.showMissionCreationInterface = showMissionCreationInterface;
    window.nextMissionBuilderStep = nextMissionBuilderStep;
    window.previousMissionBuilderStep = previousMissionBuilderStep;
    window.addMissionChapter = addMissionChapter;
    window.removeMissionChapter = removeMissionChapter;
    window.createMission = createMission;
    window.updateMissionBuilderContent = updateMissionBuilderContent;
    window.generateASCIIProgressBar = generateASCIIProgressBar;
}

