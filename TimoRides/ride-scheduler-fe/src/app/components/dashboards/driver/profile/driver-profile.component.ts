interface UserDataPayload {
  [key: string]: any; // Allows any type of value
}

import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  FormControl,
  Validators,
} from '@angular/forms';
import { Store } from '@ngrx/store';
import { combineLatest, Observable, Subscription } from 'rxjs';
import { AppState } from 'src/app/app.state';
import { FileUploaderService } from 'src/app/components/services/file-uploader/file-uploader.service';
import { UserService } from 'src/app/components/services/user/user.service';
import { accountTypes, idList, titles } from 'src/app/constants/user.constants';
import { SelectConfiguration } from 'src/app/models/ride-form';
import { DriverProfile, Profile } from 'src/app/models/user.model';
import { updateUserProfile } from 'src/app/store/user/user.actions';
import {
  selectUserError,
  selectUserProfile,
} from 'src/app/store/user/user.selectors';
import {
  ProfileImageUploadProgress,
  profileImageUploadProgress,
} from 'src/app/constants/ride-schedule.constants';
import { imageToBase64 } from 'src/app/utils/base64-converter';
import { FileUploadRequest } from 'src/app/models/file-uploader';
import { GoogleAddressService } from 'src/app/components/services/google/google.service';
import { MapLocation } from 'src/app/models/booking-form';
import * as CarActions from 'src/app/store/cars/car.actions';
import { Car } from 'src/app/models/car';
import {
  selectCreatedCar,
  selectDriverCar,
} from 'src/app/store/cars/car.selectors';
import { DatePipe } from '@angular/common';
import { SnackbarUtilService } from 'src/app/components/shared/snackbar/snackbar-util.service';
import { DropdownConfiguration } from 'src/app/models/dropdown';
import { environment } from 'src/environments/environment';
import { selectBanks } from 'src/app/store/banks/bank.selectors';
import { BankInfo } from 'src/app/store/banks/bank.model';

