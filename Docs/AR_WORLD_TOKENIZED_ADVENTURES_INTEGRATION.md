# AR World Tokenized Adventures Integration Guide

## Overview

This document explains how to integrate tokenized adventures into the AR World game using the STAR API's Missions and Quests endpoints. Tokenized adventures are location-based, gamified experiences that can be minted as NFTs, placed in real-world locations, and completed by players for rewards.

## Architecture

### Components

1. **STAR API** - Backend API providing missions and quests management
2. **AR World Unity Game** - Mobile AR application that displays and manages adventures
3. **GeoNFT System** - Location-based NFTs that represent adventure locations
4. **Mission/Quest System** - Structured objectives and challenges

## STAR API Endpoints

### Base URL
- **Development**: `http://localhost:50564/api`
- **Production**: `https://star-api.oasisweb4.com/api`

### Authentication
```http
Authorization: Bearer {JWT_TOKEN}
```
or
```http
Authorization: Avatar {AVATAR_ID}
```

## Missions API Integration

### 1. Create a Tokenized Adventure Mission

Missions are high-level adventure containers that can contain multiple quests or chapters.

**Endpoint**: `POST /api/missions`

**Request Body**:
```json
{
  "name": "London Historical Adventure",
  "description": "Explore London's historical landmarks and complete challenges",
  "type": "Quest",
  "difficulty": "Medium",
  "objectives": [
    {
      "description": "Visit Big Ben",
      "type": "Location",
      "target": {
        "lat": 51.4994,
        "long": -0.1245,
        "radius": 50
      }
    },
    {
      "description": "Collect 5 historical artifacts",
      "type": "Collection",
      "target": 5
    }
  ],
  "rewards": {
    "karma": 500,
    "experience": 250,
    "currency": {
      "amount": 100.0,
      "type": "STAR"
    },
    "items": ["London Explorer Badge", "Historical Artifact Pack"]
  },
  "requirements": {
    "level": 5,
    "prerequisites": [],
    "timeLimit": 7,
    "maxParticipants": 1000
  },
  "metadata": {
    "tags": ["historical", "london", "ar-adventure"],
    "category": "Location-Based",
    "version": "1.0",
    "geoLocation": {
      "centerLat": 51.5074,
      "centerLong": -0.1278,
      "radius": 5000
    }
  }
}
```

