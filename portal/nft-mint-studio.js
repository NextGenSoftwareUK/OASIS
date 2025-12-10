/**
 * NFT Mint Studio - Vanilla JavaScript Implementation
 * Direct integration into OASIS Portal (converted from React)
 */

// Supported Blockchain Chains for NFT Minting
// Logo paths match the logos/ directory (as used in oracle.js and bridge.js)
const SUPPORTED_CHAINS = [
    {
        id: 'solana',
        name: 'Solana',
        logo: 'logos/solana.svg',
        standard: 'SPL',
        wallet: 'Phantom',
        provider: 'SolanaOASIS',
        gasEstimate: '$0.01-0.05',
        status: 'available',
        description: 'High-performance blockchain with low fees',
        configOptions: ['Metaplex Standard', 'Collection with Verified Creator', 'Editioned Series', 'Compressed NFT (Bubblegum)'],
        providerMapping: {
            onChain: { value: 3, name: 'SolanaOASIS' },
            offChain: { value: 23, name: 'MongoDBOASIS' },
            nftOffChainMetaType: { value: 3, name: 'ExternalJsonURL' },
            nftStandardType: { value: 2, name: 'SPL' }
        }
    },
    {
        id: 'ethereum',
        name: 'Ethereum',
        logo: 'logos/ethereum.svg',
        standard: 'ERC-721',
        wallet: 'MetaMask',
        provider: 'EthereumOASIS',
        gasEstimate: '$5-50',
        status: 'available',
        description: 'The original smart contract platform',
        configOptions: ['ERC-721 Standard', 'ERC-1155', 'Custom Contract'],
        providerMapping: {
            onChain: { value: 1, name: 'EthereumOASIS' },
            offChain: { value: 23, name: 'MongoDBOASIS' },
            nftOffChainMetaType: { value: 3, name: 'ExternalJsonURL' },
            nftStandardType: { value: 1, name: 'ERC721' }
        }
    },
    {
        id: 'polygon',
        name: 'Polygon',
        logo: 'logos/polygon.svg',
        standard: 'ERC-721',
        wallet: 'MetaMask',
        provider: 'PolygonOASIS',
        gasEstimate: '$0.01-0.10',
        status: 'available',
        description: 'Ethereum scaling solution with low fees',
        configOptions: ['ERC-721 Standard', 'ERC-1155'],
        providerMapping: {
            onChain: { value: 6, name: 'PolygonOASIS' },
            offChain: { value: 23, name: 'MongoDBOASIS' },
            nftOffChainMetaType: { value: 3, name: 'ExternalJsonURL' },
            nftStandardType: { value: 1, name: 'ERC721' }
        }
    },
    {
        id: 'arbitrum',
        name: 'Arbitrum',
        logo: 'logos/arbitrum.png',
        standard: 'ERC-721',
        wallet: 'MetaMask',
        provider: 'ArbitrumOASIS',
        gasEstimate: '$0.50-5',
        status: 'available',
        description: 'Ethereum Layer 2 with faster transactions',
        configOptions: ['ERC-721 Standard', 'ERC-1155'],
        providerMapping: {
            onChain: { value: 2, name: 'ArbitrumOASIS' },
            offChain: { value: 23, name: 'MongoDBOASIS' },
            nftOffChainMetaType: { value: 3, name: 'ExternalJsonURL' },
            nftStandardType: { value: 1, name: 'ERC721' }
        }
    },
    {
        id: 'base',
        name: 'Base',
        logo: 'logos/base.png',
        standard: 'ERC-721',
        wallet: 'MetaMask',
        provider: 'BaseOASIS',
        gasEstimate: '$0.10-2',
        status: 'available',
        description: 'Coinbase Layer 2 built on Optimism',
        configOptions: ['ERC-721 Standard', 'ERC-1155'],
        providerMapping: {
            onChain: { value: 5, name: 'BaseOASIS' },
            offChain: { value: 23, name: 'MongoDBOASIS' },
            nftOffChainMetaType: { value: 3, name: 'ExternalJsonURL' },
            nftStandardType: { value: 1, name: 'ERC721' }
        }
    },
    {
        id: 'optimism',
        name: 'Optimism',
        logo: 'logos/optimism-ethereum-op-logo.png',
        standard: 'ERC-721',
        wallet: 'MetaMask',
        provider: 'OptimismOASIS',
        gasEstimate: '$0.50-3',
        status: 'available',
        description: 'Ethereum Layer 2 with low fees',
        configOptions: ['ERC-721 Standard', 'ERC-1155'],
        providerMapping: {
            onChain: { value: 4, name: 'OptimismOASIS' },
            offChain: { value: 23, name: 'MongoDBOASIS' },
            nftOffChainMetaType: { value: 3, name: 'ExternalJsonURL' },
            nftStandardType: { value: 1, name: 'ERC721' }
        }
    },
    {
        id: 'avalanche',
        name: 'Avalanche',
        logo: 'logos/avalanche.svg',
        standard: 'ERC-721',
        wallet: 'MetaMask',
        provider: 'AvalancheOASIS',
        gasEstimate: '$0.20-1',
        status: 'available',
        description: 'High-performance blockchain platform',
        configOptions: ['ERC-721 Standard', 'ERC-1155'],
        providerMapping: {
            onChain: { value: 7, name: 'AvalancheOASIS' },
            offChain: { value: 23, name: 'MongoDBOASIS' },
            nftOffChainMetaType: { value: 3, name: 'ExternalJsonURL' },
            nftStandardType: { value: 1, name: 'ERC721' }
        }
    },
    {
        id: 'bnb-chain',
        name: 'BNB Chain',
        logo: 'logos/bnb.svg',
        standard: 'ERC-721',
        wallet: 'MetaMask',
        provider: 'BNBChainOASIS',
        gasEstimate: '$0.05-0.50',
        status: 'available',
        description: 'Binance Smart Chain - Low fee EVM chain',
        configOptions: ['ERC-721 Standard', 'ERC-1155'],
        providerMapping: {
            onChain: { value: 8, name: 'BNBChainOASIS' },
            offChain: { value: 23, name: 'MongoDBOASIS' },
            nftOffChainMetaType: { value: 3, name: 'ExternalJsonURL' },
            nftStandardType: { value: 1, name: 'ERC721' }
        }
    },
    {
        id: 'fantom',
        name: 'Fantom',
        logo: 'logos/fantom.svg',
        standard: 'ERC-721',
        wallet: 'MetaMask',
        provider: 'FantomOASIS',
        gasEstimate: '$0.01-0.20',
        status: 'available',
        description: 'Fast and scalable EVM-compatible chain',
        configOptions: ['ERC-721 Standard', 'ERC-1155'],
        providerMapping: {
            onChain: { value: 9, name: 'FantomOASIS' },
            offChain: { value: 23, name: 'MongoDBOASIS' },
            nftOffChainMetaType: { value: 3, name: 'ExternalJsonURL' },
            nftStandardType: { value: 1, name: 'ERC721' }
        }
    }
];

