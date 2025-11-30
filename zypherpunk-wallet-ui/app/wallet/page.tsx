'use client';

import React, { useState, useEffect } from 'react';
import { MobileWalletHome } from '@/components/wallet/MobileWalletHome';
import { AvatarAuthScreen } from '@/components/wallet/AvatarAuthScreen';
import { SendScreen } from '@/components/wallet/SendScreen';
import { ShieldedSendScreen } from '@/components/privacy/ShieldedSendScreen';
import { ReceiveScreen } from '@/components/wallet/ReceiveScreen';
import { BuyScreen } from '@/components/wallet/BuyScreen';
import { TokensListScreen } from '@/components/wallet/TokensListScreen';
import { HistoryScreen } from '@/components/wallet/HistoryScreen';
import { SwapScreen } from '@/components/wallet/SwapScreen';
import { PrivacyBridgeScreen } from '@/components/bridge/PrivacyBridgeScreen';
import { StablecoinDashboard } from '@/components/stablecoin/StablecoinDashboard';
import { ToastContainer } from '@/components/ui/toast';
import { useWalletStore } from '@/lib/store';
import { ProviderType } from '@/lib/types';
import { toastManager } from '@/lib/toast';
import { useAvatarStore } from '@/lib/avatarStore';

type Screen = 'home' | 'create-wallet' | 'send' | 'shielded-send' | 'receive' | 'buy' | 'tokens' | 'collectibles' | 'history' | 'swap' | 'privacy' | 'bridge' | 'stablecoin' | 'security';

