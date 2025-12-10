/**
 * NFT Mint Studio - Vanilla JavaScript Implementation
 * Direct integration into OASIS Portal (converted from React)
 */

// NFT Mint Studio State
const nftMintStudioState = {
    loading: false,
    activeStep: 'solana-config',
    configPreset: 'Metaplex Standard',
    authToken: null,
    avatarId: null,
    providerStates: [
        {
            id: 'solana',
            label: 'SolanaOASIS',
            description: 'Handles on-chain mint + transfer across Solana devnet',
            registerEndpoint: '/api/provider/register-provider-type/SolanaOASIS',
            activateEndpoint: '/api/provider/activate-provider/SolanaOASIS',
            state: 'idle' // 'idle' | 'registered' | 'active'
        },
        {
            id: 'mongo',
            label: 'MongoDBOASIS',
            description: 'Stores off-chain metadata JSON for NFTs',
            registerEndpoint: '/api/provider/register-provider-type/MongoDBOASIS',
            activateEndpoint: '/api/provider/activate-provider/MongoDBOASIS',
            state: 'idle'
        }
    ],
    providerLoading: [],
    assetDraft: {
        title: 'MetaBrick Test NFT',
        description: 'Test NFT minted via OASIS Portal',
        symbol: 'MBRICK',
        jsonUrl: '',
        imageUrl: '',
        thumbnailUrl: '',
        sendToAddress: '85ArqfA2fy8spGcMGsSW7cbEJAWj26vewmmoG2bwkgT9',
        recipientLabel: 'Primary Recipient',
        imageFileName: null,
        thumbnailFileName: null,
        imageData: null,
        thumbnailData: null,
        imageUploading: false,
        thumbnailUploading: false,
        metadataUploading: false
    },
    x402Config: {
        enabled: false,
        paymentEndpoint: '',
        revenueModel: 'equal', // 'equal' | 'weighted' | 'creator-split'
        treasuryWallet: '',
        preAuthorizeDistributions: false,
        metadata: {
            contentType: 'other',
            distributionFrequency: 'realtime',
            revenueSharePercentage: 100,
            creatorSplitPercentage: 50
        }
    },
    mintReady: false,
    baseUrl: window.location.hostname.includes('devnet') || window.location.hostname === 'localhost'
        ? 'http://devnet.oasisweb4.one'
        : 'https://oasisweb4.one'
};

const WIZARD_STEPS = [
    { id: 'solana-config', title: 'Solana Configuration', description: 'Select the minting profile you want to use (Metaplex, editions, compression).' },
    { id: 'auth', title: 'Authenticate & Providers', description: 'Login with Site Avatar credentials and activate SolanaOASIS + MongoDBOASIS.' },
    { id: 'assets', title: 'Assets & Metadata', description: 'Upload artwork, thumbnails, and JSON metadata placeholders.' },
    { id: 'x402-revenue', title: 'x402 Revenue Sharing', description: 'Enable automatic payment distribution to NFT holders via x402 protocol.' },
    { id: 'mint', title: 'Review & Mint', description: 'Generate the PascalCase payload and fire `/api/nft/mint-nft`.' }
];

/**
 * Load NFT Mint Studio into the portal
 */
async function loadNFTMintStudio() {
    const container = document.getElementById('nft-mint-studio-content');
    if (!container) {
        console.error('NFT Mint Studio container not found');
        return;
    }

    nftMintStudioState.loading = true;
    container.innerHTML = renderLoadingState();

    try {
        renderNFTMintStudio(container);
        nftMintStudioState.loading = false;
    } catch (error) {
        console.error('Error loading NFT Mint Studio:', error);
        container.innerHTML = renderErrorState(error.message);
        nftMintStudioState.loading = false;
    }
}

/**
 * Render the main NFT Mint Studio UI
 */
function renderNFTMintStudio(container) {
    container.innerHTML = `
        <div class="portal-section">
            <div class="portal-section-header">
                <div>
                    <h2 class="portal-section-title">NFT Mint Studio</h2>
                    <p class="portal-section-subtitle">Multi-chain NFT minting platform powered by OASIS</p>
                </div>
                <div style="display: flex; gap: 0.5rem; align-items: center;">
                    <span style="font-size: 0.875rem; color: var(--text-secondary);">
                        ${nftMintStudioState.baseUrl.includes('devnet') ? '🔷 Devnet' : '✅ Production'}
                    </span>
                </div>
            </div>
        </div>

        <div class="portal-section">
            ${renderSessionSummary()}
        </div>

        <div class="portal-section">
            ${renderWizardShell()}
        </div>
    `;

    // Attach event listeners
    attachWizardListeners();
}

/**
 * Render session summary
 */
