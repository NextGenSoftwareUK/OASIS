import { Component, EventEmitter, Input, OnInit, Output } from "@angular/core";

@Component({
  selector: "app-login",
  templateUrl: "./login.component.html",
  styleUrls: ["./login.component.scss"],
})
export class LoginComponent implements OnInit {
  constructor() {}
  @Input() view: string = "login";
  ngOnInit(): void {}
  setView(view: string) {
    this.view = view;
  }

  @Input() visible: boolean = false;
  @Output() visibleChange: EventEmitter<boolean> = new EventEmitter<boolean>();

  closeModal(event: any) {
    var x = event.target as HTMLElement;
    if (x.id == "modal") this.visible = false;
    this.visibleChange.emit(this.visible);
  }
}
export enum LoginTypes {
  login,
  signup,
  forgot,
  reset,
}
