# Missions UI Proposal for OASIS Portal

## Overview

Based on the STAR architecture, missions are high-level story containers that organize quests into chapters. Missions provide the narrative structure for user progression, while quests are the actionable steps within missions.

## Key Concepts from STAR

### Mission Structure
- **Mission**: A container for quests, organized into optional chapters
- **Chapters**: Optional grouping of quests (for large missions)
- **Quests**: Actionable steps within a mission
- **MissionType**: Enum defining mission types (Easy, Medium, Hard, Expert)
- **Inherits from QuestBase**: Missions inherit quest properties like:
  - Name, Description
  - Status (Pending, InProgress, Completed, etc.)
  - Difficulty
  - Rewards (Karma, XP)
  - Quests list
  - Order/Sequence

### Mission Operations
- Create mission
- Start mission (for user)
- Complete mission (when all quests/chapters complete)
- Update mission
- Delete mission
- Publish/Unpublish mission
- Download/Install mission
- Clone mission

## UI Design Proposal

### 1. Mission Tracker (Main View)
**Location**: STAR Dashboard → Missions Tab

**Layout**:
```
┌─────────────────────────────────────────────────────────┐
│ Mission Tracker                              [+ Create] │
│ Track your story progression and mission objectives     │
├─────────────────────────────────────────────────────────┤
│                                                          │
│ ┌────────────────────────────────────────────────────┐ │
│ │ Mission: "The Great Discovery"          [75%]     │ │
│ │ A journey through ancient lands...                 │ │
│ │                                                     │ │
│ │ Story Progress: ████████████████░░░░ 75%          │ │
│ │                                                     │ │
│ │ Quest Timeline:                                     │ │
│ │  [✓]─[✓]─[✓]─[4]─[5]─[6]─[7]─[8]                  │ │
│ │  Ch1   Ch2   Ch3  Ch4  Ch5  Ch6  Ch7  Ch8         │ │
│ │                                                     │ │
│ │ Type: Expert | Quests: 6/8 | Started: 2 days ago  │ │
│ └────────────────────────────────────────────────────┘ │
│                                                          │
│ ┌────────────────────────────────────────────────────┐ │
│ │ Mission: "Rookie Explorer"              [100%]    │ │
│ │ Complete your first steps in STAR...               │ │
│ │                                                     │ │
│ │ Story Progress: ████████████████████ 100% ✓       │ │
│ │                                                     │ │
│ │ Quest Timeline:                                     │ │
│ │  [✓]─[✓]─[✓]─[✓]                                   │ │
│ │  Q1   Q2   Q3   Q4                                  │ │
│ │                                                     │ │
│ │ Type: Easy | Completed 5 days ago                  │ │
│ └────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

**Features**:
- **Mission Cards**: Visual cards showing mission progress
- **Progress Visualization**: 
  - Progress bar showing completion percentage
  - Quest timeline showing completed/active/pending quests
  - Color-coded nodes (green = completed, purple = active, gray = pending)
- **Mission Metadata**: Type, quest count, start/completion dates
- **Chapter Support**: Visual grouping if mission has chapters
- **Status Indicators**: Active, Completed, Not Started

### 2. Mission Detail View

**Layout**:
```
┌─────────────────────────────────────────────────────────┐
│ ← Back to Missions                                      │
│                                                          │
│ The Great Discovery                                     │
├────────────────────────────┬────────────────────────────┤
│                            │                            │
│ Description                │ Mission Info               │
│ ─────────────              │ ─────────────              │
│ A journey through...       │ Type: Expert               │
│                            │ Progress: 75%              │
│                            │ Quests: 6/8                │
│                            │ Status: In Progress        │
│ Quests (6/8 completed)     │ Started: 2 days ago        │
│ ─────────────              │                            │
│                            │ Chapters: 3                │
│ ┌────────────────────────┐ │                            │
│ │ ✓ Chapter 1: Beginning │ │ Rewards                    │
│ │   ✓ Quest 1: Start     │ │ ─────────────              │
│ │   ✓ Quest 2: Explore   │ │ XP: 500                    │
│ │   ✓ Quest 3: Discover  │ │ Karma: 1000                │
│ └────────────────────────┘ │                            │
│                            │                            │
│ ┌────────────────────────┐ │ Quick Actions              │
│ │ ✓ Chapter 2: Journey   │ │ ─────────────              │
│ │   ✓ Quest 4: Travel    │ │ [View Chapter 3]           │
│ │   ✓ Quest 5: Meet      │ │ [Complete Mission]         │
│ │   → Quest 6: Continue  │ │ [Share Progress]           │
│ └────────────────────────┘ │                            │
│                            │                            │
│ ┌────────────────────────┐ │                            │
│ │ → Chapter 3: Climax    │ │                            │
│ │   → Quest 7: Challenge │ │                            │
│ │   → Quest 8: Finale    │ │                            │
│ └────────────────────────┘ │                            │
└────────────────────────────┴────────────────────────────┘
```

**Features**:
- **Chapter Organization**: Group quests by chapters
- **Quest List**: Show all quests with completion status
- **Progress Tracking**: Real-time progress updates
- **Rewards Display**: XP and Karma rewards
- **Action Buttons**: 
  - Start mission (if not started)
  - Complete mission (if all quests done)
  - Navigate to active quest
  - Share mission progress

### 3. Mission Creation Interface

**Layout** (Similar to Quest Builder but simpler):
```
┌─────────────────────────────────────────────────────────┐
│ Create New Mission                                       │
├─────────────────────────────────────────────────────────┤
│                                                          │
│ Step 1: Basic Info         [────●───] Step 2 of 3      │
│                                                          │
│ Name:                                                      │
│ ┌────────────────────────────────────────────────────┐ │
│ │ The Great Adventure                                 │ │
│ └────────────────────────────────────────────────────┘ │
│                                                          │
│ Description:                                             │
│ ┌────────────────────────────────────────────────────┐ │
│ │ An epic journey through multiple realms...         │ │
│ │                                                     │ │
│ └────────────────────────────────────────────────────┘ │
│                                                          │
│ Mission Type: [Easy ▼]  Difficulty: [Medium ▼]          │
│                                                          │
│                                                          │
│                    [← Back]  [Next →]                   │
├─────────────────────────────────────────────────────────┤
│ Step 2: Organization      [───●────] Step 2 of 3      │
│                                                          │
│ Quest Organization:                                      │
│ ○ Flat list (no chapters)                               │
│ ● Chapters (recommended for large missions)              │
│                                                          │
│ If using chapters:                                       │
│ ┌────────────────────────────────────────────────────┐ │
│ │ + Add Chapter                                       │ │
│ │                                                     │ │
│ │ Chapter 1: Beginning                                │ │
│ │   └─ Quest: "Start Your Journey"                    │ │
│ │   └─ Quest: "Meet Your Guide"                       │ │
│ │                                                     │ │
│ │ Chapter 2: Exploration                              │ │
│ │   └─ Quest: "Explore the Forest"                    │ │
│ │   └─ Quest: "Discover Hidden Paths"                 │ │
│ └────────────────────────────────────────────────────┘ │
│                                                          │
│                    [← Back]  [Next →]                   │
├─────────────────────────────────────────────────────────┤
│ Step 3: Rewards          [─────●───] Step 3 of 3      │
│                                                          │
│ Completion Rewards:                                      │
│                                                          │
│ XP:        [500     ]                                   │
│ Karma:     [1000    ]                                   │
│                                                          │
│                            [Create Mission]             │
└─────────────────────────────────────────────────────────┘
```

**Features**:
- **3-Step Wizard**: Simple creation flow
- **Basic Info**: Name, description, type, difficulty
- **Organization**: Choose flat or chapter-based structure
- **Quest Selection**: Link existing quests or create new ones
- **Chapter Management**: Add/remove/reorder chapters
- **Rewards**: Set XP and Karma rewards

### 4. Mission Timeline View (Alternative)

**Alternative visualization for complex missions**:
```
┌─────────────────────────────────────────────────────────┐
│ Mission Timeline: The Great Discovery                   │
├─────────────────────────────────────────────────────────┤
│                                                          │
│ Chapter 1: Beginning            [Completed ✓]           │
│ ────────────────────────────────────────────────────    │
│                                                          │
│     [✓] Quest 1: Start                                 │
│      │                                                   │
│      └─[✓] Quest 2: Explore                            │
│         │                                               │
│         └─[✓] Quest 3: Discover                        │
│                                                          │
│ Chapter 2: Journey              [Completed ✓]           │
│ ────────────────────────────────────────────────────    │
│                                                          │
│     [✓] Quest 4: Travel                                │
│      │                                                   │
│      └─[✓] Quest 5: Meet                               │
│         │                                               │
│         └─[→] Quest 6: Continue    [Active]            │
│                                                          │
│ Chapter 3: Climax               [In Progress]           │
│ ────────────────────────────────────────────────────    │
│                                                          │
│     [→] Quest 7: Challenge                             │
│      │                                                   │
│      └─[○] Quest 8: Finale                             │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

