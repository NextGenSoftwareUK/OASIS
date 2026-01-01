/**
 * Quest Builder - Interactive Quest Creation Interface
 */

const questBuilderState = {
    baseUrl: window.location.origin,
    authToken: null,
    avatarId: null,
    currentStep: 1,
    totalSteps: 5,
    userNFTs: [],
    quest: {
        name: '',
        description: '',
        questType: 'MainQuest',
        difficulty: 'Easy',
        objectives: [],
        rewardXP: 0,
        rewardKarma: 0,
        rewardNFTs: [],
        parentMissionId: null
    }
};

/**
 * Show Quest Creation Interface
 */
async function showQuestCreationInterface() {
    questBuilderState.currentStep = 1;
    questBuilderState.quest = {
        name: '',
        description: '',
        questType: 'MainQuest',
        difficulty: 'Easy',
        objectives: [],
        rewardXP: 0,
        rewardKarma: 0,
        rewardNFTs: [],
        parentMissionId: null
    };

    // Load user's NFTs
    await loadUserNFTs();

    const container = document.getElementById('star-dashboard-content') || document.getElementById('quest-builder-container');
    if (!container) {
        // Create container in STAR tab
        const starTab = document.getElementById('tab-star');
        if (starTab) {
            const newContainer = document.createElement('div');
            newContainer.id = 'quest-builder-container';
            starTab.innerHTML = '';
            starTab.appendChild(newContainer);
            container = newContainer;
        } else {
            console.error('Quest builder container not found');
            return;
        }
    }
    
    container.innerHTML = renderQuestBuilder();
    attachQuestBuilderListeners();
}

/**
 * Render Quest Builder
 */
