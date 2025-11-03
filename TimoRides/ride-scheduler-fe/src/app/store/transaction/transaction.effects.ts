import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { catchError, mergeMap, tap } from 'rxjs/operators';
import * as transactionActions from './transaction.actions';
import { SnackbarUtilService } from 'src/app/components/shared/snackbar/snackbar-util.service';
import { WithdrawalService } from 'src/app/components/services/withdrawal.service';
import { HttpErrorResponse } from '@angular/common/http';
import { topupUserWallet } from '../user/user.actions';

@Injectable()
export class TransactionEffects {
  constructor(
    private actions$: Actions,
    private withdrawalService: WithdrawalService,
    private _snackBar: SnackbarUtilService
  ) {}

  topupWallet$ = createEffect(() =>
    this.actions$.pipe(
      ofType(transactionActions.topupWallet),
      mergeMap((action) =>
        this.withdrawalService.topupWallet(action.data).pipe(
          mergeMap((response) => [
            transactionActions.topupWalletSuccess({ response: action.data }),
            topupUserWallet({ amount: response.wallet }),
          ]),
          tap(() => {
            this._snackBar.displaySnackBar(
              'Wallet topup successful',
              '',
              'green-snackbar'
            );
          }),
          catchError((error: HttpErrorResponse) => {
            this._snackBar.displaySnackBar(
              'Failed to topup wallet',
              '',
              'red-snackbar'
            );
            return of(transactionActions.topupWalletFailure({ error }));
          })
        )
      )
    )
  );
}
