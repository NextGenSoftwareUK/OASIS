import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap, take } from 'rxjs/operators';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { AppState } from '../app.state';
import { selectAccessToken } from '../store/auth/auth.selectors';
import {
  carsURL,
  emailVerificationURL,
  loginURL,
  signUpURL,
  usersURL,
  verifyEmailURL,
  bookingURL,
  updateBookedRideURL,
  bankListURL,
  ipInfoUrl,
} from '../base-url/base-url';

@Injectable()
export class TokenInterceptor implements HttpInterceptor {
  constructor(private router: Router, private store: Store<AppState>) {}

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    const bypassUrls: RegExp[] = [
      new RegExp(loginURL, 'i'),
      new RegExp(signUpURL, 'i'),
      new RegExp(verifyEmailURL, 'i'),
      new RegExp(emailVerificationURL, 'i'),
      new RegExp(updateBookedRideURL, 'i'),
      new RegExp(bankListURL),
      new RegExp(ipInfoUrl),
    ];

    if (
      this.isBypassed(request.url, bypassUrls) ||
      (request.url.includes(usersURL) &&
        !request.url.endsWith(usersURL) &&
        request.method === 'GET') ||
      (request.url.includes(carsURL) && request.url.includes('proximity')) ||
      (request.url.includes(carsURL) &&
        !request.url.includes('current-car') &&
        request.method !== 'POST') ||
      (request.url.includes(bookingURL) &&
        request.url.endsWith(bookingURL) &&
        request.method === 'POST')
    ) {
      return next.handle(request);
    }

    return this.store.select(selectAccessToken).pipe(
      take(1),
      switchMap((token) => {
        if (token) {
          request = request.clone({
            setHeaders: {
              Authorization: `Bearer ${token}`,
            },
          });
          return next.handle(request);
        } else {
          // Redirect to login if no token is found
          this.router.navigate(['/login'], {
            queryParams: { returnUrl: this.router.url },
          });
          return throwError(() => 'No access token found');
        }
      }),
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          // Token expired or unauthorized, redirect to login
          this.router.navigate(['/login'], {
            queryParams: { returnUrl: this.router.url },
          });
        }
        return throwError(() => error);
      })
    );
  }

  private isBypassed(url: string, bypassUrls: RegExp[]): boolean {
    return bypassUrls.some((bypassUrl) => bypassUrl.test(url));
  }
}
