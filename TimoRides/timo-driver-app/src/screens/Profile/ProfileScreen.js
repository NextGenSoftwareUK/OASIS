import React from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { Text, Card, Button, List } from 'react-native-paper';
import { useDispatch, useSelector } from 'react-redux';
import { useNavigation } from '@react-navigation/native';
import { logout } from '../../store/slices/authSlice';
import { TimoColors, Spacing, BorderRadius, GlowStyles } from '../../utils/theme';

const ProfileScreen = () => {
  const dispatch = useDispatch();
  const navigation = useNavigation();
  const { user } = useSelector((state) => state.auth);
  const { isOnline } = useSelector((state) => state.driver);

  const handleLogout = () => {
    dispatch(logout());
  };

  return (
    <ScrollView style={styles.container}>
      <Card style={styles.profileCard}>
        <Card.Content style={styles.profileContent}>
          <View style={styles.avatar}>
            <Text style={styles.avatarText}>
              {user?.fullName?.charAt(0) || 'D'}
            </Text>
          </View>
          <Text variant="headlineSmall" style={styles.name}>
            {user?.fullName || 'Driver'}
          </Text>
          <Text variant="bodyMedium" style={styles.email}>
            {user?.email || ''}
          </Text>
          <Text variant="bodySmall" style={styles.phone}>
            {user?.phone || ''}
          </Text>
          <View style={styles.statusBadge}>
            <Text style={styles.statusText}>
              {isOnline ? '🟢 Online' : '⚫ Offline'}
            </Text>
          </View>
        </Card.Content>
      </Card>

      <Card style={styles.menuCard}>
        <Card.Content>
          <List.Item
            title="Earnings"
            left={(props) => <List.Icon {...props} icon="cash" />}
            right={(props) => <List.Icon {...props} icon="chevron-right" />}
            onPress={() => navigation.navigate('Earnings')}
          />
          <List.Item
            title="Settings"
            left={(props) => <List.Icon {...props} icon="cog" />}
            right={(props) => <List.Icon {...props} icon="chevron-right" />}
          />
          <List.Item
            title="Help & Support"
            left={(props) => <List.Icon {...props} icon="help-circle" />}
            right={(props) => <List.Icon {...props} icon="chevron-right" />}
          />
        </Card.Content>
      </Card>

      <View style={styles.logoutContainer}>
        <Button
          mode="contained"
          onPress={handleLogout}
          style={styles.logoutButton}
          buttonColor={TimoColors.error}
          textColor={TimoColors.white}
        >
          Logout
        </Button>
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: TimoColors.backgroundLight,
  },
  profileCard: {
    margin: Spacing.md,
    borderRadius: BorderRadius.xl,
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    ...GlowStyles.primary,
  },
  profileContent: {
    alignItems: 'center',
    paddingVertical: Spacing.xl,
  },
  avatar: {
    width: 100,
    height: 100,
    borderRadius: 50,
    backgroundColor: TimoColors.primary,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: Spacing.md,
  },
  avatarText: {
    color: TimoColors.white,
    fontSize: 40,
    fontWeight: 'bold',
  },
  name: {
    fontWeight: 'bold',
    color: TimoColors.textMain,
    marginBottom: Spacing.xs,
  },
  email: {
    color: TimoColors.textMid,
    marginBottom: Spacing.xs,
  },
  phone: {
    color: TimoColors.textMid,
    marginBottom: Spacing.md,
  },
  statusBadge: {
    backgroundColor: TimoColors.backgroundLight,
    paddingHorizontal: Spacing.md,
    paddingVertical: Spacing.xs,
    borderRadius: BorderRadius.round,
  },
  statusText: {
    color: TimoColors.textMain,
    fontWeight: '600',
  },
  menuCard: {
    margin: Spacing.md,
    borderRadius: BorderRadius.xl,
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
  },
  logoutContainer: {
    padding: Spacing.md,
  },
  logoutButton: {
    paddingVertical: Spacing.xs,
  },
});

export default ProfileScreen;

