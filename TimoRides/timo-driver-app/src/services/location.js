import * as Location from 'expo-location';
import { Platform } from 'react-native';
import { driversService } from './api/drivers';
import { LOCATION_UPDATE_INTERVAL } from '../utils/constants';

let locationSubscription = null;
let locationUpdateInterval = null;

export const locationService = {
  // Request location permissions
  requestPermissions: async () => {
    try {
      const { status } = await Location.requestForegroundPermissionsAsync();
      if (status !== 'granted') {
        throw new Error('Location permission not granted');
      }
      
      // Request background location for Android
      if (Platform.OS === 'android') {
        const bgStatus = await Location.requestBackgroundPermissionsAsync();
        if (bgStatus.status !== 'granted') {
          console.warn('Background location permission not granted');
        }
      }
      
      return true;
    } catch (error) {
      console.error('Error requesting location permissions:', error);
      return false;
    }
  },

  // Start location tracking
  startTracking: (driverId, onLocationUpdate) => {
    if (locationSubscription) {
      console.warn('Location tracking already started');
      return;
    }

    const startUpdates = async () => {
      try {
        // Get current location
        const location = await Location.getCurrentPositionAsync({
          accuracy: Location.Accuracy.High,
        });

        if (onLocationUpdate) {
          onLocationUpdate(location);
        }

        // Update location to backend
        if (driverId) {
          await driversService.updateLocation(driverId, {
            latitude: location.coords.latitude,
            longitude: location.coords.longitude,
            bearing: location.coords.heading || 0,
            speed: location.coords.speed || 0,
          });
        }

        // Set up interval for location updates
        locationUpdateInterval = setInterval(async () => {
          try {
            const currentLocation = await Location.getCurrentPositionAsync({
              accuracy: Location.Accuracy.High,
            });

            if (onLocationUpdate) {
              onLocationUpdate(currentLocation);
            }

            if (driverId) {
              await driversService.updateLocation(driverId, {
                latitude: currentLocation.coords.latitude,
                longitude: currentLocation.coords.longitude,
                bearing: currentLocation.coords.heading || 0,
                speed: currentLocation.coords.speed || 0,
              });
            }
          } catch (error) {
            console.error('Error updating location:', error);
          }
        }, LOCATION_UPDATE_INTERVAL);
      } catch (error) {
        console.error('Error starting location tracking:', error);
      }
    };

    startUpdates();
  },

  // Stop location tracking
  stopTracking: () => {
    if (locationUpdateInterval) {
      clearInterval(locationUpdateInterval);
      locationUpdateInterval = null;
    }
    if (locationSubscription) {
      locationSubscription.remove();
      locationSubscription = null;
    }
  },

  // Get current location once
  getCurrentLocation: async () => {
    try {
      const location = await Location.getCurrentPositionAsync({
        accuracy: Location.Accuracy.High,
      });
      return location;
    } catch (error) {
      console.error('Error getting current location:', error);
      throw error;
    }
  },
};

