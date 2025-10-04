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

## 🏗️ **STARNETHolon Operations**

### **Basic CRUD Operations**
```bash
# Create a new STARNETHolon
star create oapp "My OAPP" --description "A sample OAPP"
star create quest "Adventure Quest" --description "An epic adventure"
star create nft "Cool NFT" --description "A unique NFT"

# Edit existing STARNETHolon
star edit oapp "My OAPP" --description "Updated description"
star edit quest "Adventure Quest" --add-reward "100 coins"

# Delete STARNETHolon
star delete oapp "My OAPP"
star delete quest "Adventure Quest"
```

### **Publishing and Downloading**
```bash
# Publish to STARNET
star publish oapp "My OAPP"
star publish quest "Adventure Quest"
star publish nft "Cool NFT"

# Download from STARNET
star download oapp "Popular OAPP"
star download quest "Epic Quest"
star download nft "Rare NFT"

# Install downloaded STARNETHolons
star install oapp "Popular OAPP"
star install quest "Epic Quest"
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
