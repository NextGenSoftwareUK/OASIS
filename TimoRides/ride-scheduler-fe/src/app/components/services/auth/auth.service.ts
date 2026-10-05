import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map, switchMap, tap } from 'rxjs/operators';
import { Store } from '@ngrx/store';
import { AppState } from 'src/app/app.state';
import {
  emailVerificationURL,
  loginURL,
  refreshTokenURL,
  signUpURL,
  verifyEmailURL,
} from 'src/app/base-url/base-url';
import {
  EmailVerificationLinkRequest,
  EmailVerificationLinkResponse,
  EmailVerificationRequest,
  EmailVerificationResponse,
  LoggedInUser,
  RefreshTokenLinkRequest,
  RefreshTokenLinkResponse,
  UserLogin,
  UserRegistration,
  UserRegistrationResponse,
} from 'src/app/models/auth';
import { UserProfile } from 'src/app/models/user.model';
import { selectUserRefreshToken } from 'src/app/store/user/user.selectors';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  constructor(private httpClient: HttpClient, private store: Store<AppState>) {}

  registerUser(
    payload: UserRegistration
  ): Observable<UserRegistrationResponse> {
    return this.httpClient.post<UserRegistrationResponse>(signUpURL, payload);
  }

  loginUser(payload: UserLogin): Observable<UserProfile> {
    return this.httpClient.post<UserProfile>(loginURL, payload);
  }

  verifyEmail(
    payload: EmailVerificationRequest
  ): Observable<EmailVerificationResponse> {
    return this.httpClient.post<EmailVerificationResponse>(
      verifyEmailURL,
      payload
    );
  }

  sendVerificationLink(
    payload: EmailVerificationLinkRequest
  ): Observable<EmailVerificationLinkResponse> {
    return this.httpClient.post<EmailVerificationLinkResponse>(
      emailVerificationURL,
      payload
    );
  }

  getToken(name: string): string | null {
    const user = sessionStorage.getItem('userInfo');

    if (!user) return null;

    const userData: LoggedInUser = JSON.parse(user);
    return userData[name as keyof LoggedInUser] as string;
  }

  getAccessToken(): string | null {
    return this.getToken('accessToken');
  }

  setAccessToken(token: string): void {
    sessionStorage.setItem('accessToken', token);
  }

  removeAccessToken(): void {
    sessionStorage.removeItem('accessToken');
  }

  refreshToken(): Observable<string> {
    return this.store.select(selectUserRefreshToken).pipe(
      switchMap((refreshToken) => {
        if (!refreshToken) return of('');

        const refreshTokenPayload: RefreshTokenLinkRequest = {
          refreshToken,
        };

        return this.httpClient
          .post<RefreshTokenLinkResponse>(refreshTokenURL, refreshTokenPayload)
          .pipe(
            tap((response: RefreshTokenLinkResponse) => {
              const newToken = response.accessToken;
              this.setAccessToken(newToken);
            }),
            map((response) => response.accessToken)
          );
      })
    );
  }

  getSessionToken = (): string => {
    const userInfo = sessionStorage.getItem('userInfo');

    if (userInfo) {
      const userData = JSON.parse(userInfo) as LoggedInUser;
      const requestToken = userData.refreshToken;

      return requestToken;
    } else {
      return '';
    }
  };
}
