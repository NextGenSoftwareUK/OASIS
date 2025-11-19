import React from 'react';
import { View, StyleSheet, ScrollView, FlatList } from 'react-native';
import { Text, Card, Divider } from 'react-native-paper';
import { TimoColors, Spacing, BorderRadius, GlowStyles } from '../../utils/theme';

const HistoryScreen = () => {
  // Mock history data
  const historyData = [
    {
      id: '1',
      date: '2024-01-15',
      rider: 'Sarah Johnson',
      pickup: 'Durban Central',
      destination: 'Umhlanga Beach',
      fare: 250,
      rating: 5,
    },
    {
      id: '2',
      date: '2024-01-14',
      rider: 'Mike Thompson',
      pickup: 'Gateway Mall',
      destination: 'Durban Airport',
      fare: 320,
      rating: 4,
    },
  ];

  const renderHistoryItem = ({ item }) => (
    <Card style={styles.historyCard}>
      <Card.Content>
        <View style={styles.historyHeader}>
          <Text variant="titleMedium" style={styles.historyDate}>
            {new Date(item.date).toLocaleDateString()}
          </Text>
          <Text variant="titleLarge" style={styles.historyFare}>
            R {item.fare}
          </Text>
        </View>
        <Divider style={styles.divider} />
        <Text variant="bodyLarge" style={styles.riderName}>
          {item.rider}
        </Text>
        <Text variant="bodyMedium" style={styles.location}>
          {item.pickup} → {item.destination}
        </Text>
        <View style={styles.ratingContainer}>
          <Text style={styles.ratingText}>★ {item.rating}</Text>
        </View>
      </Card.Content>
    </Card>
  );

  return (
    <View style={styles.container}>
      <ScrollView>
        <View style={styles.header}>
          <Text variant="headlineSmall" style={styles.title}>
            Ride History
          </Text>
        </View>
        {historyData.length > 0 ? (
          <FlatList
            data={historyData}
            renderItem={renderHistoryItem}
            keyExtractor={(item) => item.id}
            scrollEnabled={false}
          />
        ) : (
          <Card style={styles.emptyCard}>
            <Card.Content>
              <Text variant="bodyLarge" style={styles.emptyText}>
                No ride history yet
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
  historyCard: {
    margin: Spacing.md,
    marginTop: 0,
    borderRadius: BorderRadius.xl,
    backgroundColor: TimoColors.white,
    ...GlowStyles.primary,
  },
  historyHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: Spacing.sm,
  },
  historyDate: {
    color: TimoColors.textMid,
  },
  historyFare: {
    fontWeight: 'bold',
    color: TimoColors.accent,
  },
  divider: {
    marginVertical: Spacing.md,
  },
  riderName: {
    fontWeight: '600',
    marginBottom: Spacing.xs,
  },
  location: {
    color: TimoColors.textMid,
    marginBottom: Spacing.sm,
  },
  ratingContainer: {
    marginTop: Spacing.xs,
  },
  ratingText: {
    color: TimoColors.accent,
    fontSize: 16,
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

export default HistoryScreen;

