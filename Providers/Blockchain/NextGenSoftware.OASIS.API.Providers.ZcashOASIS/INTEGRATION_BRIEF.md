# Zcash OASIS Provider - Full Integration Brief

**Last Updated:** December 2025  
**Status:** Partial Implementation - Bridge/Privacy Features Only  
**Priority:** High - Complete OASIS Interface Implementation Needed

---

## Executive Summary

The ZcashOASIS provider currently implements **Zcash-specific privacy features** (shielded transactions, viewing keys, bridge operations) but is **missing 34+ standard OASIS interface methods**. This brief outlines the requirements and approach for full OASIS integration.

**Current State:** ~20% integrated (Zcash-specific features only)  
**Target State:** 100% integrated (full OASIS interface compliance)

---

## Current Implementation Status

### ✅ **Implemented Features**

**Zcash-Specific Operations:**
- `CreateShieldedTransactionAsync()` - Shielded transaction creation
- `GenerateViewingKeyAsync()` - Viewing key generation for auditability
- `CreatePartialNoteAsync()` - Partial note creation for enhanced privacy
- `LockZECForBridgeAsync()` - Lock ZEC for cross-chain bridges
- `ReleaseZECAsync()` - Release locked ZEC
- Basic `LoadHolonAsync()` / `SaveHolonAsync()` - Simplified holon storage
- RPC client integration - Connection to Zcash node

**Infrastructure:**
- Provider activation/deactivation
- ZcashRepository (basic structure)
- ZcashService (shielded transaction logic)
- ZcashBridgeService (bridge operations)

### ❌ **Missing Features (34+ Methods)**

**Avatar Operations (15+ methods):**
- `LoadAvatarByUsernameAsync()` - Load avatar by username
- `LoadAvatarByEmailAsync()` - Load avatar by email
- `LoadAvatarDetailAsync()` - Load detailed avatar info
- `SaveAvatarAsync()` - Save avatar data
- `SaveAvatarDetailAsync()` - Save detailed avatar info
- `DeleteAvatarAsync()` - Delete avatar (multiple overloads)
- `LoadAllAvatarDetailsAsync()` - Load all avatar details
- All provider-specific avatar operations

**Holon Operations (12+ methods):**
- `LoadHolonsForParentAsync()` - Load child holons
- `LoadHolonsByMetaDataAsync()` - Search holons by metadata
- `LoadAllHolonsAsync()` - Load all holons
- `DeleteHolonAsync()` - Delete holons
- Advanced querying and filtering

**Search & Discovery (2+ methods):**
- `SearchAsync()` - Universal search across holons/avatars

**Export/Import (4+ methods):**
- `ExportAllDataForAvatarByIdAsync()` - Export avatar data
- `ExportAllDataForAvatarByUsernameAsync()` - Export by username
- `ExportAllDataForAvatarByEmailAsync()` - Export by email
- `ImportAsync()` - Import holons

**Wallet Operations (3+ methods):**
- `GetBalanceAsync()` - Get wallet balance (returns "not yet fully implemented")
- `GetTransactionsAsync()` - Get transaction history (returns "not yet fully implemented")
- `GenerateKeyPairAsync()` - Generate wallet keypair (returns error)

**NFT Operations:**
- All NFT interface methods (if `IOASISNFTProvider` is implemented)

---

## Integration Requirements

### Phase 1: Core Storage Operations (Priority: High)

**Objective:** Enable basic data storage and retrieval on Zcash blockchain

**Tasks:**
1. **Holon Storage Implementation**
   - Store holons in Zcash shielded transactions (memo field)
   - Map holon GUID → Zcash transaction ID
   - Store holon metadata in shielded transaction memos
   - Implement parent-child relationships via transaction references
   - Handle large holons (split across multiple transactions if needed)

2. **Holon Retrieval Implementation**
   - Load holon by GUID (lookup transaction ID)
   - Load holon by provider key (transaction ID directly)
   - Implement `LoadHolonsForParentAsync()` - query transactions by parent reference
   - Implement `LoadHolonsByMetaDataAsync()` - search transaction memos
   - Implement `LoadAllHolonsAsync()` - scan all holon transactions

