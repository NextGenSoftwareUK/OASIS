import { createSelector } from '@ngrx/store';
import { AppState } from 'src/app/app.state';
import { AdminState } from './admin.reducer';

export const selectAdminState = (state: AppState) => state.admin;

export const selectAllUsers = createSelector(
  selectAdminState,
  (state: AdminState) => state.users
);

export const selectUserById = (id: string) =>
  createSelector(selectAdminState, (state: AdminState) =>
    state.users.find((user) => user.id === id)
  );

export const selectAllTrips = createSelector(
  selectAdminState,
  (state: AdminState) => state.trips
);

export const selectCarByDriverId = (id: string) =>
  createSelector(selectAdminState, (state: AdminState) => state.selectedCar);

export const selectTotalCustomers = createSelector(
  selectAllUsers,
  (users) => users.filter((user) => user).length
);

export const selectTotalDrivers = createSelector(
  selectAllUsers,
  (users) => users.filter((user) => user.role === 'driver').length
);

export const selectTotalAdmins = createSelector(
  selectAllUsers,
  (users) => users.filter((user) => user.role === 'admin').length
);

export const selectTotalUsers = createSelector(
  selectAllUsers,
  (users) => users.filter((user) => user.role === 'user').length
);

export const selectConfigs = createSelector(
  selectAdminState,
  (state) => state.config
);

export const selectWithdrawals = createSelector(selectAdminState, (state) =>
  state.withdrawals.map((withdrawal) => {
    return {
      ...withdrawal,
      userId:
        state.users.find((user) => user.id === withdrawal.userId)?.fullName ||
        withdrawal.userId,
    };
  })
);
