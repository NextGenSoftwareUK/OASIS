# Miden ↔ Zcash Bridge Testing Guide

## Prerequisites

1. **Miden Testnet Access**
   - API URL: `https://testnet.miden.xyz` (or configured endpoint)
   - API Key (if required)
   - Testnet account with funds

2. **Zcash Testnet Access**
   - RPC URL configured
   - Testnet node running
   - Testnet account with funds

3. **Environment Variables**
   ```bash
   export MIDEN_API_URL="https://testnet.miden.xyz"
   export MIDEN_API_KEY="your_api_key"
   export MIDEN_BRIDGE_POOL_ADDRESS="miden_bridge_pool"
   export ZCASH_RPC_URL="http://localhost:8232"
   export ZCASH_BRIDGE_POOL_ADDRESS="zt1bridgepool"
   ```

## Test Cases

### Test 1: Zcash → Miden Bridge (Basic)

**Objective**: Test basic bridge operation from Zcash to Miden

**Steps**:
1. Create bridge order request
2. Lock ZEC on Zcash
3. Generate STARK proof
4. Mint private note on Miden
5. Verify completion

**Expected Result**: 
- ZEC locked on Zcash
- Private note created on Miden
- Bridge order completed

**Test Code**:
```csharp
var request = new CreateBridgeOrderRequest
{
    FromToken = "ZEC",
    ToToken = "MIDEN",
    Amount = 0.1m,
    FromAddress = "zs1test...",
    DestinationAddress = "miden1test...",
    UserId = Guid.NewGuid(),
    EnableViewingKeyAudit = true,
    RequireProofVerification = true
};

var result = await bridgeManager.CreateBridgeOrderAsync(request);
Assert.IsFalse(result.IsError);
Assert.IsNotNull(result.Result);
```

### Test 2: Miden → Zcash Bridge (Basic)

**Objective**: Test reverse bridge operation from Miden to Zcash

**Steps**:
1. Create bridge order request
2. Lock on Miden (private note)
3. Generate STARK proof
4. Mint on Zcash (shielded)
5. Verify completion

**Expected Result**:
- Private note locked on Miden
- Shielded transaction on Zcash
- Bridge order completed

**Test Code**:
```csharp
var request = new CreateBridgeOrderRequest
{
    FromToken = "MIDEN",
    ToToken = "ZEC",
    Amount = 0.1m,
    FromAddress = "miden1test...",
    DestinationAddress = "zs1test...",
    UserId = Guid.NewGuid(),
    RequireProofVerification = true
};

var result = await bridgeManager.CreateBridgeOrderAsync(request);
Assert.IsFalse(result.IsError);
```

### Test 3: STARK Proof Generation

**Objective**: Test STARK proof generation and verification

**Steps**:
1. Generate STARK proof for bridge operation
2. Verify proof validity
3. Test with invalid proof

**Expected Result**:
- Valid proof verifies successfully
- Invalid proof is rejected

**Test Code**:
```csharp
var midenService = new MidenService(apiClient);
var proof = await midenService.GenerateSTARKProofAsync(
    programHash: "bridge_program_hash",
    inputs: new { amount = 0.1m, source = "Zcash" },
    outputs: new { noteId = "note123" }
);

var verified = await midenService.VerifySTARKProofAsync(proof);
Assert.IsTrue(verified);
```

### Test 4: Viewing Key Audit

**Objective**: Test viewing key generation and audit logging

**Steps**:
1. Create bridge with viewing key enabled
2. Verify viewing key is generated
3. Check audit log entry

**Expected Result**:
- Viewing key generated
- Audit entry created
- Viewing key can verify transaction

**Test Code**:
```csharp
var request = new CreateBridgeOrderRequest
{
    FromToken = "ZEC",
    ToToken = "MIDEN",
    Amount = 0.1m,
    EnableViewingKeyAudit = true,
    ViewingKey = "generated_viewing_key"
};

var result = await bridgeManager.CreateBridgeOrderAsync(request);
// Verify viewing key audit entry exists
```