3. **Holon Deletion**
   - Mark holons as deleted (store deletion flag in new transaction)
   - Maintain transaction history for audit

**Technical Approach:**
```csharp
// Store holon in shielded transaction memo
var holonJson = JsonSerializer.Serialize(holon);
var memo = Convert.ToBase64String(Encoding.UTF8.GetBytes(holonJson));

// Create shielded transaction with holon data in memo
var tx = await CreateShieldedTransactionAsync(
    fromAddress: holonStorageAddress,
    toAddress: holonStorageAddress, // Self-transfer
    amount: 0m, // Minimal amount
    memo: memo
);

// Store transaction ID as provider key
holon.ProviderUniqueStorageKey[ProviderType.ZcashOASIS] = tx.TransactionId;
```

**Considerations:**
- Zcash transaction memos have size limits (~512 bytes)
- Large holons may need compression or splitting
- Transaction costs for storage operations
- Privacy vs. accessibility trade-offs

---

### Phase 2: Avatar Operations (Priority: High)

**Objective:** Enable avatar storage and management on Zcash

**Tasks:**
1. **Avatar Storage**
   - Convert avatar to holon format
   - Store avatar in shielded transaction
   - Maintain avatar → transaction ID mapping
   - Store avatar username/email indexes for lookup

2. **Avatar Retrieval**
   - `LoadAvatarAsync(Guid id)` - Load by GUID
   - `LoadAvatarByUsernameAsync()` - Index lookup by username
   - `LoadAvatarByEmailAsync()` - Index lookup by email
   - `LoadAvatarByProviderKeyAsync()` - Load by Zcash address

3. **Avatar Management**
   - `SaveAvatarAsync()` - Create/update avatar
   - `SaveAvatarDetailAsync()` - Save detailed avatar info
   - `DeleteAvatarAsync()` - Soft delete with deletion marker

**Indexing Strategy:**
- Maintain index holons for username/email → avatar GUID mapping
- Store indexes in separate shielded transactions
- Use consistent naming pattern for index holons

**Technical Approach:**
```csharp
// Create index holon for username lookup
var indexHolon = new Holon
{
    Name = $"avatar_index_username_{username}",
    HolonType = HolonType.Index,
    MetaData = new Dictionary<string, object>
    {
        { "index_type", "username" },
        { "username", username },
        { "avatar_id", avatar.Id.ToString() }
    }
};
await SaveHolonAsync(indexHolon);

// Retrieve avatar by username
var indexHolon = await LoadHolonByMetaDataAsync("index_type", "username");
var avatarId = Guid.Parse(indexHolon.MetaData["avatar_id"].ToString());
var avatar = await LoadAvatarAsync(avatarId);
```

---

### Phase 3: Search & Query (Priority: Medium)

**Objective:** Enable search across Zcash-stored data

**Tasks:**
1. **Search Implementation**
   - Implement `SearchAsync()` method
   - Search transaction memos by keyword
   - Filter by holon type, metadata, date ranges
   - Return paginated results

2. **Metadata Queries**
   - `LoadHolonsByMetaDataAsync()` - Single key-value search
   - `LoadHolonsByMetaDataAsync(Dictionary)` - Multi-key-value search
   - Support exact match, contains, starts-with, etc.

**Technical Approach:**
- Use Zcash RPC `z_listreceivedbyaddress` and `z_listunspent` to scan transactions
- Parse transaction memos to extract holon data
- Filter in-memory or use external indexing service
- Consider using IPFS for large-scale indexing (hybrid approach)

**Performance Considerations:**
- Full blockchain scan is slow - consider caching/indexing layer
- May need to maintain separate index database
- Consider using MongoDB/IPFS for search indexes while storing actual data on Zcash

---

### Phase 4: Wallet Integration (Priority: Medium)

**Objective:** Enable wallet operations for Zcash

**Tasks:**
1. **Balance Queries**
   - Implement `GetBalanceAsync()` - Get ZEC balance for address
   - Support both transparent and shielded addresses
   - Aggregate balance across multiple addresses

