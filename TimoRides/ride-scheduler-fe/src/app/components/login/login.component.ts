import { UserLogin } from '../../models/auth';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../services/auth/auth.service';
import { Subscription } from 'rxjs';
import { Store } from '@ngrx/store';
import { UserProfile } from 'src/app/models/user.model';
import { AppState } from 'src/app/app.state';
import { SnackbarUtilService } from '../shared/snackbar/snackbar-util.service';
import { updateUserProfile } from 'src/app/store/user/user.actions';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
})
export class LoginComponent implements OnInit, OnDestroy {
  loginForm!: FormGroup;
  subscription: Subscription = new Subscription();
  previousRoute!: string;
  returnUrl: string;

  constructor(
    private formBuilder: FormBuilder,
    private router: Router,
    private authService: AuthService,
    private route: ActivatedRoute,
    private store: Store<AppState>,
    private _snackBar: SnackbarUtilService
  ) {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
  }

  ngOnInit() {
    this.loginForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
    });
  }

  login() {
    if (this.loginForm.valid) {
      const userData: UserLogin = this.loginForm.value;

      this.subscription.add(
        this.authService.loginUser(userData).subscribe({
          next: (res: UserProfile) => {
            if ('error' in res) {
            } else if (res.accessToken) {
              this._snackBar.displaySnackBar(
                'Login successful',
                'OK',
                'green-snackbar'
              );

              // Dispatch the action to update the user profile in the state
              this.store.dispatch(updateUserProfile({ profile: res }));

              // route to previous page, else go to profile page
              if (this.returnUrl === '/') {
                // if user is a customer, route to user profile
                const userRole = res.role;

                switch (userRole) {
                  case 'user':
                    this.router.navigate(['/ride-scheduler']);
                    break;
                  case 'driver':
                    this.router.navigate(['/dashboards/driver']);
                    break;
                  case 'admin':
                    this.router.navigate(['/dashboards/admin']);
                    break;
                  default:
                    break;
                }

                // else, route to driver if user = driver
              } else {
                this.router.navigateByUrl(this.returnUrl);
              }
            }
          },
          error: (err) => {
            if ('error' in err && 'error' in err['error']) {
              this._snackBar.displaySnackBar(
                err.error.error,
                '',
                'red-snackbar'
              );
            } else if ('message' in err) {
              this._snackBar.displaySnackBar(err.message, '', 'red-snackbar');
            }
          },
        })
      );
    }
  }

  goToSignup() {
    this.router.navigateByUrl(
      '/signup' + (this.previousRoute ? '?url=' + this.previousRoute : '')
    );
  }

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }
}
