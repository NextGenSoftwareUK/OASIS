import { Component, Input, Output, EventEmitter } from '@angular/core';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-geonft-management',
  template: `
    <div class="oasis-geonft-mgmt" [class.oasis-geonft-mgmt--dark]="theme === 'dark'">
      <h3>Create Geo-NFT</h3>

      <div class="form-group">
        <label>Name</label>
        <input type="text" [(ngModel)]="formData.name" placeholder="My Geo-NFT" />
      </div>

      <div class="form-group">
        <label>Description</label>
        <textarea [(ngModel)]="formData.description" 
                  placeholder="Description of your Geo-NFT"
                  rows="3"></textarea>
      </div>

      <div class="form-group">
        <label>Image URL</label>
        <input type="text" [(ngModel)]="formData.imageUrl" placeholder="https://..." />
      </div>

      <div class="form-group">
        <label>Radius (meters)</label>
        <input type="number" 
               [(ngModel)]="formData.radius"
               min="10"
               max="10000" />
      </div>

      <div *ngIf="location" class="location-info">
        <h4>Selected Location:</h4>
        <p>Latitude: {{ location.lat | number:'1.6-6' }}</p>
        <p>Longitude: {{ location.lng | number:'1.6-6' }}</p>
      </div>

      <button class="mint-btn"
              (click)="handleMint()"
              [disabled]="minting || !formData.name || !location">
        {{ minting ? 'Minting...' : 'Mint Geo-NFT' }}
      </button>

      <div *ngIf="error" class="error">{{ error }}</div>
      <div *ngIf="success" class="success">Geo-NFT minted successfully!</div>
    </div>
  `,
  styles: [`
    .oasis-geonft-mgmt { padding: 1.5rem; }
    .form-group { margin-bottom: 1rem; }
    .form-group label { display: block; margin-bottom: 0.5rem; font-weight: bold; }
    .form-group input, .form-group textarea { width: 100%; padding: 0.5rem; }
  `]
})
export class OasisGeoNFTManagementComponent {
  @Input() avatarId!: string;
  @Input() location?: { lat: number; lng: number };
  @Input() theme: 'light' | 'dark' = 'dark';
  @Output() onMint = new EventEmitter<any>();

  formData: any = { name: '', description: '', imageUrl: '', radius: 100, metadata: {} };
  minting = false;
  error = '';
  success = false;

  constructor(private oasisService: OASISService) {}

  async handleMint() {
    if (!this.location) {
      this.error = 'Please select a location on the map';
      return;
    }

    this.minting = true;
    this.error = '';
    this.success = false;

    try {
      const result = await this.oasisService.mintGeoNFT(this.avatarId, {
        ...this.formData,
        location: this.location
      });
      this.onMint.emit(result);
      this.success = true;
      this.formData = { name: '', description: '', imageUrl: '', radius: 100, metadata: {} };
    } catch (err) {
      this.error = 'Failed to mint Geo-NFT. Please try again.';
    } finally {
      this.minting = false;
    }
  }
}

