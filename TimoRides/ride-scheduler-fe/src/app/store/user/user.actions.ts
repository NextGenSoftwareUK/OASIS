import { createAction, props } from '@ngrx/store';
import { Profile } from '../../models/user.model';

// load User Profile
export const loadUserProfile = createAction('[User] Load User Profile');

export const loadUserProfileSuccess = createAction(
  '[User] Load User Profile Success',
  props<{ profile: Profile }>()
);

export const loadUserProfileFailure = createAction(
  '[User] Load User Profile Failure',
  props<{ error: any }>()
);

// update profile
export const updateUserProfile = createAction(
  '[Profile] Update User Profile',
  props<{ profile: Profile }>()
);

// Upload Profile Image
export const updateProfileImage = createAction(
  '[Profile] Update Profile Image',
  props<{ imgUrl: string }>()
);

// topup user wallet
export const topupUserWallet = createAction(
  '[Dashboard] Topup User Wallet',
  props<{ amount: number }>()
);

// logout action
export const logout = createAction('[Profile] Logout');
