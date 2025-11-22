import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpErrorResponse,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { AuthService } from '../components/services/auth/auth.service';
import {
  bankListURL,
  bookingURL,
  carsURL,
  emailVerificationURL,
  ipInfoUrl,
  loginURL,
  signUpURL,
  updateBookedRideURL,
  usersURL,
  verifyEmailURL,
} from '../base-url/base-url';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(private authService: AuthService) {}
  private bypassUrls: RegExp[] = [
    new RegExp(loginURL, 'i'),
    new RegExp(signUpURL, 'i'),
    new RegExp(verifyEmailURL, 'i'),
    new RegExp(emailVerificationURL, 'i'),
    new RegExp(updateBookedRideURL, 'i'),
    new RegExp(bankListURL),
    new RegExp(ipInfoUrl),
  ];

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    if (
      this.isBypassed(request.url, this.bypassUrls) ||
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

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          return this.handle401Error(request, next);
        }
        return throwError(() => new Error(JSON.stringify(error)));
      })
    );
  }

  private handle401Error(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    return this.authService.refreshToken().pipe(
      switchMap((newToken: string) => {
        request = request.clone({
          setHeaders: {
            Authorization: `Bearer ${newToken}`,
          },
        });
        return next.handle(request);
      }),
      catchError((error: any) => {
        return throwError(() => new Error(JSON.stringify(error)));
      })
    );
  }

  private isBypassed(url: string, bypassUrls: RegExp[]): boolean {
    return bypassUrls.some((bypassUrl) => bypassUrl.test(url));
  }
}
