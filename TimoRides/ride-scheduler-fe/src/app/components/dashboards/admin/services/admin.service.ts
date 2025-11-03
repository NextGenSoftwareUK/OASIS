import {
  adminSettingsURL,
  confirmPaymentURL,
  currentCarURL,
  getWithdrawalsURL,
  usersURL,
} from './../../../../base-url/base-url';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { Profile, UsersProfileResponse } from 'src/app/models/user.model';
import { BookedRidesInfo } from 'src/app/models/booking-form';
import { CarById } from 'src/app/models/car';
import {
  AdminConfig,
  UpdateAdminConfigRequest,
  WithdrawalPaymentModel,
} from '../store/admin.model';

@Injectable({
  providedIn: 'root',
})
export class AdminService {
  constructor(private http: HttpClient) {}

  getUsers(): Observable<Profile[]> {
    return this.http
      .get<UsersProfileResponse>(`${usersURL}`)
      .pipe(map((users: UsersProfileResponse) => users.data));
  }

  getUser(id: string): Observable<Profile> {
    return this.http.get<Profile>(`${usersURL}/${id}`);
  }

  getTrips(): Observable<BookedRidesInfo[]> {
    return this.http.get<BookedRidesInfo[]>(`${usersURL}/trips`);
  }

  getCarByDriverId(id: string): Observable<CarById> {
    return this.http.get<CarById>(`${currentCarURL}/${id}`);
  }

  getAdminConfig(): Observable<AdminConfig> {
    return this.http.get<AdminConfig>(adminSettingsURL);
  }

  updateAdminConfig(
    payload: UpdateAdminConfigRequest
  ): Observable<AdminConfig> {
    return this.http.put<AdminConfig>(adminSettingsURL, payload);
  }

  updateBusinessCommission(
    payload: UpdateAdminConfigRequest
  ): Observable<AdminConfig> {
    return this.http.put<AdminConfig>(adminSettingsURL, payload);
  }

  updateTripAmount(
    tripId: string,
    amount: number
  ): Observable<BookedRidesInfo> {
    return this.http.put<BookedRidesInfo>(
      `${usersURL}/trips/${tripId}/amount`,
      {
        amount,
      }
    );
  }

  getPendingWithdrawals(): Observable<WithdrawalPaymentModel[]> {
    return this.http.get<WithdrawalPaymentModel[]>(getWithdrawalsURL);
  }

  payUser(requestId: string): Observable<WithdrawalPaymentModel> {
    return this.http.put<WithdrawalPaymentModel>(
      `${confirmPaymentURL}/${requestId}`,
      null
    );
  }
}
