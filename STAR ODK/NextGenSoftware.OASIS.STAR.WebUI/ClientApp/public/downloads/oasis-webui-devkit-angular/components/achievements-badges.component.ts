import { Component, Input, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-achievements-badges',
  template: `
    <div class="oasis-achievements oasis-achievements--{{theme}} oasis-achievements--{{layout}}">
      <div class="achievements-header">
        <h3>Achievements & Badges</h3>
        <span class="count" *ngIf="achievements$ | async as achievements">
          {{ getUnlockedCount(achievements) }} / {{ achievements.length }}
        </span>
      </div>

      <div [class]="'achievements-' + layout">
        <div *ngIf="loading">Loading achievements...</div>
        <div *ngFor="let achievement of achievements$ | async"
             [class.unlocked]="achievement.unlocked"
             [class.locked]="!achievement.unlocked"
             class="achievement-item">
          <div class="achievement-icon">
            <img *ngIf="achievement.unlocked" [src]="achievement.iconUrl" [alt]="achievement.name" />
            <div *ngIf="!achievement.unlocked" class="locked-icon">🔒</div>
          </div>
          <div class="achievement-info">
            <h4>{{ achievement.name }}</h4>
            <p>{{ achievement.description }}</p>
            
            <div *ngIf="showProgress && !achievement.unlocked && achievement.progress" class="progress-bar">
              <div class="progress-fill" 
                   [style.width.%]="(achievement.progress.current / achievement.progress.total) * 100">
              </div>
              <span class="progress-text">
                {{ achievement.progress.current }} / {{ achievement.progress.total }}
              </span>
            </div>
            
            <span *ngIf="achievement.unlocked" class="unlock-date">
              Unlocked: {{ achievement.unlockedAt | date }}
            </span>
          </div>
          <span *ngIf="achievement.rarity" [class]="'rarity rarity--' + achievement.rarity">
            {{ achievement.rarity }}
          </span>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .achievements-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(250px, 1fr)); gap: 1rem; }
    .achievement-item { padding: 1rem; border: 1px solid #ddd; border-radius: 8px; }
    .achievement-item.locked { opacity: 0.5; }
    .progress-bar { position: relative; height: 20px; background: #eee; border-radius: 10px; overflow: hidden; }
    .progress-fill { height: 100%; background: #00bcd4; transition: width 0.3s; }
  `]
})
export class OasisAchievementsBadgesComponent implements OnInit {
  @Input() avatarId!: string;
  @Input() theme: 'light' | 'dark' = 'dark';
  @Input() layout: 'grid' | 'list' = 'grid';
  @Input() showProgress = true;

  achievements$!: Observable<any[]>;
  loading = false;

  constructor(private oasisService: OASISService) {}

  ngOnInit() {
    this.achievements$ = this.oasisService.getAchievements(this.avatarId);
  }

  getUnlockedCount(achievements: any[]): number {
    return achievements.filter(a => a.unlocked).length;
  }
}

