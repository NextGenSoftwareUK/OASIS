import { DatePipe } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import { BookingRequest } from 'src/app/models/booking-form';
import { orderItem } from 'src/app/utils/orderItem';

@Component({
  selector: 'app-ride-summary',
  templateUrl: './ride-summary.component.html',
  styleUrls: ['./ride-summary.component.css'],
})
export class RideSummaryComponent implements OnInit {
  @Input('rideDetails') rideDetails!: BookingRequest | undefined;
  orderItem = orderItem;
  datePipe: DatePipe;

  constructor() {
    this.datePipe = new DatePipe('en-US');
  }

  ngOnInit(): void {
    //
  }
}
