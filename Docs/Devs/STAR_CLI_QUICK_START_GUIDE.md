# STAR CLI Quick Start Guide

## 🚀 **Get Started with STAR CLI in 5 Minutes**

The STAR CLI is the most important tool for developers working with the OASIS ecosystem. This quick start guide will have you up and running in just a few minutes.

## 📋 **Prerequisites**

- .NET SDK 8.0 or higher
- Access to OASIS platform
- Basic command-line knowledge

## 🛠️ **Installation**

### **1.1 Clone and Build**
```bash
# Clone the OASIS repository
git clone https://github.com/oasisplatform/OASIS.git
cd OASIS

# Navigate to STAR CLI
cd "STAR ODK/NextGenSoftware.OASIS.STAR.CLI"

# Build the CLI
dotnet build
```

### **1.2 First Run**
```bash
# Start the STAR CLI
dotnet run

# The CLI will guide you through:
# 1. Avatar authentication
# 2. Provider selection
# 3. Initial configuration
```

## 🎯 **Step 1: Authentication**

### **1.1 Avatar Login**
```bash
# Login with your avatar
star login --avatar "your-avatar-username"

# Or use avatar ID
star login --avatar-id "your-avatar-id"
```

### **1.2 Provider Selection**
```bash
# Set default provider
star config --provider "MongoDB"

# Available providers:
# - MongoDB
# - SQLite
# - IPFS
# - Holochain
# - Ethereum
# - And more...
```

## 🏗️ **Step 2: Create Your First STARNETHolon**

### **2.1 Create an OAPP**
```bash
# Create a new OAPP
star create oapp "My First OAPP" --description "A sample OAPP"

# The CLI will create:
# - OAPP structure
# - DNA file
# - Basic configuration
```

### **2.2 Create Supporting STARNETHolons**
```bash
# Create a Quest
star create quest "Tutorial Quest" --description "Learn the basics"

# Create an NFT
star create nft "Character NFT" --description "A unique character"

# Create an Inventory Item
star create inventoryitem "Sword" --description "A powerful weapon"
```

## 🔗 **Step 3: Link Dependencies**

### **3.1 Add Dependencies to OAPP**
```bash
# Link Quest to OAPP
star add-dependency oapp "My First OAPP" quest "Tutorial Quest"

# Link NFT to OAPP
star add-dependency oapp "My First OAPP" nft "Character NFT"

# Link Inventory Item to OAPP
star add-dependency oapp "My First OAPP" inventoryitem "Sword"
```

### **3.2 Create Complex Dependencies**
```bash
# Link Quest to NFT (Quest reward)
star add-dependency quest "Tutorial Quest" nft "Character NFT"

# Link Quest to Inventory Item (Quest item)
star add-dependency quest "Tutorial Quest" inventoryitem "Sword"

# Link Inventory Item to NFT (Item can be NFT)
star add-dependency inventoryitem "Sword" nft "Character NFT"
```

## 📦 **Step 4: Publish to STARNET**

### **4.1 Publish STARNETHolons**
```bash
# Publish OAPP to STARNET
star publish oapp "My First OAPP"

# Publish Quest
star publish quest "Tutorial Quest"

# Publish NFT
star publish nft "Character NFT"
```

### **4.2 Verify Publication**
```bash
# List published STARNETHolons
star list oapps --published
star list quests --published
star list nfts --published
```

## 🔍 **Step 5: Explore and Download**

### **5.1 Search STARNET**
```bash
# Search for OAPPs
star search oapp "game"

# Search for Quests
star search quest "adventure"

# Search for NFTs
star search nft "character"
```

### **5.2 Download and Install**
```bash
# Download an OAPP
star download oapp "Popular Game"

# Install downloaded OAPP
star install oapp "Popular Game"

# List installed STARNETHolons
star list oapps --installed
```

## 🧬 **Step 6: Understand DNA System**

### **6.1 View DNA File**
```bash
# Generate DNA for your OAPP
star generate-dna oapp "My First OAPP" --output "my-oapp-dna.json"

# View DNA file
cat my-oapp-dna.json
```

### **6.2 DNA Structure**
```json
{
  "Id": "unique-id",
  "Name": "My First OAPP",
  "Description": "A sample OAPP",
  "STARNETHolonType": "OAPP",
  "Dependencies": {
    "Quests": ["Tutorial Quest"],
    "NFTs": ["Character NFT"],
    "InventoryItems": ["Sword"]
  },
  "Version": "1.0.0",
  "PublishedOnSTARNET": true
}
```

