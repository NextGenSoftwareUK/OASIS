#!/bin/bash

echo "🚀 Deploying BillionsHealed Backend to Heroku"
echo "=============================================="
echo ""

# Check if logged in
if ! heroku auth:whoami 2>/dev/null; then
    echo "Not logged in to Heroku. Please login first:"
    echo "Email: oasisweb4@gmail.com"
    echo ""
    heroku login
fi

# Create Heroku app
echo "Creating Heroku app..."
heroku create billionshealed-api

# Deploy
echo "Deploying to Heroku..."
git push heroku main

# Set environment variables
echo "Setting environment variables..."
heroku config:set NODE_ENV=production
heroku config:set CORS_ORIGIN=https://billionshealed.com

# Show app info
echo ""
echo "✅ Deployment complete!"
echo ""
echo "Backend URL:"
heroku info -s | grep web_url | cut -d= -f2
echo ""
echo "Next steps:"
echo "1. Copy the URL above"
echo "2. Update frontend/app.js with: const API_BASE_URL = 'YOUR_URL/api';"
echo "3. Redeploy frontend: cd ../frontend && surge . billionshealed.com"
echo ""
echo "Check logs with: heroku logs --tail"
