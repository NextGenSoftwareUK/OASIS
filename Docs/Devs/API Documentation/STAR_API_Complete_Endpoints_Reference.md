# STAR API - Complete Endpoints Reference

## 📋 **Overview**

This document provides a comprehensive reference of ALL STAR API endpoints, organized by controller and functionality. This is the definitive guide for all available endpoints in the STAR Web API.

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

## 📚 **Complete API Endpoints**

### **Avatar Controller**
```http
GET    /api/avatar                    # Get avatar information
POST   /api/avatar                    # Create avatar
PUT    /api/avatar/{id}               # Update avatar
DELETE /api/avatar/{id}               # Delete avatar
```

### **STAR Controller**
```http
GET    /api/star                     # Get STAR information
GET    /api/star/status              # Get STAR status
GET    /api/star/config              # Get STAR configuration
PUT    /api/star/config            # Update STAR configuration
```

### **OAPPs Controller**
```http
GET    /api/oapps                    # Get all OAPPs
GET    /api/oapps/{id}               # Get OAPP by ID
POST   /api/oapps                    # Create OAPP
PUT    /api/oapps/{id}               # Update OAPP
DELETE /api/oapps/{id}               # Delete OAPP
POST   /api/oapps/{id}/clone         # Clone OAPP
POST   /api/oapps/{id}/publish        # Publish OAPP
POST   /api/oapps/search             # Search OAPPs
GET    /api/oapps/{id}/versions       # Get OAPP versions
POST   /api/oapps/{id}/download       # Download OAPP
POST   /api/oapps/{id}/edit           # Edit OAPP
POST   /api/oapps/{id}/unpublish      # Unpublish OAPP
POST   /api/oapps/{id}/republish      # Republish OAPP
POST   /api/oapps/{id}/activate       # Activate OAPP
POST   /api/oapps/{id}/deactivate     # Deactivate OAPP
```

### **Quests Controller**
```http
GET    /api/quests                   # Get all quests
GET    /api/quests/{id}              # Get quest by ID
POST   /api/quests                   # Create quest
PUT    /api/quests/{id}              # Update quest
DELETE /api/quests/{id}              # Delete quest
POST   /api/quests/{id}/clone        # Clone quest
POST   /api/quests/{id}/publish       # Publish quest
POST   /api/quests/search            # Search quests
GET    /api/quests/{id}/versions      # Get quest versions
POST   /api/quests/{id}/download      # Download quest
POST   /api/quests/{id}/edit          # Edit quest
POST   /api/quests/{id}/unpublish     # Unpublish quest
POST   /api/quests/{id}/republish     # Republish quest
POST   /api/quests/{id}/activate      # Activate quest
POST   /api/quests/{id}/deactivate    # Deactivate quest
```

### **NFTs Controller**
```http
GET    /api/nfts                     # Get all NFTs
GET    /api/nfts/{id}                # Get NFT by ID
POST   /api/nfts                     # Create NFT
PUT    /api/nfts/{id}                # Update NFT
DELETE /api/nfts/{id}                # Delete NFT
POST   /api/nfts/{id}/clone          # Clone NFT
POST   /api/nfts/{id}/publish         # Publish NFT
POST   /api/nfts/search              # Search NFTs
GET    /api/nfts/{id}/versions        # Get NFT versions
POST   /api/nfts/{id}/download        # Download NFT
POST   /api/nfts/{id}/edit            # Edit NFT
POST   /api/nfts/{id}/unpublish       # Unpublish NFT
POST   /api/nfts/{id}/republish       # Republish NFT
POST   /api/nfts/{id}/activate        # Activate NFT
POST   /api/nfts/{id}/deactivate      # Deactivate NFT
```

### **Inventory Items Controller**
```http
GET    /api/inventoryitems           # Get all inventory items
GET    /api/inventoryitems/{id}      # Get inventory item by ID
POST   /api/inventoryitems           # Create inventory item
PUT    /api/inventoryitems/{id}      # Update inventory item
DELETE /api/inventoryitems/{id}      # Delete inventory item
POST   /api/inventoryitems/{id}/clone # Clone inventory item
POST   /api/inventoryitems/{id}/publish # Publish inventory item
POST   /api/inventoryitems/search    # Search inventory items
GET    /api/inventoryitems/{id}/versions # Get inventory item versions
POST   /api/inventoryitems/{id}/download # Download inventory item
POST   /api/inventoryitems/{id}/edit # Edit inventory item
POST   /api/inventoryitems/{id}/unpublish # Unpublish inventory item
POST   /api/inventoryitems/{id}/republish # Republish inventory item
POST   /api/inventoryitems/{id}/activate # Activate inventory item
POST   /api/inventoryitems/{id}/deactivate # Deactivate inventory item
```

