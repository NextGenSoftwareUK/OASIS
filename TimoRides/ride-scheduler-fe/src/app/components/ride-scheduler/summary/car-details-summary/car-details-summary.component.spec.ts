import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CarDetailsSummaryComponent } from './car-details-summary.component';

describe('CarDetailsSummaryComponent', () => {
  let component: CarDetailsSummaryComponent;
  let fixture: ComponentFixture<CarDetailsSummaryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CarDetailsSummaryComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CarDetailsSummaryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
