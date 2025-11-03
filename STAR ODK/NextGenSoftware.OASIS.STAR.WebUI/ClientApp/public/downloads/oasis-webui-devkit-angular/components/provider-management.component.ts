import { Component, Input, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-provider-management',
  template: `
    <div class="oasis-provider-mgmt" [class.oasis-provider-mgmt--dark]="theme === 'dark'">
      <h3>Storage Provider</h3>
      
      <div class="current-provider" *ngIf="showStatus && (currentProvider$ | async) as provider">
        <span class="label">Active Provider:</span>
        <span class="value">{{ provider.name }}</span>
        <span [class]="'status status--' + provider.status">{{ provider.status }}</span>
      </div>

      <div class="provider-list">
        <div *ngIf="loading">Loading providers...</div>
        <div *ngFor="let provider of providers$ | async" 
             class="provider-item"
             [class.selected]="selectedProvider === provider.id"
             (click)="selectedProvider = provider.id">
          <div class="provider-icon">
            <img [src]="provider.icon" [alt]="provider.name" />
          </div>
          <div class="provider-info">
            <h4>{{ provider.name }}</h4>
            <p>{{ provider.description }}</p>
            <div class="provider-stats">
              <span>Speed: {{ provider.speed }}</span>
              <span>Cost: {{ provider.cost }}</span>
              <span>Reliability: {{ provider.reliability }}%</span>
            </div>
          </div>
          <div *ngIf="provider.id === (currentProvider$ | async)?.id" class="active-badge">
            Active
          </div>
        </div>
      </div>

      <button *ngIf="selectedProvider && selectedProvider !== (currentProvider$ | async)?.id"
              class="switch-btn"
              (click)="handleSwitch()"
              [disabled]="switching">
        {{ switching ? 'Switching...' : 'Switch Provider' }}
      </button>
    </div>
  `,
  styles: [`
    .provider-item {
      display: flex;
      gap: 1rem;
      padding: 1rem;
      border: 1px solid #ddd;
      border-radius: 8px;
      margin-bottom: 1rem;
      cursor: pointer;
    }
    .provider-item.selected { border-color: #00bcd4; }
  `]
})
export class OasisProviderManagementComponent implements OnInit {
  @Input() theme: 'light' | 'dark' = 'dark';
  @Input() showStatus = true;

  providers$!: Observable<any[]>;
  currentProvider$!: Observable<any>;
  selectedProvider = '';
  loading = false;
  switching = false;

  constructor(private oasisService: OASISService) {}

  ngOnInit() {
    this.providers$ = this.oasisService.getAvailableProviders();
    this.currentProvider$ = this.oasisService.getCurrentProvider();
  }

  async handleSwitch() {
    this.switching = true;
    try {
      await this.oasisService.switchProvider(this.selectedProvider);
      this.currentProvider$ = this.oasisService.getCurrentProvider();
    } finally {
      this.switching = false;
    }
  }
}

