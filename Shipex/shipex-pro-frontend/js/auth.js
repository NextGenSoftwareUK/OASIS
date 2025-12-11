/**
 * Authentication Component
 * Extends OASIS avatar authentication
 */

class AuthComponent {
    constructor() {
        this.mode = 'login'; // 'login' or 'register'
        this.init();
    }

    init() {
        this.render();
        this.attachEvents();
    }

    render() {
        const authScreen = document.getElementById('authScreen');
        authScreen.innerHTML = `
            <div class="auth-container">
                <div class="auth-card">
                    <div class="auth-header">
                        <div class="auth-logo">
                            <div class="logo-icon">S</div>
                        </div>
                        <h1>Shipex Pro</h1>
                        <p class="auth-subtitle">Logistics Management Platform</p>
                    </div>

                    <div class="auth-tabs">
                        <button class="auth-tab ${this.mode === 'login' ? 'active' : ''}" data-mode="login">
                            Sign In
                        </button>
                        <button class="auth-tab ${this.mode === 'register' ? 'active' : ''}" data-mode="register">
                            Create Account
                        </button>
                    </div>

                    <form id="authForm" class="auth-form">
                        ${this.mode === 'register' ? this.renderRegisterFields() : this.renderLoginFields()}
                        
                        <div id="authError" class="auth-error" style="display: none;"></div>
                        
                        <button type="submit" class="btn-primary" id="authSubmit" style="width: 100%;">
                            ${this.mode === 'login' ? 'Sign In' : 'Create Account'}
                        </button>
                    </form>

                    <div class="auth-footer">
                        <p>Powered by <strong>OASIS</strong> Avatar Authentication</p>
                    </div>
                </div>
            </div>
        `;
    }

    renderLoginFields() {
        return `
            <div class="form-group">
                <label class="form-label">Username or Email</label>
                <input type="text" class="form-input" id="username" name="username" 
                       placeholder="Enter your username or email" required autocomplete="username">
            </div>
            <div class="form-group">
                <label class="form-label">Password</label>
                <input type="password" class="form-input" id="password" name="password" 
                       placeholder="Enter your password" required autocomplete="current-password">
            </div>
        `;
    }

    renderRegisterFields() {
        return `
            <div class="form-group">
                <label class="form-label">Username</label>
                <input type="text" class="form-input" id="regUsername" name="username" 
                       placeholder="Choose a username" required autocomplete="username">
            </div>
            <div class="form-group">
                <label class="form-label">Email</label>
                <input type="email" class="form-input" id="regEmail" name="email" 
                       placeholder="your@email.com" required autocomplete="email">
            </div>
            <div class="form-group">
                <label class="form-label">Password</label>
                <input type="password" class="form-input" id="regPassword" name="password" 
                       placeholder="Create a password" required autocomplete="new-password">
            </div>
            <div class="form-group">
                <label class="form-label">Company Name</label>
                <input type="text" class="form-input" id="companyName" name="companyName" 
                       placeholder="Your company name" required>
            </div>
            <div class="form-group">
                <label class="form-label">Rate Limit Tier</label>
                <select class="form-select" id="rateLimitTier" name="rateLimitTier" required>
                    <option value="Basic">Basic</option>
                    <option value="Premium">Premium</option>
                    <option value="Enterprise">Enterprise</option>
                </select>
            </div>
        `;
    }