function renderSessionSummary() {
    const providerActive = nftMintStudioState.providerStates.every(p => p.state === 'active');
    const statusBadge = providerActive && nftMintStudioState.mintReady 
        ? '<span style="border: 1px solid rgba(34, 197, 94, 0.6); background: rgba(20, 118, 96, 0.25); color: rgba(34, 197, 94, 1); padding: 0.25rem 0.75rem; border-radius: 999px; font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.1em;">Ready To Mint</span>'
        : '<span style="border: 1px solid rgba(239, 68, 68, 0.6); background: rgba(120, 35, 50, 0.2); color: rgba(239, 68, 68, 1); padding: 0.25rem 0.75rem; border-radius: 999px; font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.1em;">Pending Configuration</span>';

    return `
        <div class="portal-card" style="display: flex; flex-wrap: wrap; align-items: center; gap: 1rem; font-size: 0.6875rem;">
            <span style="font-size: 0.5625rem; text-transform: uppercase; letter-spacing: 0.16em; color: var(--text-tertiary);">Session Summary</span>
            <div style="display: flex; align-items: center; gap: 0.75rem;">
                <span style="color: var(--text-primary); font-size: 0.75rem; font-weight: 500;">Profile</span>
                <span style="color: var(--text-secondary);">${nftMintStudioState.configPreset}</span>
            </div>
            <div style="display: flex; align-items: center; gap: 0.75rem;">
                <span style="color: var(--text-primary); font-size: 0.75rem; font-weight: 500;">On-chain</span>
                <span style="color: var(--text-secondary);">SolanaOASIS (3)</span>
            </div>
            <div style="display: flex; align-items: center; gap: 0.75rem;">
                <span style="color: var(--text-primary); font-size: 0.75rem; font-weight: 500;">Off-chain</span>
                <span style="color: var(--text-secondary);">MongoDBOASIS (23)</span>
            </div>
            <div style="display: flex; align-items: center; gap: 0.75rem;">
                <span style="color: var(--text-primary); font-size: 0.75rem; font-weight: 500;">x402</span>
                <span style="color: ${nftMintStudioState.x402Config.enabled ? 'rgba(34, 197, 94, 1)' : 'var(--text-secondary)'};">
                    ${nftMintStudioState.x402Config.enabled ? 'Enabled ✓' : 'Disabled'}
                </span>
            </div>
            ${statusBadge}
        </div>
    `;
}

/**
 * Render wizard shell with steps
 */
function renderWizardShell() {
    const activeStepIndex = WIZARD_STEPS.findIndex(s => s.id === nftMintStudioState.activeStep);
    
    return `
        <div class="portal-card" style="position: relative; overflow: hidden; padding: 2rem;">
            <div style="position: absolute; inset: 0; background: radial-gradient(circle at top left, rgba(255, 255, 255, 0.05), transparent 70%); pointer-events: none;"></div>
            <div style="display: grid; grid-template-columns: 280px 1fr; gap: 2.5rem; position: relative;">
                <aside style="display: flex; flex-direction: column; gap: 1.5rem;">
                    <div>
                        <h2 style="font-size: 1.125rem; font-weight: 500; color: var(--text-primary); margin-bottom: 0.5rem;">Solana Mint Flow</h2>
                        <p style="font-size: 0.875rem; color: var(--text-secondary); line-height: 1.6;">
                            Configure SolanaOASIS + MongoDBOASIS, upload your assets, and submit a compliant payload.
                        </p>
                    </div>
                    <ol style="display: flex; flex-direction: column; gap: 0.75rem; list-style: none; padding: 0; margin: 0;">
                        ${WIZARD_STEPS.map((step, index) => {
                            const isActive = step.id === nftMintStudioState.activeStep;
                            const isPast = activeStepIndex > index;
                            return `
                                <li>
                                    <button 
                                        type="button"
                                        class="wizard-step-btn"
                                        data-step="${step.id}"
                                        style="
                                            width: 100%;
                                            display: flex;
                                            align-items: center;
                                            gap: 0.75rem;
                                            padding: 0.75rem 1rem;
                                            border-radius: 0.5rem;
                                            border: 1px solid ${isActive ? 'rgba(255, 255, 255, 0.2)' : 'transparent'};
                                            background: ${isActive ? 'rgba(255, 255, 255, 0.04)' : 'rgba(255, 255, 255, 0.02)'};
                                            color: ${isActive ? 'var(--text-primary)' : 'var(--text-secondary)'};
                                            text-align: left;
                                            cursor: pointer;
                                            transition: all 0.2s;
                                            font-family: inherit;
                                        "
                                        onmouseover="this.style.borderColor='rgba(255, 255, 255, 0.2)'; this.style.background='rgba(255, 255, 255, 0.04)'; this.style.color='var(--text-primary)';"
                                        onmouseout="
                                            const isActive = '${step.id}' === '${nftMintStudioState.activeStep}';
                                            this.style.borderColor = isActive ? 'rgba(255, 255, 255, 0.2)' : 'transparent';
                                            this.style.background = isActive ? 'rgba(255, 255, 255, 0.04)' : 'rgba(255, 255, 255, 0.02)';
                                            this.style.color = isActive ? 'var(--text-primary)' : 'var(--text-secondary)';
                                        "
                                    >
                                        <span style="
                                            display: flex;
                                            align-items: center;
                                            justify-content: center;
                                            width: 24px;
                                            height: 24px;
                                            border-radius: 50%;
                                            font-size: 0.75rem;
                                            font-weight: 500;
                                            background: ${isActive ? 'var(--text-primary)' : 'rgba(255, 255, 255, 0.1)'};
                                            color: ${isActive ? 'var(--bg-primary)' : 'var(--text-secondary)'};
                                        ">${index + 1}</span>
                                        <div>
                                            <p style="font-weight: 500; line-height: 1.3; margin: 0; font-size: 0.875rem;">${step.title}</p>
                                            <p style="font-size: 0.75rem; color: var(--text-tertiary); margin: 0; margin-top: 0.125rem;">${step.description}</p>
                                        </div>
                                    </button>
                                </li>
                            `;
                        }).join('')}
                    </ol>
                </aside>
                <section class="portal-card" style="min-height: 460px;">
                    <div id="wizard-step-content">
                        ${renderCurrentStep()}
                    </div>
                    <div style="margin-top: 2rem; padding-top: 1.5rem; border-top: 1px solid var(--border-color);">
                        ${renderWizardFooter()}
                    </div>
                </section>
            </div>
        </div>
    `;
}

