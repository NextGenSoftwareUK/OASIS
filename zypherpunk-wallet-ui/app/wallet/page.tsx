'use client';

import React, { useState, useEffect } from 'react';
import { MobileWalletHome } from '@/components/wallet/MobileWalletHome';
import { AvatarAuthScreen } from '@/components/wallet/AvatarAuthScreen';
import { CreateWalletScreen } from '@/components/wallet/CreateWalletScreen';
import { SendScreen } from '@/components/wallet/SendScreen';
import { ShieldedSendScreen } from '@/components/privacy/ShieldedSendScreen';
import { PrivacyDropScreen } from '@/components/privacy/PrivacyDropScreen';
import { ClaimDropScreen } from '@/components/privacy/ClaimDropScreen';
import { ReceiveScreen } from '@/components/wallet/ReceiveScreen';
import { BuyScreen } from '@/components/wallet/BuyScreen';
import { TokensListScreen } from '@/components/wallet/TokensListScreen';
import { HistoryScreen } from '@/components/wallet/HistoryScreen';
import { SwapScreen } from '@/components/wallet/SwapScreen';
import { PrivacyBridgeScreen } from '@/components/bridge/PrivacyBridgeScreen';
import { StablecoinDashboard } from '@/components/stablecoin/StablecoinDashboard';
import { WalletDetailScreen } from '@/components/wallet/WalletDetailScreen';
import { ToastContainer } from '@/components/ui/toast';
import { useWalletStore } from '@/lib/store';
import { ProviderType, Wallet } from '@/lib/types';
import { toastManager } from '@/lib/toast';
import { useAvatarStore } from '@/lib/avatarStore';
import { removeWalletsByProviderType } from '@/lib/walletUtils';
import { createUnifiedWallet } from '@/lib/unifiedWallet';

type Screen = 'home' | 'create-wallet' | 'send' | 'shielded-send' | 'receive' | 'buy' | 'tokens' | 'collectibles' | 'history' | 'swap' | 'privacy' | 'bridge' | 'stablecoin' | 'security' | 'wallet-detail' | 'create-drop' | 'claim-drop';

