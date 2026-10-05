import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BookedTripsComponent } from './booked-trips.component';

describe('BookedTripsComponent', () => {
  let component: BookedTripsComponent;
  let fixture: ComponentFixture<BookedTripsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BookedTripsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BookedTripsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