**Features**:
- **Visual Timeline**: Tree/graph view of mission structure
- **Chapter Separation**: Clear visual boundaries
- **Quest Dependencies**: Show quest relationships
- **Status Indicators**: Visual status per quest/chapter

## UI Components Needed

### Components
1. **MissionCard**: Reusable card component showing mission summary
2. **MissionProgressBar**: Progress visualization component
3. **QuestTimeline**: Timeline visualization for quests
4. **ChapterAccordion**: Collapsible chapter sections
5. **MissionCreator**: 3-step wizard for creating missions
6. **ChapterSelector**: Component for organizing quests into chapters
7. **MissionDetailView**: Full mission detail with chapters and quests
8. **MissionStatusBadge**: Status indicator badge

### API Integration Points
- `GET /api/missions` - List all missions
- `GET /api/missions/{id}` - Get mission details
- `POST /api/missions` - Create mission
- `PUT /api/missions/{id}` - Update mission
- `DELETE /api/missions/{id}` - Delete mission
- `POST /api/missions/{id}/start` - Start mission
- `POST /api/missions/{id}/complete` - Complete mission
- `GET /api/missions/{id}/quests` - Get quests for mission
- `GET /api/missions/{id}/chapters` - Get chapters for mission

## Design Principles

1. **Visual Hierarchy**: Clear visual distinction between missions, chapters, and quests
2. **Progress Clarity**: Always show progress clearly (percentage, visual indicators)
3. **Chapter Support**: Primary organization method for large missions
4. **Quest Integration**: Seamless integration with quest system
5. **Status Awareness**: Clear status indicators at all levels
6. **Timeline Visualization**: Visual representation of quest sequence
7. **Responsive Design**: Works on desktop and mobile
8. **Consistent Styling**: Matches portal design system

## Color Scheme

- **Mission Primary**: Pink/Magenta (rgba(236, 72, 153, ...))
- **Completed**: Green (rgba(34, 197, 94, ...))
- **Active**: Purple (rgba(168, 85, 247, ...))
- **Pending**: Gray (rgba(255, 255, 255, 0.1))

## Next Steps

1. Enhance existing `mission-interface.js` with chapter support
2. Create mission creation wizard similar to quest builder
3. Add chapter management UI components
4. Integrate with quest selection/creation
5. Add timeline visualization component
6. Connect to backend API endpoints
7. Add mission progress tracking
8. Implement mission completion flow

