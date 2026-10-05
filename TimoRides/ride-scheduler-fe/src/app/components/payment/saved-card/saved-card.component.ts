import { Component, EventEmitter, Input, Output } from '@angular/core';
import { PaymentCard } from 'src/app/models/payment-card';

@Component({
  selector: 'app-saved-card',
  templateUrl: './saved-card.component.html',
  styleUrls: ['./saved-card.component.css'],
})
export class SavedCardComponent {
  @Input('cardInfo') cardInfo!: PaymentCard;
  @Output('delete') delete: EventEmitter<PaymentCard> = new EventEmitter();

  deleteCard() {
    this.delete.emit(this.cardInfo);
  }
}
