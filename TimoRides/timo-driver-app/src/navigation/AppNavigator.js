import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createStackNavigator } from '@react-navigation/stack';
import { useSelector } from 'react-redux';

// Auth Screens
import SplashScreen from '../screens/Auth/SplashScreen';
import LoginScreen from '../screens/Auth/LoginScreen';
import RegisterScreen from '../screens/Auth/RegisterScreen';

// Main Screens
import HomeScreen from '../screens/Home/HomeScreen';
import RideRequestScreen from '../screens/Rides/RideRequestScreen';
import ActiveRideScreen from '../screens/Rides/ActiveRideScreen';
import EarningsScreen from '../screens/Earnings/EarningsDashboard';
import ProfileScreen from '../screens/Profile/ProfileScreen';

const Stack = createStackNavigator();

const AppNavigator = () => {
  const { isAuthenticated, isLoading } = useSelector((state) => state.auth);

  if (isLoading) {
    return <SplashScreen />;
  }

  return (
    <NavigationContainer>
      <Stack.Navigator
        screenOptions={{
          headerShown: false,
          cardStyle: { backgroundColor: '#FFFFFF' },
        }}
      >
        {!isAuthenticated ? (
          // Auth Stack
          <>
            <Stack.Screen name="Splash" component={SplashScreen} />
            <Stack.Screen name="Login" component={LoginScreen} />
            <Stack.Screen name="Register" component={RegisterScreen} />
          </>
        ) : (
          // Main Stack
          <>
            <Stack.Screen name="Home" component={HomeScreen} />
            <Stack.Screen 
              name="RideRequest" 
              component={RideRequestScreen}
              options={{ headerShown: true, title: 'Ride Request' }}
            />
            <Stack.Screen 
              name="ActiveRide" 
              component={ActiveRideScreen}
              options={{ headerShown: true, title: 'Active Ride' }}
            />
            <Stack.Screen 
              name="Earnings" 
              component={EarningsScreen}
              options={{ headerShown: true, title: 'Earnings' }}
            />
            <Stack.Screen 
              name="Profile" 
              component={ProfileScreen}
              options={{ headerShown: true, title: 'Profile' }}
            />
          </>
        )}
      </Stack.Navigator>
    </NavigationContainer>
  );
};

export default AppNavigator;

