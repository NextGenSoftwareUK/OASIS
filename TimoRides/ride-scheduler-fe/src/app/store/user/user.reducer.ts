import { createReducer, on } from '@ngrx/store';
import * as UserActions from './user.actions';
import { Profile } from 'src/app/models/user.model';

export interface ProfileState {
  profile: Profile | null;
  error: any;
}

export const initialState: ProfileState = {
  profile: null,
  error: null,
};

export const userReducer = createReducer(
  initialState,
  on(UserActions.updateUserProfile, (state, { profile }) => ({
    ...state,
    profile: {
      ...state.profile,
      ...profile,
    },
  })),
  on(UserActions.loadUserProfileSuccess, (state, { profile }) => ({
    ...state,
    profile: {
      ...state.profile,
      ...profile,
    },
  })),
  on(UserActions.loadUserProfileFailure, (state, { error }) => ({
    ...state,
    profile: {
      ...state.profile,
      ...error,
    },
  })),
  on(UserActions.updateProfileImage, (state, { imgUrl }) => ({
    ...state,
    profile: state.profile
      ? {
          ...state.profile,
          profileImg: imgUrl,
        }
      : null,
  })),
  on(UserActions.topupUserWallet, (state, { amount }) => ({
    ...state,
    profile: state.profile
      ? {
          ...state.profile,
          wallet: amount,
        }
      : null,
  })),
  on(UserActions.logout, (state) => ({ ...state, profile: null }))
);
