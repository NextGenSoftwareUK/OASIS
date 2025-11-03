import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { Store } from '@ngrx/store';
import { AppState } from 'src/app/app.state';
import { topupWalletURL, withdrawAmountURL } from 'src/app/base-url/base-url';
import {
  WithdrawAmountRequest,
  WithdrawAmountResponse,
} from 'src/app/store/withdrawal/withdrawal.model';
import {
  TopupRequest,
  TopupResponse,
} from 'src/app/store/transaction/transaction.model';

@Injectable({
  providedIn: 'root',
})
export class WithdrawalService {
  constructor(private http: HttpClient, private store: Store<AppState>) {}

  withdrawAmount(
    payload: WithdrawAmountRequest
  ): Observable<WithdrawAmountResponse> {
    return this.http
      .post<WithdrawAmountResponse>(withdrawAmountURL, payload)
      .pipe(map((data) => data));
  }

  topupWallet(payload: TopupRequest): Observable<TopupResponse> {
    return this.http
      .post<TopupResponse>(topupWalletURL, payload)
      .pipe(map((data) => data));
  }
}
