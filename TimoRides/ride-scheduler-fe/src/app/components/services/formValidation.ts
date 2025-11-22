import { FormI } from 'src/app/models/dynamic-formI';

interface FormDataI {
  [key: string]: string | undefined; // Allow properties to be strings or undefined
  email?: string;
}

export class ValidateForm {
  formData?: object;
  formValidInfo: FormI = {};
  currentDate = new Date();

  constructor(formData: FormDataI) {
    this.formData = formData;
  }

  isValid() {
    Object.entries(this.formData!).forEach(([key, value]) => {
      if (!value) {
        this.formValidInfo[`${key}`] = {
          [key]: true,
          errorMsg: 'Input is Empty',
        };
      } else if (key === 'email' && !this.isValidEmail(value) && value) {
        // Handle invalid email
        this.formValidInfo[`${key}`] = {
          [key]: true,
          errorMsg: 'Provide proper email addresss',
        };
      } else if (key === 'pickUpTime' && value) {
        let valueInput = new Date(value);
        // Handle invalid date
        if (valueInput < this.currentDate) {
          this.formValidInfo[`${key}`] = {
            [key]: true,
            errorMsg: 'Provide a current or future date',
          };
        }
      } else if (key === 'phone' && !this.isValidPhoneNumber(value) && value) {
        // Handle invalid date

        this.formValidInfo[`${key}`] = {
          [key]: true,
          errorMsg: 'Provide a valid phone number',
        };
      }
    });

    return this.formValidInfo;
  }

  private isValidEmail(email: string) {
    // Regular expression for validating email addresses
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    return emailRegex.test(email);
  }

  private isValidPhoneNumber(phoneNumber: string) {
    const phoneNumberRegex = /^[0-9 ()+-]+$/;

    if (phoneNumber.length < 10) {
      return false;
    }

    // Check if the remaining value consists of only digits
    return phoneNumberRegex.test(phoneNumber); // Not a valid phone number (contains non-digits)
  }
}
