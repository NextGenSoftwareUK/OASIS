# STAR CLI Documentation

## 📋 **Overview**

The STAR CLI (Command Line Interface) is the most important tool for developers working with the OASIS ecosystem. It provides comprehensive command-line access to all STARNET functionality, including STARNETHolon management, DNA system operations, and dependency management.

## 🎯 **Key Features**

### **STARNETHolon Management**
- **Create, Edit, Delete**: Full CRUD operations for all STARNETHolon types
- **Publish & Download**: Publish to STARNET and download from others
- **Version Control**: Manage versions and rollbacks
- **Search & Discovery**: Find and explore STARNETHolons

### **DNA System**
- **Dependency Management**: Link any STARNETHolon to any other STARNETHolon
- **DNA Generation**: Automatic DNA file creation and management
- **Dependency Resolution**: Handle complex dependency chains
- **Metadata Management**: Advanced metadata system integration

### **STARNETHolon Types**
- **OAPPs**: OASIS Applications
- **Templates**: Application templates
- **Runtimes**: Execution environments
- **Libraries**: Code libraries and frameworks
- **NFTs**: Non-fungible tokens
- **GeoNFTs**: Location-based NFTs
- **GeoHotSpots**: Geographic interaction points
- **Quests**: Game quests and objectives
- **Missions**: Mission system
- **Chapters**: Story chapters
- **InventoryItems**: Game items and rewards
- **CelestialSpaces**: Virtual spaces
- **CelestialBodies**: Virtual worlds
- **Zomes**: Code modules
- **Holons**: Data objects
- **MetaData**: CelestialBodies, Zomes, and Holons metadata

## 🚀 **Getting Started**

### **Installation**
```bash
# Clone the repository
git clone https://github.com/oasisplatform/OASIS.git
cd OASIS

# Build the CLI
cd "STAR ODK/NextGenSoftware.OASIS.STAR.CLI"
dotnet build
dotnet run
```

### **First Run**
```bash
# Start the STAR CLI
dotnet run

# The CLI will guide you through:
# 1. Avatar authentication
# 2. Provider selection
# 3. Initial configuration
```

## 🎮 **Main Commands**

### **System Commands**
- `ignite` - Ignite STAR & Boot The OASIS
- `extinguish` - Extinguish STAR & Shutdown The OASIS
- `help [full]` - Show help page (full shows all sub-commands)
- `version` - Show versions of STAR ODK, COSMIC ORM, OASIS Runtime & Providers
- `status` - Show the status of STAR ODK
- `exit` - Exit the STAR CLI

### **Core Development Commands**
- `light` - Creates a new OAPP (Zomes/Holons/Star/Planet/Moon) at the given genesis folder location
- `light wiz` - Start the Light Wizard
- `light transmute` - Creates a new Planet (OApp) from hApp DNA
- `bang` - Generate a whole metaverse or part of one (Multiverses, Universes, Dimensions, etc.)
- `wiz` - Start the STAR ODK Wizard for creating tailored OAPPs

### **OAPP Management Commands**
- `seed` - Deploy/Publish an OAPP to the STARNET Store
- `unseed` - Undeploy/Unpublish an OAPP from the STARNET Store
- `reseed` - Redeploy/Republish an OAPP to the STARNET Store
- `dust` - Delete an OAPP (removes from STARNET if published)
- `radiate` - Highlight the OAPP in the STARNET Store (Admin/Wizards Only)
- `emit` - Show how much light the OAPP is emitting (karma score)
- `reflect` - Show stats of the OAPP
- `evolve` - Upgrade/update an OAPP
- `mutate` - Import/Export hApp, dApp & others
- `love` - Send/Receive Love for an avatar
- `burst` - View network stats/management/settings
- `super` - Reserved For Future Use
- `net` - Launch the STARNET Library/Store
- `gate` - Opens the STARGATE to the OASIS Portal
- `api [oasis]` - Opens the WEB5 STAR API (or WEB4 OASIS API if oasis included)

## 🚀 **Getting Started**

### **Installation**
```bash
# Clone the repository
git clone https://github.com/oasisplatform/OASIS.git
cd OASIS

# Build the CLI
cd "STAR ODK/NextGenSoftware.OASIS.STAR.CLI"
dotnet build
dotnet run
```