/**
 * Render current step content
 */
function renderCurrentStep() {
    switch (nftMintStudioState.activeStep) {
        case 'solana-config':
            return renderSolanaConfigStep();
        case 'auth':
            return renderAuthStep();
        case 'assets':
            return renderAssetsStep();
        case 'x402-revenue':
            return renderX402Step();
        case 'mint':
            return renderMintStep();
        default:
            return '<p>Unknown step</p>';
    }
}

/**
 * Render Solana Configuration step
 */
function renderSolanaConfigStep() {
    const presets = [
        { id: 'Metaplex Standard', title: 'Metaplex Standard', desc: 'Default Solana NFT format with metadata hosted off-chain and verified collection support.' },
        { id: 'Collection with Verified Creator', title: 'Verified Creator', desc: 'Configure collection and creator signatures to comply with marketplaces like Magic Eden.' },
        { id: 'Editioned Series', title: 'Editioned Series', desc: 'Enable limited edition prints or master edition drops managed through Metaplex.' },
        { id: 'Compressed NFT (Bubblegum)', title: 'Compressed NFT (Bubblegum)', desc: 'Prepare metadata for compressed mints via OASIS + Metaplex Bubblegum pipelines.' }
    ];

    return `
        <div style="display: flex; flex-direction: column; gap: 1.5rem;">
            <div>
                <h3 style="font-size: 1.25rem; font-weight: 500; color: var(--text-primary); margin-bottom: 0.5rem;">Select Minting Profile</h3>
                <p style="font-size: 0.875rem; color: var(--text-secondary);">Choose the mint type that best fits your NFT collection needs.</p>
            </div>
            <div style="display: grid; grid-template-columns: repeat(2, 1fr); gap: 1rem;">
                ${presets.map(preset => {
                    const isSelected = nftMintStudioState.configPreset === preset.id;
                    return `
                        <button
                            type="button"
                            class="config-preset-btn trading-template-card"
                            data-preset="${preset.id}"
                            style="${isSelected ? 'border-color: rgba(255, 255, 255, 0.3); background: rgba(255, 255, 255, 0.06);' : ''}"
                        >
                            <h4 style="font-size: 1rem; font-weight: 500; color: var(--text-primary); margin-bottom: 0.5rem;">${preset.title}</h4>
                            <p style="font-size: 0.875rem; color: var(--text-secondary);">${preset.desc}</p>
                        </button>
                    `;
                }).join('')}
            </div>
        </div>
    `;
}

/**
 * Render Authentication step
 */
function renderAuthStep() {
    return `
        <div style="display: flex; flex-direction: column; gap: 2rem;">
            <div>
                <h3 style="font-size: 1.25rem; font-weight: 500; color: var(--text-primary); margin-bottom: 0.5rem;">Authenticate Site Avatar</h3>
                <p style="font-size: 0.875rem; color: var(--text-secondary); margin-bottom: 1rem;">
                    Enter the Site Avatar credentials to obtain a JWT. The token is required for every subsequent provider call.
                </p>
                ${renderCredentialsPanel()}
            </div>
            <div>
                <h3 style="font-size: 1.25rem; font-weight: 500; color: var(--text-primary); margin-bottom: 0.5rem;">Register & Activate Providers</h3>
                <p style="font-size: 0.875rem; color: var(--text-secondary); margin-bottom: 1rem;">
                    Flip the toggles to enable SolanaOASIS and MongoDBOASIS. Both must show Active before minting.
                </p>
                ${renderProviderTogglePanel()}
            </div>
        </div>
    `;
}

/**
 * Render credentials panel
 */
