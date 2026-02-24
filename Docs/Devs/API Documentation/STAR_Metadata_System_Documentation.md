# STAR Metadata System - Complete Documentation

## 📋 **Overview**

The STAR Metadata System is a comprehensive key-value pair management system that provides advanced metadata capabilities for Celestial Bodies, Zomes, and Holons. This system enables flexible configuration, versioning, publishing, and sharing of metadata across the OASIS ecosystem.

## 🏗️ **Architecture**

### **Core Components**
- **Celestial Bodies MetaData**: Metadata for stars, planets, moons, asteroids, and other celestial objects
- **Zomes MetaData**: Metadata for code modules and functional components
- **Holons MetaData**: Metadata for data objects and their relationships
- **DNA Management**: Advanced metadata DNA system for complex configurations
- **STARNET Integration**: Publishing, downloading, and versioning capabilities

### **Key Features**
- **Flexible Data Types**: Support for strings, integers, booleans, datetimes, and complex objects
- **Version Control**: Complete version history with rollback capabilities
- **Publishing System**: STARNET integration for sharing and distribution
- **Search & Discovery**: Advanced search across all metadata types
- **Lifecycle Management**: Full CRUD operations with advanced features

## 🔗 **Base URLs**

### **Development**
```
https://localhost:5004/api
```

### **Production**
```
https://star-api.oasisweb4.com/api
```

## 🔐 **Authentication**

### **API Key Authentication**
```http
Authorization: Bearer YOUR_STAR_API_KEY
```

### **Avatar Authentication**
```http
Authorization: Avatar YOUR_AVATAR_ID
```

## 📚 **API Endpoints**

### **Celestial Bodies MetaData API**

#### **Get All Celestial Bodies MetaData**
```http
GET /api/celestialbodiesmetadata
```

**Response:**
```json
{
  "result": [
    {
      "id": "uuid",
      "name": "Alpha Centauri Metadata",
      "description": "Metadata for the Alpha Centauri star system",
      "type": "Star",
      "category": "Binary Star System",
      "metadata": {
        "mass": 1.1,
        "radius": 1.2,
        "temperature": 5790,
        "luminosity": 1.5,
        "composition": {
          "hydrogen": 0.73,
          "helium": 0.25,
          "metals": 0.02
        },
        "properties": {
          "age": "4.6 billion years",
          "spectralClass": "G2V",
          "habitableZone": true
        }
      },
      "createdBy": "uuid",
      "createdDate": "2024-01-15T10:30:00Z",
      "lastModified": "2024-01-15T14:30:00Z",
      "version": "1.2.0",
      "isPublished": true,
      "downloads": 150,
      "rating": 4.8,
      "tags": ["star", "binary", "habitable", "nearby"]
    }
  ],
  "isError": false,
  "message": "Celestial bodies metadata loaded successfully"
}
```

#### **Get Celestial Body MetaData by ID**
```http
GET /api/celestialbodiesmetadata/{id}
```

#### **Create Celestial Body MetaData**
```http
POST /api/celestialbodiesmetadata
```

**Request Body:**
```json
{
  "name": "string",
  "description": "string",
  "type": "Star|Planet|Moon|Asteroid|Nebula",
  "category": "string",
  "metadata": {
    "mass": 1.1,
    "radius": 1.2,
    "temperature": 5790,
    "luminosity": 1.5,
    "composition": {
      "hydrogen": 0.73,
      "helium": 0.25,
      "metals": 0.02
    },
    "properties": {
      "age": "string",
      "spectralClass": "string",
      "habitableZone": true
    }
  },
  "tags": ["string"]
}
```

#### **Update Celestial Body MetaData**
```http
PUT /api/celestialbodiesmetadata/{id}
```

#### **Delete Celestial Body MetaData**
```http
DELETE /api/celestialbodiesmetadata/{id}
```

#### **Clone Celestial Body MetaData**
```http
POST /api/celestialbodiesmetadata/{id}/clone
```

**Request Body:**
```json
{
  "name": "string",
  "description": "string"
}
```

#### **Publish Celestial Body MetaData**
```http
POST /api/celestialbodiesmetadata/{id}/publish
```

**Request Body:**
```json
{
  "version": "1.0.0",
  "description": "string",
  "isPublic": true
}
```

