import { Injectable } from '@angular/core';
import { LoadingComponent } from './loading.component';

@Injectable({
  providedIn: 'root',
})
export class LoadingService {
  private loadingComponent!: LoadingComponent;

  registerLoadingComponent(loadingComponent: LoadingComponent) {
    this.loadingComponent = loadingComponent;
  }

  show() {
    if (this.loadingComponent) {
      this.loadingComponent.show();
    }
  }

  hide() {
    if (this.loadingComponent) {
      this.loadingComponent.hide();
    }
  }
}
