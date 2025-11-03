import { FormControl } from '@angular/forms';

export const convertNegativeToPositive = (formControl: FormControl) => {
  const value = formControl.value;
  if (value.includes('-')) {
    formControl.setValue(value.split('-').join(''));
  }
};
