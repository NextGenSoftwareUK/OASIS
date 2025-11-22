import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { Wallet, Transaction, WalletTransactionRequest, WalletImportRequest, ProviderType, User, WalletStore } from './types';
import { oasisWalletAPI } from './api';

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
            
            if (isApiUnavailable) {
              set({ 
                error: 'API is currently unavailable. The OASIS API may be blocked or unreachable. Please check your connection or try using a local API server.', 
                isLoading: false,
                wallets: {} // Clear wallets on API error
              });
            } else {
              set({ error: result.message || 'Failed to load wallets', isLoading: false });
            }
            return;
          }

          set({ wallets: result.result || {}, isLoading: false, error: null });
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