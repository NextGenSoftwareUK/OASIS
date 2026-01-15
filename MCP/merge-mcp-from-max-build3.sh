#!/bin/bash

# Script to safely merge MCP files from max-build3 branch
# Preserves existing files (like solana-wallet-tools.ts)

set -e

echo "🔄 Merging MCP files from max-build3 branch..."
echo ""

# Get the commit hash with MCP implementation
COMMIT="052e864fb"

# Files to preserve (won't be overwritten)
PRESERVE_FILES=(
  "solana-wallet-tools.ts"
  "SOLANA_WALLET_MCP_ENDPOINTS.md"
  "TEST_MCP_ENDPOINTS.md"
  "test-mcp-endpoints.ts"
  "merge-mcp-from-max-build3.sh"
)

# Create backup
echo "📦 Creating backup of current MCP directory..."
BACKUP_DIR="../MCP_backup_$(date +%Y%m%d_%H%M%S)"
mkdir -p "$BACKUP_DIR"
cp -r . "$BACKUP_DIR/" 2>/dev/null || true
echo "   Backup created at: $BACKUP_DIR"
echo ""

# Get list of files from the commit
echo "📋 Getting file list from commit $COMMIT..."
FILES=$(git show "$COMMIT" --name-only | grep "^MCP/" | sed 's|^MCP/||')

# Copy files, preserving existing ones
echo "📥 Copying files from max-build3..."
COPIED=0
SKIPPED=0

for file in $FILES; do
  # Skip if file is in preserve list and already exists
  if [[ " ${PRESERVE_FILES[@]} " =~ " ${file} " ]] && [[ -f "$file" ]]; then
    echo "   ⏭️  Skipping (preserved): $file"
    ((SKIPPED++))
    continue
  fi
  
  # Skip node_modules and dist
  if [[ "$file" == *"node_modules"* ]] || [[ "$file" == *"/dist/"* ]]; then
    continue
  fi
  
  # Get file from commit
  if git show "$COMMIT:MCP/$file" > /dev/null 2>&1; then
    # Create directory if needed
    mkdir -p "$(dirname "$file")"
    
    # Copy file
    git show "$COMMIT:MCP/$file" > "$file"
    echo "   ✅ Copied: $file"
    ((COPIED++))
  fi
done

echo ""
echo "✅ Merge complete!"
echo "   Files copied: $COPIED"
echo "   Files preserved: $SKIPPED"
echo ""
echo "📝 Next steps:"
echo "   1. Review the changes"
echo "   2. Run: npm install (in MCP directory)"
echo "   3. Test the MCP server"
echo ""
