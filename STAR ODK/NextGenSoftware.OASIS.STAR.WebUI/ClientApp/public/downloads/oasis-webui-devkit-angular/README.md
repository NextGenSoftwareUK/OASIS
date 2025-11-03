# OASIS Web UI Dev Kit - Angular

Complete Angular component library for integrating OASIS functionality into your applications.

## 🚀 Quick Start

```bash
ng add @oasis/webui-devkit-angular
# or
npm install @oasis/webui-devkit-angular
```

## 📦 Components Included

### Authentication & User Management
- **OasisAvatarSSOComponent** - Multi-provider authentication
- **OasisAvatarDetailComponent** - Profile management
- **OasisAvatarCardComponent** - Compact avatar display

### Data & Storage
- **OasisDataManagementComponent** - CRUD operations
- **OasisProviderManagementComponent** - Provider switching
- **OasisSettingsComponent** - Configuration panel

### Karma & Gamification
- **OasisKarmaManagementComponent** - Karma display & management
- **OasisKarmaLeaderboardComponent** - Top earners ranking
- **OasisAchievementsBadgesComponent** - Achievement system

### NFT & Assets
- **OasisNFTGalleryComponent** - NFT collection display
- **OasisNFTManagementComponent** - Mint, transfer, manage NFTs
- **OasisGeoNFTMapComponent** - Location-based NFT map
- **OasisGeoNFTManagementComponent** - Geo-NFT operations

### Communication
- **OasisMessagingComponent** - Real-time chat
- **OasisChatWidgetComponent** - Embeddable chat
- **OasisNotificationsComponent** - Notification system

### Social & Community
- **OasisSocialFeedComponent** - Activity feed
- **OasisFriendsListComponent** - Connections management
- **OasisGroupManagementComponent** - Group operations

## 🎨 Theming

```typescript
import { OasisTheme } from '@oasis/webui-devkit-angular';

const customTheme: OasisTheme = {
  primaryColor: '#00bcd4',
  secondaryColor: '#ff4081',
  mode: 'dark'
};
```

## 📖 Basic Usage

```typescript
// app.module.ts
import { OasisWebUIModule } from '@oasis/webui-devkit-angular';

@NgModule({
  imports: [
    OasisWebUIModule.forRoot({
      apiEndpoint: 'https://api.oasis.network',
      defaultProvider: 'holochain'
    })
  ]
})
export class AppModule { }

// component.ts
import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  template: `
    <oasis-avatar-sso
      [providers]="['holochain', 'ethereum', 'solana']"
      (onSuccess)="handleLogin($event)"
    ></oasis-avatar-sso>

    <oasis-karma-management
      [avatarId]="currentAvatarId"
      [theme]="'dark'"
    ></oasis-karma-management>

    <oasis-nft-gallery
      [collections]="['my-nfts']"
      [columns]="3"
    ></oasis-nft-gallery>

    <oasis-messaging
      [chatId]="'global-chat'"
      [position]="'bottom-right'"
    ></oasis-messaging>
  `
})
export class AppComponent {
  currentAvatarId: string;

  handleLogin(avatar: any) {
    this.currentAvatarId = avatar.id;
    console.log('Logged in:', avatar);
  }
}
```

## 🔧 Services

```typescript
import { OASISService } from '@oasis/webui-devkit-angular';

export class MyComponent {
  constructor(private oasisService: OASISService) {}

  async loadData() {
    const avatar = await this.oasisService.getAvatar(this.avatarId);
    const karma = await this.oasisService.getKarma(this.avatarId);
    const nfts = await this.oasisService.getNFTs(this.avatarId);
  }
}
```

## 📚 Documentation

Full documentation: https://docs.oasis.network/webui-devkit/angular

## 🛠️ Requirements

- Angular 15+
- TypeScript 4.9+
- RxJS 7+

## 📄 License

MIT License