function renderCredentialsPanel() {
    const authenticated = !!nftMintStudioState.authToken;
    
    return `
        <div style="display: flex; flex-direction: column; gap: 1rem;">
            <div class="portal-card" style="display: flex; flex-wrap: wrap; gap: 1rem; align-items: flex-end;">
                <div style="flex: 1; min-width: 200px;">
                    <label style="font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; color: var(--text-tertiary); display: block; margin-bottom: 0.5rem;">Username</label>
                    <input
                        type="text"
                        id="nft-username"
                        value="metabricks_admin"
                        placeholder="metabricks_admin"
                        style="width: 100%; padding: 0.5rem 0.75rem; border-radius: 0.5rem; border: 1px solid var(--border-color); background: rgba(0, 0, 0, 0.3); color: var(--text-primary); font-size: 0.875rem; font-family: inherit;"
                    />
                </div>
                <div style="flex: 1; min-width: 200px;">
                    <label style="font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; color: var(--text-tertiary); display: block; margin-bottom: 0.5rem;">Password</label>
                    <input
                        type="password"
                        id="nft-password"
                        value="Uppermall1!"
                        placeholder="Uppermall1!"
                        style="width: 100%; padding: 0.5rem 0.75rem; border-radius: 0.5rem; border: 1px solid var(--border-color); background: rgba(0, 0, 0, 0.3); color: var(--text-primary); font-size: 0.875rem; font-family: inherit;"
                    />
                </div>
                <div style="display: flex; flex-direction: column; gap: 0.75rem;">
                    <button
                        type="button"
                        id="nft-auth-btn"
                        onclick="handleNFTAuthenticate()"
                        class="btn-primary"
                        style="white-space: nowrap; ${authenticated ? 'background: rgba(34, 197, 94, 0.2); color: rgba(34, 197, 94, 1);' : ''}"
                    >
                        ${authenticated ? '✓ Authenticated' : 'Authenticate Avatar'}
                    </button>
                    <button
                        type="button"
                        onclick="window.open('https://metabricks.xyz', '_blank')"
                        class="btn-text"
                        style="white-space: nowrap; border: 1px solid var(--border-color); padding: 0.5rem 1rem; border-radius: 0.5rem;"
                    >
                        Acquire Avatar
                    </button>
                </div>
            </div>
            <p style="font-size: 0.75rem; color: var(--text-tertiary);">
                No avatar yet? Purchasing a MetaBrick at <a href="https://metabricks.xyz" target="_blank" style="color: var(--text-primary); text-decoration: underline;">MetaBricks.xyz</a> will provision credentials automatically.
            </p>
            <div id="nft-auth-message" style="font-size: 0.75rem;"></div>
        </div>
    `;
}

/**
 * Handle NFT authentication
 */
async function handleNFTAuthenticate() {
    const username = document.getElementById('nft-username')?.value;
    const password = document.getElementById('nft-password')?.value;
    const btn = document.getElementById('nft-auth-btn');
    const messageEl = document.getElementById('nft-auth-message');

    if (!username || !password) {
        if (messageEl) {
            messageEl.style.color = 'rgba(239, 68, 68, 1)';
            messageEl.textContent = 'Username and password are required';
        }
        return;
    }

    if (btn) {
        btn.disabled = true;
        btn.textContent = 'Authenticating...';
    }

    try {
        // Use OASIS API if available, otherwise direct call
        let response;
        if (typeof oasisAPI !== 'undefined' && oasisAPI.authenticateAvatar) {
            response = await oasisAPI.authenticateAvatar(username, password);
        } else {
            // Direct API call
            response = await fetch(`${nftMintStudioState.baseUrl}/api/avatar/authenticate`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password })
            });
            response = await response.json();
        }

        if (response && (response.result?.jwtToken || response.jwtToken || response.token)) {
            const token = response.result?.jwtToken || response.jwtToken || response.token;
            const avatarId = response.result?.avatarId || response.avatarId || response.result?.avatar?.id || response.result?.avatar?.AvatarId;

            nftMintStudioState.authToken = token;
            nftMintStudioState.avatarId = avatarId || null;

            if (btn) {
                btn.textContent = '✓ Authenticated';
                btn.style.background = 'rgba(34, 197, 94, 0.2)';
                btn.style.color = 'rgba(34, 197, 94, 1)';
                btn.style.border = '1px solid rgba(34, 197, 94, 0.6)';
            }

            if (messageEl) {
                messageEl.style.color = 'rgba(34, 197, 94, 1)';
                messageEl.textContent = 'Authentication successful!';
            }

            // Re-render to update UI
            updateWizardStep();
        } else {
            throw new Error(response?.message || 'Authentication failed');
        }
    } catch (error) {
        console.error('Authentication error:', error);
        if (btn) {
            btn.disabled = false;
            btn.textContent = 'Authenticate Avatar';
        }
        if (messageEl) {
            messageEl.style.color = 'rgba(239, 68, 68, 1)';
            messageEl.textContent = error.message || 'Authentication failed';
        }
    }
}

/**
 * Render provider toggle panel
 */
