/**
 * AI NFT Assistant - Natural Language NFT Creation
 * Integrates with OpenAI to parse user requests and create NFTs
 */

// AI NFT Assistant State
const aiNFTAssistantState = {
    isActive: false,
    userInput: '',
    parsedData: null,
    isLoading: false,
    error: null,
    apiKey: null, // Will be set from environment or config
    uploadedFiles: [], // Array of {file: File, preview: string, url: string, uploading: boolean}
    uploadProvider: 'PinataOASIS' // Default upload provider
};

/**
 * Initialize AI NFT Assistant
 */
function initAINFTAssistant() {
    // Try to get API key from environment or config
    // In a real implementation, this would come from a secure backend endpoint
    // For now, we'll need to configure it
    aiNFTAssistantState.apiKey = getOpenAIAPIKey();
}

/**
 * Get OpenAI API Key (placeholder - implement secure retrieval)
 */
function getOpenAIAPIKey() {
    // Option 1: From environment variable (if running server-side)
    // Option 2: From secure backend endpoint
    // Option 3: From user settings/configuration
    // For now, return null - will need to be configured
    return null;
}

/**
 * Render AI NFT Assistant UI
 */
function renderAINFTAssistant() {
    return `
        <div class="portal-section">
            <div class="portal-section-header">
                <div>
                    <h2 class="portal-section-title">AI NFT Assistant</h2>
                    <p class="portal-section-subtitle">Describe what you'd like to create in plain English</p>
                </div>
            </div>
        </div>

        <div class="portal-section">
            <div class="portal-card" style="padding: 1rem;">
                <div style="display: flex; gap: 0.5rem; align-items: center;">
                    <span style="
                        font-size: 0.75rem;
                        color: var(--text-tertiary);
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                        margin-right: 0.5rem;
                    ">Mode:</span>
                    <button
                        type="button"
                        id="ai-mode-toggle-form"
                        class="mode-toggle-btn"
                        data-mode="form"
                        style="
                            padding: 0.5rem 1rem;
                            background: transparent;
                            border: 1px solid var(--border-color);
                            border-radius: 0.375rem;
                            color: var(--text-secondary);
                            font-size: 0.875rem;
                            cursor: pointer;
                            transition: all 0.2s;
                        "
                        onmouseover="this.style.background='rgba(255, 255, 255, 0.05)'; this.style.color='var(--text-primary)';"
                        onmouseout="this.style.background='transparent'; this.style.color='var(--text-secondary)';"
                    >
                        Form Mode
                    </button>
                    <button
                        type="button"
                        id="ai-mode-toggle-ai"
                        class="mode-toggle-btn"
                        data-mode="ai"
                        style="
                            padding: 0.5rem 1rem;
                            background: rgba(255, 255, 255, 0.1);
                            border: 1px solid var(--border-color);
                            border-radius: 0.375rem;
                            color: var(--text-primary);
                            font-size: 0.875rem;
                            cursor: pointer;
                            transition: all 0.2s;
                        "
                    >
                        AI Mode
                    </button>
                </div>
            </div>
        </div>

        <div class="portal-section">
            <div class="portal-card">
                <!-- Chat Interface -->
                <div style="display: flex; flex-direction: column; gap: 1.5rem;">
                    <!-- Messages Area -->
                    <div id="ai-messages-container" style="
                        min-height: 300px;
                        max-height: 500px;
                        padding: 1.5rem;
                        background: rgba(0, 0, 0, 0.4);
                        border: 1px solid var(--border-color);
                        border-radius: 0.5rem;
                        overflow-y: auto;
                        display: flex;
                        flex-direction: column;
                        gap: 1rem;
                    ">
                        ${renderAIWelcomeMessage()}
                        ${aiNFTAssistantState.userInput ? renderUserMessage(aiNFTAssistantState.userInput) : ''}
                        ${aiNFTAssistantState.isLoading ? renderAILoadingMessage() : ''}
                        ${aiNFTAssistantState.parsedData ? renderParsedPreview(aiNFTAssistantState.parsedData) : ''}
                        ${aiNFTAssistantState.error ? renderErrorMessage(aiNFTAssistantState.error) : ''}
                    </div>

                    <!-- Input Area -->
                    <div style="display: flex; flex-direction: column; gap: 0.75rem;">
                        <div id="file-upload-section-container">
                            ${renderFileUploadSection()}
                        </div>
                        <textarea
                            id="ai-input-field"
                            placeholder="Try: &quot;Create an NFT called 'My Artwork' with description 'A beautiful piece' priced at 1 SOL&quot;"
                            style="
                                width: 100%;
                                min-height: 100px;
                                padding: 1rem;
                                background: rgba(255, 255, 255, 0.05);
                                border: 1px solid var(--border-color);
                                border-radius: 0.5rem;
                                color: var(--text-primary);
                                font-family: inherit;
                                font-size: 0.875rem;
                                resize: vertical;
                            "
                        ></textarea>
                        
                        <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.75rem;">
                            <div style="display: flex; gap: 0.5rem; flex-wrap: wrap;">
                                <button
                                    type="button"
                                    onclick="insertExamplePrompt('nft')"
                                    class="btn-text"
                                    style="font-size: 0.75rem;"
                                >
                                    NFT Example
                                </button>
                                <button
                                    type="button"
                                    onclick="insertExamplePrompt('geonft')"
                                    class="btn-text"
                                    style="font-size: 0.75rem;"
                                >
                                    GeoNFT Example
                                </button>
                                <button
                                    type="button"
                                    onclick="insertExamplePrompt('quest')"
                                    class="btn-text"
                                    style="font-size: 0.75rem;"
                                >
                                    Quest Example
                                </button>
                                <button
                                    type="button"
                                    onclick="insertExamplePrompt('mission')"
                                    class="btn-text"
                                    style="font-size: 0.75rem;"
                                >
                                    Mission Example
                                </button>
                            </div>
                            
                            <div style="display: flex; gap: 0.5rem;">
                                <button
                                    type="button"
                                    onclick="clearAIChat()"
                                    class="btn-text"
                                >
                                    Clear
                                </button>
                                <button
                                    type="button"
                                    onclick="processAIRequest()"
                                    id="ai-send-btn"
                                    class="btn-primary"
                                    ${aiNFTAssistantState.isLoading ? 'disabled' : ''}
                                    style="opacity: ${aiNFTAssistantState.isLoading ? 0.5 : 1}; cursor: ${aiNFTAssistantState.isLoading ? 'not-allowed' : 'pointer'};"
                                >
                                    ${aiNFTAssistantState.isLoading ? 'Processing...' : 'Send'}
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        ${aiNFTAssistantState.parsedData ? renderCreateSection() : ''}
    `;
}

