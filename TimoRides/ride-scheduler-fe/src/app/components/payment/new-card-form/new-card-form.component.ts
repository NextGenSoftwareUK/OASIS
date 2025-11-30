import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  Validators,
} from '@angular/forms';
import { CardType, PaymentCard } from 'src/app/models/payment-card';
import { convertNegativeToPositive } from 'src/app/utils/convertToPositive';
import { numberRegex } from 'src/app/utils/utilities';
import { SnackbarUtilService } from '../../shared/snackbar/snackbar-util.service';

@Component({
  selector: 'app-new-card-form',
  templateUrl: './new-card-form.component.html',
  styleUrls: ['./new-card-form.component.css'],
})
export class NewCardFormComponent implements OnInit {
  @Output() saveCard: EventEmitter<PaymentCard> = new EventEmitter();
  isChecked!: boolean;
  newCardForm: FormGroup = this.formBuilder.group({
    cardType: ['', [Validators.required]],
    cardNumber: [
      '',
      [
        Validators.required,
        Validators.pattern(numberRegex),
        Validators.min(1),
        Validators.maxLength(16),
        Validators.minLength(16),
      ],
    ],
    cardholderName: ['', [Validators.required]],
    cardExpiration: [
      '',
      [
        Validators.required,
        Validators.pattern(numberRegex),
        Validators.min(1),
        Validators.minLength(4),
        Validators.maxLength(4),
      ],
    ],
    ccv: [
      '',
      [
        Validators.required,
        Validators.pattern(numberRegex),
        Validators.min(1),
        Validators.minLength(3),
        Validators.maxLength(3),
      ],
    ],
    isCardSaved: [false],
  });

  constructor(
    private formBuilder: FormBuilder,
    private _snackBar: SnackbarUtilService
  ) {}

  ngOnInit(): void {
    // prevent entering negative numbers from input
    // number up-down control
    this.subscribeToNumericControls([
      this.cardNumber,
      this.cardExpiration,
      this.ccv,
    ]);
  }

  get cardType() {
    return this.newCardForm.get('cardType') as FormControl;
  }

  get isCardSaved() {
    return this.newCardForm.get('isCardSaved') as FormControl;
  }

  get cardNumber() {
    return this.newCardForm.get('cardNumber') as FormControl;
  }

  get cardExpiration() {
    return this.newCardForm.get('cardExpiration') as FormControl;
  }

  get ccv() {
    return this.newCardForm.get('ccv') as FormControl;
  }

  subscribeToNumericControls(controls: FormControl[]) {
    controls.forEach((control) => {
      control.valueChanges.subscribe(() => {
        convertNegativeToPositive(control);
      });
    });
  }

  setCheckedState(event: boolean) {
    this.isCardSaved.setValue(event);
  }

  setCardType(card: CardType) {
    this.cardType.setValue(card);
  }

  registerCard() {
    if (this.newCardForm.invalid) {
      this._snackBar.displaySnackBar(
        'some fields are missing.',
        '',
        'red-snackbar'
      );
      return;
    }

    this.saveCard.emit(this.newCardForm.value);
  }
}
