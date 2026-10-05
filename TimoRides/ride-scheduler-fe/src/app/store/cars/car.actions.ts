import { HttpErrorResponse } from '@angular/common/http';
import { createAction, props } from '@ngrx/store';
import {
  Car,
  CarProximityRequest,
  CarProximityResponse,
  CarRequest,
  ProxyCar,
} from 'src/app/models/car';
import { Profile } from 'src/app/models/user.model';

export const loadCarsByProxy = createAction(
  '[Car List Page] Load Cars By Proxy',
  props<{ proxyInfo: CarProximityRequest }>()
);

export const loadCarsByProxySuccess = createAction(
  '[Car API] Load Cars By Proxy Success',
  props<{ cars: CarProximityResponse }>()
);

export const loadCarsByProxyFailure = createAction(
  '[Car API] Load Cars By Proxy Failure',
  props<{ error: HttpErrorResponse }>()
);

export const selectProxyDriverCar = createAction(
  '[Car] Select Proxy Driver Car',
  props<{ car: ProxyCar }>()
);

export const selectCar = createAction(
  '[Car List Page] Select Car',
  props<{ car: Car }>()
);

export const selectDriver = createAction(
  '[Car List Page] Select Driver',
  props<{ driver: Profile }>()
);

export const loadSelectedDriver = createAction(
  '[Driver] Load Select Driver',
  props<{ id: string }>()
);

export const loadSelectedDriverSuccess = createAction(
  '[Driver] Load Selected Driver Success',
  props<{ profile: Profile }>()
);

export const loadSelectedDriverFailure = createAction(
  '[Driver] Load Selected Driver Failure',
  props<{ error: any }>()
);

export const clearSelection = createAction(
  '[Car Details Page] Reset Selection'
);

export const clearCarState = createAction('[Car] Clear Car State');

export const createCar = createAction(
  '[Car API] Create Car',
  props<{ car: CarRequest }>()
);

export const createCarSuccess = createAction(
  '[Car API] Create Car Success',
  props<{ car: Car }>()
);

export const createCarFailure = createAction(
  '[Car API] Create Car Failure',
  props<{ error: HttpErrorResponse }>()
);

// Update car actions
export const updateCar = createAction(
  '[Car] Update Car',
  props<{ car: Car }>()
);

export const updateCarSuccess = createAction(
  '[Car] Update Car Success',
  props<{ car: Car }>()
);

export const updateCarFailure = createAction(
  '[Car] Update Car Failure',
  props<{ error: HttpErrorResponse }>()
);

export const loadDriverCar = createAction(
  '[Car] Load Driver Car',
  props<{ driverId: string }>()
);

export const loadDriverCarSuccess = createAction(
  '[Car] Load Driver Car Success',
  props<{ car: Car }>()
);

export const loadDriverCarFailure = createAction(
  '[Car] Load Driver Car Failure',
  props<{ error: any }>()
);

export const loadSelectedRebookedCar = createAction(
  '[Car Details Page] Load Selected Rebooked Car',
  props<{ carId: string; bookingId?: string }>()
);

export const loadSelectedRebookedCarSuccess = createAction(
  '[Car Details Page] Load Selected Rebooked Car Success',
  props<{ car: ProxyCar }>()
);

export const loadSelectedRebookedCarFailure = createAction(
  '[Car Details Page] Load Selected Rebooked Car Failure',
  props<{ error: any }>()
);

export const resetCar = createAction('[Car List Page] ResetCar');
