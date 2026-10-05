import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CardType } from 'src/app/models/payment-card';

@Component({
  selector: 'app-card-type',
  templateUrl: './card-type.component.html',
  styleUrls: ['./card-type.component.css'],
})
export class CardTypeComponent {
  @Input('paymentCard') paymentCard!: CardType;
  @Input('isSelected') isSelected!: boolean;
  @Output() onCardSelect: EventEmitter<CardType> = new EventEmitter();

  selectCard(card: CardType) {
    this.onCardSelect.emit(card);
  }
}
