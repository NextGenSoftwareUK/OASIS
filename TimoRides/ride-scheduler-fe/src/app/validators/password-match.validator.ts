import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function passwordMatchValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const formGroup = control.parent;
    if (!formGroup) {
      return null;
    }

    const pwdControl = formGroup.get('newPassword');
    const pwdConfirmControl = formGroup.get('newPasswordConfirm');

    if (!pwdControl || !pwdConfirmControl) {
      return null;
    }

    return pwdControl.value === pwdConfirmControl.value
      ? null
      : { passwordMismatch: true };
  };
}
