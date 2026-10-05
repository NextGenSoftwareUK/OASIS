import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DriverDetailsSummaryComponent } from './driver-details-summary.component';

describe('DriverDetailsSummaryComponent', () => {
  let component: DriverDetailsSummaryComponent;
  let fixture: ComponentFixture<DriverDetailsSummaryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ DriverDetailsSummaryComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DriverDetailsSummaryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
