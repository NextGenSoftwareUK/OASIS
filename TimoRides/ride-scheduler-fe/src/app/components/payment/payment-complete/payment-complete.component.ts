import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-payment-complete',
  templateUrl: './payment-complete.component.html',
  styleUrls: ['./payment-complete.component.css'],
})
export class PaymentCompleteComponent implements OnInit {
  bookingID!: string;
  constructor(private activatedRoute: ActivatedRoute) {}

  ngOnInit(): void {
    const params = this.activatedRoute.snapshot.queryParams;
    if ('id' in params) {
      this.bookingID = params['id'];
    }
  }
}
