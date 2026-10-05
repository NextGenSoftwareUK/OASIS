import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CarListComponent } from './components/ride-scheduler/car-list/car-list.component';
import { CarDetailsComponent } from './components/ride-scheduler/car-details/car-details.component';
import { EntryComponent } from './components/entry/entry.component';
import { PaymentComponent } from './components/payment/payment-page/payment.component';
import { PaymentCompleteComponent } from './components/payment/payment-complete/payment-complete.component';
import { DriverComponent } from './components/dashboards/driver/driver.component';
import { SignUpComponent } from './components/sign-up/sign-up.component';
import { LoginComponent } from './components/login/login.component';
import { EmailVerificationComponent } from './components/email-verification/email-verification.component';
import { UserProfileComponent } from './components/dashboards/user/profile/user-profile.component';
import { DriverProfileComponent } from './components/dashboards/driver/profile/driver-profile.component';
import { AuthGuard } from './guards/auth.guard';
import { AdminDashboardComponent } from './components/dashboards/admin/admin-dashboard/admin-dashboard.component';
import { RoleGuard } from './guards/role.guard';
import { TermsAndConditionComponent } from './components/shared/terms-and-condition/terms-and-condition.component';
import { FaqComponent } from './components/faq/faq.component';

const routes: Routes = [
  {
    path: 'ride-schedule',
    component: EntryComponent,
  },
  {
    path: '',
    redirectTo: 'ride-schedule',
    pathMatch: 'full',
  },
  {
    path: 'ride-schedule/cars',
    component: CarListComponent,
  },
  {
    path: 'ride-schedule/cars/:id',
    component: CarDetailsComponent,
  },
  {
    path: 'ride-schedule/pay',
    component: PaymentComponent,
  },
  {
    path: 'ride-schedule/complete',
    component: PaymentCompleteComponent,
  },
  {
    path: 'dashboards',
    children: [
      {
        path: '',
        redirectTo: 'driver',
        pathMatch: 'full',
      },
      {
        path: 'driver',
        component: DriverComponent,
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['driver'] },
      },
      {
        path: 'user',
        component: DriverComponent,
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['user'] },
      },
      {
        path: 'admin',
        component: AdminDashboardComponent,
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['admin'] },
      },
    ],
  },
  {
    path: 'signup',
    component: SignUpComponent,
  },
  {
    path: 'login',
    component: LoginComponent,
  },
  {
    path: 'verify',
    component: EmailVerificationComponent,
  },
  {
    path: 'accept-ride',
    component: EmailVerificationComponent,
  },
  {
    path: 'users',
    children: [
      {
        path: '',
        redirectTo: 'users',
        pathMatch: 'full',
      },
      {
        path: ':id',
        component: UserProfileComponent,
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['user', 'admin'] },
      },
    ],
  },
  {
    path: 'drivers',
    children: [
      {
        path: '',
        redirectTo: 'drivers',
        pathMatch: 'full',
      },
      {
        path: ':id',
        component: DriverProfileComponent,
        canActivate: [AuthGuard, RoleGuard],
        data: { roles: ['driver'] },
      },
    ],
  },
  { path: 'terms-and-conditions', component: TermsAndConditionComponent },
  { path: 'faq', component: FaqComponent },
  { path: '**', redirectTo: '/ride-schedule' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
