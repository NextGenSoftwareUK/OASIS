import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-avatar-detail',
  template: `
    <div class="oasis-avatar-detail" [class.oasis-avatar-detail--dark]="theme === 'dark'">
      <div class="avatar-header" *ngIf="avatar$ | async as avatar">
        <img [src]="avatar.image || '/default-avatar.png'" [alt]="avatar.username" class="avatar-image" />
        <div class="avatar-info">
          <h2>{{ avatar.username }}</h2>
          <p class="email">{{ avatar.email }}</p>
        </div>
        <button *ngIf="editable" (click)="toggleEdit()">
          {{ isEditing ? 'Cancel' : 'Edit' }}
        </button>
      </div>

      <div class="avatar-details" *ngIf="avatar$ | async as avatar">
        <div *ngIf="!isEditing">
          <div class="detail-row">
            <span class="label">Full Name:</span>
            <span class="value">{{ avatar.firstName }} {{ avatar.lastName }}</span>
          </div>
          <div class="detail-row">
            <span class="label">Bio:</span>
            <span class="value">{{ avatar.bio || 'No bio provided' }}</span>
          </div>
          <div class="detail-row">
            <span class="label">Member Since:</span>
            <span class="value">{{ avatar.createdDate | date }}</span>
          </div>
          <div class="detail-row">
            <span class="label">Karma:</span>
            <span class="value karma">{{ avatar.karma || 0 }}</span>
          </div>
          <div class="detail-row">
            <span class="label">Level:</span>
            <span class="value">{{ avatar.level || 1 }}</span>
          </div>
        </div>

        <form *ngIf="isEditing" (ngSubmit)="handleSave()">
          <input type="text" placeholder="First Name" [(ngModel)]="formData.firstName" name="firstName" />
          <input type="text" placeholder="Last Name" [(ngModel)]="formData.lastName" name="lastName" />
          <textarea placeholder="Bio" [(ngModel)]="formData.bio" name="bio"></textarea>
          <button type="submit" [disabled]="loading">
            {{ loading ? 'Saving...' : 'Save Changes' }}
          </button>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .oasis-avatar-detail { padding: 1.5rem; }
    .avatar-header { display: flex; align-items: center; gap: 1rem; margin-bottom: 1.5rem; }
    .avatar-image { width: 80px; height: 80px; border-radius: 50%; }
    .detail-row { display: flex; justify-content: space-between; padding: 0.5rem 0; }
  `]
})
export class OasisAvatarDetailComponent implements OnInit {
  @Input() avatarId!: string;
  @Input() theme: 'light' | 'dark' = 'dark';
  @Input() editable = true;
  @Output() onUpdate = new EventEmitter<any>();

  avatar$: any;
  isEditing = false;
  loading = false;
  formData: any = {};

  constructor(private oasisService: OASISService) {}

  ngOnInit() {
    this.avatar$ = this.oasisService.getAvatarDetail(this.avatarId);
  }

  toggleEdit() {
    this.isEditing = !this.isEditing;
  }

  async handleSave() {
    this.loading = true;
    try {
      const result = await this.oasisService.updateAvatar(this.avatarId, this.formData);
      this.onUpdate.emit(result);
      this.isEditing = false;
    } finally {
      this.loading = false;
    }
  }
}

