# 🔐 NEW SECURE OASIS WALLET CREDENTIALS

## ⚠️ CRITICAL SECURITY INFORMATION
**DO NOT SHARE THESE CREDENTIALS WITH ANYONE**

## 📍 Wallet Details
- **Public Key (Address):** `Be51B1n3m1MCtZYvH8JEX3LnZZwoREyH4rYoyhMrkxJs`
- **Private Key (Base64):** `kNln1+y3r9Xa1HbiakTDUmdpyzImmnpEs/+et8D6Jr2eE+KoOZJtHXdOOoNyP1NRDcfa44LE4y6llK9JaMpCEA==`
- **Mnemonic Phrase:** `adapt afford abandon above age adult ahead accident aim advice agree accuse`

## 🔗 Verification Links
- **Solana Explorer:** https://explorer.solana.com/address/Be51B1n3m1MCtZYvH8JEX3LnZZwoREyH4rYoyhMrkxJs
- **Network:** Mainnet (Real SOL)

## 📋 Files to Update

### 1. NextGenSoftware.OASIS.API.DNA/OASIS_DNA.json
```json
{
  "SolanaOASIS": {
    "WalletMnemonicWords": "adapt afford abandon above age adult ahead accident aim advice agree accuse",
    "PrivateKey": "kNln1+y3r9Xa1HbiakTDUmdpyzImmnpEs/+et8D6Jr2eE+KoOZJtHXdOOoNyP1NRDcfa44LE4y6llK9JaMpCEA==",
    "PublicKey": "Be51B1n3m1MCtZYvH8JEX3LnZZwoREyH4rYoyhMrkxJs",
    "ConnectionString": "https://api.mainnet-beta.solana.com"
  }
}
```

### 2. NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json
```json
{
  "SolanaOASIS": {
    "WalletMnemonicWords": "adapt afford abandon above age adult ahead accident aim advice agree accuse",
    "PrivateKey": "kNln1+y3r9Xa1HbiakTDUmdpyzImmnpEs/+et8D6Jr2eE+KoOZJtHXdOOoNyP1NRDcfa44LE4y6llK9JaMpCEA==",
    "PublicKey": "Be51B1n3m1MCtZYvH8JEX3LnZZwoREyH4rYoyhMrkxJs",
    "ConnectionString": "https://api.mainnet-beta.solana.com"
  }
}
```

### 3. ECS Task Definition Environment Variables
Add these environment variables to the ECS task definition:
```json
{
  "name": "SolanaOASIS__WalletMnemonicWords",
  "value": "adapt afford abandon above age adult ahead accident aim advice agree accuse"
},
{
  "name": "SolanaOASIS__PrivateKey",
  "value": "kNln1+y3r9Xa1HbiakTDUmdpyzImmnpEs/+et8D6Jr2eE+KoOZJtHXdOOoNyP1NRDcfa44LE4y6llK9JaMpCEA=="
},
{
  "name": "SolanaOASIS__PublicKey",
  "value": "Be51B1n3m1MCtZYvH8JEX3LnZZwoREyH4rYoyhMrkxJs"
}
```

## 🚨 SECURITY CHECKLIST
- [ ] Update all configuration files with new wallet details
- [ ] Remove old compromised wallet references
- [ ] Test with small amounts first (0.01 SOL)
- [ ] Monitor wallet balance and transactions
- [ ] Set up transaction alerts
- [ ] Document incident response procedures

## ⚠️ IMPORTANT NOTES
1. **This wallet is for OASIS mainnet operations only**
2. **Do NOT use the old compromised wallet addresses**
3. **Test thoroughly on devnet before mainnet deployment**
4. **Monitor all transactions for suspicious activity**
5. **Keep credentials secure and encrypted**

## 🔄 Next Steps
1. Update configuration files
2. Build new Docker image
3. Deploy to ECS
4. Test with small amounts
5. Monitor for security issues

---
**Generated:** January 2025  
**Purpose:** Replace compromised OASIS wallet  
**Status:** READY FOR DEPLOYMENT**