#### **Download Celestial Body MetaData**
```http
POST /api/celestialbodiesmetadata/{id}/download
```

**Request Body:**
```json
{
  "version": "1.0.0",
  "includeDependencies": true
}
```

#### **Get Celestial Body MetaData Versions**
```http
GET /api/celestialbodiesmetadata/{id}/versions
```

**Response:**
```json
{
  "result": [
    {
      "version": "1.2.0",
      "createdDate": "2024-01-15T14:30:00Z",
      "description": "Updated with new composition data",
      "isPublished": true,
      "downloads": 25
    },
    {
      "version": "1.1.0",
      "createdDate": "2024-01-10T10:00:00Z",
      "description": "Added habitable zone information",
      "isPublished": true,
      "downloads": 50
    }
  ],
  "isError": false,
  "message": "Versions loaded successfully"
}
```

#### **Search Celestial Bodies MetaData**
```http
POST /api/celestialbodiesmetadata/search
```

**Request Body:**
```json
{
  "query": "string",
  "filters": {
    "type": ["Star", "Planet"],
    "tags": ["habitable", "nearby"],
    "dateRange": {
      "start": "2024-01-01T00:00:00Z",
      "end": "2024-12-31T23:59:59Z"
    }
  },
  "sortBy": "name|createdDate|rating",
  "sortOrder": "asc|desc",
  "page": 1,
  "pageSize": 20
}
```

#### **Edit Celestial Body MetaData**
```http
POST /api/celestialbodiesmetadata/{id}/edit
```

**Request Body:**
```json
{
  "name": "string",
  "description": "string",
  "metadata": {
    "mass": 1.1,
    "radius": 1.2,
    "temperature": 5790
  },
  "tags": ["string"]
}
```

#### **Unpublish Celestial Body MetaData**
```http
POST /api/celestialbodiesmetadata/{id}/unpublish
```

#### **Republish Celestial Body MetaData**
```http
POST /api/celestialbodiesmetadata/{id}/republish
```

#### **Activate Celestial Body MetaData**
```http
POST /api/celestialbodiesmetadata/{id}/activate
```

#### **Deactivate Celestial Body MetaData**
```http
POST /api/celestialbodiesmetadata/{id}/deactivate
```

### **Zomes MetaData API**

#### **Get All Zomes MetaData**
```http
GET /api/zomesmetadata
```

**Response:**
```json
{
  "result": [
    {
      "id": "uuid",
      "name": "Central Hub Metadata",
      "description": "Metadata for the central hub zome",
      "type": "hub",
      "category": "Infrastructure",
      "metadata": {
        "capacity": 10000,
        "efficiency": 0.95,
        "uptime": 99.9,
        "services": [
          "Data Processing",
          "Energy Distribution",
          "Transport Hub"
        ],
        "connections": [
          {
            "zomeId": "uuid",
            "type": "data",
            "bandwidth": 1000
          }
        ],
        "properties": {
          "maintenanceSchedule": "weekly",
          "energyConsumption": 1000,
          "processingPower": 5000
        }
      },
      "createdBy": "uuid",
      "createdDate": "2024-01-15T10:30:00Z",
      "lastModified": "2024-01-15T14:30:00Z",
      "version": "2.1.0",
      "isPublished": true,
      "downloads": 75,
      "rating": 4.6,
      "tags": ["hub", "infrastructure", "central"]
    }
  ],
  "isError": false,
  "message": "Zomes metadata loaded successfully"
}
```

#### **Get Zome MetaData by ID**
```http
GET /api/zomesmetadata/{id}
```

#### **Create Zome MetaData**
```http
POST /api/zomesmetadata
```

**Request Body:**
```json
{
  "name": "string",
  "description": "string",
  "type": "hub|transport|storage|processing",
  "category": "string",
  "metadata": {
    "capacity": 10000,
    "efficiency": 0.95,
    "uptime": 99.9,
    "services": ["string"],
    "connections": [
      {
        "zomeId": "uuid",
        "type": "data|transport|energy",
        "bandwidth": 1000
      }
    ],
    "properties": {
      "maintenanceSchedule": "string",
      "energyConsumption": 1000,
      "processingPower": 5000
    }
  },
  "tags": ["string"]
}
```

#### **Update Zome MetaData**
```http
PUT /api/zomesmetadata/{id}
```

