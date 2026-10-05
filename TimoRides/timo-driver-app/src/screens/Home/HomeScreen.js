import React, { useEffect, useState } from 'react';
import {
  View,
  StyleSheet,
  Dimensions,
  TouchableOpacity,
} from 'react-native';
import { Text, Button, Card, FAB } from 'react-native-paper';
import MapView, { Marker } from 'react-native-maps';
import { useDispatch, useSelector } from 'react-redux';
import { useNavigation } from '@react-navigation/native';
import { getDriverStatus, updateDriverStatus } from '../../store/slices/driverSlice';
import { fetchBookings, addSimulatedBooking, setActiveBooking, removePendingBooking, updateBookingStatus } from '../../store/slices/bookingSlice';
import { TimoColors, Spacing, BorderRadius, GlowStyles } from '../../utils/theme';
import { BOOKING_POLL_INTERVAL } from '../../utils/constants';

const { width, height } = Dimensions.get('window');

const HomeScreen = () => {
  const dispatch = useDispatch();
  const navigation = useNavigation();
  const { driverId, user } = useSelector((state) => state.auth);
  const { isOnline, isOffline, car, location } = useSelector((state) => state.driver);
  const { pendingBookings, activeBooking } = useSelector((state) => state.bookings);

  const [mapRegion, setMapRegion] = useState({
    latitude: -29.8587,
    longitude: 31.0218,
    latitudeDelta: 0.0922,
    longitudeDelta: 0.0421,
  });

  useEffect(() => {
    if (driverId) {
      dispatch(getDriverStatus(driverId));
      dispatch(fetchBookings());
    }
  }, [driverId, dispatch]);

  // Poll for new bookings when online
  useEffect(() => {
    if (!isOnline || !driverId) return;

    const interval = setInterval(() => {
      dispatch(fetchBookings());
    }, BOOKING_POLL_INTERVAL);

    return () => clearInterval(interval);
  }, [isOnline, driverId, dispatch]);

  const handleToggleAvailability = async () => {
    if (driverId) {
      await dispatch(updateDriverStatus({
        driverId,
        statusData: {
          isOffline: !isOffline,
          isActive: !isOffline,
        },
      }));
    }
  };

  const handleRideRequest = (booking) => {
    navigation.navigate('RideRequest', { booking });
  };

  const handleSimulateRideRequest = () => {
    // Durban area locations with proper coordinates
    const locations = [
      { 
        pickup: 'Durban Central', 
        pickupLat: -29.8587, 
        pickupLng: 31.0218,
        destination: 'Umhlanga Beach', 
        destLat: -29.7284, 
        destLng: 31.0819,
        amount: 250 
      },
      { 
        pickup: 'Umhlanga', 
        pickupLat: -29.7284, 
        pickupLng: 31.0819,
        destination: 'Durban Airport', 
        destLat: -29.6144, 
        destLng: 31.1197,
        amount: 320 
      },
      { 
        pickup: 'Gateway Mall', 
        pickupLat: -29.7284, 
        pickupLng: 31.0819,
        destination: 'Durban Central', 
        destLat: -29.8587, 
        destLng: 31.0218,
        amount: 180 
      },
      { 
        pickup: 'Berea', 
        pickupLat: -29.8500, 
        pickupLng: 30.9900,
        destination: 'Glenwood', 
        destLat: -29.8700, 
        destLng: 31.0000,
        amount: 150 
      },
      { 
        pickup: 'Morningside', 
        pickupLat: -29.8300, 
        pickupLng: 31.0100,
        destination: 'Westville', 
        destLat: -29.8200, 
        destLng: 30.9300,
        amount: 200 
      },
    ];
    
    const randomLocation = locations[Math.floor(Math.random() * locations.length)];
    const riders = ['Alvin Armstrong', 'Sarah Johnson', 'Mike Thompson', 'Emma Davis', 'John Smith'];
    const randomRider = riders[Math.floor(Math.random() * riders.length)];
    
    // Calculate distance and duration based on coordinates
    const latDiff = Math.abs(randomLocation.destLat - randomLocation.pickupLat);
    const lngDiff = Math.abs(randomLocation.destLng - randomLocation.pickupLng);
    const distanceKm = Math.sqrt(latDiff * latDiff + lngDiff * lngDiff) * 111; // Rough conversion
    const durationMin = Math.round(distanceKm * 2); // Rough estimate: 2 min per km
    
    // Generate booking ID
    const bookingId = `sim-${Date.now()}`;
    
    const mockBooking = {
      id: bookingId,
      status: 'pending',
      fullName: randomRider,
      phoneNumber: `+27 82 ${Math.floor(Math.random() * 9000) + 1000} ${Math.floor(Math.random() * 9000) + 1000}`,
      sourceLocation: {
        address: randomLocation.pickup,
        latitude: randomLocation.pickupLat,
        longitude: randomLocation.pickupLng,
      },
      destinationLocation: {
        address: randomLocation.destination,
        latitude: randomLocation.destLat,
        longitude: randomLocation.destLng,
      },
      tripAmount: randomLocation.amount,
      tripDistance: `${distanceKm.toFixed(1)} km`,
      tripDuration: `${durationMin} min`,
      passengers: Math.floor(Math.random() * 3) + 1,
      isCash: Math.random() > 0.5,
      currency: {
        symbol: 'R',
        code: 'ZAR',
      },
      createdAt: new Date().toISOString(),
    };

    dispatch(addSimulatedBooking(mockBooking));
    
    // Navigate to the ride request screen after a short delay
    setTimeout(() => {
      navigation.navigate('RideRequest', { booking: mockBooking });
    }, 500);
  };

  return (
    <View style={styles.container}>
      {/* Map View */}
      <MapView
        style={styles.map}
        region={mapRegion}
        onRegionChangeComplete={setMapRegion}
      >
        {location && (
          <Marker
            coordinate={{
              latitude: location.latitude,
              longitude: location.longitude,
            }}
            title="Your Location"
            pinColor={isOnline ? TimoColors.online : TimoColors.offline}
          />
        )}
      </MapView>

      {/* Header Info */}
      <View style={styles.header}>
        <Card style={styles.headerCard}>
          <Card.Content style={styles.headerContent}>
            <View>
              <Text variant="titleMedium" style={styles.greeting}>
                Welcome, {user?.fullName || 'Driver'}
              </Text>
              <Text variant="bodySmall" style={styles.statusText}>
                {isOnline ? '🟢 Online' : '⚫ Offline'}
              </Text>
            </View>
            <TouchableOpacity
              onPress={() => navigation.navigate('Profile')}
              style={styles.avatar}
            >
              <Text style={styles.avatarText}>
                {user?.fullName?.charAt(0) || 'D'}
              </Text>
            </TouchableOpacity>
          </Card.Content>
        </Card>
      </View>

      {/* Pending Ride Requests */}
      {pendingBookings.length > 0 && (
        <View style={styles.requestsContainer}>
          {pendingBookings.slice(0, 2).map((booking) => (
            <Card
              key={booking.id}
              style={styles.requestCard}
              onPress={() => handleRideRequest(booking)}
            >
              <Card.Content>
                <Text variant="titleSmall" style={styles.requestTitle}>
                  New Ride Request
                </Text>
                <Text variant="bodyMedium">
                  {booking.sourceLocation?.address || 'Pickup location'}
                </Text>
                <Text variant="bodySmall" style={styles.fareText}>
                  {booking.currency?.symbol || 'R'} {booking.tripAmount || '0.00'}
                </Text>
              </Card.Content>
            </Card>
          ))}
        </View>
      )}

      {/* Active Ride Banner */}
      {activeBooking && (
        <Card
          style={styles.activeRideCard}
          onPress={() => navigation.navigate('ActiveRide', { booking: activeBooking })}
        >
          <Card.Content>
            <Text variant="titleMedium" style={styles.activeRideTitle}>
              Active Ride
            </Text>
            <Text variant="bodyMedium">
              {activeBooking.destinationLocation?.address || 'Destination'}
            </Text>
          </Card.Content>
        </Card>
      )}

      {/* Availability Toggle */}
      <View style={styles.bottomContainer}>
        <Button
          mode="contained"
          onPress={handleToggleAvailability}
          style={[
            styles.availabilityButton,
            isOnline ? GlowStyles.online : {},
          ]}
          buttonColor={isOnline ? TimoColors.online : TimoColors.offline}
          textColor={TimoColors.white}
        >
          {isOnline ? 'GO OFFLINE' : 'GO ONLINE'}
        </Button>
      </View>

      {/* Simulate Ride Button (for testing) */}
      <FAB
        icon="plus"
        style={[styles.fab, styles.simulateFab]}
        label="Simulate Ride"
        onPress={handleSimulateRideRequest}
        color={TimoColors.white}
      />

      {/* Quick Actions FAB */}
      <FAB
        icon="menu"
        style={styles.fab}
        onPress={() => {
          // Open menu/drawer
        }}
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: TimoColors.background,
  },
  map: {
    width: width,
    height: height,
  },
  header: {
    position: 'absolute',
    top: Spacing.lg,
    left: Spacing.md,
    right: Spacing.md,
  },
  headerCard: {
    borderRadius: BorderRadius.lg,
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    ...GlowStyles.primary,
  },
  headerContent: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  greeting: {
    fontWeight: 'bold',
    color: TimoColors.textMain,
  },
  statusText: {
    color: TimoColors.textMid,
    marginTop: Spacing.xs,
  },
  avatar: {
    width: 50,
    height: 50,
    borderRadius: 25,
    backgroundColor: TimoColors.primary,
    justifyContent: 'center',
    alignItems: 'center',
  },
  avatarText: {
    color: TimoColors.white,
    fontSize: 20,
    fontWeight: 'bold',
  },
  requestsContainer: {
    position: 'absolute',
    top: 120,
    left: Spacing.md,
    right: Spacing.md,
  },
  requestCard: {
    marginBottom: Spacing.sm,
    borderRadius: BorderRadius.lg,
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    ...GlowStyles.accent,
  },
  requestTitle: {
    fontWeight: 'bold',
    color: TimoColors.primary,
    marginBottom: Spacing.xs,
  },
  fareText: {
    color: TimoColors.accent,
    fontWeight: 'bold',
    marginTop: Spacing.xs,
  },
  activeRideCard: {
    position: 'absolute',
    top: 250,
    left: Spacing.md,
    right: Spacing.md,
    borderRadius: BorderRadius.lg,
    backgroundColor: TimoColors.accent,
    ...GlowStyles.accent,
  },
  activeRideTitle: {
    fontWeight: 'bold',
    color: TimoColors.white,
  },
  bottomContainer: {
    position: 'absolute',
    bottom: Spacing.xl,
    left: Spacing.md,
    right: Spacing.md,
  },
  availabilityButton: {
    paddingVertical: Spacing.sm,
    borderRadius: BorderRadius.lg,
  },
  fab: {
    position: 'absolute',
    right: Spacing.md,
    top: 100,
    backgroundColor: TimoColors.primary,
  },
  simulateFab: {
    bottom: 180,
    top: 'auto',
    backgroundColor: TimoColors.primary,
  },
});

export default HomeScreen;

