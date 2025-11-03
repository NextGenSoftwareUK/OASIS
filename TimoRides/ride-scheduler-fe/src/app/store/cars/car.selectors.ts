import { createSelector } from '@ngrx/store';
import { AppState } from 'src/app/app.state';
import { CarState } from './car.reducer';
import { CarInfo } from 'src/app/models/car';
import {
  selectRebooking,
  selectRideSchedule,
} from '../ride-booking/state/ride-schedule.selectors';

export const selectCarState = (state: AppState) => state.car;

export const selectSelectedCar = createSelector(
  selectCarState,
  (state: CarState) => state.selectedCar
);

export const selectCreatedCar = createSelector(
  selectCarState,
  (state: CarState) => state.createdCar
);

export const selectSelectedProxyCar = createSelector(
  selectCarState,
  (state: CarState) => state.selectedProxyCar
);

export const selectSelectedDriver = createSelector(
  selectCarState,
  (state) => state.selectedDriver
);

export const selectSelectedDriverProfile = createSelector(
  selectCarState,
  (state: CarState) => state.selectedDriver
);

export const selectDriverProfile = createSelector(
  selectCarState,
  (state: CarState) => state.selectedDriver
);

export const selectCarInfo = createSelector(
  selectSelectedProxyCar,
  selectDriverProfile,
  selectRideSchedule,
  selectRebooking,
  (car, driver, rideSchedule, rebooking): CarInfo | null => {
    if (car && driver && rideSchedule) {
      return { car, driver, rideSchedule };
    } else if (car && driver && rebooking) {
      return { car, driver, rebooking };
    } else {
      return null;
    }
  }
);

export const selectDriverCar = createSelector(
  selectCarState,
  (state) => state.selectedDriverCar
);
