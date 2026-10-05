# Avatar Dashboard - Transferred from oasis-wallet-ui

## Overview

The avatar dashboard has been successfully transferred from `oasis-wallet-ui` to the portal and adapted to work with vanilla JavaScript/HTML instead of React/Next.js.

## Files Added

### 1. `avatar-dashboard.js`
- Contains all the dashboard rendering logic
- Functions for building avatar insights data
- Components: Hero, Stats Grid, Activity Feed, Missions Panel, Rewards Grid
- Adapted from React components to vanilla JavaScript functions

### 2. Updated `portal.html`
- Added "Avatar" tab to the navigation
- Added avatar dashboard tab content section
- Integrated `avatar-dashboard.js` script
- Added comprehensive CSS styles for all avatar dashboard components
- Updated `switchTab()` function to load dashboard when avatar tab is selected

## Features

### Avatar Dashboard Hero
- Displays avatar initials, username, trust level
- Shows avatar ID with copy functionality
- Action buttons: Copy token, Manage avatar, Sign out

### Stats Grid
- Karma Score
- TimoRides Trips
- Community Impact
- Rewards Earned
- Each stat shows trend indicators (up/down/flat)

### Activity Feed
- Recent cross-network activities
- Shows provider logos (Solana, Ethereum, Polygon)
- Displays activity source, title, description, timestamp
- Value changes (e.g., "+1 NFT", "+45 karma")

### Missions Panel
- Active quests/missions
- Progress bars showing completion status
- Reward summaries
- Status indicators (active, completed, locked)

### Rewards Grid
- NFTs and perks earned
- Reward status (claimed, available, locked)
- Images and descriptions
- Source attribution

## Usage

1. **Access the Dashboard**: Click the "Avatar" tab in the portal navigation
2. **Authentication Required**: The dashboard loads data from `localStorage.getItem('oasis_auth')`
3. **Data Source**: Currently uses mock data from `buildAvatarInsights()` function
4. **Future Integration**: Ready to be connected to real OASIS APIs (see `OASIS_API_INTEGRATION_RESEARCH.md`)

## Data Structure

The dashboard expects an avatar object with:
```javascript
{
  avatarId: string,
  id: string,
  username: string,
  firstName?: string,
  lastName?: string,
  email?: string,
  karma?: number,
  trustLevel?: 'bronze' | 'silver' | 'gold' | 'platinum'
}
```

## Integration with Real APIs

To connect to real OASIS APIs:

1. **Update `buildAvatarInsights()`** in `avatar-dashboard.js`:
   - Replace mock data with API calls
   - Use the OASIS API client from `OASIS_API_INTEGRATION_RESEARCH.md`

2. **Load Real Data**:
   ```javascript
   async function loadAvatarDashboard() {
       const auth = JSON.parse(localStorage.getItem('oasis_auth'));
       const avatarId = auth.avatar.id || auth.avatar.avatarId;
       
       // Load real stats, activities, rewards, missions from APIs
       const stats = await oasisAPI.getAvatarStats(avatarId);
       const activities = await oasisAPI.getActivities(avatarId);
       const rewards = await oasisAPI.getRewards(avatarId);
       const missions = await oasisAPI.getMissions(avatarId);
       
       // Render with real data
       renderDashboard({ stats, activities, rewards, missions });
   }
   ```

## Styling

All styles are included in `portal.html` within the `<style>` tag. The styles use:
- CSS custom properties (variables) from the portal theme
- Responsive design with media queries
- Gradient backgrounds matching the portal aesthetic
- Consistent spacing and typography

## Responsive Design

The dashboard is fully responsive:
- **Desktop**: 2-column layout for activity feed and missions
- **Tablet**: Stacked layout with adjusted grid columns
- **Mobile**: Single column layout, optimized spacing

## Next Steps

1. **Connect to Real APIs**: Replace mock data with actual OASIS API calls
2. **Add Loading States**: Show loading indicators while fetching data
3. **Error Handling**: Add error states for failed API calls
4. **Real-time Updates**: Implement polling or WebSocket for live updates
5. **Interactive Features**: Make buttons and links functional (e.g., "Manage avatar", "View all")

## Original Source

- **Location**: `/Volumes/Storage/OASIS_CLEAN/oasis-wallet-ui/app/avatar/`
- **Components**: 
  - `page.tsx` - Main dashboard page
  - `AvatarDashboardHero.tsx`
  - `StatsGrid.tsx`
  - `ActivityFeed.tsx`
  - `MissionsPanel.tsx`
  - `RewardsGrid.tsx`
- **Data**: `lib/avatarInsights.ts` - Mock data builder

## Notes

- The dashboard currently uses mock/sample data
- All React hooks and state management have been converted to vanilla JavaScript
- The design and functionality match the original React implementation
- Ready for API integration following the patterns in `OASIS_API_INTEGRATION_RESEARCH.md`
