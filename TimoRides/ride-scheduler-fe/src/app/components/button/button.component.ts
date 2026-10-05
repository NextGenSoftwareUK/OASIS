import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ButtonService } from './button.service';
import { IButtonConfig } from '../../models/button';

@Component({
  selector: 'app-button',
  templateUrl: './button.component.html',
  styleUrls: ['./button.component.css'],
})
export class ButtonComponent {
  @Input('btnConfig') btnConfig: IButtonConfig | undefined;
  @Output() click: EventEmitter<string> = new EventEmitter();

  buttonWidth = () => {
    return this.btnConfig?.block ? 'w-full' : 'w-fit';
  };

  constructor(private btnServie: ButtonService) {}

  triggerEmit(event: Event, text: string) {
    event.stopPropagation();
    this.btnServie.emitButtonEvent(text);
    this.click.emit(text);
  }
}
