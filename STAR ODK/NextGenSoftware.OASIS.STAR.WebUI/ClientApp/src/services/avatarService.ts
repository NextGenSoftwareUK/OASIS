import axios from 'axios';

// WEB4 OASIS API Configuration
const WEB4_API_BASE_URL = process.env.REACT_APP_WEB4_API_URL || 'http://localhost:50564/api';

const web4Api = axios.create({
  baseURL: WEB4_API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Helper function to check demo mode
const isDemoMode = () => {
  const saved = localStorage.getItem('demoMode');
  const result = saved ? JSON.parse(saved) : true;
  console.log('avatarService isDemoMode check:', { saved, result, type: typeof result });
  return result;
};

export interface OASISAvatar {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  avatar: string;
  isLoggedIn: boolean;
  createdAt?: string;
  lastLogin?: string;
  permissions?: string[];
  karma?: number;
  level?: number;
}

export interface OASISResult<T> {
  isError: boolean;
  message: string;
  result: T | null;
}

export interface SignupData {
  title?: string;
  firstName: string;
  lastName: string;
  avatarType: string;
  email: string;
  username: string;
  password: string;
  confirmPassword: string;
  acceptTerms: boolean;
}

export interface SigninData {
  username: string;
  password: string;
}

export const avatarService = {
  // Avatar Authentication
  async signup(data: SignupData): Promise<OASISResult<OASISAvatar>> {
    console.log('avatarService.signup called, isDemoMode():', isDemoMode());
    console.log('WEB4_API_BASE_URL:', WEB4_API_BASE_URL);
    
    if (isDemoMode()) {
      // Demo mode - simulate successful signup
      console.log('Demo mode - creating mock avatar');
      const mockAvatar: OASISAvatar = {
        id: `avatar-${Date.now()}`,
        username: data.username,
        email: data.email,
        firstName: data.firstName,
        lastName: data.lastName,
        avatar: `https://api.dicebear.com/7.x/avataaars/svg?seed=${data.username}`,
        isLoggedIn: true,
        createdAt: new Date().toISOString(),
        karma: 0,
        level: 1,
        permissions: ['user'],
      };
      
      return {
        isError: false,
        message: 'Avatar created successfully (Demo Mode)',
        result: mockAvatar,
      };
    }

    console.log('Live mode - making API call to:', WEB4_API_BASE_URL + '/avatar/register');
    console.log('Sending data to API:', data);
    try {
      const response = await web4Api.post('/avatar/register', data);
      console.log('Avatar register API response:', response.data);
      return response.data;
    } catch (error) {
      console.error('Avatar signup API failed:', error);
      console.error('Error details:', {
        message: error instanceof Error ? error.message : 'Unknown error',
        code: (error as any)?.code,
        response: (error as any)?.response?.data,
        status: (error as any)?.response?.status,
      });
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Failed to create avatar',
        result: null,
      };
    }
  },

  async signin(data: SigninData): Promise<OASISResult<OASISAvatar>> {
    if (isDemoMode()) {
      // Demo mode - simulate successful signin
      const mockAvatar: OASISAvatar = {
        id: 'avatar-demo-123',
        username: data.username,
        email: `${data.username}@demo.oasis.com`,
        firstName: 'Demo',
        lastName: 'User',
        avatar: `https://api.dicebear.com/7.x/avataaars/svg?seed=${data.username}`,
        isLoggedIn: true,
        lastLogin: new Date().toISOString(),
        karma: 150,
        level: 3,
        permissions: ['user', 'demo'],
      };
      
      return {
        isError: false,
        message: 'Avatar signed in successfully (Demo Mode)',
        result: mockAvatar,
      };
    }

    try {
      const response = await web4Api.post('/avatar/signin', data);
      return response.data;
    } catch (error) {
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Failed to sign in',
        result: null,
      };
    }
  },

  async signout(): Promise<OASISResult<boolean>> {
    if (isDemoMode()) {
      return {
        isError: false,
        message: 'Avatar signed out successfully (Demo Mode)',
        result: true,
      };
    }

    try {
      const response = await web4Api.post('/avatar/signout');
      return response.data;
    } catch (error) {
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Failed to sign out',
        result: false,
      };
    }
  },

  async getCurrentAvatar(): Promise<OASISResult<OASISAvatar>> {
    if (isDemoMode()) {
      // Demo mode - return mock avatar
      const mockAvatar: OASISAvatar = {
        id: 'avatar-demo-123',
        username: 'demo_user',
        email: 'demo@oasis.com',
        firstName: 'Demo',
        lastName: 'User',
        avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=demo',
        isLoggedIn: true,
        karma: 150,
        level: 3,
        permissions: ['user', 'demo'],
      };
      
      return {
        isError: false,
        message: 'Current avatar retrieved (Demo Mode)',
        result: mockAvatar,
      };
    }

    try {
      const response = await web4Api.get('/avatar/current');
      return response.data;
    } catch (error) {
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Failed to get current avatar',
        result: null,
      };
    }
  },

  async updateAvatar(data: Partial<OASISAvatar>): Promise<OASISResult<OASISAvatar>> {
    if (isDemoMode()) {
      // Demo mode - simulate update
      const updatedAvatar: OASISAvatar = {
        id: 'avatar-demo-123',
        username: data.username || 'demo_user',
        email: data.email || 'demo@oasis.com',
        firstName: data.firstName || 'Demo',
        lastName: data.lastName || 'User',
        avatar: data.avatar || 'https://api.dicebear.com/7.x/avataaars/svg?seed=demo',
        isLoggedIn: true,
        karma: 150,
        level: 3,
        permissions: ['user', 'demo'],
      };
      
      return {
        isError: false,
        message: 'Avatar updated successfully (Demo Mode)',
        result: updatedAvatar,
      };
    }

    try {
      const response = await web4Api.put('/avatar/update', data);
      return response.data;
    } catch (error) {
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Failed to update avatar',
        result: null,
      };
    }
  },

  // Avatar Management
  async getAvatarById(id: string): Promise<OASISResult<OASISAvatar>> {
    if (isDemoMode()) {
      const mockAvatar: OASISAvatar = {
        id,
        username: 'demo_user',
        email: 'demo@oasis.com',
        firstName: 'Demo',
        lastName: 'User',
        avatar: `https://api.dicebear.com/7.x/avataaars/svg?seed=${id}`,
        isLoggedIn: true,
        karma: 150,
        level: 3,
        permissions: ['user', 'demo'],
      };
      
      return {
        isError: false,
        message: 'Avatar retrieved (Demo Mode)',
        result: mockAvatar,
      };
    }

    try {
      const response = await web4Api.get(`/avatar/${id}`);
      return response.data;
    } catch (error) {
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Failed to get avatar',
        result: null,
      };
    }
  },

  async searchAvatars(query: string): Promise<OASISResult<OASISAvatar[]>> {
    if (isDemoMode()) {
      const mockAvatars: OASISAvatar[] = [
        {
          id: 'avatar-1',
          username: 'john_doe',
          email: 'john@oasis.com',
          firstName: 'John',
          lastName: 'Doe',
          avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=john',
          isLoggedIn: false,
          karma: 250,
          level: 5,
          permissions: ['user'],
        },
        {
          id: 'avatar-2',
          username: 'jane_smith',
          email: 'jane@oasis.com',
          firstName: 'Jane',
          lastName: 'Smith',
          avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=jane',
          isLoggedIn: false,
          karma: 180,
          level: 4,
          permissions: ['user'],
        },
      ];
      
      return {
        isError: false,
        message: 'Avatars found (Demo Mode)',
        result: mockAvatars,
      };
    }

    try {
      const response = await web4Api.get(`/avatar/search?q=${encodeURIComponent(query)}`);
      return response.data;
    } catch (error) {
      return {
        isError: true,
        message: error instanceof Error ? error.message : 'Failed to search avatars',
        result: null,
      };
    }
  },
};

export default avatarService;