### **Celestial Bodies Controller**
```http
GET    /api/celestialbodies          # Get all celestial bodies
GET    /api/celestialbodies/{id}     # Get celestial body by ID
POST   /api/celestialbodies          # Create celestial body
PUT    /api/celestialbodies/{id}     # Update celestial body
DELETE /api/celestialbodies/{id}     # Delete celestial body
POST   /api/celestialbodies/{id}/clone # Clone celestial body
POST   /api/celestialbodies/{id}/publish # Publish celestial body
POST   /api/celestialbodies/search   # Search celestial bodies
GET    /api/celestialbodies/{id}/versions # Get celestial body versions
POST   /api/celestialbodies/{id}/download # Download celestial body
POST   /api/celestialbodies/{id}/edit # Edit celestial body
POST   /api/celestialbodies/{id}/unpublish # Unpublish celestial body
POST   /api/celestialbodies/{id}/republish # Republish celestial body
POST   /api/celestialbodies/{id}/activate # Activate celestial body
POST   /api/celestialbodies/{id}/deactivate # Deactivate celestial body
```

### **Celestial Spaces Controller**
```http
GET    /api/celestialspaces          # Get all celestial spaces
GET    /api/celestialspaces/{id}     # Get celestial space by ID
POST   /api/celestialspaces          # Create celestial space
PUT    /api/celestialspaces/{id}     # Update celestial space
DELETE /api/celestialspaces/{id}     # Delete celestial space
POST   /api/celestialspaces/{id}/clone # Clone celestial space
POST   /api/celestialspaces/{id}/publish # Publish celestial space
POST   /api/celestialspaces/search   # Search celestial spaces
GET    /api/celestialspaces/{id}/versions # Get celestial space versions
POST   /api/celestialspaces/{id}/download # Download celestial space
POST   /api/celestialspaces/{id}/edit # Edit celestial space
POST   /api/celestialspaces/{id}/unpublish # Unpublish celestial space
POST   /api/celestialspaces/{id}/republish # Republish celestial space
POST   /api/celestialspaces/{id}/activate # Activate celestial space
POST   /api/celestialspaces/{id}/deactivate # Deactivate celestial space
```

### **GeoNFTs Controller**
```http
GET    /api/geonfts                  # Get all GeoNFTs
GET    /api/geonfts/{id}            # Get GeoNFT by ID
POST   /api/geonfts                 # Create GeoNFT
PUT    /api/geonfts/{id}            # Update GeoNFT
DELETE /api/geonfts/{id}            # Delete GeoNFT
POST   /api/geonfts/{id}/clone      # Clone GeoNFT
POST   /api/geonfts/{id}/publish     # Publish GeoNFT
POST   /api/geonfts/search          # Search GeoNFTs
GET    /api/geonfts/{id}/versions   # Get GeoNFT versions
POST   /api/geonfts/{id}/download    # Download GeoNFT
POST   /api/geonfts/{id}/edit       # Edit GeoNFT
POST   /api/geonfts/{id}/unpublish   # Unpublish GeoNFT
POST   /api/geonfts/{id}/republish   # Republish GeoNFT
POST   /api/geonfts/{id}/activate   # Activate GeoNFT
POST   /api/geonfts/{id}/deactivate # Deactivate GeoNFT
```

### **GeoHotSpots Controller**
```http
GET    /api/geohotspots             # Get all GeoHotSpots
GET    /api/geohotspots/{id}         # Get GeoHotSpot by ID
POST   /api/geohotspots             # Create GeoHotSpot
PUT    /api/geohotspots/{id}        # Update GeoHotSpot
DELETE /api/geohotspots/{id}        # Delete GeoHotSpot
POST   /api/geohotspots/{id}/clone  # Clone GeoHotSpot
POST   /api/geohotspots/{id}/publish # Publish GeoHotSpot
POST   /api/geohotspots/search      # Search GeoHotSpots
GET    /api/geohotspots/{id}/versions # Get GeoHotSpot versions
POST   /api/geohotspots/{id}/download # Download GeoHotSpot
POST   /api/geohotspots/{id}/edit  # Edit GeoHotSpot
POST   /api/geohotspots/{id}/unpublish # Unpublish GeoHotSpot
POST   /api/geohotspots/{id}/republish # Republish GeoHotSpot
POST   /api/geohotspots/{id}/activate # Activate GeoHotSpot
POST   /api/geohotspots/{id}/deactivate # Deactivate GeoHotSpot
```

### **Chapters Controller**
```http
GET    /api/chapters                # Get all chapters
GET    /api/chapters/{id}           # Get chapter by ID
POST   /api/chapters                # Create chapter
PUT    /api/chapters/{id}           # Update chapter
DELETE /api/chapters/{id}           # Delete chapter
POST   /api/chapters/{id}/clone     # Clone chapter
POST   /api/chapters/{id}/publish   # Publish chapter
POST   /api/chapters/search         # Search chapters
GET    /api/chapters/{id}/versions  # Get chapter versions
POST   /api/chapters/{id}/download  # Download chapter
POST   /api/chapters/{id}/edit      # Edit chapter
POST   /api/chapters/{id}/unpublish # Unpublish chapter
POST   /api/chapters/{id}/republish # Republish chapter
POST   /api/chapters/{id}/activate  # Activate chapter
POST   /api/chapters/{id}/deactivate # Deactivate chapter
```

