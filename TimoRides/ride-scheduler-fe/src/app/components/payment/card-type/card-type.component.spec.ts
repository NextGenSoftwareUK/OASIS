import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CardTypeComponent } from './card-type.component';

describe('CardTypeComponent', () => {
  let component: CardTypeComponent;
  let fixture: ComponentFixture<CardTypeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CardTypeComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CardTypeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
