// User Types
export interface User {
  id: string;
  email?: string;
  phone?: string;
  name?: string;
  avatar?: string;
}

// Driver Types
export interface Driver {
  id: string;
  name: string;
  photo?: string;
  rating: number;
  reviewCount: number;
  vehicle: {
    make: string;
    model: string;
    color: string;
    year?: number;
  };
  languages: string[];
  amenities: string[];
  karmaScore?: number;
  eta?: number; // in minutes
  fareEstimate?: number; // in ZAR
  location: {
    latitude: number;
    longitude: number;
  };
}

// Booking Types
export interface Booking {
  id: string;
  driverId: string;
  userId: string;
  pickup: {
    latitude: number;
    longitude: number;
    address: string;
  };
  destination: {
    latitude: number;
    longitude: number;
    address: string;
  };
  fare: number;
  status: 'pending' | 'accepted' | 'in-progress' | 'completed' | 'cancelled';
  phoneNumber: string;
  createdAt: string;
  updatedAt: string;
}

// Payment Types
export type PaymentMethod = 'cash' | 'mobile-money' | 'crypto' | 'wallet';

export interface Payment {
  id: string;
  bookingId: string;
  amount: number;
  method: PaymentMethod;
  status: 'pending' | 'completed' | 'failed';
  createdAt: string;
}

// Navigation Types
export type RootStackParamList = {
  Splash: undefined;
  Onboarding: undefined;
  Auth: undefined;
  Login: undefined;
  OTP: { phone: string };
  Signup: undefined;
  Home: undefined;
  DriverSelection: {
    pickup: { latitude: number; longitude: number; address: string };
    destination: { latitude: number; longitude: number; address: string };
  };
  Booking: { bookingId: string };
  History: undefined;
  Settings: undefined;
  Profile: undefined;
};

