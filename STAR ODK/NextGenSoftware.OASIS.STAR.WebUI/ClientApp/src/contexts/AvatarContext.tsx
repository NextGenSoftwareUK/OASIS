import React, { createContext, useState, useContext, useEffect, ReactNode } from 'react';
import { OASISAvatar, SignupData } from '../services/avatarService';
import { avatarService } from '../services/avatarService';
import { toast } from 'react-hot-toast';

interface AvatarContextType {
  currentAvatar: OASISAvatar | null;
  isLoggedIn: boolean;
  isLoading: boolean;
  signin: (username: string, password: string, provider?: string) => Promise<boolean>;
  signup: (data: SignupData) => Promise<boolean>;
  signout: () => Promise<void>;
  updateAvatar: (data: Partial<OASISAvatar>) => Promise<boolean>;
  refreshAvatar: () => Promise<void>;
}

const AvatarContext = createContext<AvatarContextType | undefined>(undefined);

export const AvatarProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [currentAvatar, setCurrentAvatar] = useState<OASISAvatar | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const isLoggedIn = currentAvatar?.isLoggedIn || false;

  // Load avatar from localStorage on mount
  useEffect(() => {
    const loadStoredAvatar = async () => {
      const stored = localStorage.getItem('oasisAvatar');
      if (stored) {
        try {
          const avatar = JSON.parse(stored);
          setCurrentAvatar(avatar);
        } catch (error) {
          console.error('Failed to load stored avatar:', error);
          localStorage.removeItem('oasisAvatar');
        }
      }
    };

    loadStoredAvatar();
  }, []);

  const signin = async (username: string, password: string, provider?: string): Promise<boolean> => {
    setIsLoading(true);
    try {
      // Only pass providerType if it's not 'Auto' or 'Default'
      const signinData: any = { username, password };
      if (provider && provider !== 'Auto' && provider !== 'Default') {
        signinData.providerType = provider;
      }
      const result = await avatarService.signin(signinData);
      
      if (result.isError || !result.result) {
        toast.error(result.message || 'Sign in failed');
        return false;
      }

      setCurrentAvatar(result.result);
      localStorage.setItem('oasisAvatar', JSON.stringify(result.result));
      toast.success(`Welcome back to The OASIS, ${result.result.firstName}! 🌟`);
      return true;
    } catch (error) {
      toast.error('Sign in failed. Please try again.');
      return false;
    } finally {
      setIsLoading(false);
    }
  };

  const signup = async (data: SignupData): Promise<boolean> => {
    setIsLoading(true);
    try {
      const result = await avatarService.signup(data);
      
      if (result.isError || !result.result) {
        toast.error(result.message || 'Sign up failed');
        return false;
      }

      setCurrentAvatar(result.result);
      localStorage.setItem('oasisAvatar', JSON.stringify(result.result));
      toast.success(`Welcome to The OASIS, ${result.result.firstName}! 🚀`);
      return true;
    } catch (error) {
      toast.error('Sign up failed. Please try again.');
      return false;
    } finally {
      setIsLoading(false);
    }
  };

  const signout = async (): Promise<void> => {
    setIsLoading(true);
    try {
      await avatarService.signout();
      setCurrentAvatar(null);
      localStorage.removeItem('oasisAvatar');
      toast.success('Signed out successfully! 👋');
    } catch (error) {
      console.error('Sign out error:', error);
      // Still clear local state even if API call fails
      setCurrentAvatar(null);
      localStorage.removeItem('oasisAvatar');
    } finally {
      setIsLoading(false);
    }
  };

  const updateAvatar = async (data: Partial<OASISAvatar>): Promise<boolean> => {
    setIsLoading(true);
    try {
      const result = await avatarService.updateAvatar(data);
      
      if (result.isError || !result.result) {
        toast.error(result.message || 'Update failed');
        return false;
      }

      setCurrentAvatar(result.result);
      localStorage.setItem('oasisAvatar', JSON.stringify(result.result));
      toast.success('Avatar updated successfully! ✨');
      return true;
    } catch (error) {
      toast.error('Update failed. Please try again.');
      return false;
    } finally {
      setIsLoading(false);
    }
  };

  const refreshAvatar = async (): Promise<void> => {
    setIsLoading(true);
    try {
      const result = await avatarService.getCurrentAvatar();
      
      if (result.isError || !result.result) {
        // If we can't get current avatar, user might be logged out
        setCurrentAvatar(null);
        localStorage.removeItem('oasisAvatar');
        return;
      }

      setCurrentAvatar(result.result);
      localStorage.setItem('oasisAvatar', JSON.stringify(result.result));
    } catch (error) {
      console.error('Refresh avatar error:', error);
      setCurrentAvatar(null);
      localStorage.removeItem('oasisAvatar');
    } finally {
      setIsLoading(false);
    }
  };

  const value: AvatarContextType = {
    currentAvatar,
    isLoggedIn,
    isLoading,
    signin,
    signup,
    signout,
    updateAvatar,
    refreshAvatar,
  };

  return (
    <AvatarContext.Provider value={value}>
      {children}
    </AvatarContext.Provider>
  );
};

export const useAvatar = () => {
  const context = useContext(AvatarContext);
  if (context === undefined) {
    throw new Error('useAvatar must be used within an AvatarProvider');
  }
  return context;
};

export default AvatarContext;