#### **Delete Zome MetaData**
```http
DELETE /api/zomesmetadata/{id}
```

#### **Clone Zome MetaData**
```http
POST /api/zomesmetadata/{id}/clone
```

#### **Publish Zome MetaData**
```http
POST /api/zomesmetadata/{id}/publish
```

#### **Download Zome MetaData**
```http
POST /api/zomesmetadata/{id}/download
```

#### **Get Zome MetaData Versions**
```http
GET /api/zomesmetadata/{id}/versions
```

#### **Search Zomes MetaData**
```http
POST /api/zomesmetadata/search
```

#### **Edit Zome MetaData**
```http
POST /api/zomesmetadata/{id}/edit
```

#### **Unpublish Zome MetaData**
```http
POST /api/zomesmetadata/{id}/unpublish
```

#### **Republish Zome MetaData**
```http
POST /api/zomesmetadata/{id}/republish
```

#### **Activate Zome MetaData**
```http
POST /api/zomesmetadata/{id}/activate
```

#### **Deactivate Zome MetaData**
```http
POST /api/zomesmetadata/{id}/deactivate
```

### **Holons MetaData API**

#### **Get All Holons MetaData**
```http
GET /api/holonsmetadata
```

**Response:**
```json
{
  "result": [
    {
      "id": "uuid",
      "name": "Data Processor Metadata",
      "description": "Metadata for the data processor holon",
      "type": "data",
      "category": "Processing",
      "metadata": {
        "level": 5,
        "processingPower": 1000,
        "memory": 5000,
        "energyConsumption": 100,
        "properties": {
          "efficiency": 0.92,
          "lastMaintenance": "2024-01-10T10:00:00Z",
          "nextMaintenance": "2024-01-17T10:00:00Z"
        },
        "relationships": {
          "parentHolonId": "uuid",
          "childHolons": ["uuid"],
          "connections": [
            {
              "holonId": "uuid",
              "type": "data",
              "strength": 0.8
            }
          ]
        }
      },
      "createdBy": "uuid",
      "createdDate": "2024-01-15T10:30:00Z",
      "lastModified": "2024-01-15T14:30:00Z",
      "version": "1.5.0",
      "isPublished": true,
      "downloads": 45,
      "rating": 4.4,
      "tags": ["data", "processing", "holon"]
    }
  ],
  "isError": false,
  "message": "Holons metadata loaded successfully"
}
```

#### **Get Holon MetaData by ID**
```http
GET /api/holonsmetadata/{id}
```

#### **Create Holon MetaData**
```http
POST /api/holonsmetadata
```

**Request Body:**
```json
{
  "name": "string",
  "description": "string",
  "type": "data|energy|matter|information",
  "category": "string",
  "metadata": {
    "level": 5,
    "processingPower": 1000,
    "memory": 5000,
    "energyConsumption": 100,
    "properties": {
      "efficiency": 0.92,
      "lastMaintenance": "2024-01-10T10:00:00Z",
      "nextMaintenance": "2024-01-17T10:00:00Z"
    },
    "relationships": {
      "parentHolonId": "uuid",
      "childHolons": ["uuid"],
      "connections": [
        {
          "holonId": "uuid",
          "type": "data|energy|matter",
          "strength": 0.8
        }
      ]
    }
  },
  "tags": ["string"]
}
```

#### **Update Holon MetaData**
```http
PUT /api/holonsmetadata/{id}
```

#### **Delete Holon MetaData**
```http
DELETE /api/holonsmetadata/{id}
```

#### **Clone Holon MetaData**
```http
POST /api/holonsmetadata/{id}/clone
```

#### **Publish Holon MetaData**
```http
POST /api/holonsmetadata/{id}/publish
```

#### **Download Holon MetaData**
```http
POST /api/holonsmetadata/{id}/download
```

#### **Get Holon MetaData Versions**
```http
GET /api/holonsmetadata/{id}/versions
```

#### **Search Holons MetaData**
```http
POST /api/holonsmetadata/search
```

#### **Edit Holon MetaData**
```http
POST /api/holonsmetadata/{id}/edit
```

#### **Unpublish Holon MetaData**
```http
POST /api/holonsmetadata/{id}/unpublish
```

#### **Republish Holon MetaData**
```http
POST /api/holonsmetadata/{id}/republish
```

