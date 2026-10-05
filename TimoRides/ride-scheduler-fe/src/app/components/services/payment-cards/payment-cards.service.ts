import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { CardType, ICardType } from 'src/app/models/payment-card';

@Injectable({
  providedIn: 'root',
})
export class PaymentCardsService {
  private paymentCardsURL: string = 'data/card-types.json';

  constructor(private http: HttpClient) {}

  getPaymentCardTypes(): Observable<CardType[]> {
    return this.http.get(this.paymentCardsURL).pipe(
      tap((resp: any) => resp),
      map((res: ICardType) => res['cardTypes']),
      tap((items) => items)
    );
  }
}
