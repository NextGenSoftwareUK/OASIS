import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Star } from 'src/app/models/rating';

@Component({
  selector: 'app-star',
  templateUrl: './star.component.html',
  styleUrls: ['./star.component.css'],
})
export class StarComponent {
  @Input('star') star!: Star;
  @Input('clickable') clickable?: boolean = false;
  @Input('index') index: number = 0;
  @Output('starClick') starClick: EventEmitter<number> =
    new EventEmitter<number>();

  emitStar(event: Event) {
    event.stopPropagation();

    // Emit the index of the clicked star to the parent component
    this.starClick.emit(this.index);
  }
}
