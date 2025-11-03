import { createReducer, on } from '@ngrx/store';
import * as AdminActions from './admin.actions';
import {
  AdminConfig,
  UserWithCars,
  WithdrawalPaymentModel,
} from './admin.model';
import { BookedRidesInfo } from 'src/app/models/booking-form';
import { CarById } from 'src/app/models/car';

export interface AdminState {
  users: UserWithCars[];
  selectedUser: UserWithCars | null;
  trips: BookedRidesInfo[];
  selectedCar: CarById | null;
  config: AdminConfig | null;
  withdrawals: WithdrawalPaymentModel[];
}

export const initialState: AdminState = {
  users: [],
  selectedUser: null,
  trips: [],
  selectedCar: null,
  config: null,
  withdrawals: [],
};

export const adminReducer = createReducer(
  initialState,
  on(AdminActions.loadUsers, (state) => ({
    ...state,
    users: [],
  })),
  on(AdminActions.loadUsersSuccess, (state, { users }) => ({
    ...state,
    users,
  })),
  on(AdminActions.loadUserSuccess, (state, { user }) => ({
    ...state,
    selectedUser: user,
  })),
  on(AdminActions.loadTripsSuccess, (state, { trips }) => ({
    ...state,
    trips,
  })),
  on(AdminActions.updateTripAmountSuccess, (state, { trip }) => ({
    ...state,
    trips: state.trips.map((t) => (t.id === trip.id ? trip : t)),
  })),
  on(AdminActions.loadConfigSuccess, (state, { config }) => ({
    ...state,
    config,
  })),
  on(AdminActions.loadConfigFailed, (state, { error }) => ({
    ...state,
    error,
  })),
  on(AdminActions.updateConfigSuccess, (state, { config }) => ({
    ...state,
    config: {
      ...state.config,
      ...config,
    },
  })),
  on(AdminActions.updateBusinessCommissionSuccess, (state, { config }) => ({
    ...state,
    config: {
      ...state.config,
      ...config,
    },
  })),
  on(AdminActions.verifyCarSuccess, (state, { car }) => ({
    ...state,
    users: state.users.map((user) =>
      user.cars && user.cars.some((c) => c.id === car.id)
        ? {
            ...user,
            cars: user.cars.map((c) => (c.id === car.id ? car : c)),
          }
        : user
    ),
    error: null,
  })),
  on(AdminActions.verifyCarFailure, (state, { error }) => ({
    ...state,
    error,
  })),
  on(AdminActions.loadWithdrawalsSuccess, (state, { withdrawals }) => ({
    ...state,
    withdrawals,
  })),
  on(AdminActions.payUserSuccess, (state, { withdrawal }) => ({
    ...state,
    withdrawals: state.withdrawals.map((w) =>
      w.id === withdrawal.id ? withdrawal : w
    ),
  }))
);
