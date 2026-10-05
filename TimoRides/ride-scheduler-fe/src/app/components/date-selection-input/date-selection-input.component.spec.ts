import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DateSelectionInputComponent } from './date-selection-input.component';

describe('DateSelectionInputComponent', () => {
  let component: DateSelectionInputComponent;
  let fixture: ComponentFixture<DateSelectionInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ DateSelectionInputComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DateSelectionInputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
