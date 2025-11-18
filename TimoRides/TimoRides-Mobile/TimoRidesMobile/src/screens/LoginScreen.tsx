import React, { useState } from 'react';
import { View, StyleSheet, ScrollView, KeyboardAvoidingView, Platform } from 'react-native';
import { Text, TextInput, Button, Card, SegmentedButtons } from 'react-native-paper';
import { useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { RootStackParamList } from '../types';
import TimoColors from '../theme/colors';
import ApiService from '../services/api';
import AsyncStorage from '@react-native-async-storage/async-storage';

type LoginScreenNavigationProp = StackNavigationProp<RootStackParamList, 'Login'>;

const LoginScreen: React.FC = () => {
  const navigation = useNavigation<LoginScreenNavigationProp>();
  const [mode, setMode] = useState<'signup' | 'signin'>('signin');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [phone, setPhone] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);

  const handleLogin = async () => {
    if (mode === 'signin') {
      if (!phone) {
        // Show error
        return;
      }
      setLoading(true);
      try {
        // Send OTP
        await ApiService.sendOTP(phone);
        navigation.navigate('OTP', { phone });
      } catch (error) {
        console.error('Login error:', error);
        // Handle error
      } finally {
        setLoading(false);
      }
    } else {
      // Sign up
      if (!email || !password) {
        return;
      }
      setLoading(true);
      try {
        const response = await ApiService.register(email, password);
        await AsyncStorage.setItem('auth_token', response.token);
        await AsyncStorage.setItem('user', JSON.stringify(response.user));
        navigation.replace('Home');
      } catch (error) {
        console.error('Signup error:', error);
      } finally {
        setLoading(false);
      }
    }
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
        <View style={styles.header}>
          <View style={styles.logoPlaceholder}>
            <Text style={styles.logoText}>T</Text>
          </View>
        </View>

        <Card style={styles.card} elevation={4}>
          <Card.Content style={styles.cardContent}>
            <SegmentedButtons
              value={mode}
              onValueChange={(value) => setMode(value as 'signup' | 'signin')}
              buttons={[
                { value: 'signup', label: 'Sign Up' },
                { value: 'signin', label: 'Sign In' },
              ]}
              style={styles.segmentedButtons}
            />

            {mode === 'signin' ? (
              <View style={styles.form}>
                <TextInput
                  label="Phone Number"
                  value={phone}
                  onChangeText={setPhone}
                  mode="outlined"
                  keyboardType="phone-pad"
                  style={styles.input}
                  left={<TextInput.Icon icon="phone" />}
                />
                <Button
                  mode="contained"
                  onPress={handleLogin}
                  loading={loading}
                  disabled={loading}
                  style={styles.button}
                  contentStyle={styles.buttonContent}
                >
                  Continue
                </Button>
              </View>
            ) : (
              <View style={styles.form}>
                <TextInput
                  label="Email Address"
                  value={email}
                  onChangeText={setEmail}
                  mode="outlined"
                  keyboardType="email-address"
                  autoCapitalize="none"
                  style={styles.input}
                  left={<TextInput.Icon icon="email" />}
                />
                <TextInput
                  label="Password"
                  value={password}
                  onChangeText={setPassword}
                  mode="outlined"
                  secureTextEntry={!showPassword}
                  style={styles.input}
                  left={<TextInput.Icon icon="lock" />}
                  right={
                    <TextInput.Icon
                      icon={showPassword ? 'eye-off' : 'eye'}
                      onPress={() => setShowPassword(!showPassword)}
                    />
                  }
                />
                <Button
                  mode="contained"
                  onPress={handleLogin}
                  loading={loading}
                  disabled={loading}
                  style={styles.button}
                  contentStyle={styles.buttonContent}
                >
                  Create Account
                </Button>
              </View>
            )}

            <View style={styles.divider}>
              <View style={styles.dividerLine} />
              <Text style={styles.dividerText}>OR</Text>
              <View style={styles.dividerLine} />
            </View>

            <Button
              mode="outlined"
              icon="facebook"
              onPress={() => {
                // Handle Facebook login
              }}
              style={styles.socialButton}
              labelStyle={styles.socialButtonLabel}
            >
              Continue with Facebook
            </Button>

            <Text style={styles.terms}>
              By clicking start, you agree to our{' '}
              <Text style={styles.termsLink}>Terms and Conditions</Text>
            </Text>
          </Card.Content>
        </Card>
      </ScrollView>
    </KeyboardAvoidingView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: TimoColors.blue.primary,
  },
  scrollContent: {
    flexGrow: 1,
    paddingTop: 64,
    paddingHorizontal: 24,
    paddingBottom: 32,
  },
  header: {
    alignItems: 'center',
    marginBottom: 32,
  },
  logoPlaceholder: {
    width: 120,
    height: 120,
    borderRadius: 24,
    backgroundColor: TimoColors.white,
    alignItems: 'center',
    justifyContent: 'center',
  },
  logoText: {
    fontSize: 64,
    fontWeight: '700',
    color: TimoColors.blue.primary,
  },
  card: {
    borderRadius: 24,
  },
  cardContent: {
    padding: 24,
  },
  segmentedButtons: {
    marginBottom: 24,
  },
  form: {
    marginBottom: 16,
  },
  input: {
    marginBottom: 16,
  },
  button: {
    marginTop: 8,
    borderRadius: 28,
  },
  buttonContent: {
    paddingVertical: 8,
  },
  divider: {
    flexDirection: 'row',
    alignItems: 'center',
    marginVertical: 24,
  },
  dividerLine: {
    flex: 1,
    height: 1,
    backgroundColor: TimoColors.gray[300],
  },
  dividerText: {
    marginHorizontal: 16,
    color: TimoColors.gray[600],
    fontSize: 14,
  },
  socialButton: {
    borderRadius: 28,
    borderColor: '#1877F2',
  },
  socialButtonLabel: {
    color: '#1877F2',
  },
  terms: {
    marginTop: 24,
    textAlign: 'center',
    fontSize: 12,
    color: TimoColors.gray[600],
  },
  termsLink: {
    color: TimoColors.blue.primary,
    fontWeight: '600',
  },
});

export default LoginScreen;

