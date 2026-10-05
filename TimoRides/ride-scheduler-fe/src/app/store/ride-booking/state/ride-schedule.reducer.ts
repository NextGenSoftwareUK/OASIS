import { createReducer, on } from '@ngrx/store';
import {
  BookingRequest,
  MapLocation,
  SaveRebooking,
} from 'src/app/models/booking-form';
import * as RideSchedulePageActions from './actions/ride-schedule-page.action';
import * as RideScheduleAPIActions from './actions/ride-schedule-api.action';
import { RidePriceResponse } from 'src/app/models/pricing';

export interface BookingState {
  rideSchedule: BookingRequest | null;
  rebooking: SaveRebooking | null;
  state: string;
}

export const initialState: BookingState = {
  rideSchedule: null,
  rebooking: null,
  state: '',
};

export const RideScheduleReducer = createReducer(
  initialState,
  on(RideSchedulePageActions.enter, (state) => ({
    ...state,
  })),
  on(RideSchedulePageActions.enterPickupLocation, (state, { pickUp }) => {
    if (!state.rideSchedule) {
      // Handle the case where rideSchedule is null
      const newRideSchedule: BookingRequest = {
        sourceLocation: { ...pickUp },
        car: '',
        fullName: '',
        phoneNumber: '',
        bookingType: '',
        destinationLocation: {} as MapLocation,
        email: '',
        tripInfo: {} as RidePriceResponse,
        isCash: false,
        departureTime: '',
        passengers: '',
      };
      return {
        ...state,
        rideSchedule: newRideSchedule,
      };
    } else {
      // When rideSchedule is not null
      const newSourceLocation = {
        ...state.rideSchedule.sourceLocation,
        ...pickUp,
      };
      return {
        ...state,
        rideSchedule: {
          ...state.rideSchedule,
          sourceLocation: newSourceLocation,
        },
      };
    }
  }),
  on(RideSchedulePageActions.enterDropOffLocation, (state, { dropOff }) => {
    if (!state.rideSchedule) {
      // Handle the case where rideSchedule is null
      const newRideSchedule: BookingRequest = {
        sourceLocation: {} as MapLocation,
        car: '',
        fullName: '',
        phoneNumber: '',
        bookingType: '',
        destinationLocation: { ...dropOff },
        email: '',
        tripInfo: {} as RidePriceResponse,
        isCash: false,
        departureTime: '',
        passengers: '',
      };
      return {
        ...state,
        rideSchedule: newRideSchedule,
      };
    } else {
      // When rideSchedule is not null
      const newDestinationLocation = {
        ...state.rideSchedule.destinationLocation,
        ...dropOff,
      };
      return {
        ...state,
        rideSchedule: {
          ...state.rideSchedule,
          destinationLocation: newDestinationLocation,
        },
      };
    }
  }),
  on(RideSchedulePageActions.submitForm, (state, { formData }) => {
    return {
      ...state,
      rideSchedule: {
        ...state.rideSchedule,
        fullName: formData.fullName,
        phoneNumber: formData.phoneNumber,
        bookingType: formData.bookingType,
        passengers: formData.passengers,
        email: formData.email,
        departureTime: formData.departureTime,
        car: state.rideSchedule?.car || '',
        tripInfo: state.rideSchedule?.tripInfo || ({} as RidePriceResponse),
        isCash: state.rideSchedule?.isCash || false,
        sourceLocation:
          state.rideSchedule?.sourceLocation || ({} as MapLocation),
        destinationLocation:
          state.rideSchedule?.destinationLocation || ({} as MapLocation),
      },
    };
  }),
  on(RideScheduleAPIActions.fetchTripCostSuccess, (state, { tripInfo }) => {
    if (!state.rideSchedule) {
      return state;
    }
    return {
      ...state,
      rideSchedule: {
        ...state.rideSchedule,
        tripInfo,
      },
    };
  }),
  on(RideSchedulePageActions.choosePaymentOption, (state, { isCash }) => {
    if (!state.rideSchedule) {
      return state;
    }
    return {
      ...state,
      rideSchedule: {
        ...state.rideSchedule,
        isCash,
      },
    };
  }),
  on(RideSchedulePageActions.clearBookingState, (state) => ({
    ...state,
    rideSchedule: null,
  })),
  on(RideSchedulePageActions.saveState, (state, { countryState }) => ({
    ...state,
    state: countryState,
  })),
  on(
    RideSchedulePageActions.updateRideSchedule,
    (state, { trxId, trxRef, isCash }) => {
      if (!state.rideSchedule) {
        return state;
      }
      return {
        ...state,
        rideSchedule: {
          ...state.rideSchedule,
          trxId,
          trxRef,
          isCash,
        },
      };
    }
  ),
  on(
    RideSchedulePageActions.updateBiodata,
    (state, { fullName, email, phoneNumber }) => {
      if (!state.rideSchedule) {
        return state;
      }
      return {
        ...state,
        rideSchedule: {
          ...state.rideSchedule,
          fullName,
          email,
          phoneNumber,
        },
      };
    }
  ),
  on(RideSchedulePageActions.saveRebooking, (state, { rebooking }) => ({
    ...initialState,
    rebooking,
  })),
  on(RideSchedulePageActions.resetBooking, (state) => initialState)
);
