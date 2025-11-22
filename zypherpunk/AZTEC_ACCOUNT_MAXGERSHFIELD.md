# 🔐 Aztec Account: maxgershfield

## Account Details

**Status**: ✅ Created and Registered (Deployment pending due to testnet restrictions)

### Account Information

- **Alias**: `maxgershfield`
- **Address**: `0x09d16dbfac70e06fc61cbd984190ac9d385131f1011faeb436da4e17eaa2a686`
- **Public Key**: `0x028d3e15b8a3355e381263d067e40b6152c737132eab258c4917d02fcc6cd8602c6d4c190f447a496b9cd3dc7fffec2d2ab60622f10ce4be248897957153d4a42db3c00a984f4bcb0fdfe1662540ac48c9f9e6addb39d3aac597d6da69547139191a3d042eb0ea9f7fcc5fcfe444c8894beaa605baeca4de477baed33c9ffde0281f8979fed4e5e7f23714d5c89169d24b421b0c7f1295f2a7cd25f793fdf69e1a41b694bfb3beaf77718900553ad771766dbea88dc9f7ab13f67a684468c77b175068c56a23b872e6b2f2a8522c65539727c48487503cdb626268dcb554cee10e52b50c6c2ce62b0ffcc8a52ed7b8f5ce9be2359288bfccd223ba74b0f5e20a`
- **Secret Key**: `0x13b1096a8b708a4788ce8ff9189b85e17b0846dc9df676361a24eb618b1c50de`
- **Partial Address**: `0x0945a96b3d0ad9082aee962aaebe6e039c913bf820ecc39bcf610a629cef1e00`
- **Salt**: `0x0000000000000000000000000000000000000000000000000000000000000000`
- **Init Hash**: `0x0795f2cb85cd9af0bca3a8df3caa3291c7036f686c160f97dd7faaf3d8b1ed26`

### Account Status

- ✅ **Created**: Account successfully created
- ✅ **Registered**: Account registered with Aztec testnet
- ✅ **Stored**: Account stored in wallet database with alias `maxgershfield`
- ⚠️ **Deployment**: Deployment failed with "Transactions are not permitted" error

### Deployment Issue

The account deployment failed because the Aztec testnet node (`https://aztec-testnet-fullnode.zkv.xyz`) returned:
```
Error: Invalid tx: Transactions are not permitted
```

**Possible Reasons**:
1. Testnet node is in read-only mode
2. Account needs to be deployed via different method
3. Testnet requires additional permissions or setup

**Note**: The account is still usable for queries and can be deployed later when testnet permissions are available.

---

## Using the Account

### In OASIS Bridge

You can use this account address in the OASIS bridge:

```json
{
  "fromToken": "ZEC",
  "toToken": "AZTEC",
  "amount": 0.5,
  "fromAddress": "zt1test123",
  "toAddress": "0x09d16dbfac70e06fc61cbd984190ac9d385131f1011faeb436da4e17eaa2a686",
  "fromChain": "Zcash",
  "toChain": "Aztec"
}
```

### Query Account Balance

The OASIS bridge can now query real private notes for this account:

```csharp
var balanceResult = await aztecBridge.GetAccountBalanceAsync(
    "0x09d16dbfac70e06fc61cbd984190ac9d385131f1011faeb436da4e17eaa2a686"
);
```

This will query **REAL** private notes from Aztec testnet - NO MOCKS.

---

## Next Steps

1. **Try Alternative Deployment Method**: 
   - May need to use Aztec SDK directly
   - Or wait for testnet to allow transactions

2. **Use Account for Queries**:
   - Account can be used for balance queries
   - Can query private notes
   - Can check transaction status

3. **Deploy When Ready**:
   - Once testnet allows transactions, deploy using:
     ```bash
     aztec-wallet deploy-account \
         --node-url $NODE_URL \
         --from maxgershfield \
         --payment method=fpc-sponsored,fpc=contracts:sponsoredfpc \
         --register-class
     ```

---

## Account Storage Location

- **Wallet Database**: `~/.aztec/wallet/`
- **Alias**: `maxgershfield` (also stored as `last`)

---

**Created**: 2024-01-15
**Status**: Ready for queries, deployment pending

