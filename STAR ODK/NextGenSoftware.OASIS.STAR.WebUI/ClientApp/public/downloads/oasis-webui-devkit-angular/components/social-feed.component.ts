import { Component, Input, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-social-feed',
  template: `
    <div class="oasis-social-feed" [class.oasis-social-feed--dark]="theme === 'dark'">
      <div class="post-composer" *ngIf="enablePosting && avatarId">
        <textarea placeholder="What's on your mind?" 
                  [(ngModel)]="newPost"
                  rows="3"></textarea>
        <button (click)="handlePost()" [disabled]="posting || !newPost.trim()">
          {{ posting ? 'Posting...' : 'Post' }}
        </button>
      </div>

      <div class="feed-posts">
        <div *ngIf="loading">Loading feed...</div>
        <div *ngFor="let post of posts$ | async" class="post-item">
          <div class="post-header">
            <img [src]="post.author.image" [alt]="post.author.username" />
            <div class="post-author">
              <h4>{{ post.author.username }}</h4>
              <span class="post-time">{{ post.createdAt | date:'short' }}</span>
            </div>
          </div>
          <div class="post-content">
            <p>{{ post.content }}</p>
            <img *ngIf="post.imageUrl" [src]="post.imageUrl" alt="Post" />
          </div>
          <div class="post-actions">
            <button (click)="handleLike(post.id)">❤️ {{ post.likes || 0 }}</button>
            <button>💬 {{ post.comments || 0 }}</button>
            <button>🔄 Share</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .post-item {
      border: 1px solid #ddd;
      border-radius: 8px;
      padding: 1rem;
      margin-bottom: 1rem;
    }
    .post-header { display: flex; gap: 1rem; margin-bottom: 1rem; }
    .post-header img { width: 40px; height: 40px; border-radius: 50%; }
  `]
})
export class OasisSocialFeedComponent implements OnInit {
  @Input() avatarId?: string;
  @Input() feedType: 'global' | 'friends' | 'personal' = 'global';
  @Input() theme: 'light' | 'dark' = 'dark';
  @Input() enablePosting = true;

  posts$!: Observable<any[]>;
  newPost = '';
  loading = false;
  posting = false;

  constructor(private oasisService: OASISService) {}

  ngOnInit() {
    this.posts$ = this.oasisService.getSocialFeed(this.feedType, this.avatarId);
  }

  async handlePost() {
    if (!this.newPost.trim()) return;
    
    this.posting = true;
    try {
      await this.oasisService.createPost(this.avatarId!, this.newPost);
      this.newPost = '';
      this.posts$ = this.oasisService.getSocialFeed(this.feedType, this.avatarId);
    } finally {
      this.posting = false;
    }
  }

  async handleLike(postId: string) {
    await this.oasisService.likePost(postId);
    this.posts$ = this.oasisService.getSocialFeed(this.feedType, this.avatarId);
  }
}

