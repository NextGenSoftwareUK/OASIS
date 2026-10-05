# Telegram Gamification UI Design

## UI Elements Overview

The Telegram gamification section in the portal should include the following key elements:

---

## 1. Connection Status Banner

**Purpose**: Show if Telegram is linked and provide quick link action

**Elements**:
- Status indicator (Linked/Not Linked)
- Telegram username (if linked)
- "Link Telegram" button (if not linked)
- "Disconnect" option (if linked)
- Quick link to Telegram group

**Visual Design**:
```
┌─────────────────────────────────────────────┐
│  🔗 Telegram Connection                     │
│  ────────────────────────────────────────   │
│  Status: ✅ Linked                          │
│  Username: @your_username                    │
│  [Join Telegram Group] [Disconnect]        │
└─────────────────────────────────────────────┘
```

---

## 2. Stats Overview Grid

**Purpose**: Quick overview of Telegram rewards earned

**Elements**:
- **Total Karma Earned**: From Telegram activities
- **Total Tokens Earned**: Token rewards received
- **NFTs Earned**: Count of NFTs from Telegram
- **Daily Streak**: Consecutive days active
- **Weekly Active**: Days active this week
- **Achievements Completed**: Total achievements
- **Groups Joined**: Number of Telegram groups

**Visual Design**:
```
┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
│ Karma    │ │ Tokens   │ │ NFTs     │ │ Streak   │
│ 1,250    │ │ 45.2     │ │ 8        │ │ 12 days  │
│ +50 week │ │ +5.2 week│ │ +2 week  │ │ 🔥       │
└──────────┘ └──────────┘ └──────────┘ └──────────┘
```

---

## 3. Recent Rewards Feed

**Purpose**: Show real-time rewards as they're earned

**Elements**:
- Timestamp
- Action type (mention, link share, quality post, etc.)
- Reward details (karma, tokens, NFT)
- Link to view full details

**Visual Design**:
```
Recent Rewards
─────────────────────────────────────
🎉 2h ago
   Mentioned OASIS
   +5 karma

🎉 5h ago
   Shared OASIS link
   +10 karma, +0.1 tokens

🎉 1d ago
   Created quality post
   +30 karma, +1 token

🎉 2d ago
   Earned NFT: Tutorial Creator
   +100 karma, +5 tokens, +1 NFT
```

---

## 4. Achievement Badges Grid

**Purpose**: Show progress toward achievements

**Elements**:
- Achievement icon/badge
- Achievement name
- Progress bar (X/Y completed)
- Reward preview
- Status (Locked/In Progress/Completed)

**Visual Design**:
```
Achievement Badges
─────────────────────────────────────
┌─────────────┐ ┌─────────────┐
│ 🥉 Bronze   │ │ 🥈 Silver   │
│ OASIS       │ │ Quality     │
│ Mentioner   │ │ Contributor │
│ 8/10        │ │ 7/10        │
│ ████████░░  │ │ ███████░░░  │
│ +50 karma   │ │ +100 karma  │
└─────────────┘ └─────────────┘
```

---

## 5. Activity Timeline

**Purpose**: Detailed history of all Telegram activities

**Elements**:
- Chronological list of actions
- Action type with icon
- Reward earned
- Date/time
- Filter options (All/Karma/Tokens/NFTs)

**Visual Design**:
```
Activity Timeline                    [Filter: All ▼]
─────────────────────────────────────
Today
  • Mentioned OASIS              +5 karma
  • Shared link                  +10 karma, +0.1 tokens

Yesterday
  • Created quality post         +30 karma, +1 token
  • Answered question            +15 karma, +0.3 tokens

This Week
  • Daily active bonus           +10 karma
  • Weekly active bonus          +25 karma, +0.5 tokens
```

---

## 6. Leaderboard

**Purpose**: Show top performers in Telegram group

**Elements**:
- Rank
- Username/Avatar
- Total karma
- Total tokens
- Achievements count
- Filter by time period (Daily/Weekly/All-time)

**Visual Design**:
```
Leaderboard                    [Period: Weekly ▼]
─────────────────────────────────────
🥇 @user1    1,250 karma  45.2 tokens  12 achievements
🥈 @user2    980 karma    32.1 tokens  9 achievements
🥉 @user3    750 karma    25.5 tokens  7 achievements
   4. @user4  620 karma    18.3 tokens  5 achievements
   5. @user5  450 karma    12.1 tokens  4 achievements
```

---

## 7. Achievement Progress Cards

**Purpose**: Detailed view of specific achievements

**Elements**:
- Achievement name and description
- Current progress
- Target
- Time remaining (if applicable)
- Rewards on completion
- Action items to complete