### **Zomes Controller**
```http
GET    /api/zomes                   # Get all zomes
GET    /api/zomes/{id}              # Get zome by ID
POST   /api/zomes                   # Create zome
PUT    /api/zomes/{id}              # Update zome
DELETE /api/zomes/{id}              # Delete zome
POST   /api/zomes/{id}/clone        # Clone zome
POST   /api/zomes/{id}/publish       # Publish zome
POST   /api/zomes/search            # Search zomes
GET    /api/zomes/{id}/versions      # Get zome versions
POST   /api/zomes/{id}/download      # Download zome
POST   /api/zomes/{id}/edit          # Edit zome
POST   /api/zomes/{id}/unpublish     # Unpublish zome
POST   /api/zomes/{id}/republish     # Republish zome
POST   /api/zomes/{id}/activate      # Activate zome
POST   /api/zomes/{id}/deactivate    # Deactivate zome
```

### **Holons Controller**
```http
GET    /api/holons                  # Get all holons
GET    /api/holons/{id}             # Get holon by ID
POST   /api/holons                  # Create holon
PUT    /api/holons/{id}             # Update holon
DELETE /api/holons/{id}             # Delete holon
POST   /api/holons/{id}/clone       # Clone holon
POST   /api/holons/{id}/publish      # Publish holon
POST   /api/holons/search           # Search holons
GET    /api/holons/{id}/versions     # Get holon versions
POST   /api/holons/{id}/download     # Download holon
POST   /api/holons/{id}/edit         # Edit holon
POST   /api/holons/{id}/unpublish    # Unpublish holon
POST   /api/holons/{id}/republish    # Republish holon
POST   /api/holons/{id}/activate     # Activate holon
POST   /api/holons/{id}/deactivate   # Deactivate holon
```

### **Parks Controller**
```http
GET    /api/parks                   # Get all parks
GET    /api/parks/{id}              # Get park by ID
POST   /api/parks                   # Create park
PUT    /api/parks/{id}              # Update park
DELETE /api/parks/{id}              # Delete park
POST   /api/parks/{id}/clone        # Clone park
POST   /api/parks/{id}/publish       # Publish park
POST   /api/parks/search            # Search parks
GET    /api/parks/{id}/versions      # Get park versions
POST   /api/parks/{id}/download      # Download park
POST   /api/parks/{id}/edit          # Edit park
POST   /api/parks/{id}/unpublish     # Unpublish park
POST   /api/parks/{id}/republish     # Republish park
POST   /api/parks/{id}/activate      # Activate park
POST   /api/parks/{id}/deactivate    # Deactivate park
```

### **Templates Controller**
```http
GET    /api/templates               # Get all templates
GET    /api/templates/{id}          # Get template by ID
POST   /api/templates               # Create template
PUT    /api/templates/{id}          # Update template
DELETE /api/templates/{id}          # Delete template
POST   /api/templates/{id}/clone    # Clone template
POST   /api/templates/{id}/publish  # Publish template
POST   /api/templates/search        # Search templates
GET    /api/templates/{id}/versions # Get template versions
POST   /api/templates/{id}/download # Download template
POST   /api/templates/{id}/edit     # Edit template
POST   /api/templates/{id}/unpublish # Unpublish template
POST   /api/templates/{id}/republish # Republish template
POST   /api/templates/{id}/activate # Activate template
POST   /api/templates/{id}/deactivate # Deactivate template
```

### **Libraries Controller**
```http
GET    /api/libraries               # Get all libraries
GET    /api/libraries/{id}          # Get library by ID
POST   /api/libraries               # Create library
PUT    /api/libraries/{id}          # Update library
DELETE /api/libraries/{id}          # Delete library
POST   /api/libraries/{id}/clone    # Clone library
POST   /api/libraries/{id}/publish  # Publish library
POST   /api/libraries/search        # Search libraries
GET    /api/libraries/{id}/versions  # Get library versions
POST   /api/libraries/{id}/download # Download library
POST   /api/libraries/{id}/edit     # Edit library
POST   /api/libraries/{id}/unpublish # Unpublish library
POST   /api/libraries/{id}/republish # Republish library
POST   /api/libraries/{id}/activate # Activate library
POST   /api/libraries/{id}/deactivate # Deactivate library
```

### **Runtimes Controller**
```http
GET    /api/runtimes                # Get all runtimes
GET    /api/runtimes/{id}           # Get runtime by ID
POST   /api/runtimes                # Create runtime
PUT    /api/runtimes/{id}           # Update runtime
DELETE /api/runtimes/{id}           # Delete runtime
POST   /api/runtimes/{id}/clone     # Clone runtime
POST   /api/runtimes/{id}/publish   # Publish runtime
POST   /api/runtimes/search         # Search runtimes
GET    /api/runtimes/{id}/versions  # Get runtime versions
POST   /api/runtimes/{id}/download  # Download runtime
POST   /api/runtimes/{id}/edit      # Edit runtime
POST   /api/runtimes/{id}/unpublish # Unpublish runtime
POST   /api/runtimes/{id}/republish # Republish runtime
POST   /api/runtimes/{id}/activate  # Activate runtime
POST   /api/runtimes/{id}/deactivate # Deactivate runtime
```

