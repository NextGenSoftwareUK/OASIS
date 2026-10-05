import apiClient from './client';

// OASIS API Configuration
const OASIS_API_BASE_URL = import.meta.env.VITE_OASIS_API_URL || 'https://api.oasisplatform.io';

export const oasisService = {
  // Authenticate with OASIS Avatar
  authenticate: async (username, password) => {
    try {
      const response = await fetch(`${OASIS_API_BASE_URL}/api/auth/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          username,
          password,
        }),
      });

      if (!response.ok) {
        throw new Error('OASIS authentication failed');
      }

      const data = await response.json();
      
      if (data.success && data.jwtToken) {
        return {
          token: data.jwtToken,
          avatar: data.avatar,
        };
      }

      throw new Error(data.message || 'Authentication failed');
    } catch (error) {
      console.error('OASIS authentication error:', error);
      throw error;
    }
  },

  // Get Avatar details
  getAvatar: async (token) => {
    try {
      const response = await fetch(`${OASIS_API_BASE_URL}/api/avatar`, {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error('Failed to get avatar');
      }

      const data = await response.json();
      return data.avatar || data;
    } catch (error) {
      console.error('OASIS getAvatar error:', error);
      throw error;
    }
  },

  // Create Avatar (registration)
  createAvatar: async (avatarData) => {
    try {
      const response = await fetch(`${OASIS_API_BASE_URL}/api/avatar`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(avatarData),
      });

      if (!response.ok) {
        throw new Error('Failed to create avatar');
      }

      const data = await response.json();
      return data.avatar || data;
    } catch (error) {
      console.error('OASIS createAvatar error:', error);
      throw error;
    }
  },
};


