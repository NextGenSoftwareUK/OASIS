import { ref, readonly, computed } from 'vue';
import type { Ref, ComputedRef, App } from 'vue';
import axios, { AxiosInstance } from 'axios';

interface User {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  avatarUrl?: string;
}

interface OasisConfig {
  apiUrl: string;
  provider?: string;
  onAuthChange?: (isAuthenticated: boolean) => void;
}

interface OasisSSOComposable {
  user: Readonly<Ref<User | null>>;
  isAuthenticated: Readonly<Ref<boolean>>;
  isLoading: Readonly<Ref<boolean>>;
  login: (username: string, password: string, provider?: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<void>;
}

// Global state
const user = ref<User | null>(null);
const isAuthenticated = ref(false);
const isLoading = ref(true);
let api: AxiosInstance;
let config: OasisConfig;

export function useOasisSSO(): OasisSSOComposable {
  const login = async (username: string, password: string, provider?: string) => {
    try {
      const response = await api.post('/avatar/authenticate', {
        username,
        password,
        provider: provider || config.provider || 'Auto'
      });

      if (response.data.success && response.data.token) {
        const { token, avatar } = response.data;
        
        localStorage.setItem('oasis_token', token);
        localStorage.setItem('oasis_user', JSON.stringify(avatar));
        api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
        
        user.value = mapAvatarToUser(avatar);
        isAuthenticated.value = true;
        
        config.onAuthChange?.(true);
      } else {
        throw new Error(response.data.message || 'Login failed');
      }
    } catch (error) {
      console.error('Login error:', error);
      throw error;
    }
  };

  const logout = async () => {
    try {
      await api.post('/avatar/logout');
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      clearAuthState();
    }
  };

  const refreshToken = async () => {
    try {
      const response = await api.post('/avatar/refresh');
      
      if (response.data.token) {
        localStorage.setItem('oasis_token', response.data.token);
        api.defaults.headers.common['Authorization'] = `Bearer ${response.data.token}`;
      }
    } catch (error) {
      console.error('Token refresh error:', error);
      clearAuthState();
      throw error;
    }
  };

  return {
    user: readonly(user),
    isAuthenticated: readonly(isAuthenticated),
    isLoading: readonly(isLoading),
    login,
    logout,
    refreshToken
  };
}

async function loadAuthState() {
  const token = localStorage.getItem('oasis_token');
  const userJson = localStorage.getItem('oasis_user');

  if (token && userJson) {
    try {
      const avatar = JSON.parse(userJson);
      api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      
      // Verify token is still valid
      await api.get('/avatar/verify');
      
      user.value = mapAvatarToUser(avatar);
      isAuthenticated.value = true;
    } catch {
      clearAuthState();
    }
  }
  isLoading.value = false;
}

function clearAuthState() {
  localStorage.removeItem('oasis_token');
  localStorage.removeItem('oasis_user');
  delete api.defaults.headers.common['Authorization'];
  user.value = null;
  isAuthenticated.value = false;
  config.onAuthChange?.(false);
}

function mapAvatarToUser(avatar: any): User {
  return {
    id: avatar.id || avatar.avatarId,
    username: avatar.username,
    email: avatar.email,
    firstName: avatar.firstName || '',
    lastName: avatar.lastName || '',
    avatarUrl: avatar.image || avatar.avatarUrl
  };
}

// Vue plugin
export const OasisSSOPlugin = {
  install(app: App, options: OasisConfig) {
    config = options;
    api = axios.create({ baseURL: options.apiUrl });
    
    // Load auth state on install
    loadAuthState();
    
    // Provide globally
    app.provide('oasisSSO', useOasisSSO());
  }
};

export default OasisSSOPlugin;


