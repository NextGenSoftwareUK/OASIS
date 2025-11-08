import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAvatar } from '../contexts/AvatarContext';
import { useDemoMode } from '../contexts/DemoModeContext';
import { Box, CircularProgress } from '@mui/material';

interface ProtectedRouteProps {
  children: React.ReactNode;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children }) => {
  const { isLoggedIn, isLoading } = useAvatar();
  const { isDemoMode } = useDemoMode();

  // Show loading spinner while checking authentication
  if (isLoading) {
    return (
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          minHeight: '100vh',
        }}
      >
        <CircularProgress />
      </Box>
    );
  }

  // Allow access if in demo mode OR if logged in
  if (!isLoggedIn && !isDemoMode) {
    return <Navigate to="/avatar/signin" replace />;
  }

  // Render children if authenticated or in demo mode
  return <>{children}</>;
};

export default ProtectedRoute;
