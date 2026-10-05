/**
 * Settings Component
 * Includes QuickBooks OAuth connection
 */

class SettingsComponent {
    constructor() {
        this.merchant = null;
    }

    async init() {
        this.merchant = shipexAPI.getMerchant();
        this.render();
        this.attachEvents();
    }

    render() {
        const settingsScreen = document.getElementById('settingsScreen');
        const quickBooksConnected = this.merchant?.quickBooksConnected || this.merchant?.QuickBooksConnected || false;

        settingsScreen.innerHTML = `
            <div class="settings-container">
                <h1>Settings</h1>

                <!-- QuickBooks Section -->
                <div class="card" style="margin-bottom: 24px;">
                    <div class="card-header">
                        <div>
                            <h2 class="card-title">QuickBooks Integration</h2>
                            <p style="color: var(--text-secondary); font-size: 14px; margin-top: 4px;">
                                Connect QuickBooks for automated invoicing
                            </p>
                        </div>
                        <span class="status-badge ${quickBooksConnected ? 'delivered' : 'quote-requested'}">
                            ${quickBooksConnected ? 'Connected' : 'Not Connected'}
                        </span>
                    </div>

                    ${quickBooksConnected ? `
                        <div class="settings-connected">
                            <p style="color: var(--text-secondary); margin: 16px 0;">
                                QuickBooks is connected and ready to sync invoices.
                            </p>
                            <button class="btn-secondary" id="disconnectQuickBooksBtn">Disconnect</button>
                        </div>
                    ` : `
                        <div class="settings-not-connected">
                            <p style="color: var(--text-secondary); margin: 16px 0;">
                                Connect your QuickBooks account to automatically create invoices for shipments.
                            </p>
                            <button class="btn-primary" id="connectQuickBooksBtn">Connect QuickBooks</button>
                        </div>
                    `}
                </div>

                <!-- Merchant Info Section -->
                <div class="card">
                    <h2 class="card-title">Merchant Information</h2>
                    <div class="settings-info">
                        <div class="settings-info-item">
                            <span class="settings-info-label">Company Name:</span>
                            <span class="settings-info-value">${this.merchant?.companyName || this.merchant?.CompanyName || 'N/A'}</span>
                        </div>
                        <div class="settings-info-item">
                            <span class="settings-info-label">Email:</span>
                            <span class="settings-info-value">${this.merchant?.contactInfo?.email || this.merchant?.ContactInfo?.Email || 'N/A'}</span>
                        </div>
                        <div class="settings-info-item">
                            <span class="settings-info-label">Rate Limit Tier:</span>
                            <span class="settings-info-value">${this.merchant?.rateLimitTier || this.merchant?.RateLimitTier || 'Basic'}</span>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    attachEvents() {
        const connectBtn = document.getElementById('connectQuickBooksBtn');
        if (connectBtn) {
            connectBtn.addEventListener('click', () => this.connectQuickBooks());
        }

        const disconnectBtn = document.getElementById('disconnectQuickBooksBtn');
        if (disconnectBtn) {
            disconnectBtn.addEventListener('click', () => this.disconnectQuickBooks());
        }
    }

    async connectQuickBooks() {
        try {
            const merchant = shipexAPI.getMerchant();
            if (!merchant) {
                throw new Error('Please log in');
            }

            showLoading('Connecting to QuickBooks...');

            // Get authorization URL
            const result = await shipexAPI.getQuickBooksAuthUrl(
                merchant.merchantId || merchant.MerchantId,
                'shipex-oauth-state'
            );

            hideLoading();

            const authUrl = result.result?.authorizationUrl || result.authorizationUrl || result.result;
            
            if (!authUrl) {
                throw new Error('Failed to get QuickBooks authorization URL');
            }

            // Open QuickBooks OAuth in popup
            const width = 600;
            const height = 700;
            const left = (screen.width - width) / 2;
            const top = (screen.height - height) / 2;

            const popup = window.open(
                authUrl,
                'QuickBooks OAuth',
                `width=${width},height=${height},left=${left},top=${top},resizable=yes,scrollbars=yes`
            );

            // Listen for OAuth callback
            const checkClosed = setInterval(() => {
                if (popup.closed) {
                    clearInterval(checkClosed);
                    // Refresh settings to check connection status
                    this.init();
                }
            }, 1000);

            // Also listen for message from popup (if OAuth callback sends message)
            window.addEventListener('message', (event) => {
                if (event.data.type === 'quickbooks-connected') {
                    popup.close();
                    showToast('QuickBooks connected successfully', 'success');
                    this.init();
                }
            }, { once: true });

        } catch (error) {
            hideLoading();
            showToast(error.message || 'Failed to connect QuickBooks', 'error');
        }
    }

    async disconnectQuickBooks() {
        if (!confirm('Are you sure you want to disconnect QuickBooks?')) {
            return;
        }

        showToast('Disconnect functionality coming soon', 'info');
        // TODO: Implement disconnect endpoint
    }
}

let settingsComponent = null;
