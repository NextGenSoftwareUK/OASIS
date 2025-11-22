import apiClient from './client';
import { API_ENDPOINTS } from '../../utils/constants';

export const carsService = {
  // Get all cars (authenticated user's cars)
  getAllCars: async () => {
    const response = await apiClient.get(API_ENDPOINTS.CARS);
    return response.data;
  },

  // Get current active car
  getCurrentCar: async (driverId) => {
    const response = await apiClient.get(API_ENDPOINTS.CURRENT_CAR(driverId));
    return response.data;
  },

  // Get specific car
  getCar: async (carId) => {
    const response = await apiClient.get(API_ENDPOINTS.CAR(carId));
    return response.data;
  },

  // Create new car
  createCar: async (carData) => {
    const response = await apiClient.post(API_ENDPOINTS.CARS, carData);
    return response.data;
  },

  // Update car
  updateCar: async (driverId, carData) => {
    const response = await apiClient.put(
      `/api/cars/${driverId}`,
      carData
    );
    return response.data;
  },
};

