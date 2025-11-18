import React from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { Text, Button, Card } from 'react-native-paper';
import { useDispatch } from 'react-redux';
import { useRoute, useNavigation } from '@react-navigation/native';
import { acceptBooking, cancelBooking } from '../../store/slices/bookingSlice';
import { TimoColors, Spacing, BorderRadius, GlowStyles } from '../../utils/theme';

const RideRequestScreen = () => {
  const dispatch = useDispatch();
  const navigation = useNavigation();
  const route = useRoute();
  const { booking } = route.params || {};

  if (!booking) {
    return null;
  }

  const handleAccept = async () => {
    await dispatch(acceptBooking(booking.id));
    navigation.goBack();
  };

  const handleDecline = async () => {
    await dispatch(cancelBooking(booking.id));
    navigation.goBack();
  };

  return (
    <ScrollView style={styles.container}>
      <Card style={styles.card}>
        <Card.Content>
          <Text variant="headlineSmall" style={styles.title}>
            Ride Request
          </Text>

          <View style={styles.section}>
            <Text variant="titleMedium" style={styles.sectionTitle}>
              Rider Information
            </Text>
            <Text variant="bodyLarge">{booking.fullName}</Text>
            <Text variant="bodyMedium" style={styles.phone}>
              {booking.phoneNumber}
            </Text>
          </View>

          <View style={styles.section}>
            <Text variant="titleMedium" style={styles.sectionTitle}>
              📍 Pickup Location
            </Text>
            <Text variant="bodyLarge">
              {booking.sourceLocation?.address || 'N/A'}
            </Text>
          </View>

          <View style={styles.section}>
            <Text variant="titleMedium" style={styles.sectionTitle}>
              📍 Destination
            </Text>
            <Text variant="bodyLarge">
              {booking.destinationLocation?.address || 'N/A'}
            </Text>
          </View>

          <View style={styles.section}>
            <Text variant="titleMedium" style={styles.sectionTitle}>
              Trip Details
            </Text>
            <Text variant="bodyMedium">
              Distance: {booking.tripDistance || 'N/A'}
            </Text>
            <Text variant="bodyMedium">
              Duration: {booking.tripDuration || 'N/A'}
            </Text>
            <Text variant="bodyMedium">
              Passengers: {booking.passengers || 1}
            </Text>
          </View>

          <View style={styles.fareSection}>
            <Text variant="headlineMedium" style={styles.fareAmount}>
              {booking.currency?.symbol || 'R'} {booking.tripAmount || '0.00'}
            </Text>
            <Text variant="bodySmall" style={styles.paymentMethod}>
              Payment: {booking.isCash ? 'Cash' : 'Online'}
            </Text>
          </View>

          <View style={styles.buttonContainer}>
            <Button
              mode="outlined"
              onPress={handleDecline}
              style={styles.declineButton}
              textColor={TimoColors.error}
            >
              Decline
            </Button>
            <Button
              mode="contained"
              onPress={handleAccept}
              style={[styles.acceptButton, GlowStyles.primary]}
              buttonColor={TimoColors.primary}
              textColor={TimoColors.white}
            >
              Accept Ride
            </Button>
          </View>
        </Card.Content>
      </Card>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: TimoColors.backgroundLight,
  },
  card: {
    margin: Spacing.md,
    borderRadius: BorderRadius.xl,
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    ...GlowStyles.primary,
  },
  title: {
    fontWeight: 'bold',
    marginBottom: Spacing.lg,
    color: TimoColors.textMain,
  },
  section: {
    marginBottom: Spacing.lg,
  },
  sectionTitle: {
    fontWeight: '600',
    marginBottom: Spacing.sm,
    color: TimoColors.primary,
  },
  phone: {
    color: TimoColors.textMid,
    marginTop: Spacing.xs,
  },
  fareSection: {
    backgroundColor: TimoColors.backgroundLight,
    padding: Spacing.lg,
    borderRadius: BorderRadius.lg,
    marginVertical: Spacing.lg,
    alignItems: 'center',
  },
  fareAmount: {
    fontWeight: 'bold',
    color: TimoColors.accent,
    marginBottom: Spacing.xs,
  },
  paymentMethod: {
    color: TimoColors.textMid,
  },
  buttonContainer: {
    flexDirection: 'row',
    gap: Spacing.md,
    marginTop: Spacing.md,
  },
  declineButton: {
    flex: 1,
    borderColor: TimoColors.error,
  },
  acceptButton: {
    flex: 1,
  },
});

export default RideRequestScreen;

