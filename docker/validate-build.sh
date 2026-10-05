#!/bin/bash

# Pre-build validation script
# Checks that all required files exist and .dockerignore is configured correctly

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${PROJECT_ROOT}"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}🔍 Validating Docker Build Configuration...${NC}"
echo ""

ERRORS=0

# Check required project files
echo -e "${YELLOW}Checking project files...${NC}"

check_file() {
    if [ -f "$1" ]; then
        echo -e "  ${GREEN}✅${NC} $1"
    else
        echo -e "  ${RED}❌${NC} $1 (NOT FOUND)"
        ((ERRORS++))
    fi
}

check_file "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj"
check_file "OASIS Architecture/NextGenSoftware.OASIS.API.Core/NextGenSoftware.OASIS.API.Core.csproj"
check_file "OASIS Architecture/NextGenSoftware.OASIS.API.DNA/NextGenSoftware.OASIS.API.DNA.csproj"
check_file "ONODE/NextGenSoftware.OASIS.API.ONODE.Core/NextGenSoftware.OASIS.API.ONODE.Core.csproj"
check_file "OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/NextGenSoftware.OASIS.OASISBootLoader.csproj"
check_file "The OASIS.sln"

echo ""

# Check required directories
echo -e "${YELLOW}Checking required directories...${NC}"

check_dir() {
    if [ -d "$1" ]; then
        echo -e "  ${GREEN}✅${NC} $1"
    else
        echo -e "  ${RED}❌${NC} $1 (NOT FOUND)"
        ((ERRORS++))
    fi
}

check_dir "External Libs"
check_dir "NextGenSoftware-Libraries"
check_dir "ONODE"
check_dir "OASIS Architecture"

echo ""

# Check .dockerignore
echo -e "${YELLOW}Checking .dockerignore configuration...${NC}"

if [ -f ".dockerignore" ]; then
    echo -e "  ${GREEN}✅${NC} .dockerignore exists"
    
    if grep -q "ONODE/\*\*/bin/" .dockerignore; then
        echo -e "  ${GREEN}✅${NC} ONODE bin exclusion found"
    else
        echo -e "  ${RED}❌${NC} ONODE bin exclusion NOT found"
        ((ERRORS++))
    fi
    
    if grep -q "holochain-client-csharp" .dockerignore; then
        echo -e "  ${GREEN}✅${NC} holochain-client-csharp exclusion found"
    else
        echo -e "  ${YELLOW}⚠️${NC}  holochain-client-csharp exclusion NOT found (may be included)"
    fi
else
    echo -e "  ${RED}❌${NC} .dockerignore NOT found"
    ((ERRORS++))
fi

echo ""

# Check Dockerfile
echo -e "${YELLOW}Checking Dockerfile...${NC}"

if [ -f "docker/Dockerfile" ]; then
    echo -e "  ${GREEN}✅${NC} Dockerfile exists"
    
    if grep -q "COPY.*holochain-client-csharp" docker/Dockerfile; then
        echo -e "  ${RED}❌${NC} Dockerfile still copies holochain-client-csharp (should be removed)"
        ((ERRORS++))
    else
        echo -e "  ${GREEN}✅${NC} holochain-client-csharp not copied in Dockerfile"
    fi
    
    if grep -q "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI" docker/Dockerfile; then
        echo -e "  ${GREEN}✅${NC} ONODE WebAPI path correct"
    else
        echo -e "  ${RED}❌${NC} ONODE WebAPI path incorrect"
        ((ERRORS++))
    fi
else
    echo -e "  ${RED}❌${NC} Dockerfile NOT found"
    ((ERRORS++))
fi

echo ""

# Summary
if [ $ERRORS -eq 0 ]; then
    echo -e "${GREEN}✅ All checks passed! Ready to build.${NC}"
    exit 0
else
    echo -e "${RED}❌ Found $ERRORS error(s). Please fix before building.${NC}"
    exit 1
fi

