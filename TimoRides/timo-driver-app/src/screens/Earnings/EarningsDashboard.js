import React from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { Text, Card } from 'react-native-paper';
import { useSelector } from 'react-redux';
import { TimoColors, Spacing, BorderRadius, GlowStyles } from '../../utils/theme';

const EarningsDashboard = () => {
  const { bookings } = useSelector((state) => state.bookings);

  // Calculate earnings from completed bookings
  const completedBookings = bookings.filter((b) => b.status === 'completed');
  const todayEarnings = completedBookings
    .filter((b) => {
      const today = new Date();
      const bookingDate = new Date(b.completedAt || b.createdAt);
      return bookingDate.toDateString() === today.toDateString();
    })
    .reduce((sum, b) => sum + (parseFloat(b.tripAmount) || 0), 0);

  const weekEarnings = completedBookings
    .filter((b) => {
      const weekAgo = new Date();
      weekAgo.setDate(weekAgo.getDate() - 7);
      const bookingDate = new Date(b.completedAt || b.createdAt);
      return bookingDate >= weekAgo;
    })
    .reduce((sum, b) => sum + (parseFloat(b.tripAmount) || 0), 0);

  const monthEarnings = completedBookings
    .filter((b) => {
      const monthAgo = new Date();
      monthAgo.setMonth(monthAgo.getMonth() - 1);
      const bookingDate = new Date(b.completedAt || b.createdAt);
      return bookingDate >= monthAgo;
    })
    .reduce((sum, b) => sum + (parseFloat(b.tripAmount) || 0), 0);

  return (
    <ScrollView style={styles.container}>
      <Card style={[styles.earningsCard, GlowStyles.accent]}>
        <Card.Content>
          <Text variant="titleMedium" style={styles.earningsLabel}>
            Today's Earnings
          </Text>
          <Text variant="headlineLarge" style={styles.earningsAmount}>
            R {todayEarnings.toFixed(2)}
          </Text>
          <Text variant="bodySmall" style={styles.ridesCount}>
            {completedBookings.filter((b) => {
              const today = new Date();
              const bookingDate = new Date(b.completedAt || b.createdAt);
              return bookingDate.toDateString() === today.toDateString();
            }).length} rides
          </Text>
        </Card.Content>
      </Card>

      <View style={styles.statsContainer}>
        <Card style={styles.statCard}>
          <Card.Content>
            <Text variant="titleSmall" style={styles.statLabel}>
              This Week
            </Text>
            <Text variant="headlineSmall" style={styles.statAmount}>
              R {weekEarnings.toFixed(2)}
            </Text>
          </Card.Content>
        </Card>

        <Card style={styles.statCard}>
          <Card.Content>
            <Text variant="titleSmall" style={styles.statLabel}>
              This Month
            </Text>
            <Text variant="headlineSmall" style={styles.statAmount}>
              R {monthEarnings.toFixed(2)}
            </Text>
          </Card.Content>
        </Card>
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: TimoColors.backgroundLight,
  },
  earningsCard: {
    margin: Spacing.md,
    borderRadius: BorderRadius.xl,
    backgroundColor: TimoColors.primary,
    ...GlowStyles.accent,
  },
  earningsLabel: {
    color: TimoColors.white,
    marginBottom: Spacing.sm,
  },
  earningsAmount: {
    color: TimoColors.accent,
    fontWeight: 'bold',
  },
  ridesCount: {
    color: TimoColors.white,
    marginTop: Spacing.xs,
  },
  statsContainer: {
    flexDirection: 'row',
    paddingHorizontal: Spacing.md,
    gap: Spacing.md,
  },
  statCard: {
    flex: 1,
    borderRadius: BorderRadius.lg,
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
  },
  statLabel: {
    color: TimoColors.textMid,
    marginBottom: Spacing.xs,
  },
  statAmount: {
    color: TimoColors.primary,
    fontWeight: 'bold',
  },
});

export default EarningsDashboard;

