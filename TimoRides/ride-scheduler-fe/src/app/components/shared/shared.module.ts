import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { InputGroupComponent } from '../input-group/input-group.component';
import { ReactiveFormsModule } from '@angular/forms';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { LoadingComponent } from './loading/loading.component';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TermsAndConditionComponent } from './terms-and-condition/terms-and-condition.component';
import { AccordionComponent } from './accordion/accordion.component';

@NgModule({
  declarations: [
    InputGroupComponent,
    LoadingComponent,
    TermsAndConditionComponent,
    AccordionComponent,
  ],

  imports: [
    CommonModule,
    FontAwesomeModule,
    ReactiveFormsModule,
    MatProgressSpinnerModule,
  ],
  exports: [
    InputGroupComponent,
    FontAwesomeModule,
    LoadingComponent,
    TermsAndConditionComponent,
    AccordionComponent,
  ],
})
export class SharedModule {}