/**
 * Render welcome message
 */
function renderAIWelcomeMessage() {
    return `
        <div style="
            padding: 1rem;
            background: rgba(255, 255, 255, 0.03);
            border: 1px solid var(--border-color);
            border-radius: 0.5rem;
        ">
            <div style="
                font-size: 0.875rem;
                color: var(--text-primary);
                margin-bottom: 0.75rem;
                font-weight: 500;
            ">Welcome! I can help you create NFTs using natural language.</div>
                    <div style="
                        font-size: 0.8125rem;
                        color: var(--text-secondary);
                        line-height: 1.6;
                    ">
                        Just describe what you want to create. For example:
                        <ul style="margin-top: 0.5rem; padding-left: 1.25rem; list-style: disc; color: var(--text-tertiary);">
                            <li style="margin-bottom: 0.25rem;">"Create an NFT called 'Sunset Dream' with description 'Beautiful artwork' priced at 0.5 SOL"</li>
                            <li style="margin-bottom: 0.25rem;">"Create a GeoNFT at Big Ben in London"</li>
                            <li style="margin-bottom: 0.25rem;">"Create a quest called 'Find the Treasure' with 100 XP reward"</li>
                            <li style="margin-bottom: 0.25rem;">"Create a mission called 'Save the World'"</li>
                        </ul>
                    </div>
        </div>
    `;
}

/**
 * Render user message
 */
function renderUserMessage(message) {
    return `
        <div style="display: flex; justify-content: flex-end;">
            <div style="
                max-width: 75%;
                padding: 0.75rem 1rem;
                background: rgba(255, 255, 255, 0.1);
                border: 1px solid var(--border-color);
                border-radius: 0.5rem 0.5rem 0.125rem 0.5rem;
                color: var(--text-primary);
                font-size: 0.875rem;
                line-height: 1.5;
            ">
                ${escapeHtml(message)}
            </div>
        </div>
    `;
}

/**
 * Render AI loading message
 */
function renderAILoadingMessage() {
    return `
        <div style="
            display: flex;
            align-items: flex-start;
            gap: 0.75rem;
            padding: 0.75rem 1rem;
            background: rgba(255, 255, 255, 0.03);
            border: 1px solid var(--border-color);
            border-radius: 0.5rem;
        ">
            <div style="
                width: 16px;
                height: 16px;
                border: 2px solid rgba(255, 255, 255, 0.1);
                border-top-color: var(--text-primary);
                border-radius: 50%;
                animation: spin 0.8s linear infinite;
                flex-shrink: 0;
                margin-top: 2px;
            "></div>
            <div style="
                color: var(--text-secondary);
                font-size: 0.875rem;
            ">
                Parsing your request...
            </div>
        </div>
    `;
}

/**
 * Render parsed preview
 */
function renderParsedPreview(parsedData) {
    if (!parsedData || parsedData.intent === 'error') {
        return '';
    }

    // Check if validation failed - show error message as a friendly question
    if (!parsedData.isValid && parsedData.errorMessage) {
        return `
            <div style="
                padding: 1rem;
                background: rgba(251, 191, 36, 0.1);
                border: 1px solid rgba(251, 191, 36, 0.3);
                border-radius: 0.5rem;
            ">
                <div style="
                    font-size: 0.875rem;
                    color: var(--text-primary);
                    margin-bottom: 0.5rem;
                    font-weight: 500;
                ">I need a bit more information:</div>
                <div style="
                    font-size: 0.875rem;
                    color: var(--text-secondary);
                    line-height: 1.5;
                ">${escapeHtml(parsedData.errorMessage)}</div>
            </div>
        `;
    }

    let preview = '';
    const intent = parsedData.intent?.toLowerCase();
    
    // Validate required fields before showing preview
    if (intent === 'create_nft') {
        if (!validateNFTParams(parsedData.parameters)) {
            return renderMissingFieldsMessage('create_nft', parsedData.parameters);
        }
        preview = renderNFTPreview(parsedData.parameters);
    } else if (intent === 'create_geonft') {
        if (!validateGeoNFTParams(parsedData.parameters)) {
            return renderMissingFieldsMessage('create_geonft', parsedData.parameters);
        }
        preview = renderGeoNFTPreview(parsedData.parameters);
    } else if (intent === 'place_geonft') {
        preview = renderGeoNFTPreview(parsedData.parameters);
    } else if (intent === 'create_quest') {
        if (!validateQuestParams(parsedData.parameters)) {
            return renderMissingFieldsMessage('create_quest', parsedData.parameters);
        }
        preview = renderQuestPreview(parsedData.parameters);
    } else if (intent === 'create_mission') {
        if (!validateMissionParams(parsedData.parameters)) {
            return renderMissingFieldsMessage('create_mission', parsedData.parameters);
        }
        preview = renderMissionPreview(parsedData.parameters);
    } else {
        preview = renderGenericPreview(parsedData);
    }

    if (!preview) {
        return '';
    }

    return `
        <div style="
            padding: 1rem;
            background: rgba(34, 197, 94, 0.05);
            border: 1px solid rgba(34, 197, 94, 0.2);
            border-radius: 0.5rem;
        ">
            <div style="
                font-size: 0.875rem;
                color: var(--text-primary);
                margin-bottom: 1rem;
                font-weight: 500;
            ">I understood! Here's what I'll create:</div>
            ${preview}
        </div>
    `;
}

/**
 * Validate NFT parameters
 */
function validateNFTParams(params) {
    if (!params) return false;
    return !!(params.title && params.description);
}

/**
 * Validate GeoNFT parameters
 */
