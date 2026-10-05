# Starknet Address Generation Fix - Implementation Brief

## Problem

Starknet addresses are being rejected by the faucet:
- **Address:** `0xf260c0af2b1346307ff0cbfc5062ff574827dbe218ff0a5c3194683718727b4a`
- **Format:** ✅ Correct (66 chars: 0x + 64 hex)
- **Issue:** ❌ Using SHA256 hash instead of Pedersen hash

## Root Cause

**Current Implementation (WRONG):**
```csharp
// We're using SHA256 - this is incorrect for Starknet!
byte[] hash = ComputeSHA256(publicKeyBytes);
byte[] addressBytes = hash.Take(32).ToArray();
return "0x" + BytesToHex(addressBytes).ToLower();
```

**Starknet Requirements:**
- ✅ **Pedersen hash** (not SHA256)
- ✅ Account class hash
- ✅ Constructor calldata
- ✅ Proper account derivation algorithm

## Starknet Address Derivation

Starknet addresses are derived using:
```
address = pedersen_hash(
    account_class_hash,
    pedersen_hash(public_key, salt),
    constructor_calldata_hash
)
```

This is **much more complex** than a simple hash!

## Solution Options

### Option 1: Use StarkSharp SDK (✅ FOUND - Recommended)

**StarkSharp - Starknet .NET SDK:**
- **Repository:** https://github.com/project3fusion/StarkSharp
- **License:** MIT
- **Status:** Active (18 stars, 5 forks)
- **Features:**
  - ✅ ABI support
  - ✅ JSON RPC
  - ✅ Stark Curve Signer
  - ✅ Web Socket Wallet Connection
  - ✅ Address generation (implied by wallet features)

**Installation:**
```bash
# Clone repository
git clone https://github.com/project3fusion/StarkSharp.git

# Or add as project reference
# Or check if available on NuGet
```

**Note:** SDK is **NOT audited** - use at your own risk (as stated in their README)

### Option 2: Implement Pedersen Hash in C#

**Challenges:**
- Pedersen hash is complex (elliptic curve operations)
- Requires Starknet's specific curve parameters
- Account class hash must be correct
- Constructor calldata format must match

**Libraries:**
- May need to port from Python/JavaScript
- Or use FFI bindings to Rust/C++ implementation

### Option 3: Use Starknet CLI Tool

Similar to Miden approach:
- Install Starknet CLI (starknet-devnet or starknet.py)
- Call from C# via Process execution
- Parse output for address

## Recommended Implementation

### Step 1: Add StarkSharp to Project

**Option A: Add as Project Reference**
```xml
<!-- In NextGenSoftware.OASIS.API.Core.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\..\external-libs\StarkSharp\StarkSharp.csproj" />
</ItemGroup>
```

**Option B: Clone and Reference**
```bash
cd external-libs
git clone https://github.com/project3fusion/StarkSharp.git
```

**Option C: Check NuGet (if available)**
```bash
dotnet add package StarkSharp
```

### Step 2: Update AddressDerivationHelper.cs

**Using StarkSharp SDK:**
```csharp
using StarkSharp; // Adjust namespace based on actual SDK structure
using StarkSharp.Accounts; // May vary

private static string DeriveStarknetAddress(string publicKey, string network = "mainnet")
{
    try
    {
        // StarkSharp likely has account creation/address derivation
        // Exact API may vary - check StarkSharp documentation/examples
        
        // Option 1: If SDK has direct address derivation
        // var address = StarkSharp.Accounts.AccountDeriver.DeriveAddress(publicKey, network);
        
        // Option 2: If SDK requires account creation
        // var account = new StarknetAccount(publicKey, network);
        // return account.Address;
        
        // Option 3: If SDK uses signer/wallet
        // var signer = new StarkCurveSigner(publicKey);
        // var account = new Account(signer, network);
        // return account.Address;
        
        // Placeholder - adjust based on actual StarkSharp API
        // TODO: Check StarkSharp repository for exact API
        throw new NotImplementedException("StarkSharp integration pending - check SDK API");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error deriving Starknet address with StarkSharp: {ex.Message}");
        return GetStarknetAddressFallback(publicKey, network);
    }
}
```

**Important:** Check StarkSharp repository for exact API:
- Review examples in their README
- Check their test files
- Look for account/address generation methods

**If Using CLI:**
```csharp
private static string DeriveStarknetAddress(string publicKey, string network = "mainnet")
{
    try
    {
        string starknetCliPath = GetStarknetCliPath();
        if (string.IsNullOrEmpty(starknetCliPath))
        {
            return GetStarknetAddressFallback(publicKey, network);
        }

        // Call Starknet CLI
        string arguments = $"account derive --public-key {publicKey} --network {network}";
        
        var processInfo = new ProcessStartInfo
        {
            FileName = starknetCliPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processInfo))
        {
            if (process == null)
                return GetStarknetAddressFallback(publicKey, network);

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
                return GetStarknetAddressFallback(publicKey, network);

            // Parse address from output
            string address = ParseStarknetCliOutput(output);
            if (IsValidStarknetAddress(address))
                return address;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error calling Starknet CLI: {ex.Message}");
    }

    return GetStarknetAddressFallback(publicKey, network);
}
```

### Step 3: Validation

```csharp
private static bool IsValidStarknetAddress(string address)
{
    if (string.IsNullOrEmpty(address))
        return false;
    
    // Must be 66 chars: 0x + 64 hex
    if (address.Length != 66)
        return false;
    
    if (!address.StartsWith("0x"))
        return false;
    
    // Must be valid hex
    string hex = address.Substring(2);
    return System.Text.RegularExpressions.Regex.IsMatch(hex, @"^[0-9a-fA-F]{64}$");
}
```

## Testing

1. **Generate address using SDK/CLI**
2. **Verify format:** 66 chars, starts with 0x
3. **Test with Starknet faucet:**
   - https://stakely.io/faucet/starknet
   - https://faucet.starknet.io (if available)
4. **Check if account needs deployment** (some faucets require deployed accounts)

## Important Notes

### Account Deployment

Starknet addresses may need to be **deployed** before use:
- Addresses are contract addresses
- Must be deployed to network
- Faucet may reject undeployed addresses

### Network Compatibility

- **Testnet:** Use testnet addresses
- **Mainnet:** Use mainnet addresses
- Ensure network matches faucet network

## Alternative: Pre-deploy Accounts

If address generation is complex:
1. Generate addresses using official Starknet tools
2. Deploy accounts to testnet
3. Store deployed addresses in database
4. Assign to users on demand

## References

- **StarkSharp SDK:** https://github.com/project3fusion/StarkSharp ⭐ **USE THIS**
- Starknet Documentation: https://docs.starknet.io
- Starknet.js: https://github.com/starknet-io/starknet.js
- Starknet Python: https://github.com/starknet-py/starknet.py
- Address Format: https://docs.starknet.io/documentation/architecture_and_concepts/Accounts/account-abstraction/

## Next Steps

1. **Research .NET SDK availability**
2. **If SDK found:** Integrate into `AddressDerivationHelper.cs`
3. **If no SDK:** Use CLI approach (like Miden)
4. **Test with faucet**
5. **Handle account deployment if required**

