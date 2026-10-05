import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-geonft-map',
  template: `
    <div class="oasis-geonft-map" [class.oasis-geonft-map--dark]="theme === 'dark'">
      <div #mapContainer class="map-container"></div>
      
      <div class="map-controls">
        <button *ngIf="enableCreation" class="create-geonft-btn">
          📍 Create Geo-NFT
        </button>
      </div>

      <div *ngIf="selectedNFT" class="geonft-popup">
        <img [src]="selectedNFT.imageUrl" [alt]="selectedNFT.name" />
        <h4>{{ selectedNFT.name }}</h4>
        <p>{{ selectedNFT.description }}</p>
        <div class="location">
          📍 {{ selectedNFT.location.lat | number:'1.6-6' }}, 
             {{ selectedNFT.location.lng | number:'1.6-6' }}
        </div>
      </div>
    </div>
  `,
  styles: [`
    .oasis-geonft-map { position: relative; height: 100%; }
    .map-container { width: 100%; height: 100%; }
    .map-controls { position: absolute; top: 10px; right: 10px; }
    .geonft-popup {
      position: absolute;
      bottom: 20px;
      left: 20px;
      background: white;
      padding: 1rem;
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0,0,0,0.2);
      max-width: 300px;
    }
  `]
})
export class OasisGeoNFTMapComponent implements OnInit {
  @Input() avatarId?: string;
  @Input() center = { lat: 0, lng: 0 };
  @Input() zoom = 2;
  @Input() theme: 'light' | 'dark' = 'dark';
  @Input() enableCreation = true;
  @Output() onLocationSelect = new EventEmitter<any>();

  geoNFTs: any[] = [];
  selectedNFT: any = null;

  constructor(private oasisService: OASISService) {}

  async ngOnInit() {
    this.geoNFTs = await this.oasisService.getGeoNFTs(this.avatarId);
  }
}

