import { Component, Input, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-group-management',
  template: `
    <div class="oasis-group-mgmt" [class.oasis-group-mgmt--dark]="theme === 'dark'">
      <div class="groups-header">
        <h3>My Groups</h3>
        <button *ngIf="enableCreation" (click)="showCreateDialog = true">+ Create Group</button>
      </div>

      <div class="create-dialog" *ngIf="showCreateDialog">
        <input type="text" placeholder="Group Name" [(ngModel)]="newGroup.name" />
        <textarea placeholder="Description" [(ngModel)]="newGroup.description"></textarea>
        <label>
          <input type="checkbox" [(ngModel)]="newGroup.isPrivate" />
          Private Group
        </label>
        <div class="dialog-actions">
          <button (click)="handleCreate()" [disabled]="creating">Create</button>
          <button (click)="showCreateDialog = false">Cancel</button>
        </div>
      </div>

      <div class="groups-list">
        <div *ngIf="loading">Loading groups...</div>
        <div *ngFor="let group of groups$ | async" class="group-item">
          <img [src]="group.imageUrl || '/default-group.png'" [alt]="group.name" />
          <div class="group-info">
            <h4>{{ group.name }}</h4>
            <p>{{ group.description }}</p>
            <span class="members">👥 {{ group.memberCount }} members</span>
          </div>
          <div class="group-actions">
            <button class="view-btn">View</button>
            <button class="leave-btn" (click)="handleLeave(group.id)">Leave</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .group-item {
      display: flex;
      gap: 1rem;
      padding: 1rem;
      border: 1px solid #ddd;
      border-radius: 8px;
      margin-bottom: 1rem;
    }
    .group-item img { width: 60px; height: 60px; border-radius: 8px; }
  `]
})
export class OasisGroupManagementComponent implements OnInit {
  @Input() avatarId!: string;
  @Input() theme: 'light' | 'dark' = 'dark';
  @Input() enableCreation = true;

  groups$!: Observable<any[]>;
  showCreateDialog = false;
  loading = false;
  creating = false;
  newGroup: any = { name: '', description: '', isPrivate: false };

  constructor(private oasisService: OASISService) {}

  ngOnInit() {
    this.groups$ = this.oasisService.getGroups(this.avatarId);
  }

  async handleCreate() {
    this.creating = true;
    try {
      await this.oasisService.createGroup(this.avatarId, this.newGroup);
      this.showCreateDialog = false;
      this.newGroup = { name: '', description: '', isPrivate: false };
      this.groups$ = this.oasisService.getGroups(this.avatarId);
    } finally {
      this.creating = false;
    }
  }

  async handleLeave(groupId: string) {
    await this.oasisService.leaveGroup(this.avatarId, groupId);
    this.groups$ = this.oasisService.getGroups(this.avatarId);
  }
}

