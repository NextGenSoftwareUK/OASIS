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
}
