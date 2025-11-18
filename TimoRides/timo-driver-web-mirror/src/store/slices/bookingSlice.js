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

export const { setActiveBooking, clearActiveBooking, clearError } = bookingSlice.actions;
export default bookingSlice.reducer;

