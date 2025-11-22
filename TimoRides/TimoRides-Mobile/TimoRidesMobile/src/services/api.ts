import axios, { AxiosInstance } from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';

// Backend API base URL - can be configured via environment variable
const API_BASE_URL = __DEV__
  ? 'http://localhost:4205/api' // iOS Simulator
  : 'https://ride-scheduler-be.onrender.com/api'; // Production

class ApiService {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Add request interceptor to include auth token
    this.client.interceptors.request.use(
      async (config) => {
        const token = await AsyncStorage.getItem('auth_token');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    // Add response interceptor for error handling
    this.client.interceptors.response.use(
      (response) => response,
      async (error) => {
        if (error.response?.status === 401) {
          // Token expired or invalid - clear storage and redirect to login
          await AsyncStorage.removeItem('auth_token');
          await AsyncStorage.removeItem('user');
        }
        return Promise.reject(error);
      }
    );
  }

  // Auth endpoints
  async login(phone: string): Promise<{ token: string; user: any }> {
    const response = await this.client.post('/auth/login', { phone });
    return response.data;
  }

  async sendOTP(phone: string): Promise<{ success: boolean }> {
    const response = await this.client.post('/auth/send-otp', { phone });
    return response.data;
  }

  async verifyOTP(phone: string, otp: string): Promise<{ token: string; user: any }> {
    const response = await this.client.post('/auth/verify-otp', { phone, otp });
    return response.data;
  }

  async register(email: string, password: string): Promise<{ token: string; user: any }> {
    const response = await this.client.post('/auth/register', { email, password });
    return response.data;
  }

  // Driver endpoints
  async getNearbyDrivers(latitude: number, longitude: number, radius = 5000): Promise<any[]> {
    const response = await this.client.get('/cars/proximity', {
      params: { latitude, longitude, radius },
    });
    return response.data;
  }

  // Booking endpoints
  async createBooking(data: {
    driverId: string;
    pickup: { latitude: number; longitude: number; address: string };
    destination: { latitude: number; longitude: number; address: string };
    fare: number;
    phoneNumber: string;
  }): Promise<any> {
    const response = await this.client.post('/bookings', data);
    return response.data;
  }

  async getBooking(bookingId: string): Promise<any> {
    const response = await this.client.get(`/bookings/${bookingId}`);
    return response.data;
  }

  async getBookingHistory(): Promise<any[]> {
    const response = await this.client.get('/bookings/history');
    return response.data;
  }

  async updateBookingStatus(bookingId: string, status: string): Promise<any> {
    const response = await this.client.patch(`/bookings/${bookingId}/status`, { status });
    return response.data;
  }

  // Payment endpoints
  async processPayment(bookingId: string, method: string, amount: number): Promise<any> {
    const response = await this.client.post('/payments', {
      bookingId,
      method,
      amount,
    });
    return response.data;
  }

  async getPaymentHistory(): Promise<any[]> {
    const response = await this.client.get('/payments/history');
    return response.data;
  }
}

export default new ApiService();

