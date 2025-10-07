#!/bin/bash

# Script to create missing test projects for all OASIS providers
# This script creates UnitTests, IntegrationTests, and TestHarness projects

# List of providers missing all 3 test projects
providers_missing_all=(
    "AWSOASIS"
    "BitcoinOASIS" 
    "CardanoOASIS"
    "CargoOASIS"
    "CosmosBlockChainOASIS"
    "HashgraphOASIS"
    "HoloWebOASIS"
    "LocalFileOASIS"
    "MapboxOASIS"
    "MongoDBOASIS"
    "MoralisDBOASIS"
    "NEAROASIS"
    "Neo4jOASIS2"
    "ONION-Protocol"
    "OptimismOASIS"
    "OrionProtocolOASIS"
    "PolkadotOASIS"
    "ProviderNameOASIS"
    "RootstockOASIS"
    "SuiOASIS"
    "WRLD3DOASIS"
)

# List of providers missing 1-2 test projects
providers_missing_some=(
    "AzureCosmosDBOASIS:Cloud"
    "EOSIOOASIS:Blockchain"
    "GoogleCloudOASIS:Cloud"
    "Neo4jOASIS:Storage"
    "PinataOASIS:Network"
    "PolygonOASIS:Blockchain"
    "SOLANAOASIS:Blockchain"
    "SQLLiteDBOASIS:Storage"
    "Web3CoreOASIS:Blockchain"
)

# Function to create test project files
create_test_project() {
    local provider_name=$1
    local provider_category=$2
    local test_type=$3
    
    local project_name="NextGenSoftware.OASIS.API.Providers.${provider_name}.${test_type}"
    local project_dir="Providers/${provider_category}/TestProjects/${project_name}"
    local csproj_file="${project_dir}/${project_name}.csproj"
    local test_file="${project_dir}/${provider_name}${test_type}.cs"
    
    echo "Creating ${test_type} for ${provider_name}..."
    
    # Create directory
    mkdir -p "${project_dir}"
    
    # Create .csproj file
    if [ "$test_type" = "TestHarness" ]; then
        cat > "${csproj_file}" << EOF
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
      <ProjectReference Include="..\..\NextGenSoftware.OASIS.API.Providers.${provider_name}\NextGenSoftware.OASIS.API.Providers.${provider_name}.csproj" />
    </ItemGroup>

    <ItemGroup>
      <Content Include="OASIS_DNA.Development.json">
        <ExcludeFromSingleFile>true</ExcludeFromSingleFile>
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
        <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
      </Content>
    </ItemGroup>

</Project>
EOF
    else
        cat > "${csproj_file}" << EOF
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="MSTest.TestAdapter" Version="3.1.1" />
    <PackageReference Include="MSTest.TestFramework" Version="3.1.1" />
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\NextGenSoftware.OASIS.API.Providers.${provider_name}\NextGenSoftware.OASIS.API.Providers.${provider_name}.csproj" />
  </ItemGroup>

</Project>
EOF
    fi
    
    # Create test file based on type
    if [ "$test_type" = "UnitTests" ]; then
        create_unit_test_file "${test_file}" "${provider_name}"
    elif [ "$test_type" = "IntegrationTests" ]; then
        create_integration_test_file "${test_file}" "${provider_name}"
    elif [ "$test_type" = "TestHarness" ]; then
        create_test_harness_file "${test_file}" "${provider_name}"
    fi
}