### **First Run**
```bash
# Start the STAR CLI
dotnet run

# The CLI will guide you through:
# 1. Avatar authentication
# 2. Provider selection
# 3. Initial configuration
```

## 🏗️ **STARNETHolon Operations**

### **OAPP Commands**
```bash
# OAPP Management
oapp create                    # Create a new OAPP
oapp update {id/name}          # Update an existing OAPP
oapp delete {id/name}          # Delete an OAPP
oapp publish {id/name}         # Publish OAPP to STARNET
oapp unpublish {id/name}       # Unpublish OAPP from STARNET
oapp republish {id/name}       # Republish OAPP to STARNET
oapp activate {id/name}        # Activate OAPP
oapp deactivate {id/name}      # Deactivate OAPP
oapp download {id/name}        # Download OAPP
oapp install {id/name}         # Install OAPP
oapp uninstall {id/name}       # Uninstall OAPP
oapp show {id/name} [detailed] # Show OAPP details
oapp list [allVersions] [forAllAvatars] [detailed] # List OAPPs
oapp search [allVersions] [forAllAvatars] # Search OAPPs

# OAPP Dependencies
oapp adddependency {id/name}    # Add dependency to OAPP
oapp removedependency {id/name} # Remove dependency from OAPP

# OAPP Templates
oapp template create            # Create OAPP template
oapp template update {id/name}  # Update OAPP template
oapp template delete {id/name}  # Delete OAPP template
oapp template publish {id/name} # Publish OAPP template
oapp template unpublish {id/name} # Unpublish OAPP template
oapp template republish {id/name} # Republish OAPP template
oapp template activate {id/name} # Activate OAPP template
oapp template deactivate {id/name} # Deactivate OAPP template
oapp template download {id/name} # Download OAPP template
oapp template install {id/name}  # Install OAPP template
oapp template uninstall {id/name} # Uninstall OAPP template
oapp template show {id/name} [detailed] # Show OAPP template
oapp template list [allVersions] [forAllAvatars] [detailed] # List OAPP templates
oapp template search [allVersions] [forAllAvatars] # Search OAPP templates
```

### **hApp Commands**
```bash
# hApp Management
happ create                    # Create a new hApp
happ update {id/name}          # Update an existing hApp
happ delete {id/name}          # Delete an hApp
happ publish {id/name}         # Publish hApp to STARNET
happ unpublish {id/name}       # Unpublish hApp from STARNET
happ republish {id/name}       # Republish hApp to STARNET
happ activate {id/name}        # Activate hApp
happ deactivate {id/name}      # Deactivate hApp
happ download {id/name}        # Download hApp
happ install {id/name}         # Install hApp
happ uninstall {id/name}       # Uninstall hApp
happ show {id/name} [detailed] # Show hApp details
happ list [allVersions] [forAllAvatars] [detailed] # List hApps
happ search [allVersions] [forAllAvatars] # Search hApps

# hApp Dependencies
happ adddependency {id/name}    # Add dependency to hApp
happ removedependency {id/name} # Remove dependency from hApp
```

### **Runtime Commands**
```bash
# Runtime Management
runtime create                 # Create a new runtime
runtime update {id/name}       # Update an existing runtime
runtime delete {id/name}       # Delete a runtime
runtime publish {id/name}      # Publish runtime
runtime unpublish {id/name}    # Unpublish runtime
runtime republish {id/name}    # Republish runtime
runtime activate {id/name}     # Activate runtime
runtime deactivate {id/name}   # Deactivate runtime
runtime download {id/name}     # Download runtime
runtime install {id/name}      # Install runtime
runtime uninstall {id/name}    # Uninstall runtime
runtime show {id/name} [detailed] # Show runtime details
runtime list [allVersions] [forAllAvatars] [detailed] # List runtimes
runtime search [allVersions] [forAllAvatars] # Search runtimes

# Runtime Dependencies
runtime adddependency {id/name}    # Add dependency to runtime
runtime removedependency {id/name} # Remove dependency from runtime
```

