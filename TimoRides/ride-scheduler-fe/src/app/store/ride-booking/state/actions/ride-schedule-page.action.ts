import { createAction, props } from '@ngrx/store';
import {
  BookingPayload,
  BookingRequest,
  MapLocation,
  SaveRebooking,
  UpdateBookedRideRequest,
} from 'src/app/models/booking-form';

export const enter = createAction('[Booking Page] Enter');

export const enterPickupLocation = createAction(
  '[Booking Page] Enter Pickup Location',
  props<{ pickUp: MapLocation }>()
);

export const enterDropOffLocation = createAction(
  '[Booking Page] Enter DropOff Location',
  props<{ dropOff: MapLocation }>()
);

export const submitForm = createAction(
  '[Booking Page] Submit Form',
  props<{ formData: BookingRequest }>()
);

export const choosePaymentOption = createAction(
  '[Payment Page] Choose Payment Option',
  props<{ isCash: boolean }>()
);

export const saveScheduledRide = createAction(
  '[Payment Page] Save Scheduled Ride',
  props<{ scheduledRide: BookingPayload }>()
);

export const saveScheduledRideSuccess = createAction(
  '[Payment Page] Save Scheduled Ride Success',
  props<{ response: string }>()
);

export const saveScheduledRideFailure = createAction(
  '[Payment Page] Save Scheduled Ride Failure',
  props<{ error: any }>()
);

export const saveState = createAction(
  '[Booking Page] Save State',
  props<{ countryState: string }>()
);

export const updateRideSchedule = createAction(
  '[Payment Page] Update Ride Schedule',
  props<{ trxId: string | null; trxRef: string | null; isCash: boolean }>()
);

export const updateBiodata = createAction(
  '[User Ride Form] Update Biodata',
  props<{
    fullName: string;
    email: string;
    phoneNumber: string;
  }>()
);

export const clearBookingState = createAction('[Booking] Clear Booking State');

export const submitRebooking = createAction(
  '[Car List Page] Submit Rebooking',
  props<{ rebookingInfo: UpdateBookedRideRequest }>()
);

export const saveRebooking = createAction(
  '[Car List Page] Save Rebooking',
  props<{
    rebooking: SaveRebooking;
  }>()
);

export const saveRebookingSuccess = createAction(
  '[Car List Page] Save Rebooking Success'
);

export const saveRebookingFailure = createAction(
  '[Car List Page] Save Rebooking Failure'
);

export const resetBooking = createAction('[Car List Page] Reset Booking');