export default function WalletPage() {
  const { wallets, loadWallets, isLoading, user, error } = useWalletStore();
  const { logout, hasHydrated } = useAvatarStore();
  const [currentScreen, setCurrentScreen] = useState<Screen>('home');
  const [selectedProvider, setSelectedProvider] = useState<ProviderType>(ProviderType.SolanaOASIS);
  const [selectedWalletId, setSelectedWalletId] = useState<string | null>(null);
  const [selectedWallet, setSelectedWallet] = useState<Wallet | null>(null);
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

  // Refresh wallets when screen changes to 'home' to ensure latest wallets are shown
  useEffect(() => {
    if (currentScreen === 'home' && user?.id) {
      loadWallets(user.id);
    }
  }, [currentScreen, user?.id, loadWallets]);

  // Clean up Universal/Default wallets and create Zypherpunk wallets on first load
  useEffect(() => {
    if (user?.id && Object.keys(wallets).length > 0) {
      const initKey = `wallet-init-${user.id}`;
      const hasRunInit = sessionStorage.getItem(initKey);
      
      if (!hasRunInit) {
        sessionStorage.setItem(initKey, 'true');
        
        // Check for Universal wallets to clean up
        const hasUniversalWallets = wallets[ProviderType.Default]?.length || 
                                   wallets[ProviderType.LocalFileOASIS]?.length;
        
        // Check if Zypherpunk wallets exist
        const hasZcash = wallets[ProviderType.ZcashOASIS]?.length > 0;
        const hasAztec = wallets[ProviderType.AztecOASIS]?.length > 0;
        const hasMiden = wallets[ProviderType.MidenOASIS]?.length > 0;
        const hasStarknet = wallets[ProviderType.StarknetOASIS]?.length > 0;
        const hasZypherpunkWallets = hasZcash || hasAztec || hasMiden || hasStarknet;
        
        // Clean up Universal wallets first
        if (hasUniversalWallets) {
          removeWalletsByProviderType(user.id, [
            ProviderType.Default,
            ProviderType.LocalFileOASIS,
          ]).then((result) => {
            if (result.success) {
              toastManager.success('Cleaned up Universal wallets');
              loadWallets(user.id);
            }
          });
        }
        
        // Auto-create Zypherpunk wallets if they don't exist
        if (!hasZypherpunkWallets) {
          toastManager.info('Creating Zypherpunk privacy wallets (Zcash, Aztec, Miden, Starknet)...');
          createUnifiedWallet(user.id).then((unifiedWallet) => {
            toastManager.success(`Created ${Object.keys(unifiedWallet.wallets).length} privacy wallets!`);
            loadWallets(user.id);
          }).catch((error) => {
            console.error('Failed to create unified wallet:', error);
            toastManager.error('Failed to create privacy wallets. Please create them manually.');
          });
        }
      }
    }
  }, [user?.id, wallets]); // Run when user or wallets change

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

  // Get the selected wallet - prefer selected, then most recent wallet
  const getWallet = (providerType: ProviderType) => {
    if (selectedWalletId) {
      const wallet = wallets[providerType]?.find(w => w.walletId === selectedWalletId);
      if (wallet) return wallet;
    }
    
    // Get the most recent wallet for this provider type
    const walletsOfType = wallets[providerType] || [];
    if (walletsOfType.length === 0) return undefined;
    
    // Sort by modifiedDate (most recent first), then createdDate
    // FIRST: Prioritize the working address above all else
    const sortedWallets = [...walletsOfType].sort((a, b) => {
      // Check for working address first - it takes absolute priority
      const aIsWorking = a.walletAddress === 'tmAZ65X3Z7o69p31bzMvftAUCTS46Kw1VtT';
      const bIsWorking = b.walletAddress === 'tmAZ65X3Z7o69p31bzMvftAUCTS46Kw1VtT';
      if (aIsWorking && !bIsWorking) return -1; // Working address comes first
      if (bIsWorking && !aIsWorking) return 1;
      
      const aDate = a.modifiedDate || a.createdDate || '';
      const bDate = b.modifiedDate || b.createdDate || '';
      
      // If both have valid dates, sort normally
      if (aDate && aDate !== '0001-01-01T00:00:00' && bDate && bDate !== '0001-01-01T00:00:00') {
        return bDate.localeCompare(aDate);
      }
      
      // If one has empty date and one has valid date, prioritize the one with valid date
      if (aDate && aDate !== '0001-01-01T00:00:00' && (!bDate || bDate === '0001-01-01T00:00:00')) {
        return -1; // a comes first
      }
      if (bDate && bDate !== '0001-01-01T00:00:00' && (!aDate || aDate === '0001-01-01T00:00:00')) {
        return 1; // b comes first
      }
      
      // If both have empty dates, prioritize wallets with valid addresses
      if ((!aDate || aDate === '0001-01-01T00:00:00') && (!bDate || bDate === '0001-01-01T00:00:00')) {
        const aHasAddress = a.walletAddress && a.walletAddress.length > 0;
        const bHasAddress = b.walletAddress && b.walletAddress.length > 0;
        if (aHasAddress && !bHasAddress) return -1;
        if (bHasAddress && !aHasAddress) return 1;
        
        if (aHasAddress && bHasAddress) {
          const aIsValid = a.walletAddress.startsWith('tm') || a.walletAddress.startsWith('t1') || a.walletAddress.startsWith('u1') || a.walletAddress.startsWith('utest1');
          const bIsValid = b.walletAddress.startsWith('tm') || b.walletAddress.startsWith('t1') || b.walletAddress.startsWith('u1') || b.walletAddress.startsWith('utest1');
          if (aIsValid && !bIsValid) return -1;
          if (bIsValid && !aIsValid) return 1;
        }
        
        return (b.walletId || '').localeCompare(a.walletId || '');
      }
      
      return bDate.localeCompare(aDate);
    });
    
    return sortedWallets[0]; // Most recent wallet
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

  const handleCreateDrop = () => {
    // Try privacy chains first, then others
    const wallet = getWallet(ProviderType.ZcashOASIS) ||
                   getWallet(ProviderType.AztecOASIS) ||
                   getWallet(ProviderType.MidenOASIS) ||
                   getWallet(ProviderType.StarknetOASIS) ||
                   getWallet(ProviderType.SolanaOASIS) ||
                   getWallet(ProviderType.EthereumOASIS);
    
    if (wallet) {
      setSelectedProvider(wallet.providerType);
      setSelectedWalletId(wallet.walletId);
      setSelectedWallet(wallet);
      setCurrentScreen('create-drop');
    } else {
      toastManager.warning('Please create a wallet first');
    }
  };

  const handleClaimDrop = () => {
    setCurrentScreen('claim-drop');
  };


  const handleReceive = () => {
    // Use selected provider if available, otherwise try privacy chains first
    let wallet = selectedProvider ? getWallet(selectedProvider) : null;
    
    if (!wallet) {
      // Try privacy chains in priority order
      wallet = getWallet(ProviderType.ZcashOASIS) ||
               getWallet(ProviderType.AztecOASIS) ||
               getWallet(ProviderType.MidenOASIS) ||
               getWallet(ProviderType.StarknetOASIS) ||
               getWallet(ProviderType.SolanaOASIS) ||
               getWallet(ProviderType.EthereumOASIS);
    }
    
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
      // Use selected provider if available, otherwise try privacy chains first
      let wallet = selectedProvider ? getWallet(selectedProvider) : null;
      
      if (!wallet) {
        // Try privacy chains in priority order
        wallet = getWallet(ProviderType.ZcashOASIS) ||
                 getWallet(ProviderType.AztecOASIS) ||
                 getWallet(ProviderType.MidenOASIS) ||
                 getWallet(ProviderType.StarknetOASIS) ||
                 getWallet(ProviderType.SolanaOASIS) ||
                 getWallet(ProviderType.EthereumOASIS);
      }
      
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

    case 'wallet-detail': {
      if (!selectedWallet) {
        screen = renderNoWallet();
      } else {
        screen = (
          <WalletDetailScreen
            wallet={selectedWallet}
            onBack={() => setCurrentScreen('home')}
            onReceive={() => {
              setSelectedProvider(selectedWallet.providerType);
              setCurrentScreen('receive');
            }}
            onBuy={() => {
              setSelectedProvider(selectedWallet.providerType);
              setCurrentScreen('buy');
            }}
            onShare={() => {
              // Share functionality
            }}
          />
        );
      }
      break;
    }

    case 'create-drop': {
      const wallet = selectedWallet || getWallet(selectedProvider) || 
                     getWallet(ProviderType.ZcashOASIS) ||
                     getWallet(ProviderType.SolanaOASIS) ||
                     getWallet(ProviderType.EthereumOASIS);
      if (!wallet) {
        screen = renderNoWallet();
      } else {
        screen = (
          <PrivacyDropScreen
            wallet={wallet}
            onBack={() => setCurrentScreen('home')}
            onSuccess={() => {
              if (user?.id) {
                loadWallets(user.id);
              }
              setCurrentScreen('home');
            }}
          />
        );
      }
      break;
    }

    case 'claim-drop':
      screen = (
        <ClaimDropScreen
          onBack={() => setCurrentScreen('home')}
          onSuccess={() => {
            if (user?.id) {
              loadWallets(user.id);
            }
            setCurrentScreen('home');
          }}
        />
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
          onCreateDrop={handleCreateDrop}
          onClaimDrop={handleClaimDrop}
          onLogout={logout}
          onWalletClick={(wallet) => {
            setSelectedWallet(wallet);
            setSelectedProvider(wallet.providerType);
            setSelectedWalletId(wallet.walletId);
            setCurrentScreen('wallet-detail');
          }}
        />
      );
      break;
  }

  // Render nothing until hydrated to prevent hydration mismatches
  if (!hasHydrated) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-black">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-white"></div>
      </div>
    );
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