function validateGeoNFTParams(params) {
    if (!params) return false;
    return !!(params.title && params.description && params.lat != null && params.long != null);
}

/**
 * Validate Quest parameters
 */
function validateQuestParams(params) {
    if (!params) return false;
    return !!(params.name && params.description);
}

/**
 * Validate Mission parameters
 */
function validateMissionParams(params) {
    if (!params) return false;
    return !!(params.name && params.description);
}

/**
 * Render missing fields message
 */
function renderMissingFieldsMessage(intent, params) {
    const missing = [];
    
    if (intent === 'create_nft') {
        if (!params.title) missing.push('title');
        if (!params.description) missing.push('description');
    } else if (intent === 'create_geonft') {
        if (!params.title) missing.push('title');
        if (!params.description) missing.push('description');
        if (params.lat == null) missing.push('latitude (lat)');
        if (params.long == null) missing.push('longitude (long)');
    } else if (intent === 'create_quest') {
        if (!params.name) missing.push('name');
        if (!params.description) missing.push('description');
    } else if (intent === 'create_mission') {
        if (!params.name) missing.push('name');
        if (!params.description) missing.push('description');
    }
    
    const missingList = missing.join(', ');
    const message = intent === 'create_nft' || intent === 'create_geonft' 
        ? `To create an NFT, I need: ${missingList}. Could you please provide ${missing.length === 1 ? 'this' : 'these'}?`
        : `To create a ${intent.replace('create_', '')}, I need: ${missingList}. Could you please provide ${missing.length === 1 ? 'this' : 'these'}?`;
    
    return `
        <div style="
            padding: 1rem;
            background: rgba(251, 191, 36, 0.1);
            border: 1px solid rgba(251, 191, 36, 0.3);
            border-radius: 0.5rem;
        ">
            <div style="
                font-size: 0.875rem;
                color: var(--text-primary);
                margin-bottom: 0.5rem;
                font-weight: 500;
            ">I need a bit more information:</div>
            <div style="
                font-size: 0.875rem;
                color: var(--text-secondary);
                line-height: 1.5;
            ">${escapeHtml(message)}</div>
        </div>
    `;
}

/**
 * Render NFT preview
 */
