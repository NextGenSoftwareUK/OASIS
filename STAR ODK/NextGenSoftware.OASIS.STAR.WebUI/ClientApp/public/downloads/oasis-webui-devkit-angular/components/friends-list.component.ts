import { Component, Input, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-friends-list',
  template: `
    <div class="oasis-friends-list" [class.oasis-friends-list--dark]="theme === 'dark'">
      <div class="friends-header">
        <h3>Friends ({{ (friends$ | async)?.length || 0 }})</h3>
        <input type="text" 
               placeholder="Search friends..."
               [(ngModel)]="searchQuery"
               (ngModelChange)="filterFriends()" />
      </div>

      <div class="friends-list">
        <div *ngIf="loading">Loading friends...</div>
        <div *ngFor="let friend of filteredFriends$ | async" class="friend-item">
          <img [src]="friend.image || '/default-avatar.png'" [alt]="friend.username" />
          <div class="friend-info">
            <h4>{{ friend.username }}</h4>
            <span *ngIf="showOnlineStatus" 
                  [class]="'status ' + (friend.isOnline ? 'online' : 'offline')">
              {{ friend.isOnline ? '🟢 Online' : '⚫ Offline' }}
            </span>
            <p class="karma">Karma: {{ friend.karma || 0 }}</p>
          </div>
          <div class="friend-actions" *ngIf="enableManagement">
            <button class="message-btn">💬</button>
            <button class="remove-btn" (click)="handleRemove(friend.id)">✕</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .friend-item {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1rem;
      border: 1px solid #ddd;
      border-radius: 8px;
      margin-bottom: 0.5rem;
    }
    .friend-item img { width: 50px; height: 50px; border-radius: 50%; }
  `]
})
export class OasisFriendsListComponent implements OnInit {
  @Input() avatarId!: string;
  @Input() theme: 'light' | 'dark' = 'dark';
  @Input() showOnlineStatus = true;
  @Input() enableManagement = true;

  friends$!: Observable<any[]>;
  filteredFriends$!: Observable<any[]>;
  searchQuery = '';
  loading = false;

  constructor(private oasisService: OASISService) {}

  ngOnInit() {
    this.friends$ = this.oasisService.getFriends(this.avatarId);
    this.filteredFriends$ = this.friends$;
  }

  filterFriends() {
    // Implement filtering logic
  }

  async handleRemove(friendId: string) {
    await this.oasisService.removeFriend(this.avatarId, friendId);
    this.friends$ = this.oasisService.getFriends(this.avatarId);
    this.filteredFriends$ = this.friends$;
  }
}

