#!/bin/bash

# 🚂 OASIS Railway Deployment Script
# This script helps you prepare and deploy your OASIS ecosystem to Railway

echo "🚀 OASIS Railway Deployment Setup"
echo "=================================="

# Check if we're in the right directory
if [ ! -f "README.md" ]; then
    echo "❌ Please run this script from the OASIS root directory"
    exit 1
fi

echo "✅ Found OASIS project directory"

# Check if Railway CLI is installed
if ! command -v railway &> /dev/null; then
    echo "📦 Installing Railway CLI..."
    npm install -g @railway/cli
else
    echo "✅ Railway CLI already installed"
fi

# Login to Railway
echo "🔐 Please login to Railway:"
railway login

# Create new project
echo "🏗️  Creating new Railway project..."
railway init

echo ""
echo "🎉 Setup complete! Next steps:"
echo ""
echo "1. 📁 Deploy OASIS API Web API:"
echo "   railway add --service oasis-api"
echo "   railway connect oasis-api"
echo "   cd ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI"
echo "   railway up"
echo ""
echo "2. 🌐 Deploy STAR WEB UI:"
echo "   railway add --service star-web-ui"
echo "   railway connect star-web-ui"
echo "   cd oasisweb4.com"
echo "   railway up"
echo ""
echo "3. 🏢 Deploy OASIS OPORTAL (optional):"
echo "   railway add --service oasis-oportal"
echo "   railway connect oasis-oportal"
echo "   cd ONODE/NextGenSoftware.OASIS.API.ONODE.OPORTAL"
echo "   railway up"
echo ""
echo "📖 For detailed instructions, see: RAILWAY_DEPLOYMENT_GUIDE.md"
echo ""
echo "🚀 Happy deploying!"
