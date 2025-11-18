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
import { signup } from '../../store/slices/authSlice';
import { TimoColors, Spacing, BorderRadius, GlowStyles } from '../../utils/theme';

const RegisterScreen = ({ navigation }) => {
  const dispatch = useDispatch();
  const { isLoading, error } = useSelector((state) => state.auth);

  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    phone: '',
    password: '',
    confirmPassword: '',
  });
  const [showPassword, setShowPassword] = useState(false);

  const handleRegister = async () => {
    if (!formData.fullName || !formData.email || !formData.phone || !formData.password) {
      return;
    }
    if (formData.password !== formData.confirmPassword) {
      return;
    }
    await dispatch(signup({
      fullName: formData.fullName,
      email: formData.email,
      phone: formData.phone,
      password: formData.password,
      role: 'driver',
    }));
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
              Driver Registration
            </Text>
            <Text variant="bodyMedium" style={styles.subtitle}>
              Create your driver account
            </Text>

            {error && (
              <Text style={styles.errorText} variant="bodySmall">
                {error}
              </Text>
            )}

            <TextInput
              label="Full Name"
              value={formData.fullName}
              onChangeText={(text) => setFormData({ ...formData, fullName: text })}
              mode="outlined"
              style={styles.input}
              theme={{ colors: { primary: TimoColors.primary } }}
            />

            <TextInput
              label="Email"
              value={formData.email}
              onChangeText={(text) => setFormData({ ...formData, email: text })}
              mode="outlined"
              keyboardType="email-address"
              autoCapitalize="none"
              style={styles.input}
              theme={{ colors: { primary: TimoColors.primary } }}
            />

            <TextInput
              label="Phone Number"
              value={formData.phone}
              onChangeText={(text) => setFormData({ ...formData, phone: text })}
              mode="outlined"
              keyboardType="phone-pad"
              style={styles.input}
              theme={{ colors: { primary: TimoColors.primary } }}
            />

            <TextInput
              label="Password"
              value={formData.password}
              onChangeText={(text) => setFormData({ ...formData, password: text })}
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

            <TextInput
              label="Confirm Password"
              value={formData.confirmPassword}
              onChangeText={(text) => setFormData({ ...formData, confirmPassword: text })}
              mode="outlined"
              secureTextEntry={!showPassword}
              style={styles.input}
              theme={{ colors: { primary: TimoColors.primary } }}
            />

            <Button
              mode="contained"
              onPress={handleRegister}
              loading={isLoading}
              disabled={isLoading}
              style={[styles.button, GlowStyles.primary]}
              buttonColor={TimoColors.primary}
              textColor={TimoColors.white}
            >
              Sign Up
            </Button>

            <Button
              mode="text"
              onPress={() => navigation.navigate('Login')}
              style={styles.linkButton}
              textColor={TimoColors.primary}
            >
              Already have an account? Sign in
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

export default RegisterScreen;

