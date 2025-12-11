// API Configuration
export const API_BASE_URL = __DEV__ 
  ? 'http://localhost:4205' 
  : 'https://api.timorides.com';

// API Endpoints
export const API_ENDPOINTS = {
  // Auth
  LOGIN: '/api/auth/login',
  SIGNUP: '/api/auth/signup',
  REFRESH_TOKEN: '/api/auth/refresh-token',
  VERIFY_TOKEN: '/api/auth/verify-token',
  UPDATE_PASSWORD: '/api/auth/update-password',
  
  // Driver
  DRIVER_STATUS: (driverId) => `/api/drivers/${driverId}/status`,
  DRIVER_LOCATION: (driverId) => `/api/drivers/${driverId}/location`,
  
  // Bookings
  BOOKINGS: '/api/bookings',
  BOOKING: (id) => `/api/bookings/${id}`,
  CONFIRM_ACCEPTANCE: '/api/bookings/confirm-acceptance-status',
  CANCEL_ACCEPTANCE: '/api/bookings/cancel-acceptance',
  UPDATE_PAYMENT: (id) => `/api/bookings/${id}/payment`,
  
  // Trips
  TRIPS: '/api/trips',
  CONFIRM_OTP: '/api/trips/confirm-otp',
  
  // Cars
  CARS: '/api/cars',
  CAR: (carId) => `/api/cars/${carId}`,
  CURRENT_CAR: (driverId) => `/api/cars/current-car/${driverId}`,
  
  // Users
  USER: (id) => `/api/users/${id}`,
  REQUEST_PAYMENT: '/api/users/request-payment',
  WALLET_TOPUP: '/api/users/wallet-topup',
  WALLET_TRANSACTIONS: '/api/users/wallet-transaction',
  
  // Health
  HEALTH: '/health',
};

// Storage Keys
export const STORAGE_KEYS = {
  AUTH_TOKEN: '@timo_driver:auth_token',
  REFRESH_TOKEN: '@timo_driver:refresh_token',
  USER_DATA: '@timo_driver:user_data',
  DRIVER_ID: '@timo_driver:driver_id',
};

// Booking Status
export const BOOKING_STATUS = {
  PENDING: 'pending',
  ACCEPTED: 'accepted',
  STARTED: 'started',
  COMPLETED: 'completed',
  CANCELLED: 'cancelled',
};

// Payment Methods
export const PAYMENT_METHODS = {
  CASH: 'cash',
  WALLET: 'wallet',
  CARD: 'card',
  MOBILE_MONEY: 'mobile_money',
  PAYSTACK: 'paystack',
  MPESA: 'mpesa',
};

// Location Update Interval (milliseconds)
export const LOCATION_UPDATE_INTERVAL = 5000; // 5 seconds

// Booking Poll Interval (milliseconds)
export const BOOKING_POLL_INTERVAL = 10000; // 10 seconds

