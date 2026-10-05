import { Component, Input } from '@angular/core';
import {
  trigger,
  state,
  style,
  animate,
  transition,
} from '@angular/animations';

@Component({
  selector: 'app-accordion',
  templateUrl: './accordion.component.html',
  styleUrls: ['./accordion.component.css'],
  animations: [
    trigger('accordionAnimation', [
      state(
        'collapsed',
        style({
          height: '0',
          opacity: 0,
          transform: 'scaleY(0.9)',
        })
      ),
      state(
        'expanded',
        style({
          height: '*',
          opacity: 1,
          transform: 'scaleY(1)',
        })
      ),
      transition('collapsed <=> expanded', [
        animate('400ms cubic-bezier(0.86, 0, 0.07, 1)'),
      ]),
    ]),
  ],
})
export class AccordionComponent {
  @Input() faqs: { question: string; answer: string; open?: boolean }[] = [];

  toggle(index: number) {
    this.faqs[index].open = !this.faqs[index].open;
  }

  getState(faq: { open?: boolean }) {
    return faq.open ? 'expanded' : 'collapsed';
  }

  formatAnswer(answer: string): string[] {
    return answer.split('\n').filter((item) => item.trim() !== '');
  }
}
