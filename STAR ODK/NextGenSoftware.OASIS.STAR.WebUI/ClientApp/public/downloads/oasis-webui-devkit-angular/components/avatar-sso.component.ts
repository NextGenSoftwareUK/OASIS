import { Component, Input, Output, EventEmitter } from '@angular/core';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-avatar-sso',
  template: `
    <div class="oasis-sso" [class.oasis-sso--dark]="theme === 'dark'">
      <button 
        class="oasis-sso__trigger"
        (click)="isOpen = true"
      >
        Sign In with OASIS
      </button>

      <div class="oasis-sso__modal" *ngIf="isOpen">
        <div class="oasis-sso__header">
          <h2>Sign In to OASIS</h2>
          <button (click)="isOpen = false">×</button>
        </div>

        <div class="oasis-sso__providers">
          <button
            *ngFor="let provider of providers"
            class="oasis-sso__provider-btn"
            (click)="handleProviderLogin(provider)"
            [disabled]="loading === provider"
          >
            {{ loading === provider ? 'Connecting...' : 'Connect with ' + provider }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .oasis-sso__modal {
      position: fixed;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      background: white;
      padding: 2rem;
      border-radius: 12px;
      box-shadow: 0 10px 40px rgba(0,0,0,0.3);
      z-index: 1000;
    }
    .oasis-sso--dark .oasis-sso__modal {
      background: #1a1a1a;
      color: white;
    }
  `]
})
export class OasisAvatarSSOComponent {
  @Input() providers: string[] = ['holochain', 'ethereum', 'solana', 'polygon'];
  @Input() theme: 'light' | 'dark' = 'dark';
  @Output() onSuccess = new EventEmitter<any>();
  @Output() onError = new EventEmitter<Error>();

  isOpen = false;
  loading: string | null = null;

  constructor(private oasisService: OASISService) {}

  async handleProviderLogin(provider: string) {
    this.loading = provider;
    try {
      const result = await this.oasisService.authenticateWithProvider(provider);
      this.onSuccess.emit(result);
      this.isOpen = false;
    } catch (error) {
      this.onError.emit(error as Error);
    } finally {
      this.loading = null;
    }
  }
}

