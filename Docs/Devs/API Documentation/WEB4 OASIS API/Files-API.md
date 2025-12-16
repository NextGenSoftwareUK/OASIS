# Files API

## 📋 **Table of Contents**

- [Overview](#overview)
- [Core Endpoints](#core-endpoints)
- [Models](#models)

## Overview

The Files API provides comprehensive file management services for the OASIS ecosystem. It handles file upload, download, storage, sharing, and analytics with support for multiple file types, encryption, and real-time collaboration.

The WEB4 Files API is backed by `FilesController` and `FilesManager`.  
It provides file storage with metadata support.

All endpoints live under:

```http
Base: /api/files
```

All responses are wrapped in `OASISResult<T>`.

---

## Core Endpoints

- **Get all files for current avatar**
  - `GET /api/files/get-all-files-stored-for-current-logged-in-avatar`
  - Returns: `List<StoredFile>` for `AvatarId`

- **Upload file for current avatar**
  - `POST /api/files/upload-file`
  - Parameters:
    - `fileName` (string)
    - `fileData` (`byte[]`)
    - `contentType` (string)
    - `metadata` (`Dictionary<string, object>`, optional)
  - Returns: `StoredFile`

- **Download file**
  - `GET /api/files/download-file/{fileId}`
  - Route:
    - `fileId` (Guid)
  - Returns: `FileDownload`

- **Delete file**
  - `DELETE /api/files/delete-file/{fileId}`
  - Returns: `bool`

- **Get file metadata**
  - `GET /api/files/file-metadata/{fileId}`
  - Returns: `StoredFile`

- **Update file metadata**
  - `PUT /api/files/update-file-metadata/{fileId}`
  - Body: `Dictionary<string, object> metadata`
  - Returns: `bool`

---

## Models

### StoredFile

```csharp
public class StoredFile
{
    public Guid Id { get; set; }
    public Guid AvatarId { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

### FileDownload

```csharp
public class FileDownload
{
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public byte[] Data { get; set; }
    public long Size { get; set; }
}
```