export default function WalletPage() {
  const { wallets, loadWallets, isLoading, user, error } = useWalletStore();
  const { logout, hasHydrated } = useAvatarStore();
  const [currentScreen, setCurrentScreen] = useState<Screen>('home');
  const [selectedProvider, setSelectedProvider] = useState<ProviderType>(ProviderType.SolanaOASIS);
  const [selectedWalletId, setSelectedWalletId] = useState<string | null>(null);
  const [toasts, setToasts] = useState(toastManager.getToasts());

  // Force hydration after timeout to prevent infinite loading
  useEffect(() => {
    if (!hasHydrated) {
      const timer = setTimeout(() => {
        useAvatarStore.setState({ hasHydrated: true });
      }, 2000);
      return () => clearTimeout(timer);
    }
  }, [hasHydrated]);

  useEffect(() => {
    if (user?.id) {
      loadWallets(user.id);
    }
  }, [user?.id, loadWallets]);

  useEffect(() => {
    const unsubscribe = toastManager.subscribe(setToasts);
    return unsubscribe;
  }, []);

  useEffect(() => {
    // Show error toast if there's an error
    if (error) {
      toastManager.error(error);
    }
  }, [error]);

  // Get the selected wallet - prefer selected, then default, then first available
  const getWallet = (providerType: ProviderType) => {
    if (selectedWalletId) {
      return wallets[providerType]?.find(w => w.walletId === selectedWalletId);
    }
    return wallets[providerType]?.find(w => w.isDefaultWallet) || wallets[providerType]?.[0];
  };

  const handleSend = () => {
    // Try Solana first, then Ethereum
    const solanaWallet = getWallet(ProviderType.SolanaOASIS);
    const ethereumWallet = getWallet(ProviderType.EthereumOASIS);
    const wallet = solanaWallet || ethereumWallet;
    
    if (wallet) {
      setSelectedProvider(wallet.providerType);
      setSelectedWalletId(wallet.walletId);
      setCurrentScreen('send');
    } else {
      // No wallet available
      setCurrentScreen('send');
    }
  };

  const handleShieldedSend = () => {
    // For Zcash wallets, use shielded send
    const zcashWallet = getWallet(ProviderType.ZcashOASIS);
    if (zcashWallet) {
      setSelectedProvider(zcashWallet.providerType);
      setSelectedWalletId(zcashWallet.walletId);
      setCurrentScreen('shielded-send');
    } else {
      toastManager.warning('Zcash wallet required for shielded transactions');
    }
  };


  const handleReceive = () => {
    const solanaWallet = getWallet(ProviderType.SolanaOASIS);
    const ethereumWallet = getWallet(ProviderType.EthereumOASIS);
    const wallet = solanaWallet || ethereumWallet;
    
    if (wallet) {
      setSelectedProvider(wallet.providerType);
      setSelectedWalletId(wallet.walletId);
      setCurrentScreen('receive');
    } else {
      setCurrentScreen('receive');
    }
  };

  const handleBuy = () => {
    const solanaWallet = getWallet(ProviderType.SolanaOASIS);
    const ethereumWallet = getWallet(ProviderType.EthereumOASIS);
    const wallet = solanaWallet || ethereumWallet;
    
    if (wallet) {
      setSelectedProvider(wallet.providerType);
      setSelectedWalletId(wallet.walletId);
      setCurrentScreen('buy');
    } else {
      setCurrentScreen('buy');
    }
  };

  const handleSendSuccess = () => {
    toastManager.success('Transaction sent successfully!');
    if (user?.id) {
      loadWallets(user.id);
    }
    // Go back to home after a short delay
    setTimeout(() => {
      setCurrentScreen('home');
    }, 2000);
  };

  const handleBuyConfirm = (amount: number) => {
    // TODO: Implement MoonPay integration
    toastManager.info(`Buy order for $${amount} initiated. This feature is coming soon.`);
    // For now, go back to home
    setTimeout(() => {
      setCurrentScreen('home');
    }, 2000);
  };

  // Show loading only briefly, then show auth screen
  if (!hasHydrated) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-black text-white">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-purple-500 mx-auto mb-4"></div>
          <p className="text-gray-400">Initializing...</p>
        </div>
      </div>
    );
  }

  if (!user) {
    return (
      <div className="min-h-screen bg-black">
        <AvatarAuthScreen />
        <ToastContainer
          toasts={toasts}
          onClose={(id) => toastManager.remove(id)}
        />
      </div>
    );
  }

  // Don't block on loading - show UI even while loading wallets
  // if (isLoading) {
  //   return (
  //     <div className="flex items-center justify-center min-h-screen bg-black">
  //       <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-purple-500"></div>
  //     </div>
  //   );
  // }

  const renderNoWallet = () => (
    <div className="min-h-screen bg-black text-white flex items-center justify-center">
      <div className="text-center px-4">
        <p className="text-xl mb-2">No wallet available</p>
        <p className="text-sm text-gray-400 mb-4">Please import or create a wallet first</p>
        <button
          onClick={() => setCurrentScreen('home')}
          className="mt-4 px-4 py-2 bg-purple-600 rounded-lg"
        >
          Go Back
        </button>
      </div>
    </div>
  );

  const handleCreateWalletSuccess = () => {
    if (user?.id) {
      loadWallets(user.id);
    }
    setCurrentScreen('home');
  };

  let screen: React.ReactNode;

  switch (currentScreen) {
    case 'create-wallet':
      screen = (
        <CreateWalletScreen
          onBack={() => setCurrentScreen('home')}
          onSuccess={handleCreateWalletSuccess}
        />
      );
      break;

    case 'send': {
      const wallet = getWallet(selectedProvider) || getWallet(ProviderType.SolanaOASIS) || getWallet(ProviderType.EthereumOASIS);
      if (!wallet) {
        screen = renderNoWallet();
      } else {
        screen = (
          <SendScreen
            wallet={wallet}
            onBack={() => setCurrentScreen('home')}
            onSuccess={handleSendSuccess}
          />
        );
      }
      break;
    }

    case 'shielded-send': {
      const wallet = getWallet(ProviderType.ZcashOASIS);
      if (!wallet) {
        screen = renderNoWallet();
      } else {
        screen = (
          <ShieldedSendScreen
            wallet={wallet}
            onBack={() => setCurrentScreen('home')}
            onSuccess={handleSendSuccess}
          />
        );
      }
      break;
    }

    case 'privacy':
      screen = (
        <div className="min-h-screen bg-zypherpunk-bg">
          <div className="p-4">
            <button
              onClick={() => setCurrentScreen('home')}
              className="mb-4 text-zypherpunk-text-muted hover:text-zypherpunk-text flex items-center space-x-2"
            >
              <span>←</span>
              <span>Back to Wallet</span>
            </button>
            <iframe
              src="/privacy"
              className="w-full h-[calc(100vh-80px)] border border-zypherpunk-border rounded-xl"
              title="Privacy Dashboard"
            />
          </div>
        </div>
      );
      break;

    case 'receive': {
      const wallet = getWallet(selectedProvider) || getWallet(ProviderType.SolanaOASIS) || getWallet(ProviderType.EthereumOASIS);
      screen = wallet ? (
        <ReceiveScreen
          providerType={wallet.providerType}
          walletAddress={wallet.walletAddress}
          onBack={() => setCurrentScreen('home')}
        />
      ) : renderNoWallet();
      break;
    }

    case 'buy': {
      const wallet = getWallet(selectedProvider) || getWallet(ProviderType.SolanaOASIS) || getWallet(ProviderType.EthereumOASIS);
      screen = wallet ? (
        <BuyScreen
          providerType={wallet.providerType}
          walletAddress={wallet.walletAddress}
          onBack={() => setCurrentScreen('home')}
          onBuy={handleBuyConfirm}
        />
      ) : renderNoWallet();
      break;
    }

    case 'tokens':
      screen = (
        <TokensListScreen
          onBack={() => setCurrentScreen('home')}
        />
      );
      break;

    case 'collectibles':
      screen = (
        <div className="min-h-screen bg-black text-white flex items-center justify-center">
          <div className="text-center">
            <p className="text-xl mb-2">Collectibles</p>
            <p className="text-gray-400 text-sm">Coming soon</p>
          </div>
        </div>
      );
      break;

    case 'history':
      screen = <HistoryScreen onBack={() => setCurrentScreen('home')} />;
      break;

    case 'swap':
      screen = <SwapScreen onBack={() => setCurrentScreen('home')} />;
      break;

    case 'bridge':
      screen = <PrivacyBridgeScreen onBack={() => setCurrentScreen('home')} />;
      break;

    case 'stablecoin':
      screen = <StablecoinDashboard onBack={() => setCurrentScreen('home')} />;
      break;

    case 'privacy':
        screen = (
          <div className="min-h-screen bg-zypherpunk-bg">
            <div className="p-4">
              <button
                onClick={() => setCurrentScreen('home')}
                className="mb-4 text-zypherpunk-text-muted hover:text-zypherpunk-text flex items-center space-x-2"
              >
                <span>←</span>
                <span>Back to Wallet</span>
              </button>
              <iframe
                src="/privacy"
                className="w-full h-[calc(100vh-80px)] border border-zypherpunk-border rounded-xl"
                title="Privacy Dashboard"
              />
            </div>
          </div>
        );
        break;

    case 'security':
      screen = (
        <div className="min-h-screen bg-zypherpunk-bg">
          <div className="p-4">
            <button
              onClick={() => setCurrentScreen('home')}
              className="mb-4 text-zypherpunk-text-muted hover:text-zypherpunk-text flex items-center space-x-2"
            >
              <span>←</span>
              <span>Back to Wallet</span>
            </button>
            <iframe
              src="/security"
              className="w-full h-[calc(100vh-80px)] border border-zypherpunk-border rounded-xl"
              title="Security Features"
            />
          </div>
        </div>
      );
      break;

      default:
      screen = (
        <MobileWalletHome
          onSend={handleSend}
          onReceive={handleReceive}
          onSwap={() => setCurrentScreen('swap')}
          onBuy={handleBuy}
          onTokens={() => setCurrentScreen('tokens')}
          onCollectibles={() => setCurrentScreen('collectibles')}
          onHistory={() => setCurrentScreen('history')}
          onHome={() => setCurrentScreen('home')}
          onSecurity={() => setCurrentScreen('security')}
          onPrivacy={() => setCurrentScreen('privacy')}
          onShieldedSend={handleShieldedSend}
          onCreateWallet={() => setCurrentScreen('create-wallet')}
          onStablecoin={() => setCurrentScreen('stablecoin')}
          onBridge={() => setCurrentScreen('bridge')}
          onLogout={logout}
        />
      );
      break;
  }

  return (
    <>
      {screen}
      <ToastContainer
        toasts={toasts}
        onClose={(id) => toastManager.remove(id)}
      />
    </>
  );
}

