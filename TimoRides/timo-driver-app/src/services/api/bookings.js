import apiClient from './client';
import { API_ENDPOINTS } from '../../utils/constants';

export const bookingsService = {
  // Get all bookings (filtered by driver's cars)
  getAllBookings: async () => {
    const response = await apiClient.get(API_ENDPOINTS.BOOKINGS);
    return response.data;
  },

  // Get specific booking
  getBooking: async (bookingId) => {
    const response = await apiClient.get(API_ENDPOINTS.BOOKING(bookingId));
    return response.data;
  },

  // Accept booking
  acceptBooking: async (bookingId) => {
    const response = await apiClient.post(API_ENDPOINTS.CONFIRM_ACCEPTANCE, {
      bookingId,
      isAccepted: true,
    });
    return response.data;
  },

  // Decline/cancel booking
  cancelBooking: async (bookingId) => {
    const response = await apiClient.post(API_ENDPOINTS.CANCEL_ACCEPTANCE, {
      bookingId,
    });
    return response.data;
  },

  // Update payment status
  updatePayment: async (bookingId, paymentData) => {
    const response = await apiClient.patch(
      API_ENDPOINTS.UPDATE_PAYMENT(bookingId),
      paymentData
    );
    return response.data;
  },
};

