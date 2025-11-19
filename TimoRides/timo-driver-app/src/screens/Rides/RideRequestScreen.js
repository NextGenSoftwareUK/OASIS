import React from 'react';
import { View, StyleSheet, Dimensions, TouchableOpacity } from 'react-native';
import { Text, Button, Card, Chip, IconButton } from 'react-native-paper';
import { useDispatch } from 'react-redux';
import { useRoute, useNavigation } from '@react-navigation/native';
import { acceptBooking, cancelBooking, setActiveBooking, removePendingBooking, updateBookingStatus } from '../../store/slices/bookingSlice';
import { TimoColors, Spacing, BorderRadius, GlowStyles } from '../../utils/theme';
import MapViewComponent from '../../components/MapView';

const { height: SCREEN_HEIGHT } = Dimensions.get('window');

const RideRequestScreen = () => {
  const dispatch = useDispatch();
  const navigation = useNavigation();
  const route = useRoute();
  const { booking } = route.params || {};

  if (!booking) {
    return null;
  }

  const handleAccept = async () => {
    // For simulated bookings, handle directly without API call
    if (booking.id.startsWith('sim-')) {
      const acceptedBooking = { ...booking, status: 'accepted' };
      dispatch(setActiveBooking(acceptedBooking));
      dispatch(removePendingBooking(booking.id));
      dispatch(updateBookingStatus({ id: booking.id, status: 'accepted' }));
      navigation.navigate('Home');
      return;
    }

    // For real bookings, call the API
    try {
      await dispatch(acceptBooking(booking.id));
      navigation.navigate('Home');
    } catch (error) {
      console.error('Error accepting booking:', error);
    }
  };

  const handleDecline = async () => {
    // For simulated bookings, handle directly without API call
    if (booking.id.startsWith('sim-')) {
      dispatch(removePendingBooking(booking.id));
      navigation.goBack();
      return;
    }

    // For real bookings, call the API
    try {
      await dispatch(cancelBooking(booking.id));
      navigation.goBack();
    } catch (error) {
      console.error('Error declining booking:', error);
    }
  };

  // Mock pickup distance/time (would come from backend)
  const pickupDistance = '5.2 km';
  const pickupTime = '12 mins';
  const riderRating = booking.riderRating || 4.8;

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

      {/* Ride Request Card - Bottom Half */}
      <Card style={styles.bottomCard}>
        <Card.Content style={styles.cardContent}>
          {/* Handle Indicator */}
          <View style={styles.handle} />

          {/* Header */}
          <View style={styles.header}>
            <View style={styles.headerLeft}>
              <Chip
                style={styles.chip}
                textStyle={styles.chipText}
              >
                TimoRides
              </Chip>
            </View>
            <IconButton
              icon="close"
              size={24}
              onPress={handleDecline}
              style={styles.closeButton}
            />
          </View>

          {/* Fare Amount */}
          <Text variant="displaySmall" style={styles.fareAmount}>
            {booking.currency?.symbol || 'R'} {booking.tripAmount || '0.00'}
          </Text>

          {/* Rider Rating */}
          <View style={styles.ratingContainer}>
            <Text style={styles.star}>★</Text>
            <Text variant="bodyMedium" style={styles.ratingText}>
              {riderRating.toFixed(1)}
            </Text>
          </View>

          {/* Pickup Details */}
          <View style={styles.locationRow}>
            <View style={styles.locationIndicator}>
              <View style={[styles.dot, styles.pickupDot]} />
              <View style={styles.line} />
            </View>
            <View style={styles.locationDetails}>
              <View style={styles.locationHeader}>
                <Text variant="bodySmall" style={styles.locationMeta}>
                  {pickupTime} ({pickupDistance}) away
                </Text>
              </View>
              <Text variant="bodyLarge" style={styles.locationAddress}>
                {booking.sourceLocation?.address || 'Pickup location'}
              </Text>
            </View>
          </View>

          {/* Trip Details */}
          <View style={styles.locationRow}>
            <View style={styles.locationIndicator}>
              <View style={styles.line} />
              <View style={[styles.dot, styles.destinationDot]} />
            </View>
            <View style={styles.locationDetails}>
              <View style={styles.locationHeader}>
                <Text variant="bodySmall" style={styles.locationMeta}>
                  {booking.tripDuration || 'N/A'} ({booking.tripDistance || 'N/A'}) trip
                </Text>
              </View>
              <Text variant="bodyLarge" style={styles.locationAddress}>
                {booking.destinationLocation?.address || 'Destination'}
              </Text>
            </View>
          </View>

          {/* Accept Button */}
          <Button
            mode="contained"
            onPress={handleAccept}
            style={styles.acceptButton}
            buttonColor={TimoColors.primary}
            textColor={TimoColors.white}
            contentStyle={styles.acceptButtonContent}
          >
            Accept
          </Button>
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
    maxHeight: SCREEN_HEIGHT * 0.55,
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
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: Spacing.md,
  },
  headerLeft: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  chip: {
    backgroundColor: TimoColors.primary,
    height: 28,
  },
  chipText: {
    color: TimoColors.white,
    fontWeight: '600',
  },
  closeButton: {
    margin: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.05)',
  },
  fareAmount: {
    fontWeight: '700',
    color: TimoColors.primary,
    marginBottom: Spacing.sm,
  },
  ratingContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.xs,
    marginBottom: Spacing.lg,
  },
  star: {
    fontSize: 18,
    color: TimoColors.accent,
  },
  ratingText: {
    fontWeight: '600',
  },
  locationRow: {
    flexDirection: 'row',
    marginBottom: Spacing.lg,
  },
  locationIndicator: {
    alignItems: 'center',
    marginRight: Spacing.md,
    marginTop: Spacing.xs,
  },
  dot: {
    width: 12,
    height: 12,
    borderRadius: 6,
    borderWidth: 2,
    borderColor: TimoColors.white,
  },
  pickupDot: {
    backgroundColor: TimoColors.primary,
  },
  destinationDot: {
    backgroundColor: TimoColors.error,
  },
  line: {
    width: 2,
    height: 40,
    backgroundColor: '#E0E0E0',
    marginVertical: Spacing.xs,
  },
  locationDetails: {
    flex: 1,
  },
  locationHeader: {
    marginBottom: Spacing.xs,
  },
  locationMeta: {
    color: TimoColors.textMid,
    fontWeight: '500',
  },
  locationAddress: {
    fontWeight: '500',
  },
  acceptButton: {
    marginTop: Spacing.md,
    borderRadius: BorderRadius.md,
    ...GlowStyles.primary,
  },
  acceptButtonContent: {
    paddingVertical: Spacing.sm,
  },
});

export default RideRequestScreen;
