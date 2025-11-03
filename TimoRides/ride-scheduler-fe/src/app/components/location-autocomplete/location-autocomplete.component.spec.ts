import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LocationAutocompleteComponent } from './location-autocomplete.component';

describe('LocationAutocompleteComponent', () => {
  let component: LocationAutocompleteComponent;
  let fixture: ComponentFixture<LocationAutocompleteComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LocationAutocompleteComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LocationAutocompleteComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
