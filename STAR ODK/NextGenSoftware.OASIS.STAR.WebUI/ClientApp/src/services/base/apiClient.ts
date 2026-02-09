/**
 * Shared API Clients
 * Centralized axios instances with shared interceptors
 */

import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { API_CONFIG } from '../config/apiConfig';

// STAR API client (WEB5)
export const starApiClient: AxiosInstance = axios.create({
  baseURL: API_CONFIG.STAR_API_URL,
  timeout: API_CONFIG.TIMEOUT,
  headers: API_CONFIG.HEADERS,
});

// WEB4 OASIS API client (ONODE)
export const web4ApiClient: AxiosInstance = axios.create({
  baseURL: API_CONFIG.WEB4_API_URL,
  timeout: API_CONFIG.TIMEOUT,
  headers: API_CONFIG.HEADERS,
});

// OASIS Hub client
export const hubApiClient: AxiosInstance = axios.create({
  baseURL: API_CONFIG.HUB_URL,
  timeout: API_CONFIG.TIMEOUT,
  headers: API_CONFIG.HEADERS,
});

// Shared request interceptor
const setupRequestInterceptor = (client: AxiosInstance) => {
  client.interceptors.request.use(
    (config: any) => {
      // Add auth token if available
      const token = localStorage.getItem('authToken');
      if (token) {
        config.headers = {
          ...config.headers,
          Authorization: `Bearer ${token}`,
        };
      }
      
      // Add request ID for tracking
      config.headers = {
        ...config.headers,
        'X-Request-ID': `req_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      };
      
      return config;
    },
    (error) => {
      console.error('Request interceptor error:', error);
      return Promise.reject(error);
    }
  );
};

// Shared response interceptor
const setupResponseInterceptor = (client: AxiosInstance) => {
  client.interceptors.response.use(
    (response: AxiosResponse) => {
      // Log successful requests in development
      if (import.meta.env?.DEV ?? process.env.NODE_ENV === 'development') {
        console.log(`✅ ${response.config.method?.toUpperCase()} ${response.config.url} - ${response.status}`);
      }
      return response;
    },
    (error) => {
      // Handle common errors
      if (error.response?.status === 401) {
        // Unauthorized - clear auth and redirect to login
        localStorage.removeItem('authToken');
        localStorage.removeItem('avatar');
        window.location.href = '/login';
      } else if (error.response?.status === 403) {
        // Forbidden
        console.error('Access forbidden:', error.response.data);
      } else if (error.response?.status >= 500) {
        // Server error
        console.error('Server error:', error.response.data);
      } else if (error.code === 'ECONNABORTED') {
        // Timeout
        console.error('Request timeout:', error.config.url);
      } else if (!error.response) {
        // Network error
        console.error('Network error:', error.message);
      }
      
      return Promise.reject(error);
    }
  );
};

// Setup interceptors for all clients
setupRequestInterceptor(starApiClient);
setupResponseInterceptor(starApiClient);

setupRequestInterceptor(web4ApiClient);
setupResponseInterceptor(web4ApiClient);

setupRequestInterceptor(hubApiClient);
setupResponseInterceptor(hubApiClient);

// Export client types for TypeScript
export type { AxiosInstance, AxiosRequestConfig, AxiosResponse };
