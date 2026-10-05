#!/bin/bash

# 🚀 Kadena Asset Rail Setup Script
# This script sets up the complete Asset Rail Smart Trust Platform

set -e  # Exit on any error

echo "🚀 Setting up Asset Rail Smart Trust Platform for Kadena..."
echo "=================================================="

# Check prerequisites
echo "📋 Checking prerequisites..."

if ! command -v git &> /dev/null; then
    echo "❌ Git is not installed. Please install Git first."
    exit 1
fi

if ! command -v node &> /dev/null; then
    echo "❌ Node.js is not installed. Please install Node.js first."
    exit 1
fi

if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET 8 SDK is not installed. Please install .NET 8 SDK first."
    exit 1
fi

echo "✅ All prerequisites are installed!"

# Clone repository if not already present
if [ ! -d "Asset-rail" ]; then
    echo "📥 Cloning Asset Rail repository (kadena branch)..."
    git clone -b kadena https://github.com/QuantumStreet/Asset-rail.git
    cd Asset-rail
else
    echo "📁 Asset Rail directory already exists"
    cd Asset-rail
    
    # Pull latest changes
    echo "🔄 Pulling latest changes..."
    git pull origin kadena
fi

# Verify we're in the correct directory
echo "🔍 Verifying directory structure..."
if [ ! -d "frontend" ] || [ ! -d "mvp-sc-gen-main" ]; then
    echo "❌ Error: Not in the correct directory. Please run this script from the parent directory of Asset-rail"
    echo "Expected structure:"
    echo "  Asset-rail/"
    echo "    ├── frontend/"
    echo "    └── mvp-sc-gen-main/"
    exit 1
fi
echo "✅ Directory structure verified"

# Setup Smart Contract Generator
echo "🔧 Setting up Smart Contract Generator..."
cd mvp-sc-gen-main
dotnet restore
echo "✅ Smart Contract Generator dependencies installed"

# Setup Frontend
echo "🔧 Setting up Frontend..."
cd ../frontend
npm install
echo "✅ Frontend dependencies installed"

echo ""
echo "📚 Documentation:"
echo "   Complete Guide:         ./KADENA-GET-STARTED-GUIDE.md"
echo ""
echo "🧪 Test the Trust Wizard:"
echo "   1. Open http://localhost:3000"
echo "   2. Fill out the trust creation form"
echo "   3. Generate your first Wyoming Statutory Trust contract"
echo ""
echo "📖 Next Steps:"
echo "   1. Read the KADENA-GET-STARTED-GUIDE.md for complete instructions"
echo "   2. Test the trust creation wizard"
echo "   3. Explore the API endpoints"
echo "   4. Review the contract generation capabilities"
echo ""
echo "Happy building! 🚀"