function renderProviderTogglePanel() {
    return `
        <div style="display: flex; flex-direction: column; gap: 0.75rem;">
            ${nftMintStudioState.providerStates.map(provider => {
                const isRegistered = provider.state !== 'idle';
                const isActive = provider.state === 'active';
                const isLoading = nftMintStudioState.providerLoading.includes(provider.id);

                return `
                    <div class="portal-card" style="display: flex; align-items: center; justify-content: space-between; padding: 1rem;">
                        <div>
                            <p style="font-size: 0.9375rem; font-weight: 500; color: var(--text-primary); margin: 0; margin-bottom: 0.25rem;">${provider.label}</p>
                            <p style="font-size: 0.8125rem; color: var(--text-secondary); margin: 0;">${provider.description}</p>
                        </div>
                        <div style="display: flex; align-items: center; gap: 0.5rem;">
                            <button
                                type="button"
                                class="provider-register-btn"
                                data-provider="${provider.id}"
                                onclick="handleProviderRegister('${provider.id}')"
                                ${isRegistered || isLoading ? 'disabled' : ''}
                                style="
                                    padding: 0.5rem 1rem;
                                    border-radius: 0.5rem;
                                    font-size: 0.875rem;
                                    border: 1px solid ${isRegistered ? 'var(--text-primary)' : 'var(--border-color)'};
                                    background: ${isRegistered ? 'var(--text-primary)' : 'rgba(255, 255, 255, 0.1)'};
                                    color: ${isRegistered ? 'var(--bg-primary)' : 'var(--text-primary)'};
                                    cursor: ${isRegistered || isLoading ? 'not-allowed' : 'pointer'};
                                    opacity: ${isLoading ? 0.5 : 1};
                                    font-family: inherit;
                                    transition: all 0.2s;
                                "
                            >
                                ${isLoading ? 'Processing' : isRegistered ? 'Registered' : 'Register'}
                            </button>
                            <button
                                type="button"
                                class="provider-activate-btn"
                                data-provider="${provider.id}"
                                onclick="handleProviderActivate('${provider.id}')"
                                ${!isRegistered || isLoading ? 'disabled' : ''}
                                style="
                                    padding: 0.5rem 1rem;
                                    border-radius: 0.5rem;
                                    font-size: 0.875rem;
                                    border: 1px solid ${isActive ? 'rgba(34, 197, 94, 0.6)' : 'var(--border-color)'};
                                    background: ${isActive ? 'rgba(34, 197, 94, 0.2)' : 'rgba(255, 255, 255, 0.1)'};
                                    color: ${isActive ? 'rgba(34, 197, 94, 1)' : 'var(--text-primary)'};
                                    cursor: ${!isRegistered || isLoading ? 'not-allowed' : 'pointer'};
                                    opacity: ${isLoading ? 0.5 : 1};
                                    font-family: inherit;
                                    transition: all 0.2s;
                                "
                            >
                                ${isLoading ? 'Processing' : isActive ? 'Active' : 'Activate'}
                            </button>
                        </div>
                    </div>
                `;
            }).join('')}
        </div>
    `;
}

/**
 * Handle provider register
 */
async function handleProviderRegister(providerId) {
    const provider = nftMintStudioState.providerStates.find(p => p.id === providerId);
    if (!provider || provider.state !== 'idle' || !nftMintStudioState.authToken) {
        if (!nftMintStudioState.authToken) {
            alert('You must authenticate first');
        }
        return;
    }

    nftMintStudioState.providerLoading.push(providerId);
    updateWizardStep();

    try {
        const response = await oasisAPI.request(provider.registerEndpoint, { method: 'POST' });
        if (!response.isError) {
            provider.state = 'registered';
            updateWizardStep();
        } else {
            throw new Error(response.message || 'Register provider failed');
        }
    } catch (error) {
        alert(`${provider.label} register failed: ${error.message}`);
    } finally {
        nftMintStudioState.providerLoading = nftMintStudioState.providerLoading.filter(id => id !== providerId);
        updateWizardStep();
    }
}

/**
 * Handle provider activate
 */
async function handleProviderActivate(providerId) {
    const provider = nftMintStudioState.providerStates.find(p => p.id === providerId);
    if (!provider || provider.state !== 'registered' || !nftMintStudioState.authToken) {
        return;
    }

    nftMintStudioState.providerLoading.push(providerId);
    updateWizardStep();

    try {
        const response = await oasisAPI.request(provider.activateEndpoint, { method: 'POST' });
        if (!response.isError) {
            provider.state = 'active';
            updateWizardStep();
        } else {
            throw new Error(response.message || 'Activate provider failed');
        }
    } catch (error) {
        alert(`${provider.label} activate failed: ${error.message}`);
    } finally {
        nftMintStudioState.providerLoading = nftMintStudioState.providerLoading.filter(id => id !== providerId);
        updateWizardStep();
    }
}

/**
 * Render Assets step (simplified - full implementation would include file upload)
 */
