import apiClient from './client';
import { API_ENDPOINTS } from '../../utils/constants';

export const driversService = {
  getStatus: async (driverId) => {
    const response = await apiClient.get(API_ENDPOINTS.DRIVER_STATUS(driverId));
    return response.data;
  },

  updateStatus: async (driverId, statusData) => {
    const response = await apiClient.patch(
      API_ENDPOINTS.DRIVER_STATUS(driverId),
      statusData
    );
    return response.data;
  },

  updateLocation: async (driverId, locationData) => {
    const response = await apiClient.patch(
      API_ENDPOINTS.DRIVER_LOCATION(driverId),
      locationData
    );
    return response.data;
  },
};