### **Plugins Controller**
```http
GET    /api/plugins                 # Get all plugins
GET    /api/plugins/{id}            # Get plugin by ID
POST   /api/plugins                 # Create plugin
PUT    /api/plugins/{id}            # Update plugin
DELETE /api/plugins/{id}            # Delete plugin
POST   /api/plugins/{id}/clone      # Clone plugin
POST   /api/plugins/{id}/publish     # Publish plugin
POST   /api/plugins/search          # Search plugins
GET    /api/plugins/{id}/versions   # Get plugin versions
POST   /api/plugins/{id}/download   # Download plugin
POST   /api/plugins/{id}/edit       # Edit plugin
POST   /api/plugins/{id}/unpublish  # Unpublish plugin
POST   /api/plugins/{id}/republish  # Republish plugin
POST   /api/plugins/{id}/activate   # Activate plugin
POST   /api/plugins/{id}/deactivate # Deactivate plugin
```

### **Competition Controller**
```http
GET    /api/competition             # Get all competitions
GET    /api/competition/{id}         # Get competition by ID
POST   /api/competition              # Create competition
PUT    /api/competition/{id}         # Update competition
DELETE /api/competition/{id}         # Delete competition
POST   /api/competition/{id}/join    # Join competition
GET    /api/competition/{id}/leaderboard # Get competition leaderboard
POST   /api/competition/{id}/submit-score # Submit score
```

### **Eggs Controller**
```http
GET    /api/eggs                    # Get all eggs
GET    /api/eggs/{id}               # Get egg by ID
POST   /api/eggs                    # Create egg
PUT    /api/eggs/{id}               # Update egg
DELETE /api/eggs/{id}               # Delete egg
POST   /api/eggs/{id}/hatch         # Hatch egg
POST   /api/eggs/{id}/feed          # Feed egg
GET    /api/eggs/{id}/stats         # Get egg statistics
```

### **Missions Controller**
```http
GET    /api/missions                 # Get all missions
GET    /api/missions/{id}            # Get mission by ID
POST   /api/missions                 # Create mission
PUT    /api/missions/{id}            # Update mission
DELETE /api/missions/{id}            # Delete mission
POST   /api/missions/{id}/clone      # Clone mission
POST   /api/missions/{id}/publish    # Publish mission
POST   /api/missions/search          # Search missions
GET    /api/missions/{id}/versions   # Get mission versions
POST   /api/missions/{id}/download   # Download mission
POST   /api/missions/{id}/edit       # Edit mission
POST   /api/missions/{id}/unpublish  # Unpublish mission
POST   /api/missions/{id}/republish # Republish mission
POST   /api/missions/{id}/activate   # Activate mission
POST   /api/missions/{id}/deactivate # Deactivate mission
```

### **Quests Controller**
```http
GET    /api/quests                   # Get all quests
GET    /api/quests/{id}              # Get quest by ID
POST   /api/quests                   # Create quest
PUT    /api/quests/{id}              # Update quest
DELETE /api/quests/{id}              # Delete quest
POST   /api/quests/{id}/clone       # Clone quest
POST   /api/quests/{id}/publish     # Publish quest
POST   /api/quests/search            # Search quests
GET    /api/quests/{id}/versions     # Get quest versions
POST   /api/quests/{id}/download     # Download quest
POST   /api/quests/{id}/edit         # Edit quest
POST   /api/quests/{id}/unpublish    # Unpublish quest
POST   /api/quests/{id}/republish    # Republish quest
POST   /api/quests/{id}/activate     # Activate quest
POST   /api/quests/{id}/deactivate   # Deactivate quest
```

### **Chapters Controller**
```http
GET    /api/chapters                 # Get all chapters
GET    /api/chapters/{id}            # Get chapter by ID
POST   /api/chapters                 # Create chapter
PUT    /api/chapters/{id}            # Update chapter
DELETE /api/chapters/{id}            # Delete chapter
POST   /api/chapters/{id}/clone      # Clone chapter
POST   /api/chapters/{id}/publish    # Publish chapter
POST   /api/chapters/search          # Search chapters
GET    /api/chapters/{id}/versions   # Get chapter versions
POST   /api/chapters/{id}/download   # Download chapter
POST   /api/chapters/{id}/edit       # Edit chapter
POST   /api/chapters/{id}/unpublish  # Unpublish chapter
POST   /api/chapters/{id}/republish  # Republish chapter
POST   /api/chapters/{id}/activate   # Activate chapter
POST   /api/chapters/{id}/deactivate # Deactivate chapter
```

