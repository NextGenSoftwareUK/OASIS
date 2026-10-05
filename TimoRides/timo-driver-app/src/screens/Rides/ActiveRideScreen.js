import React, { useState } from 'react';
import { View, StyleSheet, Dimensions, ScrollView } from 'react-native';
import { Text, Button, Card, Avatar, Divider } from 'react-native-paper';
import { useRoute, useNavigation } from '@react-navigation/native';
import { useDispatch } from 'react-redux';
import { updateBookingStatus } from '../../store/slices/bookingSlice';
import { TimoColors, Spacing, BorderRadius, GlowStyles } from '../../utils/theme';
import MapViewComponent from '../../components/MapView';

const { height: SCREEN_HEIGHT } = Dimensions.get('window');

const ActiveRideScreen = () => {
  const route = useRoute();
  const navigation = useNavigation();
  const dispatch = useDispatch();
  const { booking } = route.params || {};
  const [rideStatus, setRideStatus] = useState(booking?.status || 'accepted');

  if (!booking) {
    return null;
  }

  const handleStartRide = () => {
    setRideStatus('started');
    dispatch(updateBookingStatus({ id: booking.id, status: 'started' }));
  };

  const handleCompleteRide = () => {
    setRideStatus('completed');
    dispatch(updateBookingStatus({ id: booking.id, status: 'completed' }));
    // Navigate to trip complete screen
    navigation.navigate('TripComplete', { booking: { ...booking, status: 'completed' } });
  };

  const statusText = rideStatus === 'accepted' 
    ? 'Heading to Pickup' 
    : rideStatus === 'started' 
    ? 'In Transit' 
    : 'Completed';

  return (
    <View style={styles.container}>
      {/* Map Section - Top Half */}
      <View style={styles.mapContainer}>
        <MapViewComponent
          pickupLocation={booking.sourceLocation}
          destinationLocation={booking.destinationLocation}
          showRoute={true}
          showUserLocation={true}
        />
      </View>

      {/* Trip Details Card - Bottom Half */}
      <Card style={styles.bottomCard}>
        <Card.Content style={styles.cardContent}>
          {/* Handle Indicator */}
          <View style={styles.handle} />

          {/* Status Banner */}
          <View style={[styles.statusBox, { backgroundColor: rideStatus === 'started' ? TimoColors.success : TimoColors.primary }]}>
            <Text variant="titleLarge" style={styles.statusText}>
              {statusText}
            </Text>
          </View>

          {/* Rider Information */}
          <View style={styles.riderInfo}>
            <Avatar.Text
              size={56}
              label={booking.fullName?.charAt(0) || 'R'}
              style={styles.avatar}
            />
            <View style={styles.riderDetails}>
              <Text variant="titleMedium" style={styles.riderName}>
                {booking.fullName || 'Rider'}
              </Text>
              <Text variant="bodySmall" style={styles.riderPhone}>
                {booking.phoneNumber}
              </Text>
            </View>
          </View>

          <Divider style={styles.divider} />

          {/* Pickup Location */}
          <View style={styles.locationSection}>
            <Text variant="titleSmall" style={styles.locationLabel}>
              📍 Pickup Location
            </Text>
            <Text variant="bodyLarge" style={styles.locationAddress}>
              {booking.sourceLocation?.address || 'N/A'}
            </Text>
            <Button
              mode="outlined"
              icon="navigation"
              style={styles.navButton}
              onPress={() => {
                // Open navigation app
              }}
            >
              Navigate to Pickup
            </Button>
          </View>

          {/* Destination */}
          <View style={styles.locationSection}>
            <Text variant="titleSmall" style={styles.locationLabel}>
              📍 Destination
            </Text>
            <Text variant="bodyLarge" style={styles.locationAddress}>
              {booking.destinationLocation?.address || 'N/A'}
            </Text>
            <Button
              mode="outlined"
              icon="navigation"
              style={styles.navButton}
              onPress={() => {
                // Open navigation app
              }}
            >
              Navigate to Destination
            </Button>
          </View>

          {/* Trip Details */}
          <View style={styles.tripDetails}>
            <View style={styles.detailRow}>
              <Text variant="bodyMedium" style={styles.detailLabel}>
                Distance
              </Text>
              <Text variant="bodyMedium" style={styles.detailValue}>
                {booking.tripDistance || 'N/A'}
              </Text>
            </View>
            <View style={styles.detailRow}>
              <Text variant="bodyMedium" style={styles.detailLabel}>
                Duration
              </Text>
              <Text variant="bodyMedium" style={styles.detailValue}>
                {booking.tripDuration || 'N/A'}
              </Text>
            </View>
            <View style={styles.detailRow}>
              <Text variant="bodyMedium" style={styles.detailLabel}>
                Fare
              </Text>
              <Text variant="bodyMedium" style={[styles.detailValue, styles.fareText]}>
                {booking.currency?.symbol || 'R'} {booking.tripAmount || '0.00'}
              </Text>
            </View>
          </View>

          {/* Action Buttons */}
          <View style={styles.actionButtons}>
            {rideStatus === 'accepted' && (
              <Button
                mode="contained"
                onPress={handleStartRide}
                style={styles.startButton}
                buttonColor={TimoColors.success}
                textColor={TimoColors.white}
                contentStyle={styles.buttonContent}
              >
                Start Ride
              </Button>
            )}
            {rideStatus === 'started' && (
              <Button
                mode="contained"
                onPress={handleCompleteRide}
                style={styles.completeButton}
                buttonColor={TimoColors.primary}
                textColor={TimoColors.white}
                contentStyle={styles.buttonContent}
              >
                Complete Ride
              </Button>
            )}
            <View style={styles.secondaryButtons}>
              <Button
                mode="outlined"
                icon="phone"
                style={styles.secondaryButton}
                onPress={() => {
                  // Call rider
                }}
              >
                Call
              </Button>
              <Button
                mode="outlined"
                icon="message"
                style={styles.secondaryButton}
                onPress={() => {
                  // Message rider
                }}
              >
                Message
              </Button>
            </View>
          </View>
        </Card.Content>
      </Card>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  mapContainer: {
    flex: 1,
  },
  bottomCard: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    backgroundColor: 'rgba(255, 255, 255, 0.98)',
    maxHeight: SCREEN_HEIGHT * 0.6,
    ...GlowStyles.primary,
  },
  cardContent: {
    padding: Spacing.lg,
    paddingTop: Spacing.md,
  },
  handle: {
    width: 40,
    height: 4,
    backgroundColor: '#E0E0E0',
    borderRadius: 2,
    alignSelf: 'center',
    marginBottom: Spacing.md,
  },
  statusBox: {
    padding: Spacing.md,
    borderRadius: BorderRadius.lg,
    marginBottom: Spacing.lg,
    alignItems: 'center',
  },
  statusText: {
    color: TimoColors.white,
    fontWeight: 'bold',
  },
  riderInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: Spacing.md,
  },
  avatar: {
    backgroundColor: TimoColors.primary,
    marginRight: Spacing.md,
  },
  riderDetails: {
    flex: 1,
  },
  riderName: {
    fontWeight: '600',
    marginBottom: Spacing.xs,
  },
  riderPhone: {
    color: TimoColors.textMid,
  },
  divider: {
    marginVertical: Spacing.md,
  },
  locationSection: {
    marginBottom: Spacing.lg,
  },
  locationLabel: {
    fontWeight: '600',
    marginBottom: Spacing.xs,
    color: TimoColors.primary,
  },
  locationAddress: {
    marginBottom: Spacing.sm,
  },
  navButton: {
    marginTop: Spacing.xs,
  },
  tripDetails: {
    backgroundColor: TimoColors.backgroundLight,
    padding: Spacing.md,
    borderRadius: BorderRadius.lg,
    marginBottom: Spacing.lg,
  },
  detailRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: Spacing.xs,
  },
  detailLabel: {
    color: TimoColors.textMid,
  },
  detailValue: {
    fontWeight: '600',
  },
  fareText: {
    color: TimoColors.accent,
    fontSize: 18,
  },
  actionButtons: {
    marginTop: Spacing.sm,
  },
  startButton: {
    marginBottom: Spacing.md,
    borderRadius: BorderRadius.md,
  },
  completeButton: {
    marginBottom: Spacing.md,
    borderRadius: BorderRadius.md,
    ...GlowStyles.primary,
  },
  buttonContent: {
    paddingVertical: Spacing.sm,
  },
  secondaryButtons: {
    flexDirection: 'row',
    gap: Spacing.md,
  },
  secondaryButton: {
    flex: 1,
  },
});

export default ActiveRideScreen;
