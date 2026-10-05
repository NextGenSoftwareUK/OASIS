import { Component, Input, OnInit } from '@angular/core';
import { maximumStarRating } from 'src/app/models/rating';

@Component({
  selector: 'app-rating-widget',
  templateUrl: './rating-widget.component.html',
  styleUrls: ['./rating-widget.component.css'],
})
export class RatingWidgetComponent implements OnInit {
  @Input('rated') rated: number | undefined = 2;
  @Input('isEditable') isEditable?: boolean = false;
  maxRating = maximumStarRating;
  carRating!: number[];

  ngOnInit() {
    this.carRating = Array(this.maxRating).fill(0);
  }

  clickStar(index: number) {
    // Toggle star state based on the clicked index
    for (let i = 0; i < this.carRating.length; i++) {
      if (i <= index) {
        // If index is less than or equal to clicked index, set to 1 (filled)
        this.carRating[i] = 1;
      } else {
        // If index is greater than clicked index, set to 0 (no-fill)
        this.carRating[i] = 0;
      }
    }

    // Update the rating based on the number of filled stars
    this.rated = this.carRating.filter((value) => value === 1).length;
  }
}
