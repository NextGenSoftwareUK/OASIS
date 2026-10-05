import { Component, OnDestroy } from '@angular/core';
import {
  FormGroup,
  FormControl,
  Validators,
  FormBuilder,
} from '@angular/forms';

import { IconProp } from '@fortawesome/fontawesome-svg-core';
import { faLock, faArrowCircleLeft } from '@fortawesome/free-solid-svg-icons';
import { faEye, faEyeSlash } from '@fortawesome/free-regular-svg-icons';
import { ActivatedRoute, Router } from '@angular/router';
import { UserRegistration } from 'src/app/models/auth';
import { AuthService } from '../services/auth/auth.service';
import { Subscription } from 'rxjs';
import { SnackbarUtilService } from '../shared/snackbar/snackbar-util.service';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-sign-up',
  templateUrl: './sign-up.component.html',
  styleUrls: ['./sign-up.component.css'],
})
export class SignUpComponent implements OnDestroy {
  isRiderSignUpChoice: boolean = false;
  signupChoice: string = 'user';
  registrationForm!: FormGroup;
  showPassword: boolean = false;
  showConfirmPassword: boolean = false;
  faLock: IconProp = faLock;
  faEye: IconProp = faEye;
  faEyeSlash: IconProp = faEyeSlash;
  faArrowCircleLeft: IconProp = faArrowCircleLeft;
  myForm: any;
  subscription: Subscription = new Subscription();
  previousRoute!: string;

  constructor(
    private formBuilder: FormBuilder,
    private router: Router,
    private authService: AuthService,
    private route: ActivatedRoute,
    private _snackBar: SnackbarUtilService
  ) {}

  ngOnInit() {
    this.registrationForm = this.formBuilder.group(
      {
        email: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required, Validators.minLength(8)]],
        confirmPassword: ['', [Validators.required]],
        phone: [
          '',
          [
            Validators.required,
            Validators.minLength(10),
            Validators.maxLength(15),
          ],
        ],
        names: ['', [Validators.required]],
      },
      {
        validator: this.passwordMatchValidator,
      }
    );

    // Access the verification token from the URL
    this.subscription.add(
      this.route.queryParams.subscribe((params) => {
        this.previousRoute = params['url'];
      })
    );
  }

  // Custom validator function
  passwordMatchValidator(group: FormGroup) {
    const passwordControl = group.get('password');
    const confirmPasswordControl = group.get('confirmPassword');

    if (passwordControl!.value === confirmPasswordControl!.value) {
      confirmPasswordControl!.setErrors(null);
    } else {
      confirmPasswordControl!.setErrors({ mismatch: true });
    }
  }

  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPassword() {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  goToLogin() {
    this.router.navigateByUrl(
      '/login' + (this.previousRoute ? '?url=' + this.previousRoute : '')
    );
  }

  setSignInChoice(value: boolean) {
    this.isRiderSignUpChoice = value;
    this.signupChoice = value ? 'driver' : 'user';
  }

  onSubmit() {
    if (this.registrationForm.valid) {
      const signUpPayload: UserRegistration = {
        email: (this.registrationForm.get('email') as FormControl).value,
        password: (this.registrationForm.get('password') as FormControl).value,
        userType: this.signupChoice,
        fullName: (this.registrationForm.get('names') as FormControl).value,
        phone: (this.registrationForm.get('phone') as FormControl).value,
      };

      // create user
      this.subscription.add(
        this.authService.registerUser(signUpPayload).subscribe({
          next: (res) => {
            this._snackBar.displaySnackBar(res.message, '', 'green-snackbar');
            this.registrationForm.reset();
          },
          error: (err: HttpErrorResponse) => {
            if (err.status === 409) {
              if ('error' in err.error) {
                this._snackBar.displaySnackBar(
                  err.error['error'],
                  '',
                  'red-snackbar'
                );
              } else if (typeof err.error === 'object') {
                this._snackBar.displaySnackBar(
                  JSON.stringify(err.error),
                  '',
                  'red-snackbar'
                );
              }
            } else {
              this._snackBar.displaySnackBar(
                'Signup failed: ' + JSON.stringify(err),
                '',
                'red-snackbar'
              );
            }
          },
        })
      );
    } else {
      this.registrationForm.markAllAsTouched();
      this._snackBar.displaySnackBar(
        'Complete the form in order to proceed',
        '',
        'red-snackbar'
      );
    }
  }

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }
}