### **Library Commands**
```bash
# Library Management
lib create                     # Create a new library
lib update {id/name}           # Update an existing library
lib delete {id/name}           # Delete a library
lib publish {id/name}          # Publish library
lib unpublish {id/name}        # Unpublish library
lib republish {id/name}        # Republish library
lib activate {id/name}         # Activate library
lib deactivate {id/name}       # Deactivate library
lib download {id/name}         # Download library
lib install {id/name}          # Install library
lib uninstall {id/name}        # Uninstall library
lib show {id/name} [detailed]   # Show library details
lib list [allVersions] [forAllAvatars] [detailed] # List libraries
lib search [allVersions] [forAllAvatars] # Search libraries

# Library Dependencies
lib adddependency {id/name}     # Add dependency to library
lib removedependency {id/name} # Remove dependency from library
```

### **Celestial Space Commands**
```bash
# Celestial Space Management
celestialspace create          # Create a celestial space
celestialspace update {id/name} # Update celestial space
celestialspace delete {id/name} # Delete celestial space
celestialspace publish {id/name} # Publish celestial space
celestialspace unpublish {id/name} # Unpublish celestial space
celestialspace show {id/name} [detailed] # Show celestial space
celestialspace list [allVersions] [forAllAvatars] [detailed] # List celestial spaces
celestialspace search [allVersions] [forAllAvatars] # Search celestial spaces

# Celestial Space Dependencies
celestialspace adddependency {id/name}    # Add dependency to celestial space
celestialspace removedependency {id/name} # Remove dependency from celestial space
```

### **Celestial Body Commands**
```bash
# Celestial Body Management
celestialbody create           # Create a celestial body
celestialbody update {id/name} # Update celestial body
celestialbody delete {id/name} # Delete celestial body
celestialbody publish {id/name} # Publish celestial body
celestialbody unpublish {id/name} # Unpublish celestial body
celestialbody show {id/name}   # Show celestial body
celestialbody list [allVersions] [forAllAvatars] [detailed] # List celestial bodies
celestialbody search [allVersions] [forAllAvatars] # Search celestial bodies

# Celestial Body Dependencies
celestialbody adddependency {id/name}    # Add dependency to celestial body
celestialbody removedependency {id/name} # Remove dependency from celestial body

# Celestial Body Metadata
celestialbody metadata create  # Create celestial body metadata
celestialbody metadata update {id/name} # Update celestial body metadata
celestialbody metadata delete {id/name} # Delete celestial body metadata
celestialbody metadata publish {id/name} # Publish celestial body metadata
celestialbody metadata unpublish {id/name} # Unpublish celestial body metadata
celestialbody metadata show {id/name} # Show celestial body metadata
celestialbody metadata list [allVersions] [forAllAvatars] [detailed] # List celestial body metadata
celestialbody metadata search [allVersions] [forAllAvatars] # Search celestial body metadata
```

### **Zome Commands**
```bash
# Zome Management
zome create                    # Create a zome (module)
zome update {id/name}          # Update zome (can upload zome.cs file)
zome delete {id/name}          # Delete zome
zome publish {id/name}         # Publish zome to STARNET
zome unpublish {id/name}       # Unpublish zome from STARNET
zome show {id/name}            # Show zome
zome list [allVersions] [forAllAvatars] [detailed] # List zomes
zome search [allVersions] [forAllAvatars] # Search zomes

# Zome Dependencies
zome adddependency {id/name}    # Add dependency to zome
zome removedependency {id/name} # Remove dependency from zome

# Zome Metadata
zome metadata create           # Create zome metadata
zome metadata update {id/name} # Update zome metadata
zome metadata delete {id/name} # Delete zome metadata
zome metadata publish {id/name} # Publish zome metadata
zome metadata unpublish {id/name} # Unpublish zome metadata
zome metadata show {id/name}   # Show zome metadata
zome metadata list [allVersions] [forAllAvatars] # List zome metadata
zome metadata search [allVersions] [forAllAvatars] # Search zome metadata
```

### **Holon Commands**
```bash
# Holon Management
holon create json={holonJSONFile} # Create holon from JSON file
holon create wiz                # Start Create Holon Wizard
holon update {id/name}          # Update holon
holon delete {id/name}          # Delete holon
holon publish {id/name}         # Publish holon to STARNET
holon unpublish {id/name}       # Unpublish holon from STARNET
holon show {id/name} [detailed] # Show holon
holon list [allVersions] [forAllAvatars] # List holons
holon search [allVersions] [forAllAvatars] # Search holons

# Holon Metadata
holon metadata create          # Create holon metadata
holon metadata update {id/name} # Update holon metadata
holon metadata delete {id/name} # Delete holon metadata
holon metadata publish {id/name} # Publish holon metadata
holon metadata unpublish {id/name} # Unpublish holon metadata
holon metadata show {id/name} [detailed] # Show holon metadata
holon metadata list [allVersions] [forAllAvatars] [detailed] # List holon metadata
holon metadata search [allVersions] [forAllAvatars] # Search holon metadata
```

