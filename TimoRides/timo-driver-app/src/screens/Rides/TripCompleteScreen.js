import React, { useState } from 'react';
import { View, StyleSheet, ScrollView, TouchableOpacity } from 'react-native';
import { Text, Button, Card, Avatar, Divider } from 'react-native-paper';
import { useNavigation, useRoute } from '@react-navigation/native';
import { useDispatch } from 'react-redux';
// Icons will be handled with Text/Emoji for simplicity
import { clearActiveBooking, updateBookingStatus } from '../../store/slices/bookingSlice';
import { TimoColors, Spacing, BorderRadius, GlowStyles } from '../../utils/theme';

// Simple Star Rating Component
const StarRating = ({ rating, onRatingChange, size = 40 }) => {
  return (
    <View style={styles.ratingContainer}>
      {[1, 2, 3, 4, 5].map((star) => (
        <TouchableOpacity
          key={star}
          onPress={() => onRatingChange(star)}
          style={styles.starButton}
        >
          <Text style={[styles.star, { fontSize: size }]}>
            {star <= rating ? '★' : '☆'}
          </Text>
        </TouchableOpacity>
      ))}
    </View>
  );
};

const TripCompleteScreen = () => {
  const navigation = useNavigation();
  const route = useRoute();
  const dispatch = useDispatch();
  const booking = route.params?.booking;

  const [rating, setRating] = useState(0);
  const [ratingSubmitted, setRatingSubmitted] = useState(false);

  if (!booking) {
    return (
      <View style={styles.container}>
        <Text>No trip data found</Text>
        <Button onPress={() => navigation.navigate('Home')}>Go Home</Button>
      </View>
    );
  }

  const handleSubmitRating = () => {
    setRatingSubmitted(true);
    // In a real app, this would submit the rating to the backend
    setTimeout(() => {
      dispatch(clearActiveBooking());
      dispatch(updateBookingStatus({ id: booking.id, status: 'completed' }));
      navigation.navigate('Home');
    }, 2000);
  };

  const handleSkipRating = () => {
    dispatch(clearActiveBooking());
    dispatch(updateBookingStatus({ id: booking.id, status: 'completed' }));
    navigation.navigate('Home');
  };

  return (
    <ScrollView style={styles.container}>
      {/* Success Message */}
      <Card style={[styles.successCard, { backgroundColor: TimoColors.success }]}>
        <Card.Content style={styles.successContent}>
          <Text style={styles.successIcon}>✓</Text>
          <Text variant="headlineMedium" style={styles.successTitle}>
            Trip Completed!
          </Text>
          <Text variant="bodyMedium" style={styles.successSubtitle}>
            Thank you for the safe ride
          </Text>
        </Card.Content>
      </Card>

      {/* Payment Summary */}
      <Card style={styles.card}>
        <Card.Content style={styles.cardContent}>
          <View style={styles.riderInfo}>
            <Avatar.Text
              size={64}
              label={booking.fullName?.charAt(0) || 'R'}
              style={{ backgroundColor: TimoColors.primary }}
            />
            <View style={styles.riderDetails}>
              <Text variant="titleLarge" style={styles.riderName}>
                {booking.fullName || 'Rider'}
              </Text>
              <Text variant="bodySmall" style={styles.riderRoute}>
                {booking.sourceLocation?.address || 'Pickup'} →{' '}
                {booking.destinationLocation?.address || 'Destination'}
              </Text>
            </View>
          </View>

          <Divider style={styles.divider} />

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
                Payment Method
              </Text>
              <Text variant="bodyMedium" style={styles.detailValue}>
                {booking.isCash ? 'Cash' : 'Online'}
              </Text>
            </View>
          </View>

          <Divider style={styles.divider} />

          {/* Fare Amount */}
          <View style={styles.fareSection}>
            <Text variant="bodySmall" style={styles.fareLabel}>
              Total Fare
            </Text>
            <Text variant="displaySmall" style={styles.fareAmount}>
              {booking.currency?.symbol || 'R'} {booking.tripAmount || '0.00'}
            </Text>
            {!booking.isCash && (
              <View style={styles.paymentReceived}>
                <Text style={styles.paymentText}>✓ Payment Received</Text>
              </View>
            )}
          </View>
        </Card.Content>
      </Card>

      {/* Rating Section */}
      {!ratingSubmitted ? (
        <Card style={styles.card}>
          <Card.Content style={styles.cardContent}>
            <Text variant="titleLarge" style={styles.ratingTitle}>
              Rate Your Rider
            </Text>
            <Text variant="bodyMedium" style={styles.ratingSubtitle}>
              How was your experience with {booking.fullName || 'this rider'}?
            </Text>

            <View style={styles.ratingWrapper}>
              <StarRating rating={rating} onRatingChange={setRating} size={48} />
            </View>

            {rating > 0 && (
              <View style={styles.ratingButtons}>
                <Button
                  mode="outlined"
                  onPress={handleSkipRating}
                  style={styles.skipButton}
                  textColor={TimoColors.primary}
                >
                  Skip
                </Button>
                <Button
                  mode="contained"
                  onPress={handleSubmitRating}
                  style={styles.submitButton}
                  buttonColor={TimoColors.primary}
                >
                  Submit Rating
                </Button>
              </View>
            )}
          </Card.Content>
        </Card>
      ) : (
        <Card style={[styles.card, { backgroundColor: TimoColors.backgroundLight }]}>
          <Card.Content style={styles.cardContent}>
            <View style={styles.thankYouContainer}>
              <Text style={styles.thankYouIcon}>★</Text>
              <Text variant="titleLarge" style={styles.thankYouTitle}>
                Thank You!
              </Text>
              <Text variant="bodyMedium" style={styles.thankYouSubtitle}>
                Your rating has been submitted
              </Text>
            </View>
          </Card.Content>
        </Card>
      )}
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: TimoColors.backgroundLight,
  },
  successCard: {
    margin: Spacing.md,
    borderRadius: BorderRadius.xl,
    ...GlowStyles.accent,
  },
  successContent: {
    alignItems: 'center',
    padding: Spacing.xl,
  },
  successIcon: {
    fontSize: 64,
    color: TimoColors.white,
    marginBottom: Spacing.md,
  },
  successTitle: {
    fontWeight: 'bold',
    color: TimoColors.white,
    marginBottom: Spacing.sm,
  },
  successSubtitle: {
    color: TimoColors.white,
    opacity: 0.9,
  },
  card: {
    margin: Spacing.md,
    marginTop: 0,
    borderRadius: BorderRadius.xl,
    backgroundColor: TimoColors.white,
    ...GlowStyles.primary,
  },
  cardContent: {
    padding: Spacing.lg,
  },
  riderInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: Spacing.lg,
  },
  riderDetails: {
    marginLeft: Spacing.md,
    flex: 1,
  },
  riderName: {
    fontWeight: 'bold',
    marginBottom: Spacing.xs,
  },
  riderRoute: {
    color: TimoColors.textMid,
  },
  divider: {
    marginVertical: Spacing.lg,
  },
  tripDetails: {
    marginBottom: Spacing.lg,
  },
  detailRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: Spacing.sm,
  },
  detailLabel: {
    color: TimoColors.textMid,
  },
  detailValue: {
    fontWeight: '600',
  },
  fareSection: {
    backgroundColor: TimoColors.backgroundLight,
    padding: Spacing.lg,
    borderRadius: BorderRadius.lg,
    alignItems: 'center',
  },
  fareLabel: {
    color: TimoColors.textMid,
    marginBottom: Spacing.xs,
  },
  fareAmount: {
    fontWeight: 'bold',
    color: TimoColors.accent,
    marginBottom: Spacing.sm,
  },
  paymentReceived: {
    marginTop: Spacing.sm,
  },
  paymentText: {
    color: TimoColors.success,
    fontWeight: '600',
  },
  ratingTitle: {
    fontWeight: 'bold',
    textAlign: 'center',
    marginBottom: Spacing.xs,
  },
  ratingSubtitle: {
    color: TimoColors.textMid,
    textAlign: 'center',
    marginBottom: Spacing.lg,
  },
  ratingWrapper: {
    alignItems: 'center',
    marginVertical: Spacing.lg,
  },
  ratingContainer: {
    flexDirection: 'row',
    justifyContent: 'center',
    gap: Spacing.sm,
  },
  starButton: {
    padding: Spacing.xs,
  },
  star: {
    color: TimoColors.accent,
  },
  ratingButtons: {
    flexDirection: 'row',
    gap: Spacing.md,
    marginTop: Spacing.lg,
  },
  skipButton: {
    flex: 1,
    borderColor: TimoColors.primary,
  },
  submitButton: {
    flex: 1,
  },
  thankYouContainer: {
    alignItems: 'center',
    padding: Spacing.lg,
  },
  thankYouIcon: {
    fontSize: 48,
    color: TimoColors.accent,
    marginBottom: Spacing.md,
  },
  thankYouTitle: {
    fontWeight: 'bold',
    marginBottom: Spacing.xs,
  },
  thankYouSubtitle: {
    color: TimoColors.textMid,
  },
});

export default TripCompleteScreen;

