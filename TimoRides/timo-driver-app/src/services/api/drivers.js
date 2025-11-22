import apiClient from './client';
import { API_ENDPOINTS } from '../../utils/constants';

export const driversService = {
  // Get driver status and profile
  getStatus: async (driverId) => {
    const response = await apiClient.get(API_ENDPOINTS.DRIVER_STATUS(driverId));
    return response.data;
  },

  // Update driver availability
  updateStatus: async (driverId, statusData) => {
    const response = await apiClient.patch(
      API_ENDPOINTS.DRIVER_STATUS(driverId),
      statusData
    );
    return response.data;
  },

  // Update driver location
  updateLocation: async (driverId, locationData) => {
    const response = await apiClient.patch(
      API_ENDPOINTS.DRIVER_LOCATION(driverId),
      locationData
    );
    return response.data;
  },
};

