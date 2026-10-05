import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ButtonService {
  constructor() {}

  private buttonClickEvent = new BehaviorSubject<string>('');

  emitButtonEvent(value: string) {
    this.buttonClickEvent.next(value);
  }

  buttonEventListener() {
    return this.buttonClickEvent.asObservable();
  }

  resetValue() {
    this.emitButtonEvent('');
  }
}
