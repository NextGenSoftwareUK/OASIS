import React, { useState } from 'react';
import {
  View,
  StyleSheet,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
} from 'react-native';
import { TextInput, Button, Text, Card } from 'react-native-paper';
import { useDispatch, useSelector } from 'react-redux';
import { login } from '../../store/slices/authSlice';
import { TimoColors, Spacing, BorderRadius, GlowStyles } from '../../utils/theme';

const LoginScreen = ({ navigation }) => {
  const dispatch = useDispatch();
  const { isLoading, error } = useSelector((state) => state.auth);

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);

  const handleLogin = async () => {
    if (!email || !password) {
      return;
    }
    await dispatch(login({ email, password }));
  };

  return (
    <KeyboardAvoidingView
      style={styles.container}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView
        contentContainerStyle={styles.scrollContent}
        keyboardShouldPersistTaps="handled"
      >
        <Card style={styles.card}>
          <Card.Content>
            <Text variant="headlineMedium" style={styles.title}>
              Driver Login
            </Text>
            <Text variant="bodyMedium" style={styles.subtitle}>
              Sign in to start accepting rides
            </Text>

            {error && (
              <Text style={styles.errorText} variant="bodySmall">
                {error}
              </Text>
            )}

            <TextInput
              label="Email"
              value={email}
              onChangeText={setEmail}
              mode="outlined"
              keyboardType="email-address"
              autoCapitalize="none"
              style={styles.input}
              theme={{ colors: { primary: TimoColors.primary } }}
            />

            <TextInput
              label="Password"
              value={password}
              onChangeText={setPassword}
              mode="outlined"
              secureTextEntry={!showPassword}
              right={
                <TextInput.Icon
                  icon={showPassword ? 'eye-off' : 'eye'}
                  onPress={() => setShowPassword(!showPassword)}
                />
              }
              style={styles.input}
              theme={{ colors: { primary: TimoColors.primary } }}
            />

            <Button
              mode="contained"
              onPress={handleLogin}
              loading={isLoading}
              disabled={!email || !password || isLoading}
              style={[styles.button, GlowStyles.primary]}
              buttonColor={TimoColors.primary}
              textColor={TimoColors.white}
            >
              Login
            </Button>

            <Button
              mode="text"
              onPress={() => navigation.navigate('Register')}
              style={styles.linkButton}
              textColor={TimoColors.primary}
            >
              Don't have an account? Sign up
            </Button>
          </Card.Content>
        </Card>
      </ScrollView>
    </KeyboardAvoidingView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: TimoColors.backgroundLight,
  },
  scrollContent: {
    flexGrow: 1,
    justifyContent: 'center',
    padding: Spacing.lg,
  },
  card: {
    borderRadius: BorderRadius.xl,
    backgroundColor: 'rgba(255, 255, 255, 0.95)',
    ...GlowStyles.primary,
  },
  title: {
    textAlign: 'center',
    marginBottom: Spacing.sm,
    fontWeight: 'bold',
    color: TimoColors.textMain,
  },
  subtitle: {
    textAlign: 'center',
    marginBottom: Spacing.xl,
    color: TimoColors.textMid,
  },
  input: {
    marginBottom: Spacing.md,
    backgroundColor: TimoColors.white,
  },
  button: {
    marginTop: Spacing.md,
    marginBottom: Spacing.sm,
    paddingVertical: Spacing.xs,
  },
  linkButton: {
    marginTop: Spacing.sm,
  },
  errorText: {
    color: TimoColors.error,
    marginBottom: Spacing.md,
    textAlign: 'center',
  },
});

export default LoginScreen;

