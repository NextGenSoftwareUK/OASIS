import React, { useEffect, useRef, useState } from 'react';
import { Box, Typography, Button } from '@mui/material';
import { LocationOn, OpenInNew } from '@mui/icons-material';
import { TimoColors } from '../utils/theme';

const MapView = ({ 
  pickupLocation, 
  destinationLocation, 
  height = '400px',
  showRoute = true 
}) => {
  const mapRef = useRef(null);
  const [map, setMap] = useState(null);
  const [directionsService, setDirectionsService] = useState(null);
  const [directionsRenderer, setDirectionsRenderer] = useState(null);
  const [isApiLoaded, setIsApiLoaded] = useState(false);
  const [apiError, setApiError] = useState(false);
  const [userLocation, setUserLocation] = useState(null);
  const [userMarker, setUserMarker] = useState(null);
  const watchIdRef = useRef(null);

  // Get Google Maps API key from environment
  const GOOGLE_MAPS_API_KEY = import.meta.env.VITE_GOOGLE_MAPS_API_KEY || '';

  // Default location (Durban, South Africa)
  const DEFAULT_CENTER = { lat: -29.8587, lng: 31.0218 };

  // Get user's current location (request early, before map loads)
  useEffect(() => {
    if (!navigator.geolocation) {
      console.warn('Geolocation is not supported by this browser');
      return;
    }

    // Request location permission and get current position
    navigator.geolocation.getCurrentPosition(
      (position) => {
        const location = {
          lat: position.coords.latitude,
          lng: position.coords.longitude,
        };
        setUserLocation(location);
      },
      (error) => {
        console.warn('Error getting user location:', error);
        // Continue without user location
      },
      {
        enableHighAccuracy: true,
        timeout: 5000,
        maximumAge: 0,
      }
    );

    // Watch position for updates (for drivers)
    watchIdRef.current = navigator.geolocation.watchPosition(
      (position) => {
        const location = {
          lat: position.coords.latitude,
          lng: position.coords.longitude,
        };
        setUserLocation(location);
      },
      (error) => {
        console.warn('Error watching user location:', error);
      },
      {
        enableHighAccuracy: true,
        timeout: 5000,
        maximumAge: 10000, // Accept cached position up to 10 seconds old
      }
    );

    return () => {
      if (watchIdRef.current !== null) {
        navigator.geolocation.clearWatch(watchIdRef.current);
      }
    };
  }, []); // Run once on mount

  // Load Google Maps JavaScript API
  useEffect(() => {
    if (!GOOGLE_MAPS_API_KEY) {
      setApiError(true);
      return;
    }

    // Check if Google Maps API is already loaded
    if (window.google && window.google.maps) {
      setIsApiLoaded(true);
      return;
    }

    // Load the script
    const script = document.createElement('script');
    script.src = `https://maps.googleapis.com/maps/api/js?key=${GOOGLE_MAPS_API_KEY}&libraries=places,directions`;
    script.async = true;
    script.defer = true;
    
    script.onload = () => {
      setIsApiLoaded(true);
    };
    
    script.onerror = () => {
      setApiError(true);
      console.error('Failed to load Google Maps API');
    };

    document.head.appendChild(script);

    return () => {
      // Cleanup: remove script if component unmounts
      const existingScript = document.querySelector(`script[src*="maps.googleapis.com"]`);
      if (existingScript) {
        // Don't remove it as other components might use it
      }
    };
  }, [GOOGLE_MAPS_API_KEY]);

  // Initialize map
  useEffect(() => {
    if (!isApiLoaded || !mapRef.current || !window.google) return;

    const google = window.google;
    
    // Use default center (Durban) - don't use user location for initial center
    // User location will be shown as a marker instead
    const initialCenter = DEFAULT_CENTER;
    const initialZoom = 14; // Good zoom level for city view
    
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

    // Initialize map with simplified style
    const mapInstance = new google.maps.Map(mapRef.current, {
      center: initialCenter,
      zoom: initialZoom,
      mapTypeControl: false, // Hide map type control
      streetViewControl: false, // Hide street view
      fullscreenControl: false, // Hide fullscreen
      zoomControl: true, // Keep zoom control
      styles: simplifiedMapStyle, // Apply simplified style
      disableDefaultUI: false, // We want zoom control
    });

    setMap(mapInstance);

    // Initialize directions service and renderer
    const dirService = new google.maps.DirectionsService();
    const dirRenderer = new google.maps.DirectionsRenderer({
      map: mapInstance,
      suppressMarkers: false,
    });

    setDirectionsService(dirService);
    setDirectionsRenderer(dirRenderer);

    // Don't auto-center on user location - keep Durban as center
    // User location will be shown as a marker
  }, [isApiLoaded, userLocation, pickupLocation, destinationLocation]);

  // Add/update user location marker
  useEffect(() => {
    if (!map || !userLocation || !window.google) return;

    const google = window.google;

    // Remove existing user marker
    if (userMarker) {
      userMarker.setMap(null);
    }

    // Create new user location marker (blue circle with pulsing effect)
    const marker = new google.maps.Marker({
      position: userLocation,
      map,
      icon: {
        path: google.maps.SymbolPath.CIRCLE,
        scale: 10,
        fillColor: '#4285F4',
        fillOpacity: 1,
        strokeColor: '#ffffff',
        strokeWeight: 3,
      },
      title: 'Your Location',
      zIndex: 1000, // Always on top
    });

    setUserMarker(marker);

    // Add pulsing circle animation
    const circle = new google.maps.Circle({
      strokeColor: '#4285F4',
      strokeOpacity: 0.8,
      strokeWeight: 2,
      fillColor: '#4285F4',
      fillOpacity: 0.2,
      map,
      center: userLocation,
      radius: 50,
    });

    // Animate the circle
    let radius = 50;
    const animateCircle = () => {
      radius = radius >= 200 ? 50 : radius + 5;
      circle.setRadius(radius);
      if (map && userLocation) {
        requestAnimationFrame(animateCircle);
      }
    };
    animateCircle();

    return () => {
      if (marker) marker.setMap(null);
      if (circle) circle.setMap(null);
    };
  }, [map, userLocation, userMarker]);

  // Update map with route or markers
  useEffect(() => {
    if (!map || !directionsService || !directionsRenderer) return;

    const google = window.google;

    // If we have both pickup and destination, show route
    if ((pickupLocation?.address || pickupLocation?.latitude) && 
        (destinationLocation?.address || destinationLocation?.latitude) && 
        showRoute) {
      // Use coordinates if available, otherwise use address
      const origin = pickupLocation.latitude && pickupLocation.longitude
        ? `${pickupLocation.latitude},${pickupLocation.longitude}`
        : pickupLocation.address;
      const destination = destinationLocation.latitude && destinationLocation.longitude
        ? `${destinationLocation.latitude},${destinationLocation.longitude}`
        : destinationLocation.address;

      const request = {
        origin,
        destination,
        travelMode: google.maps.TravelMode.DRIVING,
      };

      directionsService.route(request, (result, status) => {
        if (status === google.maps.DirectionsStatus.OK) {
          // Configure directions renderer with simplified styling
          directionsRenderer.setOptions({
            suppressMarkers: false,
            preserveViewport: false,
            polylineOptions: {
              strokeColor: TimoColors.primary,
              strokeWeight: 5,
              strokeOpacity: 0.8,
            },
          });
          
          directionsRenderer.setDirections(result);
          
          // Fit map to show entire route with padding (accounting for bottom card)
          const bounds = result.routes[0].bounds;
          // Add padding: top/right/left = 100px, bottom = 300px (for bottom card)
          map.fitBounds(bounds, { top: 100, right: 100, bottom: 300, left: 100 });
        } else {
          console.error('Directions request failed:', status);
          // Fallback: show markers
          showMarkers();
        }
      });
    } else if (pickupLocation || destinationLocation) {
      // Show markers for individual locations
      showMarkers();
    } else {
      // Default view - center on Durban with good zoom level
      map.setCenter(DEFAULT_CENTER);
      map.setZoom(14); // City-level zoom
      directionsRenderer.setDirections({ routes: [] });
      
      // If user location exists, extend bounds to include it but keep Durban centered
      if (userLocation) {
        // Simple distance calculation (Haversine formula approximation)
        const latDiff = Math.abs(userLocation.lat - DEFAULT_CENTER.lat);
        const lngDiff = Math.abs(userLocation.lng - DEFAULT_CENTER.lng);
        // Rough estimate: 1 degree ≈ 111km
        const distanceKm = Math.sqrt(latDiff * latDiff + lngDiff * lngDiff) * 111;
        
        // Only fit bounds if user is reasonably close to Durban (within ~50km)
        if (distanceKm < 50) {
          const bounds = new google.maps.LatLngBounds();
          bounds.extend(DEFAULT_CENTER);
          bounds.extend(userLocation);
          map.fitBounds(bounds);
        }
      }
    }
  }, [map, directionsService, directionsRenderer, pickupLocation, destinationLocation, showRoute, userLocation]);

  const showMarkers = () => {
    if (!map || !window.google) return;

    const google = window.google;
    const bounds = new google.maps.LatLngBounds();
    const markers = [];

    // Add pickup marker
    if (pickupLocation) {
      let position;
      if (pickupLocation.latitude && pickupLocation.longitude) {
        position = { lat: pickupLocation.latitude, lng: pickupLocation.longitude };
      } else if (pickupLocation.address) {
        // Geocode address if we only have address
        const geocoder = new google.maps.Geocoder();
        geocoder.geocode({ address: pickupLocation.address }, (results, status) => {
          if (status === 'OK' && results[0]) {
            position = results[0].geometry.location;
            const marker = new google.maps.Marker({
              position,
              map,
              label: 'P',
              title: pickupLocation.address,
              icon: {
                path: google.maps.SymbolPath.CIRCLE,
                scale: 8,
                fillColor: '#4CAF50',
                fillOpacity: 1,
                strokeColor: '#ffffff',
                strokeWeight: 2,
              },
            });
            markers.push(marker);
            bounds.extend(position);
            if (markers.length === (pickupLocation ? 1 : 0) + (destinationLocation ? 1 : 0)) {
              map.fitBounds(bounds);
            }
          }
        });
        return; // Will continue in geocode callback
      }

      if (position) {
        const marker = new google.maps.Marker({
          position,
          map,
          label: 'P',
          title: pickupLocation.address,
          icon: {
            path: google.maps.SymbolPath.CIRCLE,
            scale: 8,
            fillColor: '#4CAF50',
            fillOpacity: 1,
            strokeColor: '#ffffff',
            strokeWeight: 2,
          },
        });
        markers.push(marker);
        bounds.extend(position);
      }
    }

    // Add destination marker
    if (destinationLocation) {
      let position;
      if (destinationLocation.latitude && destinationLocation.longitude) {
        position = { lat: destinationLocation.latitude, lng: destinationLocation.longitude };
      } else if (destinationLocation.address) {
        const geocoder = new google.maps.Geocoder();
        geocoder.geocode({ address: destinationLocation.address }, (results, status) => {
          if (status === 'OK' && results[0]) {
            position = results[0].geometry.location;
            const marker = new google.maps.Marker({
              position,
              map,
              label: 'D',
              title: destinationLocation.address,
              icon: {
                path: google.maps.SymbolPath.CIRCLE,
                scale: 8,
                fillColor: '#F44336',
                fillOpacity: 1,
                strokeColor: '#ffffff',
                strokeWeight: 2,
              },
            });
            markers.push(marker);
            bounds.extend(position);
            if (markers.length === (pickupLocation ? 1 : 0) + (destinationLocation ? 1 : 0)) {
              map.fitBounds(bounds);
            }
          }
        });
        return;
      }

      if (position) {
        const marker = new google.maps.Marker({
          position,
          map,
          label: 'D',
          title: destinationLocation.address,
          icon: {
            path: google.maps.SymbolPath.CIRCLE,
            scale: 8,
            fillColor: '#F44336',
            fillOpacity: 1,
            strokeColor: '#ffffff',
            strokeWeight: 2,
          },
        });
        markers.push(marker);
        bounds.extend(position);
      }
    }

    // Fit bounds to show all markers
    if (markers.length > 0) {
      map.fitBounds(bounds);
    }
  };

  // Generate directions URL for opening in Google Maps
  const getDirectionsUrl = () => {
    if (pickupLocation?.address && destinationLocation?.address) {
      const origin = encodeURIComponent(pickupLocation.address);
      const destination = encodeURIComponent(destinationLocation.address);
      return `https://www.google.com/maps/dir/${origin}/${destination}`;
    }
    return `https://www.google.com/maps?q=${encodeURIComponent('Durban, South Africa')}`;
  };

  if (!GOOGLE_MAPS_API_KEY) {
    return (
      <Box
        sx={{
          height,
          width: '100%',
          borderRadius: 2,
          overflow: 'hidden',
          border: '2px solid rgba(40, 71, 188, 0.2)',
          background: 'linear-gradient(135deg, #E3F2FD 0%, #BBDEFB 50%, #90CAF9 100%)',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          p: 3,
        }}
      >
        <LocationOn sx={{ fontSize: 60, mb: 2, color: TimoColors.primary }} />
        <Typography variant="h6" fontWeight="bold" gutterBottom>
          Google Maps API Key Required
        </Typography>
        <Typography variant="body2" color="text.secondary" textAlign="center">
          Please add VITE_GOOGLE_MAPS_API_KEY to your .env file
        </Typography>
      </Box>
    );
  }

  if (apiError) {
    return (
      <Box
        sx={{
          height,
          width: '100%',
          borderRadius: 2,
          overflow: 'hidden',
          border: '2px solid rgba(40, 71, 188, 0.2)',
          background: 'linear-gradient(135deg, #E3F2FD 0%, #BBDEFB 50%, #90CAF9 100%)',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          p: 3,
        }}
      >
        <LocationOn sx={{ fontSize: 60, mb: 2, color: TimoColors.primary }} />
        <Typography variant="h6" fontWeight="bold" gutterBottom>
          Failed to Load Map
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2, textAlign: 'center' }}>
          Please check that Maps JavaScript API is enabled in Google Cloud Console
        </Typography>
        <Button
          variant="contained"
          startIcon={<OpenInNew />}
          href={getDirectionsUrl()}
          target="_blank"
          sx={{ bgcolor: TimoColors.primary }}
        >
          Open in Google Maps
        </Button>
      </Box>
    );
  }

  return (
    <Box
      sx={{
        height,
        width: '100%',
        borderRadius: 2,
        overflow: 'hidden',
        border: '2px solid rgba(40, 71, 188, 0.2)',
        boxShadow: '0 4px 20px rgba(0, 0, 0, 0.1)',
        position: 'relative',
      }}
    >
      <Box
        ref={mapRef}
        sx={{
          width: '100%',
          height: '100%',
        }}
      />
      {!isApiLoaded && (
        <Box
          sx={{
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            background: 'rgba(255, 255, 255, 0.9)',
            zIndex: 1000,
          }}
        >
          <Typography>Loading map...</Typography>
        </Box>
      )}
    </Box>
  );
};

export default MapView;