function renderAssetsStep() {
    return `
        <div style="display: flex; flex-direction: column; gap: 1.5rem;">
            <div>
                <h3 style="font-size: 1.25rem; font-weight: 500; color: var(--text-primary); margin-bottom: 0.5rem;">Assets & Metadata</h3>
                <p style="font-size: 0.875rem; color: var(--text-secondary);">Upload artwork, thumbnails, and JSON metadata.</p>
            </div>
            <div class="portal-card">
                <p style="color: var(--text-secondary); font-size: 0.875rem; margin-bottom: 1rem;">
                    Asset upload functionality will be implemented here. For now, you can manually enter URLs.
                </p>
                <div style="display: flex; flex-direction: column; gap: 1rem;">
                    <div>
                        <label style="display: block; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; color: var(--text-tertiary); margin-bottom: 0.5rem;">Image URL</label>
                        <input
                            type="url"
                            id="nft-image-url"
                            value="${nftMintStudioState.assetDraft.imageUrl}"
                            onchange="nftMintStudioState.assetDraft.imageUrl = this.value"
                            placeholder="https://..."
                            style="width: 100%; padding: 0.75rem; border-radius: 0.5rem; border: 1px solid var(--border-color); background: rgba(0, 0, 0, 0.3); color: var(--text-primary); font-family: inherit; font-size: 0.875rem;"
                        />
                    </div>
                    <div>
                        <label style="display: block; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; color: var(--text-tertiary); margin-bottom: 0.5rem;">JSON Metadata URL</label>
                        <input
                            type="url"
                            id="nft-json-url"
                            value="${nftMintStudioState.assetDraft.jsonUrl}"
                            onchange="nftMintStudioState.assetDraft.jsonUrl = this.value"
                            placeholder="https://..."
                            style="width: 100%; padding: 0.75rem; border-radius: 0.5rem; border: 1px solid var(--border-color); background: rgba(0, 0, 0, 0.3); color: var(--text-primary); font-family: inherit; font-size: 0.875rem;"
                        />
                    </div>
                    <div>
                        <label style="display: block; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; color: var(--text-tertiary); margin-bottom: 0.5rem;">Send To Address</label>
                        <input
                            type="text"
                            id="nft-send-address"
                            value="${nftMintStudioState.assetDraft.sendToAddress}"
                            onchange="nftMintStudioState.assetDraft.sendToAddress = this.value"
                            placeholder="Solana address"
                            style="width: 100%; padding: 0.75rem; border-radius: 0.5rem; border: 1px solid var(--border-color); background: rgba(0, 0, 0, 0.3); color: var(--text-primary); font-family: monospace; font-size: 0.875rem;"
                        />
                    </div>
                </div>
            </div>
        </div>
    `;
}

/**
 * Render x402 Revenue step
 */
function renderX402Step() {
    return `
        <div style="display: flex; flex-direction: column; gap: 1.5rem;">
            <div>
                <h3 style="font-size: 1.25rem; font-weight: 500; color: var(--text-primary); margin-bottom: 0.5rem;">x402 Revenue Sharing</h3>
                <p style="font-size: 0.875rem; color: var(--text-secondary);">Enable automatic payment distribution to NFT holders.</p>
            </div>
            <div class="portal-card">
                <label style="display: flex; align-items: center; gap: 1rem; cursor: pointer; margin-bottom: ${nftMintStudioState.x402Config.enabled ? '1rem' : '0'};">
                    <input
                        type="checkbox"
                        ${nftMintStudioState.x402Config.enabled ? 'checked' : ''}
                        onchange="nftMintStudioState.x402Config.enabled = this.checked; updateWizardStep();"
                        style="width: 20px; height: 20px; cursor: pointer;"
                    />
                    <span style="font-weight: 500; color: var(--text-primary);">Enable x402 Revenue Sharing</span>
                </label>
                ${nftMintStudioState.x402Config.enabled ? `
                    <div style="padding-top: 1rem; border-top: 1px solid var(--border-color);">
                        <div>
                            <label style="display: block; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; color: var(--text-tertiary); margin-bottom: 0.5rem;">Payment Endpoint URL</label>
                            <input
                                type="url"
                                value="${nftMintStudioState.x402Config.paymentEndpoint}"
                                onchange="nftMintStudioState.x402Config.paymentEndpoint = this.value"
                                placeholder="https://api.yourservice.com/x402/revenue"
                                style="width: 100%; padding: 0.75rem; border-radius: 0.5rem; border: 1px solid var(--border-color); background: rgba(0, 0, 0, 0.3); color: var(--text-primary); font-family: inherit; font-size: 0.875rem;"
                            />
                        </div>
                    </div>
                ` : ''}
            </div>
        </div>
    `;
}

/**
 * Render Mint step
 */
