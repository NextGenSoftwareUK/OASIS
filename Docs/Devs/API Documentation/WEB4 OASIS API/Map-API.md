# Map API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Map Management](#map-management)
- [Map Operations](#map-operations)
- [Map Visualization](#map-visualization)
- [Map Analytics](#map-analytics)
- [Error Responses](#error-responses)

## Overview

The Map API provides comprehensive mapping and geospatial services for the OASIS ecosystem. It handles map creation, management, visualization, and analytics with support for 2D/3D objects, sprites, routes, and real-time updates.

## Map Management

### Get All Maps
```http
GET /api/map/all
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `status` (string, optional): Filter by status (Active, Inactive, Archived)
- `type` (string, optional): Filter by type (2D, 3D, Hybrid)
- `sortBy` (string, optional): Sort field (name, createdAt, lastModified)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "maps": [
        {
          "id": "map_123",
          "name": "Main OASIS Map",
          "description": "Primary map for the OASIS ecosystem",
          "type": "3D",
          "status": "Active",
          "dimensions": {
            "width": 10000,
            "height": 10000,
            "depth": 1000
          },
          "center": {
            "x": 5000,
            "y": 5000,
            "z": 500
          },
          "zoom": {
            "min": 0.1,
            "max": 10.0,
            "default": 1.0
          },
          "objects": 150,
          "routes": 25,
          "createdAt": "2024-01-20T14:30:00Z",
          "lastModified": "2024-01-20T14:30:00Z"
        }
      ],
      "totalCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "Maps retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Map by ID
```http
GET /api/map/{mapId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `mapId` (string): Map UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "map_123",
      "name": "Main OASIS Map",
      "description": "Primary map for the OASIS ecosystem",
      "type": "3D",
      "status": "Active",
      "dimensions": {
        "width": 10000,
        "height": 10000,
        "depth": 1000
      },
      "center": {
        "x": 5000,
        "y": 5000,
        "z": 500
      },
      "zoom": {
        "min": 0.1,
        "max": 10.0,
        "default": 1.0
      },
      "objects": [
        {
          "id": "obj_123",
          "type": "3D",
          "name": "OASIS Tower",
          "position": {
            "x": 5000,
            "y": 5000,
            "z": 100
          },
          "rotation": {
            "x": 0,
            "y": 0,
            "z": 0
          },
          "scale": {
            "x": 1.0,
            "y": 1.0,
            "z": 1.0
          },
          "model": "https://example.com/models/oasis_tower.glb",
          "texture": "https://example.com/textures/oasis_tower.jpg"
        }
      ],
      "routes": [
        {
          "id": "route_123",
          "name": "Main Route",
          "start": {
            "x": 1000,
            "y": 1000,
            "z": 0
          },
          "end": {
            "x": 9000,
            "y": 9000,
            "z": 0
          },
          "waypoints": [
            {
              "x": 3000,
              "y": 3000,
              "z": 0
            },
            {
              "x": 6000,
              "y": 6000,
              "z": 0
            }
          ],
          "color": "#00ff00",
          "width": 5,
          "style": "solid"
        }
      ],
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Map retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Create Map
```http
POST /api/map/create
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "name": "New OASIS Map",
  "description": "A new map for the OASIS ecosystem",
  "type": "3D",
  "dimensions": {
    "width": 5000,
    "height": 5000,
    "depth": 500
  },
  "center": {
    "x": 2500,
    "y": 2500,
    "z": 250
  },
  "zoom": {
    "min": 0.1,
    "max": 5.0,
    "default": 1.0
  },
  "settings": {
    "lighting": "dynamic",
    "shadows": true,
    "fog": false,
    "skybox": "https://example.com/skyboxes/space.jpg"
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "map_124",
      "name": "New OASIS Map",
      "description": "A new map for the OASIS ecosystem",
      "type": "3D",
      "status": "Active",
      "dimensions": {
        "width": 5000,
        "height": 5000,
        "depth": 500
      },
      "center": {
        "x": 2500,
        "y": 2500,
        "z": 250
      },
      "zoom": {
        "min": 0.1,
        "max": 5.0,
        "default": 1.0
      },
      "settings": {
        "lighting": "dynamic",
        "shadows": true,
        "fog": false,
        "skybox": "https://example.com/skyboxes/space.jpg"
      },
      "objects": [],
      "routes": [],
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "Map created successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update Map
```http
PUT /api/map/{mapId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `mapId` (string): Map UUID

**Request Body:**
```json
{
  "name": "Updated OASIS Map",
  "description": "Updated description for the OASIS map",
  "dimensions": {
    "width": 7500,
    "height": 7500,
    "depth": 750
  },
  "settings": {
    "lighting": "realistic",
    "shadows": true,
    "fog": true,
    "skybox": "https://example.com/skyboxes/earth.jpg"
  }
}
```

### Delete Map
```http
DELETE /api/map/{mapId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `mapId` (string): Map UUID

## Map Operations

### Set Map Provider
```http
POST /api/map/setprovider/{providerType}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `providerType` (string): Map provider type (OpenStreetMap, GoogleMaps, Mapbox, Custom)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "providerType": "OpenStreetMap",
      "status": "Active",
      "config": {
        "apiKey": "encrypted_key",
        "baseUrl": "https://tile.openstreetmap.org",
        "maxZoom": 18,
        "minZoom": 0
      },
      "setAt": "2024-01-20T14:30:00Z"
    },
    "message": "Map provider set successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Draw 3D Object
```http
POST /api/map/draw3dobject
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "mapId": "map_123",
  "objectPath": "https://example.com/models/building.glb",
  "position": {
    "x": 1000,
    "y": 1000,
    "z": 0
  },
  "rotation": {
    "x": 0,
    "y": 0,
    "z": 0
  },
  "scale": {
    "x": 1.0,
    "y": 1.0,
    "z": 1.0
  },
  "name": "Building A",
  "description": "A 3D building model"
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "objectId": "obj_124",
      "mapId": "map_123",
      "type": "3D",
      "name": "Building A",
      "description": "A 3D building model",
      "objectPath": "https://example.com/models/building.glb",
      "position": {
        "x": 1000,
        "y": 1000,
        "z": 0
      },
      "rotation": {
        "x": 0,
        "y": 0,
        "z": 0
      },
      "scale": {
        "x": 1.0,
        "y": 1.0,
        "z": 1.0
      },
      "createdAt": "2024-01-20T14:30:00Z"
    },
    "message": "3D object drawn successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Draw 2D Sprite
```http
POST /api/map/draw2dsprite
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "mapId": "map_123",
  "spritePath": "https://example.com/sprites/icon.png",
  "position": {
    "x": 2000,
    "y": 2000,
    "z": 0
  },
  "size": {
    "width": 64,
    "height": 64
  },
  "name": "Icon A",
  "description": "A 2D sprite icon"
}
```

### Draw 2D Sprite on HUD
```http
POST /api/map/draw2dspriteonhud
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "spritePath": "https://example.com/sprites/hud_icon.png",
  "position": {
    "x": 10,
    "y": 10
  },
  "size": {
    "width": 32,
    "height": 32
  },
  "name": "HUD Icon",
  "description": "A HUD sprite icon"
}
```

### Place Holon
```http
POST /api/map/placeHolon
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "mapId": "map_123",
  "holonId": "holon_123",
  "position": {
    "x": 3000,
    "y": 3000,
    "z": 0
  },
  "rotation": {
    "x": 0,
    "y": 0,
    "z": 0
  },
  "scale": {
    "x": 1.0,
    "y": 1.0,
    "z": 1.0
  }
}
```

### Place Building
```http
POST /api/map/placeBuilding
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "mapId": "map_123",
  "buildingId": "building_123",
  "position": {
    "x": 4000,
    "y": 4000,
    "z": 0
  },
  "rotation": {
    "x": 0,
    "y": 0,
    "z": 0
  },
  "scale": {
    "x": 1.0,
    "y": 1.0,
    "z": 1.0
  }
}
```

### Place Quest
```http
POST /api/map/placeQuest
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "mapId": "map_123",
  "questId": "quest_123",
  "position": {
    "x": 5000,
    "y": 5000,
    "z": 0
  },
  "rotation": {
    "x": 0,
    "y": 0,
    "z": 0
  },
  "scale": {
    "x": 1.0,
    "y": 1.0,
    "z": 1.0
  }
}
```

### Place GeoNFT
```http
POST /api/map/placeGeoNFT
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "mapId": "map_123",
  "geoNFTId": "geonft_123",
  "position": {
    "x": 6000,
    "y": 6000,
    "z": 0
  },
  "rotation": {
    "x": 0,
    "y": 0,
    "z": 0
  },
  "scale": {
    "x": 1.0,
    "y": 1.0,
    "z": 1.0
  }
}
```

### Place GeoHotSpot
```http
POST /api/map/placeGeoHotSpot
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "mapId": "map_123",
  "geoHotSpotId": "geohotspot_123",
  "position": {
    "x": 7000,
    "y": 7000,
    "z": 0
  },
  "rotation": {
    "x": 0,
    "y": 0,
    "z": 0
  },
  "scale": {
    "x": 1.0,
    "y": 1.0,
    "z": 1.0
  }
}
```

### Place OAPP
```http
POST /api/map/placeOAPP
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "mapId": "map_123",
  "oappId": "oapp_123",
  "position": {
    "x": 8000,
    "y": 8000,
    "z": 0
  },
  "rotation": {
    "x": 0,
    "y": 0,
    "z": 0
  },
  "scale": {
    "x": 1.0,
    "y": 1.0,
    "z": 1.0
  }
}
```

## Map Visualization

### Pan Map Left
```http
POST /api/map/panLeft
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "action": "panLeft",
      "mapId": "map_123",
      "newPosition": {
        "x": 4900,
        "y": 5000,
        "z": 500
      },
      "panDistance": 100,
      "timestamp": "2024-01-20T14:30:00Z"
    },
    "message": "Map panned left successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Pan Map Right
