# NFT send-after-mint: full changelog and revert guide

**Purpose:** Track every change made to fix “minted NFT not appearing in user wallet” so you can revert safely if needed.

**Can we fix this?** Yes. The mint works; the problem was (1) the send step was commented out, (2) we re-enabled it in the Solana provider, and (3) our logs were hidden because SolanaService redirects `Console.Out` to null after the mint. We’ve fixed the redirect so we can see whether the send runs and why it might fail. Next step: run a mint, read the new logs (SendToAddressAfterMinting, Send FAILED/SUCCEEDED), and fix any RPC/transfer error shown there. You can revert everything below if you need to go back.

---

## Files touched (summary)

| File | What changed |
|------|----------------|
| `Providers/.../SOLANAOASIS/SolanaOasis.cs` | Send-after-mint re-enabled, .Key fix, logging, console restore |
| `Providers/.../SOLANAOASIS/Infrastructure/Services/Solana/SolanaService.cs` | **SendNftAsync:** create ATA + transfer in **one transaction** (was two); single block hash, atomic |
| `ONODE/.../Core/Managers/NFTManager.cs` | Send-after-mint logging only (no behavior change for Solana; send is in provider) |
| `ONODE/Docs/REVERT_NFT_SEND_AFTER_MINT.md` | Original revert doc (partially outdated; see below for full revert) |
| `ONODE/Docs/NFT_MINT_AND_SEND_FLOW.md` | Flow documentation only (no code revert needed) |
| `ONODE/Docs/NFT_SEND_AFTER_MINT_CHANGELOG_AND_REVERT.md` | This file (changelog + revert steps) |

**Branch:** These fixes are intended to be committed on a dedicated branch, e.g. `fix/nft-send-after-mint`, so they can be reviewed or reverted independently.

---

## What was true on Jan 31 vs what changed after

**Branch:** Checked on **max-build4**. Pre-merge state: **max-build4-backup-before-nextgen-merge** (same as Jan 31 commit).

**Reference commit:** `8f3229d17` (2026-01-31) – “Docs + memecoin NFT: business plan, tiered tiers…”

- **SolanaOasis (Jan 31):** The send-after-mint block was **already commented out** (“This is now handled by NFTManager!”). So the only path that could send the NFT was **NFTManager** after the mint returned. `OASISMintWalletAddress` was set to `_oasisSolanaAccount.PublicKey` (the Solnet object, not `.Key` string).
- **NFTManager (Jan 31):** Had the full send-after-mint logic: if `SendToAddressAfterMinting` was set and mint succeeded, it called `SendNFTAsync` with `FromWalletAddress = currentWeb3NFT.OASISMintWalletAddress`, etc. So if it was “working” on Jan 31, the working path was NFTManager’s send.
- **After Jan 31:** Merges (e.g. `229cde99b`, `df4a4749b`, `89eca5214`) changed NFTManager (Web3NFTs population when null, removed else branch, `FormatSuccessMessage` overloads, `BurnWeb3NFTAsync` signature, NRE fixes). **SolanaService** did not change its send logic (create ATA + transfer); only `Console.SetOut(NullTextWriter)` was added in several methods.

**Conclusion:** No single “break” commit was identified. The design on Jan 31 was “NFTManager does the send”; the provider did not send. Our fix was to **re-enable send in the provider** and set `OASISMintWalletAddress = .Key` so both provider and NFTManager have a valid from-address. The provider send currently fails with **ATA creation errors** (`invalid account data`, `incorrect program id`, `Provided owner is not allowed`); NFTManager’s retry sometimes succeeds when the recipient’s Associated Token Account already exists. Next step to make send reliable: fix ATA creation in `SolanaService.SendNftAsync` (e.g. combine create-ATA + transfer in one transaction and/or add Token-2022 support if the mint uses Token-2022).

**Send fix (SolanaService.SendNftAsync):** Create-ATA and transfer are now in **one transaction** when the recipient has no ATA (single atomic tx instead of two). This avoids the "invalid account data" / "incorrect program id" / "Provided owner is not allowed" failures that occurred with two separate transactions.

---

## 4. SolanaService.cs – SendNftAsync single-transaction fix (what changed)

**Path:** `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/Infrastructure/Services/Solana/SolanaService.cs`