### **Quest System Commands**
```bash
# Chapter Commands
chapter create                 # Create a chapter
chapter update {id/name}       # Update chapter
chapter delete {id/name}       # Delete chapter
chapter publish {id/name}      # Publish chapter to STARNET
chapter unpublish {id/name}    # Unpublish chapter from STARNET
chapter show {id/name}         # Show chapter
chapter list [allVersions] [forAllAvatars] # List chapters
chapter search [allVersions] [forAllAvatars] # Search chapters

# Mission Commands
mission create                 # Create a mission
mission update {id/name}       # Update mission
mission delete {id/name}       # Delete mission
mission publish {id/name}      # Publish mission to STARNET
mission unpublish {id/name}    # Unpublish mission from STARNET
mission show {id/name} [detailed] # Show mission
missions list [allVersions] [forAllAvatars] [detailed] # List missions
missions search [allVersions] [forAllAvatars] # Search missions

# Quest Commands
quest create                   # Create a quest
quest update {id/name}         # Update quest
quest delete {id/name}         # Delete quest
quest publish {id/name}        # Publish quest to STARNET
quest unpublish {id/name}      # Unpublish quest from STARNET
quest show {id/name} [detailed] # Show quest
quest list [allVersions] [forAllAvatars] [detailed] # List quests
quest search [allVersions] [forAllAvatars] # Search quests
```

### **NFT Commands**
```bash
# NFT Management
nft mint                       # Mint WEB4 OASIS NFT
nft update {id/name}           # Update NFT
nft burn {id/name}             # Burn NFT
nft send {id/name}             # Send NFT cross-chain
nft import [web3]              # Import WEB4 OASIS NFT JSON file
nft export                     # Export WEB4 OASIS NFT as JSON
nft clone                      # Clone WEB4 OASIS NFT
nft convert                    # Convert to different WEB3 NFT standards
nft publish {id/name}          # Publish NFT to STARNET
nft unpublish {id/name}        # Unpublish NFT from STARNET
nft show {id/name} [detailed]  # Show NFT
nft list [allVersions] [forAllAvatars] [detailed] # List NFTs
nft search [allVersions] [forAllAvatars] # Search NFTs

# GeoNFT Management
geonft mint                    # Mint OASIS Geo-NFT
geonft update {id/name}        # Update GeoNFT
geonft burn {id/name}          # Burn GeoNFT
geonft place {id/name}         # Place NFT in Our World/AR World
geonft send {id/name}          # Send GeoNFT cross-chain
geonft import                  # Import WEB4 OASIS Geo-NFT JSON file
geonft export                  # Export WEB4 OASIS Geo-NFT as JSON
geonft clone                   # Clone WEB4 OASIS Geo-NFT
geonft convert                 # Convert to different WEB3 NFT standards
geonft publish {id/name}       # Publish GeoNFT to STARNET
geonft unpublish {id/name}     # Unpublish GeoNFT from STARNET
geonft show {id/name} [detailed] # Show GeoNFT
geonft list [allVersions] [forAllAvatars] [detailed] # List GeoNFTs
geonft search [allVersions] [forAllAvatars] # Search GeoNFTs

# GeoHotSpot Commands
geohotspot create              # Create geo-hotspot
geohotspot update {id/name}    # Update geo-hotspot
geohotspot delete {id/name}    # Delete geo-hotspot
geohotspot publish {id/name}   # Publish geo-hotspot to STARNET
geohotspot unpublish {id/name} # Unpublish geo-hotspot from STARNET
geohotspot show {id/name} [detailed] # Show geo-hotspot
geohotspots list [allVersions] [forAllAvatars] [detailed] # List geo-hotspots
geohotspots search [allVersions] [forAllAvatars] # Search geo-hotspots

# Inventory Item Commands
inventoryitem create         # Create inventory item
inventoryitem update {id/name}   # Update inventory item
inventoryitem delete {id/name}   # Delete inventory item
inventoryitem publish {id/name}  # Publish inventory item to STARNET
inventoryitem unpublish {id/name} # Unpublish inventory item from STARNET
inventoryitem show {id/name} [detailed] # Show inventory item
inventoryitem list [allVersions] [forAllAvatars] [detailed] # List inventory items
inventoryitem search [allVersions] [forAllAvatars] # Search inventory items
```

