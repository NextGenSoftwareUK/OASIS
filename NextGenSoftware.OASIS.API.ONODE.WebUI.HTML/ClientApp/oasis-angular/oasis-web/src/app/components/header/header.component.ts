import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss'],
})
export class HeaderComponent implements OnInit {
  constructor() {}
  isMenuVisible: boolean = false;
  ngOnInit(): void {}
  toggleMenu() {
    this.isMenuVisible = !this.isMenuVisible;
  }
}