```http
POST /api/map/panRight
Authorization: Bearer YOUR_TOKEN
```

### Pan Map Up
```http
POST /api/map/panUp
Authorization: Bearer YOUR_TOKEN
```

### Pan Map Down
```http
POST /api/map/panDown
Authorization: Bearer YOUR_TOKEN
```

### Zoom Out
```http
POST /api/map/zoomOut
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "action": "zoomOut",
      "mapId": "map_123",
      "newZoom": 0.8,
      "previousZoom": 1.0,
      "zoomFactor": 0.8,
      "timestamp": "2024-01-20T14:30:00Z"
    },
    "message": "Map zoomed out successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Zoom In
```http
POST /api/map/zoomIn
Authorization: Bearer YOUR_TOKEN
```

### Zoom to Holon
```http
POST /api/map/zoomToHolon
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "mapId": "map_123",
  "holonId": "holon_123",
  "zoomLevel": 2.0
}
```

### Zoom to Building
```http
POST /api/map/zoomToBuilding
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "mapId": "map_123",
  "buildingId": "building_123",
  "zoomLevel": 2.0
}
```

### Zoom to Quest
```http
POST /api/map/zoomToQuest
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "mapId": "map_123",
  "questId": "quest_123",
  "zoomLevel": 2.0
}
```

### Zoom to GeoNFT
```http
POST /api/map/zoomToGeoNFT
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "mapId": "map_123",
  "geoNFTId": "geonft_123",
  "zoomLevel": 2.0
}
```

### Zoom to GeoHotSpot
```http
POST /api/map/zoomToGeoHotSpot
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "mapId": "map_123",
  "geoHotSpotId": "geohotspot_123",
  "zoomLevel": 2.0
}
```

### Zoom to OAPP
```http
POST /api/map/zoomToOAPP
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "mapId": "map_123",
  "oappId": "oapp_123",
  "zoomLevel": 2.0
}
```

### Zoom to Coordinates
```http
POST /api/map/zoomToCoords
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "mapId": "map_123",
  "coordinates": {
    "x": 5000,
    "y": 5000,
    "z": 0
  },
  "zoomLevel": 2.0
}
```

## Map Analytics

### Get Map Statistics
```http
GET /api/map/{mapId}/stats
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `mapId` (string): Map UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "mapId": "map_123",
      "name": "Main OASIS Map",
      "statistics": {
        "objects": {
          "total": 150,
          "3D": 100,
          "2D": 50,
          "byType": {
            "buildings": 75,
            "vehicles": 25,
            "decorations": 50
          }
        },
        "routes": {
          "total": 25,
          "active": 20,
          "inactive": 5,
          "totalLength": 50000
        },
        "usage": {
          "views": 15000,
          "uniqueViewers": 5000,
          "averageSessionTime": 1800,
          "lastViewed": "2024-01-20T14:30:00Z"
        },
        "performance": {
          "averageLoadTime": 2.5,
          "peakLoadTime": 5.0,
          "errorRate": 0.001,
          "availability": 99.9
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Map statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Map Performance
```http
GET /api/map/{mapId}/performance
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `mapId` (string): Map UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "mapId": "map_123",
      "performance": {
        "averageLoadTime": 2.5,
        "peakLoadTime": 5.0,
        "averageRenderTime": 16.7,
        "peakRenderTime": 33.3,
        "fps": 60,
        "memoryUsage": 512,
        "cpuUsage": 25.5
      },
      "metrics": {
        "requestsPerSecond": 100,
        "averageLatency": 2.5,
        "p95Latency": 4.0,
        "p99Latency": 5.0,
        "errorRate": 0.001,
        "successRate": 0.999
      },
      "trends": {
        "loadTime": "stable",
        "renderTime": "stable",
        "fps": "stable",
        "memoryUsage": "increasing"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Map performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get Map Health
```http
GET /api/map/{mapId}/health
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `mapId` (string): Map UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "mapId": "map_123",
      "status": "Healthy",
      "overallHealth": 0.95,
      "components": {
        "rendering": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "objects": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "routes": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "performance": {
          "status": "Warning",
          "health": 0.85,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Rendering Test",
          "status": "Pass",
          "responseTime": 16.7,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Objects Test",
          "status": "Pass",
          "responseTime": 2.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Routes Test",
          "status": "Pass",
          "responseTime": 1.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Performance Test",
          "status": "Warning",
          "responseTime": 5.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [
        {
          "type": "High Memory Usage",
          "severity": "Medium",
          "message": "Memory usage is above 80%",
          "createdAt": "2024-01-20T14:30:00Z"
        }
      ],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "Map health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Error Responses

### Map Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Map not found",
  "exception": "Map with ID map_123 not found"
}
```

### Invalid Coordinates
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid coordinates",
  "exception": "Coordinates must be within map bounds"
}
```

### Object Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Object not found",
  "exception": "Object with ID obj_123 not found"
}
```

### Route Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "Route not found",
  "exception": "Route with ID route_123 not found"
}
```

### Provider Not Supported
```json
{
  "result": null,
  "isError": true,
  "message": "Provider not supported",
  "exception": "Provider 'CustomProvider' is not supported"
}
```

---

## Navigation

**← Previous:** [Settings API](Settings-API.md) | **Next:** [Chat API](Chat-API.md) →