### **Plugin Commands**
```bash
# Plugin Management
plugin create                  # Create a plugin
plugin update {id/name}        # Update plugin
plugin delete {id/name}        # Delete plugin
plugin publish {id/name}       # Publish plugin to STARNET
plugin unpublish {id/name}     # Unpublish plugin from STARNET
plugin show {id/name} [detailed] # Show plugin
plugin list [allVersions] [forAllAvatars] # List plugins
plugin list installed          # List installed plugins
plugin list uninstalled        # List uninstalled plugins
plugin list unpublished        # List unpublished plugins
plugin list deactivated        # List deactivated plugins
plugin search [allVersions] [forAllAvatars] # Search plugins
plugin install {id/name}       # Install plugin
plugin uninstall {id/name}     # Uninstall plugin
```

### **Avatar Commands**
```bash
# Avatar Management
avatar beamin                   # Beam in (log in)
avatar beamout                  # Beam out (log out)
avatar whoisbeamedin            # Display who is currently beamed in
avatar show me                  # Display currently beamed in avatar details
avatar show {id/username}       # Show avatar details
avatar edit                     # Edit currently beamed in avatar
avatar list [detailed]          # List all avatars
avatar search                   # Search avatars
avatar forgotpassword           # Send forgot password email
avatar resetpassword            # Reset password using token
```

### **Karma Commands**
```bash
# Karma System
karma list                      # Display karma thresholds
```

### **Keys Commands**
```bash
# Key Management
keys link privateKey [walletId] [privateKey] # Link private key to wallet
keys link publicKey [walletId] [publicKey]  # Link public key to wallet
keys link genKeyPair [walletId]             # Generate and link key pair
keys generateKeyPair                        # Generate key pair
keys clearCache                             # Clear cache
keys get provideruniquestoragekey {providerType} # Get provider unique storage key
keys get providerpublickeys {providerType} # Get provider public keys
keys get avataridforprovideruniquestoragekey {avatarId} # Get provider private keys
keys list                                   # Show keys for beamed in avatar
```

### **Wallet Commands**
```bash
# Wallet Management
wallet sendtoken {walletAddress} {token} {amount} # Send token to wallet
wallet transfer {fromWalletId/name} {amount} {toWalletId/name} # Transfer between wallets
wallet get {publickey}                      # Get wallet by public key
wallet getDefault                           # Get default wallet
wallet setDefault {walletId}                # Set default wallet
wallet import privateKey {privateKey}       # Import wallet with private key
wallet import publicKey {publicKey}         # Import wallet with public key
wallet import secretPhase {secretPhase}     # Import wallet with secret phase
wallet import json {jsonFile}               # Import wallet with JSON file
wallet add                                  # Add wallet
wallet list                                 # List wallets
wallet balance {walletId}                   # Get wallet balance
wallet balance                              # Get total balance for all wallets
```