### **NFTs Controller**
```http
GET    /api/nfts                     # Get all NFTs
GET    /api/nfts/{id}                # Get NFT by ID
POST   /api/nfts                     # Create NFT
PUT    /api/nfts/{id}                # Update NFT
DELETE /api/nfts/{id}                # Delete NFT
POST   /api/nfts/{id}/clone          # Clone NFT
POST   /api/nfts/{id}/publish        # Publish NFT
POST   /api/nfts/search              # Search NFTs
GET    /api/nfts/{id}/versions       # Get NFT versions
POST   /api/nfts/{id}/download       # Download NFT
POST   /api/nfts/{id}/edit           # Edit NFT
POST   /api/nfts/{id}/unpublish      # Unpublish NFT
POST   /api/nfts/{id}/republish      # Republish NFT
POST   /api/nfts/{id}/activate        # Activate NFT
POST   /api/nfts/{id}/deactivate     # Deactivate NFT
```

### **GeoNFTs Controller**
```http
GET    /api/geonfts                  # Get all GeoNFTs
GET    /api/geonfts/{id}             # Get GeoNFT by ID
POST   /api/geonfts                  # Create GeoNFT
PUT    /api/geonfts/{id}             # Update GeoNFT
DELETE /api/geonfts/{id}             # Delete GeoNFT
POST   /api/geonfts/{id}/clone       # Clone GeoNFT
POST   /api/geonfts/{id}/publish     # Publish GeoNFT
POST   /api/geonfts/search           # Search GeoNFTs
GET    /api/geonfts/{id}/versions    # Get GeoNFT versions
POST   /api/geonfts/{id}/download    # Download GeoNFT
POST   /api/geonfts/{id}/edit        # Edit GeoNFT
POST   /api/geonfts/{id}/unpublish   # Unpublish GeoNFT
POST   /api/geonfts/{id}/republish   # Republish GeoNFT
POST   /api/geonfts/{id}/activate    # Activate GeoNFT
POST   /api/geonfts/{id}/deactivate  # Deactivate GeoNFT
```

### **Inventory Items Controller**
```http
GET    /api/inventoryitems           # Get all inventory items
GET    /api/inventoryitems/{id}      # Get inventory item by ID
POST   /api/inventoryitems           # Create inventory item
PUT    /api/inventoryitems/{id}      # Update inventory item
DELETE /api/inventoryitems/{id}      # Delete inventory item
POST   /api/inventoryitems/{id}/clone # Clone inventory item
POST   /api/inventoryitems/{id}/publish # Publish inventory item
POST   /api/inventoryitems/search    # Search inventory items
GET    /api/inventoryitems/{id}/versions # Get inventory item versions
POST   /api/inventoryitems/{id}/download # Download inventory item
POST   /api/inventoryitems/{id}/edit # Edit inventory item
POST   /api/inventoryitems/{id}/unpublish # Unpublish inventory item
POST   /api/inventoryitems/{id}/republish # Republish inventory item
POST   /api/inventoryitems/{id}/activate # Activate inventory item
POST   /api/inventoryitems/{id}/deactivate # Deactivate inventory item
```

### **Celestial Bodies Controller**
```http
GET    /api/celestialbodies          # Get all celestial bodies
GET    /api/celestialbodies/{id}     # Get celestial body by ID
POST   /api/celestialbodies          # Create celestial body
PUT    /api/celestialbodies/{id}     # Update celestial body
DELETE /api/celestialbodies/{id}     # Delete celestial body
POST   /api/celestialbodies/{id}/clone # Clone celestial body
POST   /api/celestialbodies/{id}/publish # Publish celestial body
POST   /api/celestialbodies/search   # Search celestial bodies
GET    /api/celestialbodies/{id}/versions # Get celestial body versions
POST   /api/celestialbodies/{id}/download # Download celestial body
POST   /api/celestialbodies/{id}/edit # Edit celestial body
POST   /api/celestialbodies/{id}/unpublish # Unpublish celestial body
POST   /api/celestialbodies/{id}/republish # Republish celestial body
POST   /api/celestialbodies/{id}/activate # Activate celestial body
POST   /api/celestialbodies/{id}/deactivate # Deactivate celestial body
```

### **Celestial Spaces Controller**
```http
GET    /api/celestialspaces          # Get all celestial spaces
GET    /api/celestialspaces/{id}     # Get celestial space by ID
POST   /api/celestialspaces          # Create celestial space
PUT    /api/celestialspaces/{id}     # Update celestial space
DELETE /api/celestialspaces/{id}     # Delete celestial space
POST   /api/celestialspaces/{id}/clone # Clone celestial space
POST   /api/celestialspaces/{id}/publish # Publish celestial space
POST   /api/celestialspaces/search   # Search celestial spaces
GET    /api/celestialspaces/{id}/versions # Get celestial space versions
POST   /api/celestialspaces/{id}/download # Download celestial space
POST   /api/celestialspaces/{id}/edit # Edit celestial space
POST   /api/celestialspaces/{id}/unpublish # Unpublish celestial space
POST   /api/celestialspaces/{id}/republish # Republish celestial space
POST   /api/celestialspaces/{id}/activate # Activate celestial space
POST   /api/celestialspaces/{id}/deactivate # Deactivate celestial space
```

