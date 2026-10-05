import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'oasis-avatar-card',
  template: `
    <div class="oasis-avatar-card" 
         [class.oasis-avatar-card--dark]="theme === 'dark'"
         (click)="handleClick()">
      <div class="avatar-card__image">
        <img [src]="avatar.image || '/default-avatar.png'" [alt]="avatar.username" />
      </div>
      <div class="avatar-card__info">
        <h4 class="username">{{ avatar.username }}</h4>
        <div class="stat" *ngIf="showKarma">
          <span class="label">Karma:</span>
          <span class="value">{{ avatar.karma || 0 }}</span>
        </div>
        <div class="stat" *ngIf="showLevel">
          <span class="label">Level:</span>
          <span class="value">{{ avatar.level || 1 }}</span>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .oasis-avatar-card {
      padding: 1rem;
      border: 1px solid #ddd;
      border-radius: 8px;
      cursor: pointer;
      transition: transform 0.2s;
    }
    .oasis-avatar-card:hover { transform: translateY(-2px); }
    .avatar-card__image img { width: 60px; height: 60px; border-radius: 50%; }
  `]
})
export class OasisAvatarCardComponent {
  @Input() avatar: any;
  @Input() theme: 'light' | 'dark' = 'dark';
  @Input() showKarma = true;
  @Input() showLevel = true;
  @Output() onClick = new EventEmitter<any>();

  handleClick() {
    this.onClick.emit(this.avatar);
  }
}