#### **Activate Holon MetaData**
```http
POST /api/holonsmetadata/{id}/activate
```

#### **Deactivate Holon MetaData**
```http
POST /api/holonsmetadata/{id}/deactivate
```

## 🎮 **STARNET Integration**

### **Publishing Workflow**
1. **Create Metadata**: Define metadata with key-value pairs
2. **Version Control**: Automatic versioning with semantic versioning
3. **Publishing**: One-click publishing to STARNET
4. **Distribution**: Automatic distribution across the network
5. **Discovery**: Searchable and discoverable by other users

### **Downloading Workflow**
1. **Search**: Find metadata using advanced search
2. **Preview**: View metadata before downloading
3. **Download**: Download specific versions
4. **Install**: Automatic installation and configuration
5. **Integration**: Seamless integration with existing projects

### **Version Management**
- **Semantic Versioning**: Major.Minor.Patch versioning system
- **Version History**: Complete history of all changes
- **Rollback**: Easy rollback to previous versions
- **Dependencies**: Automatic dependency management
- **Compatibility**: Version compatibility checking

## 🔧 **Development Workflow**

### **1. Create Metadata**
```javascript
// Create celestial body metadata
const celestialMetadata = await client.celestialBodiesMetaData.create({
  name: 'Alpha Centauri Metadata',
  description: 'Metadata for the Alpha Centauri star system',
  type: 'Star',
  metadata: {
    mass: 1.1,
    radius: 1.2,
    temperature: 5790,
    composition: {
      hydrogen: 0.73,
      helium: 0.25,
      metals: 0.02
    }
  },
  tags: ['star', 'binary', 'habitable']
});
```

### **2. Publish to STARNET**
```javascript
// Publish metadata
await client.celestialBodiesMetaData.publish(celestialMetadata.id, {
  version: '1.0.0',
  description: 'Initial release',
  isPublic: true
});
```

### **3. Search and Download**
```javascript
// Search for metadata
const results = await client.celestialBodiesMetaData.search({
  query: 'star system',
  filters: {
    type: ['Star'],
    tags: ['habitable']
  }
});

// Download metadata
await client.celestialBodiesMetaData.download(results[0].id, {
  version: '1.0.0',
  includeDependencies: true
});
```

### **4. Integration with OAPP Builder**
```javascript
// Use metadata in OAPP builder
const oapp = await client.oappBuilder.create({
  name: 'Space Explorer',
  components: [
    {
      type: 'celestialBody',
      metadataId: celestialMetadata.id,
      version: '1.0.0'
    }
  ]
});
```

## 📊 **Advanced Features**

### **DNA Management**
- **Complex Structures**: Support for nested objects and arrays
- **Type Safety**: Strong typing for metadata values
- **Validation**: Automatic validation of metadata structure
- **Inheritance**: Metadata inheritance and composition

### **Search Capabilities**
- **Full-Text Search**: Search across all metadata fields
- **Filtered Search**: Advanced filtering by type, tags, date range
- **Fuzzy Matching**: Intelligent matching for typos and variations
- **Relevance Scoring**: Results ranked by relevance

### **Collaboration Features**
- **Sharing**: Share metadata with specific users or publicly
- **Comments**: Add comments and discussions to metadata
- **Ratings**: Rate and review metadata
- **Forks**: Fork and modify existing metadata

## 🔐 **Security & Privacy**

### **Access Control**
- **Public/Private**: Control visibility of metadata
- **User Permissions**: Granular permission system
- **API Keys**: Secure API key authentication
- **Rate Limiting**: Protection against abuse

### **Data Protection**
- **Encryption**: All data encrypted in transit and at rest
- **Audit Logging**: Complete audit trail of all operations
- **Backup**: Automatic backup and recovery
- **Compliance**: GDPR and privacy compliance

## 📱 **SDKs**

### **JavaScript/Node.js**
```bash
npm install @oasis/star-metadata-client
```

```javascript
import { STARMetadataClient } from '@oasis/star-metadata-client';

const client = new STARMetadataClient({
  apiKey: 'your-star-api-key',
  baseUrl: 'https://star-api.oasisweb4.com'
});

// Create metadata
const metadata = await client.celestialBodiesMetaData.create({
  name: 'My Star Metadata',
  type: 'Star',
  metadata: { mass: 1.0, temperature: 5000 }
});
```

