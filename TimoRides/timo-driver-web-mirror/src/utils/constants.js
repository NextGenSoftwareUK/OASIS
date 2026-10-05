// API Configuration
export const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:4205';

// API Endpoints
export const API_ENDPOINTS = {
  LOGIN: '/api/auth/login',
  SIGNUP: '/api/auth/signup',
  DRIVER_STATUS: (driverId) => `/api/drivers/${driverId}/status`,
  DRIVER_LOCATION: (driverId) => `/api/drivers/${driverId}/location`,
  BOOKINGS: '/api/bookings',
  BOOKING: (id) => `/api/bookings/${id}`,
  CONFIRM_ACCEPTANCE: '/api/bookings/confirm-acceptance-status',
  CANCEL_ACCEPTANCE: '/api/bookings/cancel-acceptance',
};

// Storage Keys
export const STORAGE_KEYS = {
  AUTH_TOKEN: 'timo_driver_auth_token',
  REFRESH_TOKEN: 'timo_driver_refresh_token',
  USER_DATA: 'timo_driver_user_data',
  DRIVER_ID: 'timo_driver_id',
};

// Booking Status
export const BOOKING_STATUS = {
  PENDING: 'pending',
  ACCEPTED: 'accepted',
  STARTED: 'started',
  COMPLETED: 'completed',
  CANCELLED: 'cancelled',
};

