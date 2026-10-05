#!/bin/bash

# Test script for Telegram CRM local setup

echo "🧪 Testing Telegram CRM Setup"
echo "=============================="
echo ""

# Check if .NET is installed
echo "1. Checking .NET SDK..."
if command -v dotnet &> /dev/null; then
    dotnet --version
else
    echo "❌ .NET SDK not found. Please install .NET 8.0 SDK"
    exit 1
fi
echo "✅ .NET SDK found"
echo ""

# Check if project builds
echo "2. Building Telegcrm project..."
cd "$(dirname "$0")"
if dotnet build; then
    echo "✅ Build successful"
else
    echo "❌ Build failed"
    exit 1
fi
echo ""

# Check MongoDB connection (if local)
echo "3. Checking MongoDB..."
if command -v mongosh &> /dev/null; then
    echo "✅ MongoDB client found"
    echo "   To test connection: mongosh 'mongodb://localhost:27017'"
else
    echo "⚠️  MongoDB client not found (optional if using Atlas)"
fi
echo ""

echo "✅ All checks passed!"
echo ""
echo "Next steps:"
echo "1. Add project reference to WebAPI project"
echo "2. Update TelegramBotService.cs (see QUICK_START.md)"
echo "3. Register services in your startup code"
echo "4. Update OASIS_DNA.json with OasisAvatarId"
echo "5. Run your WebAPI project"
echo ""
echo "See QUICK_START.md for detailed instructions"

