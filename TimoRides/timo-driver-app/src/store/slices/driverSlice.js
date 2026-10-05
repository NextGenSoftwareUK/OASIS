import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { driversService } from '../../services/api/drivers';

// Async thunks
export const getDriverStatus = createAsyncThunk(
  'driver/getStatus',
  async (driverId, { rejectWithValue }) => {
    try {
      const response = await driversService.getStatus(driverId);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to get driver status');
    }
  }
);

export const updateDriverStatus = createAsyncThunk(
  'driver/updateStatus',
  async ({ driverId, statusData }, { rejectWithValue }) => {
    try {
      const response = await driversService.updateStatus(driverId, statusData);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to update status');
    }
  }
);

export const updateDriverLocation = createAsyncThunk(
  'driver/updateLocation',
  async ({ driverId, locationData }, { rejectWithValue }) => {
    try {
      const response = await driversService.updateLocation(driverId, locationData);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to update location');
    }
  }
);

const driverSlice = createSlice({
  name: 'driver',
  initialState: {
    profile: null,
    car: null,
    isOnline: false,
    isOffline: true,
    isActive: false,
    location: null,
    isLoading: false,
    error: null,
  },
  reducers: {
    setLocation: (state, action) => {
      state.location = action.payload;
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    // Get Status
    builder
      .addCase(getDriverStatus.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(getDriverStatus.fulfilled, (state, action) => {
        state.isLoading = false;
        state.profile = action.payload.driver || action.payload;
        state.car = action.payload.car;
        state.isOnline = !action.payload.isOffline;
        state.isOffline = action.payload.isOffline || false;
        state.isActive = action.payload.isActive || false;
      })
      .addCase(getDriverStatus.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      });

    // Update Status
    builder
      .addCase(updateDriverStatus.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(updateDriverStatus.fulfilled, (state, action) => {
        state.isLoading = false;
        state.isOnline = !action.payload.isOffline;
        state.isOffline = action.payload.isOffline || false;
        state.isActive = action.payload.isActive || false;
        if (action.payload.car) {
          state.car = action.payload.car;
        }
      })
      .addCase(updateDriverStatus.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      });

    // Update Location
    builder
      .addCase(updateDriverLocation.fulfilled, (state, action) => {
        if (action.payload.location) {
          state.location = action.payload.location;
        }
      });
  },
});

export const { setLocation, clearError } = driverSlice.actions;
export default driverSlice.reducer;

