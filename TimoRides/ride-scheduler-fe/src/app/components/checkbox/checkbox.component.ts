import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CheckBox } from 'src/app/models/checkbox';

@Component({
  selector: 'app-checkbox',
  templateUrl: './checkbox.component.html',
  styleUrls: ['./checkbox.component.css'],
})
export class CheckboxComponent {
  @Input('inputConfig') config!: CheckBox;
  @Output() onChecked: EventEmitter<boolean> = new EventEmitter();
  isClicked: boolean = false;

  toggleClick() {
    this.isClicked = !this.isClicked;
    this.onChecked.emit(this.isClicked);
  }
}
