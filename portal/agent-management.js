// Agent Management Module
// Tracks and manages AI agents/avatars for testing and development

let agentManagementState = {
    agents: [],
    loading: false,
    error: null,
    selectedAgent: null,
    notification: null // { type: 'success'|'error'|'info', message: string }
};

// Show portal-styled notification
function showNotification(type, message, duration = 5000) {
    agentManagementState.notification = { type, message };
    
    // Re-render to show notification
    const container = document.getElementById('tab-agents');
    if (container) {
        container.innerHTML = renderAgentManagement();
        attachAgentManagementListeners();
    }
    
    // Auto-hide after duration
    if (duration > 0) {
        setTimeout(() => {
            agentManagementState.notification = null;
            if (container) {
                container.innerHTML = renderAgentManagement();
                attachAgentManagementListeners();
            }
        }, duration);
    }
}

// Show confirmation dialog (portal-styled)
function showConfirmDialog(message, onConfirm, onCancel) {
    const modal = document.createElement('div');
    modal.className = 'modal';
    modal.style.display = 'flex';
    modal.innerHTML = `
        <div class="modal-content" style="max-width: 400px;">
            <div class="modal-header">
                <h3>Confirm</h3>
                <button class="modal-close" onclick="this.closest('.modal').remove()">&times;</button>
            </div>
            <div class="modal-body">
                <p style="color: var(--text-secondary); margin-bottom: 1.5rem;">${message}</p>
                <div style="display: flex; gap: 1rem; justify-content: flex-end;">
                    <button class="btn-secondary" onclick="this.closest('.modal').remove(); ${onCancel ? 'onCancel()' : ''}">
                        Cancel
                    </button>
                    <button class="btn-primary" onclick="this.closest('.modal').remove(); ${onConfirm ? 'onConfirm()' : ''}">
                        Confirm
                    </button>
                </div>
            </div>
        </div>
    `;
    document.body.appendChild(modal);
    
    // Close on backdrop click
    modal.addEventListener('click', (e) => {
        if (e.target === modal) {
            modal.remove();
            if (onCancel) onCancel();
        }
    });
    
    // Store callbacks
    const confirmBtn = modal.querySelector('.btn-primary');
    const cancelBtn = modal.querySelector('.btn-secondary');
    
    confirmBtn.onclick = () => {
        modal.remove();
        if (onConfirm) onConfirm();
    };
    
    cancelBtn.onclick = () => {
        modal.remove();
        if (onCancel) onCancel();
    };
}

// Get current logged-in avatar ID
function getCurrentAvatarId() {
    try {
        if (typeof oasisAPI !== 'undefined' && typeof oasisAPI.getAvatarId === 'function') {
            return oasisAPI.getAvatarId();
        }
        // Fallback to localStorage
        const authData = localStorage.getItem('oasis_auth');
        if (authData) {
            const auth = JSON.parse(authData);
            const avatar = auth.avatar;
            return avatar?.avatarId || avatar?.id || null;
        }
    } catch (error) {
        console.error('Error getting current avatar ID:', error);
    }
    return null;
}

// Get current logged-in avatar (for email)
function getCurrentAvatar() {
    try {
        if (typeof oasisAPI !== 'undefined' && typeof oasisAPI.getAvatar === 'function') {
            return oasisAPI.getAvatar();
        }
        // Fallback to localStorage
        const authData = localStorage.getItem('oasis_auth');
        if (authData) {
            const auth = JSON.parse(authData);
            return auth.avatar || null;
        }
    } catch (error) {
        console.error('Error getting current avatar:', error);
    }
    return null;
}

// Load agents from localStorage on init (filtered by current user)
function loadAgentsFromStorage() {
    try {
        const stored = localStorage.getItem('oasis_agents');
        const deletedAgents = JSON.parse(localStorage.getItem('oasis_deleted_agents') || '[]');
        
        if (stored) {
            const allAgents = JSON.parse(stored);
            const currentAvatarId = getCurrentAvatarId();
            
            // Filter agents to only show those belonging to the current user AND not deleted
            if (currentAvatarId) {
                agentManagementState.agents = allAgents.filter(agent => 
                    agent.parentAvatarId === currentAvatarId && 
                    !deletedAgents.includes(agent.avatarId)
                );
                
                // Migrate old agents (without parentAvatarId) to current user if they exist
                // This handles agents created before the linking feature
                // BUT exclude deleted agents
                const orphanedAgents = allAgents.filter(agent => 
                    !agent.parentAvatarId && 
                    !deletedAgents.includes(agent.avatarId)
                );
                if (orphanedAgents.length > 0) {
                    orphanedAgents.forEach(agent => {
                        agent.parentAvatarId = currentAvatarId;
                        agentManagementState.agents.push(agent);
                    });
                    // Save migrated agents
                    saveAgentsToStorage();
                }
            } else {
                // If not logged in, show empty list
                agentManagementState.agents = [];
            }
        }
    } catch (error) {
        console.error('Error loading agents from storage:', error);
    }
}

// Save agents to localStorage (merge with existing agents from other users)
function saveAgentsToStorage() {
    try {
        // Load all agents (from all users)
        let allAgents = [];
        const stored = localStorage.getItem('oasis_agents');
        if (stored) {
            allAgents = JSON.parse(stored);
        }
        
        // Get deleted agents list and filter them out
        const deletedAgents = JSON.parse(localStorage.getItem('oasis_deleted_agents') || '[]');
        allAgents = allAgents.filter(agent => !deletedAgents.includes(agent.avatarId));
        
        const currentAvatarId = getCurrentAvatarId();
        
        // Remove old agents for current user
        allAgents = allAgents.filter(agent => agent.parentAvatarId !== currentAvatarId);
        
        // Add current user's agents (excluding deleted ones)
        const activeAgents = agentManagementState.agents.filter(agent => !deletedAgents.includes(agent.avatarId));
        allAgents.push(...activeAgents);
        
        // Save all agents
        localStorage.setItem('oasis_agents', JSON.stringify(allAgents));
    } catch (error) {
        console.error('Error saving agents to storage:', error);
    }
}

// Generate deterministic password for test agents
function generateAgentPassword(username) {
    // Use a simple hash function for deterministic passwords
    let hash = 0;
    for (let i = 0; i < username.length; i++) {
        const char = username.charCodeAt(i);
        hash = ((hash << 5) - hash) + char;
        hash = hash & hash; // Convert to 32bit integer
    }
    const hashStr = Math.abs(hash).toString(16).substring(0, 16);
    return `test_${hashStr}`;
}

// Load agent management module
async function loadAgentManagement() {
    const container = document.getElementById('tab-agents');
    if (!container) return;

    loadAgentsFromStorage();
    
    try {
        agentManagementState.loading = true;
        container.innerHTML = renderAgentManagement();
        attachAgentManagementListeners();
        
        // Refresh agent data from API if we have any
        if (agentManagementState.agents.length > 0) {
            await refreshAgentData();
        }
    } catch (error) {
        console.error('Error loading agent management:', error);
        agentManagementState.error = error.message;
    } finally {
        agentManagementState.loading = false;
        // Re-render after loading completes
        container.innerHTML = renderAgentManagement();
        attachAgentManagementListeners();
    }
}