function renderMintStep() {
    const canMint = nftMintStudioState.assetDraft.imageUrl && 
                    nftMintStudioState.assetDraft.jsonUrl && 
                    nftMintStudioState.assetDraft.sendToAddress &&
                    nftMintStudioState.providerStates.every(p => p.state === 'active');

    return `
        <div style="display: flex; flex-direction: column; gap: 2rem;">
            <div class="portal-card">
                <h3 style="font-size: 1.25rem; font-weight: 500; color: var(--text-primary); margin-bottom: 0.5rem;">Mint Configuration</h3>
                <p style="font-size: 0.875rem; color: var(--text-secondary); margin-bottom: 1rem;">
                    Review the payload and submit to mint your NFT.
                </p>
                <div style="padding: 1rem; background: rgba(0, 0, 0, 0.4); border-radius: 0.5rem; margin-bottom: 1rem; border: 1px solid var(--border-color);">
                    <pre style="color: var(--text-secondary); font-size: 0.75rem; margin: 0; overflow-x: auto; font-family: 'Monaco', 'Courier New', monospace;">${JSON.stringify({
                        Title: nftMintStudioState.assetDraft.title,
                        Description: nftMintStudioState.assetDraft.description,
                        Symbol: nftMintStudioState.assetDraft.symbol,
                        ImageUrl: nftMintStudioState.assetDraft.imageUrl,
                        JSONMetaDataURL: nftMintStudioState.assetDraft.jsonUrl,
                        SendToAddressAfterMinting: nftMintStudioState.assetDraft.sendToAddress
                    }, null, 2)}</pre>
                </div>
                <button
                    type="button"
                    id="nft-mint-btn"
                    onclick="handleNFTMint()"
                    ${!canMint ? 'disabled' : ''}
                    class="btn-primary"
                    style="opacity: ${canMint ? 1 : 0.5}; cursor: ${canMint ? 'pointer' : 'not-allowed'};"
                >
                    Mint NFT
                </button>
                <div id="nft-mint-message" style="margin-top: 1rem; font-size: 0.875rem;"></div>
            </div>
        </div>
    `;
}

/**
 * Handle NFT mint
 */
async function handleNFTMint() {
    const btn = document.getElementById('nft-mint-btn');
    const messageEl = document.getElementById('nft-mint-message');

    if (btn) {
        btn.disabled = true;
        btn.textContent = 'Minting...';
    }

    try {
        const payload = {
            Title: nftMintStudioState.assetDraft.title,
            Description: nftMintStudioState.assetDraft.description,
            Symbol: nftMintStudioState.assetDraft.symbol,
            OnChainProvider: { value: 3, name: 'SolanaOASIS' },
            OffChainProvider: { value: 23, name: 'MongoDBOASIS' },
            NFTOffChainMetaType: { value: 3, name: 'ExternalJsonURL' },
            NFTStandardType: { value: 2, name: 'SPL' },
            JSONMetaDataURL: nftMintStudioState.assetDraft.jsonUrl,
            ImageUrl: nftMintStudioState.assetDraft.imageUrl,
            ThumbnailUrl: nftMintStudioState.assetDraft.thumbnailUrl || nftMintStudioState.assetDraft.imageUrl,
            Price: 0,
            NumberToMint: 1,
            StoreNFTMetaDataOnChain: false,
            MintedByAvatarId: nftMintStudioState.avatarId || '89d907a8-5859-4171-b6c5-621bfe96930d',
            SendToAddressAfterMinting: nftMintStudioState.assetDraft.sendToAddress,
            WaitTillNFTSent: true,
            WaitForNFTToSendInSeconds: 60,
            AttemptToSendEveryXSeconds: 5
        };

        if (nftMintStudioState.x402Config.enabled) {
            payload.x402Config = nftMintStudioState.x402Config;
        }

        const response = await oasisAPI.request('/api/nft/mint-nft', {
            method: 'POST',
            body: JSON.stringify(payload)
        });

        if (!response.isError && response.result) {
            nftMintStudioState.mintReady = true;
            if (btn) {
                btn.textContent = '✓ Minted Successfully';
                btn.style.background = 'rgba(34, 197, 94, 0.2)';
                btn.style.color = 'rgba(34, 197, 94, 1)';
                btn.style.border = '1px solid rgba(34, 197, 94, 0.6)';
            }
            if (messageEl) {
                messageEl.style.color = 'rgba(34, 197, 94, 1)';
                messageEl.textContent = `NFT minted successfully! Transaction: ${JSON.stringify(response.result)}`;
            }
        } else {
            throw new Error(response.message || 'Mint failed');
        }
    } catch (error) {
        console.error('Mint error:', error);
        if (btn) {
            btn.disabled = false;
            btn.textContent = 'Mint NFT';
        }
        if (messageEl) {
            messageEl.style.color = 'rgba(239, 68, 68, 1)';
            messageEl.textContent = error.message || 'Minting failed';
        }
    }
}

/**
 * Render wizard footer with navigation
 */