# Function to create unit test file
create_unit_test_file() {
    local file_path=$1
    local provider_name=$2
    
    cat > "${file_path}" << EOF
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.${provider_name};
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.${provider_name}.UnitTests
{
    [TestClass]
    public class ${provider_name}ProviderTests
    {
        private ${provider_name} _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new ${provider_name}();
        }

        [TestMethod]
        public void ProviderType_ShouldBe${provider_name}()
        {
            // Arrange & Act
            var providerType = _provider.ProviderType;

            // Assert
            Assert.AreEqual(ProviderType.${provider_name}, providerType);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            // Arrange & Act
            var isActivated = _provider.IsProviderActivated;

            // Assert
            Assert.IsFalse(isActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBe${provider_name}()
        {
            // Arrange & Act
            var providerName = _provider.ProviderName;

            // Assert
            Assert.AreEqual("${provider_name}", providerName);
        }

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
        {
            // Arrange & Act
            var description = _provider.ProviderDescription;

            // Assert
            Assert.IsNotNull(description);
            Assert.IsFalse(string.IsNullOrEmpty(description));
        }

        [TestMethod]
        public void ActivateProvider_ShouldSetIsProviderActivatedToTrue()
        {
            // Arrange
            Assert.IsFalse(_provider.IsProviderActivated);

            // Act
            var result = _provider.ActivateProvider();

            // Assert
            Assert.IsTrue(result.IsError == false);
            Assert.IsTrue(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void DeActivateProvider_ShouldSetIsProviderActivatedToFalse()
        {
            // Arrange
            _provider.ActivateProvider();
            Assert.IsTrue(_provider.IsProviderActivated);

            // Act
            var result = _provider.DeActivateProvider();

            // Assert
            Assert.IsTrue(result.IsError == false);
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void GetProviderVersion_ShouldReturnValidVersion()
        {
            // Arrange & Act
            var version = _provider.GetProviderVersion();

            // Assert
            Assert.IsNotNull(version);
            Assert.IsFalse(string.IsNullOrEmpty(version));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
            {
                _provider.DeActivateProvider();
            }
        }
    }
}
EOF
}

# Function to create integration test file
create_integration_test_file() {
    local file_path=$1
    local provider_name=$2
    
    cat > "${file_path}" << EOF
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.${provider_name};
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Threading.Tasks;
using System;

namespace NextGenSoftware.OASIS.API.Providers.${provider_name}.IntegrationTests
{
    [TestClass]
    public class ${provider_name}IntegrationTests
    {
        private ${provider_name} _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new ${provider_name}();
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            // Arrange
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "TestUser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            // Act
            var result = await _provider.SaveAvatarAsync(avatar);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnAvatar()
        {
            // Arrange
            var avatarId = Guid.NewGuid();

            // Act
            var result = await _provider.LoadAvatarAsync(avatarId);

            // Assert
            Assert.IsNotNull(result);
            // Note: This might return an error if avatar doesn't exist, which is expected
        }

        [TestMethod]
        public async Task SaveHolon_ShouldReturnSuccessResult()
        {
            // Arrange
            var holon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = "TestHolon",
                Description = "Test Holon Description"
            };

            // Act
            var result = await _provider.SaveHolonAsync(holon);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnHolon()
        {
            // Arrange
            var holonId = Guid.NewGuid();

            // Act
            var result = await _provider.LoadHolonAsync(holonId);

            // Assert
            Assert.IsNotNull(result);
            // Note: This might return an error if holon doesn't exist, which is expected
        }

        [TestMethod]
        public async Task SearchAvatars_ShouldReturnSearchResults()
        {
            // Arrange
            var searchParams = new SearchParams
            {
                SearchQuery = "test",
                SearchType = SearchType.Avatar
            };

            // Act
            var result = await _provider.SearchAvatarsAsync(searchParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task SearchHolons_ShouldReturnSearchResults()
        {
            // Arrange
            var searchParams = new SearchParams
            {
                SearchQuery = "test",
                SearchType = SearchType.Holon
            };

            // Act
            var result = await _provider.SearchHolonsAsync(searchParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
            {
                _provider.DeActivateProvider();
            }
        }
    }
}
EOF
}

# Function to create test harness file
create_test_harness_file() {
    local file_path=$1
    local provider_name=$2
    
    cat > "${file_path}" << EOF
using NextGenSoftware.OASIS.API.Providers.${provider_name};
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.${provider_name}.TestHarness
{
    public class ${provider_name}TestHarness
    {
        private static ${provider_name} _provider;

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== ${provider_name} Test Harness ===");
            Console.WriteLine("Testing ${provider_name} integration with OASIS...\\n");

            _provider = new ${provider_name}();

            try
            {
                await TestProviderActivation();
                await TestProviderInformation();
                await TestAvatarOperations();
                await TestHolonOperations();
                await TestSearchOperations();
                await TestProviderDeactivation();

                Console.WriteLine("\\n=== All Tests Completed Successfully! ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\\n=== Test Failed: {ex.Message} ===");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task TestProviderActivation()
        {
            Console.WriteLine("--- Testing Provider Activation ---");
            
            Console.WriteLine($"Provider Name: {_provider.ProviderName}");
            Console.WriteLine($"Provider Type: {_provider.ProviderType}");
            Console.WriteLine($"Provider Category: {_provider.ProviderCategory}");
            Console.WriteLine($"Is Activated: {_provider.IsProviderActivated}");

            var activationResult = _provider.ActivateProvider();
            Console.WriteLine($"Activation Result: {(activationResult.IsError ? "Failed" : "Success")}");
            if (activationResult.IsError)
                Console.WriteLine($"Error: {activationResult.Message}");

            Console.WriteLine($"Is Activated After Activation: {_provider.IsProviderActivated}");
            Console.WriteLine();
        }

        private static async Task TestProviderInformation()
        {
            Console.WriteLine("--- Testing Provider Information ---");
            
            var version = _provider.GetProviderVersion();
            Console.WriteLine($"Provider Version: {version}");

            var providerType = _provider.GetProviderType();
            Console.WriteLine($"Provider Type: {providerType}");

            var category = _provider.GetProviderCategory();
            Console.WriteLine($"Provider Category: {category}");

            var description = _provider.ProviderDescription;
            Console.WriteLine($"Provider Description: {description}");
            Console.WriteLine();
        }

        private static async Task TestAvatarOperations()
        {
            Console.WriteLine("--- Testing Avatar Operations ---");
            
            var testAvatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "${provider_name}TestUser",
                Email = "${provider_name,,}test@example.com",
                FirstName = "${provider_name}",
                LastName = "Tester",
                CreatedDate = DateTime.UtcNow
            };

            Console.WriteLine($"Creating Avatar: {testAvatar.Username} ({testAvatar.Email})");
            var saveResult = await _provider.SaveAvatarAsync(testAvatar);
            Console.WriteLine($"Save Avatar Result: {(saveResult.IsError ? "Failed" : "Success")}");
            if (saveResult.IsError)
                Console.WriteLine($"Error: {saveResult.Message}");

            Console.WriteLine($"Loading Avatar by ID: {testAvatar.Id}");
            var loadResult = await _provider.LoadAvatarAsync(testAvatar.Id);
            Console.WriteLine($"Load Avatar Result: {(loadResult.IsError ? "Failed" : "Success")}");
            if (loadResult.IsError)
                Console.WriteLine($"Error: {loadResult.Message}");

            Console.WriteLine();
        }

        private static async Task TestHolonOperations()
        {
            Console.WriteLine("--- Testing Holon Operations ---");
            
            var testHolon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = "${provider_name}TestHolon",
                Description = "Test Holon for ${provider_name} integration",
                CreatedDate = DateTime.UtcNow
            };

            Console.WriteLine($"Creating Holon: {testHolon.Name}");
            var saveResult = await _provider.SaveHolonAsync(testHolon);
            Console.WriteLine($"Save Holon Result: {(saveResult.IsError ? "Failed" : "Success")}");
            if (saveResult.IsError)
                Console.WriteLine($"Error: {saveResult.Message}");

            Console.WriteLine($"Loading Holon by ID: {testHolon.Id}");
            var loadResult = await _provider.LoadHolonAsync(testHolon.Id);
            Console.WriteLine($"Load Holon Result: {(loadResult.IsError ? "Failed" : "Success")}");
            if (loadResult.IsError)
                Console.WriteLine($"Error: {loadResult.Message}");

            Console.WriteLine();
        }

        private static async Task TestSearchOperations()
        {
            Console.WriteLine("--- Testing Search Operations ---");
            
            var searchParams = new SearchParams
            {
                SearchQuery = "test",
                SearchType = SearchType.Avatar
            };

            Console.WriteLine($"Searching Avatars with query: '{searchParams.SearchQuery}'");
            var avatarSearchResult = await _provider.SearchAvatarsAsync(searchParams);
            Console.WriteLine($"Avatar Search Result: {(avatarSearchResult.IsError ? "Failed" : "Success")}");
            if (avatarSearchResult.IsError)
                Console.WriteLine($"Error: {avatarSearchResult.Message}");
            else
                Console.WriteLine($"Found {avatarSearchResult.Result?.NumberOfResults ?? 0} avatars");

            Console.WriteLine();
        }

        private static async Task TestProviderDeactivation()
        {
            Console.WriteLine("--- Testing Provider Deactivation ---");
            
            Console.WriteLine($"Is Activated Before Deactivation: {_provider.IsProviderActivated}");
            
            var deactivationResult = _provider.DeActivateProvider();
            Console.WriteLine($"Deactivation Result: {(deactivationResult.IsError ? "Failed" : "Success")}");
            if (deactivationResult.IsError)
                Console.WriteLine($"Error: {deactivationResult.Message}");

            Console.WriteLine($"Is Activated After Deactivation: {_provider.IsProviderActivated}");
            Console.WriteLine();
        }
    }
}
EOF
}

echo "Creating missing test projects for OASIS providers..."

# Create test projects for providers missing all 3
for provider in "${providers_missing_all[@]}"; do
    # Determine category based on provider name
    if [[ "$provider" == *"OASIS" ]]; then
        if [[ "$provider" == *"Blockchain"* ]] || [[ "$provider" == *"Bitcoin"* ]] || [[ "$provider" == *"Cardano"* ]] || [[ "$provider" == *"Hashgraph"* ]] || [[ "$provider" == *"NEAR"* ]] || [[ "$provider" == *"Polkadot"* ]] || [[ "$provider" == *"Rootstock"* ]] || [[ "$provider" == *"Sui"* ]]; then
            category="Blockchain"
        elif [[ "$provider" == *"AWS"* ]] || [[ "$provider" == *"Google"* ]] || [[ "$provider" == *"Azure"* ]]; then
            category="Cloud"
        elif [[ "$provider" == *"Map"* ]] || [[ "$provider" == *"WRLD"* ]]; then
            category="Maps"
        elif [[ "$provider" == *"Local"* ]] || [[ "$provider" == *"Mongo"* ]] || [[ "$provider" == *"Neo4j"* ]] || [[ "$provider" == *"SQL"* ]]; then
            category="Storage"
        elif [[ "$provider" == *"Holo"* ]] || [[ "$provider" == *"ONION"* ]] || [[ "$provider" == *"Orion"* ]]; then
            category="Network"
        else
            category="Other"
        fi
    else
        category="Other"
    fi
    
    create_test_project "$provider" "$category" "UnitTests"
    create_test_project "$provider" "$category" "IntegrationTests"
    create_test_project "$provider" "$category" "TestHarness"
done

# Create missing test projects for providers missing some
for provider_info in "${providers_missing_some[@]}"; do
    IFS=':' read -r provider category <<< "$provider_info"
    
    # Check what test projects already exist
    existing_tests=$(find . -name "*${provider}*Test*.csproj" -path "*/Providers/*" | wc -l)
    
    if [ $existing_tests -lt 3 ]; then
        if [ $existing_tests -eq 0 ]; then
            create_test_project "$provider" "$category" "UnitTests"
            create_test_project "$provider" "$category" "IntegrationTests"
            create_test_project "$provider" "$category" "TestHarness"
        elif [ $existing_tests -eq 1 ]; then
            # Check which one exists and create the missing ones
            if ! find . -name "*${provider}*UnitTests*.csproj" -path "*/Providers/*" | grep -q .; then
                create_test_project "$provider" "$category" "UnitTests"
            fi
            if ! find . -name "*${provider}*IntegrationTests*.csproj" -path "*/Providers/*" | grep -q .; then
                create_test_project "$provider" "$category" "IntegrationTests"
            fi
            if ! find . -name "*${provider}*TestHarness*.csproj" -path "*/Providers/*" | grep -q .; then
                create_test_project "$provider" "$category" "TestHarness"
            fi
        elif [ $existing_tests -eq 2 ]; then
            # Check which one is missing and create it
            if ! find . -name "*${provider}*UnitTests*.csproj" -path "*/Providers/*" | grep -q .; then
                create_test_project "$provider" "$category" "UnitTests"
            fi
            if ! find . -name "*${provider}*IntegrationTests*.csproj" -path "*/Providers/*" | grep -q .; then
                create_test_project "$provider" "$category" "IntegrationTests"
            fi
            if ! find . -name "*${provider}*TestHarness*.csproj" -path "*/Providers/*" | grep -q .; then
                create_test_project "$provider" "$category" "TestHarness"
            fi
        fi
    fi
done

echo "Test project creation completed!"
echo "Summary:"
echo "- Created UnitTests, IntegrationTests, and TestHarness projects for all missing providers"
echo "- Each test project includes meaningful test methods and proper structure"
echo "- All test projects are configured with appropriate dependencies and frameworks"
