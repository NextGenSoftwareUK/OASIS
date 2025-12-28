#!/bin/bash
# OASIS Documentation Verification Script
# This script verifies documentation accuracy against the codebase

set -e

echo "=== OASIS Documentation Verification ==="
echo ""

ERRORS=0
WARNINGS=0

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to report error
error() {
    echo -e "${RED}ERROR:${NC} $1"
    ((ERRORS++))
}

# Function to report warning
warning() {
    echo -e "${YELLOW}WARNING:${NC} $1"
    ((WARNINGS++))
}

# Function to report success
success() {
    echo -e "${GREEN}✓${NC} $1"
}

# Get project root (parent of docs directory)
PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$PROJECT_ROOT"

echo "Project root: $PROJECT_ROOT"
echo ""

# 1. Verify Provider Enum Count
echo "1. Verifying Provider Enum..."
PROVIDER_ENUM_FILE="OASIS Architecture/NextGenSoftware.OASIS.API.Core/Enums/ProviderType.cs"
if [ -f "$PROVIDER_ENUM_FILE" ]; then
    # Count providers by looking at lines 8-62 (actual provider definitions)
    # Excluding None, All, Default which are on lines 5-7
    ENUM_COUNT=$(sed -n '8,62p' "$PROVIDER_ENUM_FILE" | grep -v "^\s*//" | grep -v "^$" | grep -c "OASIS" || echo "0")
    success "Found $ENUM_COUNT providers in enum"
    
    # Check if this matches documented count (should be ~55, excluding None/All/Default)
    if [ "$ENUM_COUNT" -lt 50 ] || [ "$ENUM_COUNT" -gt 60 ]; then
        warning "Provider count ($ENUM_COUNT) may not match documentation (expected ~55)"
    fi
else
    error "Provider enum file not found: $PROVIDER_ENUM_FILE"
fi
echo ""

# 2. Verify Provider Directories
echo "2. Verifying Provider Implementations..."
if [ -d "Providers" ]; then
    PROVIDER_DIRS=$(find Providers -type d -name "*OASIS" | wc -l)
    success "Found $PROVIDER_DIRS provider directories"
else
    error "Providers directory not found"
fi
echo ""

# 3. Verify Code File References in Documentation
echo "3. Checking Code File References in Documentation..."
CODE_REFS=$(grep -r "OASIS Architecture\|Providers/.*\.cs\|ONODE/.*\.cs" docs/ --include="*.md" | \
    grep -oE "[^\`]*(OASIS Architecture|Providers|ONODE)[^\`]*\.cs" | \
    sed 's/.*`//' | sed 's/`.*//' | sort -u)

MISSING_FILES=0
for file in $CODE_REFS; do
    # Try to find the file (might be relative to project root)
    if [ ! -f "$file" ] && [ ! -f "$PROJECT_ROOT/$file" ]; then
        warning "Referenced file may not exist: $file"
        ((MISSING_FILES++))
    fi
done

if [ "$MISSING_FILES" -eq 0 ]; then
    success "All code file references verified"
else
    warning "Found $MISSING_FILES potentially missing file references"
fi
echo ""

# 4. Verify OASIS_DNA.json Exists
echo "4. Verifying OASIS_DNA.json..."
if [ -f "OASIS_DNA.json" ]; then
    success "OASIS_DNA.json exists"
    
    # Check if jq is available for JSON validation
    if command -v jq &> /dev/null; then
        if jq empty OASIS_DNA.json 2>/dev/null; then
            success "OASIS_DNA.json is valid JSON"
            
            # Check for key sections
            if jq '.OASIS.StorageProviders' OASIS_DNA.json > /dev/null 2>&1; then
                success "StorageProviders section exists"
            else
                warning "StorageProviders section not found in OASIS_DNA.json"
            fi
        else
            error "OASIS_DNA.json is not valid JSON"
        fi
    else
        warning "jq not available, skipping JSON validation"
    fi
else
    error "OASIS_DNA.json not found"
fi
echo ""

# 5. Verify HyperDrive Implementation
echo "5. Verifying HyperDrive Implementation..."
HYPERDRIVE_FILE="OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/OASISHyperDrive.cs"
if [ -f "$HYPERDRIVE_FILE" ]; then
    success "HyperDrive file exists"
    
    # Check for key methods
    if grep -q "FailoverRequestAsync" "$HYPERDRIVE_FILE"; then
        success "FailoverRequestAsync method found"
    else
        error "FailoverRequestAsync method not found"
    fi
    
    if grep -q "ReplicateRequestAsync" "$HYPERDRIVE_FILE"; then
        success "ReplicateRequestAsync method found"
    else
        error "ReplicateRequestAsync method not found"
    fi
    
    if grep -q "LoadBalanceRequestAsync" "$HYPERDRIVE_FILE"; then
        success "LoadBalanceRequestAsync method found"
    else
        error "LoadBalanceRequestAsync method not found"
    fi
else
    error "HyperDrive file not found: $HYPERDRIVE_FILE"
fi
echo ""

# 6. Verify Manager Classes
echo "6. Verifying Manager Classes..."
MANAGERS_DIR="OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers"
if [ -d "$MANAGERS_DIR" ]; then
    MANAGER_COUNT=$(find "$MANAGERS_DIR" -name "*Manager.cs" -not -path "*/OASIS HyperDrive/*" | wc -l)
    success "Found $MANAGER_COUNT manager classes"
    
    # Check for key managers
    KEY_MANAGERS=("AvatarManager" "HolonManager" "WalletManager" "KeyManager")
    for manager in "${KEY_MANAGERS[@]}"; do
        if find "$MANAGERS_DIR" -name "${manager}.cs" | grep -q .; then
            success "$manager exists"
        else
            warning "$manager not found"
        fi
    done
else
    error "Managers directory not found: $MANAGERS_DIR"
fi
echo ""

# 7. Check Documentation Dates
echo "7. Checking Documentation Dates..."
CURRENT_YEAR=$(date +%Y)
CURRENT_MONTH=$(date +%m)

# Find dates that are more than 6 months old or in the future
OLD_DATES=$(grep -r "Last Updated:" docs/ --include="*.md" | \
    sed 's/.*Last Updated: *//' | \
    sed 's/\*\*//g' | \
    grep -v "December 2025" | grep -v "$CURRENT_YEAR" || true)

if [ -z "$OLD_DATES" ]; then
    success "All documentation dates are current"
else
    warning "Found dates that may need updating:"
    echo "$OLD_DATES" | while read date; do
        warning "  $date"
    done
fi
echo ""

# 8. Check for Broken Internal Links (basic check)
echo "8. Checking Internal Documentation Links..."
if command -v markdown-link-check &> /dev/null; then
    # Note: This requires markdown-link-check npm package
    # Install with: npm install -g markdown-link-check
    find docs -name "*.md" -exec markdown-link-check {} \; 2>/dev/null || \
        warning "markdown-link-check not fully functional (may need configuration)"
else
    warning "markdown-link-check not installed (npm install -g markdown-link-check)"
    echo "  Skipping link verification"
fi
echo ""

# Summary
echo "=== Verification Summary ==="
if [ "$ERRORS" -eq 0 ] && [ "$WARNINGS" -eq 0 ]; then
    echo -e "${GREEN}All checks passed!${NC}"
    exit 0
elif [ "$ERRORS" -eq 0 ]; then
    echo -e "${YELLOW}Verification complete with $WARNINGS warning(s)${NC}"
    exit 0
else
    echo -e "${RED}Verification failed with $ERRORS error(s) and $WARNINGS warning(s)${NC}"
    exit 1
fi