function renderWizardFooter() {
    const activeStepIndex = WIZARD_STEPS.findIndex(s => s.id === nftMintStudioState.activeStep);
    const canProceed = getCanProceed();
    const canGoBack = activeStepIndex > 0;
    const canGoNext = activeStepIndex < WIZARD_STEPS.length - 1 && canProceed;

    return `
        <div style="display: flex; flex-direction: column; gap: 0.75rem;">
            <div style="font-size: 0.6875rem; color: var(--text-tertiary);">
                Need help? Follow the checklist above.
            </div>
            <div style="display: flex; gap: 0.75rem; justify-content: flex-end;">
                <button
                    type="button"
                    onclick="navigateWizardStep(-1)"
                    ${!canGoBack ? 'disabled' : ''}
                    class="btn-text"
                    style="border: 1px solid var(--border-color); padding: 0.5rem 1rem; border-radius: 0.5rem; opacity: ${canGoBack ? 1 : 0.5}; cursor: ${canGoBack ? 'pointer' : 'not-allowed'};"
                >
                    Previous
                </button>
                <button
                    type="button"
                    onclick="navigateWizardStep(1)"
                    ${!canGoNext ? 'disabled' : ''}
                    class="btn-primary"
                    style="opacity: ${canGoNext ? 1 : 0.5}; cursor: ${canGoNext ? 'pointer' : 'not-allowed'};"
                >
                    Next
                </button>
            </div>
        </div>
    `;
}

/**
 * Check if can proceed to next step
 */
function getCanProceed() {
    switch (nftMintStudioState.activeStep) {
        case 'solana-config':
            return true;
        case 'auth':
            return !!nftMintStudioState.authToken && nftMintStudioState.providerStates.every(p => p.state === 'active');
        case 'assets':
            return !!(nftMintStudioState.assetDraft.imageUrl && nftMintStudioState.assetDraft.jsonUrl && nftMintStudioState.assetDraft.sendToAddress);
        case 'x402-revenue':
            return true; // Optional
        default:
            return false;
    }
}

/**
 * Navigate wizard step
 */
function navigateWizardStep(direction) {
    const currentIndex = WIZARD_STEPS.findIndex(s => s.id === nftMintStudioState.activeStep);
    const newIndex = currentIndex + direction;
    
    if (newIndex >= 0 && newIndex < WIZARD_STEPS.length) {
        if (direction > 0 && !getCanProceed()) {
            return; // Can't proceed if requirements not met
        }
        nftMintStudioState.activeStep = WIZARD_STEPS[newIndex].id;
        updateWizardStep();
    }
}

/**
 * Update wizard step (re-render current step and summary)
 */
function updateWizardStep() {
    const container = document.getElementById('nft-mint-studio-content');
    if (!container) return;

    const stepContent = container.querySelector('#wizard-step-content');
    const summarySection = container.querySelectorAll('.portal-section')[1];
    const wizardSection = container.querySelectorAll('.portal-section')[2];

    if (stepContent) {
        stepContent.innerHTML = renderCurrentStep();
    }
    if (summarySection) {
        summarySection.innerHTML = renderSessionSummary();
    }
    if (wizardSection) {
        // Re-render the entire wizard to update step highlighting
        wizardSection.innerHTML = renderWizardShell();
    }

    attachWizardListeners();
}

/**
 * Attach wizard event listeners
 */
function attachWizardListeners() {
    // Step navigation buttons
    document.querySelectorAll('.wizard-step-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            const stepId = e.currentTarget.getAttribute('data-step');
            const stepIndex = WIZARD_STEPS.findIndex(s => s.id === stepId);
            const currentIndex = WIZARD_STEPS.findIndex(s => s.id === nftMintStudioState.activeStep);
            
            // Only allow clicking past steps or current step
            if (stepIndex <= currentIndex || getCanProceed()) {
                nftMintStudioState.activeStep = stepId;
                updateWizardStep();
            }
        });
    });

    // Config preset buttons
    document.querySelectorAll('.config-preset-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            const preset = e.currentTarget.getAttribute('data-preset');
            nftMintStudioState.configPreset = preset;
            updateWizardStep();
        });
    });
}

/**
 * Render loading state
 */
function renderLoadingState() {
    return `
        <div class="portal-section">
            <div class="empty-state">
                <div class="loading-spinner"></div>
                <p class="empty-state-text" style="margin-top: 1rem;">Loading NFT Mint Studio...</p>
            </div>
        </div>
    `;
}

/**
 * Render error state
 */
function renderErrorState(message) {
    return `
        <div class="portal-section">
            <div class="empty-state">
                <div style="font-size: 3rem; margin-bottom: 1rem; opacity: 0.5;">⚠️</div>
                <h3 style="color: var(--text-primary); margin-bottom: 0.5rem; font-weight: 500;">Error Loading NFT Mint Studio</h3>
                <p class="empty-state-text">${message || 'Unknown error occurred'}</p>
                <button class="btn-primary" onclick="loadNFTMintStudio()" style="margin-top: 1.5rem;">Retry</button>
            </div>
        </div>
    `;
}

// Export functions to window for global access
if (typeof window !== 'undefined') {
    window.loadNFTMintStudio = loadNFTMintStudio;
    window.handleNFTAuthenticate = handleNFTAuthenticate;
    window.handleProviderRegister = handleProviderRegister;
    window.handleProviderActivate = handleProviderActivate;
    window.handleNFTMint = handleNFTMint;
    window.navigateWizardStep = navigateWizardStep;
    window.updateWizardStep = updateWizardStep;
    window.nftMintStudioState = nftMintStudioState;
    
    console.log('NFT Mint Studio integration loaded (vanilla JS version)');
}
