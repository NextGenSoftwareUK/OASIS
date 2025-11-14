import { createAction, props } from '@ngrx/store';
import { BookedRidesInfo, BookedRidesType } from 'src/app/models/booking-form';
import { TripStatusRequest, TripStatusResponse } from 'src/app/models/trip';

export const loadBookings = createAction('[Ride Schedule API] Load Bookings');

export const loadBookingsSuccess = createAction(
  '[Ride Schedule API] Load Bookings Success',
  props<{ bookings: BookedRidesType }>()
);

export const loadBookingsFailure = createAction(
  '[Ride Schedule API] Load Bookings Failure',
  props<{ error: any }>()
);

export const acceptRide = createAction(
  '[Ride Schedule API] Accept Ride',
  props<{ bookingId: string }>()
);

export const acceptRideSuccess = createAction(
  '[Ride Schedule API] Accept Ride Success',
  props<{ booking: BookedRidesInfo }>()
);

export const acceptRideFailure = createAction(
  '[Ride Schedule API] Accept Ride Failure',
  props<{ error: any }>()
);

export const cancelRide = createAction(
  '[Ride Schedule API] Cancel Ride',
  props<{ bookingId: string }>()
);

export const cancelRideSuccess = createAction(
  '[Ride Schedule API] Cancel Ride Success',
  props<{ booking: BookedRidesInfo }>()
);

export const cancelRideFailure = createAction(
  '[Ride Schedule API] Cancel Ride Failure',
  props<{ error: any }>()
);

export const clearBookedRideState = createAction(
  '[Ride Schedule API] Clear Booked Ride State'
);

export const startRide = createAction(
  '[Ride Trip API] Start Ride',
  props<{ tripInfo: TripStatusRequest }>()
);

export const startRideSuccess = createAction(
  '[Ride Trip API] Start Ride Success',
  props<{ rideInfo: TripStatusResponse }>()
);

export const startRideFailure = createAction(
  '[Ride Trip API] Start Ride Failure',
  props<{ error: any }>()
);

export const endRide = createAction(
  '[Ride Trip API] End Ride',
  props<{ tripInfo: TripStatusRequest }>()
);

export const endRideSuccess = createAction(
  '[Ride Trip API] End Ride Success',
  props<{ rideInfo: TripStatusResponse }>()
);

export const endRideFailure = createAction(
  '[Ride Trip API] End Ride Failure',
  props<{ error: any }>()
);