### **Map Commands**
```bash
# Map Management
map setprovider {mapProviderType}           # Set map provider
map draw3dobject {3dObjectPath} {x} {y}     # Draw 3D object on map
map draw2dsprite {2dSpritePath} {x} {y}     # Draw 2D sprite on map
map draw2dspriteonhud {2dSpritePath}        # Draw 2D sprite on HUD
map placeHolon {HolonId/name} {x} {y}      # Place holon on map
map placeBuilding {BuildingId/name} {x} {y} # Place building on map
map placeQuest {QuestId/name} {x} {y}      # Place quest on map
map placeGeoNFT {GeoNFTId/name} {x} {y}    # Place GeoNFT on map
map placeGeoHotSpot {GeoHotSpotId/name} {x} {y} # Place GeoHotSpot on map
map placeOAPP {OAPPId/name} {x} {y}       # Place OAPP on map
map pamLeft                                 # Pan map left
map pamRight                                # Pan map right
map pamUp                                   # Pan map up
map pamDown                                 # Pan map down
map zoomOut                                 # Zoom map out
map zoomIn                                  # Zoom map in
map zoomToHolon {HolonId/name}              # Zoom to holon
map zoomToBuilding {BuildingId/name}        # Zoom to building
map zoomToQuest {QuestId/name}              # Zoom to quest
map zoomToGeoNFT {GeoNFTId/name}            # Zoom to GeoNFT
map zoomToGeoHotSpot {GeoHotSpotId/name}    # Zoom to GeoHotSpot
map zoomToOAPP {OAPPId/name}                # Zoom to OAPP
map zoomToCoOrds {x} {y}                   # Zoom to coordinates
map drawRouteOnMap {startX} {startY} {endX} {endY} # Draw route on map
map drawRouteBetweenHolons {fromHolonId} {toHolonId} # Draw route between holons
map drawRouteBetweenBuildings {fromBuildingId} {toBuildingId} # Draw route between buildings
map drawRouteBetweenQuests {fromQuestId} {toQuestId} # Draw route between quests
map drawRouteBetweenGeoNFTs {fromGeoNFTId} {toGeoNFTId} # Draw route between GeoNFTs
map drawRouteBetweenGeoHotSpots {fromGeoHotSpotId} {toGeoHotSpotId} # Draw route between GeoHotSpots
map drawRouteBetweenOAPPs {fromOAPPId/name} {toOAPPId/name} # Draw route between OAPPs
```

### **SEEDS Commands**
```bash
# SEEDS Integration
seeds balance {telosAccountName/avatarId}   # Get SEEDS balance
seeds organisations                          # Get SEEDS organisations
seeds organisation {organisationName}       # Get organisation
seeds pay {telosAccountName/avatarId}       # Pay with SEEDS
seeds donate {telosAccountName/avatarId}    # Donate with SEEDS
seeds reward {telosAccountName/avatarId}    # Reward with SEEDS
seeds invite {telosAccountName/avatarId}    # Send SEEDS invite
seeds accept {telosAccountName/avatarId}    # Accept SEEDS invite
seeds qrcode {telosAccountName/avatarId}    # Generate sign-in QR code
```

### **Data Commands**
```bash
# Data Management
data save {key} {value}                     # Save data for avatar
data load {key}                             # Load data for avatar
data delete {key}                           # Delete data for avatar
data list                                   # List all data for avatar
```

### **OLAND Commands**
```bash
# OLAND Management
oland price                                 # Get OLAND price
oland purchase                              # Purchase OLAND
oland load {id}                             # Load OLAND
oland save                                  # Save OLAND
oland delete {id}                           # Delete OLAND
oland list [allVersions] [forAllAvatars]   # List OLAND
```

### **ONODE Commands**
```bash
# ONODE Management
onode start                                 # Start OASIS Node
onode stop                                  # Stop OASIS Node
onode status                                # Show ONODE stats
onode config                                # Open ONODE OASISDNA
onode providers                             # Show OASIS Providers
onode startprovider {ProviderName}          # Start provider
onode stopprovider {ProviderName}           # Stop provider
```

### **HyperNET Commands**
```bash
# HyperNET Management
hypernet start                              # Start HoloNET P2P HyperNET Service
hypernet stop                               # Stop HyperNET Service
hypernet status                             # Show HyperNET stats
hypernet config                             # Open HyperNET DNA
```

### **ONET Commands**
```bash
# ONET Management
onet status                                 # Show ONET stats
onet providers                              # Show OASIS Providers across ONET
```

### **Configuration Commands**
```bash
# Configuration
config cosmicdetailedoutput {enable/disable/status} # Enable/disable COSMIC detailed output
config starstatusdetailedoutput {enable/disable/status} # Enable/disable STAR ODK detailed output
```

### **Testing Commands**
```bash
# Testing
runcosmictests {OAPPType} {dnaFolder} {genesisFolder} # Run STAR ODK/COSMIC tests
runoasisapitests                           # Run OASIS API tests
```

## 🔗 **Dependency Management**

### **Understanding Dependencies**
The DNA system allows ANY STARNETHolon to depend on ANY other STARNETHolon:

```json
{
  "Dependencies": {
    "OAPPs": ["game-engine-oapp", "ui-framework-oapp"],
    "Runtimes": ["nodejs-runtime", "python-runtime"],
    "Libraries": ["three-js-library", "socket-io-library"],
    "Templates": ["game-template", "ui-template"],
    "NFTs": ["character-nft", "weapon-nft"],
    "GeoNFTs": ["location-geonft", "landmark-geonft"],
    "GeoHotSpots": ["spawn-point", "shop-location"],
    "Quests": ["tutorial-quest", "main-quest"],
    "Missions": ["daily-mission", "weekly-mission"],
    "Chapters": ["intro-chapter", "story-chapter"],
    "InventoryItems": ["sword-item", "potion-item"],
    "CelestialSpaces": ["game-world", "lobby-space"],
    "CelestialBodies": ["earth-world", "mars-world"],
    "Zomes": ["game-logic-zome", "ui-zome"],
    "Holons": ["player-holon", "game-state-holon"],
    "CelestialBodiesMetaDataDNA": ["world-metadata"],
    "ZomesMetaDataDNA": ["logic-metadata"],
    "HolonsMetaDataDNA": ["data-metadata"]
  }
}
```

### **Adding Dependencies**
```bash
# Add dependencies to an OAPP
star add-dependency oapp "My Game" quest "Tutorial Quest"
star add-dependency oapp "My Game" nft "Character NFT"
star add-dependency oapp "My Game" inventoryitem "Sword Item"

# Add dependencies to a Quest
star add-dependency quest "Epic Quest" oapp "Game Engine"
star add-dependency quest "Epic Quest" geonft "Treasure Location"
star add-dependency quest "Epic Quest" inventoryitem "Reward Item"

# Add dependencies to an NFT
star add-dependency nft "Character NFT" oapp "Animation System"
star add-dependency nft "Character NFT" zome "Character Logic"
```

### **Removing Dependencies**
```bash
# Remove dependencies
star remove-dependency oapp "My Game" quest "Old Quest"
star remove-dependency quest "Epic Quest" nft "Old NFT"
```

## 🧬 **DNA System**

### **DNA File Structure**
```json
{
  "Id": "unique-id",
  "Name": "STARNETHolon Name",
  "Description": "Description",
  "STARNETHolonType": "OAPP|Quest|NFT|etc",
  "Dependencies": {
    "OAPPs": ["dependency1", "dependency2"],
    "Quests": ["quest1", "quest2"],
    "NFTs": ["nft1", "nft2"],
    "InventoryItems": ["item1", "item2"]
  },
  "MetaData": {},
  "CreatedByAvatarId": "avatar-id",
  "Version": "1.0.0",
  "PublishedOnSTARNET": false
}
```

### **DNA Operations**
```bash
# Generate DNA for existing STARNETHolon
star generate-dna oapp "My OAPP"

# Load DNA and create STARNETHolon
star load-dna "path/to/dna.json"

# Update DNA with new dependencies
star update-dna oapp "My OAPP" --add-dependency quest "New Quest"

# Export DNA
star export-dna oapp "My OAPP" --output "my-oapp-dna.json"
```

## 🔍 **Search and Discovery**

### **Search Commands**
```bash
# Search for STARNETHolons
star search oapp "game"
star search quest "adventure"
star search nft "character"

# Advanced search with filters
star search oapp --category "gaming" --rating "4+"
star search quest --difficulty "easy" --reward "coins"

# Search by dependencies
star search --depends-on oapp "Game Engine"
star search --used-by quest "Tutorial Quest"
```

### **List Commands**
```bash
# List all STARNETHolons
star list oapps
star list quests
star list nfts

# List with filters
star list oapps --created-by "my-avatar"
star list quests --published
star list nfts --installed

# List dependencies
star list-dependencies oapp "My OAPP"
star list-dependents quest "Tutorial Quest"
```

## 📊 **Version Management**

### **Version Operations**
```bash
# Create new version
star version oapp "My OAPP" --new-version "2.0.0"

# List versions
star versions oapp "My OAPP"

# Switch to specific version
star switch-version oapp "My OAPP" "1.5.0"

# Compare versions
star compare-versions oapp "My OAPP" "1.0.0" "2.0.0"
```

### **Publishing Versions**
```bash
# Publish specific version
star publish-version oapp "My OAPP" "2.0.0"

# Unpublish version
star unpublish-version oapp "My OAPP" "1.0.0"

# Republish version
star republish-version oapp "My OAPP" "2.0.0"
```

## 🎮 **Advanced Features**