@Component({
  selector: 'app-driver-profile',
  templateUrl: './driver-profile.component.html',
  styleUrls: ['./driver-profile.component.css'],
})
export class DriverProfileComponent
  implements OnInit, AfterViewInit, OnDestroy
{
  isTrue = false;
  driverDetailsForm!: FormGroup;
  attachmentsForm!: FormGroup;
  nextOfKinForm!: FormGroup;
  accountDetailsForm!: FormGroup;
  vehicleDetailsForm!: FormGroup;
  commissionDeductionForm!: FormGroup;
  termsConditionsForm!: FormGroup;
  subscription: Subscription = new Subscription();
  isProfileImageUploaded: boolean = false;
  isButtonDisabled!: boolean;
  activeSection: string = 'driverDetails';
  columnTitle: string = 'Driver Details';
  currentUploadingImage = '';
  profileImageUploadProgress: ProfileImageUploadProgress =
    profileImageUploadProgress;
  accountTypes: DropdownConfiguration = accountTypes;
  attachments: string[] = [];
  fileUploads: {
    [key: string]: {
      rawFile: string;
      uploadURL: string;
    };
  } = {};
  nextOfKinControls = [0, 1];
  dropdownExpanded: boolean = false;
  currentDropdownIndex: number = 0;
  idOptions: DropdownConfiguration = idList;
  currrentTitle: SelectConfiguration = {
    id: 0,
    name: 'Select title',
    value: '',
  };
  titles: DropdownConfiguration = titles;
  profile$: Observable<Profile | null>;
  profile: Profile | null = null;
  driverCar$: Observable<Car | null>;
  driverCar: Car | null = null;
  selectCreatedCar$: Observable<Car | null>;
  selectCreatedCar: Car | null = null;
  error$: Observable<any>;
  banks$: Observable<BankInfo[] | null>;
  banks: DropdownConfiguration | null = null;

  currentID: SelectConfiguration = {
    id: 0,
    name: 'Select means of ID',
    value: '',
  };
  uploadedFilePath: string = '';
  vehicleLocation = {
    latitude: 0,
    longitude: 0,
  };

  datePipe: DatePipe;
  fileDetails: File | null = null;
  fileError: string | null = null;
  maxFileSize: number = 5000000;

  /** this list holds all images saved to the backend
   * to enable us toggle the input type='file' element
   * between 'file' and 'text'
   */
  savedImageFiles: any = {};
  /** this list holds all images saved to the backend
   * to enable us toggle the input type='file' element
   * between 'file' and 'text'
   */

  private googleMapsScript: HTMLScriptElement | null = null;

  /** Use this to save vehicle state before saving vehicle info */
  private vehicleState: string = '';

  constructor(
    private fb: FormBuilder,
    private store: Store<AppState>,
    private fileUploaderService: FileUploaderService,
    private userService: UserService,
    private googleAddressService: GoogleAddressService,
    private changeDetectorRef: ChangeDetectorRef,
    private _snackBar: SnackbarUtilService
  ) {
    this.profile$ = this.store.select(selectUserProfile);
    this.driverCar$ = this.store.select(selectDriverCar);
    this.selectCreatedCar$ = this.store.select(selectCreatedCar);
    this.error$ = this.store.select(selectUserError);

    // get banks
    this.banks$ = this.store.select(selectBanks);

    // initialize datepipe
    this.datePipe = new DatePipe('en-GB');
  }

  ngOnInit(): void {
    // prepare banks list
    this.banks$.subscribe((banks) => {
      if (banks) {
        this.banks = {
          label: 'Select bank',
          options: banks.map((bank) => {
            return {
              value: bank.name,
              name: bank.name,
            };
          }),
        };
      }
    });

    this.driverDetailsForm = this.fb.group({
      profileImg: [''],
      title: [''],
      fullName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      homeAddress: ['', [Validators.required]],
      phone: ['', [Validators.required]],
      altPhone: [''],
      idType: ['', []],
      idNumber: ['', []],
    });

    this.attachmentsForm = this.fb.group({
      driverLicenseImg: ['', []],
      driverIdentityCardImg: ['', []],
      professionalDriverPermitImg: ['', []],
      comprehensiveInsuranceImg: ['', []],
      operatorCardImg: ['', []],
      inspectionCertImg: ['', []],
    });

    this.nextOfKinForm = this.fb.group({
      nokFullName0: ['', [Validators.required]],
      nokRelationship0: ['', [Validators.required]],
      nokContactCell0: ['', [Validators.required]],
      nokContactHome0: [''],
      nokAddress0: ['', [Validators.required]],
      nokEmail0: [''],
      nokFullName1: [''],
      nokRelationship1: [''],
      nokContactCell1: [''],
      nokContactHome1: [''],
      nokAddress1: [''],
      nokEmail1: [''],
    });

    this.accountDetailsForm = this.fb.group({
      nameOfAccountHolder: ['', [Validators.required]],
      bankName: ['', [Validators.required]],
      bankAccountNumber: ['', [Validators.required]],
    });

    this.vehicleDetailsForm = this.fb.group({
      vehicleRegistrationNumber: ['', [Validators.required]],
      vehicleModelYear: ['', [Validators.required]], // not in db schema
      vehicleMake: ['', [Validators.required]], // not in db schema
      vehicleModel: ['', [Validators.required]],
      engineNumber: ['', [Validators.required]],
      vehicleColour: ['', [Validators.required]],
      insuranceBroker: ['', [Validators.required]],
      insurancePolicyNumber: ['', [Validators.required]],
      vehicleAddress: ['', [Validators.required]],
      driverCarImage: ['', [Validators.required]],
      carExteriorImage: ['', [Validators.required]],
      carInteriorImage: ['', [Validators.required]],
    });

    this.commissionDeductionForm = this.fb.group({
      commissionAuthorization: [
        false,
        [Validators.required, Validators.requiredTrue],
      ],
      commissionSignature: ['', [Validators.required]],
      commissionDate: ['', [Validators.required]],
      commissionNameInPrint: ['', [Validators.required]],
    });

    this.termsConditionsForm = this.fb.group({
      tcConsent: ['', [Validators.required]],
      tcFullName: ['', [Validators.required]],
      tcAuthorizedSignatureImg: ['', [Validators.required]],
      tcDate: [null, [Validators.required]],
      tcPlace: ['', [Validators.required]],
    });

    this.toggleAttachmentFormValidity();

    // patch values to formGroups
    this.subscription.add(
      this.profile$.subscribe((profile) => {
        // save profile to profile variable
        this.profile = profile;

        if (profile && 'id' in (profile as DriverProfile)) {
          this.driverDetailsForm.patchValue({
            title: '',

            fullName: profile?.fullName,
            email: profile?.email,
            homeAddress: profile?.homeAddress,
            phone: profile?.phone,
          });

          // toggle required state of idType and idNumber
          this.driverDetailsForm
            .get('idType')
            ?.setValidators(
              (profile as DriverProfile).identityType
                ? null
                : [Validators.required]
            );

          this.driverDetailsForm
            .get('idNumber')
            ?.setValidators(
              (profile as DriverProfile).identityNumber
                ? null
                : [Validators.required]
            );

          this.changeDetectorRef.detectChanges();

          this.uploadedFilePath = profile.profileImg;

          // populate NOK details
          this.nextOfKinForm.patchValue({
            nokFullName0: (profile as DriverProfile).nextOfKinFullName,
            nokRelationship0: (profile as DriverProfile).nextOfKinRelationship,
            nokContactCell0: (profile as DriverProfile).nextOfKinPhoneCell,
            nokContactHome0: (profile as DriverProfile).nextOfKinPhoneHome,
            nokAddress0: (profile as DriverProfile).nextOfKinHomeAddress,
            nokEmail0: (profile as DriverProfile).nextOfKinEmailAddress,
            nokFullName1: (profile as DriverProfile).nextOfKinFullNameAlt,
            nokRelationship1: (profile as DriverProfile)
              .nextOfKinRelationshipAlt,
            nokContactCell1: (profile as DriverProfile).nextOfKinPhoneCellAlt,
            nokContactHome1: (profile as DriverProfile).nextOfKinPhoneHomeAlt,
            nokAddress1: (profile as DriverProfile).nextOfKinHomeAddressAlt,
            nokEmail1: (profile as DriverProfile).nextOfKinEmailAddressAlt,
          });

          // get all saved images
          this.savedImageFiles = {
            ...this.savedImageFiles,
            profileImg: (profile as DriverProfile).profileImg,
            driverLicenseImg: (profile as DriverProfile).driverLicenseImg,
            driverIdentityCardImg: (profile as DriverProfile)
              .driverIdentityCardImg,
            professionalDriverPermitImg: (profile as DriverProfile)
              .professionalDriverPermitImg,
            comprehensiveInsuranceImg: (profile as DriverProfile)
              .comprehensiveInsuranceImg,
            operatorCardImg: (profile as DriverProfile).operatorCardImg,
            inspectionCertImg: (profile as DriverProfile).inspectionCertImg,
            bankAuthorizedSignature: (profile as DriverProfile)
              .bankAuthorizedSignature,
            tcAuthorizedSignatureImg: (profile as DriverProfile)
              .tcAuthorizedSignatureImg,
          };

          // populate car info
          this.store.dispatch(
            CarActions.loadDriverCar({ driverId: profile.id })
          );

          combineLatest([this.selectCreatedCar$, this.driverCar$]).subscribe(
            ([selectCreatedCar, driverCar]) => {
              // Give preference to selectCreatedCar
              if (selectCreatedCar) {
                this.selectCreatedCar = selectCreatedCar;
              } else if (driverCar) {
                this.selectCreatedCar = driverCar;
              }
            }
          );
        }
      })
    );

    this.changeDetectorRef.detectChanges();
  }

  ngAfterViewInit(): void {
    this.loadScript();
    // this.loadGoogleMapsScript();
    this.subscribeToFormControlChanges();
    // this.showSection('driverDetails');
  }

  loadScript() {
    const existingScript = document.getElementById('google-maps-script');
    if (!existingScript) {
      const script = document.createElement('script');
      script.src = environment.mapsURL;
      script.id = 'google-maps-script';
      script.async = true;
      script.defer = true;
      script.onload = () => this.showSection('driverDetails');
      document.body.appendChild(script);
      this.googleMapsScript = script;
    } else {
      this.showSection('driverDetails');
    }
  }

  initializeAutocomplete(locationField: any, id: string) {
    const input = document.getElementById(
      'vehicle-address-input'
    ) as HTMLInputElement;

    const autocomplete = new google.maps.places.Autocomplete(locationField);

    autocomplete.addListener('place_changed', () => {
      const place = autocomplete.getPlace();

      const formattedAddress = (<HTMLInputElement>(
        document.getElementById(locationField.id)
      )).value;

      if (formattedAddress) {
        locationField.value = formattedAddress;
      } else {
      }

      if (id.includes('vehicle')) {
        this.getControl(this.vehicleDetailsForm, 'vehicleAddress')?.setValue(
          formattedAddress
        );

        const vehicleLocation =
          this.googleAddressService.GetLocationCoordinates(
            place
          ) as MapLocation;

        this.vehicleLocation = {
          latitude: vehicleLocation.latitude,
          longitude: vehicleLocation.longitude,
        };

        // save vehicle state
        const countryState = place.address_components
          ?.find((component) =>
            component.types.includes('administrative_area_level_1')
          )
          ?.long_name.replace(' state', '')
          .replace(' State', '');

        // dispatch request state
        if (countryState) this.vehicleState = countryState;
      } else {
        this.getControl(this.driverDetailsForm, 'homeAddress')?.setValue(
          formattedAddress
        );
      }
    });
  }

  toggleAttachmentFormValidity() {
    this.attachmentsForm.valueChanges.subscribe((values) => {
      this.isButtonDisabled = !Object.values(values).some(
        (value) => value !== undefined
      );

      this.changeDetectorRef.detectChanges();
    });
  }

  async subscribeToFormControlChanges() {
    // Loop through all the form controls
    Object.keys(this.attachmentsForm.controls).forEach((controlName) => {
      // Get the form control by its name
      const control = this.attachmentsForm.get(controlName);

      // Subscribe to the valueChanges observable
      control?.valueChanges.subscribe((value) => {
        if (value) {
          if (!this.savedImageFiles[controlName]) {
            // set current uploading file
            this.currentUploadingImage = controlName;

            // set upload status
            this.profileImageUploadProgress = {
              status: 'pending',
              message: 'processing...',
            };

            // add event listener to file controls
            this.addEvent(controlName);
          }
        } else {
          delete this.fileUploads[controlName];
          this.fileUploads = this.fileUploads;
          this.removeFromList(controlName);

          this.profileImageUploadProgress = {
            status: 'none',
            message: '',
          };
        }
      });
    });

    // subscribe to valueChanges for car images controls
    // Loop through all the form controls
    Object.keys(this.vehicleDetailsForm.controls).forEach((controlName) => {
      // Get the form control by its name
      const control = this.vehicleDetailsForm.get(controlName);

      // Subscribe to the valueChanges observable
      if (controlName.includes('Image')) {
        control?.valueChanges.subscribe((value) => {
          if (value) {
            // set current uploading file
            this.currentUploadingImage = controlName;

            // set upload status
            this.profileImageUploadProgress = {
              status: 'pending',
              message: 'processing...',
            };

            // add event listener to file controls
            this.addEvent(controlName);
          }
        });
      }
    });

    // subscribe to profileImg changes
    this.getControl(
      this.driverDetailsForm,
      'profileImg'
    ).valueChanges.subscribe(async (value) => {
      if (value) {
        // set current uploading file
        this.currentUploadingImage = 'profileImg';

        this.profileImageUploadProgress = {
          status: 'pending',
          message: 'processing...',
        };
        await this.addEvent('profileImg');
        this.saveToDB({
          profileImg: this.fileUploads['profileImg']?.['uploadURL'],
        });
      }
    });

    // subscribe to tcAuthorizedSignatureImg
    this.getControl(
      this.termsConditionsForm,
      'tcAuthorizedSignatureImg'
    ).valueChanges.subscribe(async (value) => {
      if (value) {
        // set current uploading file
        this.currentUploadingImage = 'tcAuthorizedSignatureImg';

        this.profileImageUploadProgress = {
          status: 'pending',
          message: 'processing...',
        };
        await this.addEvent('tcAuthorizedSignatureImg');
      }
    });
  }

  async addEvent(controlName: string): Promise<void> {
    const inputElement = document.querySelector(
      `input[id=${controlName}]`
    ) as HTMLInputElement;

    if (inputElement)
      return new Promise((resolve, reject) => {
        inputElement.addEventListener('change', async (event) => {
          const file = (event.target as HTMLInputElement).files?.[0];
          if (file) {
            try {
              const base64 = await imageToBase64(file);
              const fileName = (event.target as HTMLInputElement).value.split(
                '\\fakepath\\'
              )[1];
              const imagePayload: FileUploadRequest = {
                blob: base64,
                filename: fileName,
              };

              this.fileUploaderService.uploadImage(imagePayload).subscribe({
                next: (response) => {
                  this.fileUploads = {
                    ...this.fileUploads,
                    [controlName]: {
                      rawFile: fileName,
                      uploadURL: response.imgUrl,
                    },
                  };

                  this.addToList(controlName);

                  this.profileImageUploadProgress = {
                    status: 'success',
                    message: 'file name: ' + fileName,
                  };

                  resolve();
                },
                error: (err) => {
                  this.profileImageUploadProgress = {
                    status: 'failed',
                    message: 'failed to upload, try again.',
                  };

                  reject(err);
                },
              });
            } catch (error) {
              this.profileImageUploadProgress = {
                status: 'none',
                message: '',
              };
              reject(error);
            }
          } else {
            resolve();
          }
        });
      });
  }

  addToList(item: string) {
    const itemIndex = this.attachments.findIndex(
      (attachment) => attachment === item
    );

    if (itemIndex === -1) {
      this.attachments.push(item);
    }
  }

  removeFromList(item: string) {
    const itemIndex = this.attachments.findIndex(
      (attachment) => attachment === item
    );

    if (itemIndex !== -1) {
      this.attachments.splice(itemIndex, 1);
    }

    this.changeDetectorRef.markForCheck();
    this.changeDetectorRef.detectChanges();
  }

  getFileName(fileName: string) {
    return this.fileUploads[fileName]?.['rawFile'];
  }

  showSection(section: string) {
    this.activeSection = section;
    this.columnTitle = this.getSectionTitle(section);

    // exclude attachments section
    if (section !== 'attachments') {
      this.isButtonDisabled = (
        this.getForm(this.columnTitle) as FormGroup
      ).invalid;

      (this.getForm(this.columnTitle) as FormGroup).valueChanges.subscribe(
        (value) => {
          this.isButtonDisabled = (
            this.getForm(this.columnTitle) as FormGroup
          ).invalid;
        }
      );
    } else {
      // check if attachments has value
      this.toggleAttachmentFormValidity();
    }

    // apply google address service
    if (section === 'driverDetails') {
      setTimeout(() => {
        this.setupGoogleAddress(['homeAddress']);
      }, 0);
    }

    if (section === 'vehicleDetails') {
      setTimeout(() => {
        this.setupGoogleAddress(['vehicleAddress']);
      }, 0);
    }

    this.changeDetectorRef.detectChanges();
  }

  setupGoogleAddress(ids: string[]): void {
    ids.forEach((id) => {
      const locationField = document.getElementById(id) as HTMLInputElement;

      if (locationField instanceof HTMLInputElement) {
        this.initializeAutocomplete(locationField, id);
      } else {
        console.error(
          `Element with ID ${id} is not an instance of HTMLInputElement or was not found.`
        );
      }
    });
  }

  // Toggle open/close
  toggleDropdown(index: number) {
    if (index === this.currentDropdownIndex) {
      this.dropdownExpanded = !this.dropdownExpanded;
      this.currentDropdownIndex = 0;
    } else {
      this.currentDropdownIndex = index;
    }
  }

  getControl(formName: FormGroup, name: string) {
    return formName.get(name) as FormControl;
  }

  isRequired(formName: FormGroup, name: string) {
    return (formName.get(name) as FormControl).hasValidator(
      Validators.required
    );
  }

  getSectionTitle(section: string): string {
    switch (section) {
      case 'driverDetails':
        return 'Driver Details';
      case 'driverAddress':
        return 'Driver Address';
      case 'attachments':
        return 'Attachments';
      case 'nextOfKin':
        return 'Next of Kin';
      case 'accountDetails':
        return 'Account Details';
      case 'vehicleDetails':
        return 'Vehicle Details';
      case 'commissionDeduction':
        return 'Commission Deduction';
      case 'termsConditions':
        return 'Terms & Conditions';
      default:
        return '';
    }
  }

  setConsent(form: FormGroup, controlName: string, event: boolean) {
    this.getControl(form, controlName).setValue(event === true ? true : '');
  }

  saveInfo() {
    let userDataPayload: UserDataPayload = {};

    // prepare payload and upload
    switch (this.activeSection) {
      case 'driverDetails':
        userDataPayload = {
          title: this.getControl(this.driverDetailsForm, 'title').value,
          homeAddress: this.getControl(this.driverDetailsForm, 'homeAddress')
            .value,
          phone: this.getControl(this.driverDetailsForm, 'phone').value,
          workPhone: this.getControl(this.driverDetailsForm, 'altPhone').value,
          identityType: this.getControl(this.driverDetailsForm, 'idType').value,
          identityNumber: this.getControl(this.driverDetailsForm, 'idNumber')
            .value,
        };

        break;
      case 'attachments':
        Object.keys(this.fileUploads).forEach((key) => {
          userDataPayload = {
            ...userDataPayload,
            [key]: this.fileUploads[key].uploadURL,
          };
        });

        const splitObject = {
          driverLicenseImg: this.fileUploads['driverLicenseImg']?.['uploadURL'],
          driverIdentityCardImg:
            this.fileUploads['driverIdentityCardImg']?.['uploadURL'],
          comprehensiveInsuranceImg:
            this.fileUploads['comprehensiveInsuranceImg']?.['uploadURL'],
          operatorCardImg: this.fileUploads['operatorCardImg']?.['uploadURL'],
          inspectionCertImg:
            this.fileUploads['inspectionCertImg']?.['uploadURL'],
          professionalDriverPermitImg:
            this.fileUploads['professionalDriverPermitImg']?.['uploadURL'],
        };

        userDataPayload = {
          ...splitObject,
        };

        break;
      case 'nextOfKin':
        userDataPayload = {
          nextOfKinFullName: this.nextOfKinForm.value['nokFullName0'],
          nextOfKinRelationship: this.nextOfKinForm.value['nokRelationship0'],
          nextOfKinPhoneCell: this.nextOfKinForm.value['nokContactCell0'],
          nextOfKinPhoneHome: this.nextOfKinForm.value['nokContactHome0'],
          nextOfKinHomeAddress: this.nextOfKinForm.value['nokAddress0'],
          nextOfKinEmailAddress: this.nextOfKinForm.value['nokEmail0'],
          nextOfKinFullNameAlt: this.nextOfKinForm.value['nokFullName1'],
          nextOfKinRelationshipAlt:
            this.nextOfKinForm.value['nokRelationship1'],
          nextOfKinPhoneCellAlt: this.nextOfKinForm.value['nokContactCell1'],
          nextOfKinPhoneHomeAlt: this.nextOfKinForm.value['nokContactHome1'],
          nextOfKinHomeAddressAlt: this.nextOfKinForm.value['nokAddress1'],
          nextOfKinEmailAddressAlt: this.nextOfKinForm.value['nokEmail1'],
        };

        break;
      case 'accountDetails':
        userDataPayload = {
          nameOfAccountHolder:
            this.accountDetailsForm.value['nameOfAccountHolder'],
          bankName: this.accountDetailsForm.value['bankName'],
          bankAccountNumber: this.accountDetailsForm.value['bankAccountNumber'],
        };

        break;
      case 'vehicleDetails':
        userDataPayload = {
          vehicleRegNumber:
            this.vehicleDetailsForm.value['vehicleRegistrationNumber'],
          vehicleModelYear: this.vehicleDetailsForm.value['vehicleModelYear'],
          vehicleMake: this.vehicleDetailsForm.value['vehicleMake'],
          vehicleModel: this.vehicleDetailsForm.value['vehicleModel'],
          engineNumber: this.vehicleDetailsForm.value['engineNumber'],
          vehicleColor: this.vehicleDetailsForm.value['vehicleColour'],
          insuranceBroker: this.vehicleDetailsForm.value['insuranceBroker'],
          insurancePolicyNumber:
            this.vehicleDetailsForm.value['insurancePolicyNumber'],
          imagePath: this.fileUploads['driverCarImage']['uploadURL'],
          altImagePath: this.fileUploads['carExteriorImage']['uploadURL'],
          interiorImagePath: this.fileUploads['carInteriorImage']['uploadURL'],
          vehicleAddress: this.vehicleDetailsForm.value['vehicleAddress'],
          state: this.vehicleState,
          location: {
            ...this.vehicleLocation,
          },
        };

        break;
      case 'termsConditions':
        userDataPayload = {
          ...this.termsConditionsForm.value,
          tcAuthorizedSignatureImg:
            this.fileUploads['tcAuthorizedSignatureImg'].uploadURL,
        };

        break;
      default:
        break;
    }

    // remove key-value pairs with falsy values
    Object.keys(userDataPayload).forEach((key) => {
      if (!userDataPayload[key]) {
        delete userDataPayload[key];
      }
    });

    if (Object.keys(userDataPayload).length === 0) {
      return;
    }

    this.saveToDB(userDataPayload);
  }

  saveToDB(payload: any) {
    // Save to DB
    if ('vehicleRegNumber' in payload) {
      this.store.dispatch(CarActions.createCar({ car: payload }));
    } else {
      this.subscription.add(
        this.userService.updateUserProfile(payload).subscribe({
          next: (res) => {
            if (res && 'id' in res) {
              if ('profileImg' in payload) {
                this.isProfileImageUploaded = true;
              }

              this._snackBar.displaySnackBar(
                'Record saved successfully',
                '',
                'green-snackbar'
              );

              this.store.dispatch(updateUserProfile({ profile: res }));
            }
          },
          error: (err) => {
            this._snackBar.displaySnackBar(
              'An error occurred' + err.message,
              '',
              'red-snackbar'
            );
          },
        })
      );
    }
  }

  getForm(formName: string): FormGroup | undefined {
    switch (formName) {
      case 'Driver Details':
        return this.driverDetailsForm as FormGroup;
      case 'Attachments':
        return this.attachmentsForm as FormGroup;
      case 'Next of Kin':
        return this.nextOfKinForm as FormGroup;
      case 'Account Details':
        return this.accountDetailsForm as FormGroup;
      case 'Vehicle Details':
        return this.vehicleDetailsForm as FormGroup;
      case 'Terms & Conditions':
        return this.termsConditionsForm as FormGroup;
      default:
        return;
    }
  }

  getFormControl(form: FormGroup, control: string): FormControl {
    return form.get(control) as FormControl;
  }

  isDriverProfile(profile: Profile | DriverProfile): profile is DriverProfile {
    return 'identityType' in profile ? true : false;
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    if (this.googleMapsScript) {
      document.body.removeChild(this.googleMapsScript);
      this.googleMapsScript = null;
    }
  }
}
