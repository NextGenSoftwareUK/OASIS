import { Injector, NgModule } from "@angular/core";
import { BrowserModule } from "@angular/platform-browser";

import { AppRoutingModule } from "./app-routing.module";
import { AppComponent } from "./app.component";
import { HeaderComponent } from "./components/header/header.component";
import { SideNavComponent } from "./components/side-nav/side-nav.component";
import { createCustomElement } from "@angular/elements";
import { ModalComponent } from "./common/modal/modal.component";
import { HomeComponent } from "./components/home/home.component";
import { LoginComponent } from "./components/login/login.component";

@NgModule({
  declarations: [
    AppComponent,
    HeaderComponent,
    SideNavComponent,
    ModalComponent,
    HomeComponent,
    LoginComponent,
  ],
  imports: [BrowserModule, AppRoutingModule],
  providers: [],
  bootstrap: [],
})
export class AppModule {
  constructor(private injector: Injector) {}
  ngDoBootstrap() {
    const c = createCustomElement(AppComponent, {
      injector: this.injector,
    });
    customElements.define("oasis-web", c);

    const h = createCustomElement(HeaderComponent, {
      injector: this.injector,
    });
    customElements.define("oasis-header", h);

    const s = createCustomElement(SideNavComponent, {
      injector: this.injector,
    });
    customElements.define("oasis-sidenav", s);
  }
}
