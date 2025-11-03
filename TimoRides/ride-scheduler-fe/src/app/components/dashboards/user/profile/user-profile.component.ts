/**
 * WARNING: The import below fixes this error in the terminal:
 * error TS2304: Cannot find name 'google'.
 */

// import { google } from 'google-maps';

/**
 * WARNING: The import above fixes this error in the terminal:
 * error TS2304: Cannot find name 'google'.
 */
import { AfterViewInit, Component, OnDestroy, OnInit } from '@angular/core';
import {
  FormGroup,
  FormBuilder,
  Validators,
  FormControl,
  AbstractControl,
} from '@angular/forms';
import { Store } from '@ngrx/store';
import { Observable, Subscription } from 'rxjs';
import { AppState } from 'src/app/app.state';
import { FileUploaderService } from 'src/app/components/services/file-uploader/file-uploader.service';
import { UserService } from 'src/app/components/services/user/user.service';
import { FileUploadRequest } from 'src/app/models/file-uploader';
import { UserProfile } from 'src/app/models/user.model';
import { updateUserProfile } from 'src/app/store/user/user.actions';
import {
  selectUserError,
  selectUserProfile,
} from 'src/app/store/user/user.selectors';
import { imageToBase64 } from 'src/app/utils/base64-converter';
import {
  ProfileImageUploadProgress,
  profileImageUploadProgress,
} from 'src/app/constants/ride-schedule.constants';
import { SnackbarUtilService } from 'src/app/components/shared/snackbar/snackbar-util.service';
import { environment } from 'src/environments/environment';

type ActiveProfileSection =
  | 'biodata'
  | 'address'
  | 'nextOfKin'
  | 'passwordUpdate';

@Component({
  selector: 'app-user-profile',
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.css'],
})
export class UserProfileComponent implements OnInit, AfterViewInit, OnDestroy {
  subscription: Subscription = new Subscription();
  activeSection: ActiveProfileSection = 'biodata';
  columnTitle = 'Biodata';
  uploadedFilePath: string = '';
  biodataForm!: FormGroup;
  addressForm!: FormGroup;
  passwordForm!: FormGroup;
  nextOfKinForm!: FormGroup;
  profile$: Observable<UserProfile | null>;
  error$: Observable<any>;
  profileImageUploadProgress: ProfileImageUploadProgress =
    profileImageUploadProgress;

  private googleMapsScriptLoaded = false;
  private googleMapsScriptElement: HTMLScriptElement | null = null;

  constructor(
    private fb: FormBuilder,
    private store: Store<AppState>,
    private fileUploaderService: FileUploaderService,
    private userService: UserService,
    private _snackBar: SnackbarUtilService
  ) {
    this.profile$ = this.store.select(selectUserProfile);
    this.error$ = this.store.select(selectUserError);
  }

  ngOnInit(): void {
    this.loadGoogleMapsScript();

    // Initialize form groups
    this.biodataForm = this.fb.group({
      fullName: ['', Validators.required],
      profileImg: [''],
      email: ['', [Validators.required, Validators.email]],
      homeAddress: ['', [Validators.required]],
      phone: ['', Validators.required],
      altPhone: [''],
    });

    this.passwordForm = this.fb.group(
      {
        oldPassword: ['', Validators.required],
        newPassword: ['', Validators.required],
        newPasswordConfirm: ['', Validators.required],
      },
      { validators: this.getPasswordMatch }
    );

    this.nextOfKinForm = this.fb.group({
      nextOfKinFullName: ['', Validators.required],
      nextOfKinPhoneCell: ['', Validators.required],
      nextOfKinPhoneHome: [''],
      nextOfKinEmailAddress: ['', Validators.required],
      nextOfKinHomeAddress: ['', Validators.required],
      nextOfKinRelationship: ['', Validators.required],
    });

    // Set default active section
    this.activeSection = 'biodata';
    this.columnTitle = 'Biodata';

    this.subscription.add(
      this.profile$.subscribe((profile) => {
        if (profile) {
          this.biodataForm.patchValue({
            fullName: profile.fullName,
            email: profile.email,
            homeAddress: profile.homeAddress,
            phone: profile.phone,
          });

          this.uploadedFilePath = profile.profileImg;

          this.nextOfKinForm.patchValue({
            nextOfKinFullName: profile.nextOfKinFullName,
            nextOfKinPhoneCell: profile.nextOfKinPhoneCell,
            nextOfKinPhoneHome: profile.nextOfKinPhoneHome,
            nextOfKinEmailAddress: profile.nextOfKinEmailAddress,
            nextOfKinHomeAddress: profile.nextOfKinHomeAddress,
            nextOfKinRelationship: profile.nextOfKinRelationship,
          });
        }
      })
    );
  }

  // custom validation to check for password match
  getPasswordMatch(g: AbstractControl) {
    return g.get('newPassword')?.value === g.get('newPasswordConfirm')?.value
      ? null
      : { passwordMismatch: true };
  }

  loadGoogleMapsScript(): void {
    if (this.googleMapsScriptLoaded) {
      return;
    }

    const existingScript = document.getElementById('google-maps-script');
    if (!existingScript) {
      this.googleMapsScriptElement = document.createElement('script');
      this.googleMapsScriptElement.src = environment.mapsURL;
      this.googleMapsScriptElement.async = true;
      this.googleMapsScriptElement.defer = true;

      this.googleMapsScriptElement.onload = () => {
        this.googleMapsScriptLoaded = true;
        this.initializeAutocomplete();
      };

      document.body.appendChild(this.googleMapsScriptElement);
    } else {
      this.initializeAutocomplete();
    }
  }

