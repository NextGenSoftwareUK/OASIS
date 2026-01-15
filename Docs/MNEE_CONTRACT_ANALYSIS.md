# MNEE Contract Analysis

**Contract Address:** `0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF`  
**Contract Type:** TransparentUpgradeableProxy (OpenZeppelin v4.8.3)  
**Analysis Date:** 2026-01-12

---

## Contract Architecture

### Proxy Pattern

MNEE uses OpenZeppelin's **TransparentUpgradeableProxy** pattern:

```
┌─────────────────────────────────────┐
│  TransparentUpgradeableProxy         │
│  Address: 0x8ccedbAe4916b79da...    │  ← We interact with this
└──────────────┬──────────────────────┘
               │ (delegates calls)
               ▼
┌─────────────────────────────────────┐
│  ERC-20 Implementation Contract      │
│  (Actual MNEE token logic)           │  ← Implementation can be upgraded
└─────────────────────────────────────┘
```

### Key Points

1. **Proxy Address is Stable**
   - The proxy address (`0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF`) never changes
   - Users always interact with this address
   - Implementation can be upgraded without affecting users

2. **Transparent Proxy Pattern**
   - Admin functions are separate from user functions
   - Regular users can't accidentally call admin functions
   - All ERC-20 calls are forwarded to implementation

3. **Standard ERC-20 Interaction**
   - We use standard ERC-20 ABI
   - No special handling needed
   - Proxy forwards all calls transparently

---

## How to Interact with MNEE

### For Regular Users (Our Use Case)

**We interact with the proxy address using standard ERC-20 methods:**

```csharp
// Standard ERC-20 interaction - proxy forwards to implementation
var contract = web3.Eth.GetContract(ERC20_ABI, "0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF");

// These calls go through the proxy to the implementation
var balance = await contract.GetFunction("balanceOf").CallAsync<BigInteger>(address);
var transfer = await contract.GetFunction("transfer").SendTransactionAsync(...);
```

### For Admins (Not Our Use Case)

Admin functions are available but require admin privileges:
- `admin()` - Get admin address
- `implementation()` - Get implementation address
- `changeAdmin(address)` - Change admin
- `upgradeTo(address)` - Upgrade implementation
- `upgradeToAndCall(address, bytes)` - Upgrade and initialize

**We don't need these for our integration.**

---

## ERC-20 Methods Available

Since the proxy forwards all calls, we have access to all standard ERC-20 methods:

### View Functions (Read-Only)
```solidity
function balanceOf(address account) external view returns (uint256);
function totalSupply() external view returns (uint256);
function allowance(address owner, address spender) external view returns (uint256);
function decimals() external view returns (uint8);
function symbol() external view returns (string);
function name() external view returns (string);
```

### State-Changing Functions
```solidity
function transfer(address to, uint256 amount) external returns (bool);
function approve(address spender, uint256 amount) external returns (bool);
function transferFrom(address from, address to, uint256 amount) external returns (bool);
```

### Events
```solidity
event Transfer(address indexed from, address indexed to, uint256 value);
event Approval(address indexed owner, address indexed spender, uint256 value);
```

---

## Implementation Strategy

### Standard ERC-20 ABI

We use the standard ERC-20 ABI. The proxy pattern doesn't change this:

