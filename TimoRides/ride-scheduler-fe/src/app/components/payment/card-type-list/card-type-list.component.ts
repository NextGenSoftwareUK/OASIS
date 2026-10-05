import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { CardType } from 'src/app/models/payment-card';
import { PaymentCardsService } from '../../services/payment-cards/payment-cards.service';

@Component({
  selector: 'app-card-type-list',
  templateUrl: './card-type-list.component.html',
  styleUrls: ['./card-type-list.component.css'],
})
export class CardTypeListComponent implements OnInit {
  @Output() onPaymentCardSelect: EventEmitter<CardType> = new EventEmitter();
  paymentCards!: CardType[];
  selectedCard!: CardType;

  constructor(private cService: PaymentCardsService) {}

  ngOnInit(): void {
    // get list of card types
    this.cService.getPaymentCardTypes().subscribe((data) => {
      this.paymentCards = data;
    });
  }

  saveCardType(card: CardType) {
    // Set selected payment card and emit it.
    this.selectedCard = card;
    this.onPaymentCardSelect.emit(this.selectedCard);
  }
}
