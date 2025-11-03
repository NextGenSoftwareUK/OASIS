import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-nft-gallery',
  template: `
    <div class="oasis-nft-gallery" [class.oasis-nft-gallery--dark]="theme === 'dark'">
      <div class="gallery-controls" *ngIf="enableFilters">
        <select [(ngModel)]="selectedFilter" (change)="filterNFTs()">
          <option value="all">All Collections</option>
          <option *ngFor="let col of collections" [value]="col">{{ col }}</option>
        </select>
        <select [(ngModel)]="selectedSort" (change)="sortNFTs()">
          <option value="date">Sort by Date</option>
          <option value="price">Sort by Price</option>
          <option value="name">Sort by Name</option>
        </select>
      </div>

      <div class="nft-grid" [style.grid-template-columns]="'repeat(' + columns + ', 1fr)'">
        <div *ngIf="loading" class="loading">Loading NFTs...</div>
        <div *ngFor="let nft of filteredNFTs$ | async" 
             class="nft-item"
             (click)="onSelect.emit(nft)">
          <img [src]="nft.imageUrl" [alt]="nft.name" />
          <div class="nft-info">
            <h4>{{ nft.name }}</h4>
            <p class="collection">{{ nft.collection }}</p>
            <p class="price">{{ nft.price }} OASIS</p>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .nft-grid { display: grid; gap: 1rem; }
    .nft-item { cursor: pointer; border: 1px solid #ddd; border-radius: 8px; overflow: hidden; }
    .nft-item img { width: 100%; height: 200px; object-fit: cover; }
  `]
})
export class OasisNFTGalleryComponent implements OnInit {
  @Input() avatarId?: string;
  @Input() collections: string[] = [];
  @Input() columns = 3;
  @Input() theme: 'light' | 'dark' = 'dark';
  @Input() enableFilters = true;
  @Input() sortBy: 'date' | 'price' | 'name' = 'date';
  @Output() onSelect = new EventEmitter<any>();

  filteredNFTs$!: Observable<any[]>;
  loading = false;
  selectedFilter = 'all';
  selectedSort = this.sortBy;

  constructor(private oasisService: OASISService) {}

  ngOnInit() {
    this.filteredNFTs$ = this.oasisService.getNFTs(this.avatarId, this.collections);
  }

  filterNFTs() {
    // Implement filtering logic
  }

  sortNFTs() {
    // Implement sorting logic
  }
}

