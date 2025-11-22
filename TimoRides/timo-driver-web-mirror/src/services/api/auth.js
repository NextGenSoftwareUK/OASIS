import apiClient from './client';
import { API_ENDPOINTS } from '../../utils/constants';

export const authService = {
  login: async (email, password) => {
    const response = await apiClient.post(API_ENDPOINTS.LOGIN, {
      email,
      password,
    });
    return response.data;
  },

  signup: async (userData) => {
    const response = await apiClient.post(API_ENDPOINTS.SIGNUP, {
      ...userData,
      role: 'driver',
    });
    return response.data;
  },
};