// Render agent management UI
function renderAgentManagement() {
    if (agentManagementState.loading) {
        return `
            <div class="portal-section">
                <div class="portal-section-header">
                    <div>
                        <h2 class="portal-section-title">Agent Management</h2>
                        <p class="portal-section-subtitle">Track and manage your AI agents and test avatars</p>
                    </div>
                </div>
                <div class="portal-card">
                    <p>Loading agents...</p>
                </div>
            </div>
        `;
    }

    return `
        <div class="portal-section">
            <div class="portal-section-header">
                <div>
                    <h2 class="portal-section-title">Agent Management</h2>
                    <p class="portal-section-subtitle">Track and manage your AI agents and test avatars</p>
                </div>
                <button class="btn-primary" onclick="showCreateAgentModal()">
                    <span>+</span> Create Agent
                </button>
            </div>

            ${agentManagementState.error ? `
                <div class="portal-card" style="border-color: var(--error-color); background: rgba(239, 68, 68, 0.1);">
                    <div style="display: flex; align-items: center; gap: 0.75rem;">
                        <span style="font-size: 1.25rem;">⚠️</span>
                        <div>
                            <p style="color: var(--error-color); font-weight: 500; margin: 0;">Error</p>
                            <p style="color: var(--text-secondary); font-size: 0.875rem; margin: 0.25rem 0 0 0;">${agentManagementState.error}</p>
                        </div>
                    </div>
                </div>
            ` : ''}

            ${agentManagementState.notification ? `
                <div class="portal-card notification-card notification-${agentManagementState.notification.type}" style="
                    border-color: ${agentManagementState.notification.type === 'success' ? 'rgba(34, 197, 94, 0.3)' : agentManagementState.notification.type === 'error' ? 'rgba(239, 68, 68, 0.3)' : 'rgba(59, 130, 246, 0.3)'};
                    background: ${agentManagementState.notification.type === 'success' ? 'rgba(34, 197, 94, 0.1)' : agentManagementState.notification.type === 'error' ? 'rgba(239, 68, 68, 0.1)' : 'rgba(59, 130, 246, 0.1)'};
                    margin-bottom: 1.5rem;
                ">
                    <div style="display: flex; align-items: center; justify-content: space-between;">
                        <div style="display: flex; align-items: center; gap: 0.75rem;">
                            <span style="font-size: 1.25rem;">
                                ${agentManagementState.notification.type === 'success' ? '✅' : agentManagementState.notification.type === 'error' ? '❌' : 'ℹ️'}
                            </span>
                            <p style="color: ${agentManagementState.notification.type === 'success' ? 'rgba(34, 197, 94, 1)' : agentManagementState.notification.type === 'error' ? 'rgba(239, 68, 68, 1)' : 'rgba(96, 165, 250, 1)'}; margin: 0; font-weight: 500;">
                                ${agentManagementState.notification.message}
                            </p>
                        </div>
                        <button 
                            onclick="agentManagementState.notification = null; const container = document.getElementById('tab-agents'); if (container) { container.innerHTML = renderAgentManagement(); attachAgentManagementListeners(); }"
                            style="background: none; border: none; color: var(--text-tertiary); cursor: pointer; padding: 0.25rem; font-size: 1.25rem; line-height: 1; transition: color 0.2s;"
                            title="Dismiss"
                        >
                            ×
                        </button>
                    </div>
                </div>
            ` : ''}

            ${!getCurrentAvatarId() ? `
                <div class="portal-card" style="border-color: var(--error-color);">
                    <div style="text-align: center; padding: 3rem 0;">
                        <p style="color: var(--error-color); margin-bottom: 1rem;">⚠️ Please log in first</p>
                        <p class="empty-state-text" style="color: var(--text-secondary);">
                            You need to be logged in to view and manage your agents.
                        </p>
                        <p class="empty-state-text" style="margin-top: 0.5rem; color: var(--text-tertiary); font-size: 0.75rem;">
                            Agents are linked to your account, so you can manage them from a single place.
                        </p>
                    </div>
                </div>
            ` : agentManagementState.agents.length === 0 ? `
                <div class="portal-card">
                    <div style="text-align: center; padding: 3rem 0;">
                        <p class="empty-state-text">No agents created yet.</p>
                        <p class="empty-state-text" style="margin-top: 0.5rem; color: var(--text-tertiary);">
                            Create your first agent to start testing payments and interactions.
                        </p>
                        <p class="empty-state-text" style="margin-top: 0.5rem; color: var(--text-tertiary); font-size: 0.75rem;">
                            Agents will be linked to your account (${getCurrentAvatarId().substring(0, 8)}...)
                        </p>
                        <button class="btn-primary" onclick="showCreateAgentModal()" style="margin-top: 1.5rem;">
                            Create Agent
                        </button>
                    </div>
                </div>
            ` : `
                <div class="portal-grid portal-grid-3">
                    ${agentManagementState.agents.map(agent => renderAgentCard(agent)).join('')}
                </div>
                <div style="margin-top: 2rem; padding: 1rem; background: rgba(255, 255, 255, 0.05); border-radius: 6px; font-size: 0.875rem; color: var(--text-secondary);">
                    <strong>Note:</strong> These agents are linked to your account (${getCurrentAvatarId() ? 'Avatar ID: ' + getCurrentAvatarId().substring(0, 8) + '...' : 'Not logged in'}). 
                    They are test agents you can use for testing payments between agents. Click on any agent card to view details and create wallets.
                </div>
            `}
        </div>

        ${renderCreateAgentModal()}
        ${renderAgentDetailModal()}
    `;
}

// Render agent card
function renderAgentCard(agent) {
    const solanaWallet = agent.wallets?.find(w => 
        w.providerType === 'SolanaOASIS' || w.providerType === 'Solana' || w.providerType === 3
    );
    const walletAddress = solanaWallet?.address || solanaWallet?.walletAddress || solanaWallet?.publicKey || 'No wallet';
    const walletShort = walletAddress.length > 20 ? `${walletAddress.substring(0, 10)}...${walletAddress.substring(walletAddress.length - 8)}` : walletAddress;

    return `
        <div class="portal-card agent-card" data-agent-id="${agent.avatarId}">
            <div class="portal-card-header">
                <div class="portal-card-title" style="cursor: pointer;" onclick="showAgentDetails('${agent.avatarId}')">${agent.username || 'Unknown'}</div>
                <div style="display: flex; align-items: center; gap: 0.5rem;">
                    <span class="agent-status ${agent.isActive ? 'active' : 'inactive'}"></span>
                    <button 
                        class="btn-delete-agent" 
                        onclick="event.stopPropagation(); handleDeleteAgent('${agent.avatarId}', '${agent.username || 'Unknown'}')"
                        title="Delete agent"
                        style="background: none; border: none; color: var(--text-tertiary); cursor: pointer; padding: 0.25rem; font-size: 1rem; line-height: 1; transition: color 0.2s;"
                    >
                        ×
                    </button>
                </div>
            </div>
            <div style="cursor: pointer;" onclick="showAgentDetails('${agent.avatarId}')">
                <div style="margin-top: 1rem;">
                    <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem;">Avatar ID</div>
                    <div style="font-size: 0.875rem; color: var(--text-secondary); font-family: monospace; word-break: break-all;">
                        ${agent.avatarId?.substring(0, 8)}...
                    </div>
                </div>
                <div style="margin-top: 1rem;">
                    <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem;">Solana Wallet</div>
                    <div style="font-size: 0.875rem; color: var(--text-primary); font-family: monospace; word-break: break-all;">
                        ${walletShort}
                    </div>
                </div>
                <div style="margin-top: 1rem; display: flex; gap: 0.5rem; flex-wrap: wrap;">
                    <span style="font-size: 0.75rem; padding: 0.25rem 0.5rem; background: rgba(255, 255, 255, 0.1); border-radius: 4px;">
                        ${agent.wallets?.length || 0} wallet(s)
                    </span>
                    ${agent.lastUsed ? `
                        <span style="font-size: 0.75rem; padding: 0.25rem 0.5rem; background: rgba(255, 255, 255, 0.1); border-radius: 4px;">
                            Used ${formatTimeAgo(agent.lastUsed)}
                        </span>
                    ` : ''}
                </div>
            </div>
        </div>
    `;
}

