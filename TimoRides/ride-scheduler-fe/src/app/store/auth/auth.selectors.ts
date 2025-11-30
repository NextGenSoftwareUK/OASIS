import { createSelector } from '@ngrx/store';
import { AppState } from 'src/app/app.state';

export const selectAuthState = (state: AppState) => state.profile.profile;

export const selectAccessToken = createSelector(
  selectAuthState,
  (state) => state?.accessToken
);

export const selectUserRole = createSelector(
  selectAuthState,
  (state) => state?.role
);
