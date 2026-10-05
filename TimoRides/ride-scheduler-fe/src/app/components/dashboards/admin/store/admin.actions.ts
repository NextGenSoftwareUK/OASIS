import { createAction, props } from '@ngrx/store';
import { BookedRidesInfo } from 'src/app/models/booking-form';
import { Car, CarById, VerifyCar } from 'src/app/models/car';
import { Profile } from 'src/app/models/user.model';
import {
  AdminConfig,
  UserWithCars,
  WithdrawalPaymentModel,
} from './admin.model';

export const loadUsers = createAction('[Admin] Load Users');

export const loadUsersSuccess = createAction(
  '[Admin] Load Users Success',
  props<{ users: UserWithCars[] }>()
);

export const loadUser = createAction(
  '[Admin] Load User',
  props<{ id: string }>()
);

export const loadUserSuccess = createAction(
  '[Admin] Load User Success',
  props<{ user: UserWithCars }>()
);

export const verifyDriver = createAction(
  '[Admin] Verify Driver',
  props<{ driver: Profile }>()
);

export const invalidateDriver = createAction(
  '[Admin] Invalidate Driver',
  props<{ driver: Profile }>()
);

export const loadTrips = createAction('[Admin] Load Trips');

export const loadTripsSuccess = createAction(
  '[Admin] Load Trips Success',
  props<{ trips: BookedRidesInfo[] }>()
);

export const updateTripAmount = createAction(
  '[Admin] Update Trip Amount',
  props<{ tripId: string; amount: number }>()
);

export const updateTripAmountSuccess = createAction(
  '[Admin] Update Trip Amount Success',
  props<{ trip: BookedRidesInfo }>()
);

export const loadCarByDriverId = createAction(
  '[Admin] Load Car By Driver Id',
  props<{ id: string }>()
);

export const loadCarByDriverIdSuccess = createAction(
  '[Admin] Load Car By Driver Id Success',
  props<{ car: CarById }>()
);

export const loadCarByDriverIdFailed = createAction(
  '[Admin] Load Car By Driver Id Failed',
  props<{ error: any }>()
);

export const loadConfig = createAction('[Admin] Load Config');

export const loadConfigSuccess = createAction(
  '[Admin] Load Config Success',
  props<{ config: AdminConfig }>()
);

export const loadConfigFailed = createAction(
  '[Admin] Load Config Failed',
  props<{ error: any }>()
);

export const updateConfig = createAction(
  '[Admin] Update Config',
  props<{ config: Partial<AdminConfig> }>()
);

export const updateConfigSuccess = createAction(
  '[Admin] Update Config Success',
  props<{ config: AdminConfig }>()
);

export const updateConfigFailed = createAction(
  '[Admin] Update Config Failed',
  props<{ error: any }>()
);

export const updateBusinessCommission = createAction(
  '[Admin] Update Business Commission',
  props<{ config: Partial<AdminConfig> }>()
);

export const updateBusinessCommissionSuccess = createAction(
  '[Admin] Update Business Commission Success',
  props<{ config: AdminConfig }>()
);

export const updateBusinessCommissionFailed = createAction(
  '[Admin] Update Business Commission Failed',
  props<{ error: any }>()
);

export const verifyCar = createAction(
  '[Admin] Update Car',
  props<{ driverId: string; isVerify: boolean; isActive: boolean }>()
);

export const verifyCarSuccess = createAction(
  '[Admin] Update Car Success',
  props<{ car: Car }>()
);

export const verifyCarFailure = createAction(
  '[Admin] Update Car Failure',
  props<{ error: any }>()
);

export const activateCar = createAction(
  '[Car] Update Car',
  props<{ driverId: string; verifyInfo: VerifyCar }>()
);

export const activateCarSuccess = createAction(
  '[Car] Update Car Success',
  props<{ car: Car }>()
);

export const activateCarFailure = createAction(
  '[Car] Update Car Failure',
  props<{ error: any }>()
);

export const loadWithdrawals = createAction('[Withdrawal] Load Withdrawals');

export const loadWithdrawalsSuccess = createAction(
  '[Withdrawal] Load Withdrawals Success',
  props<{ withdrawals: WithdrawalPaymentModel[] }>()
);

export const loadWithdrawalsFailure = createAction(
  '[Withdrawal] Load Withdrawals Failure',
  props<{ error: any }>()
);

export const payUser = createAction(
  '[Payment] Pay User',
  props<{ requestId: string }>()
);

export const payUserSuccess = createAction(
  '[Payment] Pay User Success',
  props<{ withdrawal: WithdrawalPaymentModel }>()
);

export const payUserFailure = createAction(
  '[Payment] Pay User Failure',
  props<{ error: any }>()
);