## 🎮 **Step 7: Advanced Operations**

### **7.1 Version Management**
```bash
# Create new version
star version oapp "My First OAPP" --new-version "2.0.0"

# List versions
star versions oapp "My First OAPP"

# Switch version
star switch-version oapp "My First OAPP" "1.0.0"
```

### **7.2 Dependency Management**
```bash
# List dependencies
star list-dependencies oapp "My First OAPP"

# Remove dependency
star remove-dependency oapp "My First OAPP" quest "Tutorial Quest"

# Check for circular dependencies
star check-dependencies oapp "My First OAPP"
```

## 📊 **Step 8: Analytics and Monitoring**

### **8.1 View Statistics**
```bash
# Show download stats
star stats downloads oapp "My First OAPP"

# Show usage analytics
star analytics oapp "My First OAPP"

# Show dependency graph
star graph oapp "My First OAPP"
```

### **8.2 Monitor Performance**
```bash
# Enable monitoring
star monitor --enable

# Show performance metrics
star metrics

# Check CLI health
star health-check
```

## 🔧 **Step 9: Configuration**

### **9.1 CLI Configuration**
```bash
# Set default DNA folder
star config --dna-folder "/path/to/dna"

# Set default genesis folder
star config --genesis-folder "/path/to/genesis"

# Show current configuration
star config --show
```

### **9.2 Avatar Management**
```bash
# Switch avatar
star switch-avatar "other-avatar"

# Show current avatar
star whoami

# List available avatars
star list-avatars
```

## 🚀 **Step 10: Advanced Features**

### **10.1 Batch Operations**
```bash
# Batch create multiple STARNETHolons
star batch-create --from-file "starnetholons.json"

# Batch publish
star batch-publish --type oapp --filter "unpublished"

# Batch download
star batch-download --type quest --category "adventure"
```

### **10.2 Template Operations**
```bash
# Create from template
star create-from-template oapp "My Game" --template "game-template"

# Create template
star create-template "My Template" --from oapp "My First OAPP"

# List templates
star list-templates
```

## 🎉 **Congratulations!**

You've successfully:
- ✅ Set up STAR CLI
- ✅ Created your first STARNETHolons
- ✅ Linked dependencies
- ✅ Published to STARNET
- ✅ Downloaded and installed STARNETHolons
- ✅ Understood the DNA system
- ✅ Used advanced features

## 🚀 **Next Steps**

### **Continue Learning**
- **[STAR CLI Documentation](./STAR_CLI_DOCUMENTATION.md)** - Complete CLI reference
- **[DNA System Guide](./DNA_SYSTEM_GUIDE.md)** - Advanced DNA operations
- **[Dependency Management Guide](./DEPENDENCY_MANAGEMENT_GUIDE.md)** - Advanced dependency management

### **Explore Advanced Features**
- **[Building a Metaverse Game](./TUTORIALS/BUILDING_A_METAVERSE_GAME.md)** - Complete game development
- **[Creating Your First OAPP](./TUTORIALS/CREATING_YOUR_FIRST_OAPP.md)** - Advanced OAPP development
- **[STARNET Web UI Integration](./STARNET_WEB_UI_OVERVIEW.md)** - Web UI integration

### **Join Community**
- **Discord**: Connect with other developers
- **GitHub**: Contribute to the project
- **Documentation**: Explore advanced features
- **Support**: Get help when you need it

## 📞 **Support & Resources**

- **Documentation**: [docs.oasisplatform.world](https://docs.oasisplatform.world)
- **Community**: [Discord](https://discord.gg/oasis)
- **GitHub**: [github.com/oasisplatform](https://github.com/oasisplatform)
- **Email**: support@oasisplatform.world

## 🆘 **Common Issues**

### **Authentication Problems**
```bash
# Reset authentication
star logout
star login --avatar "your-avatar"

# Check avatar status
star whoami
```

### **Dependency Issues**
```bash
# Resolve dependencies
star resolve-dependencies oapp "My OAPP"

# Check for conflicts
star check-conflicts oapp "My OAPP"
```

### **Performance Issues**
```bash
# Clear CLI cache
star cache --clear

# Optimize CLI
star optimize

# Check CLI health
star health-check
```

---

*The STAR CLI is the most powerful tool for developers working with the OASIS ecosystem. Continue exploring to unlock its full potential!*
