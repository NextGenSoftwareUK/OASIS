import apiClient from './client';
import { API_ENDPOINTS } from '../../utils/constants';

export const authService = {
  // Login
  login: async (email, password) => {
    const response = await apiClient.post(API_ENDPOINTS.LOGIN, {
      email,
      password,
    });
    return response.data;
  },

  // Signup (Driver registration)
  signup: async (userData) => {
    const response = await apiClient.post(API_ENDPOINTS.SIGNUP, {
      ...userData,
      role: 'driver', // Ensure role is set to driver
    });
    return response.data;
  },

  // Refresh token
  refreshToken: async (refreshToken) => {
    const response = await apiClient.post(API_ENDPOINTS.REFRESH_TOKEN, {
      refreshToken,
    });
    return response.data;
  },

  // Verify token
  verifyToken: async (token) => {
    const response = await apiClient.post(API_ENDPOINTS.VERIFY_TOKEN, {
      token,
    });
    return response.data;
  },

  // Update password
  updatePassword: async (currentPassword, newPassword) => {
    const response = await apiClient.post(API_ENDPOINTS.UPDATE_PASSWORD, {
      currentPassword,
      newPassword,
    });
    return response.data;
  },
};

