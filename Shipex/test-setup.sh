#!/bin/bash

# Shipex Pro Test Setup Script
# This script helps set up the test project structure

set -e

echo "🚀 Setting up Shipex Pro Test Project..."

# Navigate to Shipex directory
cd "$(dirname "$0")"

# Create test project
echo "📦 Creating test project..."
dotnet new xunit -n NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests -f net8.0

cd NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests

# Add required packages
echo "📚 Adding NuGet packages..."
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 8.0.0
dotnet add package Moq --version 4.20.70
dotnet add package FluentAssertions --version 6.12.0
dotnet add package coverlet.collector --version 6.0.2
dotnet add package MongoDB.Driver --version 2.19.0
dotnet add package Microsoft.Extensions.DependencyInjection --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration --version 8.0.0
dotnet add package Microsoft.Extensions.Logging --version 8.0.0

# Add project reference
echo "🔗 Adding project reference..."
dotnet add reference ../NextGenSoftware.OASIS.API.Providers.ShipexProOASIS/NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.csproj

# Create directory structure
echo "📁 Creating test directory structure..."
mkdir -p Services
mkdir -p Repositories
mkdir -p Controllers
mkdir -p Integration
mkdir -p Api
mkdir -p TestHelpers

# Create basic test file
echo "📝 Creating sample test file..."
cat > Services/RateServiceTests.cs << 'EOF'
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Tests.Services;

public class RateServiceTests
{
    [Fact]
    public void Test_ShouldPass()
    {
        // Arrange
        var expected = true;
        
        // Act
        var actual = true;
        
        // Assert
        actual.Should().Be(expected);
    }
}
EOF

# Create test configuration
echo "⚙️ Creating test configuration..."
cat > appsettings.Test.json << 'EOF'
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "shipex_test"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
EOF

# Restore packages
echo "📥 Restoring packages..."
dotnet restore

# Build project
echo "🔨 Building test project..."
dotnet build

# Run tests
echo "🧪 Running initial tests..."
dotnet test

echo "✅ Test project setup complete!"
echo ""
echo "Next steps:"
echo "1. Read TESTING_GUIDE.md for detailed examples"
echo "2. Start implementing tests in Services/, Repositories/, etc."
echo "3. Run tests with: dotnet test"
echo ""
echo "Happy testing! 🎉"




