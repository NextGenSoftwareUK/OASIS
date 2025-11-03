import { createFeatureSelector, createSelector } from '@ngrx/store';
import { BookedRideState } from './booked-rides.reducer';

export const selectBookedRideState =
  createFeatureSelector<BookedRideState>('bookedRides');

export const selectAllBookings = createSelector(
  selectBookedRideState,
  (state: BookedRideState) => state.bookings
);

export const selectTotalRides = createSelector(
  selectAllBookings,
  (bookings) => bookings?.bookings?.length
);

export const selectTotalAcceptedRides = createSelector(
  selectAllBookings,
  (bookings) =>
    bookings?.bookings?.filter((b) => b.status === 'accepted').length
);

export const selectTotalCancelledRides = createSelector(
  selectAllBookings,
  (bookings) =>
    bookings?.bookings?.filter((b) => b.status === 'cancelled').length
);

export const selectTotalPendingRides = createSelector(
  selectAllBookings,
  (bookings) => bookings?.bookings?.filter((b) => b.status === 'pending').length
);

export const selectTotalCompletedRides = createSelector(
  selectAllBookings,
  (bookings) =>
    bookings?.bookings?.filter((b) => b.status === 'completed').length
);