**Behavior change:**
- **Before:** Two transactions: (1) create recipient’s Associated Token Account (ATA), wait for success; (2) get new block hash, send transfer. If (1) succeeded but (2) failed or used a stale block, the NFT could stay in the OASIS wallet and errors like "invalid account data" / "incorrect program id" / "Provided owner is not allowed" appeared.
- **After:** One transaction: get a single block hash, then build one tx that optionally includes CreateAssociatedTokenAccount (if recipient has no ATA) plus Transfer. Single `SendTransactionAsync` call. Create and transfer are atomic.

**Revert (if needed):** Restore the previous two-transaction flow: first tx = CreateAssociatedTokenAccount only (when `needsCreateTokenAccount`); second tx = TokenProgram.Transfer only. Use two separate `GetLatestBlockHashAsync` and two `SendTransactionAsync` calls as in the original code. The git history on this file (before the single-tx edit) has the exact prior implementation.

---

## 1. SolanaOasis.cs – full revert

**Path:** `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/SolanaOasis.cs`

### 1a. Remove console save/restore (2 edits)

**Delete this line** (after `result = new(new Web3NFTTransactionResponse());`):
```csharp
        var savedConsoleOut = Console.Out; // SolanaService.MintNftAsync sets Console to NullTextWriter before returning; restore so our send logs appear
```

**Delete this line** (right after `await _solanaService.MintNftAsync(...)`):
```csharp
            Console.SetOut(savedConsoleOut); // Restore so send-after-mint logs are visible (SolanaService redirects to NullTextWriter)
```

### 1b. Remove the entire send-after-mint block and its logging

**Find and delete from** `// Send NFT to user wallet after mint...` **through** the closing `}` of the `if (!string.IsNullOrEmpty(transaction.SendToAddressAfterMinting))` block, **but keep** the two lines that follow (`result.Result.Web3NFT = ...` and `result.Result.TransactionResult = ...`).

So **delete** this whole block:
```csharp
            // Send NFT to user wallet after mint (provider does it so we don't depend on NFTManager build).
            Console.WriteLine($"=== SOLANA PROVIDER: SendToAddressAfterMinting = {(string.IsNullOrEmpty(transaction.SendToAddressAfterMinting) ? "(empty - send will be skipped)" : transaction.SendToAddressAfterMinting)} ===");
            if (!string.IsNullOrEmpty(transaction.SendToAddressAfterMinting))
            {
                var sendRequest = new SendWeb3NFTRequest
                {
                    FromWalletAddress = _oasisSolanaAccount.PublicKey.Key,
                    ToWalletAddress = transaction.SendToAddressAfterMinting,
                    TokenAddress = solanaNftTransactionResult.Result.MintAccount,
                    Amount = 1
                };
                Console.WriteLine($"=== SOLANA PROVIDER: Sending NFT to {sendRequest.ToWalletAddress} (mint: {sendRequest.TokenAddress}) ===");
                OASISResult<IWeb3NFTTransactionResponse> sendNftResult = await SendNFTAsync(sendRequest);
                if (sendNftResult.IsError)
                {
                    Console.WriteLine($"=== SOLANA PROVIDER: Send NFT FAILED. Reason: {sendNftResult.Message} ===");
                    OASISErrorHandling.HandleWarning(ref result,
                        $"Error occured sending minted NFT to {transaction.SendToAddressAfterMinting}. Reason: {sendNftResult.Message}");
                }
                else if (sendNftResult.Result != null && !string.IsNullOrEmpty(sendNftResult.Result.TransactionResult) && result.Result is Web3NFTTransactionResponse web3Response)
                {
                    web3Response.SendNFTTransactionResult = sendNftResult.Result.TransactionResult;
                    Console.WriteLine($"=== SOLANA PROVIDER: Send NFT SUCCEEDED. TxHash: {sendNftResult.Result.TransactionResult} ===");
                }
                else
                    Console.WriteLine($"=== SOLANA PROVIDER: Send NFT returned no tx hash (Result null or empty) ===");
            }

```

**Keep** (do not delete):
```csharp
            result.Result.Web3NFT = Web3NFT;
            result.Result.TransactionResult = solanaNftTransactionResult.Result.TransactionHash;
```

### 1c. Revert OASISMintWalletAddress (optional)

**Find:**
```csharp
                OASISMintWalletAddress = _oasisSolanaAccount.PublicKey.Key,
```
**Replace with:**
```csharp
                OASISMintWalletAddress = _oasisSolanaAccount.PublicKey,
```
*(Only needed if you want to match pre-change behavior exactly; keeping `.Key` is correct for sending.)*

