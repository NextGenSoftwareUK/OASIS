# Gateway API Implementation Plan for Read-Only Component Method Calls

## Summary

Based on research into the Radix Gateway API documentation, we have identified the `/state/entity/details` endpoint as the primary method for querying component state. This document outlines the implementation plan for using this endpoint to support read-only component method calls.

## Confirmed Endpoint: `/state/entity/details`

### Endpoint Details
- **Full URL**: `{gateway_url}/state/entity/details`
- **Method**: POST
- **Base URLs**:
  - Mainnet: `https://mainnet.radixdlt.com`
  - Stokenet: `https://stokenet.radixdlt.com`

### Request Format
```json
{
  "addresses": ["component_address_here"],
  "at_ledger_state": {
    "state_version": 1000
  }
}
```

**Parameters**:
- `addresses` (required): Array of entity addresses to query
- `at_ledger_state` (optional): Specify historical state version, omit for current state

### Response Structure
- Response format varies by entity type
- For components, should include state information including KeyValueStore entries
- Need to parse component state to extract KeyValueStore data

## Implementation Approach

### Phase 1: Basic State Query Implementation

1. **Create DTOs for Gateway API State Endpoints**
   - `StateEntityDetailsRequest.cs`
   - `StateEntityDetailsResponse.cs`
   - Component state specific DTOs

2. **Implement State Query Method**
   - Add method to `RadixComponentService` or `RadixService`
   - Query component state via `/state/entity/details`
   - Parse response to extract component state

3. **KeyValueStore Parsing**
   - Parse component state to find KeyValueStore entries
   - Extract entries by key (entity IDs)
   - Deserialize JSON strings stored in KeyValueStore

### Phase 2: Integration with Read-Only Methods

1. **Update `CallComponentMethodAsync`**
   - Replace transaction stakes approach with state query
   - Query component state directly
   - Extract method return values from KeyValueStore

2. **Method Mapping**
   - `get_avatar(entityId)` → Query KeyValueStore with entity ID key
   - `get_holon(entityId)` → Query KeyValueStore with entity ID key
   - `get_avatar_by_email(email)` → Query email index KeyValueStore
   - `get_avatar_by_username(username)` → Query username index KeyValueStore

### Phase 3: Error Handling & Optimization

1. **Error Handling**
   - Handle missing component addresses
   - Handle missing KeyValueStore entries
   - Handle JSON deserialization errors

2. **Caching**
   - Cache component state queries (with appropriate TTL)
   - Cache deserialized objects

3. **Fallback Strategy**
   - If state query fails, fallback to transaction stakes approach
   - Log warnings for fallback usage

## Implementation Steps

### Step 1: Create DTOs

```csharp
// Infrastructure/Entities/DTOs/State/StateEntityDetailsRequest.cs
public class StateEntityDetailsRequest
{
    [JsonPropertyName("addresses")]
    public List<string> Addresses { get; set; } = new();
    
    [JsonPropertyName("at_ledger_state")]
    public LedgerState? AtLedgerState { get; set; }
}

public class LedgerState
{
    [JsonPropertyName("state_version")]
    public ulong StateVersion { get; set; }
}

// Infrastructure/Entities/DTOs/State/StateEntityDetailsResponse.cs
public class StateEntityDetailsResponse
{
    // TODO: Define response structure based on Gateway API documentation
    // This will include component state with KeyValueStore entries
}
```

### Step 2: Implement State Query Method

```csharp
// In RadixComponentService or RadixService
public async Task<OASISResult<ComponentState>> GetComponentStateAsync(
    string componentAddress,
    CancellationToken token = default)
{
    var request = new StateEntityDetailsRequest
    {
        Addresses = new List<string> { componentAddress }
    };
    
    var response = await HttpClientHelper.PostAsync<StateEntityDetailsRequest, StateEntityDetailsResponse>(
        _httpClient,
        $"{_config.HostUri}/state/entity/details",
        request,
        token
    );
    
    // Parse component state from response
    // Extract KeyValueStore entries
    // Return parsed component state
}
```

### Step 3: Update CallComponentMethodAsync

```csharp
public async Task<OASISResult<string>> CallComponentMethodAsync(
    string componentAddress,
    string methodName,
    List<object> args,
    CancellationToken token = default)
{
    // Get component state
    var stateResult = await GetComponentStateAsync(componentAddress, token);
    if (stateResult.IsError)
        return OASISErrorHandling.HandleError<string>(...);
    
    // Map method name to KeyValueStore key
    string key = MapMethodToKey(methodName, args);
    
    // Extract value from KeyValueStore
    string jsonValue = ExtractFromKeyValueStore(stateResult.Result, key);
    
    // Return JSON string
    return new OASISResult<string> { Result = jsonValue, IsError = false };
}
```

## Key Considerations

### KeyValueStore Structure
- Need to understand how Scrypto stores KeyValueStore data in component state
- Keys are likely stored as SBOR-encoded values
- Values are stored as JSON strings (in our case)

### Key Mapping
- Entity IDs: Convert `ulong` entity ID to KeyValueStore key format
- Index keys: Email/username strings → KeyValueStore keys
- Provider keys: String provider keys → KeyValueStore keys

### State Versioning
- Can query historical state using `at_ledger_state` parameter
- Useful for versioning support (not currently required, but good to know)

## Testing Plan

1. **Unit Tests**
   - Test state query request/response parsing
   - Test KeyValueStore extraction logic
   - Test method-to-key mapping

2. **Integration Tests**
   - Test with deployed component on Stokenet
   - Test all read-only methods (get_avatar, get_holon, etc.)
   - Test error scenarios (missing component, missing keys)

3. **Performance Tests**
   - Measure state query latency
   - Test caching effectiveness
   - Compare with transaction stakes approach

## Next Steps

1. **Review Gateway API Response Schema**
   - Examine actual response structure for component entities
   - Understand KeyValueStore representation in response
   - Document response schema in DTOs

2. **Implement Phase 1**
   - Create DTOs based on actual API response
   - Implement basic state query method
   - Test with real component on Stokenet

3. **Implement Phase 2**
   - Update `CallComponentMethodAsync` to use state queries
   - Implement KeyValueStore parsing
   - Test all read-only methods

4. **Implement Phase 3**
   - Add error handling and caching
   - Optimize performance
   - Document usage patterns

## References

- **Gateway API Documentation**: https://radix-babylon-gateway-api.redoc.ly/
- **State Endpoints Section**: https://radix-babylon-gateway-api.redoc.ly/#tag/State
- **Network APIs Overview**: https://docs.radixdlt.com/docs/network-apis

