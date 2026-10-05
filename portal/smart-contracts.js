/**
 * Smart Contracts Tab Loader
 * Loads the Smart Contract Generator UI into the portal
 */

let scGenUILoaded = false;

/**
 * Load Smart Contract Generator UI
 */
function loadSmartContracts() {
    const container = document.getElementById('smart-contracts-content');
    if (!container) {
        console.error('Smart contracts content container not found');
        return;
    }

    // If already loaded, just show it
    if (scGenUILoaded) {
        return;
    }

    // Check if user is authenticated
    const auth = window.authStore?.getAuth();
    if (!auth || !auth.token) {
        container.innerHTML = `
            <div style="text-align: center; padding: 4rem 2rem;">
                <h2 style="color: var(--text-primary); margin-bottom: 1rem;">Authentication Required</h2>
                <p style="color: var(--text-secondary); margin-bottom: 2rem;">
                    Please log in to your OASIS avatar to generate and deploy smart contracts.
                </p>
                <button class="btn-login" onclick="showLoginModal()" style="padding: 0.75rem 2rem; font-size: 1rem;">
                    Log In
                </button>
            </div>
        `;
        return;
    }

    // Load UI via iframe (Next.js app running on port 3001)
    const uiUrl = 'http://localhost:3001';
    
    // Show loading state
    container.innerHTML = `
        <div style="width: 100%; height: 100vh; min-height: 800px; position: relative;">
            <iframe 
                id="scgen-iframe"
                src="${uiUrl}/generate/template" 
                style="width: 100%; height: 100%; border: none; background: transparent;"
                title="Smart Contract Generator"
                allow="clipboard-read; clipboard-write"
                onerror="this.onerror=null; this.src='data:text/html,<html><body style=\"background:#0a0a0a;color:#fff;padding:4rem;text-align:center;font-family:system-ui\"><h2>UI Not Available</h2><p>Smart Contract Generator UI is not running on port 3001</p><p style=\"color:#999;margin-top:2rem\">Start it with: cd SmartContractGenerator/ScGen.UI && npm run dev</p></body></html>'"
            ></iframe>
            <div id="scgen-error" style="display: none; position: absolute; top: 0; left: 0; right: 0; bottom: 0; background: var(--bg-primary); padding: 4rem 2rem; text-align: center;">
                <h2 style="color: var(--text-primary); margin-bottom: 1rem;">Smart Contract Generator UI Not Running</h2>
                <p style="color: var(--text-secondary); margin-bottom: 2rem;">
                    The Smart Contract Generator UI needs to be started to use this feature.
                </p>
                <div style="background: var(--bg-secondary); border: 1px solid var(--border-color); padding: 1.5rem; text-align: left; max-width: 600px; margin: 0 auto;">
                    <p style="color: var(--text-secondary); margin-bottom: 1rem; font-family: 'Courier New', monospace; font-size: 0.875rem;">
                        To start the UI, run:
                    </p>
                    <code style="display: block; background: var(--bg-primary); padding: 1rem; border: 1px solid var(--border-color); color: var(--text-primary); font-family: 'Courier New', monospace; font-size: 0.875rem; margin-bottom: 1rem; white-space: pre;">
cd SmartContractGenerator/ScGen.UI
npm run dev
                    </code>
                    <p style="color: var(--text-tertiary); font-size: 0.875rem; margin-bottom: 0.5rem;">
                        <strong>Note:</strong> This requires Node.js >= 20.9.0
                    </p>
                    <p style="color: var(--text-tertiary); font-size: 0.875rem;">
                        Check your Node version: <code style="background: var(--bg-primary); padding: 0.25rem 0.5rem; border: 1px solid var(--border-color);">node --version</code>
                    </p>
                </div>
            </div>
        </div>
    `;
    
    // Try to detect if iframe loaded successfully
    const iframe = document.getElementById('scgen-iframe');
    const errorDiv = document.getElementById('scgen-error');
    
    // Check if UI is reachable after a delay
    setTimeout(() => {
        fetch(uiUrl, { method: 'HEAD', mode: 'no-cors' })
            .catch(() => {
                // UI not available, show error message
                if (errorDiv && iframe) {
                    iframe.style.display = 'none';
                    errorDiv.style.display = 'block';
                }
            });
    }, 2000);
    
    // Pass auth token to iframe when it loads
    if (iframe) {
        iframe.addEventListener('load', function() {
            const auth = window.authStore?.getAuth();
            if (auth && auth.token && this.contentWindow) {
                try {
                    this.contentWindow.postMessage({
                        type: 'OASIS_AUTH_TOKEN',
                        token: auth.token,
                        avatarId: auth.avatar?.id,
                        walletAddress: auth.avatar?.providerWallets?.SolanaOASIS?.[0]?.walletAddress
                    }, uiUrl);
                } catch (e) {
                    console.warn('Could not send auth to iframe:', e);
                }
            }
        });
        
        iframe.addEventListener('error', function() {
            if (errorDiv) {
                this.style.display = 'none';
                errorDiv.style.display = 'block';
            }
        });
    }
    
    // Listen for messages from iframe
    window.addEventListener('message', function(event) {
        // Verify origin
        if (event.origin !== uiUrl) return;
        
        if (event.data.type === 'REQUEST_OASIS_AUTH') {
            // Send auth data to iframe
            const auth = window.authStore?.getAuth();
            if (auth && event.source) {
                event.source.postMessage({
                    type: 'OASIS_AUTH_TOKEN',
                    token: auth.token,
                    avatarId: auth.avatar?.id,
                    walletAddress: auth.avatar?.providerWallets?.SolanaOASIS?.[0]?.walletAddress
                }, event.origin);
            }
        }
    });

    scGenUILoaded = true;
}

// Make function globally available
window.loadSmartContracts = loadSmartContracts;