### **Zomes Controller**
```http
GET    /api/zomes                    # Get all zomes
GET    /api/zomes/{id}               # Get zome by ID
POST   /api/zomes                    # Create zome
PUT    /api/zomes/{id}               # Update zome
DELETE /api/zomes/{id}               # Delete zome
POST   /api/zomes/{id}/clone         # Clone zome
POST   /api/zomes/{id}/publish       # Publish zome
POST   /api/zomes/search             # Search zomes
GET    /api/zomes/{id}/versions      # Get zome versions
POST   /api/zomes/{id}/download      # Download zome
POST   /api/zomes/{id}/edit          # Edit zome
POST   /api/zomes/{id}/unpublish     # Unpublish zome
POST   /api/zomes/{id}/republish     # Republish zome
POST   /api/zomes/{id}/activate      # Activate zome
POST   /api/zomes/{id}/deactivate    # Deactivate zome
```

### **Holons Controller**
```http
GET    /api/holons                   # Get all holons
GET    /api/holons/{id}              # Get holon by ID
POST   /api/holons                   # Create holon
PUT    /api/holons/{id}              # Update holon
DELETE /api/holons/{id}              # Delete holon
POST   /api/holons/{id}/clone        # Clone holon
POST   /api/holons/{id}/publish      # Publish holon
POST   /api/holons/search            # Search holons
GET    /api/holons/{id}/versions     # Get holon versions
POST   /api/holons/{id}/download     # Download holon
POST   /api/holons/{id}/edit         # Edit holon
POST   /api/holons/{id}/unpublish    # Unpublish holon
POST   /api/holons/{id}/republish    # Republish holon
POST   /api/holons/{id}/activate     # Activate holon
POST   /api/holons/{id}/deactivate   # Deactivate holon
```

### **Templates Controller**
```http
GET    /api/templates                # Get all templates
GET    /api/templates/{id}           # Get template by ID
POST   /api/templates                # Create template
PUT    /api/templates/{id}           # Update template
DELETE /api/templates/{id}           # Delete template
POST   /api/templates/{id}/clone     # Clone template
POST   /api/templates/{id}/publish   # Publish template
POST   /api/templates/search         # Search templates
GET    /api/templates/{id}/versions  # Get template versions
POST   /api/templates/{id}/download  # Download template
POST   /api/templates/{id}/edit      # Edit template
POST   /api/templates/{id}/unpublish # Unpublish template
POST   /api/templates/{id}/republish # Republish template
POST   /api/templates/{id}/activate # Activate template
POST   /api/templates/{id}/deactivate # Deactivate template
```

### **Libraries Controller**
```http
GET    /api/libraries                # Get all libraries
GET    /api/libraries/{id}          # Get library by ID
POST   /api/libraries                # Create library
PUT    /api/libraries/{id}           # Update library
DELETE /api/libraries/{id}           # Delete library
POST   /api/libraries/{id}/clone     # Clone library
POST   /api/libraries/{id}/publish   # Publish library
POST   /api/libraries/search        # Search libraries
GET    /api/libraries/{id}/versions # Get library versions
POST   /api/libraries/{id}/download # Download library
POST   /api/libraries/{id}/edit     # Edit library
POST   /api/libraries/{id}/unpublish # Unpublish library
POST   /api/libraries/{id}/republish # Republish library
POST   /api/libraries/{id}/activate # Activate library
POST   /api/libraries/{id}/deactivate # Deactivate library
```

### **Runtimes Controller**
```http
GET    /api/runtimes                 # Get all runtimes
GET    /api/runtimes/{id}            # Get runtime by ID
POST   /api/runtimes                 # Create runtime
PUT    /api/runtimes/{id}            # Update runtime
DELETE /api/runtimes/{id}            # Delete runtime
POST   /api/runtimes/{id}/clone     # Clone runtime
POST   /api/runtimes/{id}/publish   # Publish runtime
POST   /api/runtimes/search         # Search runtimes
GET    /api/runtimes/{id}/versions  # Get runtime versions
POST   /api/runtimes/{id}/download  # Download runtime
POST   /api/runtimes/{id}/edit      # Edit runtime
POST   /api/runtimes/{id}/unpublish # Unpublish runtime
POST   /api/runtimes/{id}/republish # Republish runtime
POST   /api/runtimes/{id}/activate # Activate runtime
POST   /api/runtimes/{id}/deactivate # Deactivate runtime
```