function renderQuestBuilder() {
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
                ">Quest Builder</h2>
                <p style="
                    font-size: 0.875rem;
                    color: var(--text-secondary);
                ">Create an interactive quest with stages, objectives, and rewards</p>
            </div>

            ${renderQuestBuilderSteps()}
            ${renderQuestBuilderContent()}
            ${renderNFTSelectorModal()}
        </div>
    `;
}

/**
 * Render Quest Builder Steps
 */
function renderQuestBuilderSteps() {
    const steps = [
        { id: 1, title: 'Basic Info', description: 'Name, type, difficulty' },
        { id: 2, title: 'Objectives', description: 'Quest stages and tasks' },
        { id: 3, title: 'Rewards', description: 'XP, Karma, items' },
        { id: 4, title: 'Requirements', description: 'Prerequisites and limits' },
        { id: 5, title: 'Review', description: 'Preview and create' }
    ];

    return `
        <div class="portal-card" style="margin-bottom: 2rem; padding: 1.5rem;">
            <div style="display: flex; justify-content: space-between; align-items: center; gap: 1rem; overflow-x: auto;">
                ${steps.map((step, index) => `
                <div style="
                    display: flex;
                    align-items: center;
                    gap: 0.75rem;
                    flex: 1;
                    min-width: 140px;
                ">
                    <div style="
                        width: 32px;
                        height: 32px;
                        border-radius: 50%;
                        background: ${step.id === questBuilderState.currentStep ? 'rgba(168, 85, 247, 0.2)' : 
                                    step.id < questBuilderState.currentStep ? 'rgba(34, 197, 94, 0.2)' : 'rgba(255, 255, 255, 0.05)'};
                        border: 2px solid ${step.id === questBuilderState.currentStep ? 'rgba(168, 85, 247, 1)' : 
                                        step.id < questBuilderState.currentStep ? 'rgba(34, 197, 94, 1)' : 'var(--border-color)'};
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        font-size: 0.875rem;
                        font-weight: 500;
                        color: ${step.id === questBuilderState.currentStep ? 'rgba(168, 85, 247, 1)' : 
                               step.id < questBuilderState.currentStep ? 'rgba(34, 197, 94, 1)' : 'var(--text-secondary)'};
                        flex-shrink: 0;
                    ">
                        ${step.id < questBuilderState.currentStep ? '✓' : step.id}
                    </div>
                    <div style="flex: 1; min-width: 0;">
                        <div style="
                            font-size: 0.875rem;
                            font-weight: ${step.id === questBuilderState.currentStep ? '500' : '400'};
                            color: ${step.id === questBuilderState.currentStep ? 'var(--text-primary)' : 'var(--text-secondary)'};
                        ">${step.title}</div>
                        <div style="
                            font-size: 0.75rem;
                            color: var(--text-tertiary);
                        ">${step.description}</div>
                    </div>
                    ${index < steps.length - 1 ? `
                    <div style="
                        width: 20px;
                        height: 2px;
                        background: ${step.id < questBuilderState.currentStep ? 'rgba(34, 197, 94, 0.3)' : 'rgba(255, 255, 255, 0.1)'};
                        margin: 0 0.5rem;
                        flex-shrink: 0;
                    "></div>
                    ` : ''}
                </div>
                `).join('')}
            </div>
        </div>
    `;
}

/**
 * Render Quest Builder Content
 */
function renderQuestBuilderContent() {
    switch (questBuilderState.currentStep) {
        case 1:
            return renderBasicInfoStep();
        case 2:
            return renderObjectivesStep();
        case 3:
            return renderRewardsStep();
        case 4:
            return renderRequirementsStep();
        case 5:
            return renderReviewStep();
        default:
            return '';
    }
}

/**
 * Render Basic Info Step
 */
function renderBasicInfoStep() {
    return `
        <div class="portal-card" style="padding: 2rem;">
            <h3 style="
                font-size: 1.125rem;
                font-weight: 500;
                color: var(--text-primary);
                margin-bottom: 2rem;
            ">Basic Information</h3>

            <div style="display: flex; flex-direction: column; gap: 1.5rem;">
                <div>
                    <label style="
                        display: block;
                        font-size: 0.875rem;
                        font-weight: 500;
                        color: var(--text-primary);
                        margin-bottom: 0.5rem;
                    ">Quest Name *</label>
                    <input 
                        type="text" 
                        id="quest-name-input"
                        class="form-input"
                        placeholder="e.g., Find the Hidden Treasure"
                        value="${escapeHtml(questBuilderState.quest.name)}"
                        style="width: 100%;"
                    />
                </div>

                <div>
                    <label style="
                        display: block;
                        font-size: 0.875rem;
                        font-weight: 500;
                        color: var(--text-primary);
                        margin-bottom: 0.5rem;
                    ">Description *</label>
                    <textarea 
                        id="quest-description-input"
                        class="form-input"
                        placeholder="Describe what players need to do in this quest..."
                        rows="4"
                        style="width: 100%; resize: vertical;"
                    >${escapeHtml(questBuilderState.quest.description)}</textarea>
                </div>

                <div style="display: grid; grid-template-columns: repeat(2, 1fr); gap: 1.5rem;">
                    <div>
                        <label style="
                            display: block;
                            font-size: 0.875rem;
                            font-weight: 500;
                            color: var(--text-primary);
                            margin-bottom: 0.5rem;
                        ">Quest Type</label>
                        <select id="quest-type-input" class="form-select" style="width: 100%;">
                            <option value="MainQuest" ${questBuilderState.quest.questType === 'MainQuest' ? 'selected' : ''}>Main Quest</option>
                            <option value="SideQuest" ${questBuilderState.quest.questType === 'SideQuest' ? 'selected' : ''}>Side Quest</option>
                            <option value="MagicQuest" ${questBuilderState.quest.questType === 'MagicQuest' ? 'selected' : ''}>Magic Quest</option>
                            <option value="EggQuest" ${questBuilderState.quest.questType === 'EggQuest' ? 'selected' : ''}>Egg Quest</option>
                        </select>
                    </div>

                    <div>
                        <label style="
                            display: block;
                            font-size: 0.875rem;
                            font-weight: 500;
                            color: var(--text-primary);
                            margin-bottom: 0.5rem;
                        ">Difficulty</label>
                        <select id="quest-difficulty-input" class="form-select" style="width: 100%;">
                            <option value="Easy" ${questBuilderState.quest.difficulty === 'Easy' ? 'selected' : ''}>Easy</option>
                            <option value="Medium" ${questBuilderState.quest.difficulty === 'Medium' ? 'selected' : ''}>Medium</option>
                            <option value="Hard" ${questBuilderState.quest.difficulty === 'Hard' ? 'selected' : ''}>Hard</option>
                            <option value="Expert" ${questBuilderState.quest.difficulty === 'Expert' ? 'selected' : ''}>Expert</option>
                        </select>
                    </div>
                </div>
            </div>

            ${renderQuestBuilderNavigation()}
        </div>
    `;
}

/**
 * Render Objectives Step
 */
function renderObjectivesStep() {
    return `
        <div class="portal-card" style="padding: 2rem;">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">
                <h3 style="
                    font-size: 1.125rem;
                    font-weight: 500;
                    color: var(--text-primary);
                    margin: 0;
                ">Quest Objectives</h3>
                <button 
                    class="btn-secondary"
                    onclick="addQuestObjective()"
                    style="font-size: 0.875rem;"
                >
                    + Add Objective
                </button>
            </div>
            ${questBuilderState.userNFTs.length > 0 ? `
            <div style="
                padding: 0.75rem 1rem;
                background: rgba(59, 130, 246, 0.05);
                border: 1px solid rgba(59, 130, 246, 0.2);
                border-radius: 0.5rem;
                margin-bottom: 1.5rem;
            ">
                <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0;">
                    <strong>Tip:</strong> Use "Collect NFT" objective type to require players to collect specific NFTs from your collection.
                    You have ${questBuilderState.userNFTs.length} NFT${questBuilderState.userNFTs.length !== 1 ? 's' : ''} available.
                </p>
            </div>
            ` : ''}
            <div id="objectives-list" style="display: flex; flex-direction: column; gap: 1rem; margin-bottom: 2rem;">
                ${questBuilderState.quest.objectives.length === 0 ? `
                <div class="empty-state" style="padding: 2rem;">
                    <p class="empty-state-text">No objectives yet. Add your first objective to get started!</p>
                </div>
                ` : questBuilderState.quest.objectives.map((obj, index) => renderObjectiveItem(obj, index)).join('')}
            </div>

            ${renderQuestBuilderNavigation()}
        </div>
    `;
}

/**
 * Render Objective Item
 */
function renderObjectiveItem(objective, index) {
    return `
        <div class="portal-card" style="padding: 1.5rem; border-left: 4px solid rgba(168, 85, 247, 0.3);" data-objective-index="${index}">
            <div style="display: flex; align-items: start; gap: 1rem;">
                <div style="
                    width: 32px;
                    height: 32px;
                    border-radius: 50%;
                    background: rgba(168, 85, 247, 0.1);
                    border: 2px solid rgba(168, 85, 247, 0.5);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    font-size: 0.875rem;
                    font-weight: 500;
                    color: rgba(168, 85, 247, 1);
                    flex-shrink: 0;
                ">
                    ${index + 1}
                </div>
                <div style="flex: 1;">
                    <input 
                        type="text" 
                        class="form-input objective-description-input"
                        placeholder="Objective description (e.g., Find the first clue, Collect 5 items)"
                        value="${escapeHtml(objective.description || objective.text || '')}"
                        data-objective-index="${index}"
                        style="width: 100%; margin-bottom: 0.75rem;"
                    />
                    <div style="display: flex; gap: 0.5rem; flex-wrap: wrap; align-items: center;">
                        <select class="form-select objective-type-input" data-objective-index="${index}" style="width: 150px;" onchange="handleObjectiveTypeChange(${index}, this.value)">
                            <option value="Action" ${objective.type === 'Action' ? 'selected' : ''}>Action</option>
                            <option value="Collection" ${objective.type === 'Collection' ? 'selected' : ''}>Collect NFT</option>
                            <option value="Location" ${objective.type === 'Location' ? 'selected' : ''}>Location</option>
                            <option value="Social" ${objective.type === 'Social' ? 'selected' : ''}>Social</option>
                        </select>
                        ${objective.type === 'Collection' ? `
                        <select class="form-select objective-nft-input" data-objective-index="${index}" style="width: 250px;" onchange="handleObjectiveNFTChange(${index}, this.value)">
                            <option value="">Select NFT to collect...</option>
                            ${questBuilderState.userNFTs.map(nft => `
                            <option value="${nft.id}" ${objective.nftId === nft.id ? 'selected' : ''}>
                                ${escapeHtml(nft.title || 'Unnamed NFT')}
                            </option>
                            `).join('')}
                        </select>
                        ${objective.nftId ? `
                        <div style="
                            display: flex;
                            align-items: center;
                            gap: 0.5rem;
                            padding: 0.5rem;
                            background: rgba(59, 130, 246, 0.1);
                            border: 1px solid rgba(59, 130, 246, 0.3);
                            border-radius: 0.375rem;
                            font-size: 0.75rem;
                            color: var(--text-primary);
                        ">
                            ${(() => {
                                const nft = questBuilderState.userNFTs.find(n => n.id === objective.nftId);
                                return nft?.imageUrl ? `<img src="${escapeHtml(nft.imageUrl)}" alt="" style="width: 24px; height: 24px; border-radius: 0.25rem; object-fit: cover;" />` : '';
                            })()}
                            <span>${escapeHtml(objective.nftTitle || 'NFT')}</span>
                        </div>
                        ` : ''}
                        ` : `
                        <input 
                            type="number" 
                            class="form-input objective-target-input"
                            placeholder="Target (optional)"
                            value="${objective.target || ''}"
                            data-objective-index="${index}"
                            style="width: 120px;"
                        />
                        `}
                    </div>
                </div>
                <button 
                    class="btn-text"
                    onclick="removeQuestObjective(${index})"
                    style="color: rgba(239, 68, 68, 1); font-size: 0.875rem; flex-shrink: 0;"
                >
                    Remove
                </button>
            </div>
        </div>
    `;
}

/**
 * Render Rewards Step
 */
function renderRewardsStep() {
    return `
        <div class="portal-card" style="padding: 2rem;">
            <h3 style="
                font-size: 1.125rem;
                font-weight: 500;
                color: var(--text-primary);
                margin-bottom: 2rem;
            ">Quest Rewards</h3>

            <div style="display: flex; flex-direction: column; gap: 1.5rem;">
                <div style="display: grid; grid-template-columns: repeat(2, 1fr); gap: 1.5rem;">
                    <div>
                        <label style="
                            display: block;
                            font-size: 0.875rem;
                            font-weight: 500;
                            color: var(--text-primary);
                            margin-bottom: 0.5rem;
                        ">Experience Points (XP)</label>
                        <input 
                            type="number" 
                            id="reward-xp-input"
                            class="form-input"
                            placeholder="0"
                            value="${questBuilderState.quest.rewardXP}"
                            min="0"
                            style="width: 100%;"
                        />
                    </div>

                    <div>
                        <label style="
                            display: block;
                            font-size: 0.875rem;
                            font-weight: 500;
                            color: var(--text-primary);
                            margin-bottom: 0.5rem;
                        ">Karma Points</label>
                        <input 
                            type="number" 
                            id="reward-karma-input"
                            class="form-input"
                            placeholder="0"
                            value="${questBuilderState.quest.rewardKarma}"
                            min="0"
                            style="width: 100%;"
                        />
                    </div>
                </div>

                <div>
                    <label style="
                        display: block;
                        font-size: 0.875rem;
                        font-weight: 500;
                        color: var(--text-primary);
                        margin-bottom: 0.5rem;
                    ">NFT Rewards (optional)</label>
                    <div id="nft-rewards-section" style="
                        padding: 1rem;
                        background: rgba(255, 255, 255, 0.02);
                        border: 1px solid var(--border-color);
                        border-radius: 0.5rem;
                        min-height: 100px;
                    ">
                        ${renderNFTRewardsSelector()}
                    </div>
                </div>
            </div>

            ${renderQuestBuilderNavigation()}
        </div>
    `;
}

/**
 * Render Requirements Step
 */
function renderRequirementsStep() {
    return `
        <div class="portal-card" style="padding: 2rem;">
            <h3 style="
                font-size: 1.125rem;
                font-weight: 500;
                color: var(--text-primary);
                margin-bottom: 2rem;
            ">Quest Requirements</h3>

            <div style="display: flex; flex-direction: column; gap: 1.5rem;">
                <div>
                    <label style="
                        display: block;
                        font-size: 0.875rem;
                        font-weight: 500;
                        color: var(--text-primary);
                        margin-bottom: 0.5rem;
                    ">Parent Mission (optional)</label>
                    <input 
                        type="text" 
                        id="parent-mission-input"
                        class="form-input"
                        placeholder="Mission ID or name (leave empty for standalone quest)"
                        style="width: 100%;"
                    />
                    <p style="font-size: 0.75rem; color: var(--text-tertiary); margin-top: 0.5rem;">
                        Link this quest to a mission if it's part of a larger story.
                    </p>
                </div>

                <div style="
                    padding: 1rem;
                    background: rgba(255, 255, 255, 0.02);
                    border: 1px solid var(--border-color);
                    border-radius: 0.5rem;
                ">
                    <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0;">
                        Additional requirements (prerequisites, level requirements, time limits) can be configured after quest creation.
                    </p>
                </div>
            </div>

            ${renderQuestBuilderNavigation()}
        </div>
    `;
}

/**
 * Render Review Step
 */
function renderReviewStep() {
    const objectivesCount = questBuilderState.quest.objectives.length;
    const totalRewards = questBuilderState.quest.rewardXP + questBuilderState.quest.rewardKarma;

    return `
        <div class="portal-card" style="padding: 2rem;">
            <h3 style="
                font-size: 1.125rem;
                font-weight: 500;
                color: var(--text-primary);
                margin-bottom: 2rem;
            ">Review Your Quest</h3>

            <div style="display: flex; flex-direction: column; gap: 2rem;">
                <div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem; text-transform: uppercase; letter-spacing: 0.05em;">Quest Information</div>
                    <div style="padding: 1.5rem; background: rgba(255, 255, 255, 0.02); border: 1px solid var(--border-color); border-radius: 0.5rem;">
                        <div style="margin-bottom: 1rem;">
                            <div style="font-size: 0.875rem; color: var(--text-tertiary); margin-bottom: 0.25rem;">Name</div>
                            <div style="font-size: 1rem; font-weight: 500; color: var(--text-primary);">${escapeHtml(questBuilderState.quest.name || 'Unnamed Quest')}</div>
                        </div>
                        <div style="margin-bottom: 1rem;">
                            <div style="font-size: 0.875rem; color: var(--text-tertiary); margin-bottom: 0.25rem;">Description</div>
                            <div style="font-size: 0.875rem; color: var(--text-secondary); line-height: 1.6;">${escapeHtml(questBuilderState.quest.description || 'No description')}</div>
                        </div>
                        <div style="display: grid; grid-template-columns: repeat(2, 1fr); gap: 1rem;">
                            <div>
                                <div style="font-size: 0.875rem; color: var(--text-tertiary); margin-bottom: 0.25rem;">Type</div>
                                <div style="font-size: 0.875rem; color: var(--text-primary);">${questBuilderState.quest.questType}</div>
                            </div>
                            <div>
                                <div style="font-size: 0.875rem; color: var(--text-tertiary); margin-bottom: 0.25rem;">Difficulty</div>
                                <div style="font-size: 0.875rem; color: var(--text-primary);">${questBuilderState.quest.difficulty}</div>
                            </div>
                        </div>
                    </div>
                </div>

                <div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem; text-transform: uppercase; letter-spacing: 0.05em;">Objectives (${objectivesCount})</div>
                    <div style="display: flex; flex-direction: column; gap: 0.75rem;">
                        ${objectivesCount === 0 ? `
                        <div style="padding: 1rem; background: rgba(255, 255, 255, 0.02); border: 1px solid var(--border-color); border-radius: 0.5rem; text-align: center; color: var(--text-tertiary);">
                            No objectives defined
                        </div>
                        ` : questBuilderState.quest.objectives.map((obj, index) => `
                        <div style="padding: 1rem; background: rgba(255, 255, 255, 0.02); border: 1px solid var(--border-color); border-radius: 0.5rem; border-left: 4px solid rgba(168, 85, 247, 0.3);">
                            <div style="display: flex; align-items: start; gap: 1rem;">
                                <div style="
                                    width: 24px;
                                    height: 24px;
                                    border-radius: 50%;
                                    background: rgba(168, 85, 247, 0.1);
                                    border: 2px solid rgba(168, 85, 247, 0.5);
                                    display: flex;
                                    align-items: center;
                                    justify-content: center;
                                    font-size: 0.75rem;
                                    font-weight: 500;
                                    color: rgba(168, 85, 247, 1);
                                    flex-shrink: 0;
                                ">
                                    ${index + 1}
                                </div>
                                <div style="flex: 1;">
                                    <div style="font-size: 0.875rem; color: var(--text-primary); margin-bottom: 0.25rem;">
                                        ${escapeHtml(obj.description || obj.text || 'Objective')}
                                    </div>
                                    <div style="font-size: 0.75rem; color: var(--text-tertiary);">
                                        Type: ${obj.type || 'Action'}${obj.target ? ` | Target: ${obj.target}` : ''}${obj.nftId ? ` | NFT: ${obj.nftTitle || obj.nftId}` : ''}
                                    </div>
                                </div>
                            </div>
                        </div>
                        `).join('')}
                    </div>
                </div>

                <div>
                    <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem; text-transform: uppercase; letter-spacing: 0.05em;">Rewards</div>
                    <div style="padding: 1.5rem; background: rgba(255, 255, 255, 0.02); border: 1px solid var(--border-color); border-radius: 0.5rem;">
                        <div style="display: grid; grid-template-columns: repeat(2, 1fr); gap: 1rem;">
                            <div>
                                <div style="font-size: 0.875rem; color: var(--text-tertiary); margin-bottom: 0.25rem;">XP Reward</div>
                                <div style="font-size: 1.125rem; font-weight: 500; color: var(--text-primary);">${questBuilderState.quest.rewardXP}</div>
                            </div>
                            <div>
                                <div style="font-size: 0.875rem; color: var(--text-tertiary); margin-bottom: 0.25rem;">Karma Reward</div>
                                <div style="font-size: 1.125rem; font-weight: 500; color: var(--text-primary);">${questBuilderState.quest.rewardKarma}</div>
                            </div>
                        </div>
                        ${questBuilderState.quest.rewardNFTs && questBuilderState.quest.rewardNFTs.length > 0 ? `
                        <div style="margin-top: 1rem; padding-top: 1rem; border-top: 1px solid var(--border-color);">
                            <div style="font-size: 0.875rem; color: var(--text-tertiary); margin-bottom: 0.5rem;">NFT Rewards (${questBuilderState.quest.rewardNFTs.length})</div>
                            <div style="display: flex; flex-wrap: wrap; gap: 0.5rem;">
                                ${questBuilderState.quest.rewardNFTs.map(nftId => {
                                    const nft = questBuilderState.userNFTs.find(n => n.id === nftId);
                                    return `
                                    <div style="
                                        padding: 0.5rem;
                                        background: rgba(59, 130, 246, 0.1);
                                        border: 1px solid rgba(59, 130, 246, 0.3);
                                        border-radius: 0.375rem;
                                        font-size: 0.75rem;
                                        color: var(--text-primary);
                                    ">
                                        ${escapeHtml(nft?.title || nftId.substring(0, 8) + '...')}
                                    </div>
                                    `;
                                }).join('')}
                            </div>
                        </div>
                        ` : ''}
                    </div>
                </div>
            </div>

            <div style="display: flex; gap: 1rem; margin-top: 2rem;">
                <button 
                    class="btn-secondary"
                    onclick="previousQuestBuilderStep()"
                    style="flex: 1;"
                >
                    Back
                </button>
                <button 
                    class="btn-primary"
                    onclick="createQuest()"
                    style="flex: 1;"
                    id="create-quest-btn"
                >
                    Create Quest
                </button>
            </div>
        </div>
    `;
}

/**
 * Render Quest Builder Navigation
 */
function renderQuestBuilderNavigation() {
    return `
        <div style="display: flex; gap: 1rem; margin-top: 2rem; justify-content: space-between;">
            <button 
                class="btn-secondary"
                onclick="previousQuestBuilderStep()"
                ${questBuilderState.currentStep === 1 ? 'disabled style="opacity: 0.5; cursor: not-allowed;"' : ''}
            >
                Previous
            </button>
            <button 
                class="btn-primary"
                onclick="nextQuestBuilderStep()"
            >
                Next
            </button>
        </div>
    `;
}

/**
 * Attach Quest Builder Listeners
 */
function attachQuestBuilderListeners() {
    // Basic info inputs
    const nameInput = document.getElementById('quest-name-input');
    if (nameInput) {
        nameInput.addEventListener('input', (e) => {
            questBuilderState.quest.name = e.target.value;
        });
    }

    const descriptionInput = document.getElementById('quest-description-input');
    if (descriptionInput) {
        descriptionInput.addEventListener('input', (e) => {
            questBuilderState.quest.description = e.target.value;
        });
    }

    const typeInput = document.getElementById('quest-type-input');
    if (typeInput) {
        typeInput.addEventListener('change', (e) => {
            questBuilderState.quest.questType = e.target.value;
        });
    }

    const difficultyInput = document.getElementById('quest-difficulty-input');
    if (difficultyInput) {
        difficultyInput.addEventListener('change', (e) => {
            questBuilderState.quest.difficulty = e.target.value;
        });
    }

    // Rewards inputs
    const xpInput = document.getElementById('reward-xp-input');
    if (xpInput) {
        xpInput.addEventListener('input', (e) => {
            questBuilderState.quest.rewardXP = parseInt(e.target.value) || 0;
        });
    }

    const karmaInput = document.getElementById('reward-karma-input');
    if (karmaInput) {
        karmaInput.addEventListener('input', (e) => {
            questBuilderState.quest.rewardKarma = parseInt(e.target.value) || 0;
        });
    }

    // Objectives inputs
    document.querySelectorAll('.objective-description-input').forEach(input => {
        input.addEventListener('input', (e) => {
            const index = parseInt(e.target.dataset.objectiveIndex);
            if (questBuilderState.quest.objectives[index]) {
                questBuilderState.quest.objectives[index].description = e.target.value;
                questBuilderState.quest.objectives[index].text = e.target.value;
            }
        });
    });

    document.querySelectorAll('.objective-type-input').forEach(select => {
        select.addEventListener('change', (e) => {
            const index = parseInt(e.target.dataset.objectiveIndex);
            if (questBuilderState.quest.objectives[index]) {
                questBuilderState.quest.objectives[index].type = e.target.value;
            }
        });
    });

    document.querySelectorAll('.objective-target-input').forEach(input => {
        input.addEventListener('input', (e) => {
            const index = parseInt(e.target.dataset.objectiveIndex);
            if (questBuilderState.quest.objectives[index]) {
                questBuilderState.quest.objectives[index].target = parseInt(e.target.value) || null;
            }
        });
    });
}

/**
 * Next Quest Builder Step
 */
function nextQuestBuilderStep() {
    // Validate current step
    if (questBuilderState.currentStep === 1) {
        if (!questBuilderState.quest.name.trim()) {
            alert('Please enter a quest name');
            return;
        }
        if (!questBuilderState.quest.description.trim()) {
            alert('Please enter a quest description');
            return;
        }
    }

    if (questBuilderState.currentStep < questBuilderState.totalSteps) {
        questBuilderState.currentStep++;
        updateQuestBuilderContent();
    }
}

/**
 * Previous Quest Builder Step
 */
function previousQuestBuilderStep() {
    if (questBuilderState.currentStep > 1) {
        questBuilderState.currentStep--;
        updateQuestBuilderContent();
    }
}

/**
 * Update Quest Builder Content
 */
function updateQuestBuilderContent() {
    const container = document.getElementById('quest-builder-container') || document.getElementById('star-dashboard-content');
    if (container) {
        const stepsHtml = renderQuestBuilderSteps();
        const contentHtml = renderQuestBuilderContent();
        container.innerHTML = stepsHtml + contentHtml;
        attachQuestBuilderListeners();
    }
}

/**
 * Add Quest Objective
 */
function addQuestObjective() {
    questBuilderState.quest.objectives.push({
        description: '',
        text: '',
        type: 'Action',
        target: null
    });
    updateQuestBuilderContent();
}

/**
 * Remove Quest Objective
 */
function removeQuestObjective(index) {
    questBuilderState.quest.objectives.splice(index, 1);
    updateQuestBuilderContent();
}

/**
 * Create Quest
 */
async function createQuest() {
    const createBtn = document.getElementById('create-quest-btn');
    if (createBtn) {
        createBtn.disabled = true;
        createBtn.textContent = 'Creating...';
    }

    try {
        const authData = localStorage.getItem('oasis_auth');
        let authToken = null;
        let avatarId = null;
        if (authData) {
            const auth = JSON.parse(authData);
            authToken = auth.token;
            avatarId = auth.avatarId || auth.avatar?.id;
        }

        // Prepare request
        const request = {
            name: questBuilderState.quest.name,
            description: questBuilderState.quest.description,
            questType: questBuilderState.quest.questType,
            difficulty: questBuilderState.quest.difficulty,
            rewardKarma: questBuilderState.quest.rewardKarma,
            rewardXP: questBuilderState.quest.rewardXP,
            objectives: questBuilderState.quest.objectives.map((obj, index) => ({
                description: obj.description || obj.text,
                type: obj.type || 'Action',
                target: obj.target || null
            }))
        };

        const response = await fetch(`${questBuilderState.baseUrl}/api/quests`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': authToken ? `Bearer ${authToken}` : ''
            },
            body: JSON.stringify({
                ...request,
                createdByAvatarId: avatarId
            })
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Failed to create quest');
        }

        const result = await response.json();
        
        alert('Quest created successfully!');
        
        // Return to quest browser or STAR dashboard
        if (typeof showQuestBrowser === 'function') {
            showQuestBrowser();
        } else if (typeof initSTARDashboard === 'function') {
            // Clear the builder container and reload STAR dashboard
            const builderContainer = document.getElementById('quest-builder-container');
            if (builderContainer) {
                builderContainer.remove();
            }
            initSTARDashboard();
        }
    } catch (error) {
        console.error('Error creating quest:', error);
        alert('Failed to create quest: ' + error.message);
        
        if (createBtn) {
            createBtn.disabled = false;
            createBtn.textContent = 'Create Quest';
        }
    }
}

/**
 * Load User NFTs
 */
async function loadUserNFTs() {
    try {
        const authData = localStorage.getItem('oasis_auth');
        let authToken = null;
        let avatarId = null;
        if (authData) {
            const auth = JSON.parse(authData);
            authToken = auth.token;
            avatarId = auth.avatarId || auth.avatar?.id;
            questBuilderState.authToken = authToken;
            questBuilderState.avatarId = avatarId;
        }

        if (!avatarId) {
            console.warn('No avatar ID found, cannot load NFTs');
            return;
        }

        const response = await fetch(`${questBuilderState.baseUrl}/api/nft/load-all-nfts-for_avatar/${avatarId}`, {
            headers: {
                'Authorization': authToken ? `Bearer ${authToken}` : ''
            }
        });

        if (response.ok) {
            const data = await response.json();
            questBuilderState.userNFTs = data.result || [];
            console.log(`Loaded ${questBuilderState.userNFTs.length} NFTs for quest builder`);
        } else {
            console.error('Failed to load NFTs:', response.statusText);
        }
    } catch (error) {
        console.error('Error loading user NFTs:', error);
    }
}

/**
 * Render NFT Rewards Selector
 */
function renderNFTRewardsSelector() {
    if (questBuilderState.userNFTs.length === 0) {
        return `
            <div style="text-align: center; padding: 1rem;">
                <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0 0 0.5rem 0;">
                    No NFTs found. Create NFTs first to use them as rewards.
                </p>
                <button 
                    class="btn-text"
                    onclick="window.location.href='#nfts'; switchTab('nfts');"
                    style="font-size: 0.75rem;"
                >
                    Go to NFT Mint Studio →
                </button>
            </div>
        `;
    }

    const selectedNFTIds = questBuilderState.quest.rewardNFTs || [];

    return `
        <div style="display: flex; flex-direction: column; gap: 1rem;">
            <div style="display: flex; justify-content: space-between; align-items: center;">
                <span style="font-size: 0.8125rem; color: var(--text-secondary);">
                    Select NFTs to give as rewards (${selectedNFTIds.length} selected)
                </span>
                <button 
                    class="btn-text"
                    onclick="showNFTSelectorModal()"
                    style="font-size: 0.75rem;"
                >
                    ${selectedNFTIds.length === 0 ? 'Select NFTs' : 'Change Selection'}
                </button>
            </div>
            ${selectedNFTIds.length > 0 ? `
            <div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(120px, 1fr)); gap: 0.75rem;" id="selected-nft-rewards">
                ${selectedNFTIds.map(nftId => {
                    const nft = questBuilderState.userNFTs.find(n => n.id === nftId);
                    if (!nft) return '';
                    return `
                    <div style="
                        padding: 0.75rem;
                        background: rgba(59, 130, 246, 0.05);
                        border: 1px solid rgba(59, 130, 246, 0.2);
                        border-radius: 0.5rem;
                        cursor: pointer;
                        transition: all 0.2s;
                    " onclick="removeNFTReward('${nftId}')" title="Click to remove">
                        ${nft.imageUrl ? `
                        <div style="
                            width: 100%;
                            aspect-ratio: 1;
                            background: rgba(255, 255, 255, 0.05);
                            border-radius: 0.375rem;
                            margin-bottom: 0.5rem;
                            overflow: hidden;
                        ">
                            <img src="${escapeHtml(nft.imageUrl)}" alt="${escapeHtml(nft.title || 'NFT')}" style="width: 100%; height: 100%; object-fit: cover;" />
                        </div>
                        ` : `
                        <div style="
                            width: 100%;
                            aspect-ratio: 1;
                            background: rgba(59, 130, 246, 0.1);
                            border-radius: 0.375rem;
                            margin-bottom: 0.5rem;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            color: rgba(59, 130, 246, 0.5);
                            font-size: 1.5rem;
                        ">NFT</div>
                        `}
                        <div style="
                            font-size: 0.75rem;
                            color: var(--text-primary);
                            font-weight: 500;
                            text-align: center;
                            overflow: hidden;
                            text-overflow: ellipsis;
                            white-space: nowrap;
                        " title="${escapeHtml(nft.title || 'Unnamed NFT')}">
                            ${escapeHtml((nft.title || 'Unnamed NFT').substring(0, 20))}
                        </div>
                    </div>
                    `;
                }).join('')}
            </div>
            ` : `
            <div style="
                padding: 2rem;
                background: rgba(255, 255, 255, 0.02);
                border: 2px dashed var(--border-color);
                border-radius: 0.5rem;
                text-align: center;
            ">
                <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0;">
                    No NFTs selected. Click "Select NFTs" to add NFT rewards.
                </p>
            </div>
            `}
        </div>
    `;
}

/**
 * Render NFT Selector Modal
 */
function renderNFTSelectorModal() {
    return `
        <div id="nft-selector-modal" style="
            display: none;
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0, 0, 0, 0.8);
            z-index: 10000;
            align-items: center;
            justify-content: center;
            padding: 2rem;
        " onclick="closeNFTSelectorModal(event)">
            <div class="portal-card" style="
                max-width: 800px;
                width: 100%;
                max-height: 90vh;
                overflow-y: auto;
                padding: 2rem;
            " onclick="event.stopPropagation()">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem;">
                    <h3 style="
                        font-size: 1.125rem;
                        font-weight: 500;
                        color: var(--text-primary);
                        margin: 0;
                    ">Select NFT Rewards</h3>
                    <button 
                        class="btn-text"
                        onclick="closeNFTSelectorModal()"
                        style="font-size: 1.5rem; color: var(--text-secondary);"
                    >
                        ×
                    </button>
                </div>
                <div style="
                    display: grid;
                    grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
                    gap: 1rem;
                    margin-bottom: 1.5rem;
                " id="nft-selector-grid">
                    ${questBuilderState.userNFTs.map(nft => {
                        const isSelected = (questBuilderState.quest.rewardNFTs || []).includes(nft.id);
                        return `
                        <div style="
                            padding: 0.75rem;
                            background: ${isSelected ? 'rgba(59, 130, 246, 0.1)' : 'rgba(255, 255, 255, 0.02)'};
                            border: 2px solid ${isSelected ? 'rgba(59, 130, 246, 0.5)' : 'var(--border-color)'};
                            border-radius: 0.5rem;
                            cursor: pointer;
                            transition: all 0.2s;
                        " onclick="toggleNFTReward('${nft.id}')">
                            ${nft.imageUrl ? `
                            <div style="
                                width: 100%;
                                aspect-ratio: 1;
                                background: rgba(255, 255, 255, 0.05);
                                border-radius: 0.375rem;
                                margin-bottom: 0.5rem;
                                overflow: hidden;
                            ">
                                <img src="${escapeHtml(nft.imageUrl)}" alt="${escapeHtml(nft.title || 'NFT')}" style="width: 100%; height: 100%; object-fit: cover;" />
                            </div>
                            ` : `
                            <div style="
                                width: 100%;
                                aspect-ratio: 1;
                                background: rgba(59, 130, 246, 0.1);
                                border-radius: 0.375rem;
                                margin-bottom: 0.5rem;
                                display: flex;
                                align-items: center;
                                justify-content: center;
                                color: rgba(59, 130, 246, 0.5);
                                font-size: 1.5rem;
                            ">NFT</div>
                            `}
                            <div style="
                                font-size: 0.75rem;
                                color: var(--text-primary);
                                font-weight: 500;
                                text-align: center;
                                overflow: hidden;
                                text-overflow: ellipsis;
                                white-space: nowrap;
                            " title="${escapeHtml(nft.title || 'Unnamed NFT')}">
                                ${escapeHtml((nft.title || 'Unnamed NFT').substring(0, 20))}
                            </div>
                            ${isSelected ? `
                            <div style="
                                position: absolute;
                                top: 0.5rem;
                                right: 0.5rem;
                                width: 24px;
                                height: 24px;
                                border-radius: 50%;
                                background: rgba(59, 130, 246, 1);
                                display: flex;
                                align-items: center;
                                justify-content: center;
                                color: white;
                                font-size: 0.75rem;
                                font-weight: bold;
                            ">✓</div>
                            ` : ''}
                        </div>
                        `;
                    }).join('')}
                </div>
                <div style="display: flex; gap: 1rem; justify-content: flex-end;">
                    <button class="btn-secondary" onclick="closeNFTSelectorModal()">Cancel</button>
                    <button class="btn-primary" onclick="confirmNFTRewards()">Confirm Selection</button>
                </div>
            </div>
        </div>
    `;
}

/**
 * Show NFT Selector Modal
 */
function showNFTSelectorModal() {
    const modal = document.getElementById('nft-selector-modal');
    if (modal) {
        modal.style.display = 'flex';
    }
}

/**
 * Close NFT Selector Modal
 */
function closeNFTSelectorModal(event) {
    if (event && event.target !== event.currentTarget) return;
    const modal = document.getElementById('nft-selector-modal');
    if (modal) {
        modal.style.display = 'none';
    }
}

/**
 * Toggle NFT Reward
 */
function toggleNFTReward(nftId) {
    if (!questBuilderState.quest.rewardNFTs) {
        questBuilderState.quest.rewardNFTs = [];
    }
    
    const index = questBuilderState.quest.rewardNFTs.indexOf(nftId);
    if (index > -1) {
        questBuilderState.quest.rewardNFTs.splice(index, 1);
    } else {
        questBuilderState.quest.rewardNFTs.push(nftId);
    }
    
    // Update modal display
    updateNFTSelectorModal();
}

/**
 * Remove NFT Reward
 */
function removeNFTReward(nftId) {
    if (!questBuilderState.quest.rewardNFTs) return;
    
    const index = questBuilderState.quest.rewardNFTs.indexOf(nftId);
    if (index > -1) {
        questBuilderState.quest.rewardNFTs.splice(index, 1);
        updateNFTRewardsDisplay();
    }
}

/**
 * Confirm NFT Rewards
 */
function confirmNFTRewards() {
    closeNFTSelectorModal();
    updateNFTRewardsDisplay();
}

/**
 * Update NFT Rewards Display
 */
function updateNFTRewardsDisplay() {
    const section = document.getElementById('nft-rewards-section');
    if (section && questBuilderState.currentStep === 3) {
        section.innerHTML = renderNFTRewardsSelector();
    }
}

/**
 * Update NFT Selector Modal
 */
function updateNFTSelectorModal() {
    const modal = document.getElementById('nft-selector-modal');
    if (modal) {
        const grid = document.getElementById('nft-selector-grid');
        if (grid) {
            grid.innerHTML = questBuilderState.userNFTs.map(nft => {
                const isSelected = (questBuilderState.quest.rewardNFTs || []).includes(nft.id);
                return `
                <div style="
                    position: relative;
                    padding: 0.75rem;
                    background: ${isSelected ? 'rgba(59, 130, 246, 0.1)' : 'rgba(255, 255, 255, 0.02)'};
                    border: 2px solid ${isSelected ? 'rgba(59, 130, 246, 0.5)' : 'var(--border-color)'};
                    border-radius: 0.5rem;
                    cursor: pointer;
                    transition: all 0.2s;
                " onclick="toggleNFTReward('${nft.id}')">
                    ${nft.imageUrl ? `
                    <div style="
                        width: 100%;
                        aspect-ratio: 1;
                        background: rgba(255, 255, 255, 0.05);
                        border-radius: 0.375rem;
                        margin-bottom: 0.5rem;
                        overflow: hidden;
                    ">
                        <img src="${escapeHtml(nft.imageUrl)}" alt="${escapeHtml(nft.title || 'NFT')}" style="width: 100%; height: 100%; object-fit: cover;" />
                    </div>
                    ` : `
                    <div style="
                        width: 100%;
                        aspect-ratio: 1;
                        background: rgba(59, 130, 246, 0.1);
                        border-radius: 0.375rem;
                        margin-bottom: 0.5rem;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        color: rgba(59, 130, 246, 0.5);
                        font-size: 1.5rem;
                    ">NFT</div>
                    `}
                    <div style="
                        font-size: 0.75rem;
                        color: var(--text-primary);
                        font-weight: 500;
                        text-align: center;
                        overflow: hidden;
                        text-overflow: ellipsis;
                        white-space: nowrap;
                    " title="${escapeHtml(nft.title || 'Unnamed NFT')}">
                        ${escapeHtml((nft.title || 'Unnamed NFT').substring(0, 20))}
                    </div>
                    ${isSelected ? `
                    <div style="
                        position: absolute;
                        top: 0.5rem;
                        right: 0.5rem;
                        width: 24px;
                        height: 24px;
                        border-radius: 50%;
                        background: rgba(59, 130, 246, 1);
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        color: white;
                        font-size: 0.75rem;
                        font-weight: bold;
                    ">✓</div>
                    ` : ''}
                </div>
                `;
            }).join('');
        }
    }
}

/**
 * Handle Objective Type Change
 */
function handleObjectiveTypeChange(index, type) {
    if (questBuilderState.quest.objectives[index]) {
        questBuilderState.quest.objectives[index].type = type;
        if (type === 'Collection') {
            // Initialize NFT selection for collection objective
            if (!questBuilderState.quest.objectives[index].nftId) {
                questBuilderState.quest.objectives[index].nftId = null;
            }
        } else {
            // Remove NFT ID if not collection type
            delete questBuilderState.quest.objectives[index].nftId;
        }
        updateQuestBuilderContent();
    }
}

/**
 * Handle Objective NFT Change
 */
function handleObjectiveNFTChange(index, nftId) {
    if (questBuilderState.quest.objectives[index]) {
        questBuilderState.quest.objectives[index].nftId = nftId || null;
        if (nftId) {
            const nft = questBuilderState.userNFTs.find(n => n.id === nftId);
            if (nft) {
                questBuilderState.quest.objectives[index].nftTitle = nft.title;
                // Auto-fill description if empty
                if (!questBuilderState.quest.objectives[index].description) {
                    questBuilderState.quest.objectives[index].description = `Collect ${nft.title || 'NFT'}`;
                    questBuilderState.quest.objectives[index].text = `Collect ${nft.title || 'NFT'}`;
                }
            }
        }
        // Update the form to reflect changes
        attachQuestBuilderListeners();
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

// Export functions to window
if (typeof window !== 'undefined') {
    window.showQuestCreationInterface = showQuestCreationInterface;
    window.nextQuestBuilderStep = nextQuestBuilderStep;
    window.previousQuestBuilderStep = previousQuestBuilderStep;
    window.addQuestObjective = addQuestObjective;
    window.removeQuestObjective = removeQuestObjective;
    window.createQuest = createQuest;
    window.showNFTSelectorModal = showNFTSelectorModal;
    window.closeNFTSelectorModal = closeNFTSelectorModal;
    window.toggleNFTReward = toggleNFTReward;
    window.removeNFTReward = removeNFTReward;
    window.confirmNFTRewards = confirmNFTRewards;
    window.handleObjectiveTypeChange = handleObjectiveTypeChange;
    window.handleObjectiveNFTChange = handleObjectiveNFTChange;
}