**Response**:
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "mission_123",
      "name": "London Historical Adventure",
      "status": "Active",
      "createdAt": "2024-01-20T14:30:00Z"
    }
  }
}
```

### 2. Get Available Missions for AR World

**Endpoint**: `GET /api/missions?type=Quest&status=Active&limit=50`

**Query Parameters**:
- `limit` - Number of results (default: 50)
- `offset` - Pagination offset
- `type` - Filter by type (Quest, Challenge, Task, Achievement)
- `status` - Filter by status (Active, Inactive, Completed)
- `difficulty` - Filter by difficulty (Easy, Medium, Hard, Expert)
- `sortBy` - Sort field (name, createdAt, difficulty, rewards)

**Use Case**: AR World can fetch nearby missions based on player location:
```csharp
// Unity C# Example
public async Task<List<Mission>> GetNearbyMissions(double lat, double lng, double radius)
{
    var response = await httpClient.GetAsync(
        $"{baseUrl}/api/missions?status=Active&limit=50"
    );
    var missions = await response.Content.ReadFromJsonAsync<OASISResult<List<Mission>>>();
    
    // Filter by location
    return missions.Result.Where(m => 
        IsWithinRadius(m.Metadata.GeoLocation, lat, lng, radius)
    ).ToList();
}
```

### 3. Start a Mission

**Endpoint**: `POST /api/missions/{missionId}/start`

**Response**:
```json
{
  "result": {
    "success": true,
    "data": {
      "missionId": "mission_123",
      "status": "Active",
      "startedAt": "2024-01-20T14:30:00Z",
      "deadline": "2024-01-27T14:30:00Z",
      "progress": {
        "completed": 0,
        "total": 2,
        "percentage": 0.0
      }
    }
  }
}
```

### 4. Track Mission Progress

**Endpoint**: `GET /api/missions/{missionId}/progress`

**Response**:
```json
{
  "result": {
    "success": true,
    "data": {
      "missionId": "mission_123",
      "status": "Active",
      "progress": {
        "completed": 1,
        "total": 2,
        "percentage": 50.0
      },
      "objectives": [
        {
          "id": "obj_123",
          "description": "Visit Big Ben",
          "type": "Location",
          "status": "Completed",
          "completedAt": "2024-01-20T14:30:00Z"
        },
        {
          "id": "obj_124",
          "description": "Collect 5 historical artifacts",
          "type": "Collection",
          "status": "Pending",
          "current": 2,
          "target": 5
        }
      ],
      "timeRemaining": 6.5
    }
  }
}
```

### 5. Complete Mission

**Endpoint**: `POST /api/missions/{missionId}/complete`

**Response**:
```json
{
  "result": {
    "success": true,
    "data": {
      "missionId": "mission_123",
      "status": "Completed",
      "completedAt": "2024-01-20T14:30:00Z",
      "rewards": {
        "karma": 500,
        "experience": 250,
        "currency": {
          "amount": 100.0,
          "type": "STAR"
        },
        "items": ["London Explorer Badge", "Historical Artifact Pack"]
      }
    }
  }
}
```

## Quests API Integration

### 1. Create a Quest (Individual Adventure Step)

Quests are individual challenges within a mission or standalone adventures.

**Endpoint**: `POST /api/quests`

**Request Body**:
```json
{
  "name": "Big Ben Discovery",
  "description": "Find and interact with Big Ben in AR",
  "type": "Main",
  "difficulty": "Easy",
  "objectives": [
    {
      "description": "Reach Big Ben location",
      "type": "Location",
      "target": {
        "lat": 51.4994,
        "long": -0.1245,
        "radius": 50
      }
    },
    {
      "description": "Scan AR marker at Big Ben",
      "type": "ARInteraction",
      "target": "big_ben_marker_001"
    },
    {
      "description": "Answer historical question",
      "type": "Quiz",
      "target": "question_001"
    }
  ],
  "rewards": {
    "karma": 100,
    "experience": 50,
    "currency": {
      "amount": 10.0,
      "type": "STAR"
    },
    "items": ["Big Ben Badge"]
  },
  "requirements": {
    "level": 1,
    "prerequisites": [],
    "timeLimit": 60,
    "maxParticipants": 1000
  },
  "metadata": {
    "tags": ["big-ben", "london", "ar"],
    "category": "Landmark",
    "geoLocation": {
      "lat": 51.4994,
      "long": -0.1245,
      "radius": 50
    },
    "arContent": {
      "markerId": "big_ben_marker_001",
      "3dModel": "https://cdn.example.com/models/big_ben.glb",
      "audioGuide": "https://cdn.example.com/audio/big_ben.mp3"
    }
  },
  "parentMissionId": "mission_123"
}
```

### 2. Get Quests by Location

**Endpoint**: `GET /api/quests/search?query=london`

**Use Case**: AR World can search for quests near player location:
```csharp
// Unity C# Example
public async Task<List<Quest>> GetQuestsNearLocation(double lat, double lng, double radius)
{
    // First, get all active quests
    var response = await httpClient.GetAsync(
        $"{baseUrl}/api/quests?status=Active&limit=100"
    );
    var quests = await response.Content.ReadFromJsonAsync<OASISResult<List<Quest>>>();
    
    // Filter by location proximity
    return quests.Result.Where(q => 
        CalculateDistance(
            q.Metadata.GeoLocation.Lat, 
            q.Metadata.GeoLocation.Long, 
            lat, lng
        ) <= radius
    ).ToList();
}
```

### 3. Start Quest

**Endpoint**: `POST /api/quests/{questId}/start`

### 4. Update Quest Progress

When a player completes an objective in AR World:

```csharp
// Unity C# Example
public async Task UpdateQuestObjective(string questId, string objectiveId, bool completed)
{
    // Update local quest state
    var quest = GetLocalQuest(questId);
    var objective = quest.Objectives.First(o => o.Id == objectiveId);
    objective.Status = completed ? "Completed" : "InProgress";
    
    // Sync with server when objective is completed
    if (completed)
    {
        await httpClient.PostAsync(
            $"{baseUrl}/api/quests/{questId}/objectives/{objectiveId}/complete",
            null
        );
    }
}
```

### 5. Complete Quest

**Endpoint**: `POST /api/quests/{questId}/complete`

## Tokenization: Creating NFT Adventures

### Step 1: Create Mission/Quest as Adventure

First, create the adventure structure using the Missions or Quests API as shown above.

### Step 2: Mint as NFT

Adventures can be tokenized as NFTs using the OASIS NFT API:

**Endpoint**: `POST /api/nft/mint`

**Request Body**:
```json
{
  "title": "London Historical Adventure NFT",
  "description": "A tokenized AR adventure exploring London's history",
  "imageUrl": "https://cdn.example.com/adventures/london-adventure.jpg",
  "metadata": {
    "missionId": "mission_123",
    "adventureType": "LocationBased",
    "location": {
      "lat": 51.5074,
      "long": -0.1278,
      "city": "London",
      "country": "UK"
    },
    "difficulty": "Medium",
    "estimatedDuration": "2 hours",
    "rewards": {
      "karma": 500,
      "experience": 250,
      "currency": 100.0
    }
  },
  "onChainProvider": "SolanaOASIS",
  "offChainProvider": "MongoDBOASIS",
  "nftStandardType": "SPL",
  "numberToMint": 1
}
```

### Step 3: Place as GeoNFT in AR World

Once minted, place the adventure NFT at a real-world location:

**Endpoint**: `POST /api/nft/place-geo-nft`

**Request Body**:
```json
{
  "originalOASISNFTId": "nft_guid_here",
  "lat": 51507400,  // Micro-degrees (51.5074 * 1000000)
  "long": -127800,  // Micro-degrees (-0.1278 * 1000000)
  "allowOtherPlayersToAlsoCollect": true,
  "permSpawn": true,
  "globalSpawnQuantity": 1,
  "playerSpawnQuantity": 1,
  "respawnDurationInSeconds": 0,
  "geoNFTMetaDataProvider": "MongoDBOASIS",
  "placedByAvatarId": "avatar_guid_here"
}
```

## AR World Integration Flow

### 1. Game Initialization

```csharp
public class ARWorldAdventureManager : MonoBehaviour
{
    private string starApiBaseUrl = "https://star-api.oasisweb4.com/api";
    private string authToken;
    private string avatarId;
    
