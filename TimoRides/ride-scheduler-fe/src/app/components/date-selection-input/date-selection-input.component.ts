import {
  Component,
  ViewChild,
  ElementRef,
  Output,
  EventEmitter,
  Input,
} from '@angular/core';

@Component({
  selector: 'app-date-selection-input',
  templateUrl: './date-selection-input.component.html',
  styleUrls: ['./date-selection-input.component.css'],
})
export class DateSelectionInputComponent {
  @ViewChild('dateTime') dateTime!: ElementRef;
  @Output() clickedDateTime = new EventEmitter();

  datePickerValue?: string;
  private inputDateTime?: ElementRef;

  minDate: string;

  constructor() {
    // Set the minimum date to today's date
    const today = new Date();
    this.minDate = today.toISOString().substring(0, 16); // ISO 8601 format without seconds and milliseconds
  }

  ngAfterViewInit() {
    this.inputDateTime = this.dateTime;
  }

  setDateTimeValue() {
    if (this.inputDateTime) {
      this.datePickerValue = this.inputDateTime.nativeElement.value;
      this.clickedDateTime?.emit(this.datePickerValue);
    }
  }
}
