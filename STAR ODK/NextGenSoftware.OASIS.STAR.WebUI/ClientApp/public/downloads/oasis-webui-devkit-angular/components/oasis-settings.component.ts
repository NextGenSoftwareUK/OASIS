import { Component, Input } from '@angular/core';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-settings',
  template: `
    <div class="oasis-settings" [class.oasis-settings--dark]="theme === 'dark'">
      <div class="settings-sidebar">
        <button *ngFor="let section of sections"
                [class.active]="activeSection === section"
                (click)="activeSection = section">
          {{ section | titlecase }}
        </button>
      </div>

      <div class="settings-content">
        <h3>{{ activeSection | titlecase }} Settings</h3>
        
        <div *ngIf="activeSection === 'general'" class="settings-group">
          <label>
            Language:
            <select [(ngModel)]="settings.general.language">
              <option value="en">English</option>
              <option value="es">Español</option>
              <option value="fr">Français</option>
            </select>
          </label>
          <label>
            Timezone:
            <select [(ngModel)]="settings.general.timezone">
              <option value="UTC">UTC</option>
              <option value="EST">EST</option>
              <option value="PST">PST</option>
            </select>
          </label>
        </div>

        <div *ngIf="activeSection === 'privacy'" class="settings-group">
          <label>
            <input type="checkbox" [(ngModel)]="settings.privacy.showEmail" />
            Show Email Publicly
          </label>
          <label>
            <input type="checkbox" [(ngModel)]="settings.privacy.showKarma" />
            Show Karma Score
          </label>
        </div>

        <div *ngIf="activeSection === 'notifications'" class="settings-group">
          <label>
            <input type="checkbox" [(ngModel)]="settings.notifications.emailNotifications" />
            Email Notifications
          </label>
          <label>
            <input type="checkbox" [(ngModel)]="settings.notifications.pushNotifications" />
            Push Notifications
          </label>
        </div>

        <div *ngIf="activeSection === 'advanced'" class="settings-group">
          <label>
            <input type="checkbox" [(ngModel)]="settings.advanced.developerMode" />
            Developer Mode
          </label>
          <label>
            <input type="checkbox" [(ngModel)]="settings.advanced.experimentalFeatures" />
            Experimental Features
          </label>
        </div>

        <button class="save-btn" (click)="handleSave()" [disabled]="saving">
          {{ saving ? 'Saving...' : 'Save Settings' }}
        </button>
      </div>
    </div>
  `,
  styles: [`
    .oasis-settings { display: flex; gap: 2rem; }
    .settings-sidebar { min-width: 200px; }
    .settings-content { flex: 1; }
    .settings-group { display: flex; flex-direction: column; gap: 1rem; }
  `]
})
export class OasisSettingsComponent {
  @Input() avatarId!: string;
  @Input() theme: 'light' | 'dark' = 'dark';
  @Input() sections = ['general', 'privacy', 'notifications', 'advanced'];

  activeSection = this.sections[0];
  saving = false;
  settings: any = {
    general: { language: 'en', timezone: 'UTC', dateFormat: 'MM/DD/YYYY' },
    privacy: { profileVisibility: 'public', showEmail: false, showKarma: true },
    notifications: { emailNotifications: true, pushNotifications: false, chatNotifications: true },
    advanced: { developerMode: false, debugLogs: false, experimentalFeatures: false }
  };

  constructor(private oasisService: OASISService) {}

  async handleSave() {
    this.saving = true;
    try {
      await this.oasisService.updateSettings(this.avatarId, this.settings);
    } finally {
      this.saving = false;
    }
  }
}

