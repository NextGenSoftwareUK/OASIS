import React, { useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ThemeProvider, CssBaseline } from '@mui/material';
import { Provider, useSelector, useDispatch } from 'react-redux';
import { store } from './store/store';
import { checkAuth } from './store/slices/authSlice';
import { theme, globalStyles } from './utils/theme';

// Screens
import LoginScreen from './screens/Auth/LoginScreen';
import RegisterScreen from './screens/Auth/RegisterScreen';
import HomeScreen from './screens/Home/HomeScreen';
import RideRequestScreen from './screens/Rides/RideRequestScreen';
import EarningsDashboard from './screens/Earnings/EarningsDashboard';
import ProfileScreen from './screens/Profile/ProfileScreen';

// Inject global styles
const styleSheet = document.createElement('style');
styleSheet.textContent = globalStyles;
document.head.appendChild(styleSheet);

const PrivateRoute = ({ children }) => {
  const { isAuthenticated } = useSelector((state) => state.auth);
  return isAuthenticated ? children : <Navigate to="/login" />;
};

const AppRoutes = () => {
  const dispatch = useDispatch();
  const { isAuthenticated } = useSelector((state) => state.auth);

  useEffect(() => {
    dispatch(checkAuth());
  }, [dispatch]);

  return (
    <Routes>
      <Route
        path="/login"
        element={isAuthenticated ? <Navigate to="/home" /> : <LoginScreen />}
      />
      <Route
        path="/register"
        element={isAuthenticated ? <Navigate to="/home" /> : <RegisterScreen />}
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
      <Route path="/" element={<Navigate to="/login" />} />
    </Routes>
  );
};

function App() {
  return (
    <Provider store={store}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <BrowserRouter>
          <AppRoutes />
        </BrowserRouter>
      </ThemeProvider>
    </Provider>
  );
}

export default App;

