import React from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { Text, Card, Button } from 'react-native-paper';
import { Linking } from 'react-native';
import { TimoColors, Spacing, BorderRadius, GlowStyles } from '../../utils/theme';

const HelpScreen = () => {
  const helpSections = [
    {
      title: 'Getting Started',
      content: 'Learn how to start accepting rides and go online.',
    },
    {
      title: 'Accepting Rides',
      content: 'When you receive a ride request, you can accept or decline it. Accepted rides will appear in your active rides.',
    },
    {
      title: 'Navigation',
      content: 'Use the navigation buttons to get directions to pickup and destination locations.',
    },
    {
      title: 'Payments',
      content: 'Payments are processed automatically. Cash payments are collected directly from riders.',
    },
    {
      title: 'Support',
      content: 'Need help? Contact our support team at support@timorides.com or call +27 11 123 4567',
    },
  ];

  const handleContactSupport = () => {
    Linking.openURL('mailto:support@timorides.com');
  };

  return (
    <View style={styles.container}>
      <ScrollView>
        <View style={styles.header}>
          <Text variant="headlineSmall" style={styles.title}>
            Help & Support
          </Text>
        </View>

        {helpSections.map((section, index) => (
          <Card key={index} style={styles.helpCard}>
            <Card.Content>
              <Text variant="titleMedium" style={styles.sectionTitle}>
                {section.title}
              </Text>
              <Text variant="bodyMedium" style={styles.sectionContent}>
                {section.content}
              </Text>
            </Card.Content>
          </Card>
        ))}

        <Card style={styles.contactCard}>
          <Card.Content>
            <Text variant="titleLarge" style={styles.contactTitle}>
              Contact Support
            </Text>
            <Text variant="bodyMedium" style={styles.contactText}>
              Our support team is available 24/7 to help you with any questions or issues.
            </Text>
            <Button
              mode="contained"
              onPress={handleContactSupport}
              style={styles.contactButton}
              buttonColor={TimoColors.primary}
            >
              Email Support
            </Button>
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
  helpCard: {
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
  sectionContent: {
    color: TimoColors.textMid,
    lineHeight: 24,
  },
  contactCard: {
    margin: Spacing.lg,
    borderRadius: BorderRadius.xl,
    backgroundColor: TimoColors.primary,
    ...GlowStyles.primary,
  },
  contactTitle: {
    fontWeight: 'bold',
    color: TimoColors.white,
    marginBottom: Spacing.sm,
  },
  contactText: {
    color: TimoColors.white,
    opacity: 0.9,
    marginBottom: Spacing.lg,
  },
  contactButton: {
    marginTop: Spacing.sm,
  },
});

export default HelpScreen;

