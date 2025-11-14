import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild,
} from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import { faLock, faArrowCircleLeft } from '@fortawesome/free-solid-svg-icons';
import { faEye, faEyeSlash } from '@fortawesome/free-regular-svg-icons';
import intlTelInput from 'intl-tel-input'; // import intl-tel-input library for phone number validation.
import { SnackbarUtilService } from '../shared/snackbar/snackbar-util.service';

interface IInputConfig {
  label?: string;
  isRequired: boolean;
  placeholder?: string;
  id: string;
  chkLabel?: string;
  theme?: 'light' | 'dark' | undefined;
  min?: string;
  max?: string;
  readonly?: boolean;
  accept?: string;
}

@Component({
  selector: 'app-input-group',
  templateUrl: './input-group.component.html',
  styleUrls: ['./input-group.component.css'],
})
export class InputGroupComponent implements AfterViewInit {
  @Input('isClicked') isClicked!: boolean;
  @Input('inputConfig') config!: IInputConfig;
  @Input() controlName!: string;
  @Input() form!: FormGroup;
  @Input('inputType') inputType!: string;
  @Input('isDisabled') isDisabled!: boolean;
  @Input() maxSize: number = 1000000; // Default max size in bytes (5MB)
  @Output() fileSelected = new EventEmitter<File>();
  @Output() fileTooLarge = new EventEmitter<string>();
  @Output() onChecked: EventEmitter<boolean> = new EventEmitter();
  @ViewChild('inputField') inputField!: ElementRef;
  @ViewChild('phone') intlPhone: ElementRef | undefined;
  showPassword: boolean = false;
  faLock: IconProp = faLock;
  faEye: IconProp = faEye;
  faEyeSlash: IconProp = faEyeSlash;
  faArrowCircleLeft: IconProp = faArrowCircleLeft;
  iti: any;
  errorMessage: string | null = null;

  constructor(private _snackBar: SnackbarUtilService) {}

  ngAfterViewInit(): void {
    if (this.intlPhone) {
      this.iti = intlTelInput(this.intlPhone.nativeElement, {
        initialCountry: 'ZA',
        showSelectedDialCode: true,
        placeholderNumberType: 'MOBILE',
        formatAsYouType: true,
        utilsScript:
          'https://cdnjs.cloudflare.com/ajax/libs/intl-tel-input/21.2.5/js/utils.js',
        formatOnDisplay: true,
      });

      // Set width to 100%
      (this.intlPhone.nativeElement as HTMLElement).style.width = '100%';

      // Add event listeners
      this.intlPhone.nativeElement.addEventListener('blur', () => {
        this.onBlur();
      });

      this.intlPhone.nativeElement.addEventListener('countrychange', () => {
        const countryData = this.iti.getSelectedCountryData();
        this.getSelectedCountry(countryData);
      });

      this.intlPhone.nativeElement.addEventListener('input', () => {
        const number = this.iti.getNumber();
        this.getNumber(number);
      });

      this.intlPhone.nativeElement.addEventListener('keyup', () => {
        const isValid = this.iti.isValidNumber();
        this.hasError(!isValid);
      });

      // Add event listener for country change
      this.intlPhone.nativeElement.addEventListener('countrychange', () => {
        this.updateMaxLengthValidator();
      });

      // Initialize the max length based on the initial country
      this.updateMaxLengthValidator();
    }
  }

  // Method to update the maxlength validator based on the selected country
  updateMaxLengthValidator(): void {
    const countryData = this.iti.getSelectedCountryData();
    const exampleNumber = this.iti.getNumberType();
    const maxLength = this.getPhoneNumberMaxLength(
      countryData.dialCode,
      exampleNumber
    );

    // console.log('countryData', countryData);
    // console.log('exampleNumber', exampleNumber);
    // console.log('this.iti', this.iti);
    // console.log('maxLength', maxLength);

    this.formName().setValidators([Validators.maxLength(maxLength)]);

    if (this.config.isRequired) {
      this.formName().setValidators([Validators.required]);
    }

    // Update the validity status of the form control
    this.formName().updateValueAndValidity();
    this.form.updateValueAndValidity();
  }

  // Helper method to get the max length of the phone number for the selected country
  getPhoneNumberMaxLength(dialCode: string, exampleNumber: number): number {
    // Implement logic to determine the max length based on the dial code and example number
    // This logic will depend on the specific requirements and available data
    // You might need to use an external library or API to get the exact number length
    // For demonstration purposes, we'll assume a default length of 15
    // (this can be adjusted based on actual requirements)
    return 15;
  }

  // Method to set the phone number and update the country flag
  setPhoneNumber(phoneNumber: string): void {
    if (this.iti) {
      this.iti.setNumber(phoneNumber);
    }
  }

  // Event handlers
  hasError(hasError: boolean): void {
    // do something here
  }

  getNumber(number: string): void {
    // do something here
    this.formName().setValue(number);
  }

  getSelectedCountry(countryData: any): void {
    // do something here
  }

  onBlur(): void {
    // do something here
  }

  formName() {
    return this.form?.get(this.controlName) as FormControl;
  }

  preventInvalidCharacters(event: KeyboardEvent) {
    // Get the pressed key
    const key = event.key;
    if (this.inputType === 'number' || this.inputType === 'tel') {
      if (key === 'e' || key === '-' || (key && key.match(/^[a-zA-Z]$/))) {
        event.preventDefault();
      }
    }
  }

  toggleClick() {
    this.onChecked.emit(!this.isClicked);
  }

  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  onFileSelected(event: Event): void {
    event.preventDefault();
    if ((event.target as HTMLInputElement).type === 'file') {
      const input = event.target as HTMLInputElement;
      if (input.files && input.files.length > 0) {
        const fileSize = input.files[0].size / 1000000;

        if (fileSize > this.maxSize / 1_000_000) {
          this.formName().reset(); // Clear the form control value
          this._snackBar.displaySnackBar(
            `WARNING: Large file detected. Please upload a file less than ${
              this.maxSize / 1000000
            }MB`,
            '',
            'red-snackbar'
          );
          this.errorMessage = `File size exceeds the maximum limit of ${
            this.maxSize / 1000000
          } MB.`;
          this.fileTooLarge.emit(this.errorMessage);
        } else {
          this.errorMessage = null;
          this.fileSelected.emit(input.files[0]);
        }
      }
    }
  }
}