  ngAfterViewInit(): void {
    if (this.googleMapsScriptLoaded) {
      this.initializeAutocomplete();
    }

    // Listen to profileImg control value changes
    this.subscription.add(
      this.passwordForm.valueChanges.subscribe((res) => {})
    );

    // Listen to profileImg control value changes
    this.subscription.add(
      this.getControl(this.biodataForm, 'profileImg').valueChanges.subscribe(
        (value) => {
          if (value) {
            // Set upload status
            this.profileImageUploadProgress = {
              status: 'pending',
              message: 'processing...',
            };

            // Add event listener to file controls
            this.addEvent();
          }
        }
      )
    );
  }

  initializeAutocomplete(): void {
    // Get home address location field
    const locationField = document.querySelector('[id=homeAddress]');

    if (locationField) {
      // Configure autocomplete for the location field
      const autocomplete = new google.maps.places.Autocomplete(
        locationField as HTMLInputElement
      );

      // Add event listener to the location field
      autocomplete.addListener('place_changed', () => {
        const place = autocomplete?.getPlace();

        // Update the form control value
        this.getControl(this.biodataForm, 'homeAddress')?.setValue(
          (<HTMLInputElement>document.getElementById('homeAddress')).value
        );
      });
    }
  }

  addEvent(): void {
    const inputElement = document.querySelector(
      'input[type="file"]'
    ) as HTMLInputElement;

    if (inputElement) {
      inputElement.addEventListener('change', (event) => {
        const file = (event.target as HTMLInputElement).files?.[0];
        if (file) {
          imageToBase64(file)
            .then((base64) => {
              const fileName = (event.target as HTMLInputElement).value.split(
                '\\fakepath\\'
              )[1];
              const imagePayload: FileUploadRequest = {
                blob: base64,
                filename: fileName,
              };

              this.fileUploaderService.uploadImage(imagePayload).subscribe({
                next: (response) => {
                  // Replace image
                  this.uploadedFilePath = response.imgUrl;

                  // Set upload status
                  this.profileImageUploadProgress = {
                    status: 'success',
                    message: 'file name: ' + fileName,
                  };
                },
                error: () => {
                  // Set upload status
                  this.profileImageUploadProgress = {
                    status: 'failed',
                    message: 'failed to upload, try again.',
                  };
                },
              });
            })
            .catch((error) => console.error('Error:', error));
        }
      });
    }
  }

  getControl(formName: FormGroup, controlName: string) {
    return formName.get(controlName) as FormControl;
  }

  getForm(formName: string): FormGroup | undefined {
    switch (formName) {
      case 'Biodata':
        return this.biodataForm as FormGroup;
      case 'Address':
        return this.addressForm as FormGroup;
      case 'Next of Kin':
        return this.nextOfKinForm as FormGroup;
      case 'Password Update':
        return this.passwordForm as FormGroup;
      default:
        return;
    }
  }

  setActiveSection(section: ActiveProfileSection): void {
    this.activeSection = section;
    switch (section) {
      case 'biodata':
        this.columnTitle = 'Biodata';
        break;
      case 'address':
        this.columnTitle = 'Address';
        break;
      case 'nextOfKin':
        this.columnTitle = 'Next of Kin';
        break;
      case 'passwordUpdate':
        this.columnTitle = 'Password Update';
        break;
      default:
        break;
    }
  }

  saveInfo() {
    let userDataPayload = {};

    // Prepare payload and upload
    switch (this.activeSection) {
      case 'biodata':
        userDataPayload = {
          profileImg: this.uploadedFilePath,
          homeAddress: this.getControl(this.biodataForm, 'homeAddress').value,
          phone: this.getControl(this.biodataForm, 'phone').value,
        };
        break;
      case 'nextOfKin':
        userDataPayload = this.nextOfKinForm.value;
        break;
      case 'passwordUpdate':
        break;
      default:
        break;
    }

    this.subscription.add(
      this.userService.updateUserProfile(userDataPayload).subscribe({
        next: (res) => {
          if (res && 'id' in res) {
            this._snackBar.displaySnackBar(
              'Record saved successfully',
              'OK',
              'green-snackbar'
            );

            // Dispatch the action to update the user profile in the state
            this.store.dispatch(updateUserProfile({ profile: res }));
          }
        },
        error: (err) => {
          if ('error' in err && 'error' in err['error']) {
            this._snackBar.displaySnackBar(err.error.error, '', 'red-snackbar');
          } else if ('message' in err) {
            this._snackBar.displaySnackBar(err.message, '', 'red-snackbar');
          } else {
            this._snackBar.displaySnackBar(
              'Unable to save record, please contact admin',
              '',
              'red-snackbar'
            );
          }
        },
      })
    );
  }

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }

    // Cleanup Google Maps script
    if (this.googleMapsScriptElement) {
      document.body.removeChild(this.googleMapsScriptElement);
      this.googleMapsScriptElement = null;
      this.googleMapsScriptLoaded = false;
    }
  }
}
