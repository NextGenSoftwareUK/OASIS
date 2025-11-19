import React, { useEffect, useRef, useState } from 'react';
import { StyleSheet, View } from 'react-native';
import MapView, { Marker, Polyline, PROVIDER_GOOGLE } from 'react-native-maps';
import * as Location from 'expo-location';
import { TimoColors } from '../utils/theme';

// Simplified map style (Uber-like minimal design)
const simplifiedMapStyle = [
  {
    featureType: 'poi',
    elementType: 'labels',
    stylers: [{ visibility: 'off' }],
  },
  {
    featureType: 'poi.business',
    stylers: [{ visibility: 'off' }],
  },
  {
    featureType: 'transit',
    elementType: 'labels',
    stylers: [{ visibility: 'off' }],
  },
  {
    featureType: 'road',
    elementType: 'labels.icon',
    stylers: [{ visibility: 'off' }],
  },
  {
    featureType: 'administrative',
    elementType: 'labels.text.fill',
    stylers: [{ color: '#6b6b6b' }],
  },
  {
    featureType: 'road',
    elementType: 'geometry',
    stylers: [{ color: '#ffffff' }],
  },
  {
    featureType: 'road',
    elementType: 'geometry.stroke',
    stylers: [{ color: '#e0e0e0' }, { weight: 1 }],
  },
  {
    featureType: 'water',
    elementType: 'geometry',
    stylers: [{ color: '#e8f4f8' }],
  },
  {
    featureType: 'landscape',
    elementType: 'geometry',
    stylers: [{ color: '#f5f5f5' }],
  },
];

const DEFAULT_CENTER = {
  latitude: -29.8587,
  longitude: 31.0218,
  latitudeDelta: 0.05,
  longitudeDelta: 0.05,
};

const MapViewComponent = ({
  pickupLocation,
  destinationLocation,
  showRoute = false,
  showUserLocation = true,
  style,
  onMapReady,
}) => {
  const mapRef = useRef(null);
  const [userLocation, setUserLocation] = useState(null);
  const [routeCoordinates, setRouteCoordinates] = useState([]);
  const [region, setRegion] = useState(DEFAULT_CENTER);

  // Get user's current location
  useEffect(() => {
    if (!showUserLocation) return;

    (async () => {
      const { status } = await Location.requestForegroundPermissionsAsync();
      if (status !== 'granted') {
        console.warn('Location permission not granted');
        return;
      }

      try {
        const location = await Location.getCurrentPositionAsync({
          accuracy: Location.Accuracy.High,
        });
        const coords = {
          latitude: location.coords.latitude,
          longitude: location.coords.longitude,
        };
        setUserLocation(coords);
      } catch (error) {
        console.warn('Error getting location:', error);
      }
    })();
  }, [showUserLocation]);

  // Calculate route and fit bounds
  useEffect(() => {
    if (!pickupLocation || !destinationLocation || !showRoute) {
      // If no route, center on default or user location
      if (userLocation) {
        setRegion({
          ...userLocation,
          latitudeDelta: 0.02,
          longitudeDelta: 0.02,
        });
      }
      return;
    }

    const pickup = {
      latitude: pickupLocation.latitude || pickupLocation.lat,
      longitude: pickupLocation.longitude || pickupLocation.lng,
    };
    const destination = {
      latitude: destinationLocation.latitude || destinationLocation.lat,
      longitude: destinationLocation.longitude || destinationLocation.lng,
    };

    // Simple straight-line route (for demo - in production, use Directions API)
    setRouteCoordinates([pickup, destination]);

    // Fit bounds to show both locations
    const minLat = Math.min(pickup.latitude, destination.latitude);
    const maxLat = Math.max(pickup.latitude, destination.latitude);
    const minLng = Math.min(pickup.longitude, destination.longitude);
    const maxLng = Math.max(pickup.longitude, destination.longitude);

    const latDelta = (maxLat - minLat) * 1.5; // Add padding
    const lngDelta = (maxLng - minLng) * 1.5;

    const newRegion = {
      latitude: (minLat + maxLat) / 2,
      longitude: (minLng + maxLng) / 2,
      latitudeDelta: Math.max(latDelta, 0.01),
      longitudeDelta: Math.max(lngDelta, 0.01),
    };

    setRegion(newRegion);

    // Animate to region
    if (mapRef.current) {
      setTimeout(() => {
        mapRef.current?.animateToRegion(newRegion, 1000);
      }, 100);
    }
  }, [pickupLocation, destinationLocation, showRoute, userLocation]);

  return (
    <View style={[styles.container, style]}>
      <MapView
        ref={mapRef}
        provider={PROVIDER_GOOGLE}
        style={styles.map}
        customMapStyle={simplifiedMapStyle}
        initialRegion={DEFAULT_CENTER}
        region={region}
        showsUserLocation={showUserLocation}
        showsMyLocationButton={false}
        showsCompass={false}
        showsScale={false}
        toolbarEnabled={false}
        onMapReady={onMapReady}
      >
        {/* User Location Marker */}
        {userLocation && showUserLocation && (
          <Marker
            coordinate={userLocation}
            title="Your Location"
            pinColor={TimoColors.primary}
          />
        )}

        {/* Pickup Marker */}
        {pickupLocation && (
          <Marker
            coordinate={{
              latitude: pickupLocation.latitude || pickupLocation.lat,
              longitude: pickupLocation.longitude || pickupLocation.lng,
            }}
            title="Pickup"
            pinColor={TimoColors.primary}
          />
        )}

        {/* Destination Marker */}
        {destinationLocation && (
          <Marker
            coordinate={{
              latitude: destinationLocation.latitude || destinationLocation.lat,
              longitude: destinationLocation.longitude || destinationLocation.lng,
            }}
            title="Destination"
            pinColor={TimoColors.error}
          />
        )}

        {/* Route Polyline */}
        {showRoute && routeCoordinates.length > 0 && (
          <Polyline
            coordinates={routeCoordinates}
            strokeColor={TimoColors.primary}
            strokeWidth={5}
            lineDashPattern={[0]}
          />
        )}
      </MapView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  map: {
    flex: 1,
  },
});

export default MapViewComponent;