function renderNFTPreview(params) {
    return `
        <div style="
            padding: 1rem;
            background: rgba(0, 0, 0, 0.4);
            border: 1px solid var(--border-color);
            border-radius: 0.5rem;
        ">
            <div style="display: grid; grid-template-columns: 150px 1fr; gap: 1.5rem;">
                <div>
                    <div style="
                        font-size: 0.75rem;
                        color: var(--text-tertiary);
                        margin-bottom: 0.5rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                    ">Preview</div>
                    <div style="
                        width: 100%;
                        aspect-ratio: 1;
                        background: rgba(255, 255, 255, 0.05);
                        border-radius: 0.5rem;
                        border: 1px solid var(--border-color);
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        color: var(--text-tertiary);
                        font-size: 0.75rem;
                    ">Image</div>
                </div>
                <div style="display: flex; flex-direction: column; gap: 1rem;">
                    <div>
                        <div style="
                            font-size: 0.75rem;
                            color: var(--text-tertiary);
                            margin-bottom: 0.25rem;
                            text-transform: uppercase;
                            letter-spacing: 0.05em;
                        ">Title</div>
                        <div style="
                            font-size: 0.875rem;
                            font-weight: 500;
                            color: var(--text-primary);
                        ">${escapeHtml(params.title || 'Not specified')}</div>
                    </div>
                    <div>
                        <div style="
                            font-size: 0.75rem;
                            color: var(--text-tertiary);
                            margin-bottom: 0.25rem;
                            text-transform: uppercase;
                            letter-spacing: 0.05em;
                        ">Description</div>
                        <div style="
                            font-size: 0.8125rem;
                            color: var(--text-secondary);
                            line-height: 1.5;
                        ">${escapeHtml(params.description || 'Not specified')}</div>
                    </div>
                    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 1.5rem;">
                        <div>
                            <div style="
                                font-size: 0.75rem;
                                color: var(--text-tertiary);
                                margin-bottom: 0.25rem;
                                text-transform: uppercase;
                                letter-spacing: 0.05em;
                            ">Price</div>
                            <div style="
                                font-size: 0.875rem;
                                color: var(--text-primary);
                                font-weight: 500;
                            ">${params.price || 0} SOL</div>
                        </div>
                        <div>
                            <div style="
                                font-size: 0.75rem;
                                color: var(--text-tertiary);
                                margin-bottom: 0.25rem;
                                text-transform: uppercase;
                                letter-spacing: 0.05em;
                            ">Chain</div>
                            <div style="
                                font-size: 0.875rem;
                                color: var(--text-primary);
                            ">${params.onChainProvider || 'SolanaOASIS'}</div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Render GeoNFT preview
 */
function renderGeoNFTPreview(params) {
    return `
        <div style="
            padding: 1rem;
            background: rgba(0, 0, 0, 0.4);
            border: 1px solid var(--border-color);
            border-radius: 0.5rem;
        ">
            <div style="display: grid; grid-template-columns: repeat(2, 1fr); gap: 1.5rem;">
                <div>
                    <div style="
                        font-size: 0.75rem;
                        color: var(--text-tertiary);
                        margin-bottom: 0.25rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                    ">Title</div>
                    <div style="
                        font-size: 0.875rem;
                        font-weight: 500;
                        color: var(--text-primary);
                    ">${escapeHtml(params.title || 'Not specified')}</div>
                </div>
                <div>
                    <div style="
                        font-size: 0.75rem;
                        color: var(--text-tertiary);
                        margin-bottom: 0.25rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                    ">Location</div>
                    <div style="
                        font-size: 0.875rem;
                        color: var(--text-primary);
                    ">${params.lat || 0}, ${params.long || 0}</div>
                </div>
                <div>
                    <div style="
                        font-size: 0.75rem;
                        color: var(--text-tertiary);
                        margin-bottom: 0.25rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                    ">Description</div>
                    <div style="
                        font-size: 0.8125rem;
                        color: var(--text-secondary);
                        line-height: 1.5;
                    ">${escapeHtml(params.description || 'Not specified')}</div>
                </div>
                <div>
                    <div style="
                        font-size: 0.75rem;
                        color: var(--text-tertiary);
                        margin-bottom: 0.25rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                    ">Price</div>
                    <div style="
                        font-size: 0.875rem;
                        color: var(--text-primary);
                        font-weight: 500;
                    ">${params.price || 0} SOL</div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Render error message
 */
function renderErrorMessage(error) {
    return `
        <div style="
            padding: 0.75rem 1rem;
            background: rgba(239, 68, 68, 0.1);
            border: 1px solid rgba(239, 68, 68, 0.3);
            border-radius: 0.5rem;
            color: rgba(239, 68, 68, 1);
            font-size: 0.875rem;
        ">
            ${escapeHtml(error)}
        </div>
    `;
}

/**
 * Render create section
 */
function renderCreateSection() {
    if (!aiNFTAssistantState.parsedData) return '';
    
    const intent = aiNFTAssistantState.parsedData.intent?.toLowerCase();
    let buttonText = 'Create';
    let descriptionText = 'Review the details above and click to create';
    
    if (intent === 'create_nft') {
        buttonText = 'Create NFT';
        descriptionText = 'Review the details above and click to create your NFT';
    } else if (intent === 'create_geonft' || intent === 'place_geonft') {
        buttonText = intent === 'place_geonft' ? 'Place GeoNFT' : 'Create GeoNFT';
        descriptionText = `Review the details above and click to ${intent === 'place_geonft' ? 'place' : 'create'} your GeoNFT`;
    } else if (intent === 'create_quest') {
        buttonText = 'Create Quest';
        descriptionText = 'Review the details above and click to create your quest';
    } else if (intent === 'create_mission') {
        buttonText = 'Create Mission';
        descriptionText = 'Review the details above and click to create your mission';
    } else if (intent === 'start_quest') {
        buttonText = 'Start Quest';
        descriptionText = 'Click to start the quest';
    } else if (intent === 'complete_quest') {
        buttonText = 'Complete Quest';
        descriptionText = 'Click to complete the quest';
    }

    return `
        <div class="portal-section">
            <div class="portal-card" style="
                padding: 1.5rem;
                text-align: center;
                background: rgba(34, 197, 94, 0.05);
                border-color: rgba(34, 197, 94, 0.2);
            ">
                <button
                    type="button"
                    onclick="executeAIIntent()"
                    class="btn-primary"
                    style="
                        padding: 0.75rem 2rem;
                        margin-bottom: 0.75rem;
                    "
                >
                    ${buttonText}
                </button>
                <p style="
                    font-size: 0.75rem;
                    color: var(--text-tertiary);
                    margin: 0;
                ">${descriptionText}</p>
            </div>
        </div>
    `;
}

/**
 * Process AI request
 */
async function processAIRequest() {
    const inputField = document.getElementById('ai-input-field');
    const userInput = inputField?.value.trim();

    if (!userInput) {
        showAIError('Please enter a description of the NFT you want to create');
        return;
    }

    if (!aiNFTAssistantState.apiKey) {
        showAIError('OpenAI API key not configured. Please configure it in settings.');
        return;
    }

    aiNFTAssistantState.userInput = userInput;
    aiNFTAssistantState.isLoading = true;
    aiNFTAssistantState.error = null;
    aiNFTAssistantState.parsedData = null;

    // Update UI
    updateAIMessagesContainer();

    try {
        // Call OpenAI API (or backend endpoint)
        const parsedIntent = await callOpenAIAPI(userInput);
        
        aiNFTAssistantState.parsedData = parsedIntent;
        aiNFTAssistantState.isLoading = false;
        
        // Update UI
        const container = document.getElementById('nft-mint-studio-content');
        if (container && nftMintStudioState.mode === 'ai') {
            container.innerHTML = renderAINFTAssistant();
        }
    } catch (error) {
        console.error('Error processing AI request:', error);
        aiNFTAssistantState.error = error.message || 'Failed to parse request. Please try rephrasing.';
        aiNFTAssistantState.isLoading = false;
        updateAIMessagesContainer();
    }
}

/**
 * Call OpenAI API to parse intent
 */
async function callOpenAIAPI(userInput) {
    // Build context
    const context = buildAIContext();
    
    // For now, we'll need to call a backend endpoint
    // In production, this should be a secure backend call, not direct API call from frontend
    const response = await fetch('/api/ai/parse-intent', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            userInput: userInput,
            context: context
        })
    });

    if (!response.ok) {
        throw new Error('Failed to parse request');
    }

    const result = await response.json();
    return result;
}

/**
 * Build AI context
 */
function buildAIContext() {
    // Get current avatar info if available
    const avatar = window.currentAvatar || getAvatar() || {};
    
    return {
        avatar: avatar.username || 'Unknown',
        avatarId: avatar.id || '',
        availableProviders: ['SolanaOASIS', 'MongoDBOASIS'],
        defaultOnChainProvider: 'SolanaOASIS',
        defaultOffChainProvider: 'MongoDBOASIS'
    };
}

/**
 * Execute AI intent (renamed from createNFTFromAI for clarity)
 */
async function executeAIIntent() {
    if (!aiNFTAssistantState.parsedData || !aiNFTAssistantState.parsedData.parameters) {
        showAIError('No parsed data available. Please parse a request first.');
        return;
    }

    const params = aiNFTAssistantState.parsedData.parameters;
    const intent = aiNFTAssistantState.parsedData.intent?.toLowerCase();

    try {
        if (intent === 'create_nft') {
            await createNFTFromAIParams(params);
        } else if (intent === 'create_geonft') {
            await createGeoNFTFromAIParams(params);
        } else if (intent === 'place_geonft') {
            await placeGeoNFTFromAIParams(params);
        } else if (intent === 'create_quest') {
            await createQuestFromAIParams(params);
        } else if (intent === 'create_mission') {
            await createMissionFromAIParams(params);
        } else if (intent === 'start_quest') {
            await startQuestFromAIParams(params);
        } else if (intent === 'complete_quest') {
            await completeQuestFromAIParams(params);
        } else if (intent === 'show_quest_progress') {
            await showQuestProgressFromAIParams(params);
        } else if (intent === 'show_nearby_geonfts') {
            await showNearbyGeoNFTsFromAIParams(params);
        } else {
            showAIError(`Unsupported intent type: ${intent}`);
        }
    } catch (error) {
        console.error('Error executing AI intent:', error);
        showAIError(`Failed to execute: ${error.message}`);
    }
}

