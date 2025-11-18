import apiClient from './client';
import { API_ENDPOINTS } from '../../utils/constants';

export const bookingsService = {
  getAllBookings: async () => {
    const response = await apiClient.get(API_ENDPOINTS.BOOKINGS);
    return response.data;
  },

  getBooking: async (bookingId) => {
    const response = await apiClient.get(API_ENDPOINTS.BOOKING(bookingId));
    return response.data;
  },

  acceptBooking: async (bookingId) => {
    const response = await apiClient.post(API_ENDPOINTS.CONFIRM_ACCEPTANCE, {
      bookingId,
      isAccepted: true,
    });
    return response.data;
  },

  cancelBooking: async (bookingId) => {
    const response = await apiClient.post(API_ENDPOINTS.CANCEL_ACCEPTANCE, {
      bookingId,
    });
    return response.data;
  },
};

