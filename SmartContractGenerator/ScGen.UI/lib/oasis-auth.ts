/**
 * OASIS Authentication Integration
 * Handles authentication from portal parent window
 */

export interface OASISAuth {
  token: string;
  avatar: {
    id: string;
    avatarId: string;
    username: string;
    email?: string;
    firstName?: string;
    lastName?: string;
  };
}

let oasisAuth: OASISAuth | null = null;

/**
 * Initialize OASIS authentication listener
 * Listens for auth messages from portal parent window
 */
export function initOASISAuth(): void {
  // Listen for auth messages from parent (portal)
  window.addEventListener('message', (event) => {
    // Verify origin (in production, check actual portal URL)
    if (event.data.type === 'OASIS_AUTH') {
      oasisAuth = {
        token: event.data.token,
        avatar: event.data.avatar
      };
      console.log('✅ OASIS authentication received from portal');
      
      // Dispatch event for components to listen
      window.dispatchEvent(new CustomEvent('oasis:auth:update', {
        detail: oasisAuth
      }));
    }
  });

  // Request auth from parent if in iframe
  if (window.self !== window.top) {
    console.log('📡 Requesting OASIS authentication from portal...');
    window.parent.postMessage({ type: 'OASIS_GET_AUTH' }, '*');
  }
}

/**
 * Get current OASIS auth
 */
export function getOASISAuth(): OASISAuth | null {
  return oasisAuth;
}

/**
 * Get OASIS JWT token
 */
export function getOASISToken(): string | null {
  return oasisAuth?.token || null;
}

/**
 * Get avatar wallet address (if available in auth response)
 */
export function getAvatarWalletAddress(): string | null {
  if (!oasisAuth?.avatar) return null;
  
  // Check if wallet info is in avatar object
  const wallets = (oasisAuth.avatar as any).providerWallets;
  if (wallets?.SolanaOASIS?.[0]) {
    return wallets.SolanaOASIS[0].walletAddress || wallets.SolanaOASIS[0].publicKey;
  }
  
  return null;
}