### **C#/.NET**
```bash
dotnet add package OASIS.STAR.Metadata.Client
```

```csharp
using OASIS.STAR.Metadata.Client;

var client = new STARMetadataClient("your-star-api-key");

// Create metadata
var metadata = await client.CelestialBodiesMetaData.CreateAsync(new CelestialBodyMetaDataRequest
{
    Name = "My Star Metadata",
    Type = "Star",
    Metadata = new Dictionary<string, object>
    {
        ["mass"] = 1.0,
        ["temperature"] = 5000
    }
});
```

## 📚 **Examples**

### **Complete Metadata Workflow**
```javascript
// 1. Create metadata for a star system
const starMetadata = await client.celestialBodiesMetaData.create({
  name: 'Proxima Centauri System',
  description: 'Complete metadata for the Proxima Centauri system',
  type: 'Star',
  metadata: {
    mass: 0.12,
    radius: 0.14,
    temperature: 3042,
    luminosity: 0.0017,
    distance: 4.24,
    composition: {
      hydrogen: 0.68,
      helium: 0.28,
      metals: 0.04
    },
    planets: [
      {
        name: 'Proxima Centauri b',
        type: 'Planet',
        mass: 1.27,
        orbitalPeriod: 11.2
      }
    ]
  },
  tags: ['star', 'red-dwarf', 'nearby', 'habitable-zone']
});

// 2. Create metadata for a zome
const zomeMetadata = await client.zomesMetaData.create({
  name: 'Research Station Zome',
  description: 'Metadata for a research station zome',
  type: 'hub',
  metadata: {
    capacity: 5000,
    services: ['Research', 'Data Analysis', 'Communication'],
    efficiency: 0.88,
    connections: [
      {
        zomeId: 'central-hub-id',
        type: 'data',
        bandwidth: 2000
      }
    ]
  },
  tags: ['research', 'station', 'scientific']
});

// 3. Create metadata for a holon
const holonMetadata = await client.holonsMetaData.create({
  name: 'Data Analysis Holon',
  description: 'Metadata for a data analysis holon',
  type: 'data',
  metadata: {
    level: 8,
    processingPower: 2000,
    memory: 10000,
    algorithms: ['neural-network', 'machine-learning', 'statistical'],
    efficiency: 0.95
  },
  tags: ['data', 'analysis', 'ai', 'processing']
});

// 4. Publish all metadata
await Promise.all([
  client.celestialBodiesMetaData.publish(starMetadata.id, { version: '1.0.0' }),
  client.zomesMetaData.publish(zomeMetadata.id, { version: '1.0.0' }),
  client.holonsMetaData.publish(holonMetadata.id, { version: '1.0.0' })
]);

// 5. Search for metadata
const searchResults = await client.celestialBodiesMetaData.search({
  query: 'star system',
  filters: {
    type: ['Star'],
    tags: ['nearby', 'habitable-zone']
  }
});

// 6. Use in OAPP builder
const oapp = await client.oappBuilder.create({
  name: 'Proxima Centauri Explorer',
  components: [
    {
      type: 'celestialBody',
      metadataId: starMetadata.id,
      version: '1.0.0'
    },
    {
      type: 'zome',
      metadataId: zomeMetadata.id,
      version: '1.0.0'
    },
    {
      type: 'holon',
      metadataId: holonMetadata.id,
      version: '1.0.0'
    }
  ]
});
```

## 🚀 **Getting Started**

1. **Get API Key**: Sign up for a STAR API key
2. **Install SDK**: Choose your preferred SDK
3. **Create Metadata**: Start creating metadata for your components
4. **Publish**: Publish your metadata to STARNET
5. **Share**: Share and collaborate with the community

## 📞 **Support**

- **Documentation**: [docs.oasisplatform.world/star/metadata](https://docs.oasisplatform.world/star/metadata)
- **Community**: [Discord](https://discord.gg/oasis)
- **GitHub**: [github.com/oasisplatform/star](https://github.com/oasisplatform/star)
- **Email**: star-support@oasisplatform.world

---

*This documentation covers the complete STAR Metadata System. For the latest updates and examples, visit [docs.oasisplatform.world/star/metadata](https://docs.oasisplatform.world/star/metadata)*
