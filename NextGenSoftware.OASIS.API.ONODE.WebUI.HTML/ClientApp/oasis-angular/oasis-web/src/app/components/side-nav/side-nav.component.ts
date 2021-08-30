import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-side-nav',
  templateUrl: './side-nav.component.html',
  styleUrls: ['./side-nav.component.scss'],
})
export class SideNavComponent implements OnInit {
  constructor() {}
  stylesObj: any = { 'is-visible': false };
  @Input() visible: boolean = false;
  @Output() visibleChange: EventEmitter<boolean> = new EventEmitter<boolean>();
  ngOnInit(): void {}
  // toggleMenu() {
  //   this.visible = !this.visible;
  //   this.stylesObj['is-visible'] = this.visible;
  //   this.visibleChange.emit(this.visible);
  // }
  menuItemClick(event: any) {
    var elem = event.target as HTMLElement;
    if (elem.classList.contains('submenu-open')) {
      elem.classList.remove('submenu-open');
      elem.nextElementSibling?.classList.remove('show');
    } else {
      elem.classList.add('submenu-open');
      elem.nextElementSibling?.classList.add('show');
    }
  }
}