2. **Transaction History**
   - Implement `GetTransactionsAsync()` - Get transaction history
   - Filter by date, amount, transaction type
   - Return both transparent and shielded transactions
   - Include transaction metadata

3. **Key Pair Generation**
   - Implement `GenerateKeyPairAsync()` - Generate Zcash keypair
   - Support both transparent (t-address) and shielded (z-address) key generation
   - Return public key, private key, and mnemonic phrase

**Zcash-Specific Considerations:**
- Transparent addresses (t-addresses) start with `t`
- Shielded addresses (z-addresses) start with `zs` or `z`
- Need to handle both address types
- Shielded balance requires viewing key or wallet scanning

**Technical Approach:**
```csharp
// Get transparent balance
var balanceResult = await _rpcClient.GetBalanceAsync(transparentAddress);

// Get shielded balance (requires wallet or viewing key)
var shieldedBalance = await _rpcClient.GetZBalanceAsync(zAddress);

// Generate keypair
var keypair = await _rpcClient.GenerateZAddressAsync(); // or GenerateTAddressAsync()
```

---

### Phase 5: Export/Import (Priority: Low)

**Objective:** Enable data export and import operations

**Tasks:**
1. **Export Operations**
   - `ExportAllDataForAvatarByIdAsync()` - Export all holons for avatar
   - `ExportAllDataForAvatarByUsernameAsync()` - Export by username
   - `ExportAllDataForAvatarByEmailAsync()` - Export by email
   - Return JSON or other serialized format

2. **Import Operations**
   - `ImportAsync()` - Import holons from external source
   - Validate holon structure
   - Store imported holons on Zcash
   - Preserve GUIDs or generate new ones

**Technical Approach:**
- Query all holons for avatar (by creator avatar ID metadata)
- Serialize holons to JSON
- For import: deserialize, validate, save to Zcash
- Handle GUID preservation vs. regeneration

---

## Technical Architecture

### Data Storage Strategy

**Option 1: Shielded Transactions (Current Approach)**
- Store holon data in transaction memo fields
- Privacy-preserving but limited memo size (~512 bytes)
- Requires scanning blockchain for queries
- Best for: Small holons, privacy-critical data

**Option 2: Hybrid Approach (Recommended)**
- Store holon metadata in Zcash memos (references)
- Store actual holon data in IPFS/MongoDB
- Use Zcash transaction ID as IPFS CID reference
- Best for: Large holons, searchable data, privacy + accessibility

**Option 3: Transparent Transactions**
- Store holon data in transparent transaction outputs
- Publicly readable, faster queries
- Less private but more efficient
- Best for: Public data, high-performance queries

**Recommendation:** Use **Option 2 (Hybrid)** for most holons, Option 1 for highly sensitive data.

### Indexing Strategy

**Challenge:** Zcash shielded transactions are private - cannot query by content efficiently

**Solutions:**
1. **External Index Database**
   - Maintain MongoDB/Neo4j index of holon metadata
   - Zcash stores actual data, index enables fast queries
   - Sync index when holons are created/updated

2. **Index Holons**
   - Store index holons on Zcash itself
   - Search indexes using viewing keys
   - Fully decentralized but slower

3. **IPFS + Zcash**
   - Store data in IPFS (searchable)
   - Store IPFS CID hash in Zcash memo
   - Zcash provides immutability proof, IPFS provides searchability

**Recommendation:** Use **Option 1 (External Index)** for production performance, with option to use Option 2 for fully decentralized scenarios.

---

## Implementation Priority

### **Phase 1: Critical Path (Weeks 1-2)**
1. Complete holon save/load operations
2. Implement holon deletion
3. Implement basic avatar storage/retrieval
4. Fix wallet balance/transaction methods

### **Phase 2: Core Features (Weeks 3-4)**
5. Implement avatar lookup by username/email
6. Implement holon parent-child relationships
7. Implement basic search functionality
8. Complete key pair generation

### **Phase 3: Advanced Features (Weeks 5-6)**
9. Implement metadata-based queries
10. Implement export operations
11. Implement import operations
12. Performance optimization and caching