// Legacy function name for backward compatibility
async function createNFTFromAI() {
    return executeAIIntent();
}

/**
 * Create NFT from AI params
 */
async function createNFTFromAIParams(params) {
    // Use uploaded file URL if available, otherwise use imageUrl from params
    const uploadedUrls = getUploadedFileUrls();
    const imageUrl = uploadedUrls.length > 0 ? uploadedUrls[0] : (params.imageUrl || '');
    
    // Use existing NFT creation API
    const request = {
        title: params.title,
        description: params.description || '',
        price: params.price || 0,
        imageUrl: imageUrl,
        symbol: params.symbol || 'OASISNFT',
        onChainProvider: params.onChainProvider || 'SolanaOASIS',
        offChainProvider: 'MongoDBOASIS',
        nftStandardType: 'SPL',
        nftOffChainMetaType: 'OASIS',
        storeNFTMetaDataOnChain: false,
        numberToMint: 1
    };

    // Call existing NFT minting endpoint
    const baseUrl = nftMintStudioState.baseUrl || window.location.origin;
    const avatar = getAvatar();
    const authToken = nftMintStudioState.authToken || window.authToken;

    const response = await fetch(`${baseUrl}/api/nft/mint`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': authToken ? `Bearer ${authToken}` : ''
        },
        body: JSON.stringify({
            ...request,
            mintedByAvatarId: avatar?.id || nftMintStudioState.avatarId
        })
    });

    if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Failed to create NFT');
    }

    const result = await response.json();
    
    // Show success message
    showAISuccess(`Successfully created NFT: ${result.result?.title || 'NFT'}`);
    
    // Clear the chat and show success
    setTimeout(() => {
        clearAIChat();
    }, 2000);
}

/**
 * Create GeoNFT from AI params
 */
async function createGeoNFTFromAIParams(params) {
    // Validate required fields
    if (!params.title || !params.description || params.lat == null || params.long == null) {
        showAIError('Title, description, latitude, and longitude are required to create a GeoNFT. Please provide all of these.');
        return;
    }
    
    // Convert coordinates to micro-degrees (multiply by 1,000,000)
    const latMicroDegrees = Math.round((parseFloat(params.lat) || 0) * 1000000);
    const longMicroDegrees = Math.round((parseFloat(params.long) || 0) * 1000000);

    // Use uploaded file URL if available, otherwise use imageUrl from params
    const uploadedUrls = getUploadedFileUrls();
    const imageUrl = uploadedUrls.length > 0 ? uploadedUrls[0] : (params.imageUrl || '');

    const request = {
        title: params.title,
        description: params.description || '',
        price: parseFloat(params.price) || 0,
        imageUrl: imageUrl,
        lat: latMicroDegrees,
        long: longMicroDegrees,
        onChainProvider: params.onChainProvider || 'SolanaOASIS',
        offChainProvider: 'MongoDBOASIS',
        nftStandardType: 'SPL',
        nftOffChainMetaType: 'OASIS',
        storeNFTMetaDataOnChain: false,
        numberToMint: 1,
        allowOtherPlayersToAlsoCollect: true,
        permSpawn: false,
        globalSpawnQuantity: 1,
        playerSpawnQuantity: 1,
        respawnDurationInSeconds: 0
    };

    const baseUrl = nftMintStudioState.baseUrl || window.location.origin;
    const avatar = getAvatar();
    const authToken = nftMintStudioState.authToken || window.authToken;

    const response = await fetch(`${baseUrl}/api/nft/mint-and-place-geo-nft`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': authToken ? `Bearer ${authToken}` : ''
        },
        body: JSON.stringify({
            ...request,
            mintedByAvatarId: avatar?.id || nftMintStudioState.avatarId
        })
    });

    if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Failed to create GeoNFT');
    }

    const result = await response.json();
    showAISuccess(`Successfully created GeoNFT: ${result.result?.title || 'GeoNFT'}`);
    setTimeout(() => clearAIChat(), 2000);
}

/**
 * Place GeoNFT from AI params
 */
async function placeGeoNFTFromAIParams(params) {
    const latMicroDegrees = Math.round((parseFloat(params.lat) || 0) * 1000000);
    const longMicroDegrees = Math.round((parseFloat(params.long) || 0) * 1000000);

    const request = {
        originalOASISNFTId: params.geonftId,
        lat: latMicroDegrees,
        long: longMicroDegrees,
        allowOtherPlayersToAlsoCollect: true,
        permSpawn: false,
        globalSpawnQuantity: 1,
        playerSpawnQuantity: 1,
        respawnDurationInSeconds: 0,
        geoNFTMetaDataProvider: 'MongoDBOASIS'
    };

    const baseUrl = nftMintStudioState.baseUrl || window.location.origin;
    const avatar = getAvatar();
    const authToken = nftMintStudioState.authToken || window.authToken;

    const response = await fetch(`${baseUrl}/api/nft/place-geo-nft`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': authToken ? `Bearer ${authToken}` : ''
        },
        body: JSON.stringify({
            ...request,
            placedByAvatarId: avatar?.id || nftMintStudioState.avatarId
        })
    });

    if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Failed to place GeoNFT');
    }

    const result = await response.json();
    showAISuccess(`Successfully placed GeoNFT`);
    setTimeout(() => clearAIChat(), 2000);
}

/**
 * Create Quest from AI params
 */
