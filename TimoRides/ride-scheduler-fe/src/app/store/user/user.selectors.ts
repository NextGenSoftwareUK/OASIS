import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AppState } from 'src/app/app.state';
import { DriverProfile } from 'src/app/models/user.model';
import { ProfileState } from './user.reducer';

export const selectUserState = createFeatureSelector<ProfileState>('profile');

export const selectUserProfile = createSelector(
  selectUserState,
  (state: ProfileState) => state.profile
);

export const selectProfileId = createSelector(
  selectUserState,
  (state: ProfileState) => state.profile?.id
);

export const selectDriverProfile = createSelector(
  selectUserState,
  (state: ProfileState) => state.profile as DriverProfile
);

export const selectUserRefreshToken = createSelector(
  selectUserState,
  (state: ProfileState) => state.profile?.refreshToken
);

export const selectUserError = createSelector(
  selectUserState,
  (state: ProfileState) => state.error
);

export const selectProfileState = (state: AppState) => state.profile;

export const getProfileState = createSelector(
  selectProfileState,
  (profileState: ProfileState) => profileState
);
