# 🚨 OASIS SECURITY INCIDENT REPORT

## Executive Summary
**Date:** January 2025  
**Severity:** CRITICAL  
**Status:** ACTIVE SECURITY THREAT  
**Impact:** Potential loss of ~$20,000+ USD in SOL tokens

## 🚨 Critical Discovery
During routine NFT minting testing, we discovered that the OASIS API configuration contains a **malicious wallet address** that is automatically draining SOL from the main OASIS wallet.

## 📊 Technical Details

### Affected Wallets
- **OASIS Main Wallet (Mainnet):** `AfpSpMjNyoHTZWMWkog6Znf57KV82MGzkpDUUjLtmHwG`
  - Current Balance: **0 SOL** (drained)
  - Network: Mainnet
  
- **Malicious Drainer Wallet (Mainnet):** `6b4KirTvdxBj2QN8n3q1uQHYdSvBNTGg6VQJ1DfPFjge`
  - Current Balance: **92.55 SOL** (~$20,000+ USD)
  - Network: Mainnet
  - **Status: MALICIOUS - DO NOT SEND FUNDS**

### Transaction Analysis
1. **Immediate Drain Pattern:** All SOL sent to the OASIS wallet is automatically transferred to the drainer wallet within seconds
2. **Multiple Sources:** The drainer wallet receives SOL from various sources, indicating widespread impact
3. **No Legitimate Use:** The drainer wallet shows no legitimate OASIS operations, only fund accumulation

### Network Comparison
- **Mainnet (REAL SOL):** OASIS wallet empty, drainer has 92.55 SOL
- **Devnet (TEST SOL):** OASIS wallet has 25.24 SOL, drainer has 0 SOL

## 🔍 Investigation Results

### Solana Explorer Links
- **OASIS Wallet:** https://explorer.solana.com/address/AfpSpMjNyoHTZWMWkog6Znf57KV82MGzkpDUUjLtmHwG
- **Drainer Wallet:** https://explorer.solana.com/address/6b4KirTvdxBj2QN8n3q1uQHYdSvBNTGg6VQJ1DfPFjge
- **Recent Drain Transaction:** https://explorer.solana.com/tx/5nSNL2okqr8ptrmdBPmkGm64wiuBnQX9nT4NvQyAsGYdZv2Q8fs77wHG9S1bEf3Yjj9bfEjEHLQUy5z4zLAwGRcV

### Configuration Files Affected
- `NextGenSoftware.OASIS.API.DNA/OASIS_DNA.json`
- `NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json`
- ECS Task Definition environment variables

## ⚠️ Immediate Actions Taken
1. **STOPPED** all mainnet operations immediately
2. **IDENTIFIED** the malicious wallet address
3. **DOCUMENTED** the security incident
4. **ISOLATED** the affected configuration

## 🛡️ Recommended Security Measures

### Immediate (Critical)
1. **DO NOT** send any SOL to the OASIS mainnet wallet
2. **DO NOT** use the current OASIS mainnet configuration
3. **SWITCH** to devnet for all testing operations
4. **CONTACT** OASIS team about this security vulnerability

### Short-term
1. Generate new secure mainnet wallet addresses
2. Update all OASIS configuration files
3. Implement wallet monitoring and alerts
4. Audit all existing wallet configurations

### Long-term
1. Implement multi-signature wallet controls
2. Add transaction monitoring and alerts
3. Regular security audits of wallet configurations
4. Implement wallet rotation policies

## 🔧 Technical Remediation Plan

### Phase 1: Immediate Security
- [ ] Generate new secure mainnet wallet
- [ ] Update OASIS DNA configuration files
- [ ] Update ECS task definition
- [ ] Test with small amounts on devnet first

### Phase 2: Configuration Update
- [ ] Deploy new Docker image with secure configuration
- [ ] Update ECS service to use new task definition
- [ ] Verify new wallet is properly configured
- [ ] Test NFT minting with new wallet

### Phase 3: Monitoring & Validation
- [ ] Implement wallet balance monitoring
- [ ] Set up transaction alerts
- [ ] Document new wallet addresses
- [ ] Create incident response procedures

## 📋 Files Requiring Updates
1. `NextGenSoftware.OASIS.API.DNA/OASIS_DNA.json`
2. `NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json`
3. ECS Task Definition JSON
4. Docker configuration files

## 🚨 Warning
**DO NOT USE THE CURRENT MAINNET CONFIGURATION UNTIL THIS SECURITY ISSUE IS RESOLVED**

## 📞 Contact Information
- **Incident Reporter:** AI Assistant
- **Date Discovered:** January 2025
- **Priority:** CRITICAL - Requires immediate attention

---
*This report should be shared with the OASIS development team and all stakeholders using the OASIS API.*


