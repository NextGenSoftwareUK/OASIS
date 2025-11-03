import { ActionReducer } from '@ngrx/store';
import { AppState } from './app.state';
import { InjectionToken } from '@angular/core';

import * as fromProfile from 'src/app/store/user/user.actions';
import * as fromBookingPage from 'src/app/store/ride-booking/state/actions/ride-schedule-page.action';
import * as fromCar from 'src/app/store/cars/car.actions';
import * as fromBookedRides from 'src/app/store/booked-rides/booked-rides.actions';
import { EncryptionService } from './components/services/encryption/encryption.service';

export const SESSION_STORAGE_SYNC_REDUCER = new InjectionToken<
  ActionReducer<AppState>
>('SessionStorageSyncReducer');

export function sessionStorageSyncReducerFactory(
  encryptionService: EncryptionService
) {
  return (reducer: ActionReducer<AppState>): ActionReducer<AppState> => {
    return (state, action) => {
      if (
        action.type === fromProfile.logout.type ||
        action.type === fromBookingPage.clearBookingState.type ||
        action.type === fromCar.clearCarState.type ||
        action.type === fromBookedRides.clearBookedRideState.type
      ) {
        sessionStorage.removeItem('tmr');
        state = undefined;
      }

      const nextState = reducer(state, action);
      if (nextState) {
        const encryptedState = encryptionService.encryptData(nextState);
        sessionStorage.setItem('tmr', encryptedState);
      }
      return nextState;
    };
  };
}

export function rehydrateState(
  encryptionService: EncryptionService
): AppState | undefined {
  const encryptedState = sessionStorage.getItem('tmr');
  if (encryptedState) {
    const decryptedState = encryptionService.decryptData(encryptedState);
    return decryptedState;
  }
  return undefined;
}
