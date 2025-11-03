import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { catchError, mergeMap, tap } from 'rxjs/operators';
import * as WithdrawalActions from './withdrawal.actions';
import { SnackbarUtilService } from 'src/app/components/shared/snackbar/snackbar-util.service';
import { WithdrawalService } from 'src/app/components/services/withdrawal.service';
import { HttpErrorResponse } from '@angular/common/http';
import { topupUserWallet } from '../user/user.actions';

@Injectable()
export class WithdrawalEffects {
  constructor(
    private actions$: Actions,
    private withdrawalService: WithdrawalService,
    private _snackBar: SnackbarUtilService
  ) {}

  withdrawFromWallet$ = createEffect(() =>
    this.actions$.pipe(
      ofType(WithdrawalActions.withdrawFromWallet),
      mergeMap((action) => {
        return this.withdrawalService.withdrawAmount(action).pipe(
          mergeMap((response) => [
            WithdrawalActions.withdrawFromWalletSuccess({
              response,
            }),
            topupUserWallet({ amount: response['wallet'] }),
          ]),
          tap(() =>
            this._snackBar.displaySnackBar(
              'Withdrawal is being processed.',
              '',
              'green-snackbar'
            )
          ),
          catchError((error: HttpErrorResponse) => {
            this._snackBar.displaySnackBar(
              'Failed to initiate withdrawal',
              '',
              'red-snackbar'
            );
            return of(WithdrawalActions.withdrawFromWalletFailure({ error }));
          })
        );
      })
    )
  );
}
