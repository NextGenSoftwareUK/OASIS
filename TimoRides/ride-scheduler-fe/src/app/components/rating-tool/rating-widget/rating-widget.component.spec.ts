import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RatingWidgetComponent } from './rating-widget.component';

describe('RatingWidgetComponent', () => {
  let component: RatingWidgetComponent;
  let fixture: ComponentFixture<RatingWidgetComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ RatingWidgetComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RatingWidgetComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
