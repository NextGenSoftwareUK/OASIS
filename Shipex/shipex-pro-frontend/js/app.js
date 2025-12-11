/**
 * Main Application Initialization
 */

// Check authentication on load
function checkAuth() {
    const token = shipexAPI.getAuthToken();
    const merchant = shipexAPI.getMerchant();

    // Allow demo token for testing
    const isDemoToken = token && token.startsWith('demo-token-for-testing');
    
    if ((!token || !merchant) && !isDemoToken) {
        // Not authenticated - show auth screen
        document.getElementById('authScreen').classList.add('active');
        document.getElementById('nav').style.display = 'none';
        return false;
    }

    // Authenticated - show nav and dashboard
    document.getElementById('nav').style.display = 'block';
    document.getElementById('merchantInfo').style.display = 'flex';
    const merchantName = merchant.companyName || merchant.CompanyName || 'Merchant';
    const merchantTier = merchant.rateLimitTier || merchant.RateLimitTier || 'Basic';
    document.getElementById('merchantName').textContent = merchantName;
    document.getElementById('merchantTier').textContent = merchantTier;
    document.getElementById('logoutBtn').style.display = 'block';
    
    // Show testing indicator if using demo token
    if (isDemoToken) {
        const tierElement = document.getElementById('merchantTier');
        if (tierElement) {
            tierElement.textContent = merchantTier + ' (TEST)';
            tierElement.style.color = 'var(--status-in-transit)';
        }
    }
    
    return true;
}

// Initialize router routes
router.route('auth', () => {
    if (!authComponent) {
        authComponent = new AuthComponent();
    }
    document.getElementById('nav').style.display = 'none';
});

router.route('dashboard', async () => {
    if (!checkAuth()) {
        router.navigate('auth');
        return;
    }
    
    // Initialize or re-initialize dashboard component
    if (!dashboardComponent) {
        dashboardComponent = new DashboardComponent();
    }
    await dashboardComponent.init();
});

router.route('quote', () => {
    if (!checkAuth()) {
        router.navigate('auth');
        return;
    }
    if (!quoteComponent) {
        quoteComponent = new QuoteComponent();
    }
    quoteComponent.init();
});

router.route('tracking', (params) => {
    if (!checkAuth()) {
        router.navigate('auth');
        return;
    }
    const trackingNumber = params && params[0] ? decodeURIComponent(params[0]) : null;
    if (!trackingComponent) {
        trackingComponent = new TrackingComponent();
    }
    trackingComponent.init(trackingNumber);
});

router.route('markups', () => {
    if (!checkAuth()) {
        router.navigate('auth');
        return;
    }
    if (!markupsComponent) {
        markupsComponent = new MarkupsComponent();
    }
    markupsComponent.init();
});

router.route('settings', () => {
    if (!checkAuth()) {
        router.navigate('auth');
        return;
    }
    if (!settingsComponent) {
        settingsComponent = new SettingsComponent();
    }
    settingsComponent.init();
});

router.route('confirm', () => {
    if (!checkAuth()) {
        router.navigate('auth');
        return;
    }
    if (!confirmComponent) {
        confirmComponent = new ConfirmComponent();
    }
    confirmComponent.init();
});

// Logout handler
document.getElementById('logoutBtn')?.addEventListener('click', () => {
    shipexAPI.clearAuth();
    router.navigate('auth');
    window.location.reload();
});

// Mobile nav toggle
document.getElementById('navToggle')?.addEventListener('click', () => {
    document.getElementById('navMenu').classList.toggle('active');
});

// Initialize app
document.addEventListener('DOMContentLoaded', () => {
    // Restore auth state
    const token = shipexAPI.getAuthToken();
    const merchant = shipexAPI.getMerchant();

    // Check if demo token
    const isDemoToken = token && token.startsWith('demo-token-for-testing');

    if (token && merchant) {
        // Don't make API calls in demo mode
        if (isDemoToken) {
            // Just show the UI without API calls
            document.getElementById('nav').style.display = 'block';
            document.getElementById('merchantInfo').style.display = 'flex';
            document.getElementById('merchantName').textContent = merchant.companyName || merchant.CompanyName || 'Demo Merchant';
            document.getElementById('merchantTier').textContent = (merchant.rateLimitTier || merchant.RateLimitTier || 'Premium') + ' (TEST)';
            document.getElementById('merchantTier').style.color = 'var(--status-in-transit)';
            document.getElementById('logoutBtn').style.display = 'block';
        } else {
            checkAuth();
        }
        
        // Navigate to current route or dashboard
        if (!window.location.hash || window.location.hash === '#') {
            router.navigate('dashboard');
        } else {
            // Let router handle the current hash
            router.handleRoute();
        }
    } else {
        router.navigate('auth');
    }
});
