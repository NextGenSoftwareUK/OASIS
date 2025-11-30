import { MatSnackBar } from '@angular/material/snack-bar';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class SnackbarUtilService {
  constructor(private _snackBar: MatSnackBar) {}

  displaySnackBar(
    message: string,
    action: string,
    style1: string,
    style2: string = 'login-snackbar'
  ) {
    return this._snackBar.open(message, action, {
      duration: 5000,
      panelClass: [style1, style2],
    });
  }
}
