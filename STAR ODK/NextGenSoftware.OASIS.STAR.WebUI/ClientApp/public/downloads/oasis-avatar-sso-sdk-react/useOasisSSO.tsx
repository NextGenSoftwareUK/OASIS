import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import axios, { AxiosInstance } from 'axios';

interface User {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  avatarUrl?: string;
}

interface OasisSSOContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (username: string, password: string, provider?: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<void>;
}

interface OasisSSOProviderProps {
  apiUrl: string;
  provider?: string;
  onAuthChange?: (isAuthenticated: boolean) => void;
  children: ReactNode;
}

const OasisSSOContext = createContext<OasisSSOContextType | undefined>(undefined);

export function OasisSSOProvider({ 
  apiUrl, 
  provider = 'Auto', 
  onAuthChange,
  children 
}: OasisSSOProviderProps) {
  const [user, setUser] = useState<User | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [api] = useState<AxiosInstance>(() => 
    axios.create({ baseURL: apiUrl })
  );

  useEffect(() => {
    loadAuthState();
  }, []);

  useEffect(() => {
    onAuthChange?.(isAuthenticated);
  }, [isAuthenticated, onAuthChange]);

  const loadAuthState = async () => {
    const token = localStorage.getItem('oasis_token');
    const userJson = localStorage.getItem('oasis_user');

    if (token && userJson) {
      try {
        const avatar = JSON.parse(userJson);
        api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
        
        // Verify token is still valid
        await api.get('/avatar/verify');
        
        setUser(mapAvatarToUser(avatar));
        setIsAuthenticated(true);
      } catch {
        clearAuthState();
      }
    }
    setIsLoading(false);
  };

  const login = async (username: string, password: string, selectedProvider?: string) => {
    try {
      const response = await api.post('/avatar/authenticate', {
        username,
        password,
        provider: selectedProvider || provider
      });

      if (response.data.success && response.data.token) {
        const { token, avatar } = response.data;
        
        localStorage.setItem('oasis_token', token);
        localStorage.setItem('oasis_user', JSON.stringify(avatar));
        api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
        
        setUser(mapAvatarToUser(avatar));
        setIsAuthenticated(true);
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

  const clearAuthState = () => {
    localStorage.removeItem('oasis_token');
    localStorage.removeItem('oasis_user');
    delete api.defaults.headers.common['Authorization'];
    setUser(null);
    setIsAuthenticated(false);
  };

  const mapAvatarToUser = (avatar: any): User => ({
    id: avatar.id || avatar.avatarId,
    username: avatar.username,
    email: avatar.email,
    firstName: avatar.firstName || '',
    lastName: avatar.lastName || '',
    avatarUrl: avatar.image || avatar.avatarUrl
  });

  return (
    <OasisSSOContext.Provider
      value={{
        user,
        isAuthenticated,
        isLoading,
        login,
        logout,
        refreshToken
      }}
    >
      {children}
    </OasisSSOContext.Provider>
  );
}

export function useOasisSSO(): OasisSSOContextType {
  const context = useContext(OasisSSOContext);
  if (!context) {
    throw new Error('useOasisSSO must be used within OasisSSOProvider');
  }
  return context;
}