async function createQuestFromAIParams(params) {
    const request = {
        name: params.name,
        description: params.description || '',
        questType: params.questType || 'MainQuest',
        difficulty: params.difficulty || 'Easy',
        rewardKarma: parseInt(params.rewardKarma) || 0,
        rewardXP: parseInt(params.rewardXP) || 0,
        parentMissionId: params.parentMissionId || null
    };

    const baseUrl = nftMintStudioState.baseUrl || window.location.origin;
    const avatar = getAvatar();
    const authToken = nftMintStudioState.authToken || window.authToken;

    const response = await fetch(`${baseUrl}/api/quests`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': authToken ? `Bearer ${authToken}` : ''
        },
        body: JSON.stringify({
            ...request,
            createdByAvatarId: avatar?.id || nftMintStudioState.avatarId
        })
    });

    if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Failed to create quest');
    }

    const result = await response.json();
    showAISuccess(`Successfully created quest: ${result.result?.name || 'Quest'}`);
    setTimeout(() => clearAIChat(), 2000);
}

/**
 * Create Mission from AI params
 */
async function createMissionFromAIParams(params) {
    const request = {
        name: params.name,
        description: params.description || '',
        missionType: params.missionType || 'Easy'
    };

    const baseUrl = nftMintStudioState.baseUrl || window.location.origin;
    const avatar = getAvatar();
    const authToken = nftMintStudioState.authToken || window.authToken;

    const response = await fetch(`${baseUrl}/api/missions`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': authToken ? `Bearer ${authToken}` : ''
        },
        body: JSON.stringify({
            ...request,
            createdByAvatarId: avatar?.id || nftMintStudioState.avatarId
        })
    });

    if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Failed to create mission');
    }

    const result = await response.json();
    showAISuccess(`Successfully created mission: ${result.result?.name || 'Mission'}`);
    setTimeout(() => clearAIChat(), 2000);
}

/**
 * Start Quest from AI params
 */
async function startQuestFromAIParams(params) {
    const baseUrl = nftMintStudioState.baseUrl || window.location.origin;
    const avatar = getAvatar();
    const authToken = nftMintStudioState.authToken || window.authToken;
    const questId = params.questId || params.questName;

    if (!questId) {
        throw new Error('Quest ID or name is required');
    }

    const response = await fetch(`${baseUrl}/api/quests/${questId}/start`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': authToken ? `Bearer ${authToken}` : ''
        },
        body: JSON.stringify({
            avatarId: avatar?.id || nftMintStudioState.avatarId
        })
    });

    if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Failed to start quest');
    }

    const result = await response.json();
    showAISuccess(`Successfully started quest`);
    setTimeout(() => clearAIChat(), 2000);
}

/**
 * Complete Quest from AI params
 */
async function completeQuestFromAIParams(params) {
    const baseUrl = nftMintStudioState.baseUrl || window.location.origin;
    const avatar = getAvatar();
    const authToken = nftMintStudioState.authToken || window.authToken;
    const questId = params.questId || params.questName;

    if (!questId) {
        throw new Error('Quest ID or name is required');
    }

    const response = await fetch(`${baseUrl}/api/quests/${questId}/complete`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': authToken ? `Bearer ${authToken}` : ''
        },
        body: JSON.stringify({
            avatarId: avatar?.id || nftMintStudioState.avatarId
        })
    });

    if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Failed to complete quest');
    }

    const result = await response.json();
    showAISuccess(`Successfully completed quest`);
    setTimeout(() => clearAIChat(), 2000);
}

/**
 * Show Quest Progress from AI params
 */
async function showQuestProgressFromAIParams(params) {
    // This would display quest progress in the UI
    // For now, just show a message
    showAIError('Quest progress display coming soon!');
}

/**
 * Show Nearby GeoNFTs from AI params
 */
async function showNearbyGeoNFTsFromAIParams(params) {
    // This would display nearby GeoNFTs on a map
    // For now, just show a message
    showAIError('Nearby GeoNFTs display coming soon!');
}

/**
 * Update AI messages container
 */
function updateAIMessagesContainer() {
    const container = document.getElementById('ai-messages-container');
    if (!container) return;

    container.innerHTML = `
        ${renderAIWelcomeMessage()}
        ${aiNFTAssistantState.userInput ? renderUserMessage(aiNFTAssistantState.userInput) : ''}
        ${aiNFTAssistantState.isLoading ? renderAILoadingMessage() : ''}
        ${aiNFTAssistantState.parsedData ? renderParsedPreview(aiNFTAssistantState.parsedData) : ''}
        ${aiNFTAssistantState.error ? renderErrorMessage(aiNFTAssistantState.error) : ''}
    `;
}

/**
 * Clear AI chat
 */
function clearAIChat() {
    // Clear uploaded files and revoke object URLs
    aiNFTAssistantState.uploadedFiles.forEach(fileData => {
        if (fileData.preview) {
            URL.revokeObjectURL(fileData.preview);
        }
    });
    aiNFTAssistantState.uploadedFiles = [];
    
    aiNFTAssistantState.userInput = '';
    aiNFTAssistantState.parsedData = null;
    aiNFTAssistantState.error = null;
    aiNFTAssistantState.isLoading = false;
    
    const inputField = document.getElementById('ai-input-field');
    if (inputField) inputField.value = '';
    
    const container = document.getElementById('nft-mint-studio-content');
    if (container && nftMintStudioState.mode === 'ai') {
        container.innerHTML = renderAINFTAssistant();
    }
}

/**
 * Render Quest preview
 */
