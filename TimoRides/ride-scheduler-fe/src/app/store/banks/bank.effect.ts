import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { catchError, map, mergeMap } from 'rxjs/operators';
import * as BankActions from './bank.actions';
import { FlutterWaveService } from 'src/app/components/services/flutterwave.service';
import { BankInfo } from './bank.model';

@Injectable()
export class BankEffects {
  loadBanks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(BankActions.loadBanks),
      mergeMap((action) =>
        this.flutterwaveService.getBanks(action.countryCode).pipe(
          map((response) =>
            BankActions.loadBanksSuccess({
              banks: response.data.sort((a: BankInfo, b: BankInfo) =>
                a.name.localeCompare(b.name)
              ),
            })
          ),
          catchError((error) => of(BankActions.loadBanksFailure({ error })))
        )
      )
    )
  );

  constructor(
    private actions$: Actions,
    private flutterwaveService: FlutterWaveService
  ) {}
}