---

## Key Technical Decisions

### 1. **Privacy vs. Accessibility Trade-off**

**Question:** Should holons be fully private (shielded) or publicly readable (transparent)?

**Decision:** Support both modes
- **Private mode:** Store in shielded transactions (default for sensitive data)
- **Public mode:** Store in transparent transactions (for public/shared data)
- Add `HolonPrivacyMode` enum to holon metadata

### 2. **Data Size Limitations**

**Problem:** Zcash memos limited to ~512 bytes

**Solution:** 
- Small holons: Store directly in memo
- Large holons: Store in IPFS, reference CID in memo
- Add compression for medium-sized holons

### 3. **Query Performance**

**Problem:** Scanning blockchain is slow

**Solution:**
- Maintain external index (MongoDB/Neo4j) for fast queries
- Use Zcash for authoritative storage
- Sync index on holon create/update/delete

### 4. **Address Management**

**Decision:** Support both transparent (t-address) and shielded (z-address)
- Use z-addresses for privacy-critical operations
- Use t-addresses for public operations or when performance matters
- Let users choose via configuration

---

## Testing Strategy

### Unit Tests
- Test holon save/load operations
- Test avatar operations
- Test shielded transaction creation
- Test viewing key generation

### Integration Tests
- Test against Zcash testnet
- Test end-to-end avatar creation → storage → retrieval
- Test holon parent-child relationships
- Test search functionality

### Performance Tests
- Benchmark holon save/load times
- Test with various holon sizes
- Test search performance with large datasets
- Test wallet operations performance

---

## Dependencies & Prerequisites

### Required
- ✅ Zcash RPC client (already implemented)
- ✅ Zcash testnet/mainnet node access
- ⚠️ IPFS provider integration (for hybrid storage - may need to add)
- ⚠️ Index database (MongoDB/Neo4j) - for query performance

### Optional but Recommended
- Caching layer (Redis) for frequently accessed holons
- Monitoring/logging for transaction tracking
- Backup/restore mechanisms

---

## Estimated Effort

**Total Estimated Time:** 6-8 weeks for full implementation

- Phase 1 (Critical): 2 weeks
- Phase 2 (Core): 2 weeks  
- Phase 3 (Advanced): 2 weeks
- Testing & Optimization: 1-2 weeks

**Resources Needed:**
- 1 Senior Developer (blockchain/Zcash expertise)
- 1 Developer (OASIS framework knowledge)
- Zcash testnet/mainnet node access
- Testing infrastructure

---

## Success Criteria

✅ **Phase 1 Complete When:**
- All holon CRUD operations work end-to-end
- Avatar storage and basic retrieval works
- Wallet balance/transactions return real data

✅ **Phase 2 Complete When:**
- Avatar lookup by username/email works
- Holon parent-child relationships work
- Basic search returns results

✅ **Phase 3 Complete When:**
- All OASIS interface methods implemented (no "not yet implemented" errors)
- Export/import operations work
- Performance meets acceptable thresholds (< 5s for most operations)

✅ **Full Integration Complete When:**
- 100% OASIS interface compliance
- All methods tested and documented
- Performance optimized
- Production-ready code

---

## Next Steps

1. **Review & Approve Brief** - Stakeholder review of approach
2. **Set Up Development Environment** - Zcash testnet node, testing infrastructure
3. **Spike: Data Storage POC** - Proof of concept for holon storage strategy
4. **Begin Phase 1 Implementation** - Start with critical path items
5. **Regular Progress Reviews** - Weekly check-ins on implementation progress

---

## References

- [Zcash RPC Documentation](https://zcash.readthedocs.io/en/latest/rpc/)
- [OASIS Provider Interface](../OASIS_Provider_Development_Guide.md)
- [ZcashOASIS Current Implementation](./ZcashOASIS.cs)
- [Provider Status Reference](../../../docs/reference/PROVIDERS/STATUS.md)

---

**Document Owner:** Development Team  
**Last Review:** December 2025  
**Next Review:** After Phase 1 completion



