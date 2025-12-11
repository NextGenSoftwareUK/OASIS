import {
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild,
} from '@angular/core';

@Component({
  selector: 'app-text-input',
  templateUrl: './text-input.component.html',
  styleUrls: ['./text-input.component.css'],
})
export class TextInputComponent {
  @Input() placeholder?: string;
  @Input() type?: string;
  @Input() name?: string;

  @Output() clicked = new EventEmitter();

  @ViewChild('inputTextValue') inputTextValue!: ElementRef;
  inputText?: ElementRef;

  ngAfterViewInit() {
    this.inputText = this.inputTextValue;
  }

  setInputState(inputValue: Event) {
    const targetValue = inputValue.target as HTMLInputElement;
    this.clicked.emit(targetValue.value);
  }
}