function renderQuestPreview(params) {
    return `
        <div style="
            padding: 1rem;
            background: rgba(0, 0, 0, 0.4);
            border: 1px solid var(--border-color);
            border-radius: 0.5rem;
        ">
            <div style="display: flex; flex-direction: column; gap: 1rem;">
                <div>
                    <div style="
                        font-size: 0.75rem;
                        color: var(--text-tertiary);
                        margin-bottom: 0.25rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                    ">Name</div>
                    <div style="
                        font-size: 0.875rem;
                        font-weight: 500;
                        color: var(--text-primary);
                    ">${escapeHtml(params.name || 'Not specified')}</div>
                </div>
                <div>
                    <div style="
                        font-size: 0.75rem;
                        color: var(--text-tertiary);
                        margin-bottom: 0.25rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                    ">Description</div>
                    <div style="
                        font-size: 0.8125rem;
                        color: var(--text-secondary);
                        line-height: 1.5;
                    ">${escapeHtml(params.description || 'Not specified')}</div>
                </div>
                <div style="display: grid; grid-template-columns: repeat(2, 1fr); gap: 1.5rem;">
                    <div>
                        <div style="
                            font-size: 0.75rem;
                            color: var(--text-tertiary);
                            margin-bottom: 0.25rem;
                            text-transform: uppercase;
                            letter-spacing: 0.05em;
                        ">Type</div>
                        <div style="
                            font-size: 0.875rem;
                            color: var(--text-primary);
                        ">${params.questType || 'MainQuest'}</div>
                    </div>
                    <div>
                        <div style="
                            font-size: 0.75rem;
                            color: var(--text-tertiary);
                            margin-bottom: 0.25rem;
                            text-transform: uppercase;
                            letter-spacing: 0.05em;
                        ">Difficulty</div>
                        <div style="
                            font-size: 0.875rem;
                            color: var(--text-primary);
                        ">${params.difficulty || 'Easy'}</div>
                    </div>
                    <div>
                        <div style="
                            font-size: 0.75rem;
                            color: var(--text-tertiary);
                            margin-bottom: 0.25rem;
                            text-transform: uppercase;
                            letter-spacing: 0.05em;
                        ">Reward XP</div>
                        <div style="
                            font-size: 0.875rem;
                            color: var(--text-primary);
                        ">${params.rewardXP || 0}</div>
                    </div>
                    <div>
                        <div style="
                            font-size: 0.75rem;
                            color: var(--text-tertiary);
                            margin-bottom: 0.25rem;
                            text-transform: uppercase;
                            letter-spacing: 0.05em;
                        ">Reward Karma</div>
                        <div style="
                            font-size: 0.875rem;
                            color: var(--text-primary);
                        ">${params.rewardKarma || 0}</div>
                    </div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Render Mission preview
 */
function renderMissionPreview(params) {
    return `
        <div style="
            padding: 1rem;
            background: rgba(0, 0, 0, 0.4);
            border: 1px solid var(--border-color);
            border-radius: 0.5rem;
        ">
            <div style="display: flex; flex-direction: column; gap: 1rem;">
                <div>
                    <div style="
                        font-size: 0.75rem;
                        color: var(--text-tertiary);
                        margin-bottom: 0.25rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                    ">Name</div>
                    <div style="
                        font-size: 0.875rem;
                        font-weight: 500;
                        color: var(--text-primary);
                    ">${escapeHtml(params.name || 'Not specified')}</div>
                </div>
                <div>
                    <div style="
                        font-size: 0.75rem;
                        color: var(--text-tertiary);
                        margin-bottom: 0.25rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                    ">Description</div>
                    <div style="
                        font-size: 0.8125rem;
                        color: var(--text-secondary);
                        line-height: 1.5;
                    ">${escapeHtml(params.description || 'Not specified')}</div>
                </div>
                <div>
                    <div style="
                        font-size: 0.75rem;
                        color: var(--text-tertiary);
                        margin-bottom: 0.25rem;
                        text-transform: uppercase;
                        letter-spacing: 0.05em;
                    ">Type</div>
                    <div style="
                        font-size: 0.875rem;
                        color: var(--text-primary);
                    ">${params.missionType || 'Easy'}</div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Render generic preview for unsupported intents
 */
function renderGenericPreview(parsedData) {
    return `
        <div style="
            padding: 1rem;
            background: rgba(0, 0, 0, 0.4);
            border: 1px solid var(--border-color);
            border-radius: 0.5rem;
        ">
            <div style="
                font-size: 0.875rem;
                color: var(--text-secondary);
            ">
                Intent: ${parsedData.intent}<br>
                Parameters will be processed by the backend API.
            </div>
        </div>
    `;
}

/**
 * Insert example prompt
 */
function insertExamplePrompt(type) {
    const inputField = document.getElementById('ai-input-field');
    if (!inputField) return;

    const examples = {
        nft: "Create an NFT called 'Sunset Dream' with description 'A beautiful sunset over the ocean' priced at 0.5 SOL",
        geonft: "Create a GeoNFT called 'London Landmark' at coordinates 51.5074, -0.1278 with description 'A special place in London' priced at 1 SOL",
        quest: "Create a quest called 'Find the Treasure' with description 'Find the hidden treasure' type MainQuest difficulty Easy reward 100 XP and 50 karma",
        mission: "Create a mission called 'Save the World' with description 'Complete all quests to save the world' type Medium"
    };

    inputField.value = examples[type] || examples.nft;
    inputField.focus();
}

/**
 * Show AI error
 */
function showAIError(message) {
    aiNFTAssistantState.error = message;
    updateAIMessagesContainer();
}

/**
 * Show AI success
 */
function showAISuccess(message) {
    // Show success notification (could use a toast or update UI)
    // For now, use alert - can be replaced with a toast notification system
    alert(message);
}

/**
 * Escape HTML
 */
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

/**
 * Render file upload section
 */
function renderFileUploadSection() {
    const uploadedFiles = aiNFTAssistantState.uploadedFiles || [];
    
    return `
        <div style="display: flex; flex-direction: column; gap: 0.75rem;">
            <div style="display: flex; gap: 0.5rem; align-items: center;">
                <input
                    type="file"
                    id="ai-file-input"
                    accept="image/*,video/*"
                    multiple
                    style="display: none;"
                    onchange="handleFileUpload(event)"
                />
                <button
                    type="button"
                    onclick="document.getElementById('ai-file-input').click()"
                    style="
                        padding: 0.5rem 1rem;
                        background: rgba(255, 255, 255, 0.05);
                        border: 1px solid var(--border-color);
                        border-radius: 0.375rem;
                        color: var(--text-primary);
                        font-size: 0.875rem;
                        cursor: pointer;
                        transition: all 0.2s;
                    "
                    onmouseover="this.style.background='rgba(255, 255, 255, 0.1)';"
                    onmouseout="this.style.background='rgba(255, 255, 255, 0.05)';"
                >
                    Upload Image/Video
                </button>
                <select
                    id="upload-provider-select"
                    style="
                        padding: 0.5rem;
                        background: rgba(0, 0, 0, 0.4);
                        border: 1px solid var(--border-color);
                        border-radius: 0.375rem;
                        color: var(--text-primary);
                        font-size: 0.875rem;
                    "
                    onchange="aiNFTAssistantState.uploadProvider = this.value;"
                >
                    <option value="PinataOASIS" selected>Pinata (IPFS)</option>
                    <option value="IPFSOASIS">IPFS</option>
                    <option value="HoloOASIS">Holochain</option>
                </select>
            </div>
            ${uploadedFiles.length > 0 ? renderUploadedFiles() : ''}
        </div>
    `;
}

/**
 * Handle file upload
 */
async function handleFileUpload(event) {
    const files = event.target.files;
    if (!files || files.length === 0) return;

    const baseUrl = nftMintStudioState.baseUrl || window.location.origin;
    const authToken = nftMintStudioState.authToken || window.authToken;

    for (let i = 0; i < files.length; i++) {
        const file = files[i];
        const fileData = {
            file: file,
            preview: URL.createObjectURL(file),
            url: null,
            uploading: true
        };

        aiNFTAssistantState.uploadedFiles.push(fileData);
        updateFileUploadSection();

        try {
            const formData = new FormData();
            formData.append('file', file);
            formData.append('provider', aiNFTAssistantState.uploadProvider || 'PinataOASIS');

            const response = await fetch(`${baseUrl}/api/file/upload`, {
                method: 'POST',
                headers: {
                    'Authorization': authToken ? `Bearer ${authToken}` : ''
                },
                body: formData
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Failed to upload file');
            }

            const result = await response.json();
            fileData.url = result.result || result;
            fileData.uploading = false;
        } catch (error) {
            console.error('Error uploading file:', error);
            fileData.uploading = false;
            fileData.error = error.message;
        }

        updateFileUploadSection();
    }

    // Reset file input
    event.target.value = '';
}

/**
 * Render uploaded files
 */
function renderUploadedFiles() {
    const files = aiNFTAssistantState.uploadedFiles || [];
    if (files.length === 0) return '';

    return `
        <div style="display: flex; flex-direction: column; gap: 0.5rem;">
            ${files.map((fileData, index) => `
                <div style="
                    display: flex;
                    align-items: center;
                    gap: 0.75rem;
                    padding: 0.75rem;
                    background: rgba(0, 0, 0, 0.4);
                    border: 1px solid var(--border-color);
                    border-radius: 0.375rem;
                ">
                    ${fileData.preview && fileData.file.type.startsWith('image/') ? `
                        <img
                            src="${fileData.preview}"
                            alt="${fileData.file.name}"
                            style="
                                width: 48px;
                                height: 48px;
                                object-fit: cover;
                                border-radius: 0.25rem;
                            "
                        />
                    ` : `
                        <div style="
                            width: 48px;
                            height: 48px;
                            background: rgba(255, 255, 255, 0.1);
                            border-radius: 0.25rem;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            font-size: 1.5rem;
                        ">📄</div>
                    `}
                    <div style="flex: 1; min-width: 0;">
                        <div style="
                            font-size: 0.875rem;
                            color: var(--text-primary);
                            white-space: nowrap;
                            overflow: hidden;
                            text-overflow: ellipsis;
                        ">${escapeHtml(fileData.file.name)}</div>
                        ${fileData.uploading ? `
                            <div style="
                                font-size: 0.75rem;
                                color: var(--text-secondary);
                            ">Uploading...</div>
                        ` : fileData.url ? `
                            <div style="
                                font-size: 0.75rem;
                                color: var(--text-success);
                            ">Uploaded</div>
                        ` : fileData.error ? `
                            <div style="
                                font-size: 0.75rem;
                                color: var(--text-error);
                            ">Error: ${escapeHtml(fileData.error)}</div>
                        ` : ''}
                    </div>
                    <button
                        type="button"
                        onclick="removeUploadedFile(${index})"
                        style="
                            padding: 0.25rem 0.5rem;
                            background: rgba(255, 0, 0, 0.2);
                            border: 1px solid rgba(255, 0, 0, 0.3);
                            border-radius: 0.25rem;
                            color: var(--text-error);
                            font-size: 0.75rem;
                            cursor: pointer;
                        "
                    >
                        Remove
                    </button>
                </div>
            `).join('')}
        </div>
    `;
}

/**
 * Update file upload section
 */
function updateFileUploadSection() {
    const container = document.getElementById('file-upload-section-container');
    if (container) {
        container.innerHTML = renderFileUploadSection();
    }
}

/**
 * Get uploaded file URLs
 */
function getUploadedFileUrls() {
    return (aiNFTAssistantState.uploadedFiles || [])
        .filter(fileData => fileData.url && !fileData.uploading)
        .map(fileData => fileData.url);
}

/**
 * Remove uploaded file
 */
function removeUploadedFile(index) {
    const files = aiNFTAssistantState.uploadedFiles || [];
    if (index >= 0 && index < files.length) {
        const fileData = files[index];
        if (fileData.preview) {
            URL.revokeObjectURL(fileData.preview);
        }
        files.splice(index, 1);
        updateFileUploadSection();
    }
}

// Export functions to window
if (typeof window !== 'undefined') {
    window.renderAINFTAssistant = renderAINFTAssistant;
    window.processAIRequest = processAIRequest;
    window.createNFTFromAI = createNFTFromAI;
    window.executeAIIntent = executeAIIntent;
    window.clearAIChat = clearAIChat;
    window.insertExamplePrompt = insertExamplePrompt;
    window.initAINFTAssistant = initAINFTAssistant;
    window.handleFileUpload = handleFileUpload;
    window.removeUploadedFile = removeUploadedFile;
    window.renderFileUploadSection = renderFileUploadSection;
    window.getUploadedFileUrls = getUploadedFileUrls;
    window.updateFileUploadSection = updateFileUploadSection;
    
    // Initialize mode toggle listeners when AI assistant is rendered
    // This ensures the buttons work even if called before nft-mint-studio.js loads
    setTimeout(() => {
        if (typeof attachModeToggleListenersForAI === 'function') {
            attachModeToggleListenersForAI();
        }
    }, 500);
}
