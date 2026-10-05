import { configureStore } from '@reduxjs/toolkit';
import authReducer from './slices/authSlice';
import driverReducer from './slices/driverSlice';
import bookingReducer from './slices/bookingSlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    driver: driverReducer,
    bookings: bookingReducer,
  },
});

