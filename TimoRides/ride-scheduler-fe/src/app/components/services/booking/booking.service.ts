import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import {
  acceptRideFromDashBoardURL,
  acceptRideFromEmailURL,
  bookedRidesURL,
  bookingURL,
  startEndTripURL,
  updateBookedRideURL,
} from 'src/app/base-url/base-url';
import { LoggedInUser } from 'src/app/models/auth';
import {
  AcceptRideFromDashboardRequest,
  AcceptRideRequest,
  AcceptRideResponse,
  BookedRidesInfo,
  BookedRidesType,
  BookingPayload,
  BookingResponse,
  UpdateBookedRideRequest,
} from 'src/app/models/booking-form';
import { TripStatusRequest, TripStatusResponse } from 'src/app/models/trip';

@Injectable({
  providedIn: 'root',
})
export class BookingService {
  constructor(private httpClient: HttpClient) {}

  bookRide(payload: BookingPayload): Observable<BookingResponse> {
    return this.httpClient.post<BookingResponse>(bookingURL, payload);
  }

  updateBookedRide(payload: UpdateBookedRideRequest): Observable<any> {
    return this.httpClient.post<any>(updateBookedRideURL, payload);
  }

  acceptRide(payload: AcceptRideRequest): Observable<AcceptRideResponse> {
    return this.httpClient.post<AcceptRideResponse>(
      acceptRideFromEmailURL,
      payload
    );
  }

  acceptRideFromDashboard(
    payload: AcceptRideFromDashboardRequest
  ): Observable<BookedRidesInfo> {
    return this.httpClient
      .post<BookedRidesInfo>(acceptRideFromDashBoardURL, payload)
      .pipe(map((res) => res));
  }

  getAllBookedRides(): Observable<BookedRidesType> {
    return this.httpClient.get<BookedRidesType>(bookedRidesURL);
  }

  setTripStatus(payload: TripStatusRequest): Observable<TripStatusResponse> {
    return this.httpClient.post<TripStatusResponse>(startEndTripURL, payload);
  }

  getSessionToken = (): string => {
    const userInfo = sessionStorage.getItem('userInfo');

    if (userInfo) {
      const userData = JSON.parse(userInfo) as LoggedInUser;
      const requestToken = userData.accessToken;

      return requestToken;
    } else {
      return '';
    }
  };
}