### Test 5: Error Handling - Insufficient Balance

**Objective**: Test error handling when balance is insufficient

**Steps**:
1. Attempt bridge with amount > balance
2. Verify error is returned
3. Verify no transactions are created

**Expected Result**:
- Error returned
- No transactions on either chain
- Order marked as failed

### Test 6: Error Handling - Rollback

**Objective**: Test rollback when deposit fails

**Steps**:
1. Lock on source chain
2. Simulate deposit failure
3. Verify rollback occurs

**Expected Result**:
- Lock transaction created
- Deposit fails
- Funds returned to source
- Order marked as rolled back

### Test 7: Privacy Verification

**Objective**: Verify privacy is maintained

**Steps**:
1. Create bridge transaction
2. Verify Zcash transaction is shielded
3. Verify Miden note is private
4. Verify viewing key can audit without revealing amounts

**Expected Result**:
- All transactions maintain privacy
- Viewing keys enable auditability
- Amounts not revealed in public state

## Integration Tests

### Test Suite Setup

```csharp
public class MidenZcashBridgeTests
{
    private CrossChainBridgeManager _bridgeManager;
    private MidenOASIS _midenProvider;
    private ZcashOASIS _zcashProvider;

    [SetUp]
    public void Setup()
    {
        // Initialize providers
        _midenProvider = new MidenOASIS();
        _zcashProvider = new ZcashOASIS();
        
        // Initialize bridges
        var bridges = new Dictionary<string, IOASISBridge>
        {
            { "ZEC", new ZcashBridgeService(...) },
            { "MIDEN", new MidenBridgeService(...) }
        };
        
        _bridgeManager = new CrossChainBridgeManager(bridges);
    }

    [Test]
    public async Task TestZcashToMidenBridge()
    {
        // Test implementation
    }

    [Test]
    public async Task TestMidenToZcashBridge()
    {
        // Test implementation
    }
}
```

## Manual Testing Checklist

- [ ] Miden testnet connection works
- [ ] Zcash testnet connection works
- [ ] Zcash → Miden bridge completes
- [ ] Miden → Zcash bridge completes
- [ ] STARK proofs generate correctly
- [ ] STARK proofs verify correctly
- [ ] Viewing keys are generated
- [ ] Viewing keys enable auditability
- [ ] Error handling works (insufficient balance)
- [ ] Error handling works (rollback)
- [ ] Privacy is maintained
- [ ] Bridge order status tracking works

## Performance Testing

### Metrics to Track

1. **Bridge Completion Time**
   - Target: < 5 minutes
   - Measure: Time from order creation to completion

2. **STARK Proof Generation Time**
   - Target: < 30 seconds
   - Measure: Time to generate proof

3. **Transaction Confirmation Time**
   - Target: < 2 minutes per chain
   - Measure: Time for transaction confirmation

## Security Testing

1. **Proof Verification**
   - Test with invalid proofs
   - Test with tampered proofs
   - Test with expired proofs

2. **Viewing Key Security**
   - Verify viewing keys don't reveal amounts
   - Verify viewing keys can't be used to spend
   - Verify audit trail is correct

3. **Rollback Security**
   - Verify rollback returns correct amount
   - Verify rollback doesn't create double-spend
   - Verify rollback is atomic

## Troubleshooting

### Common Issues

1. **Miden API Connection Failed**
   - Check `MIDEN_API_URL` environment variable
   - Verify testnet is accessible
   - Check API key if required

2. **STARK Proof Generation Fails**
   - Verify program hash is correct
   - Check inputs/outputs format
   - Verify Miden API supports proof generation

3. **Bridge Order Fails**
   - Check balances on both chains
   - Verify addresses are valid
   - Check transaction status on both chains

## Next Steps After Testing

1. Fix any issues found
2. Optimize performance if needed
3. Add additional test coverage
4. Update documentation
5. Prepare for demo

