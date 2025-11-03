import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RideFormComponent } from './ride-form.component';

describe('RideFormComponent', () => {
  let component: RideFormComponent;
  let fixture: ComponentFixture<RideFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [RideFormComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(RideFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