    attachEvents() {
        // Tab switching
        document.querySelectorAll('.auth-tab').forEach(tab => {
            tab.addEventListener('click', (e) => {
                this.mode = e.target.dataset.mode;
                this.render();
                this.attachEvents();
            });
        });

        // Form submission
        const form = document.getElementById('authForm');
        if (form) {
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                this.handleSubmit();
            });
        }

        // Skip authentication button (for testing)
        const skipBtn = document.getElementById('skipAuthBtn');
        if (skipBtn) {
            skipBtn.addEventListener('click', () => {
                this.handleSkipAuth();
            });
        }
    }

    async handleSubmit() {
        const errorDiv = document.getElementById('authError');
        const submitBtn = document.getElementById('authSubmit');
        
        // Clear previous errors
        errorDiv.style.display = 'none';
        submitBtn.disabled = true;
        submitBtn.textContent = 'Processing...';

        try {
            if (this.mode === 'login') {
                await this.handleLogin();
            } else {
                await this.handleRegister();
            }
        } catch (error) {
            errorDiv.textContent = error.message || 'Authentication failed';
            errorDiv.style.display = 'block';
            submitBtn.disabled = false;
            submitBtn.textContent = this.mode === 'login' ? 'Sign In' : 'Create Account';
        }
    }

    async handleLogin() {
        const username = document.getElementById('username').value.trim();
        const password = document.getElementById('password').value;

        if (!username || !password) {
            throw new Error('Please enter username and password');
        }

        showLoading('Signing in...');

        try {
            // Login with OASIS avatar
            const authResult = await shipexAPI.loginAvatar(username, password);
            
            // Get or create merchant profile
            let merchant;
            try {
                const merchantResult = await shipexAPI.getMerchantByAvatar(authResult.avatarId);
                merchant = merchantResult.result || merchantResult;
            } catch (err) {
                // Merchant doesn't exist yet - we'll create it after login
                console.log('Merchant profile not found, will create after login');
            }

            // Store merchant context
            if (merchant) {
                shipexAPI.setMerchant(merchant);
            } else {
                // Create merchant profile
                const createResult = await shipexAPI.createMerchantFromAvatar(authResult.avatarId, {
                    companyName: 'My Company', // Will be updated in settings
                    rateLimitTier: 'Basic'
                });
                shipexAPI.setMerchant(createResult.result || createResult);
            }

            hideLoading();
            showToast('Successfully signed in', 'success');
            
            // Navigate to dashboard
            router.navigate('dashboard');
            window.location.reload(); // Refresh to load merchant context
        } catch (error) {
            hideLoading();
            throw error;
        }
    }

    async handleRegister() {
        const username = document.getElementById('regUsername').value.trim();
        const email = document.getElementById('regEmail').value.trim();
        const password = document.getElementById('regPassword').value;
        const companyName = document.getElementById('companyName').value.trim();
        const rateLimitTier = document.getElementById('rateLimitTier').value;

        if (!username || !email || !password || !companyName) {
            throw new Error('Please fill in all required fields');
        }

        showLoading('Creating account...');

        try {
            // Register OASIS avatar
            const authResult = await shipexAPI.registerAvatar({
                username,
                email,
                password,
                confirmPassword: password,
                firstName: companyName.split(' ')[0] || 'Merchant',
                lastName: companyName.split(' ').slice(1).join(' ') || 'User',
                title: 'Mr',
                avatarType: 'User',
                acceptTerms: true
            });

            // Create merchant profile
            const merchantResult = await shipexAPI.createMerchantFromAvatar(authResult.avatarId, {
                companyName,
                rateLimitTier
            });

            shipexAPI.setMerchant(merchantResult.result || merchantResult);

            hideLoading();
            showToast('Account created successfully', 'success');
            
            // Navigate to dashboard
            router.navigate('dashboard');
            window.location.reload();
        } catch (error) {
            hideLoading();
            throw error;
        }
    }

    async handleSkipAuth() {
        if (!confirm('Skip authentication and use demo merchant? (Testing only)')) {
            return;
        }

        showLoading('Setting up demo merchant...');

        try {
            // Create demo merchant profile with proper structure
            const demoMerchant = {
                merchantId: '00000000-0000-0000-0000-000000000001',
                MerchantId: '00000000-0000-0000-0000-000000000001',
                avatarId: '00000000-0000-0000-0000-000000000001',
                AvatarId: '00000000-0000-0000-0000-000000000001',
                companyName: 'Demo Merchant',
                CompanyName: 'Demo Merchant',
                contactInfo: {
                    email: 'demo@shipexpro.test',
                    Email: 'demo@shipexpro.test',
                    phone: '555-0000',
                    Phone: '555-0000',
                    address: '123 Demo St',
                    Address: '123 Demo St'
                },
                ContactInfo: {
                    email: 'demo@shipexpro.test',
                    Email: 'demo@shipexpro.test',
                    phone: '555-0000',
                    Phone: '555-0000',
                    address: '123 Demo St',
                    Address: '123 Demo St'
                },
                rateLimitTier: 'Premium',
                RateLimitTier: 'Premium',
                isActive: true,
                IsActive: true,
                quickBooksConnected: false,
                QuickBooksConnected: false
            };

            // Store demo merchant and a dummy token (must start with 'demo-token-for-testing')
            const demoToken = 'demo-token-for-testing-' + Date.now();
            shipexAPI.setAuthToken(demoToken);
            shipexAPI.setMerchant(demoMerchant);

            hideLoading();
            showToast('Demo merchant loaded (Testing mode)', 'info');
            
            // Small delay to ensure localStorage is written
            await new Promise(resolve => setTimeout(resolve, 200));
            
            // Navigate to dashboard
            router.navigate('dashboard');
            
            // Force re-check auth and render dashboard
            setTimeout(() => {
                // Re-check auth state
                const token = shipexAPI.getAuthToken();
                const merchant = shipexAPI.getMerchant();
                
                if (token && merchant) {
                    // Show nav
                    document.getElementById('nav').style.display = 'block';
                    document.getElementById('merchantInfo').style.display = 'flex';
                    document.getElementById('merchantName').textContent = merchant.companyName || merchant.CompanyName || 'Demo Merchant';
                    document.getElementById('merchantTier').textContent = (merchant.rateLimitTier || merchant.RateLimitTier || 'Premium') + ' (TEST)';
                    document.getElementById('merchantTier').style.color = 'var(--status-in-transit)';
                    document.getElementById('logoutBtn').style.display = 'block';
                    
                    // Initialize dashboard
                    if (!dashboardComponent) {
                        dashboardComponent = new DashboardComponent();
                    }
                    dashboardComponent.init();
                }
            }, 100);
        } catch (error) {
            hideLoading();
            showToast('Failed to setup demo merchant', 'error');
            console.error('Skip auth error:', error);
        }
    }
}

// Initialize auth component when auth screen is shown
let authComponent = null;
