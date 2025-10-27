# Files API

## 📋 **Table of Contents**

- [Overview](#overview)
- [File Management](#file-management)
- [File Operations](#file-operations)
- [File Sharing](#file-sharing)
- [File Analytics](#file-analytics)
- [Error Responses](#error-responses)

## Overview

The Files API provides comprehensive file management services for the OASIS ecosystem. It handles file upload, download, storage, sharing, and analytics with support for multiple file types, encryption, and real-time collaboration.

## File Management

### Get All Files
```http
GET /api/files
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `limit` (int, optional): Number of results (default: 50)
- `offset` (int, optional): Number to skip (default: 0)
- `type` (string, optional): Filter by file type (image, document, video, audio, archive)
- `status` (string, optional): Filter by status (Active, Archived, Deleted)
- `owner` (string, optional): Filter by owner ID
- `sortBy` (string, optional): Sort field (name, size, createdAt, lastModified)
- `sortOrder` (string, optional): Sort order (asc/desc, default: desc)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "files": [
        {
          "id": "file_123",
          "name": "document.pdf",
          "type": "document",
          "mimeType": "application/pdf",
          "size": 1024000,
          "status": "Active",
          "owner": {
            "id": "user_123",
            "username": "john_doe"
          },
          "url": "https://files.oasisplatform.world/file_123",
          "thumbnail": "https://files.oasisplatform.world/file_123/thumbnail",
          "metadata": {
            "title": "Project Document",
            "description": "Important project documentation",
            "tags": ["project", "documentation"],
            "encryption": true,
            "compression": false
          },
          "permissions": {
            "read": true,
            "write": true,
            "delete": true,
            "share": true
          },
          "createdAt": "2024-01-20T14:30:00Z",
          "lastModified": "2024-01-20T14:30:00Z"
        }
      ],
      "totalCount": 1,
      "limit": 50,
      "offset": 0
    },
    "message": "Files retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get File by ID
```http
GET /api/files/{fileId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `fileId` (string): File UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "file_123",
      "name": "document.pdf",
      "type": "document",
      "mimeType": "application/pdf",
      "size": 1024000,
      "status": "Active",
      "owner": {
        "id": "user_123",
        "username": "john_doe"
      },
      "url": "https://files.oasisplatform.world/file_123",
      "thumbnail": "https://files.oasisplatform.world/file_123/thumbnail",
      "metadata": {
        "title": "Project Document",
        "description": "Important project documentation",
        "tags": ["project", "documentation"],
        "encryption": true,
        "compression": false,
        "checksum": "sha256:abc123def456",
        "version": "1.0"
      },
      "permissions": {
        "read": true,
        "write": true,
        "delete": true,
        "share": true
      },
      "sharing": {
        "public": false,
        "sharedWith": [
          {
            "id": "user_456",
            "username": "jane_smith",
            "permissions": ["read"]
          }
        ],
        "shareUrl": null
      },
      "analytics": {
        "downloads": 25,
        "views": 100,
        "lastAccessed": "2024-01-20T14:30:00Z"
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "File retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Upload File
```http
POST /api/files/upload
Content-Type: multipart/form-data
Authorization: Bearer YOUR_TOKEN
```

**Request Body:**
```json
{
  "file": "binary_file_data",
  "name": "document.pdf",
  "metadata": {
    "title": "Project Document",
    "description": "Important project documentation",
    "tags": ["project", "documentation"],
    "encryption": true,
    "compression": false
  },
  "permissions": {
    "read": true,
    "write": true,
    "delete": true,
    "share": true
  }
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "file_124",
      "name": "document.pdf",
      "type": "document",
      "mimeType": "application/pdf",
      "size": 1024000,
      "status": "Active",
      "owner": {
        "id": "user_123",
        "username": "john_doe"
      },
      "url": "https://files.oasisplatform.world/file_124",
      "thumbnail": "https://files.oasisplatform.world/file_124/thumbnail",
      "metadata": {
        "title": "Project Document",
        "description": "Important project documentation",
        "tags": ["project", "documentation"],
        "encryption": true,
        "compression": false,
        "checksum": "sha256:def456ghi789",
        "version": "1.0"
      },
      "permissions": {
        "read": true,
        "write": true,
        "delete": true,
        "share": true
      },
      "sharing": {
        "public": false,
        "sharedWith": [],
        "shareUrl": null
      },
      "analytics": {
        "downloads": 0,
        "views": 0,
        "lastAccessed": null
      },
      "createdAt": "2024-01-20T14:30:00Z",
      "lastModified": "2024-01-20T14:30:00Z"
    },
    "message": "File uploaded successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update File
```http
PUT /api/files/{fileId}
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `fileId` (string): File UUID

**Request Body:**
```json
{
  "name": "updated_document.pdf",
  "metadata": {
    "title": "Updated Project Document",
    "description": "Updated project documentation",
    "tags": ["project", "documentation", "updated"],
    "encryption": true,
    "compression": false
  },
  "permissions": {
    "read": true,
    "write": true,
    "delete": true,
    "share": false
  }
}
```

### Delete File
```http
DELETE /api/files/{fileId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `fileId` (string): File UUID

## File Operations

### Download File
```http
GET /api/files/{fileId}/download
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `fileId` (string): File UUID

**Query Parameters:**
- `format` (string, optional): Download format (original, compressed, thumbnail)
- `quality` (string, optional): Quality for images/videos (low, medium, high)

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "file_123",
      "name": "document.pdf",
      "type": "document",
      "mimeType": "application/pdf",
      "size": 1024000,
      "url": "https://files.oasisplatform.world/file_123/download",
      "expiresAt": "2024-01-20T15:30:00Z",
      "downloadToken": "download_token_123",
      "analytics": {
        "downloads": 26,
        "views": 101,
        "lastAccessed": "2024-01-20T14:30:00Z"
      }
    },
    "message": "File download prepared successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get File Content
```http
GET /api/files/{fileId}/content
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `fileId` (string): File UUID

**Query Parameters:**
- `format` (string, optional): Content format (text, json, xml, base64)
- `encoding` (string, optional): Text encoding (utf-8, ascii, latin1)

### Get File Thumbnail
```http
GET /api/files/{fileId}/thumbnail
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `fileId` (string): File UUID

**Query Parameters:**
- `size` (string, optional): Thumbnail size (small, medium, large)
- `format` (string, optional): Thumbnail format (jpg, png, webp)

### Get File Metadata
```http
GET /api/files/{fileId}/metadata
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `fileId` (string): File UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "file_123",
      "metadata": {
        "title": "Project Document",
        "description": "Important project documentation",
        "tags": ["project", "documentation"],
        "encryption": true,
        "compression": false,
        "checksum": "sha256:abc123def456",
        "version": "1.0",
        "createdBy": "user_123",
        "lastModifiedBy": "user_123"
      },
      "technical": {
        "mimeType": "application/pdf",
        "size": 1024000,
        "encoding": "binary",
        "compression": "none",
        "encryption": "AES-256"
      },
      "permissions": {
        "read": true,
        "write": true,
        "delete": true,
        "share": true
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "File metadata retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Update File Metadata
```http
PUT /api/files/{fileId}/metadata
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `fileId` (string): File UUID

**Request Body:**
```json
{
  "title": "Updated Project Document",
  "description": "Updated project documentation",
  "tags": ["project", "documentation", "updated"],
  "encryption": true,
  "compression": false
}
```

## File Sharing

### Share File
```http
POST /api/files/{fileId}/share
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `fileId` (string): File UUID

**Request Body:**
```json
{
  "type": "public",
  "permissions": ["read"],
  "expiresAt": "2024-01-27T14:30:00Z",
  "password": "optional_password",
  "notify": true
}
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "file_123",
      "sharing": {
        "type": "public",
        "shareUrl": "https://files.oasisplatform.world/share/abc123def456",
        "permissions": ["read"],
        "expiresAt": "2024-01-27T14:30:00Z",
        "password": "optional_password",
        "notify": true
      },
      "analytics": {
        "downloads": 25,
        "views": 100,
        "lastAccessed": "2024-01-20T14:30:00Z"
      },
      "sharedAt": "2024-01-20T14:30:00Z"
    },
    "message": "File shared successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Share File with Users
```http
POST /api/files/{fileId}/share/users
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `fileId` (string): File UUID

**Request Body:**
```json
{
  "users": [
    {
      "id": "user_456",
      "permissions": ["read"]
    },
    {
      "id": "user_789",
      "permissions": ["read", "write"]
    }
  ],
  "notify": true
}
```

### Get File Shares
```http
GET /api/files/{fileId}/shares
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `fileId` (string): File UUID

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "id": "file_123",
      "shares": [
        {
          "type": "public",
          "shareUrl": "https://files.oasisplatform.world/share/abc123def456",
          "permissions": ["read"],
          "expiresAt": "2024-01-27T14:30:00Z",
          "createdAt": "2024-01-20T14:30:00Z"
        },
        {
          "type": "user",
          "user": {
            "id": "user_456",
            "username": "jane_smith"
          },
          "permissions": ["read"],
          "createdAt": "2024-01-20T14:30:00Z"
        }
      ],
      "totalShares": 2,
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "File shares retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Revoke File Share
```http
DELETE /api/files/{fileId}/shares/{shareId}
Authorization: Bearer YOUR_TOKEN
```

**Parameters:**
- `fileId` (string): File UUID
- `shareId` (string): Share UUID

## File Analytics

### Get File Statistics
```http
GET /api/files/stats
Authorization: Bearer YOUR_TOKEN
```

**Query Parameters:**
- `timeframe` (string, optional): Timeframe (hour, day, week, month)
- `type` (string, optional): Filter by file type

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "timeframe": "day",
      "statistics": {
        "files": {
          "total": 10000,
          "uploaded": 500,
          "downloaded": 2000,
          "shared": 100,
          "deleted": 50
        },
        "byType": {
          "image": 4000,
          "document": 3000,
          "video": 2000,
          "audio": 500,
          "archive": 500
        },
        "storage": {
          "totalSize": "100GB",
          "usedSize": "75GB",
          "availableSize": "25GB",
          "averageFileSize": "10MB"
        },
        "performance": {
          "averageUploadTime": 5.0,
          "averageDownloadTime": 2.0,
          "uploadSuccessRate": 0.98,
          "downloadSuccessRate": 0.99
        }
      },
      "trends": {
        "files": "increasing",
        "storage": "increasing",
        "performance": "stable"
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "File statistics retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get File Performance
```http
GET /api/files/performance
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "performance": {
        "averageUploadTime": 5.0,
        "peakUploadTime": 15.0,
        "averageDownloadTime": 2.0,
        "peakDownloadTime": 8.0,
        "uploadSuccessRate": 0.98,
        "downloadSuccessRate": 0.99
      },
      "metrics": {
        "uploadsPerSecond": 10,
        "downloadsPerSecond": 50,
        "averageLatency": 2.0,
        "p95Latency": 5.0,
        "p99Latency": 10.0,
        "errorRate": 0.02,
        "successRate": 0.98
      },
      "trends": {
        "uploadTime": "stable",
        "downloadTime": "stable",
        "successRate": "increasing",
        "errorRate": "decreasing"
      },
      "breakdown": {
        "image": {
          "averageUploadTime": 3.0,
          "averageDownloadTime": 1.0,
          "successRate": 0.99
        },
        "document": {
          "averageUploadTime": 5.0,
          "averageDownloadTime": 2.0,
          "successRate": 0.98
        },
        "video": {
          "averageUploadTime": 15.0,
          "averageDownloadTime": 5.0,
          "successRate": 0.95
        }
      },
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "File performance retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

### Get File Health
```http
GET /api/files/health
Authorization: Bearer YOUR_TOKEN
```

**Response:**
```json
{
  "result": {
    "success": true,
    "data": {
      "status": "Healthy",
      "overallHealth": 0.95,
      "components": {
        "storage": {
          "status": "Healthy",
          "health": 0.98,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "encryption": {
          "status": "Healthy",
          "health": 0.97,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "compression": {
          "status": "Healthy",
          "health": 0.95,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        "sharing": {
          "status": "Healthy",
          "health": 0.92,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      },
      "checks": [
        {
          "name": "Storage Test",
          "status": "Pass",
          "responseTime": 1.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Encryption Test",
          "status": "Pass",
          "responseTime": 0.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Compression Test",
          "status": "Pass",
          "responseTime": 2.0,
          "lastCheck": "2024-01-20T14:30:00Z"
        },
        {
          "name": "Sharing Test",
          "status": "Pass",
          "responseTime": 1.5,
          "lastCheck": "2024-01-20T14:30:00Z"
        }
      ],
      "alerts": [],
      "lastUpdated": "2024-01-20T14:30:00Z"
    },
    "message": "File health retrieved successfully"
  },
  "isError": false,
  "message": "Success"
}
```

## Error Responses

### File Not Found
```json
{
  "result": null,
  "isError": true,
  "message": "File not found",
  "exception": "File with ID file_123 not found"
}
```

### Invalid File Type
```json
{
  "result": null,
  "isError": true,
  "message": "Invalid file type",
  "exception": "File type 'executable' is not supported"
}
```

### File Too Large
```json
{
  "result": null,
  "isError": true,
  "message": "File too large",
  "exception": "File size 100MB exceeds maximum allowed size of 50MB"
}
```

### Permission Denied
```json
{
  "result": null,
  "isError": true,
  "message": "Permission denied",
  "exception": "Insufficient permissions to access this file"
}
```

### Storage Quota Exceeded
```json
{
  "result": null,
  "isError": true,
  "message": "Storage quota exceeded",
  "exception": "Storage quota of 1GB exceeded. Current usage: 1.2GB"
}
```

---

## Navigation

**← Previous:** [Messaging API](Messaging-API.md) | **Next:** [Keys API](Keys-API.md) →