// NFT Mint Studio State
const nftMintStudioState = {
    loading: false,
    activeStep: 'chain-selection', // Start with chain selection
    selectedChain: null, // Selected chain object
    configPreset: null, // Chain-specific preset
    authToken: null,
    avatarId: null,
    providerStates: [], // Will be populated based on selected chain
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

// All possible wizard steps (always show all, but disable future ones)
const ALL_WIZARD_STEPS = [
    { id: 'chain-selection', title: 'Select Chain', description: 'Choose the blockchain to mint your NFT on.' },
    { id: 'auth', title: 'Authenticate & Providers', description: 'Login and activate providers for minting.' },
    { id: 'assets', title: 'Assets & Metadata', description: 'Upload artwork, thumbnails, and JSON metadata.' },
    { id: 'x402-revenue', title: 'x402 Revenue Sharing', description: 'Enable automatic payment distribution to NFT holders.' },
    { id: 'mint', title: 'Review & Mint', description: 'Generate payload and mint your NFT.' }
];

// Dynamic wizard steps - returns all steps, with chain-specific titles when chain is selected
function getWizardSteps(selectedChain) {
    if (!selectedChain) {
        // No chain selected - only show chain selection step as enabled
        return ALL_WIZARD_STEPS.map((step, index) => ({
            ...step,
            enabled: index === 0 // Only first step enabled
        }));
    }

    // Chain selected - customize step titles and all steps are available
    return [
        { ...ALL_WIZARD_STEPS[0], enabled: true },
        { ...ALL_WIZARD_STEPS[1], title: 'Authenticate & Providers', description: `Login and activate ${selectedChain.provider} + MongoDBOASIS.`, enabled: true },
        { ...ALL_WIZARD_STEPS[2], enabled: true },
        { ...ALL_WIZARD_STEPS[3], enabled: true },
        { ...ALL_WIZARD_STEPS[4], title: 'Review & Mint', description: `Generate payload and mint your NFT on ${selectedChain.name}.`, enabled: true }
    ];
}

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
    // Don't show summary on chain selection step
    if (nftMintStudioState.activeStep === 'chain-selection') {
        return '';
    }

    if (!nftMintStudioState.selectedChain) {
        return '';
    }

    const chain = nftMintStudioState.selectedChain;
    const providerActive = nftMintStudioState.providerStates.every(p => p.state === 'active');
    const statusBadge = providerActive && nftMintStudioState.mintReady 
        ? '<span style="border: 1px solid rgba(34, 197, 94, 0.6); background: rgba(20, 118, 96, 0.25); color: rgba(34, 197, 94, 1); padding: 0.25rem 0.75rem; border-radius: 999px; font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.1em;">Ready To Mint</span>'
        : '<span style="border: 1px solid rgba(239, 68, 68, 0.6); background: rgba(120, 35, 50, 0.2); color: rgba(239, 68, 68, 1); padding: 0.25rem 0.75rem; border-radius: 999px; font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.1em;">Pending Configuration</span>';

    return `
        <div class="portal-card" style="display: flex; flex-wrap: wrap; align-items: center; gap: 1rem; font-size: 0.6875rem;">
            <span style="font-size: 0.5625rem; text-transform: uppercase; letter-spacing: 0.16em; color: var(--text-tertiary);">Session Summary</span>
            <div style="display: flex; align-items: center; gap: 0.75rem;">
                <span style="color: var(--text-primary); font-size: 0.75rem; font-weight: 500;">Chain</span>
                <span style="color: var(--text-secondary);">${chain.name}</span>
            </div>
            <div style="display: flex; align-items: center; gap: 0.75rem;">
                <span style="color: var(--text-primary); font-size: 0.75rem; font-weight: 500;">Standard</span>
                <span style="color: var(--text-secondary);">${chain.standard}</span>
            </div>
            <div style="display: flex; align-items: center; gap: 0.75rem;">
                <span style="color: var(--text-primary); font-size: 0.75rem; font-weight: 500;">Config</span>
                <span style="color: var(--text-secondary);">${nftMintStudioState.configPreset || 'Not selected'}</span>
            </div>
            <div style="display: flex; align-items: center; gap: 0.75rem;">
                <span style="color: var(--text-primary); font-size: 0.75rem; font-weight: 500;">Provider</span>
                <span style="color: var(--text-secondary);">${chain.provider}</span>
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
    const steps = getWizardSteps(nftMintStudioState.selectedChain);
    const activeStepIndex = steps.findIndex(s => s.id === nftMintStudioState.activeStep);
    const flowTitle = nftMintStudioState.selectedChain 
        ? `${nftMintStudioState.selectedChain.name} Mint Flow`
        : 'NFT Mint Flow';
    const flowDescription = nftMintStudioState.selectedChain
        ? `Configure ${nftMintStudioState.selectedChain.provider} + MongoDBOASIS, upload your assets, and mint.`
        : 'Select a blockchain to begin minting your NFT.';
    
    return `
        <div class="portal-card" style="position: relative; overflow: hidden; padding: 2rem;">
            <div style="position: absolute; inset: 0; background: radial-gradient(circle at top left, rgba(255, 255, 255, 0.05), transparent 70%); pointer-events: none;"></div>
            <div style="display: grid; grid-template-columns: 280px 1fr; gap: 2.5rem; position: relative;">
                <aside style="display: flex; flex-direction: column; gap: 1.5rem;">
                    <div>
                        <h2 style="font-size: 1.125rem; font-weight: 500; color: var(--text-primary); margin-bottom: 0.5rem;">${flowTitle}</h2>
                        <p style="font-size: 0.875rem; color: var(--text-secondary); line-height: 1.6;">
                            ${flowDescription}
                        </p>
                    </div>
                    <ol style="display: flex; flex-direction: column; gap: 0.75rem; list-style: none; padding: 0; margin: 0;">
                        ${steps.map((step, index) => {
                            const isActive = step.id === nftMintStudioState.activeStep;
                            const isPast = activeStepIndex > index;
                            const isEnabled = step.enabled !== false; // Default to enabled if not specified
                            const canClick = (isPast || isActive) && isEnabled;
                            return `
                                <li>
                                    <button 
                                        type="button"
                                        class="wizard-step-btn"
                                        data-step="${step.id}"
                                        ${!canClick ? 'disabled' : ''}
                                        style="
                                            width: 100%;
                                            display: flex;
                                            align-items: center;
                                            gap: 0.75rem;
                                            padding: 0.75rem 1rem;
                                            border-radius: 0.5rem;
                                            border: 1px solid ${isActive ? 'rgba(255, 255, 255, 0.2)' : 'transparent'};
                                            background: ${isActive ? 'rgba(255, 255, 255, 0.04)' : 'rgba(255, 255, 255, 0.02)'};
                                            color: ${isActive ? 'var(--text-primary)' : canClick ? 'var(--text-secondary)' : 'var(--text-tertiary)'};
                                            text-align: left;
                                            cursor: ${canClick ? 'pointer' : 'not-allowed'};
                                            opacity: ${canClick ? '1' : '0.4'};
                                            transition: all 0.2s;
                                            font-family: inherit;
                                        "
                                        ${canClick ? `
                                        onmouseover="this.style.borderColor='rgba(255, 255, 255, 0.2)'; this.style.background='rgba(255, 255, 255, 0.04)'; this.style.color='var(--text-primary)';"
                                        onmouseout="
                                            const isActive = '${step.id}' === '${nftMintStudioState.activeStep}';
                                            this.style.borderColor = isActive ? 'rgba(255, 255, 255, 0.2)' : 'transparent';
                                            this.style.background = isActive ? 'rgba(255, 255, 255, 0.04)' : 'rgba(255, 255, 255, 0.02)';
                                            this.style.color = isActive ? 'var(--text-primary)' : 'var(--text-secondary)';
                                        "
                                        ` : ''}
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
                                            background: ${isActive ? 'var(--text-primary)' : isPast && isEnabled ? 'rgba(255, 255, 255, 0.2)' : 'rgba(255, 255, 255, 0.1)'};
                                            color: ${isActive ? 'var(--bg-primary)' : canClick ? 'var(--text-secondary)' : 'var(--text-tertiary)'};
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
        case 'chain-selection':
            return renderChainSelectionStep();
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
 * Render Chain Selection Step - styled like Universal Asset Bridge
 */
function renderChainSelectionStep() {
    return `
        <div style="display: flex; flex-direction: column; gap: 2rem;">
            <div>
                <h3 style="font-size: 1.5rem; font-weight: 500; color: var(--text-primary); margin-bottom: 0.5rem;">Select Your Blockchain</h3>
                <p style="font-size: 0.875rem; color: var(--text-secondary);">
                    Choose the blockchain where you want to mint your NFT. Each chain has different fees, speeds, and NFT standards.
                </p>
            </div>

            <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 1.25rem;">
                ${SUPPORTED_CHAINS.map(chain => {
                    const isSelected = nftMintStudioState.selectedChain?.id === chain.id;
                    return `
                        <button
                            type="button"
                            class="chain-selection-card trading-template-card"
                            data-chain="${chain.id}"
                            style="
                                position: relative;
                                flex-direction: column;
                                align-items: center;
                                text-align: center;
                                border: 1px solid ${isSelected ? 'rgba(255, 255, 255, 0.3)' : 'rgba(255, 255, 255, 0.1)'};
                            "
                            onclick="selectChain('${chain.id}')"
                        >
                            <div class="trading-template-icon" style="
                                width: 40px;
                                height: 40px;
                                background: rgba(255, 255, 255, 0.05);
                                border-radius: 0.5rem;
                                display: flex;
                                align-items: center;
                                justify-content: center;
                                flex-shrink: 0;
                            ">
                                <img 
                                    src="${chain.logo}" 
                                    alt="${chain.name}"
                                    style="
                                        width: 32px; 
                                        height: 32px; 
                                        object-fit: contain;
                                        background: transparent !important;
                                        background-color: transparent !important;
                                        filter: none;
                                        padding: 0;
                                        margin: 0;
                                    "
                                    onerror="this.style.display='none'; this.nextElementSibling.style.display='flex';"
                                />
                                <span style="display: none; font-size: 1.5rem; align-items: center; justify-content: center; color: rgba(255, 255, 255, 0.9);">${getChainEmoji(chain)}</span>
                            </div>
                            ${isSelected ? `
                                <div style="
                                    position: absolute;
                                    top: 0.5rem;
                                    right: 0.5rem;
                                    width: 20px;
                                    height: 20px;
                                    border-radius: 50%;
                                    background: var(--accent-color);
                                    display: flex;
                                    align-items: center;
                                    justify-content: center;
                                    color: #041321;
                                    font-size: 0.75rem;
                                    font-weight: 600;
                                ">✓</div>
                            ` : ''}
                        </button>
                    `;
                }).join('')}
            </div>
        </div>
    `;
}

/**
 * Get chain emoji fallback
 */
function getChainEmoji(chain) {
    const icons = {
        'solana': '◎',
        'ethereum': 'Ξ',
        'polygon': '⬟',
        'arbitrum': '⟠',
        'base': 'BASE'
    };
    return icons[chain.id] || chain.name.charAt(0);
}

/**
 * Select a chain and initialize its flow
 */
function selectChain(chainId) {
    const chain = SUPPORTED_CHAINS.find(c => c.id === chainId);
    if (!chain) {
        console.error('Chain not found:', chainId);
        return;
    }

    nftMintStudioState.selectedChain = chain;
    nftMintStudioState.configPreset = chain.configOptions[0] || null;

    // Initialize provider states for selected chain
    nftMintStudioState.providerStates = [
        {
            id: chain.id,
            label: chain.provider,
            description: `Handles on-chain mint + transfer on ${chain.name}`,
            registerEndpoint: `/api/provider/register-provider-type/${chain.provider}`,
            activateEndpoint: `/api/provider/activate-provider/${chain.provider}`,
            state: 'idle'
        },
        {
            id: 'mongo',
            label: 'MongoDBOASIS',
            description: 'Stores off-chain metadata JSON for NFTs',
            registerEndpoint: '/api/provider/register-provider-type/MongoDBOASIS',
            activateEndpoint: '/api/provider/activate-provider/MongoDBOASIS',
            state: 'idle'
        }
    ];

    // Move to auth step (chain-config removed)
    nftMintStudioState.activeStep = 'auth';
    updateWizardStep();
}


/**
 * Render Authentication step
 */
function renderAuthStep() {
    return `
        <div style="display: flex; flex-direction: column; gap: 2rem;">
            <div>
                <h3 style="font-size: 1.25rem; font-weight: 500; color: var(--text-primary); margin-bottom: 0.5rem;">Register & Activate Providers</h3>
                <p style="font-size: 0.875rem; color: var(--text-secondary); margin-bottom: 1rem;">
                    Flip the toggles to enable ${nftMintStudioState.selectedChain ? nftMintStudioState.selectedChain.provider : 'chain'} and MongoDBOASIS. Both must show Active before minting.
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
        // Get chain-specific provider mapping
        const chain = nftMintStudioState.selectedChain;
        if (!chain || !chain.providerMapping) {
            throw new Error('Chain not selected or invalid');
        }

        const payload = {
            Title: nftMintStudioState.assetDraft.title,
            Description: nftMintStudioState.assetDraft.description,
            Symbol: nftMintStudioState.assetDraft.symbol,
            OnChainProvider: chain.providerMapping.onChain,
            OffChainProvider: chain.providerMapping.offChain,
            NFTOffChainMetaType: chain.providerMapping.nftOffChainMetaType,
            NFTStandardType: chain.providerMapping.nftStandardType,
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
    const steps = getWizardSteps(nftMintStudioState.selectedChain);
    const activeStepIndex = steps.findIndex(s => s.id === nftMintStudioState.activeStep);
    const canProceed = getCanProceed();
    const canGoBack = activeStepIndex > 0;
    const canGoNext = activeStepIndex < steps.length - 1 && canProceed;

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
        case 'chain-selection':
            return !!nftMintStudioState.selectedChain;
        case 'auth':
            return nftMintStudioState.providerStates.every(p => p.state === 'active');
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
    const steps = getWizardSteps(nftMintStudioState.selectedChain);
    const currentIndex = steps.findIndex(s => s.id === nftMintStudioState.activeStep);
    const newIndex = currentIndex + direction;
    
    if (newIndex >= 0 && newIndex < steps.length) {
        if (direction > 0 && !getCanProceed()) {
            return; // Can't proceed if requirements not met
        }
        nftMintStudioState.activeStep = steps[newIndex].id;
        updateWizardStep();
    }
}

/**
 * Update wizard step (re-render current step and summary)
 */
function updateWizardStep() {
    const container = document.getElementById('nft-mint-studio-content');
    if (!container) return;

    // Re-render everything to update wizard sidebar with new steps
    renderNFTMintStudio(container);
}

/**
 * Attach wizard event listeners
 */
function attachWizardListeners() {
    // Step navigation buttons
    document.querySelectorAll('.wizard-step-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            const stepId = e.currentTarget.getAttribute('data-step');
            const steps = getWizardSteps(nftMintStudioState.selectedChain);
            const stepIndex = steps.findIndex(s => s.id === stepId);
            const currentIndex = steps.findIndex(s => s.id === nftMintStudioState.activeStep);
            
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

    // Chain selection cards
    document.querySelectorAll('.chain-selection-card').forEach(btn => {
        btn.addEventListener('click', (e) => {
            const chainId = e.currentTarget.getAttribute('data-chain');
            selectChain(chainId);
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
    window.selectChain = selectChain;
    window.nftMintStudioState = nftMintStudioState;
    window.SUPPORTED_CHAINS = SUPPORTED_CHAINS;
    
    console.log('NFT Mint Studio integration loaded (multi-chain vanilla JS version)');
}
