#!/bin/bash

# BillionsHealed Deployment Script
echo "🚀 BillionsHealed Deployment Setup"
echo "=================================="

# Check if we're in the right directory
if [ ! -f "README.md" ]; then
    echo "❌ Error: Please run this script from the BillionsHealed root directory"
    exit 1
fi

echo "📁 Preparing deployment files..."

# Create deployment directory structure
mkdir -p deploy/frontend
mkdir -p deploy/backend

# Copy frontend files
cp frontend/index.html deploy/frontend/
cp frontend/styles.css deploy/frontend/
cp frontend/app.js deploy/frontend/

# Copy backend files
cp -r backend/* deploy/backend/

echo "✅ Frontend files prepared in deploy/frontend/"
echo "✅ Backend files prepared in deploy/backend/"
echo ""
echo "📋 Next Steps:"
echo "=============="
echo ""
echo "1. Frontend (GoDaddy):"
echo "   - Upload files from deploy/frontend/ to your GoDaddy public_html folder"
echo "   - Files: index.html, styles.css, app.js"
echo ""
echo "2. Backend (Heroku/Railway/Vercel):"
echo "   - Deploy files from deploy/backend/ to your chosen platform"
echo "   - Set environment variables (see DEPLOYMENT_GUIDE.md)"
echo ""
echo "3. Update API URL:"
echo "   - Edit deploy/frontend/app.js"
echo "   - Change API_BASE_URL to your backend URL"
echo ""
echo "📖 For detailed instructions, see DEPLOYMENT_GUIDE.md"
echo ""
echo "🔗 Quick Deploy Options:"
echo "========================"
echo ""
echo "Option 1 - Heroku (Recommended):"
echo "  cd deploy/backend"
echo "  heroku create billionshealed-api"
echo "  git init && git add . && git commit -m 'Deploy'"
echo "  git push heroku main"
echo ""
echo "Option 2 - Railway:"
echo "  - Connect GitHub repo to Railway"
echo "  - Deploy from GitHub"
echo ""
echo "Option 3 - Vercel:"
echo "  cd deploy/backend"
echo "  vercel"
echo ""
echo "✨ Deployment files ready! Choose your hosting option and follow the guide."

