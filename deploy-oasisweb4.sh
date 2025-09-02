#!/bin/bash

echo "🚀 Deploying OASIS Web4 Site to GitHub Pages"
echo "=============================================="

# Check if we're in the right directory
if [ ! -d "oasisweb4 site" ]; then
    echo "❌ Error: 'oasisweb4 site' directory not found!"
    echo "Please run this script from the OASIS_CLEAN root directory"
    exit 1
fi

# Check if we're on the max-build branch
current_branch=$(git branch --show-current)
if [ "$current_branch" != "max-build" ]; then
    echo "⚠️  Warning: You're not on the max-build branch (currently on: $current_branch)"
    echo "Switching to max-build branch..."
    git checkout max-build
fi

# Add all changes
echo "📦 Adding changes..."
git add .

# Check if there are changes to commit
if git diff --cached --quiet; then
    echo "ℹ️  No changes to commit"
else
    # Commit changes
    echo "💾 Committing changes..."
    git commit -m "Update OASIS Web4 site - $(date '+%Y-%m-%d %H:%M:%S')"
    
    # Push to GitHub
    echo "🌐 Pushing to GitHub..."
    git push origin max-build
    
    echo "✅ Deployment initiated!"
    echo ""
    echo "📋 Next steps:"
    echo "1. Go to https://github.com/NextGenSoftwareUK/OASIS/settings/pages"
    echo "2. Set Source to 'Deploy from a branch'"
    echo "3. Select 'gh-pages' branch and '/' folder"
    echo "4. Save the settings"
    echo "5. Your site will be available at: https://nextgensoftwareuk.github.io/OASIS/"
    echo ""
    echo "🔗 To use your custom domain (oasisweb4.com):"
    echo "1. Add 'oasisweb4.com' in the Custom domain field"
    echo "2. Update your Namesilo DNS settings (see DNS instructions below)"
else
    echo "ℹ️  No changes to commit"
fi

echo ""
echo "🌐 DNS Configuration for Namesilo:"
echo "===================================="
echo "Add these records in your Namesilo DNS settings:"
echo ""
echo "Type: CNAME"
echo "Name: @"
echo "Value: nextgensoftwareuk.github.io"
echo ""
echo "Type: CNAME"
echo "Name: www"
echo "Value: nextgensoftwareuk.github.io"
echo ""
echo "Note: GitHub Pages will automatically redirect to your custom domain"
echo "once the DNS is properly configured."
