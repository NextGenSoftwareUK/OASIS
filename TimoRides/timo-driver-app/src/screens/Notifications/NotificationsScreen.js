import React from 'react';
import { View, StyleSheet, ScrollView, FlatList } from 'react-native';
import { Text, Card, Divider } from 'react-native-paper';
import { TimoColors, Spacing, BorderRadius, GlowStyles } from '../../utils/theme';

const NotificationsScreen = () => {
  // Mock notifications data
  const notifications = [
    {
      id: '1',
      title: 'New Ride Request',
      message: 'You have a new ride request from Sarah Johnson',
      time: '2 minutes ago',
      read: false,
    },
    {
      id: '2',
      title: 'Payment Received',
      message: 'R 250 has been credited to your account',
      time: '1 hour ago',
      read: true,
    },
    {
      id: '3',
      title: 'Ride Completed',
      message: 'Your ride with Mike Thompson has been completed',
      time: '3 hours ago',
      read: true,
    },
  ];

  const renderNotification = ({ item }) => (
    <Card style={[styles.notificationCard, !item.read && styles.unreadCard]}>
      <Card.Content>
        <View style={styles.notificationHeader}>
          <Text variant="titleMedium" style={styles.notificationTitle}>
            {item.title}
          </Text>
          {!item.read && <View style={styles.unreadDot} />}
        </View>
        <Text variant="bodyMedium" style={styles.notificationMessage}>
          {item.message}
        </Text>
        <Text variant="bodySmall" style={styles.notificationTime}>
          {item.time}
        </Text>
      </Card.Content>
    </Card>
  );

  return (
    <View style={styles.container}>
      <ScrollView>
        <View style={styles.header}>
          <Text variant="headlineSmall" style={styles.title}>
            Notifications
          </Text>
        </View>
        {notifications.length > 0 ? (
          <FlatList
            data={notifications}
            renderItem={renderNotification}
            keyExtractor={(item) => item.id}
            scrollEnabled={false}
          />
        ) : (
          <Card style={styles.emptyCard}>
            <Card.Content>
              <Text variant="bodyLarge" style={styles.emptyText}>
                No notifications
              </Text>
            </Card.Content>
          </Card>
        )}
      </ScrollView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: TimoColors.backgroundLight,
  },
  header: {
    padding: Spacing.lg,
  },
  title: {
    fontWeight: 'bold',
    color: TimoColors.textMain,
  },
  notificationCard: {
    margin: Spacing.md,
    marginTop: 0,
    borderRadius: BorderRadius.xl,
    backgroundColor: TimoColors.white,
    ...GlowStyles.primary,
  },
  unreadCard: {
    borderLeftWidth: 4,
    borderLeftColor: TimoColors.primary,
  },
  notificationHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: Spacing.xs,
  },
  notificationTitle: {
    fontWeight: '600',
    flex: 1,
  },
  unreadDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: TimoColors.primary,
  },
  notificationMessage: {
    color: TimoColors.textMid,
    marginBottom: Spacing.xs,
  },
  notificationTime: {
    color: TimoColors.textLight,
    marginTop: Spacing.xs,
  },
  emptyCard: {
    margin: Spacing.lg,
    borderRadius: BorderRadius.xl,
    backgroundColor: TimoColors.white,
  },
  emptyText: {
    textAlign: 'center',
    color: TimoColors.textMid,
  },
});

export default NotificationsScreen;

