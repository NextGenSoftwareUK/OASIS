// user.effects.ts

import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { catchError, map, mergeMap, switchMap, tap } from 'rxjs/operators';
import * as UserActions from './user.actions';
import { UserService } from '../../components/services/user/user.service';
import { Router } from '@angular/router';
import { Profile } from '../../models/user.model';
import { SnackbarUtilService } from 'src/app/components/shared/snackbar/snackbar-util.service';
import { Store } from '@ngrx/store';
import { selectProfileId } from './user.selectors';

@Injectable()
export class UserEffects {
  constructor(
    private actions$: Actions,
    private userService: UserService,
    private store: Store,
    private _snackBar: SnackbarUtilService
  ) {}

  loadUserProfile$ = createEffect(() =>
    this.actions$.pipe(
      ofType(UserActions.loadUserProfile),
      mergeMap(() =>
        this.store.select(selectProfileId).pipe(
          switchMap((userId) => {
            if (userId) {
              return this.userService.getUserProfile(userId).pipe(
                map((profile: Profile | null) => {
                  if (profile) {
                    return UserActions.loadUserProfileSuccess({ profile });
                  } else {
                    return UserActions.loadUserProfileFailure({
                      error: 'User not found',
                    });
                  }
                }),
                tap(() =>
                  this._snackBar.displaySnackBar(
                    'User profile loaded successfully',
                    '',
                    'green-snackbar'
                  )
                ),
                catchError((error) => {
                  this._snackBar.displaySnackBar(
                    'Failed to load user profile: ' + error.message,
                    '',
                    'red-snackbar'
                  );
                  return of(UserActions.loadUserProfileFailure({ error }));
                })
              );
            } else {
              this._snackBar.displaySnackBar(
                'No user ID found',
                '',
                'red-snackbar'
              );
              return of(
                UserActions.loadUserProfileFailure({
                  error: 'No user ID found',
                })
              );
            }
          })
        )
      )
    )
  );

  // loadUserProfile$ = createEffect(() =>
  //   this.actions$.pipe(
  //     ofType(UserActions.loadUserProfile),
  //     mergeMap(() => {
  //       // const returnUrl = this.router.url;
  //       return this.userService.getUserProfile().pipe(
  //         tap((res: any) => res),
  //         map((profile: Profile) =>
  //           UserActions.loadUserProfileSuccess({ profile })
  //         ),
  //         tap(() =>
  //           this._snackBar.displaySnackBar(
  //             'User profile loaded successfully',
  //             '',
  //             'green-snackbar'
  //           )
  //         ),
  //         catchError((error) => {
  //           // this._snackBar.show('Failed to load user profile');
  //           this._snackBar.displaySnackBar(
  //             'Failed to load user profile' + error.message,
  //             '',
  //             'red-snackbar'
  //           );
  //           return of(UserActions.loadUserProfileFailure({ error }));
  //         })
  //       );
  //     })
  //   )
  // );
}
