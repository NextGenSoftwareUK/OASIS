import { Component, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from '../services/auth/auth.service';
import {
  EmailVerificationLinkRequest,
  EmailVerificationRequest,
} from 'src/app/models/auth';
import { BookingService } from '../services/booking/booking.service';
import { AcceptRideRequest } from 'src/app/models/booking-form';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { SnackbarUtilService } from '../shared/snackbar/snackbar-util.service';

@Component({
  selector: 'app-email-verification',
  templateUrl: './email-verification.component.html',
  styleUrls: ['./email-verification.component.css'],
})
export class EmailVerificationComponent implements OnDestroy {
  subscription: Subscription = new Subscription();
  verificationToken: string = '';
  verificationType: string = '';
  isVerified: boolean | undefined = undefined;
  isAccepted: boolean | undefined = undefined;
  errorMessage = '';
  isVerifyEmailSent: boolean | undefined = undefined;
  emailVerificationForm: FormGroup = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]],
  });
  errorMessages: string[] = [];

  constructor(
    private route: ActivatedRoute,
    private authService: AuthService,
    private bookingService: BookingService,
    private router: Router,
    private formBuilder: FormBuilder,
    private _snackBar: SnackbarUtilService
  ) {
    // Access the verification token from the URL
    this.subscription.add(
      this.route.queryParams.subscribe((params) => {
        this.verificationToken = params['token'];
        const param = Object.keys(params)[0];
        switch (param) {
          case 'token':
            this.verificationType = param;
            this.verificationToken = params['token'];
            this.verifyEmail();
            break;
          case 'rtoken':
            this.verificationType = param;
            this.verificationToken = params['rtoken'];
            this.acceptRide();
            break;
        }
      })
    );
  }

  verifyEmail() {
    const verifyEmailPayload: EmailVerificationRequest = {
      token: this.verificationToken,
    };
    this.subscription.add(
      this.authService.verifyEmail(verifyEmailPayload).subscribe({
        next: (res) => {
          this.isVerified = res['message'].toLowerCase().includes('success');

          this._snackBar.displaySnackBar(
            'Email verified successfully',
            'OK',
            'green-snackbar'
          );
        },
        error: (err: HttpErrorResponse) => {
          if ('error' in err) {
            if ('message' in err['error']) {
              this._snackBar.displaySnackBar(
                err['error']['message'],
                '',
                'red-snackbar'
              );
              this.isVerified = false;
              this.errorMessage = err['error']['message'];
              this.errorMessage = err['error']['message'];
              this.errorMessages.push(err['error']['message']);
            }
          }
        },
      })
    );
  }

  acceptRide() {
    const acceptRidePayload: AcceptRideRequest = {
      token: this.verificationToken,
      isAccepted: true,
      trxId: null,
    };
    this.subscription.add(
      this.bookingService.acceptRide(acceptRidePayload).subscribe({
        next: () => {
          this.isAccepted = true;

          this._snackBar.displaySnackBar(
            'Ride accepted successfully',
            '',
            'green-snackbar'
          );
        },
        error: (err) => {
          this.isVerified = false;

          this.errorMessage = JSON.parse(err.message).error.message;

          this._snackBar.displaySnackBar(
            JSON.parse(err.message).error.message,
            '',
            'red-snackbar'
          );
        },
      })
    );
  }

  goToHome() {
    this.router.navigateByUrl('/login');
  }

  resendVerificationLink() {
    if (this.emailVerificationForm.valid) {
      // logic to resend email verification
      const emailVerificationPayload: EmailVerificationLinkRequest = {
        email: this.emailVerificationForm.get('email')?.value,
      };
      this.subscription.add(
        this.authService
          .sendVerificationLink(emailVerificationPayload)
          .subscribe({
            next: (res) => {
              this.isVerifyEmailSent = res['message']
                .toLowerCase()
                .includes('confirmation email sent');

              this._snackBar.displaySnackBar(
                'Confirmation email sent successfully',
                '',
                'green-snackbar'
              );
              this.errorMessage = '';
              this.errorMessages.length = 0;
            },
            error: (err: HttpErrorResponse) => {
              this._snackBar.displaySnackBar(
                err['error']['error'],
                '',
                'red-snackbar'
              );
              this.errorMessage = err['error']['error'];
              this.errorMessages.push(err['error']['error']);
              this.isVerified = false;

              this.emailVerificationForm.reset();
            },
            complete: () => {
              this.emailVerificationForm.reset();
            },
          })
      );
    }
  }

  navigateToPage(path: string) {
    this.router.navigateByUrl('/' + path);
  }

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }
}
