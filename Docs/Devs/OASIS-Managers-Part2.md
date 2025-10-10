# OASIS Managers Complete Guide - Part 2

## Additional Managers

### 4. HolonManager

The `HolonManager` handles all holon-related operations in the OASIS system.

#### Key Features:
- **Holon Lifecycle**: Create, read, update, delete holons
- **Hierarchical Structure**: Manage parent-child relationships
- **Search Operations**: Search holons by various criteria
- **Provider Integration**: Works with all OASIS providers
- **Data Persistence**: Saves holon data across multiple storage providers

#### Main Methods:

```csharp
// Holon Management
public async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon)
public async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id)
public async Task<OASISResult<List<IHolon>>> LoadAllHolonsAsync()
public async Task<OASISResult<List<IHolon>>> LoadHolonsForParentAsync(Guid parentId)

// Holon Search
public async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams)
public async Task<OASISResult<List<IHolon>>> SearchHolonsAsync(string searchTerm)
```

#### Usage Example:

```csharp
// Create a new holon
var holon = new Holon
{
    Name = "My Holon",
    Description = "A sample holon",
    HolonType = HolonType.Holon
};

var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);

if (!saveResult.IsError && saveResult.Result != null)
{
    Console.WriteLine($"Holon created with ID: {saveResult.Result.Id}");
}

// Search holons
var searchResult = await HolonManager.Instance.SearchHolonsAsync("sample");

if (!searchResult.IsError && searchResult.Result != null)
{
    var holons = searchResult.Result;
    Console.WriteLine($"Found {holons.Count} holons");
}
```

### 5. NFTManager

The `NFTManager` handles all NFT-related operations across multiple blockchain providers.

#### Key Features:
- **NFT Management**: Create, transfer, and manage NFTs
- **Multi-Provider Support**: Works with all blockchain providers
- **Metadata Management**: Handle NFT metadata and properties
- **Ownership Tracking**: Track NFT ownership and transfers

#### Main Methods:

```csharp
// NFT Management
public async Task<OASISResult<INFT>> CreateNFTAsync(INFT nft)
public async Task<OASISResult<INFT>> TransferNFTAsync(Guid nftId, Guid fromAvatarId, Guid toAvatarId)
public async Task<OASISResult<List<INFT>>> GetNFTsForAvatarAsync(Guid avatarId)

// NFT Operations
public async Task<OASISResult<bool>> MintNFTAsync(INFT nft, Guid avatarId)
public async Task<OASISResult<bool>> BurnNFTAsync(Guid nftId)
```

#### Usage Example:

```csharp
// Create an NFT
var nft = new NFT
{
    Name = "My NFT",
    Description = "A sample NFT",
    ImageUrl = "https://example.com/image.png",
    OwnerId = avatarId
};

var createResult = await NFTManager.Instance.CreateNFTAsync(nft);

if (!createResult.IsError && createResult.Result != null)
{
    Console.WriteLine($"NFT created with ID: {createResult.Result.Id}");
}

// Get NFTs for avatar
var nftsResult = await NFTManager.Instance.GetNFTsForAvatarAsync(avatarId);

if (!nftsResult.IsError && nftsResult.Result != null)
{
    var nfts = nftsResult.Result;
    Console.WriteLine($"Avatar has {nfts.Count} NFTs");
}
```

### 6. SearchManager

The `SearchManager` provides comprehensive search capabilities across the OASIS system.

#### Key Features:
- **Global Search**: Search across all OASIS data
- **Provider-Specific Search**: Search within specific providers
- **Advanced Filtering**: Complex search criteria and filters
- **Performance Optimization**: Efficient search algorithms

#### Main Methods:

```csharp
// Global Search
public async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams)
public async Task<OASISResult<ISearchResults>> SearchAvatarsAsync(string searchTerm)
public async Task<OASISResult<ISearchResults>> SearchHolonsAsync(string searchTerm)

// Advanced Search
public async Task<OASISResult<ISearchResults>> SearchWithFiltersAsync(ISearchParams searchParams, List<ISearchFilter> filters)
public async Task<OASISResult<ISearchResults>> SearchByProviderAsync(ISearchParams searchParams, ProviderType providerType)
```

#### Usage Example:

```csharp
// Search avatars
var searchParams = new SearchParams
{
    SearchTerm = "john",
    SearchType = SearchType.Avatar
};

var searchResult = await SearchManager.Instance.SearchAsync(searchParams);

if (!searchResult.IsError && searchResult.Result != null)
{
    var results = searchResult.Result;
    Console.WriteLine($"Found {results.SearchResultAvatars.Count} avatars");
}

// Advanced search with filters
var filters = new List<ISearchFilter>
{
    new SearchFilter { Field = "Email", Value = "@example.com", Operator = SearchOperator.Contains }
};

var advancedSearchResult = await SearchManager.Instance.SearchWithFiltersAsync(searchParams, filters);
```

### 7. CacheManager

The `CacheManager` provides caching capabilities for improved performance.

#### Key Features:
- **Memory Caching**: In-memory caching for frequently accessed data
- **Distributed Caching**: Support for distributed cache systems
- **Cache Invalidation**: Automatic cache invalidation
- **Performance Optimization**: Reduces database and API calls

#### Main Methods:

```csharp
// Cache Operations
public async Task<OASISResult<T>> GetAsync<T>(string key)
public async Task<OASISResult<bool>> SetAsync<T>(string key, T value, TimeSpan? expiration = null)
public async Task<OASISResult<bool>> RemoveAsync(string key)
public async Task<OASISResult<bool>> ClearAsync()
```