### **Plugins Controller**
```http
GET    /api/plugins                  # Get all plugins
GET    /api/plugins/{id}             # Get plugin by ID
POST   /api/plugins                  # Create plugin
PUT    /api/plugins/{id}             # Update plugin
DELETE /api/plugins/{id}             # Delete plugin
POST   /api/plugins/{id}/clone       # Clone plugin
POST   /api/plugins/{id}/publish     # Publish plugin
POST   /api/plugins/search           # Search plugins
GET    /api/plugins/{id}/versions    # Get plugin versions
POST   /api/plugins/{id}/download    # Download plugin
POST   /api/plugins/{id}/edit        # Edit plugin
POST   /api/plugins/{id}/unpublish   # Unpublish plugin
POST   /api/plugins/{id}/republish   # Republish plugin
POST   /api/plugins/{id}/activate    # Activate plugin
POST   /api/plugins/{id}/deactivate  # Deactivate plugin
```

### **OAPPs Controller**
```http
GET    /api/oapps                    # Get all OAPPs
GET    /api/oapps/{id}               # Get OAPP by ID
POST   /api/oapps                    # Create OAPP
PUT    /api/oapps/{id}               # Update OAPP
DELETE /api/oapps/{id}               # Delete OAPP
POST   /api/oapps/{id}/clone         # Clone OAPP
POST   /api/oapps/{id}/publish       # Publish OAPP
POST   /api/oapps/search             # Search OAPPs
GET    /api/oapps/{id}/versions      # Get OAPP versions
POST   /api/oapps/{id}/download      # Download OAPP
POST   /api/oapps/{id}/edit          # Edit OAPP
POST   /api/oapps/{id}/unpublish     # Unpublish OAPP
POST   /api/oapps/{id}/republish     # Republish OAPP
POST   /api/oapps/{id}/activate      # Activate OAPP
POST   /api/oapps/{id}/deactivate    # Deactivate OAPP
```

### **Parks Controller**
```http
GET    /api/parks                    # Get all parks
GET    /api/parks/{id}               # Get park by ID
POST   /api/parks                    # Create park
PUT    /api/parks/{id}               # Update park
DELETE /api/parks/{id}               # Delete park
GET    /api/parks/nearby             # Get nearby parks
GET    /api/parks/type/{type}        # Get parks by type
POST   /api/parks/{id}/clone         # Clone park
POST   /api/parks/{id}/publish       # Publish park
POST   /api/parks/search             # Search parks
GET    /api/parks/{id}/versions      # Get park versions
POST   /api/parks/{id}/download      # Download park
POST   /api/parks/{id}/edit          # Edit park
POST   /api/parks/{id}/unpublish     # Unpublish park
POST   /api/parks/{id}/republish     # Republish park
POST   /api/parks/{id}/activate      # Activate park
POST   /api/parks/{id}/deactivate    # Deactivate park
```

### **GeoHotSpots Controller**
```http
GET    /api/geohotspots              # Get all GeoHotSpots
GET    /api/geohotspots/{id}         # Get GeoHotSpot by ID
POST   /api/geohotspots              # Create GeoHotSpot
PUT    /api/geohotspots/{id}         # Update GeoHotSpot
DELETE /api/geohotspots/{id}         # Delete GeoHotSpot
POST   /api/geohotspots/{id}/clone   # Clone GeoHotSpot
POST   /api/geohotspots/{id}/publish # Publish GeoHotSpot
POST   /api/geohotspots/search       # Search GeoHotSpots
GET    /api/geohotspots/{id}/versions # Get GeoHotSpot versions
POST   /api/geohotspots/{id}/download # Download GeoHotSpot
POST   /api/geohotspots/{id}/edit    # Edit GeoHotSpot
POST   /api/geohotspots/{id}/unpublish # Unpublish GeoHotSpot
POST   /api/geohotspots/{id}/republish # Republish GeoHotSpot
POST   /api/geohotspots/{id}/activate # Activate GeoHotSpot
POST   /api/geohotspots/{id}/deactivate # Deactivate GeoHotSpot
```

### **STAR Controller**
```http
GET    /api/star                     # Get STAR information
GET    /api/star/status              # Get STAR status
GET    /api/star/config              # Get STAR configuration
PUT    /api/star/config              # Update STAR configuration
```

## 🔧 **Metadata Controllers**

### **Celestial Bodies MetaData Controller**
```http
GET    /api/celestialbodiesmetadata                    # Get all metadata
GET    /api/celestialbodiesmetadata/{id}              # Get specific metadata
POST   /api/celestialbodiesmetadata                  # Create metadata
PUT    /api/celestialbodiesmetadata/{id}              # Update metadata
DELETE /api/celestialbodiesmetadata/{id}           # Delete metadata
POST   /api/celestialbodiesmetadata/{id}/clone       # Clone metadata
POST   /api/celestialbodiesmetadata/{id}/publish     # Publish metadata
POST   /api/celestialbodiesmetadata/{id}/download    # Download metadata
GET    /api/celestialbodiesmetadata/{id}/versions      # Get versions
POST   /api/celestialbodiesmetadata/search           # Search metadata
POST   /api/celestialbodiesmetadata/{id}/edit        # Edit metadata
POST   /api/celestialbodiesmetadata/{id}/unpublish   # Unpublish metadata
POST   /api/celestialbodiesmetadata/{id}/republish    # Republish metadata
POST   /api/celestialbodiesmetadata/{id}/activate    # Activate metadata
POST   /api/celestialbodiesmetadata/{id}/deactivate  # Deactivate metadata
```

