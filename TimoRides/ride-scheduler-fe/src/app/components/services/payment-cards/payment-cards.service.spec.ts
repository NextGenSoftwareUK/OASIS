import { TestBed } from '@angular/core/testing';

import { PaymentCardsService } from './payment-cards.service';

describe('PaymentCardsService', () => {
  let service: PaymentCardsService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(PaymentCardsService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