```json
[
  {
    "constant": true,
    "inputs": [{"name": "_owner", "type": "address"}],
    "name": "balanceOf",
    "outputs": [{"name": "balance", "type": "uint256"}],
    "type": "function"
  },
  {
    "constant": false,
    "inputs": [
      {"name": "_to", "type": "address"},
      {"name": "_value", "type": "uint256"}
    ],
    "name": "transfer",
    "outputs": [{"name": "", "type": "bool"}],
    "type": "function"
  },
  {
    "constant": false,
    "inputs": [
      {"name": "_spender", "type": "address"},
      {"name": "_value", "type": "uint256"}
    ],
    "name": "approve",
    "outputs": [{"name": "", "type": "bool"}],
    "type": "function"
  },
  {
    "constant": true,
    "inputs": [
      {"name": "_owner", "type": "address"},
      {"name": "_spender", "type": "address"}
    ],
    "name": "allowance",
    "outputs": [{"name": "", "type": "uint256"}],
    "type": "function"
  },
  {
    "constant": true,
    "inputs": [],
    "name": "decimals",
    "outputs": [{"name": "", "type": "uint8"}],
    "type": "function"
  },
  {
    "constant": true,
    "inputs": [],
    "name": "symbol",
    "outputs": [{"name": "", "type": "string"}],
    "type": "function"
  },
  {
    "constant": true,
    "inputs": [],
    "name": "name",
    "outputs": [{"name": "", "type": "string"}],
    "type": "function"
  }
]
```

### Code Implementation

```csharp
public class MNEEService
{
    // Use proxy address - it forwards to implementation
    private const string MNEE_CONTRACT_ADDRESS = "0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF";
    
    // Standard ERC-20 ABI works perfectly
    private const string ERC20_ABI = @"[...]"; // Standard ERC-20 ABI
    
    public MNEEService(string rpcUrl)
    {
        _web3 = new Web3(rpcUrl);
        // Proxy automatically forwards all calls to implementation
        _mneeContract = _web3.Eth.GetContract(ERC20_ABI, MNEE_CONTRACT_ADDRESS);
    }
    
    // All standard ERC-20 methods work as expected
    public async Task<decimal> GetBalanceAsync(string address) { ... }
    public async Task<string> TransferAsync(...) { ... }
    public async Task<string> ApproveAsync(...) { ... }
}
```

---

## Security Considerations

### Proxy Pattern Benefits

1. **Upgradeability**
   - Implementation can be upgraded for bug fixes
   - New features can be added
   - Address remains the same

2. **Transparency**
   - Admin functions are separate
   - Regular users can't accidentally call admin functions
   - Clear separation of concerns

3. **Standard Interface**
   - Still behaves like standard ERC-20
   - No breaking changes for users
   - Compatible with all ERC-20 tools

### For Our Integration

✅ **Safe to use** - Standard ERC-20 interaction  
✅ **No special handling** - Proxy is transparent  
✅ **Future-proof** - Implementation upgrades don't affect us  
✅ **Standard ABI** - Use standard ERC-20 ABI  

---

## Getting the Implementation Address (Optional)

If you want to see the actual implementation contract:

```csharp
// Read from storage slot (EIP-1967)
// Slot: 0x360894a13ba1a3210667c828492db98dca3e2076cc3735a920a3ca505d382bbc
var implementationAddress = await web3.Eth.GetStorageAt.SendRequestAsync(
    "0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF",
    new HexBigInteger("0x360894a13ba1a3210667c828492db98dca3e2076cc3735a920a3ca505d382bbc")
);
```

Or use the proxy interface:
```csharp
var proxyInterface = web3.Eth.GetContract(PROXY_ABI, MNEE_CONTRACT_ADDRESS);
var implementation = await proxyInterface.GetFunction("implementation").CallAsync<string>();
```

**Note:** We don't need this for our integration - we always use the proxy address.

---

## Conclusion

**For OASIS Integration:**

1. ✅ Use proxy address: `0x8ccedbAe4916b79da7F3F612EfB2EB93A2bFD6cF`
2. ✅ Use standard ERC-20 ABI
3. ✅ Interact as if it's a normal ERC-20 token
4. ✅ Proxy handles forwarding automatically
5. ✅ No special code needed

**The proxy pattern is transparent to our integration!**

---

**References:**
- OpenZeppelin TransparentUpgradeableProxy: https://docs.openzeppelin.com/contracts/4.x/api/proxy#TransparentUpgradeableProxy
- EIP-1967: https://eips.ethereum.org/EIPS/eip-1967
- ERC-20 Standard: https://eips.ethereum.org/EIPS/eip-20
