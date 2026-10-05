import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RideSummaryComponent } from './ride-summary.component';

describe('RideSummaryComponent', () => {
  let component: RideSummaryComponent;
  let fixture: ComponentFixture<RideSummaryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ RideSummaryComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RideSummaryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
