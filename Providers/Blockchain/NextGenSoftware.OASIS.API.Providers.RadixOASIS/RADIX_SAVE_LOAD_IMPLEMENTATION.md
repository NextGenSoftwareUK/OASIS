# RadixOASIS SaveAvatar/LoadAvatar Implementation

## Status: ✅ Implemented (Structure Complete)

### What's Been Implemented

1. **SaveAvatarAsync** - Complete implementation
   - Serializes avatar to JSON
   - Calculates entity ID from GUID
   - Calls component `create_avatar` method
   - Falls back to `update_avatar` if avatar already exists
   - Returns transaction hash on success

2. **LoadAvatarAsync** - Structure complete, needs Gateway API integration
   - Calculates entity ID from GUID
   - Calls component `get_avatar` method (read-only)
   - Deserializes JSON response to IAvatar
   - Note: Read-only calls need Gateway API state query implementation

3. **Component Service Integration**
   - RadixComponentService created for component interactions
   - RadixComponentHelper for argument type conversion
   - Proper manifest building for component method calls

### Current Limitations

1. **Read-Only Component Calls**
   - `CallComponentMethodAsync` (read-only) needs Gateway API state query integration
   - Currently returns "not yet fully implemented" error
   - Requires Gateway API `/state/entity/details` endpoint integration
   - Need to parse component state to extract method return values

2. **Component Deployment**
   - Component must be deployed to Radix network before use
   - Component address must be configured in `OASIS_DNA.json`
   - See `contracts/README.md` for deployment instructions

### How It Works

#### SaveAvatar Flow

```
1. Avatar object → JSON serialization
2. Avatar.Id (Guid) → EntityId (ulong) via HashUtility.GetNumericHash
3. Build transaction manifest:
   - Lock fee from sender account
   - Call component.create_avatar(entityId, avatarJson, username, email)
   - Deposit fee back
4. Sign and submit transaction
5. Return transaction hash
```

#### LoadAvatar Flow (When Gateway API Integration Complete)

```
1. Avatar.Id (Guid) → EntityId (ulong) via HashUtility.GetNumericHash
2. Query Gateway API state for component
3. Call component.get_avatar(entityId) via state query
4. Parse JSON response from component state
5. Deserialize JSON → IAvatar object
6. Return avatar
```

### Next Steps to Complete

1. **Gateway API State Query Implementation**
   - Research Gateway API `/state/entity/details` endpoint
   - Implement state parsing for component method returns
   - Handle JSON string extraction from component state

2. **Testing**
   - Deploy component to Stokenet
   - Test SaveAvatar with real transaction
   - Test LoadAvatar once Gateway API integration is complete
   - Verify JSON serialization/deserialization works correctly

3. **Error Handling**
   - Handle component method errors properly
   - Parse Scrypto panic messages
   - Provide user-friendly error messages

4. **Transaction Confirmation**
   - Wait for transaction confirmation before returning success
   - Check transaction status after submission
   - Handle failed transactions appropriately

### Example Usage

```csharp
// Initialize provider with component address
var config = new RadixOASISConfig
{
    HostUri = "https://stokenet.radixdlt.com",
    NetworkId = 2,
    AccountAddress = "account_rdx...",
    PrivateKey = "private_key_hex",
    ComponentAddress = "component_rdx1..." // Deployed component address
};

var provider = new RadixOASIS(config);
await provider.ActivateProviderAsync();

// Save avatar
var avatar = new Avatar
{
    Id = Guid.NewGuid(),
    Username = "testuser",
    Email = "test@example.com",
    FirstName = "Test",
    LastName = "User"
};

var saveResult = await provider.SaveAvatarAsync(avatar);
if (!saveResult.IsError)
{
    Console.WriteLine($"Avatar saved! Transaction: {saveResult.Result}");
}

// Load avatar
var loadResult = await provider.LoadAvatarAsync(avatar.Id);
if (!loadResult.IsError && loadResult.Result != null)
{
    Console.WriteLine($"Loaded avatar: {loadResult.Result.Username}");
}
```

### Files Modified/Created

- `RadixOASIS.cs` - Implemented SaveAvatarAsync/LoadAvatarAsync
- `RadixComponentService.cs` - Component interaction service
- `RadixComponentHelper.cs` - Argument type conversion helper
- `RadixOASISConfig.cs` - Added ComponentAddress property
- `OASISDNA.cs` - Added ComponentAddress to settings
- `OASIS_DNA.json` - Added ComponentAddress field

