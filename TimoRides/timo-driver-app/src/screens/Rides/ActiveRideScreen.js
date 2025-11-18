import React from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { Text, Button, Card } from 'react-native-paper';
import { useRoute } from '@react-navigation/native';
import { TimoColors, Spacing, BorderRadius, GlowStyles } from '../../utils/theme';

const ActiveRideScreen = () => {
  const route = useRoute();
  const { booking } = route.params || {};

  if (!booking) {
    return null;
  }

  return (
    <ScrollView style={styles.container}>
      <Card style={styles.card}>
        <Card.Content>
          <Text variant="headlineSmall" style={styles.title}>
            Active Ride
          </Text>

          <View style={styles.statusBox}>
            <Text variant="titleLarge" style={styles.statusText}>
              {booking.status === 'accepted' ? 'Heading to Pickup' : 'In Transit'}
            </Text>
          </View>

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
            <Button
              mode="outlined"
              icon="navigation"
              style={styles.navButton}
            >
              Navigate to Pickup
            </Button>
          </View>

          <View style={styles.section}>
            <Text variant="titleMedium" style={styles.sectionTitle}>
              📍 Destination
            </Text>
            <Text variant="bodyLarge">
              {booking.destinationLocation?.address || 'N/A'}
            </Text>
            <Button
              mode="outlined"
              icon="navigation"
              style={styles.navButton}
            >
              Navigate to Destination
            </Button>
          </View>

          <View style={styles.fareSection}>
            <Text variant="headlineMedium" style={styles.fareAmount}>
              {booking.currency?.symbol || 'R'} {booking.tripAmount || '0.00'}
            </Text>
          </View>

          <View style={styles.actionButtons}>
            <Button
              mode="outlined"
              icon="phone"
              style={styles.actionButton}
            >
              Call Rider
            </Button>
            <Button
              mode="outlined"
              icon="message"
              style={styles.actionButton}
            >
              Message
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
  statusBox: {
    backgroundColor: TimoColors.primary,
    padding: Spacing.lg,
    borderRadius: BorderRadius.lg,
    marginBottom: Spacing.lg,
    alignItems: 'center',
  },
  statusText: {
    color: TimoColors.white,
    fontWeight: 'bold',
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
  navButton: {
    marginTop: Spacing.sm,
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
  },
  actionButtons: {
    flexDirection: 'row',
    gap: Spacing.md,
    marginTop: Spacing.md,
  },
  actionButton: {
    flex: 1,
  },
});

export default ActiveRideScreen;