    private List<Mission> availableMissions = new List<Mission>();
    private List<Quest> nearbyQuests = new List<Quest>();
    private Dictionary<string, Mission> activeMissions = new Dictionary<string, Mission>();
    
    async void Start()
    {
        // Authenticate with STAR API
        await Authenticate();
        
        // Load nearby adventures based on GPS location
        await LoadNearbyAdventures();
        
        // Display adventures on AR map
        DisplayAdventuresOnMap();
    }
}
```

### 2. Location-Based Adventure Discovery

```csharp
public async Task LoadNearbyAdventures()
{
    // Get player's current location
    var location = await GetPlayerLocation();
    
    // Fetch missions within radius
    var missionsResponse = await httpClient.GetAsync(
        $"{starApiBaseUrl}/missions?status=Active&limit=50"
    );
    var missions = await missionsResponse.Content.ReadFromJsonAsync<OASISResult<List<Mission>>>();
    
    // Filter by location
    availableMissions = missions.Result.Where(m => 
        IsWithinRadius(
            m.Metadata.GeoLocation, 
            location.Latitude, 
            location.Longitude, 
            5000 // 5km radius
        )
    ).ToList();
    
    // Fetch quests
    var questsResponse = await httpClient.GetAsync(
        $"{starApiBaseUrl}/quests?status=Active&limit=100"
    );
    var quests = await questsResponse.Content.ReadFromJsonAsync<OASISResult<List<Quest>>>();
    
    nearbyQuests = quests.Result.Where(q => 
        CalculateDistance(
            q.Metadata.GeoLocation.Lat,
            q.Metadata.GeoLocation.Long,
            location.Latitude,
            location.Longitude
        ) <= 1000 // 1km radius for quests
    ).ToList();
}
```

### 3. Starting an Adventure

```csharp
public async Task StartAdventure(string missionId)
{
    // Start mission via API
    var response = await httpClient.PostAsync(
        $"{starApiBaseUrl}/missions/{missionId}/start",
        null
    );
    
    var result = await response.Content.ReadFromJsonAsync<OASISResult<MissionProgress>>();
    
    if (result.IsError)
    {
        Debug.LogError($"Failed to start mission: {result.Message}");
        return;
    }
    
    // Store active mission locally
    activeMissions[missionId] = result.Result.Mission;
    
    // Display mission UI
    ShowMissionUI(result.Result);
    
    // Load quests for this mission
    await LoadMissionQuests(missionId);
}
```

### 4. Tracking Progress in AR

```csharp
public async Task UpdateAdventureProgress(string missionId, string objectiveId)
{
    // Update objective status
    var objective = activeMissions[missionId].Objectives
        .First(o => o.Id == objectiveId);
    
    objective.Status = "Completed";
    objective.CompletedAt = DateTime.UtcNow;
    
    // Check if all objectives are complete
    var allComplete = activeMissions[missionId].Objectives
        .All(o => o.Status == "Completed");
    
    if (allComplete)
    {
        // Complete the mission
        await CompleteMission(missionId);
    }
    else
    {
        // Update progress on server
        await httpClient.PostAsync(
            $"{starApiBaseUrl}/missions/{missionId}/objectives/{objectiveId}/complete",
            null
        );
    }
}
```

### 5. Completing and Rewarding

```csharp
public async Task CompleteMission(string missionId)
{
    var response = await httpClient.PostAsync(
        $"{starApiBaseUrl}/missions/{missionId}/complete",
        null
    );
    
    var result = await response.Content.ReadFromJsonAsync<OASISResult<MissionCompletion>>();
    
    if (!result.IsError)
    {
        // Display rewards
        ShowRewardsUI(result.Result.Rewards);
        
        // Update player stats
        UpdatePlayerStats(result.Result.Rewards);
        
        // Remove from active missions
        activeMissions.Remove(missionId);
    }
}
```

## GeoNFT Integration for AR Display

### Fetching GeoNFTs for AR World

**Endpoint**: `GET /api/nft/get-nearby-geo-nfts?lat={lat}&long={lng}&radius={radius}`

**Response**:
```json
{
  "result": {
    "success": true,
    "data": [
      {
        "id": "geonft_123",
        "originalOASISNFTId": "nft_456",
        "lat": 51507400,
        "long": -127800,
        "nft3DObjectURI": "https://cdn.example.com/models/adventure_marker.glb",
        "nft2DSpriteURI": "https://cdn.example.com/sprites/adventure_icon.png",
        "metadata": {
          "missionId": "mission_123",
          "adventureName": "London Historical Adventure"
        }
      }
    ]
  }
}
```

### Displaying GeoNFTs in AR

```csharp
public async Task DisplayGeoNFTsInAR()
{
    var location = await GetPlayerLocation();
    
    var response = await httpClient.GetAsync(
        $"{starApiBaseUrl}/nft/get-nearby-geo-nfts?" +
        $"lat={location.Latitude * 1000000}&" +
        $"long={location.Longitude * 1000000}&" +
        $"radius=1000"
    );
    
    var geonfts = await response.Content.ReadFromJsonAsync<OASISResult<List<GeoNFT>>>();
    
    foreach (var geonft in geonfts.Result)
    {
        // Calculate AR world position
        var arPosition = CalculateARPosition(
            geonft.Lat / 1000000.0,
            geonft.Long / 1000000.0,
            location
        );
        
        // Spawn 3D marker in AR
        var marker = Instantiate(adventureMarkerPrefab, arPosition, Quaternion.identity);
        marker.GetComponent<AdventureMarker>().Initialize(geonft);
    }
}
```

## Adventure Types Supported

### 1. Location-Based Adventures
- Player must visit specific GPS coordinates
- Triggered when within radius
- Can include AR markers or 3D objects

### 2. Collection Adventures
- Collect items/NFTs at various locations
- Track collection progress
- Reward upon completion

### 3. Puzzle Adventures
- Solve puzzles at locations
- Unlock next location/objective
- Progressive difficulty

### 4. Social Adventures
- Multi-player challenges
- Team objectives
- Leaderboards

### 5. Story-Driven Adventures
- Narrative missions with chapters
- Branching storylines
- Character interactions

## Best Practices

### 1. Caching
- Cache mission/quest data locally
- Sync periodically with server
- Handle offline mode gracefully

### 2. Location Services
- Request appropriate permissions
- Handle location accuracy
- Use geofencing for proximity detection

### 3. AR Performance
- LOD (Level of Detail) for 3D models
- Object pooling for markers
- Efficient distance calculations

### 4. Error Handling
- Network retry logic
- Graceful degradation
- User-friendly error messages

### 5. Security
- Validate all API responses
- Sanitize user inputs
- Secure token storage

## Example: Complete Adventure Flow

```csharp
public class AdventureFlow
{
    // 1. Player opens AR World app
    // 2. App authenticates with STAR API
    // 3. App gets player location
    // 4. App fetches nearby missions/quests
    // 5. App displays adventures on map
    // 6. Player selects an adventure
    // 7. App starts mission via API
    // 8. App displays objectives
    // 9. Player navigates to location
    // 10. App detects proximity
    // 11. App spawns AR content
    // 12. Player interacts with AR content
    // 13. App updates objective progress
    // 14. When all objectives complete, app completes mission
    // 15. App displays rewards
    // 16. Player stats updated
}
```

## API Rate Limits

- **Missions API**: 100 requests/minute
- **Quests API**: 100 requests/minute
- **GeoNFT API**: 50 requests/minute

Implement appropriate rate limiting and caching strategies.

## Conclusion

The STAR API Missions and Quests endpoints provide a comprehensive system for creating, managing, and tracking tokenized adventures in AR World. By combining location-based GeoNFTs with structured mission/quest objectives, developers can create engaging, gamified AR experiences that reward players with karma, experience, currency, and collectible items.

The tokenization aspect allows adventures to be:
- **Tradable**: Adventures can be bought/sold as NFTs
- **Unique**: Each adventure can have limited editions
- **Verifiable**: Blockchain verification of completion
- **Monetizable**: Creators can earn from adventure sales

This integration enables a new paradigm of location-based, tokenized gaming experiences in AR World.
