import { Component, Input } from '@angular/core';
import { Profile } from 'src/app/models/user.model';

@Component({
  selector: 'app-driver-details-summary',
  templateUrl: './driver-details-summary.component.html',
  styleUrls: ['./driver-details-summary.component.css'],
})
export class DriverDetailsSummaryComponent {
  @Input('driverDetails') driverDetails!: Profile | null;
  @Input('hasPaid') hasPaid: boolean = false;
}
