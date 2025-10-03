import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-notifications',
  template: `
    <div class="oasis-notifications oasis-notifications--{{position}} oasis-notifications--{{theme}}">
      <div *ngFor="let notification of notifications"
           [class]="'notification notification--' + (notification.type || 'info')">
        <div class="notification-content">
          <h4>{{ notification.title }}</h4>
          <p>{{ notification.message }}</p>
        </div>
        <button class="dismiss-btn" (click)="handleDismiss(notification.id)">×</button>
      </div>
    </div>
  `,
  styles: [`
    .oasis-notifications {
      position: fixed;
      z-index: 9999;
      max-width: 400px;
    }
    .oasis-notifications--top-right { top: 20px; right: 20px; }
    .oasis-notifications--top-left { top: 20px; left: 20px; }
    .notification {
      background: white;
      padding: 1rem;
      margin-bottom: 0.5rem;
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0,0,0,0.15);
      display: flex;
      justify-content: space-between;
      align-items: start;
    }
  `]
})
export class OasisNotificationsComponent implements OnInit, OnDestroy {
  @Input() avatarId!: string;
  @Input() position: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left' = 'top-right';
  @Input() autoHide = true;
  @Input() duration = 5000;
  @Input() theme: 'light' | 'dark' = 'dark';

  notifications: any[] = [];
  private subscription?: Subscription;

  constructor(private oasisService: OASISService) {}

  ngOnInit() {
    this.subscription = this.oasisService.subscribeToNotifications(this.avatarId).subscribe(
      (notification: any) => {
        this.notifications.unshift(notification);
        
        if (this.autoHide) {
          setTimeout(() => {
            this.handleDismiss(notification.id);
          }, this.duration);
        }
      }
    );
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }

  handleDismiss(id: string) {
    this.notifications = this.notifications.filter(n => n.id !== id);
  }
}