**Visual Design**:
```
Active Achievements
─────────────────────────────────────
┌─────────────────────────────────────┐
│ 🎯 OASIS Mentioner                   │
│ Mention OASIS 10 times               │
│                                      │
│ Progress: 8/10 mentions              │
│ ████████░░ 80%                       │
│                                      │
│ Rewards: +50 karma, Bronze Badge NFT │
│                                      │
│ [View Details]                       │
└─────────────────────────────────────┘
```

---

## 8. Quick Actions Panel

**Purpose**: Easy access to common actions

**Elements**:
- "Join Telegram Group" button
- "View Group Rules" link
- "How to Earn Rewards" guide
- "Report Issue" link

**Visual Design**:
```
Quick Actions
─────────────────────────────────────
[ Join Telegram Group ]
[ View Rewards Guide ]
[ Group Rules ]
[ Report Issue ]
```

---

## 9. Rewards Breakdown

**Purpose**: Detailed breakdown of rewards by category

**Elements**:
- Pie chart or bar chart
- Categories (Content, Engagement, Marketing, Technical)
- Total per category
- Percentage breakdown

**Visual Design**:
```
Rewards Breakdown
─────────────────────────────────────
Content Creation     45%  (562 karma)
Community Engagement  30%  (375 karma)
Marketing & Growth    15%  (188 karma)
Technical             10%  (125 karma)
```

---

## 10. NFT Gallery

**Purpose**: Display NFTs earned from Telegram

**Elements**:
- NFT cards with images
- NFT name and description
- Date earned
- View on blockchain link
- Filter by tier (Bronze/Silver/Gold/Platinum)

**Visual Design**:
```
NFTs Earned from Telegram
─────────────────────────────────────
┌────────┐ ┌────────┐ ┌────────┐
│ [IMG]  │ │ [IMG]  │ │ [IMG]  │
│ Tutorial│ │ Viral  │ │ Code   │
│ Creator │ │ Creator│ │Contrib │
│ Gold   │ │ Gold   │ │ Gold   │
└────────┘ └────────┘ └────────┘
```

---

## Layout Structure

### Desktop Layout (3-column)
```
┌─────────────────────────────────────────────────────┐
│ Connection Status Banner                            │
├─────────────────────────────────────────────────────┤
│ Stats Grid (4 columns)                              │
├──────────────────┬──────────────────┬───────────────┤
│ Recent Rewards   │ Achievement      │ Quick Actions │
│ Feed             │ Badges Grid      │ Panel         │
│                  │                  │               │
│                  │                  │               │
├──────────────────┴──────────────────┴───────────────┤
│ Activity Timeline                                  │
├──────────────────┬──────────────────────────────────┤
│ Leaderboard      │ Rewards Breakdown                │
└──────────────────┴──────────────────────────────────┘
```

### Mobile Layout (Stacked)
```
┌─────────────────────┐
│ Connection Banner    │
├─────────────────────┤
│ Stats Grid (2x2)     │
├─────────────────────┤
│ Recent Rewards       │
├─────────────────────┤
│ Achievement Badges   │
├─────────────────────┤
│ Activity Timeline    │
├─────────────────────┤
│ Leaderboard         │
└─────────────────────┘
```

---

## Color Coding & Icons

### Achievement Tiers
- **Bronze**: 🥉 Orange/Amber (#F59E0B)
- **Silver**: 🥈 Gray/Silver (#9CA3AF)
- **Gold**: 🥇 Yellow/Gold (#FBBF24)
- **Platinum**: 💎 Purple/Platinum (#A78BFA)

### Action Types
- **Mention**: 💬 Blue
- **Link Share**: 🔗 Green
- **Quality Post**: ✍️ Purple
- **Helpful Answer**: 💡 Yellow
- **Code Example**: 💻 Cyan
- **NFT Reward**: 🎨 Rainbow gradient

### Status Indicators
- **Active**: 🟢 Green dot
- **Completed**: ✅ Green check
- **In Progress**: 🔄 Blue spinner
- **Locked**: 🔒 Gray lock

---

## Interactive Features

1. **Hover Effects**: Cards lift slightly on hover
2. **Click Actions**: 
   - Achievement badges → Show details modal
   - NFT cards → Open NFT viewer
   - Activity items → Show full details
3. **Real-time Updates**: Auto-refresh every 30 seconds
4. **Notifications**: Toast notifications for new rewards
5. **Filters**: Filter by date, type, reward amount
6. **Search**: Search achievements and activities

---

## Responsive Breakpoints

- **Desktop**: 1600px+ (3-column layout)
- **Tablet**: 1024px-1599px (2-column layout)
- **Mobile**: <1024px (Stacked layout)

---

## Accessibility

- ARIA labels for all interactive elements
- Keyboard navigation support
- Screen reader friendly
- High contrast mode support
- Focus indicators

---

## Performance Considerations

- Lazy load images (NFTs, achievement icons)
- Virtual scrolling for long activity lists
- Debounced search/filter
- Cached API responses
- Progressive loading (stats first, then details)
