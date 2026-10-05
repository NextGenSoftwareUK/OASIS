import { createSelector } from '@ngrx/store';
import { BookingState } from './ride-schedule.reducer';
import { AppState } from 'src/app/app.state';

export const selectBookingState = (state: AppState) => state.booking;

export const selectRideSchedule = createSelector(
  selectBookingState,
  (state: BookingState) => state.rideSchedule
);

export const selectTripInfo = createSelector(
  selectBookingState,
  (state: BookingState) => state.rideSchedule?.tripInfo
);

export const selectRideState = createSelector(
  selectBookingState,
  (state: BookingState) => state.state
);

export const selectRebooking = createSelector(
  selectBookingState,
  (state: BookingState) => state.rebooking
);
