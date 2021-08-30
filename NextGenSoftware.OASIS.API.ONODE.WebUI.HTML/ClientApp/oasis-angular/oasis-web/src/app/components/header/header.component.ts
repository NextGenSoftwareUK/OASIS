import { Component, EventEmitter, Input, OnInit, Output } from "@angular/core";

@Component({
  selector: "app-header",
  templateUrl: "./header.component.html",
  styleUrls: ["./header.component.scss"],
})
export class HeaderComponent implements OnInit {
  constructor() {}
  @Input() menuVisible: boolean = false;

  @Output() menuVisibleChange: EventEmitter<boolean> =
    new EventEmitter<boolean>();

  @Input() clickedWhere: string = "";

  @Output() clickedWhereChange: EventEmitter<string> =
    new EventEmitter<string>();

  menuBtnClass = {
    "is-clicked": false,
  };
  ngOnInit(): void {}
  toggleMenu() {
    this.clickedWhere = "navbar-toggle";

    this.menuVisible = !this.menuVisible;
    this.menuBtnClass["is-clicked"] = this.menuVisible;
    this.menuVisibleChange.emit(this.menuVisible);
  }
  loginBtnClick() {
    this.clickedWhere = "login";
    this.clickedWhereChange.emit(this.clickedWhere);
  }
  signupBtnClick() {
    this.clickedWhere = "signup";
    this.clickedWhereChange.emit(this.clickedWhere);
  }
}
