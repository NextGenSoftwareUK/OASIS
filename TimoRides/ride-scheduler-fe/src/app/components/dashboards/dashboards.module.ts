import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CustomerDetailsComponent } from './admin/customer-details/customer-details.component';
import { DriverDetailsComponent } from './admin/driver-details/driver-details.component';
import { BookedTripsComponent } from './admin/booked-trips/booked-trips.component';
import { AdminDashboardComponent } from './admin/admin-dashboard/admin-dashboard.component';
import { DashboardSummaryComponent } from './dashboard-components/dashboard-summary/dashboard-summary.component';
import { TableComponent } from './dashboard-components/table/table.component';
import { RideDetailsComponent } from './dashboard-components/ride-details/ride-details.component';
import { ButtonComponent } from '../button/button.component';
import { SharedModule } from '../shared/shared.module';

@NgModule({
  declarations: [
    TableComponent,
    RideDetailsComponent,
    ButtonComponent,
    AdminDashboardComponent,
    CustomerDetailsComponent,
    DriverDetailsComponent,
    BookedTripsComponent,
    DashboardSummaryComponent,
  ],
  imports: [CommonModule, SharedModule],
  exports: [
    TableComponent,
    DashboardSummaryComponent,
    ButtonComponent,
    RideDetailsComponent,
  ],
})
export class DashboardsModule {}
