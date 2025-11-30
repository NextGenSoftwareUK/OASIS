import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UserRideFormComponent } from './user-form.component';

describe('UserRideFormComponent', () => {
  let component: UserRideFormComponent;
  let fixture: ComponentFixture<UserRideFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [UserRideFormComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(UserRideFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
