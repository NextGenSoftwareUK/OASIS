import { Component, Input, Output, EventEmitter } from '@angular/core';
import { OASISService } from '../services/oasis.service';

@Component({
  selector: 'oasis-nft-management',
  template: `
    <div class="oasis-nft-mgmt" [class.oasis-nft-mgmt--dark]="theme === 'dark'">
      <div class="nft-actions">
        <button *ngIf="enableMinting" (click)="showMintDialog = true">Mint NFT</button>
        <button *ngIf="enableTransfer && selectedNFT" (click)="showTransferDialog = true">Transfer</button>
      </div>

      <div *ngIf="showMintDialog" class="dialog">
        <h3>Mint NFT</h3>
        <input type="text" placeholder="Name" [(ngModel)]="mintData.name" />
        <textarea placeholder="Description" [(ngModel)]="mintData.description"></textarea>
        <input type="text" placeholder="Image URL" [(ngModel)]="mintData.imageUrl" />
        <button (click)="handleMint()" [disabled]="minting">
          {{ minting ? 'Minting...' : 'Mint' }}
        </button>
        <button (click)="showMintDialog = false">Cancel</button>
      </div>

      <div *ngIf="showTransferDialog && selectedNFT" class="dialog">
        <h3>Transfer NFT</h3>
        <input type="text" placeholder="Recipient Address" [(ngModel)]="transferAddress" />
        <button (click)="handleTransfer()" [disabled]="transferring">
          {{ transferring ? 'Transferring...' : 'Transfer' }}
        </button>
        <button (click)="showTransferDialog = false">Cancel</button>
      </div>
    </div>
  `,
  styles: [`
    .dialog {
      position: fixed;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      background: white;
      padding: 2rem;
      border-radius: 12px;
      box-shadow: 0 10px 40px rgba(0,0,0,0.3);
    }
  `]
})
export class OasisNFTManagementComponent {
  @Input() avatarId!: string;
  @Input() enableMinting = true;
  @Input() enableTransfer = true;
  @Input() theme: 'light' | 'dark' = 'dark';
  @Input() selectedNFT?: any;
  @Output() onMintComplete = new EventEmitter<any>();
  @Output() onTransferComplete = new EventEmitter<any>();

  showMintDialog = false;
  showTransferDialog = false;
  minting = false;
  transferring = false;
  mintData: any = {};
  transferAddress = '';

  constructor(private oasisService: OASISService) {}

  async handleMint() {
    this.minting = true;
    try {
      const result = await this.oasisService.mintNFT(this.avatarId, this.mintData);
      this.onMintComplete.emit(result);
      this.showMintDialog = false;
    } finally {
      this.minting = false;
    }
  }

  async handleTransfer() {
    this.transferring = true;
    try {
      const result = await this.oasisService.transferNFT(this.selectedNFT.id, this.transferAddress);
      this.onTransferComplete.emit(result);
      this.showTransferDialog = false;
    } finally {
      this.transferring = false;
    }
  }
}

