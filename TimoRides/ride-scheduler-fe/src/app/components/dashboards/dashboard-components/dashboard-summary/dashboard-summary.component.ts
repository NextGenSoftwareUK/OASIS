import { Component, Input } from '@angular/core';
import { DashBoardCardInfo } from 'src/app/models/dashboard-card';

@Component({
  selector: 'app-dashboard-summary',
  templateUrl: './dashboard-summary.component.html',
  styleUrls: ['./dashboard-summary.component.css'],
})
export class DashboardSummaryComponent {
  @Input('summaryInfo') summaryInfo: DashBoardCardInfo | undefined;
}
