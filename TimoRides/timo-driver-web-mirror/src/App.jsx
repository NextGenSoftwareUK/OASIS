import React, { useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ThemeProvider, CssBaseline } from '@mui/material';
import { Provider, useSelector, useDispatch } from 'react-redux';
import { store } from './store/store';
import { checkAuth } from './store/slices/authSlice';
import { theme, globalStyles } from './utils/theme';
import ErrorBoundary from './ErrorBoundary';

// Screens
import LoginScreen from './screens/Auth/LoginScreen';
import RegisterScreen from './screens/Auth/RegisterScreen';
import HomeScreen from './screens/Home/HomeScreen';
import RideRequestScreen from './screens/Rides/RideRequestScreen';
import ActiveRideScreen from './screens/Rides/ActiveRideScreen';
import TripCompleteScreen from './screens/Rides/TripCompleteScreen';
import EarningsDashboard from './screens/Earnings/EarningsDashboard';
import ProfileScreen from './screens/Profile/ProfileScreen';
import HistoryScreen from './screens/History/HistoryScreen';
import NotificationsScreen from './screens/Notifications/NotificationsScreen';
import HelpScreen from './screens/Help/HelpScreen';
import SettingsScreen from './screens/Settings/SettingsScreen';

// Inject global styles
if (typeof document !== 'undefined') {
  const styleSheet = document.createElement('style');
  styleSheet.textContent = globalStyles;
  document.head.appendChild(styleSheet);
}

function App() {
  return (
    <ErrorBoundary>
      <Provider store={store}>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          <BrowserRouter>
            <AppRoutes />
          </BrowserRouter>
        </ThemeProvider>
      </Provider>
    </ErrorBoundary>
  );
}

const AppRoutes = () => {
  const dispatch = useDispatch();
  const { isAuthenticated } = useSelector((state) => state.auth);
  
  // Check for test token in localStorage
  const hasTestAuth = typeof window !== 'undefined' && localStorage.getItem('timo_driver_auth_token');
  const isAuth = isAuthenticated || hasTestAuth;

  // PrivateRoute must be defined inside AppRoutes to have access to Provider context
  const PrivateRoute = ({ children }) => {
    const { isAuthenticated: authState } = useSelector((state) => state.auth);
    // Also check localStorage for test token
    const hasTestAuth = typeof window !== 'undefined' && localStorage.getItem('timo_driver_auth_token');
    return (authState || hasTestAuth) ? children : <Navigate to="/login" />;
  };

  useEffect(() => {
    dispatch(checkAuth());
  }, [dispatch]);

  return (
    <Routes>
      <Route
        path="/login"
        element={isAuth ? <Navigate to="/home" /> : <LoginScreen />}
      />
      <Route
        path="/register"
        element={isAuth ? <Navigate to="/home" /> : <RegisterScreen />}
      />
      <Route
        path="/home"
        element={
          <PrivateRoute>
            <HomeScreen />
          </PrivateRoute>
        }
      />
      <Route
        path="/ride-request/:id"
        element={
          <PrivateRoute>
            <RideRequestScreen />
          </PrivateRoute>
        }
      />
      <Route
        path="/active-ride/:id"
        element={
          <PrivateRoute>
            <ActiveRideScreen />
          </PrivateRoute>
        }
      />
      <Route
        path="/trip-complete/:id"
        element={
          <PrivateRoute>
            <TripCompleteScreen />
          </PrivateRoute>
        }
      />
      <Route
        path="/earnings"
        element={
          <PrivateRoute>
            <EarningsDashboard />
          </PrivateRoute>
        }
      />
      <Route
        path="/profile"
        element={
          <PrivateRoute>
            <ProfileScreen />
          </PrivateRoute>
        }
      />
      <Route
        path="/history"
        element={
          <PrivateRoute>
            <HistoryScreen />
          </PrivateRoute>
        }
      />
      <Route
        path="/notifications"
        element={
          <PrivateRoute>
            <NotificationsScreen />
          </PrivateRoute>
        }
      />
      <Route
        path="/help"
        element={
          <PrivateRoute>
            <HelpScreen />
          </PrivateRoute>
        }
      />
      <Route
        path="/settings"
        element={
          <PrivateRoute>
            <SettingsScreen />
          </PrivateRoute>
        }
      />
      <Route path="/" element={<Navigate to="/login" />} />
    </Routes>
  );
}

export default App;

