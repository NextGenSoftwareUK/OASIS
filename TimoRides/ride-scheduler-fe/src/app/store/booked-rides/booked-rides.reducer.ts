import { createReducer, on } from '@ngrx/store';
import * as BookedRideActions from './booked-rides.actions';
import { BookedRidesType } from 'src/app/models/booking-form';
import { TripStatusResponse } from 'src/app/models/trip';

export interface BookedRideState {
  bookings: BookedRidesType | null;
  loading: boolean;
  rideInfo: TripStatusResponse | null;
  error: any;
}

export const initialState: BookedRideState = {
  bookings: null,
  loading: false,
  rideInfo: null,
  error: null,
};

export const bookedRideReducer = createReducer(
  initialState,
  on(BookedRideActions.loadBookings, (state) => ({
    ...state,
    loading: true,
    error: null,
  })),
  on(BookedRideActions.loadBookingsSuccess, (state, { bookings }) => ({
    ...state,
    bookings,
    loading: false,
    error: null,
  })),
  on(BookedRideActions.loadBookingsFailure, (state, { error }) => ({
    ...state,
    loading: false,
    error,
  })),
  on(BookedRideActions.acceptRide, (state) => ({
    ...state,
    loading: true,
    error: null,
  })),
  on(BookedRideActions.acceptRideSuccess, (state, { booking }) => ({
    ...state,
    bookings: state.bookings
      ? {
          ...state.bookings,
          bookings: state.bookings.bookings.map((b) =>
            b.id === booking.id ? booking : b
          ),
        }
      : null,
    loading: false,
    error: null,
  })),
  on(BookedRideActions.acceptRideFailure, (state, { error }) => ({
    ...state,
    loading: false,
    error,
  })),
  on(BookedRideActions.cancelRide, (state) => ({
    ...state,
    loading: true,
    error: null,
  })),
  on(BookedRideActions.cancelRideSuccess, (state, { booking }) => ({
    ...state,
    bookings: state.bookings
      ? {
          ...state.bookings,
          bookings: state.bookings.bookings.map((b) =>
            b.id === booking.id ? booking : b
          ),
        }
      : null,
    loading: false,
    error: null,
  })),
  on(BookedRideActions.cancelRideFailure, (state, { error }) => ({
    ...state,
    loading: false,
    error,
  })),
  on(BookedRideActions.clearBookedRideState, (state) => ({
    ...state,
    bookings: null,
    loading: false,
    error: null,
  })),
  on(BookedRideActions.startRideSuccess, (state, { rideInfo }) => ({
    ...state,
    bookings: state.bookings
      ? {
          ...state.bookings,
          bookings: state.bookings.bookings.map((b) =>
            b.id === rideInfo.bookingId ? { ...b, status: 'started' } : b
          ),
        }
      : null,
    rideInfo,
    error: null,
  })),
  on(BookedRideActions.startRideFailure, (state, { error }) => ({
    ...state,
    rideInfo: null,
    error,
  })),
  on(BookedRideActions.endRideSuccess, (state, { rideInfo }) => ({
    ...state,
    bookings: state.bookings
      ? {
          ...state.bookings,
          bookings: state.bookings.bookings.map((b) =>
            b.id === rideInfo.bookingId ? { ...b, status: 'completed' } : b
          ),
        }
      : null,
    rideInfo,
    error: null,
  })),
  on(BookedRideActions.endRideFailure, (state, { error }) => ({
    ...state,
    rideInfo: null,
    error,
  }))
);