// Render create agent modal
function renderCreateAgentModal() {
    return `
        <div id="createAgentModal" class="modal" style="display: none;">
            <div class="modal-content">
                <div class="modal-header">
                    <h3>Create New Agent</h3>
                    <button class="modal-close" onclick="closeCreateAgentModal()">&times;</button>
                </div>
                <div class="modal-body">
                    <form id="createAgentForm" onsubmit="handleCreateAgent(event)">
                        <div style="margin-bottom: 1.5rem;">
                            <label style="display: block; margin-bottom: 0.5rem; font-size: 0.875rem; color: var(--text-secondary);">
                                Username
                            </label>
                            <input 
                                type="text" 
                                id="agentUsername" 
                                required 
                                placeholder="agent_name"
                                style="width: 100%; padding: 0.75rem; background: rgba(255, 255, 255, 0.05); border: 1px solid var(--border-color); border-radius: 6px; color: var(--text-primary); font-family: inherit;"
                            />
                            <p style="font-size: 0.75rem; color: var(--text-tertiary); margin-top: 0.5rem;">
                                Password will be auto-generated (deterministic based on username)
                            </p>
                        </div>
                        <div style="margin-bottom: 1.5rem; padding: 1rem; background: rgba(59, 130, 246, 0.1); border: 1px solid rgba(59, 130, 246, 0.3); border-radius: 6px;">
                            <p style="font-size: 0.875rem; color: var(--text-secondary); margin: 0;">
                                <strong>ℹ️ Note:</strong> Agents are linked to your account and don't need real email addresses. 
                                A placeholder email will be auto-generated for API requirements.
                            </p>
                        </div>
                        <div style="margin-bottom: 1.5rem; padding: 1rem; background: rgba(34, 197, 94, 0.1); border: 1px solid rgba(34, 197, 94, 0.3); border-radius: 6px;">
                            <p style="font-size: 0.875rem; color: var(--text-secondary); margin: 0;">
                                <strong>✓ Standard:</strong> A Solana wallet will be automatically created for this agent.
                            </p>
                        </div>
                        <div style="display: flex; gap: 1rem; justify-content: flex-end;">
                            <button type="button" class="btn-secondary" onclick="closeCreateAgentModal()">
                                Cancel
                            </button>
                            <button type="submit" class="btn-primary">
                                Create Agent
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    `;
}

