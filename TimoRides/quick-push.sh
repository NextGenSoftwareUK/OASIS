#!/usr/bin/env bash

# Quick commit and push script for TimoRides
# Usage: ./quick-push.sh [backend|android] "commit message"

set -euo pipefail

GITHUB_TOKEN="${GITHUB_TOKEN:-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}"

if [[ $# -lt 2 ]]; then
  echo "Usage: ./quick-push.sh [backend|android] \"commit message\""
  exit 1
fi

TARGET="${1}"
MESSAGE="${2}"

case "${TARGET}" in
  backend)
    cd /Volumes/Storage/OASIS_CLEAN
    echo "📦 Committing backend changes..."
    git add TimoRides/ride-scheduler-be/
    git commit -m "${MESSAGE}" || echo "No changes to commit"
    
    echo "🚀 Pushing to timo-org/ride-scheduler-be..."
    git subtree split --prefix=TimoRides/ride-scheduler-be -b backend-temp
    git push https://${GITHUB_TOKEN}@github.com/timo-org/ride-scheduler-be.git backend-temp:main --force
    git branch -D backend-temp
    echo "✅ Backend pushed!"
    ;;
    
  android)
    cd /Volumes/Storage/OASIS_CLEAN/TimoRides/Timo-Android-App
    echo "📦 Committing Android changes..."
    git add -A
    git commit -m "${MESSAGE}" || echo "No changes to commit"
    
    echo "🚀 Pushing to timo-org/Timo-Android-App..."
    git push https://${GITHUB_TOKEN}@github.com/timo-org/Timo-Android-App.git main --force
    echo "✅ Android pushed!"
    ;;
    
  *)
    echo "Unknown target. Use 'backend' or 'android'"
    exit 1
    ;;
esac

echo "✨ Done!"


