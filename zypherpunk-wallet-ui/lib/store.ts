import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { Wallet, Transaction, WalletTransactionRequest, WalletImportRequest, User, WalletStore } from './types';
import { ProviderType } from './types';
import { oasisWalletAPI } from './api';
import { starknetWalletAPI } from './api/starknetApi';
import { normalizeProviderType } from './providerTypeMapper';

export const useWalletStore = create<WalletStore>()(
  persist(
    (set, get) => ({
      user: null,
      wallets: {} as Partial<Record<ProviderType, Wallet[]>>,
      selectedWallet: null,
      transactions: [],
      isLoading: false,
      error: null,

      // Actions
      setUser: (user: User | null) => set((state) => ({
        user,
        wallets: user ? state.wallets : {},
        selectedWallet: user ? state.selectedWallet : null,
      })),
      setWallets: (wallets: Partial<Record<ProviderType, Wallet[]>>) => set({ wallets }),
      setSelectedWallet: (wallet: Wallet | null) => set({ selectedWallet: wallet }),
      setTransactions: (transactions: Transaction[]) => set({ transactions }),
      setLoading: (loading: boolean) => set({ isLoading: loading }),
      setError: (error: string | null) => set({ error }),

      // Wallet operations
      loadWallets: async (userId?: string) => {
        const targetId = userId || get().user?.id;
        if (!targetId) {
          set({ error: 'No avatar selected. Please sign in first.', isLoading: false });
          return;
        }

        set({ isLoading: true, error: null });
        try {
          const result = await oasisWalletAPI.loadWalletsById(targetId);
          
          if (result.isError) {
            // Check if it's a bot protection / API unavailable error
            const isApiUnavailable = result.message?.includes('HTML response') || 
                                   result.message?.includes('bot protection') ||
                                   result.message?.includes('502');
            
            // Check if avatar doesn't exist yet (common after first login)
            const isAvatarNotFound = result.message?.includes('Avatar Not Found') ||
                                    result.message?.includes('avatar does not exist') ||
                                    result.message?.includes('does not exist');
            
            if (isApiUnavailable) {
              set({ 
                error: 'API is currently unavailable. The OASIS API may be blocked or unreachable. Please check your connection or try using a local API server.', 
                isLoading: false,
                wallets: {} // Clear wallets on API error
              });
            } else if (isAvatarNotFound) {
              // Avatar doesn't exist yet - return empty wallets (user can create wallets later)
              console.warn('Avatar not found in database, returning empty wallets:', result.message);
              set({ 
                wallets: {}, 
                isLoading: false, 
                error: null // Don't show error for missing avatar - it's expected for new users
              });
            } else {
              set({ error: result.message || 'Failed to load wallets', isLoading: false });
            }
            return;
          }

          // Normalize providerType values in wallets (convert numeric to string enum)
          const normalizedWallets: Partial<Record<ProviderType, Wallet[]>> = {};
          const rawWallets = result.result || {};
          
          console.log('📦 Raw wallets from API:', JSON.stringify(rawWallets, null, 2));
          
          for (const [providerTypeKey, walletList] of Object.entries(rawWallets)) {
            if (Array.isArray(walletList) && walletList.length > 0) {
              console.log(`📦 Processing wallet list for key "${providerTypeKey}" (${typeof providerTypeKey}):`, walletList.length, 'wallets');
              
              // Normalize each wallet's providerType
              const normalizedList = walletList.map(wallet => {
                const originalProviderType = wallet.providerType || providerTypeKey;
                const normalizedType = normalizeProviderType(originalProviderType);
                console.log(`  🔄 Wallet ${wallet.walletId?.substring(0, 8)}: ${originalProviderType} (${typeof originalProviderType}) -> ${normalizedType}`);
                return {
                  ...wallet,
                  providerType: normalizedType,
                };
              });
              
              // Group by normalized providerType
              normalizedList.forEach(wallet => {
                const normalizedType = wallet.providerType as ProviderType;
                if (!normalizedWallets[normalizedType]) {
                  normalizedWallets[normalizedType] = [];
                }
                normalizedWallets[normalizedType]!.push(wallet);
              });
            }
          }
          
          console.log('📦 Normalized wallets:', Object.keys(normalizedWallets).map(k => `${k}: ${normalizedWallets[k as ProviderType]?.length || 0} wallets`).join(', '));
          
          set({ wallets: normalizedWallets, isLoading: false, error: null });

          // Attempt to hydrate Starknet wallets via the dedicated endpoint
          // Note: This is optional - Starknet wallets are already loaded from the main API
          // This endpoint might not exist or might be for external Starknet integration
          try {
            const starknetResult = await starknetWalletAPI.getWallets(targetId);
            if (!starknetResult.isError && starknetResult.result?.length) {
              set((state) => ({
                wallets: {
                  ...state.wallets,
                  [ProviderType.StarknetOASIS]: starknetResult.result,
                },
              }));
            }
          } catch (error) {
            // Silently ignore - Starknet wallets are already loaded from main API
            // This endpoint is optional and may not be available
          }
        } catch (error) {
          const errorMessage = error instanceof Error ? error.message : 'Failed to load wallets';
          set({ 
            error: errorMessage, 
            isLoading: false,
            wallets: {} // Clear wallets on error
          });
        }
      },

      sendTransaction: async (request: WalletTransactionRequest) => {
        set({ isLoading: true, error: null });
        try {
          const result = await oasisWalletAPI.sendToken(request);
          
          if (result.isError) {
            set({ error: result.message, isLoading: false });
            return;
          }

          // Add transaction to list
          const newTransaction = result.result;
          if (newTransaction) {
            set(state => ({
              transactions: [newTransaction, ...state.transactions],
              isLoading: false,
              error: null
            }));
            
            // Refresh wallets to update balances
            const { user } = get();
            if (user) {
              // Reload wallets after a short delay to allow transaction to process
              setTimeout(() => {
                get().loadWallets(user.id);
              }, 2000);
            }
          } else {
            set({ isLoading: false, error: 'Transaction sent but no response received' });
          }
        } catch (error) {
          set({ 
            error: error instanceof Error ? error.message : 'Failed to send transaction', 
            isLoading: false 
          });
        }
      },

      importWallet: async (request: WalletImportRequest) => {
        set({ isLoading: true, error: null });
        try {
          const { user } = get();
          if (!user) {
            set({ error: 'No user found', isLoading: false });
            return;
          }

          const result = await oasisWalletAPI.importWalletByPrivateKeyById(user.id, request);
          
          if (result.isError) {
            set({ error: result.message, isLoading: false });
            return;
          }

          // Reload wallets after import
          await get().loadWallets(user.id);
          set({ isLoading: false });
        } catch (error) {
          set({ error: 'Failed to import wallet', isLoading: false });
        }
      },

      setDefaultWallet: async (walletId: string, providerType: ProviderType) => {
        set({ isLoading: true, error: null });
        try {
          const { user } = get();
          if (!user) {
            set({ error: 'No user found', isLoading: false });
            return;
          }

          const result = await oasisWalletAPI.setDefaultWalletById(user.id, walletId, providerType);
          
          if (result.isError) {
            set({ error: result.message, isLoading: false });
            return;
          }

          // Reload wallets after setting default
          await get().loadWallets(user.id);
          set({ isLoading: false });
        } catch (error) {
          set({ error: 'Failed to set default wallet', isLoading: false });
        }
      },
    }),
    {
      name: 'oasis-wallet-storage',
      partialize: (state) => ({
        user: state.user,
        selectedWallet: state.selectedWallet,
        transactions: state.transactions,
      }),
    }
  )
); 