// Render agent detail modal
function renderAgentDetailModal() {
    if (!agentManagementState.selectedAgent) return '';

    const agent = agentManagementState.selectedAgent;
    const solanaWallet = agent.wallets?.find(w => 
        w.providerType === 'SolanaOASIS' || w.providerType === 'Solana' || w.providerType === 3
    );

    return `
        <div id="agentDetailModal" class="modal" style="display: none;">
            <div class="modal-content" style="max-width: 600px;">
                <div class="modal-header">
                    <h3>Agent Details: ${agent.username}</h3>
                    <button class="modal-close" onclick="closeAgentDetailModal()">&times;</button>
                </div>
                <div class="modal-body">
                    <div style="margin-bottom: 1.5rem;">
                        <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem;">Avatar ID</div>
                        <div style="font-size: 0.875rem; color: var(--text-primary); font-family: monospace; word-break: break-all; padding: 0.75rem; background: rgba(255, 255, 255, 0.05); border-radius: 6px;">
                            ${agent.avatarId}
                        </div>
                    </div>
                    <div style="margin-bottom: 1.5rem;">
                        <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem;">Username</div>
                        <div style="font-size: 0.875rem; color: var(--text-primary);">
                            ${agent.username}
                        </div>
                    </div>
                    <div style="margin-bottom: 1.5rem;">
                        <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem;">Email</div>
                        <div style="font-size: 0.875rem; color: var(--text-primary);">
                            ${agent.email || 'N/A'}
                        </div>
                    </div>
                    ${solanaWallet ? `
                        <div style="margin-bottom: 1.5rem;">
                            <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem;">Solana Wallet Address</div>
                            <div style="font-size: 0.875rem; color: var(--text-primary); font-family: monospace; word-break: break-all; padding: 0.75rem; background: rgba(255, 255, 255, 0.05); border-radius: 6px;">
                                ${solanaWallet.address || solanaWallet.walletAddress || solanaWallet.publicKey}
                            </div>
                            <div style="margin-top: 0.5rem; display: flex; gap: 0.5rem;">
                                <button class="btn-secondary" onclick="copyToClipboard('${solanaWallet.address || solanaWallet.walletAddress || solanaWallet.publicKey}')">
                                    Copy Address
                                </button>
                                <a href="https://explorer.solana.com/address/${solanaWallet.address || solanaWallet.walletAddress || solanaWallet.publicKey}?cluster=devnet" target="_blank" class="btn-secondary">
                                    View on Explorer
                                </a>
                            </div>
                        </div>
                    ` : `
                        <div style="margin-bottom: 1.5rem;">
                            <p style="color: var(--text-tertiary); font-size: 0.875rem;">No Solana wallet found</p>
                            <button class="btn-primary" onclick="handleCreateWalletForAgent('${agent.avatarId}')" style="margin-top: 0.5rem;">
                                Create Solana Wallet
                            </button>
                        </div>
                    `}
                    <div style="margin-bottom: 1.5rem;">
                        <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem;">All Wallets</div>
                        <div style="display: flex; flex-direction: column; gap: 0.5rem;">
                            ${(agent.wallets || []).map(wallet => `
                                <div style="padding: 0.75rem; background: rgba(255, 255, 255, 0.05); border-radius: 6px; font-size: 0.875rem;">
                                    <div style="color: var(--text-primary); font-weight: 500;">${wallet.providerType || 'Unknown'}</div>
                                    <div style="color: var(--text-secondary); font-family: monospace; margin-top: 0.25rem; word-break: break-all;">
                                        ${wallet.address || wallet.walletAddress || wallet.publicKey || 'N/A'}
                                    </div>
                                </div>
                            `).join('')}
                        </div>
                    </div>
                    <div style="display: flex; gap: 1rem; margin-top: 2rem;">
                        <button class="btn-secondary" onclick="refreshAgentData('${agent.avatarId}')">
                            Refresh Data
                        </button>
                        <button class="btn-secondary" onclick="deleteAgent('${agent.avatarId}')" style="color: var(--error-color);">
                            Delete Agent
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;
}

// Attach event listeners
function attachAgentManagementListeners() {
    // Modal backdrop click to close
    const modals = document.querySelectorAll('.modal');
    modals.forEach(modal => {
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                modal.style.display = 'none';
            }
        });
    });
}

// Show create agent modal
function showCreateAgentModal() {
    const modal = document.getElementById('createAgentModal');
    if (modal) {
        modal.style.display = 'flex';
    }
}

// Close create agent modal
function closeCreateAgentModal() {
    const modal = document.getElementById('createAgentModal');
    if (modal) {
        modal.style.display = 'none';
        document.getElementById('createAgentForm')?.reset();
    }
}

// Show agent details modal
async function showAgentDetails(avatarId) {
    const agent = agentManagementState.agents.find(a => a.avatarId === avatarId);
    if (!agent) {
        console.error('Agent not found:', avatarId);
        return;
    }

    agentManagementState.selectedAgent = agent;
    
    // Re-render to include the modal
    const container = document.getElementById('tab-agents');
    if (container) {
        container.innerHTML = renderAgentManagement();
        attachAgentManagementListeners();
    }
    
    // Show the modal
    const modal = document.getElementById('agentDetailModal');
    if (modal) {
        modal.style.display = 'flex';
        
        // Refresh agent data from API in the background
        try {
            await refreshAgentData(avatarId);
            // Update selected agent with fresh data
            agentManagementState.selectedAgent = agentManagementState.agents.find(a => a.avatarId === avatarId);
            // Re-render modal with updated data
            if (modal.style.display === 'flex') {
                container.innerHTML = renderAgentManagement();
                attachAgentManagementListeners();
                document.getElementById('agentDetailModal').style.display = 'flex';
            }
        } catch (error) {
            console.error('Error refreshing agent data:', error);
        }
    } else {
        console.error('Modal element not found');
    }
}

// Close agent detail modal
function closeAgentDetailModal() {
    const modal = document.getElementById('agentDetailModal');
    if (modal) {
        modal.style.display = 'none';
        agentManagementState.selectedAgent = null;
    }
}

// Handle create agent form submission
async function handleCreateAgent(event) {
    event.preventDefault();
    
    const currentAvatarId = getCurrentAvatarId();
    if (!currentAvatarId) {
        showNotification('error', 'Please log in first to create agents linked to your account.');
        return;
    }
    
    const username = document.getElementById('agentUsername').value.trim();
    
    // Always use @agents.local for agents - this ensures they are auto-verified by the API
    // Using parent's domain causes email conflicts and verification issues
    // Add timestamp to ensure uniqueness
    const timestamp = Date.now();
    const email = `agent_${username}_${timestamp}@agents.local`;
    
    // Wallets are created by default for all agents (standard behavior)
    const createWallet = true;
    
    if (!username) {
        showNotification('error', 'Please enter a username');
        return;
    }

    try {
        // Register agent
        const registerResult = await oasisAPI.request('/api/avatar/register', {
            method: 'POST',
            body: JSON.stringify({
                username: username,
                email: email,
                password: generateAgentPassword(username),
                confirmPassword: generateAgentPassword(username),
                firstName: 'Agent',
                lastName: username,
                title: 'Agent',
                avatarType: 'User',
                acceptTerms: true
            })
        });

        // Check for error in nested structure
        console.log('Registration result:', JSON.stringify(registerResult, null, 2));
        const isError = registerResult.isError || 
                       registerResult.result?.isError || 
                       registerResult.result?.result?.isError ||
                       false;

        // Check for username/email already exists error
        const errorMessage = registerResult.message || 
                            registerResult.result?.message || 
                            registerResult.result?.result?.message || 
                            '';
        const usernameExists = errorMessage.toLowerCase().includes('username already') || 
                              errorMessage.toLowerCase().includes('already exists') ||
                              errorMessage.toLowerCase().includes('already in use');
        
        if (isError && usernameExists && !registerResult.result?.result?.avatarId) {
            throw new Error(`Username "${username}" is already taken. The previous agent may not have been fully deleted yet. Please wait a moment and try again, or use a different username.`);
        }

        // Check if registration was actually successful (has avatarId even if isError is true)
        const hasAvatarId = registerResult.result?.result?.avatarId || 
                           registerResult.result?.result?.id ||
                           registerResult.result?.avatarId || 
                           registerResult.result?.id ||
                           registerResult.avatarId ||
                           registerResult.id;

        // If registration succeeded (has avatarId), skip the error path and go directly to wallet creation
        if (hasAvatarId && !isError) {
            // Registration was successful - extract avatarId and proceed to wallet creation
            const avatarId = registerResult.result?.result?.avatarId || 
                           registerResult.result?.result?.id ||
                           registerResult.result?.avatarId || 
                           registerResult.result?.id ||
                           registerResult.avatarId ||
                           registerResult.id;
            
            console.log('Registration successful, avatarId:', avatarId);
            
            // Authenticate to get token for wallet creation
            // Wait longer after registration for database sync (especially for verification status)
            console.log('Waiting for avatar to be fully saved and verified...');
            await new Promise(resolve => setTimeout(resolve, 3000)); // Wait 3 seconds
            
            console.log('Authenticating agent for wallet creation:', username);
            const agentPassword = generateAgentPassword(username);
            
            let authResult;
            let token;
            let retries = 10; // Increase retries significantly
            
            while (retries > 0) {
                authResult = await oasisAPI.request('/api/avatar/authenticate', {
                    method: 'POST',
                    body: JSON.stringify({
                        username: username,
                        password: agentPassword
                    })
                });

                console.log(`Authentication attempt ${11 - retries}/10:`, authResult.isError ? 'Failed' : 'Success', authResult.message || authResult.result?.message || '');

                const authIsError = authResult.isError || 
                                  authResult.result?.isError || 
                                  authResult.result?.result?.isError ||
                                  false;

                if (!authIsError && authResult.result) {
                    // Try to extract token from multiple possible locations
                    token = authResult.result?.result?.JwtToken || 
                           authResult.result?.result?.jwtToken || 
                           authResult.result?.result?.token ||
                           authResult.result?.JwtToken ||
                           authResult.result?.jwtToken || 
                           authResult.result?.token ||
                           authResult.JwtToken ||
                           authResult.jwtToken ||
                           authResult.token;
                    
                    if (token) {
                        console.log('Token received successfully');
                        break;
                    } else {
                        console.warn('Authentication succeeded but no token found in response');
                    }
                } else {
                    const errorMsg = authResult.message || authResult.result?.message || 'Unknown error';
                    console.warn(`Authentication failed: ${errorMsg}`);
                }
                
                retries--;
                if (retries > 0) {
                    console.log(`Retrying authentication... (${retries} attempts left)`);
                    await new Promise(resolve => setTimeout(resolve, 2000)); // Wait 2 seconds between retries
                }
            }
            
            if (!token) {
                console.error('Failed to get token after all retries. Last auth result:', JSON.stringify(authResult, null, 2));
                throw new Error(authResult?.message || authResult?.result?.message || 'Failed to authenticate agent after registration. The agent was created but wallet creation may fail. Please try again in a moment.');
            }
            
            console.log('Token extracted successfully');
            
            // Continue with wallet creation using token
            // Create wallet for new agent
            let wallets = [];
            if (token) {
                try {
                    // Check if wallet already exists first
                    const walletsCheckResult = await oasisAPI.request(`/api/wallet/avatar/${avatarId}/wallets/false/false`, {
                        method: 'GET',
                        headers: {
                            'Authorization': `Bearer ${token}`
                        }
                    });

                    let hasSolanaWallet = false;
                    if (!walletsCheckResult.isError && walletsCheckResult.result) {
                        const walletsData = walletsCheckResult.result;
                        let existingWallets = [];
                        if (Array.isArray(walletsData)) {
                            existingWallets = walletsData;
                        } else if (typeof walletsData === 'object') {
                            Object.entries(walletsData).forEach(([key, walletList]) => {
                                if (Array.isArray(walletList)) {
                                    walletList.forEach(wallet => {
                                        if (!wallet.providerType) {
                                            wallet.providerType = key;
                                        }
                                        existingWallets.push(wallet);
                                    });
                                }
                            });
                        }
                        hasSolanaWallet = existingWallets.find(w => 
                            w.providerType === 'SolanaOASIS' || 
                            w.providerType === 'Solana' || 
                            w.providerType === 3 ||
                            w.providerType?.toString() === '3'
                        );
                    }

                    // Create wallet if it doesn't exist
                    if (!hasSolanaWallet) {
                        console.log('Creating Solana wallet for new agent:', avatarId);
                        try {
                            await createWalletForAgent(avatarId, token, username, agentPassword);
                            console.log('Wallet creation completed for agent:', avatarId);
                        } catch (walletError) {
                            console.error('Wallet creation error details:', walletError);
                            showNotification('error', `Warning: Agent created but wallet creation failed: ${walletError.message}. Check console for details.`);
                        }
                    } else {
                        console.log('Agent already has Solana wallet, skipping creation');
                    }
                    
                    // Get wallets after creation - wait and retry if needed for database sync
                    // Wait longer initially since wallet linking triggers avatar.Save() which may take time
                    await new Promise(resolve => setTimeout(resolve, 3000)); // Wait 3 seconds initially
                    
                    let walletsFetched = false;
                    let retries = 5; // Increase retries
                    
                    while (!walletsFetched && retries > 0) {
                        await new Promise(resolve => setTimeout(resolve, 3000)); // Wait 3 seconds between retries
                            
                            console.log(`Fetching wallets (${6 - retries}/5 attempts)...`);
                            
                            // Try with provider type parameter first
                            let walletsResult = await oasisAPI.request(`/api/wallet/avatar/${avatarId}/wallets/false/false?providerType=SolanaOASIS`, {
                                method: 'GET',
                                headers: {
                                    'Authorization': `Bearer ${token}`
                                }
                            });
                            
                            // If that doesn't work, try without provider type
                            if (walletsResult.isError || !walletsResult.result || Object.keys(walletsResult.result).length === 0) {
                                walletsResult = await oasisAPI.request(`/api/wallet/avatar/${avatarId}/wallets/false/false`, {
                                    method: 'GET',
                                    headers: {
                                        'Authorization': `Bearer ${token}`
                                    }
                                });
                            }

                            console.log('Raw wallets result:', JSON.stringify(walletsResult, null, 2));

                            if (!walletsResult.isError && walletsResult.result) {
                                const walletsData = walletsResult.result;
                                console.log('Wallets data structure:', typeof walletsData, Array.isArray(walletsData), walletsData);
                                console.log('Wallets data keys:', Object.keys(walletsData || {}));
                                
                                if (Array.isArray(walletsData)) {
                                    wallets = walletsData;
                                    walletsFetched = wallets.length > 0;
                                } else if (typeof walletsData === 'object') {
                                    // Handle nested structure like { SolanaOASIS: [...], EthereumOASIS: [...] }
                                    // Also check for numeric keys or different provider type names
                                    // ProviderType enum values: SolanaOASIS = 3
                                    Object.entries(walletsData).forEach(([key, walletList]) => {
                                        console.log(`Processing wallet key: ${key}, type: ${typeof key}, isArray: ${Array.isArray(walletList)}`);
                                        if (Array.isArray(walletList)) {
                                            console.log(`Found ${walletList.length} wallets for key ${key}`);
                                            walletList.forEach(wallet => {
                                                // Ensure providerType is set correctly
                                                // If key is "Default" or numeric, check wallet's providerType property
                                                if (key === 'Default' || key === '3' || key === 3) {
                                                    // Check if wallet has providerType property
                                                    if (wallet.providerType) {
                                                        // Use wallet's providerType
                                                        console.log(`Wallet has providerType: ${wallet.providerType}`);
                                                    } else {
                                                        // Default to SolanaOASIS if key is Default or 3
                                                        wallet.providerType = 'SolanaOASIS';
                                                    }
                                                } else {
                                                    wallet.providerType = key;
                                                }
                                                wallets.push(wallet);
                                            });
                                        }
                                    });
                                    walletsFetched = wallets.length > 0;
                                }
                            }
                            
                            console.log(`Total wallets found: ${wallets.length}`);
                            
                            if (!walletsFetched) {
                                retries--;
                                if (retries > 0) {
                                    console.log(`No wallets found, retrying in 3 seconds... (${retries} attempts left)`);
                                }
                            }
                        }
                    
                    console.log('Processed wallets array:', wallets);
                    console.log('Solana wallet found:', wallets.find(w => 
                        w.providerType === 'SolanaOASIS' || 
                        w.providerType === 'Solana' || 
                        w.providerType === 3 ||
                        w.providerType?.toString() === '3'
                    ));
                    
                    if (wallets.length === 0) {
                        console.warn('No wallets found after creation. Wallet may still be syncing to database.');
                    }
                } catch (error) {
                    console.error('Error creating wallet for new agent:', error);
                    showNotification('error', `Warning: Agent created but wallet creation failed: ${error.message}. Check console for details.`);
                }
            } else {
                console.warn('No token available for wallet creation');
            }

            // Add to agents list (linked to current user)
            const currentAvatarId = getCurrentAvatarId();
            const agent = {
                avatarId: avatarId,
                username: username,
                email: email,
                password: agentPassword,
                wallets: wallets,
                parentAvatarId: currentAvatarId, // Link to logged-in user
                createdAt: new Date().toISOString(),
                lastUsed: new Date().toISOString(),
                isActive: true
            };

            console.log('Agent object being saved:', JSON.stringify(agent, null, 2));

            agentManagementState.agents.push(agent);
            saveAgentsToStorage();
            closeCreateAgentModal();
            
            // Re-render
            const container = document.getElementById('tab-agents');
            if (container) {
                container.innerHTML = renderAgentManagement();
                attachAgentManagementListeners();
            }

            showNotification('success', 'Agent created successfully!');
            
            // Refresh agent data after a short delay to get latest wallet information
            setTimeout(async () => {
                try {
                    await refreshAgentData(avatarId);
                    // Re-render again with updated wallet data
                    const container2 = document.getElementById('tab-agents');
                    if (container2) {
                        container2.innerHTML = renderAgentManagement();
                        attachAgentManagementListeners();
                    }
                } catch (error) {
                    console.error('Error refreshing agent data:', error);
                }
            }, 3000);
            return; // Exit early since we've handled the successful registration
        } else if (isError && !hasAvatarId) {
            // Try to authenticate if agent already exists
            const authResult = await oasisAPI.request('/api/avatar/authenticate', {
                method: 'POST',
                body: JSON.stringify({
                    username: username,
                    password: generateAgentPassword(username)
                })
            });

            if (authResult.isError) {
                console.error('Authentication failed:', authResult);
                
                // Check if it's a verification error
                const needsVerification = authResult.message?.toLowerCase().includes('verified') || 
                                        authResult.message?.toLowerCase().includes('verification') ||
                                        authResult.result?.message?.toLowerCase().includes('verified');
                
                if (needsVerification) {
                    // Try to load the avatar and verify it if possible
                    // For now, throw a clearer error message
                    throw new Error(`Agent exists but needs email verification. Please use a different username or contact support. Original error: ${authResult.message || 'Avatar has not been verified'}`);
                }
                
                throw new Error(authResult.message || 'Failed to create or authenticate agent');
            }

            // Use authenticated agent - check multiple possible response structures
            console.log('Authentication result:', JSON.stringify(authResult, null, 2));
            const avatarId = authResult.result?.result?.avatarId || 
                           authResult.result?.result?.id ||
                           authResult.result?.avatarId || 
                           authResult.result?.id ||
                           authResult.avatarId ||
                           authResult.id ||
                           authResult.result?.avatar?.avatarId ||
                           authResult.result?.avatar?.id;
            const token = authResult.result?.result?.jwtToken || authResult.result?.jwtToken || authResult.jwtToken;

            if (!avatarId) {
                console.error('Failed to extract avatarId from auth result. Full response:', authResult);
                throw new Error('No avatar ID received from authentication. Check console for details.');
            }
            
            console.log('Extracted avatarId:', avatarId);

                // Get wallets - endpoint requires query parameters: /api/wallet/avatar/{id}/wallets/{showOnlyDefault}/{decryptPrivateKeys}
                let wallets = [];
                if (token) {
                    const walletsResult = await oasisAPI.request(`/api/wallet/avatar/${avatarId}/wallets/false/false`, {
                        method: 'GET',
                        headers: {
                            'Authorization': `Bearer ${token}`
                        }
                    });

                if (!walletsResult.isError && walletsResult.result) {
                    const walletsData = walletsResult.result;
                    if (Array.isArray(walletsData)) {
                        wallets = walletsData;
                    } else if (typeof walletsData === 'object') {
                        Object.values(walletsData).forEach(walletList => {
                            if (Array.isArray(walletList)) {
                                wallets.push(...walletList);
                            }
                        });
                    }
                }

                // Always create wallet for existing agents if it doesn't exist (standard behavior)
                const hasSolanaWallet = wallets.find(w => 
                    w.providerType === 'SolanaOASIS' || 
                    w.providerType === 'Solana' || 
                    w.providerType === 3
                );
                
                if (!hasSolanaWallet && token) {
                    try {
                        console.log('Creating Solana wallet for existing agent:', avatarId);
                        await createWalletForAgent(avatarId, token, username, generateAgentPassword(username));
                        // Refresh wallets after creation - endpoint requires query parameters
                        const walletsResult2 = await oasisAPI.request(`/api/wallet/avatar/${avatarId}/wallets/false/false`, {
                            method: 'GET',
                            headers: {
                                'Authorization': `Bearer ${token}`
                            }
                        });
                        if (!walletsResult2.isError && walletsResult2.result) {
                            const walletsData2 = walletsResult2.result;
                            if (Array.isArray(walletsData2)) {
                                wallets = walletsData2;
                            } else if (typeof walletsData2 === 'object') {
                                wallets = [];
                                Object.values(walletsData2).forEach(walletList => {
                                    if (Array.isArray(walletList)) {
                                        wallets.push(...walletList);
                                    }
                                });
                            }
                        }
                    } catch (error) {
                        console.error('Error creating wallet during authentication:', error);
                        showNotification('error', `Warning: Agent found but wallet creation failed: ${error.message}`);
                        // Continue with agent creation even if wallet creation fails
                    }
                }
            }

            // Add to agents list (linked to current user)
            const currentAvatarId = getCurrentAvatarId();
            const agent = {
                avatarId: avatarId,
                username: username,
                email: email,
                password: generateAgentPassword(username), // Store for future use
                wallets: wallets,
                parentAvatarId: currentAvatarId, // Link to logged-in user
                createdAt: new Date().toISOString(),
                lastUsed: new Date().toISOString(),
                isActive: true
            };

            agentManagementState.agents.push(agent);
            saveAgentsToStorage();
            closeCreateAgentModal();
            
            // Re-render
            const container = document.getElementById('tab-agents');
            if (container) {
                container.innerHTML = renderAgentManagement();
                attachAgentManagementListeners();
            }

            showNotification('success', 'Agent authenticated successfully!');
        } else {
            // New agent created - extract avatarId from nested response structure
            // Response structure: { result: { result: { avatarId: "...", ... } } }
            console.log('Registration result:', JSON.stringify(registerResult, null, 2));
            
            const avatarId = registerResult.result?.result?.avatarId || 
                           registerResult.result?.result?.id ||
                           registerResult.result?.avatarId || 
                           registerResult.result?.id ||
                           registerResult.avatarId ||
                           registerResult.id;
            
            if (!avatarId) {
                console.error('Failed to extract avatarId. Full response:', registerResult);
                throw new Error('No avatar ID received from registration. Response: ' + JSON.stringify(registerResult).substring(0, 200));
            }

            // Authenticate to get token
            // Wait longer after registration to ensure the avatar and password are fully saved
            console.log('Waiting for avatar to be fully saved...');
            await new Promise(resolve => setTimeout(resolve, 2000));
            
            console.log('Authenticating agent for wallet creation:', username);
            const agentPassword = generateAgentPassword(username);
            console.log('Using password:', agentPassword.substring(0, 10) + '...');
            
            let authResult;
            let token;
            let retries = 5; // Increase retries
            
            while (retries > 0) {
                authResult = await oasisAPI.request('/api/avatar/authenticate', {
                    method: 'POST',
                    body: JSON.stringify({
                        username: username,
                        password: agentPassword
                    })
                });

                console.log(`Authentication attempt ${6 - retries} result:`, JSON.stringify(authResult, null, 2));

                // Check if authentication succeeded
                const authIsError = authResult.isError || 
                                  authResult.result?.isError || 
                                  authResult.result?.result?.isError ||
                                  false;

                if (!authIsError && authResult.result) {
                    // Try to extract token from multiple possible locations (check both JwtToken and jwtToken)
                    token = authResult.result?.result?.JwtToken ||
                           authResult.result?.result?.jwtToken || 
                           authResult.result?.result?.token ||
                           authResult.result?.JwtToken ||
                           authResult.result?.jwtToken || 
                           authResult.result?.token ||
                           authResult.JwtToken ||
                           authResult.jwtToken ||
                           authResult.token;
                    
                    if (token) {
                        console.log('Token received successfully');
                        break;
                    } else {
                        console.warn('Authentication succeeded but no token found in response');
                    }
                } else {
                    console.warn('Authentication failed:', authResult.message || authResult.result?.message);
                }
                
                retries--;
                if (retries > 0) {
                    console.log(`Retrying authentication... (${retries} attempts left)`);
                    await new Promise(resolve => setTimeout(resolve, 2000)); // Wait 2 seconds between retries
                }
            }
            
            if (!token) {
                console.error('Failed to get token after all retries. Last auth result:', JSON.stringify(authResult, null, 2));
                throw new Error(authResult?.message || authResult?.result?.message || 'Failed to authenticate agent after registration. The agent was created but wallet creation may fail. Please try creating the wallet manually.');
            }
            
            console.log('Agent authenticated successfully, token received');

            // Always create wallet for new agents (standard behavior)
            let wallets = [];
            if (token) {
                try {
                    // Check if wallet already exists first
                    const walletsCheckResult = await oasisAPI.request(`/api/wallet/avatar/${avatarId}/wallets/false/false`, {
                        method: 'GET',
                        headers: {
                            'Authorization': `Bearer ${token}`
                        }
                    });

                    let hasSolanaWallet = false;
                    if (!walletsCheckResult.isError && walletsCheckResult.result) {
                        const walletsData = walletsCheckResult.result;
                        let existingWallets = [];
                        if (Array.isArray(walletsData)) {
                            existingWallets = walletsData;
                        } else if (typeof walletsData === 'object') {
                            Object.values(walletsData).forEach(walletList => {
                                if (Array.isArray(walletList)) {
                                    existingWallets.push(...walletList);
                                }
                            });
                        }
                        hasSolanaWallet = existingWallets.find(w => 
                            w.providerType === 'SolanaOASIS' || 
                            w.providerType === 'Solana' || 
                            w.providerType === 3 ||
                            w.providerType === 'SolanaOASIS' ||
                            w.providerType === 3
                        );
                    }

                    // Create wallet if it doesn't exist
                    if (!hasSolanaWallet) {
                        console.log('Creating Solana wallet for new agent:', avatarId);
                        try {
                            await createWalletForAgent(avatarId, token, username, generateAgentPassword(username));
                            console.log('Wallet creation completed for agent:', avatarId);
                        } catch (walletError) {
                            console.error('Wallet creation error details:', walletError);
                            throw walletError; // Re-throw to be caught by outer try-catch
                        }
                    } else {
                        console.log('Agent already has Solana wallet, skipping creation');
                    }
                    
                    // Get wallets after creation - endpoint requires query parameters
                    const walletsResult = await oasisAPI.request(`/api/wallet/avatar/${avatarId}/wallets/false/false`, {
                        method: 'GET',
                        headers: {
                            'Authorization': `Bearer ${token}`
                        }
                    });

                    if (!walletsResult.isError && walletsResult.result) {
                        const walletsData = walletsResult.result;
                        if (Array.isArray(walletsData)) {
                            wallets = walletsData;
                        } else if (typeof walletsData === 'object') {
                            Object.values(walletsData).forEach(walletList => {
                                if (Array.isArray(walletList)) {
                                    wallets.push(...walletList);
                                }
                            });
                        }
                    }
                } catch (error) {
                    console.error('Error creating wallet for new agent:', error);
                    console.error('Error stack:', error.stack);
                    showNotification('error', `Warning: Agent created but wallet creation failed: ${error.message}. Check console for details.`);
                    // Continue anyway - agent is still created, but wallet will need to be created manually
                }
            } else {
                console.warn('No token available for wallet creation');
            }

            // Add to agents list (linked to current user)
            const currentAvatarId = getCurrentAvatarId();
            const agent = {
                avatarId: avatarId,
                username: username,
                email: email,
                password: generateAgentPassword(username),
                wallets: wallets,
                parentAvatarId: currentAvatarId, // Link to logged-in user
                createdAt: new Date().toISOString(),
                lastUsed: new Date().toISOString(),
                isActive: true
            };

            agentManagementState.agents.push(agent);
            saveAgentsToStorage();
            closeCreateAgentModal();
            
            // Re-render
            const container = document.getElementById('tab-agents');
            if (container) {
                container.innerHTML = renderAgentManagement();
                attachAgentManagementListeners();
            }

            showNotification('success', 'Agent created successfully!');
        }
    } catch (error) {
        console.error('Error creating agent:', error);
        showNotification('error', `Error: ${error.message}`);
    }
}

// Create wallet for agent
async function createWalletForAgent(avatarId, token = null, agentUsername = null, agentPassword = null) {
    try {
        // Authenticate if no token provided
        if (!token) {
            // Try to find agent in state first
            const agent = agentManagementState.agents.find(a => a.avatarId === avatarId);
            
            // Use provided credentials or agent's stored credentials
            let username = agentUsername || agent?.username;
            let password = agentPassword || agent?.password;
            
            // If password is not stored, generate it deterministically
            if (username && !password) {
                password = generateAgentPassword(username);
                console.log('Generated password for agent:', username);
            }
            
            if (!username || !password) {
                console.error('Missing credentials:', { username, hasPassword: !!password, agent });
                throw new Error('Agent credentials not found. Please provide username and password.');
            }

            console.log('Authenticating agent for wallet creation:', username);
            const authResult = await oasisAPI.request('/api/avatar/authenticate', {
                method: 'POST',
                body: JSON.stringify({
                    username: username,
                    password: password
                })
            });

            if (authResult.isError || !authResult.result) {
                console.error('Authentication failed:', authResult);
                throw new Error(authResult.message || 'Failed to authenticate agent. Please check the browser console for details.');
            }

            token = authResult.result?.result?.jwtToken || authResult.result?.jwtToken || authResult.jwtToken;
            if (!token) {
                console.error('No token received:', authResult);
                throw new Error('Failed to get authentication token');
            }
            
            console.log('Agent authenticated successfully');
        }

        // Generate keypair
        console.log('Generating Solana keypair for agent:', avatarId);
        const keypairResult = await oasisAPI.request('/api/keys/generate_keypair_for_provider/SolanaOASIS', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (keypairResult.isError) {
            console.error('Keypair generation failed:', keypairResult);
            throw new Error(keypairResult.message || 'Failed to generate keypair');
        }

        const keypair = keypairResult.result?.result || keypairResult.result || keypairResult;
        const privateKey = keypair.privateKey;
        const publicKey = keypair.publicKey || keypair.walletAddress;

        if (!privateKey || !publicKey) {
            console.error('Invalid keypair response:', keypair);
            throw new Error('Invalid keypair response - missing privateKey or publicKey');
        }
        
        console.log('Keypair generated successfully. Public key:', publicKey?.substring(0, 20) + '...');

        // Link private key
        console.log('Linking private key to avatar:', avatarId);
        const linkPrivateResult = await oasisAPI.request('/api/keys/link_provider_private_key_to_avatar_by_id', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                AvatarID: avatarId,
                ProviderType: 'SolanaOASIS',
                ProviderKey: privateKey
            })
        });

        if (linkPrivateResult.isError) {
            console.error('Failed to link private key:', linkPrivateResult);
            throw new Error(linkPrivateResult.message || 'Failed to link private key');
        }

        const walletId = linkPrivateResult.result?.result?.walletId || linkPrivateResult.result?.walletId;
        console.log('Private key linked. Wallet ID:', walletId);

        // Link public key
        console.log('Linking public key to avatar:', avatarId);
        const linkPublicResult = await oasisAPI.request('/api/keys/link_provider_public_key_to_avatar_by_id', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                AvatarID: avatarId,
                ProviderType: 'SolanaOASIS',
                ProviderKey: publicKey,
                WalletId: walletId
            })
        });

        if (linkPublicResult.isError) {
            console.warn('Warning: Failed to link public key:', linkPublicResult);
            // Continue anyway - wallet might still work
        } else {
            console.log('Public key linked successfully');
            console.log('Public key linked successfully');
        }

        // Refresh agent data to get updated wallets
        await refreshAgentData(avatarId);
        
        // Update the selected agent if modal is open
        if (agentManagementState.selectedAgent && agentManagementState.selectedAgent.avatarId === avatarId) {
            agentManagementState.selectedAgent = agentManagementState.agents.find(a => a.avatarId === avatarId);
        }
        
        // Re-render the UI
        const container = document.getElementById('tab-agents');
        if (container) {
            container.innerHTML = renderAgentManagement();
            attachAgentManagementListeners();
        }
        
        showNotification('success', 'Solana wallet created successfully!');
        return true;
    } catch (error) {
        console.error('Error creating wallet:', error);
        showNotification('error', `Error creating wallet: ${error.message}`);
        throw error;
    }
}

// Refresh agent data from API
async function refreshAgentData(avatarId = null) {
    const agentsToRefresh = avatarId 
        ? agentManagementState.agents.filter(a => a.avatarId === avatarId)
        : agentManagementState.agents;

    for (const agent of agentsToRefresh) {
        try {
            // Authenticate
            const authResult = await oasisAPI.request('/api/avatar/authenticate', {
                method: 'POST',
                body: JSON.stringify({
                    username: agent.username,
                    password: agent.password
                })
            });

            if (authResult.isError) {
                agent.isActive = false;
                continue;
            }

            const token = authResult.result?.result?.jwtToken || authResult.result?.jwtToken || authResult.jwtToken;
            if (!token) continue;

            // Get wallets - endpoint requires query parameters: /api/wallet/avatar/{id}/wallets/{showOnlyDefault}/{decryptPrivateKeys}
            const walletsResult = await oasisAPI.request(`/api/wallet/avatar/${agent.avatarId}/wallets/false/false`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (!walletsResult.isError && walletsResult.result) {
                const walletsData = walletsResult.result;
                let wallets = [];
                
                if (Array.isArray(walletsData)) {
                    wallets = walletsData;
                } else if (typeof walletsData === 'object') {
                    // Handle nested structure like { SolanaOASIS: [...], EthereumOASIS: [...] }
                    Object.entries(walletsData).forEach(([key, walletList]) => {
                        if (Array.isArray(walletList)) {
                            walletList.forEach(wallet => {
                                // Ensure providerType is set correctly
                                if (!wallet.providerType) {
                                    wallet.providerType = key;
                                }
                                wallets.push(wallet);
                            });
                        }
                    });
                }

                agent.wallets = wallets;
                agent.lastUsed = new Date().toISOString();
                agent.isActive = true;
                
                console.log(`Refreshed wallets for agent ${agent.username}:`, wallets.length, 'wallets found');
            }
        } catch (error) {
            console.error(`Error refreshing agent ${agent.avatarId}:`, error);
            agent.isActive = false;
        }
    }

    saveAgentsToStorage();
}

// Delete agent permanently from OASIS
async function deleteAgentPermanently(avatarId, agentName) {
    try {
        // Find agent to get credentials
        const agent = agentManagementState.agents.find(a => a.avatarId === avatarId);
        if (!agent) {
            throw new Error('Agent not found in local storage');
        }

        // Authenticate as the agent to delete it
        console.log('Authenticating as agent to delete:', agent.username);
        const agentPassword = agent.password || generateAgentPassword(agent.username);
        const authResult = await oasisAPI.request('/api/avatar/authenticate', {
            method: 'POST',
            body: JSON.stringify({
                username: agent.username,
                password: agentPassword
            })
        });

        if (authResult.isError || !authResult.result) {
            throw new Error(authResult.message || 'Failed to authenticate agent for deletion');
        }

        const token = authResult.result?.result?.jwtToken || authResult.result?.jwtToken || authResult.jwtToken;
        if (!token) {
            throw new Error('Failed to get authentication token for deletion');
        }

        // Delete the avatar from OASIS (hard delete - permanent, removes wallets from MongoDB)
        console.log('Deleting agent avatar from OASIS (hard delete):', avatarId);
        const deleteResult = await oasisAPI.request(`/api/avatar/${avatarId}?softDelete=false`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (deleteResult.isError) {
            // If deletion fails, try deleting by username as fallback
            console.log('Delete by ID failed, trying delete by username:', agent.username);
            const deleteByUsernameResult = await oasisAPI.request(`/api/avatar/delete-by-username/${encodeURIComponent(agent.username)}?softDelete=false`, {
                method: 'DELETE',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (deleteByUsernameResult.isError) {
                console.error('Delete by username also failed:', deleteByUsernameResult);
                throw new Error(deleteByUsernameResult.message || 'Failed to delete agent from OASIS');
            }
        }

        console.log('Agent deleted successfully from OASIS');
        
        // Wait a moment for database sync before removing from local storage
        await new Promise(resolve => setTimeout(resolve, 1000));
        
        // Verify deletion by trying to load the avatar (should fail)
        try {
            const verifyResult = await oasisAPI.request(`/api/avatar/get-by-username/${encodeURIComponent(agent.username)}`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });
            
            if (!verifyResult.isError && verifyResult.result) {
                console.warn('Warning: Avatar still exists after deletion. This may be a database sync delay.');
            } else {
                console.log('Deletion verified: Avatar no longer exists');
            }
        } catch (error) {
            console.log('Deletion verified: Avatar no longer accessible');
        }

        // Remove from local storage
        deleteAgent(avatarId);
        
        showNotification('success', `Agent "${agentName}" permanently deleted from OASIS.`);
    } catch (error) {
        console.error('Error deleting agent permanently:', error);
        showNotification('error', `Failed to delete agent: ${error.message}. The agent may still exist in OASIS.`);
        
        // Still remove from local storage even if OASIS deletion failed
        deleteAgent(avatarId);
    }
}

// Handle delete agent (with confirmation)
function handleDeleteAgent(avatarId, agentName) {
    showConfirmDialog(
        `Are you sure you want to permanently delete agent "${agentName}"?<br><br><strong style="color: var(--error-color);">⚠️ WARNING:</strong> This will permanently delete the agent avatar from OASIS. This action cannot be undone.<br><br>This will:<br>• Delete the avatar from OASIS database<br>• Remove all associated wallets and data<br>• Remove it from your dashboard`,
        () => {
            deleteAgentPermanently(avatarId, agentName);
        },
        () => {
            // Cancelled - do nothing
        }
    );
}

// Delete agent from local storage only
function deleteAgent(avatarId) {
    // Remove from current state
    agentManagementState.agents = agentManagementState.agents.filter(a => a.avatarId !== avatarId);
    
    // Add to deleted list to prevent re-adding
    const deletedAgents = JSON.parse(localStorage.getItem('oasis_deleted_agents') || '[]');
    if (!deletedAgents.includes(avatarId)) {
        deletedAgents.push(avatarId);
        localStorage.setItem('oasis_deleted_agents', JSON.stringify(deletedAgents));
    }
    
    // Remove from main agents list in localStorage
    const stored = localStorage.getItem('oasis_agents');
    if (stored) {
        try {
            const allAgents = JSON.parse(stored);
            const filteredAgents = allAgents.filter(a => a.avatarId !== avatarId);
            localStorage.setItem('oasis_agents', JSON.stringify(filteredAgents));
        } catch (error) {
            console.error('Error removing agent from storage:', error);
        }
    }
    
    // Close modal if it's open for this agent
    if (agentManagementState.selectedAgent && agentManagementState.selectedAgent.avatarId === avatarId) {
        closeAgentDetailModal();
    }
    
    // Re-render
    const container = document.getElementById('tab-agents');
    if (container) {
        container.innerHTML = renderAgentManagement();
        attachAgentManagementListeners();
    }
}

// Handle create wallet button click
async function handleCreateWalletForAgent(avatarId) {
    const button = event.target;
    const originalText = button.textContent;
    
    try {
        button.disabled = true;
        button.textContent = 'Creating...';
        await createWalletForAgent(avatarId);
        // The createWalletForAgent function already re-renders and shows alert
    } catch (error) {
        console.error('Error creating wallet:', error);
        button.disabled = false;
        button.textContent = originalText;
    }
}

// Copy to clipboard
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => {
        showNotification('success', 'Copied to clipboard!', 2000);
    }).catch(err => {
        console.error('Failed to copy:', err);
        showNotification('error', 'Failed to copy to clipboard');
    });
}

// Format time ago
function formatTimeAgo(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
}

// Make functions globally accessible
window.showAgentDetails = showAgentDetails;
window.closeAgentDetailModal = closeAgentDetailModal;
window.showCreateAgentModal = showCreateAgentModal;
window.closeCreateAgentModal = closeCreateAgentModal;
window.handleCreateAgent = handleCreateAgent;
window.handleCreateWalletForAgent = handleCreateWalletForAgent;
window.refreshAgentData = refreshAgentData;
window.deleteAgent = deleteAgent;
window.deleteAgentPermanently = deleteAgentPermanently;
window.handleDeleteAgent = handleDeleteAgent;
window.copyToClipboard = copyToClipboard;
window.showNotification = showNotification;
window.showConfirmDialog = showConfirmDialog;

// Initialize on page load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        loadAgentsFromStorage();
    });
} else {
    loadAgentsFromStorage();
}

