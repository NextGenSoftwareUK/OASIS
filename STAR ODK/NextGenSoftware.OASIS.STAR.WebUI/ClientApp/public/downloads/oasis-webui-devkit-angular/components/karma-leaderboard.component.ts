import { Component, Input, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-karma-leaderboard',
  template: `
    <div class="oasis-karma-leaderboard" [class.oasis-karma-leaderboard--dark]="theme === 'dark'">
      <div class="leaderboard-header">
        <h3>Karma Leaderboard</h3>
        <span class="time-range">{{ timeRange | titlecase }}</span>
      </div>

      <div class="leaderboard-list">
        <div *ngIf="loading">Loading leaderboard...</div>
        <div *ngFor="let entry of leaderboard$ | async; let i = index"
             [class.current-user]="highlightCurrentUser && entry.avatarId === currentAvatarId"
             class="leaderboard-item">
          <span class="rank">{{ getMedalEmoji(i + 1) }}</span>
          <img [src]="entry.avatarImage || '/default-avatar.png'" 
               [alt]="entry.username"
               class="avatar" />
          <div class="user-info">
            <h4>{{ entry.username }}</h4>
            <span class="level">Level {{ entry.level }}</span>
          </div>
          <div class="karma-score">
            <span class="value">{{ entry.karma | number }}</span>
            <span class="label">karma</span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .leaderboard-item {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1rem;
      border: 1px solid #ddd;
      border-radius: 8px;
      margin-bottom: 0.5rem;
    }
    .leaderboard-item.current-user { background-color: #e3f2fd; }
    .avatar { width: 40px; height: 40px; border-radius: 50%; }
  `]
})
export class OasisKarmaLeaderboardComponent implements OnInit {
  @Input() limit = 10;
  @Input() timeRange: 'day' | 'week' | 'month' | 'all' = 'week';
  @Input() highlightCurrentUser = true;
  @Input() currentAvatarId?: string;
  @Input() theme: 'light' | 'dark' = 'dark';

  leaderboard$!: Observable<any[]>;
  loading = false;

  constructor(private oasisService: OASISService) {}

  ngOnInit() {
    this.leaderboard$ = this.oasisService.getKarmaLeaderboard(this.timeRange, this.limit);
  }

  getMedalEmoji(rank: number): string {
    if (rank === 1) return '🥇';
    if (rank === 2) return '🥈';
    if (rank === 3) return '🥉';
    return `#${rank}`;
  }
}

