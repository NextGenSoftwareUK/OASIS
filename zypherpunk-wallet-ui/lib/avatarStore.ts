'use client';

import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { avatarAPI } from './avatarApi';
import { oasisWalletAPI } from './api';
import { keysAPI } from './keysApi';
import { stablecoinAPI } from './api/stablecoinApi';
import type { AvatarAuthResponse, AvatarProfile, AvatarRegistrationRequest, User } from './types';
import { useWalletStore } from './store';
import { config } from './config';

interface AvatarStoreState {
  avatar: AvatarProfile | null;
  token: string | null;
  refreshToken: string | null;
  isAuthenticating: boolean;
  authError: string | null;
  hasHydrated: boolean;
  login: (username: string, password: string) => Promise<void>;
  register: (payload: AvatarRegistrationRequest) => Promise<void>;
  logout: () => void;
  useDemoAvatar: () => Promise<void>;
}

const mapAvatarToUser = (avatar: AvatarProfile | null): User | null => {
  if (!avatar) return null;
  const id = avatar.avatarId || avatar.id;
  if (!id) return null;
  return {
    id,
    username: avatar.username || avatar.email || 'anonymous',
    email: avatar.email,
    firstName: avatar.firstName,
    lastName: avatar.lastName,
    avatarUrl: avatar.avatarImageUrl,
    createdDate: avatar.createdDate,
    lastLoginDate: avatar.lastLoginDate,
  };
};

const applyAuthState = (auth: AvatarAuthResponse) => {
  const { avatar, jwtToken } = auth;
  const walletStore = useWalletStore.getState();
  const user = mapAvatarToUser(avatar);
  if (user) {
    walletStore.setUser(user);
  }
  console.log('Setting auth token for wallet API:', jwtToken ? `${jwtToken.substring(0, 20)}...` : 'null');
  oasisWalletAPI.setAuthToken(jwtToken);
  keysAPI.setAuthToken(jwtToken);
  stablecoinAPI.setAuthToken(jwtToken);
  console.log('Auth token set. Wallet API token:', oasisWalletAPI.getAuthToken() ? 'present' : 'missing');
};

export const useAvatarStore = create<AvatarStoreState>()(
  persist(
    (set, get) => ({
      avatar: null,
      token: null,
      refreshToken: null,
      isAuthenticating: false,
      authError: null,
      hasHydrated: false,

      login: async (username: string, password: string) => {
        set({ isAuthenticating: true, authError: null });
        try {
          const auth = await avatarAPI.login(username, password);
          applyAuthState(auth);
          set({
            avatar: auth.avatar,
            token: auth.jwtToken,
            refreshToken: auth.refreshToken || null,
            isAuthenticating: false,
            authError: null,
          });
        } catch (error) {
          console.error('Avatar login failed:', error);
          set({
            isAuthenticating: false,
            authError: error instanceof Error ? error.message : 'Unable to login avatar',
          });
          throw error;
        }
      },

      register: async (payload: AvatarRegistrationRequest) => {
        set({ isAuthenticating: true, authError: null });
        try {
          const auth = await avatarAPI.register(payload);
          
          // Privacy Mode: Try to auto-verify if verification token is available
          // This allows fake/disposable emails to work without manual verification
          if (payload.privacyMode && auth.avatar?.verificationToken) {
            try {
              const verifyResult = await avatarAPI.verifyEmail(auth.avatar.verificationToken);
              if (!verifyResult.isError) {
                console.log('Privacy mode: Avatar auto-verified');
              }
            } catch (verifyError) {
              console.warn('Privacy mode: Auto-verification failed, user may need to verify manually', verifyError);
              // Don't throw - registration succeeded, verification is optional
            }
          }
          
          applyAuthState(auth);
          set({
            avatar: auth.avatar,
            token: auth.jwtToken,
            refreshToken: auth.refreshToken || null,
            isAuthenticating: false,
            authError: null,
          });
        } catch (error) {
          console.error('Avatar registration failed:', error);
          set({
            isAuthenticating: false,
            authError: error instanceof Error ? error.message : 'Unable to register avatar',
          });
          throw error;
        }
      },

      logout: () => {
        set({
          avatar: null,
          token: null,
          refreshToken: null,
        });
        oasisWalletAPI.setAuthToken(null);
        keysAPI.setAuthToken(null);
        stablecoinAPI.setAuthToken(null);
        const walletStore = useWalletStore.getState();
        walletStore.setUser(null);
        walletStore.setWallets({});
        walletStore.setTransactions([]);
      },

      useDemoAvatar: async () => {
        const demoAvatar: AvatarProfile = {
          avatarId: config.demo.userId,
          username: 'demo.explorer',
          firstName: 'Demo',
          lastName: 'Explorer',
          trustLevel: 'silver',
          karma: 1120,
        };
        set({
          avatar: demoAvatar,
          token: null,
          refreshToken: null,
        });
        const walletStore = useWalletStore.getState();
        const user = mapAvatarToUser(demoAvatar);
        walletStore.setUser(user);
        if (demoAvatar.avatarId) {
          walletStore.loadWallets(demoAvatar.avatarId);
        }
      },
    }),
    {
      name: 'oasis-avatar-auth',
    }
  )
);

// Set hasHydrated immediately if persist is not available
if (typeof window !== 'undefined') {
  // Check if we're in browser
  const checkHydration = () => {
    const state = useAvatarStore.getState();
    if (!state.hasHydrated) {
      const { avatar, token } = state;
          console.log('Hydrating auth state. Token present:', !!token, token ? `${token.substring(0, 20)}...` : 'none');
      if (token) {
        oasisWalletAPI.setAuthToken(token);
        keysAPI.setAuthToken(token);
        stablecoinAPI.setAuthToken(token);
            console.log('Restored auth token to wallet API');
      } else {
        oasisWalletAPI.setAuthToken(null);
        keysAPI.setAuthToken(null);
        stablecoinAPI.setAuthToken(null);
            console.log('No token to restore');
      }

      const user = mapAvatarToUser(avatar);
      useWalletStore.getState().setUser(user);
      useAvatarStore.setState({ hasHydrated: true });
    }
  };

  // Try immediately
  checkHydration();

  // Also set up the persist callback
  useAvatarStore.persist?.onFinish?.(() => {
    checkHydration();
  });

  // Fallback: set hydrated after a short delay if not set
  setTimeout(() => {
    const state = useAvatarStore.getState();
    if (!state.hasHydrated) {
      useAvatarStore.setState({ hasHydrated: true });
    }
  }, 100);
}

