import apiClient from './client';
import { API_ENDPOINTS } from '../../utils/constants';

export const tripsService = {
  // Get all trips (OTP info for driver's bookings)
  getAllTrips: async () => {
    const response = await apiClient.get(API_ENDPOINTS.TRIPS);
    return response.data;
  },

  // Confirm OTP (start or end trip)
  confirmOtp: async (bookingId, otpCode, tripType) => {
    const response = await apiClient.post(API_ENDPOINTS.CONFIRM_OTP, {
      bookingId,
      otpCode,
      tripType, // 'startTrip' or 'endTrip'
    });
    return response.data;
  },
};