### **Zomes MetaData Controller**
```http
GET    /api/zomesmetadata                    # Get all metadata
GET    /api/zomesmetadata/{id}              # Get specific metadata
POST   /api/zomesmetadata                  # Create metadata
PUT    /api/zomesmetadata/{id}              # Update metadata
DELETE /api/zomesmetadata/{id}           # Delete metadata
POST   /api/zomesmetadata/{id}/clone       # Clone metadata
POST   /api/zomesmetadata/{id}/publish     # Publish metadata
POST   /api/zomesmetadata/{id}/download    # Download metadata
GET    /api/zomesmetadata/{id}/versions     # Get versions
POST   /api/zomesmetadata/search           # Search metadata
POST   /api/zomesmetadata/{id}/edit        # Edit metadata
POST   /api/zomesmetadata/{id}/unpublish   # Unpublish metadata
POST   /api/zomesmetadata/{id}/republish   # Republish metadata
POST   /api/zomesmetadata/{id}/activate    # Activate metadata
POST   /api/zomesmetadata/{id}/deactivate  # Deactivate metadata
```

### **Holons MetaData Controller**
```http
GET    /api/holonsmetadata                    # Get all metadata
GET    /api/holonsmetadata/{id}              # Get specific metadata
POST   /api/holonsmetadata                  # Create metadata
PUT    /api/holonsmetadata/{id}              # Update metadata
DELETE /api/holonsmetadata/{id}           # Delete metadata
POST   /api/holonsmetadata/{id}/clone       # Clone metadata
POST   /api/holonsmetadata/{id}/publish     # Publish metadata
POST   /api/holonsmetadata/{id}/download    # Download metadata
GET    /api/holonsmetadata/{id}/versions     # Get versions
POST   /api/holonsmetadata/search           # Search metadata
POST   /api/holonsmetadata/{id}/edit        # Edit metadata
POST   /api/holonsmetadata/{id}/unpublish   # Unpublish metadata
POST   /api/holonsmetadata/{id}/republish   # Republish metadata
POST   /api/holonsmetadata/{id}/activate    # Activate metadata
POST   /api/holonsmetadata/{id}/deactivate  # Deactivate metadata
```

## 🎯 **Common Operations**

### **CRUD Operations**
All controllers support the standard CRUD operations:
- **Create**: `POST /api/{controller}`
- **Read**: `GET /api/{controller}` and `GET /api/{controller}/{id}`
- **Update**: `PUT /api/{controller}/{id}`
- **Delete**: `DELETE /api/{controller}/{id}`

### **STARNET Operations**
All controllers support STARNET operations:
- **Publish**: `POST /api/{controller}/{id}/publish`
- **Download**: `POST /api/{controller}/{id}/download`
- **Clone**: `POST /api/{controller}/{id}/clone`
- **Search**: `POST /api/{controller}/search`
- **Versions**: `GET /api/{controller}/{id}/versions`
- **Edit**: `POST /api/{controller}/{id}/edit`
- **Unpublish**: `POST /api/{controller}/{id}/unpublish`
- **Republish**: `POST /api/{controller}/{id}/republish`
- **Activate**: `POST /api/{controller}/{id}/activate`
- **Deactivate**: `POST /api/{controller}/{id}/deactivate`

## 📊 **Response Format**

All endpoints return responses in the following format:

```json
{
  "result": "object|array",
  "isError": false,
  "message": "Success message",
  "exception": null
}
```

## 🔐 **Error Handling**

All endpoints include comprehensive error handling:
- **400 Bad Request**: Invalid request data
- **401 Unauthorized**: Authentication required
- **403 Forbidden**: Insufficient permissions
- **404 Not Found**: Resource not found
- **500 Internal Server Error**: Server error

## 📱 **SDKs**

### **JavaScript/Node.js**
```bash
npm install @oasis/star-api-client
```

### **C#/.NET**
```bash
dotnet add package OASIS.STAR.API.Client
```

### **Python**
```bash
pip install oasis-star-api-client
```

## 🚀 **Getting Started**

1. **Get API Key**: Sign up for a STAR API key
2. **Choose SDK**: Select your preferred SDK
3. **Start Building**: Use the endpoints to create amazing applications

## 📞 **Support**

- **Documentation**: [docs.oasisplatform.world/star](https://docs.oasisplatform.world/star)
- **Community**: [Discord](https://discord.gg/oasis)
- **GitHub**: [github.com/oasisplatform/star](https://github.com/oasisplatform/star)
- **Email**: star-support@oasisplatform.world

---

*This is the complete reference for all STAR API endpoints. For detailed documentation of each endpoint, see the individual controller documentation.*
