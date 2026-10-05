import apiClient from './client';
import { API_ENDPOINTS } from '../../utils/constants';

export const usersService = {
  // Get user profile
  getUser: async (userId) => {
    const response = await apiClient.get(API_ENDPOINTS.USER(userId));
    return response.data;
  },

  // Update user profile
  updateUser: async (userId, userData) => {
    const response = await apiClient.put(API_ENDPOINTS.USER(userId), userData);
    return response.data;
  },

  // Request payment withdrawal
  requestPayment: async (paymentData) => {
    const response = await apiClient.post(
      API_ENDPOINTS.REQUEST_PAYMENT,
      paymentData
    );
    return response.data;
  },

  // Top up wallet
  topUpWallet: async (amount, method) => {
    const response = await apiClient.post(API_ENDPOINTS.WALLET_TOPUP, {
      amount,
      method,
    });
    return response.data;
  },

  // Get wallet transactions
  getWalletTransactions: async () => {
    const response = await apiClient.get(API_ENDPOINTS.WALLET_TRANSACTIONS);
    return response.data;
  },
};

