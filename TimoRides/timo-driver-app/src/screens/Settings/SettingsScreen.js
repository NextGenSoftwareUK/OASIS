import React from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { Text, Card, Switch, List, Divider } from 'react-native-paper';
import { TimoColors, Spacing, BorderRadius, GlowStyles } from '../../utils/theme';

const SettingsScreen = () => {
  const [notificationsEnabled, setNotificationsEnabled] = React.useState(true);
  const [locationEnabled, setLocationEnabled] = React.useState(true);
  const [autoAccept, setAutoAccept] = React.useState(false);

  return (
    <View style={styles.container}>
      <ScrollView>
        <View style={styles.header}>
          <Text variant="headlineSmall" style={styles.title}>
            Settings
          </Text>
        </View>

        <Card style={styles.settingsCard}>
          <Card.Content>
            <Text variant="titleMedium" style={styles.sectionTitle}>
              Preferences
            </Text>
            <Divider style={styles.divider} />

            <List.Item
              title="Push Notifications"
              description="Receive notifications for new ride requests"
              right={() => (
                <Switch
                  value={notificationsEnabled}
                  onValueChange={setNotificationsEnabled}
                  color={TimoColors.primary}
                />
              )}
            />
            <Divider />

            <List.Item
              title="Location Services"
              description="Allow app to access your location"
              right={() => (
                <Switch
                  value={locationEnabled}
                  onValueChange={setLocationEnabled}
                  color={TimoColors.primary}
                />
              )}
            />
            <Divider />

            <List.Item
              title="Auto Accept Rides"
              description="Automatically accept ride requests"
              right={() => (
                <Switch
                  value={autoAccept}
                  onValueChange={setAutoAccept}
                  color={TimoColors.primary}
                />
              )}
            />
          </Card.Content>
        </Card>

        <Card style={styles.settingsCard}>
          <Card.Content>
            <Text variant="titleMedium" style={styles.sectionTitle}>
              Account
            </Text>
            <Divider style={styles.divider} />

            <List.Item
              title="Edit Profile"
              description="Update your personal information"
              onPress={() => {}}
              right={(props) => <List.Icon {...props} icon="chevron-right" />}
            />
            <Divider />

            <List.Item
              title="Payment Methods"
              description="Manage your payment settings"
              onPress={() => {}}
              right={(props) => <List.Icon {...props} icon="chevron-right" />}
            />
            <Divider />

            <List.Item
              title="Vehicle Information"
              description="Update your vehicle details"
              onPress={() => {}}
              right={(props) => <List.Icon {...props} icon="chevron-right" />}
            />
          </Card.Content>
        </Card>

        <Card style={styles.settingsCard}>
          <Card.Content>
            <Text variant="titleMedium" style={styles.sectionTitle}>
              About
            </Text>
            <Divider style={styles.divider} />

            <List.Item
              title="App Version"
              description="1.0.0"
            />
            <Divider />

            <List.Item
              title="Terms of Service"
              onPress={() => {}}
              right={(props) => <List.Icon {...props} icon="chevron-right" />}
            />
            <Divider />

            <List.Item
              title="Privacy Policy"
              onPress={() => {}}
              right={(props) => <List.Icon {...props} icon="chevron-right" />}
            />
          </Card.Content>
        </Card>
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
  settingsCard: {
    margin: Spacing.md,
    marginTop: 0,
    borderRadius: BorderRadius.xl,
    backgroundColor: TimoColors.white,
    ...GlowStyles.primary,
  },
  sectionTitle: {
    fontWeight: '600',
    marginBottom: Spacing.sm,
    color: TimoColors.primary,
  },
  divider: {
    marginVertical: Spacing.sm,
  },
});

export default SettingsScreen;