### **Batch Operations**
```bash
# Batch create multiple STARNETHolons
star batch-create --from-file "starnetholons.json"

# Batch publish
star batch-publish --type oapp --filter "unpublished"

# Batch download
star batch-download --type quest --category "adventure"
```

### **Template Operations**
```bash
# Create from template
star create-from-template oapp "My Game" --template "game-template"

# Create template
star create-template "My Template" --from oapp "My OAPP"

# List templates
star list-templates
```

### **Metadata Operations**
```bash
# Create metadata
star create-metadata celestialbody "World Metadata"
star create-metadata zome "Logic Metadata"
star create-metadata holon "Data Metadata"

# Link metadata
star link-metadata oapp "My OAPP" celestialbody-metadata "World Metadata"
```

## 🔧 **Configuration**

### **CLI Configuration**
```bash
# Set default provider
star config --provider "MongoDB"

# Set default DNA folder
star config --dna-folder "/path/to/dna"

# Set default genesis folder
star config --genesis-folder "/path/to/genesis"

# Show current configuration
star config --show
```

### **Avatar Management**
```bash
# Login with avatar
star login --avatar "my-avatar"

# Switch avatar
star switch-avatar "other-avatar"

# Show current avatar
star whoami
```

## 📈 **Analytics and Monitoring**

### **Usage Statistics**
```bash
# Show download statistics
star stats downloads oapp "My OAPP"

# Show usage analytics
star analytics oapp "My OAPP" --period "30days"

# Show dependency graph
star graph oapp "My OAPP"
```

### **Performance Monitoring**
```bash
# Monitor CLI performance
star monitor --enable

# Show performance metrics
star metrics

# Optimize CLI
star optimize
```

## 🚀 **Best Practices**

### **Development Workflow**
1. **Plan Dependencies**: Map out required STARNETHolons
2. **Create Base Components**: Start with foundational STARNETHolons
3. **Build Dependencies**: Create dependent STARNETHolons
4. **Test Integration**: Verify all dependencies work
5. **Publish System**: Publish complete system to STARNET

### **Dependency Management**
- **Keep Dependencies Minimal**: Only include necessary dependencies
- **Use Semantic Versioning**: Follow versioning best practices
- **Document Dependencies**: Clearly document why each dependency exists
- **Test Dependency Changes**: Verify changes don't break dependent STARNETHolons

### **DNA Best Practices**
- **Regular DNA Updates**: Keep DNA files current
- **Version Control**: Track DNA file changes
- **Backup DNA**: Maintain DNA file backups
- **Validate DNA**: Ensure DNA files are valid

## 🆘 **Troubleshooting**

### **Common Issues**
```bash
# Dependency resolution issues
star resolve-dependencies oapp "My OAPP"

# Clear CLI cache
star cache --clear

# Reset CLI configuration
star config --reset

# Check CLI health
star health-check
```

### **Debug Mode**
```bash
# Enable debug logging
star --debug create oapp "My OAPP"

# Verbose output
star --verbose list oapps

# Trace operations
star --trace publish oapp "My OAPP"
```

## 📚 **Integration with STARNET Web UI**

### **CLI to Web UI**
- **CLI Operations**: All CLI operations are reflected in the Web UI
- **Real-time Sync**: Changes appear immediately in the Web UI
- **Cross-Platform**: Use CLI and Web UI interchangeably

### **Web UI to CLI**
- **Export Operations**: Export from Web UI for CLI use
- **Import Operations**: Import CLI-created STARNETHolons
- **Synchronization**: Keep both interfaces in sync

## 📞 **Support and Resources**

### **Documentation**
- **[STAR CLI Reference](./STAR_CLI_REFERENCE.md)** - Complete command reference
- **[DNA System Guide](./DNA_SYSTEM_GUIDE.md)** - Advanced DNA operations
- **[Dependency Management](./DEPENDENCY_MANAGEMENT_GUIDE.md)** - Dependency best practices

### **Community Support**
- **Discord**: [Join our Discord](https://discord.gg/oasis)
- **GitHub**: [Contribute on GitHub](https://github.com/oasisplatform)
- **Documentation**: [docs.oasisplatform.world](https://docs.oasisplatform.world)

---

*The STAR CLI is the most powerful tool for developers working with the OASIS ecosystem, providing complete control over STARNETHolons and their dependencies.*
