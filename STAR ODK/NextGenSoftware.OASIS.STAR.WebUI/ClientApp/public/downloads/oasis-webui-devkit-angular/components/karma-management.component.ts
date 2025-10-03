import { Component, Input, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-karma-management',
  template: `
    <div class="oasis-karma" [class.oasis-karma--dark]="theme === 'dark'">
      <div class="oasis-karma__current">
        <h3>Karma Points</h3>
        <div class="oasis-karma__value" *ngIf="karma$ | async as karma">
          {{ karma.total || 0 }}
        </div>
      </div>

      <div class="oasis-karma__history" *ngIf="showHistory && (history$ | async) as history">
        <h4>Recent Activity</h4>
        <ul>
          <li *ngFor="let entry of history">
            <span class="karma-amount">{{ entry.amount > 0 ? '+' : '' }}{{ entry.amount }}</span>
            <span class="karma-reason">{{ entry.reason }}</span>
            <span class="karma-date">{{ entry.date | date:'short' }}</span>
          </li>
        </ul>
      </div>

      <div class="oasis-karma__stats" *ngIf="karma$ | async as karma">
        <div class="stat">
          <span class="label">Rank</span>
          <span class="value">#{{ karma.rank || '-' }}</span>
        </div>
        <div class="stat">
          <span class="label">Level</span>
          <span class="value">{{ karma.level || 1 }}</span>
        </div>
        <div class="stat">
          <span class="label">Next Level</span>
          <span class="value">{{ karma.nextLevelAt || '-' }}</span>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .oasis-karma {
      padding: 1.5rem;
      background: #f5f5f5;
      border-radius: 12px;
    }
    .oasis-karma--dark {
      background: #1a1a1a;
      color: white;
    }
    .oasis-karma__value {
      font-size: 3rem;
      font-weight: bold;
      color: #00bcd4;
    }
  `]
})
export class OasisKarmaManagementComponent implements OnInit {
  @Input() avatarId!: string;
  @Input() theme: 'light' | 'dark' = 'dark';
  @Input() showHistory = true;

  karma$!: Observable<any>;
  history$!: Observable<any[]>;

  constructor(private oasisService: OASISService) {}

  ngOnInit() {
    this.karma$ = this.oasisService.getAvatarKarma(this.avatarId);
    if (this.showHistory) {
      this.history$ = this.oasisService.getKarmaHistory(this.avatarId);
    }
  }
}

