import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { OASISService } from '../services/oasis.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'oasis-messaging',
  template: `
    <div class="oasis-messaging" 
         [class.oasis-messaging--dark]="theme === 'dark'"
         [class.oasis-messaging--inline]="position === 'inline'">
      
      <div class="messaging-header">
        <h3>Chat</h3>
        <button *ngIf="position !== 'inline'" (click)="isOpen = false">×</button>
      </div>

      <div class="messaging-messages" #messagesContainer>
        <div *ngFor="let msg of messages" 
             [class.message--own]="msg.avatarId === avatarId"
             class="message">
          <div class="message-avatar">
            <img [src]="msg.avatarImage" [alt]="msg.avatarName" />
          </div>
          <div class="message-content">
            <div class="message-author">{{ msg.avatarName }}</div>
            <div class="message-text">{{ msg.content }}</div>
            <div class="message-time">{{ msg.timestamp | date:'short' }}</div>
          </div>
        </div>
      </div>

      <div class="messaging-input">
        <input type="text" 
               [(ngModel)]="newMessage"
               (keyup.enter)="sendMessage()"
               placeholder="Type a message..." />
        <button *ngIf="enableEmojis" class="emoji-btn">😊</button>
        <button *ngIf="enableFileSharing" class="file-btn">📎</button>
        <button (click)="sendMessage()">Send</button>
      </div>
    </div>
  `,
  styles: [`
    .oasis-messaging { display: flex; flex-direction: column; height: 100%; }
    .messaging-messages { flex: 1; overflow-y: auto; padding: 1rem; }
    .message { display: flex; gap: 0.5rem; margin-bottom: 1rem; }
  `]
})
export class OasisMessagingComponent implements OnInit, OnDestroy {
  @Input() chatId!: string;
  @Input() avatarId!: string;
  @Input() position: 'bottom-right' | 'bottom-left' | 'inline' = 'inline';
  @Input() theme: 'light' | 'dark' = 'dark';
  @Input() enableEmojis = true;
  @Input() enableFileSharing = true;

  messages: any[] = [];
  newMessage = '';
  isOpen = true;
  private subscription?: Subscription;

  constructor(private oasisService: OASISService) {}

  async ngOnInit() {
    this.messages = await this.oasisService.getChatMessages(this.chatId);
    this.subscription = this.oasisService.subscribeToChat(this.chatId).subscribe(
      (message: any) => {
        this.messages.push(message);
      }
    );
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }

  async sendMessage() {
    if (!this.newMessage.trim()) return;
    
    await this.oasisService.sendMessage({
      chatId: this.chatId,
      avatarId: this.avatarId,
      content: this.newMessage,
      timestamp: new Date().toISOString()
    });
    
    this.newMessage = '';
  }
}

