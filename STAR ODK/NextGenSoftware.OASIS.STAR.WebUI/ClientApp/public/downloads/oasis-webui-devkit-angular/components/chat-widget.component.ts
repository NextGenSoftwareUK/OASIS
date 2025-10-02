import { Component, Input } from '@angular/core';

@Component({
  selector: 'oasis-chat-widget',
  template: `
    <div class="oasis-chat-widget oasis-chat-widget--{{position}}">
      <button *ngIf="!isOpen" 
              class="chat-widget__toggle"
              (click)="isOpen = true">
        💬
        <span *ngIf="unreadCount > 0" class="badge">{{ unreadCount }}</span>
      </button>

      <div *ngIf="isOpen" class="chat-widget__container">
        <div class="chat-widget__header">
          <span>Chat</span>
          <button (click)="isOpen = false">×</button>
        </div>
        <oasis-messaging [chatId]="chatId"
                        [avatarId]="avatarId"
                        position="inline"
                        [theme]="theme">
        </oasis-messaging>
      </div>
    </div>
  `,
  styles: [`
    .oasis-chat-widget {
      position: fixed;
      z-index: 1000;
    }
    .oasis-chat-widget--bottom-right { bottom: 20px; right: 20px; }
    .oasis-chat-widget--bottom-left { bottom: 20px; left: 20px; }
    .chat-widget__toggle {
      width: 60px;
      height: 60px;
      border-radius: 50%;
      background: #00bcd4;
      border: none;
      font-size: 24px;
      cursor: pointer;
      position: relative;
    }
    .chat-widget__container {
      width: 350px;
      height: 500px;
      background: white;
      border-radius: 12px;
      box-shadow: 0 10px 40px rgba(0,0,0,0.2);
      display: flex;
      flex-direction: column;
    }
  `]
})
export class OasisChatWidgetComponent {
  @Input() chatId!: string;
  @Input() avatarId!: string;
  @Input() position: 'bottom-right' | 'bottom-left' = 'bottom-right';
  @Input() defaultOpen = false;
  @Input() enableNotifications = true;
  @Input() theme: 'light' | 'dark' = 'dark';

  isOpen = this.defaultOpen;
  unreadCount = 0;
}

