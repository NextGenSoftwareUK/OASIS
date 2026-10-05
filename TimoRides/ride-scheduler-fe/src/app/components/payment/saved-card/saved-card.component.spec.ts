import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SavedCardComponent } from './saved-card.component';

describe('SavedCardComponent', () => {
  let component: SavedCardComponent;
  let fixture: ComponentFixture<SavedCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [SavedCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SavedCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
