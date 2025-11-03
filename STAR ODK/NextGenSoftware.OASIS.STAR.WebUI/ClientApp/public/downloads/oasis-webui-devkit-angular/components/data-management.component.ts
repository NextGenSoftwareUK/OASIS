import { Component, Input, OnInit } from '@angular/core';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-data-management',
  template: `
    <div class="oasis-data-mgmt" [class.oasis-data-mgmt--dark]="theme === 'dark'">
      <div class="data-header">
        <h3>Data Management</h3>
        <div class="data-actions">
          <button *ngIf="enableExport" (click)="handleExport()">📥 Export</button>
          <button *ngIf="enableImport">📤 Import</button>
        </div>
      </div>

      <div class="data-create">
        <input type="text" placeholder="Key" [(ngModel)]="newKey" />
        <textarea placeholder="Value (JSON)" [(ngModel)]="newValue" rows="3"></textarea>
        <button (click)="handleSave()" [disabled]="saving">
          {{ saving ? 'Saving...' : 'Save Data' }}
        </button>
      </div>

      <div class="data-list">
        <div *ngIf="loading">Loading data...</div>
        <div *ngFor="let item of dataItems | keyvalue" class="data-item">
          <div class="data-key">{{ item.key }}</div>
          <div class="data-value">
            <pre>{{ item.value | json }}</pre>
          </div>
          <button class="delete-btn" (click)="handleDelete(item.key)">🗑️</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .data-item {
      display: flex;
      gap: 1rem;
      padding: 1rem;
      border: 1px solid #ddd;
      border-radius: 8px;
      margin-bottom: 1rem;
    }
    .data-value pre { margin: 0; }
  `]
})
export class OasisDataManagementComponent implements OnInit {
  @Input() avatarId!: string;
  @Input() theme: 'light' | 'dark' = 'dark';
  @Input() enableExport = true;
  @Input() enableImport = true;

  dataItems: any = {};
  newKey = '';
  newValue = '';
  loading = false;
  saving = false;

  constructor(private oasisService: OASISService) {}

  async ngOnInit() {
    this.loading = true;
    this.dataItems = await this.oasisService.getAvatarData(this.avatarId);
    this.loading = false;
  }

  async handleSave() {
    if (!this.newKey || !this.newValue) return;
    
    this.saving = true;
    try {
      await this.oasisService.saveData(this.avatarId, this.newKey, this.newValue);
      this.dataItems[this.newKey] = JSON.parse(this.newValue);
      this.newKey = '';
      this.newValue = '';
    } finally {
      this.saving = false;
    }
  }

  async handleDelete(key: string) {
    await this.oasisService.deleteData(this.avatarId, key);
    delete this.dataItems[key];
  }

  handleExport() {
    const dataStr = JSON.stringify(this.dataItems, null, 2);
    const blob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `oasis-data-${this.avatarId}.json`;
    link.click();
  }
}

