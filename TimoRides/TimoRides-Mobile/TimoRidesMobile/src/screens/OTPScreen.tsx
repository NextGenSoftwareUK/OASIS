import React, { useState, useRef, useEffect } from 'react';
import { View, StyleSheet, KeyboardAvoidingView, Platform } from 'react-native';
import { Text, TextInput, Button, Card } from 'react-native-paper';
import { useNavigation, useRoute, RouteProp } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { RootStackParamList } from '../types';
import TimoColors from '../theme/colors';
import ApiService from '../services/api';
import AsyncStorage from '@react-native-async-storage/async-storage';

type OTPScreenRouteProp = RouteProp<RootStackParamList, 'OTP'>;
type OTPScreenNavigationProp = StackNavigationProp<RootStackParamList, 'OTP'>;

const OTPScreen: React.FC = () => {
  const navigation = useNavigation<OTPScreenNavigationProp>();
  const route = useRoute<OTPScreenRouteProp>();
  const { phone } = route.params;

  const [otp, setOtp] = useState(['', '', '', '', '', '']);
  const [loading, setLoading] = useState(false);
  const inputRefs = useRef<(TextInput | null)[]>([]);

  useEffect(() => {
    // Auto-focus first input
    inputRefs.current[0]?.focus();
  }, []);

  const handleOtpChange = (value: string, index: number) => {
    if (value.length > 1) {
      // Handle paste
      const pastedOtp = value.split('').slice(0, 6);
      const newOtp = [...otp];
      pastedOtp.forEach((digit, i) => {
        if (index + i < 6) {
          newOtp[index + i] = digit;
        }
      });
      setOtp(newOtp);
      inputRefs.current[Math.min(index + pastedOtp.length, 5)]?.focus();
      return;
    }

    const newOtp = [...otp];
    newOtp[index] = value;
    setOtp(newOtp);

    // Auto-focus next input
    if (value && index < 5) {
      inputRefs.current[index + 1]?.focus();
    }
  };

  const handleKeyPress = (e: any, index: number) => {
    if (e.nativeEvent.key === 'Backspace' && !otp[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
    }
  };

  const handleVerifyOTP = async () => {
    const otpCode = otp.join('');
    if (otpCode.length !== 6) {
      // Show error
      return;
    }

    setLoading(true);
    try {
      const response = await ApiService.verifyOTP(phone, otpCode);
      await AsyncStorage.setItem('auth_token', response.token);
      await AsyncStorage.setItem('user', JSON.stringify(response.user));
      navigation.replace('Home');
    } catch (error) {
      console.error('OTP verification error:', error);
      // Handle error - show error message
      setOtp(['', '', '', '', '', '']);
      inputRefs.current[0]?.focus();
    } finally {
      setLoading(false);
    }
  };

  const handleResendOTP = async () => {
    try {
      await ApiService.sendOTP(phone);
      // Show success message
    } catch (error) {
      console.error('Resend OTP error:', error);
    }
  };

  return (
    <KeyboardAvoidingView
      style={styles.container}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <View style={styles.content}>
        <View style={styles.header}>
          <Text variant="displaySmall" style={styles.title}>
            Enter Verification Code
          </Text>
          <Text variant="bodyMedium" style={styles.subtitle}>
            We've sent a 6-digit code to{'\n'}
            <Text style={styles.phone}>{phone}</Text>
          </Text>
        </View>

        <Card style={styles.card} elevation={4}>
          <Card.Content style={styles.cardContent}>
            <View style={styles.otpContainer}>
              {otp.map((digit, index) => (
                <TextInput
                  key={index}
                  ref={(ref) => (inputRefs.current[index] = ref)}
                  value={digit}
                  onChangeText={(value) => handleOtpChange(value, index)}
                  onKeyPress={(e) => handleKeyPress(e, index)}
                  mode="outlined"
                  keyboardType="number-pad"
                  maxLength={1}
                  style={styles.otpInput}
                  textStyle={styles.otpText}
                  selectTextOnFocus
                />
              ))}
            </View>

            <Button
              mode="contained"
              onPress={handleVerifyOTP}
              loading={loading}
              disabled={loading || otp.join('').length !== 6}
              style={styles.button}
              contentStyle={styles.buttonContent}
            >
              Verify
            </Button>

            <View style={styles.resendContainer}>
              <Text style={styles.resendText}>Didn't receive the code? </Text>
              <Button
                mode="text"
                onPress={handleResendOTP}
                labelStyle={styles.resendButton}
              >
                Resend
              </Button>
            </View>
          </Card.Content>
        </Card>
      </View>
    </KeyboardAvoidingView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: TimoColors.blue.primary,
  },
  content: {
    flex: 1,
    paddingTop: 64,
    paddingHorizontal: 24,
    paddingBottom: 32,
    justifyContent: 'center',
  },
  header: {
    alignItems: 'center',
    marginBottom: 32,
  },
  title: {
    color: TimoColors.white,
    fontWeight: '700',
    marginBottom: 16,
    textAlign: 'center',
  },
  subtitle: {
    color: TimoColors.white,
    textAlign: 'center',
    opacity: 0.9,
  },
  phone: {
    fontWeight: '600',
  },
  card: {
    borderRadius: 24,
  },
  cardContent: {
    padding: 24,
  },
  otpContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 32,
  },
  otpInput: {
    width: 48,
    height: 56,
    textAlign: 'center',
    fontSize: 24,
    fontWeight: '600',
  },
  otpText: {
    fontSize: 24,
    fontWeight: '600',
    textAlign: 'center',
  },
  button: {
    borderRadius: 28,
    marginBottom: 16,
  },
  buttonContent: {
    paddingVertical: 8,
  },
  resendContainer: {
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    marginTop: 16,
  },
  resendText: {
    color: TimoColors.gray[600],
    fontSize: 14,
  },
  resendButton: {
    fontSize: 14,
    fontWeight: '600',
  },
});

export default OTPScreen;

