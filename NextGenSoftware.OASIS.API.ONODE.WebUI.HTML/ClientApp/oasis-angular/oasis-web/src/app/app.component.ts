import { Component } from "@angular/core";
import { LoginTypes } from "./components/login/login.component";

@Component({
  selector: "oasis-web",
  templateUrl: "./app.component.html",
  styleUrls: ["./app.component.scss"],
})
export class AppComponent {
  title = "oasis-web";
  showMenu: boolean = false;
  private _clickedWhere: string = "";
  public get clickedWhere(): string {
    return this._clickedWhere;
  }
  public set clickedWhere(value: string) {
    this._clickedWhere = value;
    this.showLoginPopup(value);
  }
  isVisibleLP: boolean = false;
  showLoginPopup(val: string) {
    if (Object.values(LoginTypes).includes(val)) {
      this.isVisibleLP = true;
    } else {
      console.error("Incorrect login type");
    }
  }
}