#### Usage Example:

```csharp
// Cache avatar data
var avatar = await AvatarManager.Instance.LoadAvatarAsync(avatarId);

if (!avatar.IsError && avatar.Result != null)
{
    // Cache the avatar for 1 hour
    await CacheManager.Instance.SetAsync($"avatar_{avatarId}", avatar.Result, TimeSpan.FromHours(1));
}

// Retrieve from cache
var cachedAvatar = await CacheManager.Instance.GetAsync<IAvatar>($"avatar_{avatarId}");

if (!cachedAvatar.IsError && cachedAvatar.Result != null)
{
    Console.WriteLine($"Retrieved avatar from cache: {cachedAvatar.Result.Username}");
}
```

### 8. EmailManager

The `EmailManager` handles email operations and notifications.

#### Key Features:
- **Email Sending**: Send emails to users
- **Template Support**: Email templates and formatting
- **Notification System**: Automated notifications
- **Email Validation**: Email address validation

#### Main Methods:

```csharp
// Email Operations
public async Task<OASISResult<bool>> SendEmailAsync(string to, string subject, string body)
public async Task<OASISResult<bool>> SendTemplateEmailAsync(string to, string templateName, object data)
public async Task<OASISResult<bool>> ValidateEmailAsync(string email)
```

#### Usage Example:

```csharp
// Send welcome email
var emailResult = await EmailManager.Instance.SendEmailAsync(
    "user@example.com",
    "Welcome to OASIS",
    "Welcome to the OASIS platform!"
);

if (!emailResult.IsError && emailResult.Result)
{
    Console.WriteLine("Welcome email sent successfully");
}

// Send template email
var templateData = new { Username = "john_doe", ActivationLink = "https://example.com/activate" };
var templateResult = await EmailManager.Instance.SendTemplateEmailAsync(
    "user@example.com",
    "welcome_template",
    templateData
);
```

## Manager Integration Patterns

### 🔄 **Data Flow Patterns**

#### **Avatar → Wallet → Key Pattern**
```csharp
// 1. Load avatar
var avatarResult = await AvatarManager.Instance.LoadAvatarAsync(username);

// 2. Get wallet for avatar
var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatarResult.Result.Id, ProviderType.EthereumOASIS);

// 3. Get keys for wallet
var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(avatarResult.Result.Id, ProviderType.EthereumOASIS);
```

#### **Holon → Search → Cache Pattern**
```csharp
// 1. Search holons
var searchResult = await SearchManager.Instance.SearchHolonsAsync("sample");

// 2. Cache results
if (!searchResult.IsError && searchResult.Result != null)
{
    await CacheManager.Instance.SetAsync("search_results", searchResult.Result, TimeSpan.FromMinutes(30));
}

// 3. Load specific holon
var holonResult = await HolonManager.Instance.LoadHolonAsync(holonId);
```

### 🎯 **Best Practices by Use Case**

#### **User Registration Flow**
```csharp
// 1. Create avatar
var avatarResult = await AvatarManager.Instance.SaveAvatarAsync(avatar);

// 2. Create wallet
var walletResult = await WalletManager.Instance.CreateWalletAsync(avatarResult.Result.Id, ProviderType.EthereumOASIS);

// 3. Send welcome email
var emailResult = await EmailManager.Instance.SendEmailAsync(avatar.Email, "Welcome", "Welcome to OASIS!");
```

#### **NFT Creation Flow**
```csharp
// 1. Create NFT
var nftResult = await NFTManager.Instance.CreateNFTAsync(nft);

// 2. Cache NFT data
await CacheManager.Instance.SetAsync($"nft_{nftResult.Result.Id}", nftResult.Result);

// 3. Search for similar NFTs
var searchResult = await SearchManager.Instance.SearchHolonsAsync(nft.Name);
```

## Performance Optimization

### 🚀 **Caching Strategy**
- **Frequently accessed data**: Cache for longer periods
- **User-specific data**: Cache with user ID as key
- **Search results**: Cache with search parameters as key

### 🚀 **Search Optimization**
- **Use specific search terms**: More targeted searches
- **Filter results**: Use filters to narrow down results
- **Cache search results**: Avoid repeated searches

### 🚀 **Database Optimization**
- **Batch operations**: Group multiple operations
- **Provider selection**: Use most efficient provider
- **Connection pooling**: Reuse database connections

## Error Handling Patterns

### 🔧 **Consistent Error Handling**
```csharp
public async Task<OASISResult<T>> PerformOperationAsync<T>()
{
    var result = new OASISResult<T>();
    
    try
    {
        // Perform operation
        var operationResult = await SomeOperationAsync();
        
        if (!operationResult.IsError)
        {
            result.Result = operationResult.Result;
            result.IsError = false;
            result.Message = "Operation completed successfully";
        }
        else
        {
            OASISErrorHandling.HandleError(ref result, $"Operation failed: {operationResult.Message}");
        }
    }
    catch (Exception ex)
    {
        result.Exception = ex;
        OASISErrorHandling.HandleError(ref result, $"Error in PerformOperationAsync: {ex.Message}");
    }
    
    return result;
}
```

## Related Documentation

- [OASIS Managers Complete Guide](OASIS-Managers-Complete-Guide.md)
- [Wallet Management System](Wallet-Management-System.md)
- [Provider Management](Provider-Management.md)
- [Transaction Management](Transaction-Management.md)
- [Security Best Practices](Security-Best-Practices.md)

---

*Last updated: December 2024*
*Version: 1.0*
