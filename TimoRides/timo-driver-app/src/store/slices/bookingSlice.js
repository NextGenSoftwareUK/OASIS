import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { bookingsService } from '../../services/api/bookings';

// Async thunks
export const fetchBookings = createAsyncThunk(
  'bookings/fetchAll',
  async (_, { rejectWithValue }) => {
    try {
      const response = await bookingsService.getAllBookings();
      return response.bookings || [];
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch bookings');
    }
  }
);

export const fetchBooking = createAsyncThunk(
  'bookings/fetchOne',
  async (bookingId, { rejectWithValue }) => {
    try {
      const response = await bookingsService.getBooking(bookingId);
      return response.booking;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch booking');
    }
  }
);

export const acceptBooking = createAsyncThunk(
  'bookings/accept',
  async (bookingId, { rejectWithValue }) => {
    try {
      const response = await bookingsService.acceptBooking(bookingId);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to accept booking');
    }
  }
);

export const cancelBooking = createAsyncThunk(
  'bookings/cancel',
  async (bookingId, { rejectWithValue }) => {
    try {
      const response = await bookingsService.cancelBooking(bookingId);
      return response;
    } catch (error) {
      return rejectWithValue(error.response?.data?.message || 'Failed to cancel booking');
    }
  }
);

const bookingSlice = createSlice({
  name: 'bookings',
  initialState: {
    bookings: [],
    activeBooking: null,
    pendingBookings: [],
    isLoading: false,
    error: null,
  },
  reducers: {
    setActiveBooking: (state, action) => {
      state.activeBooking = action.payload;
    },
    clearActiveBooking: (state) => {
      state.activeBooking = null;
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    // Fetch All
    builder
      .addCase(fetchBookings.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(fetchBookings.fulfilled, (state, action) => {
        state.isLoading = false;
        state.bookings = action.payload;
        // Filter pending bookings
        state.pendingBookings = action.payload.filter(
          (b) => b.status === 'pending'
        );
        // Set active booking (accepted or started)
        const active = action.payload.find(
          (b) => b.status === 'accepted' || b.status === 'started'
        );
        if (active) {
          state.activeBooking = active;
        }
      })
      .addCase(fetchBookings.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload;
      });

    // Fetch One
    builder
      .addCase(fetchBooking.fulfilled, (state, action) => {
        state.activeBooking = action.payload;
      });

    // Accept
    builder
      .addCase(acceptBooking.fulfilled, (state, action) => {
        const booking = action.payload;
        state.activeBooking = booking;
        state.pendingBookings = state.pendingBookings.filter(
          (b) => b.id !== booking.id
        );
      });

    // Cancel
    builder
      .addCase(cancelBooking.fulfilled, (state, action) => {
        const bookingId = action.payload.id || action.meta.arg;
        state.pendingBookings = state.pendingBookings.filter(
          (b) => b.id !== bookingId
        );
        if (state.activeBooking?.id === bookingId) {
          state.activeBooking = null;
        }
      });
  },
});

export const { setActiveBooking, clearActiveBooking, clearError } = bookingSlice.actions;
export default bookingSlice.reducer;

