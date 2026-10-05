# Mobile Layout Options for BillionsHealed

## Current Problem
Twitter feed is spilling down the page and getting squashed when positioned absolutely on mobile.

## Proposed Solutions

### Option 1: Side-by-Side Compact Layout
```
┌─────────────────────────┐
│    BILLIONSHEALED       │ ← Navbar
└─────────────────────────┘
┌──────────┬──────────────┐
│ Twitter  │              │
│ Feed     │  Thermometer │
│ (40%)    │     (60%)    │
│          │              │
│ Tweets   │      🌡️      │
│ scroll   │              │
└──────────┴──────────────┘
```
**Pros:**
- Both visible simultaneously
- Natural split screen
- Easy to compare tweets → thermometer
**Cons:**
- Narrower columns on small phones

### Option 2: Twitter Above, Thermometer Below
```
┌─────────────────────────┐
│    BILLIONSHEALED       │ ← Navbar
└─────────────────────────┘
┌─────────────────────────┐
│   Twitter Feed          │
│   #billionshealed       │
│   ────────────          │
│   Tweet 1...            │
│   Tweet 2...            │
│   Tweet 3...            │
└─────────────────────────┘
        ↓ scroll ↓
┌─────────────────────────┐
│                         │
│      Thermometer        │
│          🌡️             │
│       (centered)        │
│                         │
│    [Mint Button]        │
└─────────────────────────┘
```
**Pros:**
- Full width for each section
- Clean, simple layout
- No squashing issues
**Cons:**
- Need to scroll to see thermometer

### Option 3: Compact Feed Above Thermometer (Both Visible)
```
┌─────────────────────────┐
│    BILLIONSHEALED       │ ← Navbar
└─────────────────────────┘
┌─────────────────────────┐
│  𝕏 #billionshealed      │
│  ─────────────────      │
│  • Tweet 1 excerpt...   │ ← Compact, 2-3 tweets
│  • Tweet 2 excerpt...   │   only (truncated)
└─────────────────────────┘
┌─────────────────────────┐
│                         │
│      Thermometer        │
│          🌡️             │
│       (larger)          │
│                         │
│    [Mint Button]        │
└─────────────────────────┘
```
**Pros:**
- Both visible without scrolling
- Thermometer gets more space
- Compact feed preview
**Cons:**
- Only see 2-3 tweets (truncated)

### Option 4: Tabs (Twitter / Thermometer)
```
┌─────────────────────────┐
│    BILLIONSHEALED       │ ← Navbar
└─────────────────────────┘
┌───────────┬─────────────┐
│  Tweets   │ Thermometer │ ← Tabs
└───────────┴─────────────┘
┌─────────────────────────┐
│                         │
│   [Active Tab Content]  │
│                         │
│                         │
│                         │
└─────────────────────────┘
```
**Pros:**
- Full screen for each view
- Clean interface
- Easy to switch
**Cons:**
- Can't see both simultaneously
- Extra tap required

### Option 5: Floating Mini Feed (Recommended)
```
┌─────────────────────────┐
│    BILLIONSHEALED   [𝕏] │ ← Navbar with tweet icon
└─────────────────────────┘
                    ┌──────┐
                    │𝕏 Feed│ ← Small floating
                    │──────│    box (tap to
                    │Tw1   │    expand)
                    │Tw2   │
                    └──────┘
┌─────────────────────────┐
│                         │
│      Thermometer        │
│          🌡️             │
│       (centered)        │
│                         │
│    [Mint Button]        │
└─────────────────────────┘
```
**Pros:**
- Thermometer is main focus
- Tweets always visible but compact
- Tap feed to expand full view
**Cons:**
- Requires interaction to see full tweets

### Option 6: Horizontal Cards (My Recommendation)
```
┌─────────────────────────┐
│    BILLIONSHEALED       │ ← Navbar
└─────────────────────────┘
┌─────────────────────────┐
│ 𝕏 Latest: "Amazing..."  │ ← Horizontal scrolling
│ ← swipe for more tweets │    tweet cards
└─────────────────────────┘
┌─────────────────────────┐
│                         │
│      Thermometer        │
│          🌡️             │
│       (centered)        │
│                         │
│    [Mint Button]        │
│                         │
│  Stats: 47/100 minted   │
└─────────────────────────┘
```
**Pros:**
- Both always visible
- Thermometer is primary focus
- Swipe tweets horizontally
- Clean, modern mobile UX
**Cons:**
- Only see one full tweet at a time

## My Recommendation

**Option 6: Horizontal Tweet Cards** or **Option 3: Compact Feed Above**

Both keep the thermometer as the hero element while showing recent tweets in a compact, mobile-friendly way.

## Quick Implementation

Which would you like me to implement? Or would you like to see a mockup of one of these options first?