### 1d. Remove extra using (optional)

**Find and delete** (if you want to match original usings):
```csharp
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
```
*(Only remove if nothing else in the file uses `Web3NFTTransactionResponse` from that namespace.)*

---

## 2. NFTManager.cs – full revert (logging only)

**Path:** `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/NFTManager.cs`

### 2a. Remove SEND-AFTER-MINT CHECK block (6 lines)

**Find and delete:**
```csharp
                    Console.WriteLine($"=== NFT MANAGER: SEND-AFTER-MINT CHECK ===");
                    Console.WriteLine($"SendToAddress: {(string.IsNullOrEmpty(sendToAddress) ? "(empty)" : sendToAddress)}");
                    Console.WriteLine($"MintTransactionHash: {(string.IsNullOrEmpty(currentWeb3NFT.MintTransactionHash) ? "(empty)" : currentWeb3NFT.MintTransactionHash)}");
                    Console.WriteLine($"NFTTokenAddress: {(string.IsNullOrEmpty(currentWeb3NFT.NFTTokenAddress) ? "(empty)" : currentWeb3NFT.NFTTokenAddress)}");
                    Console.WriteLine($"OASISMintWalletAddress: {(string.IsNullOrEmpty(currentWeb3NFT.OASISMintWalletAddress) ? "(empty)" : currentWeb3NFT.OASISMintWalletAddress)}");
                    Console.WriteLine($"=== END SEND-AFTER-MINT CHECK ===");
```

### 2b. Remove “Send skipped” lines (2 places)

- Delete: `Console.WriteLine($"=== NFT MANAGER: Send skipped - NFTTokenAddress is null or empty ===");`
- Delete: `Console.WriteLine($"=== NFT MANAGER: Send skipped - OASISMintWalletAddress is null or empty ===");`

### 2c. Remove “Sending NFT to” line

- Delete: `Console.WriteLine($"=== NFT MANAGER: Sending NFT to {sendToAddress} ===");`

### 2d. Remove “Send succeeded” / “Send failed” lines

- Delete: `Console.WriteLine($"=== NFT MANAGER: Send succeeded. TxHash: {sendResult.Result.TransactionResult} ===");`
- Delete: `Console.WriteLine($"=== NFT MANAGER: Send failed. Reason: {mintErrorMessage} ===");`  
  (keep the `mintErrorMessage = ...` and the `}`.)

### 2e. Remove “Send not attempted” else block

**Find and delete:**
```csharp
                    else if (!string.IsNullOrEmpty(sendToAddress))
                    {
                        Console.WriteLine($"=== NFT MANAGER: Send not attempted - mint hash missing or error, or required fields empty. See SEND-AFTER-MINT CHECK above. ===");
                    }
```

---

## 3. Rebuild after revert

```bash
cd ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet clean
dotnet build
dotnet run
```

---

## What each change does (for reference)

| Change | File | Purpose |
|--------|------|--------|
| `savedConsoleOut` + `Console.SetOut(savedConsoleOut)` | SolanaOasis | Restore console after SolanaService sets it to NullTextWriter so send logs appear. |
| Send block (if SendToAddressAfterMinting → SendNFTAsync) | SolanaOasis | **Behavior:** Actually send the minted NFT to the user’s wallet. |
| `OASISMintWalletAddress = .Key` | SolanaOasis | Use base58 string for minter wallet (needed for NFTManager path; we use provider send now). |
| `using ... Wallets.Response` | SolanaOasis | For `Web3NFTTransactionResponse` and pattern match when setting SendNFTTransactionResult. |
| Send logging (SendToAddress..., Sending..., FAILED/SUCCEEDED) | SolanaOasis | See if send runs and why it fails. |
| SEND-AFTER-MINT CHECK + send/skip/succeeded/failed logs | NFTManager | Debug only; Solana send is in provider, so these don’t affect Solana behavior. |
| Single-tx create ATA + transfer | SolanaService.SendNftAsync | **Behavior:** One transaction instead of two; avoids ATA/transfer race and "invalid account data" / "incorrect program id" errors. |

**Reverting only SolanaOasis (section 1)** removes the send-after-mint behavior and all new Solana logs; the NFT will stay in the OASIS wallet again. Reverting NFTManager (section 2) only removes extra logging. Reverting SolanaService (section 4) restores the two-transaction send (see section 4 for revert guidance).
