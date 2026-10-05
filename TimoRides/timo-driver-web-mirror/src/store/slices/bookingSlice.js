import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { bookingsService } from '../../services/api/bookings';

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
    addSimulatedBooking: (state, action) => {
      const newBooking = {
        ...action.payload,
        status: action.payload.status || 'pending',
      };
      // Only add if not already in the list
      if (!state.pendingBookings.find((b) => b.id === newBooking.id)) {
        state.pendingBookings.push(newBooking);
      }
      if (!state.bookings.find((b) => b.id === newBooking.id)) {
        state.bookings.push(newBooking);
      }
    },
    removePendingBooking: (state, action) => {
      const bookingId = action.payload;
      state.pendingBookings = state.pendingBookings.filter((b) => b.id !== bookingId);
      state.bookings = state.bookings.filter((b) => b.id !== bookingId);
    },
    updateBookingStatus: (state, action) => {
      const { id, status } = action.payload;
      const bookingIndex = state.bookings.findIndex((b) => b.id === id);
      if (bookingIndex !== -1) {
        state.bookings[bookingIndex] = { ...state.bookings[bookingIndex], status };
      }
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchBookings.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(fetchBookings.fulfilled, (state, action) => {
        state.isLoading = false;
        state.bookings = action.payload;
        state.pendingBookings = action.payload.filter((b) => b.status === 'pending');
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
      })
      .addCase(acceptBooking.fulfilled, (state, action) => {
        const booking = action.payload;
        state.activeBooking = booking;
        state.pendingBookings = state.pendingBookings.filter(
          (b) => b.id !== booking.id
        );
        // Update booking status in bookings array
        const bookingIndex = state.bookings.findIndex((b) => b.id === booking.id);
        if (bookingIndex !== -1) {
          state.bookings[bookingIndex] = { ...state.bookings[bookingIndex], status: 'accepted' };
        }
      })
      .addCase(acceptBooking.rejected, (state, action) => {
        state.error = action.payload || 'Failed to accept booking';
      })
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

export const { setActiveBooking, clearActiveBooking, clearError, addSimulatedBooking, removePendingBooking, updateBookingStatus } = bookingSlice.actions;
export default bookingSlice.reducer;

