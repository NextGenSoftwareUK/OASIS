# Private Key Security Guide for OASIS

**Last Updated:** 2026-01-09  
**Status:** ⚠️ **TESTING CONFIGURATION - NOT PRODUCTION READY**

---

## ⚠️ Current Testing Configuration

**For testing purposes only**, the private keys are stored with a simple encryption key. This is **NOT secure** for production use.

### Current Setup:
- **Encryption Key:** `TEST_KEY_FOR_DEVELOPMENT_ONLY_DO_NOT_USE_IN_PRODUCTION_256BIT`
- **Location:** `OASIS Architecture/NextGenSoftware.OASIS.API.DNA/OASIS_DNA.json`
- **Encryption Method:** Rijndael256 (AES-256)
- **Storage:** LocalFileOASIS (local filesystem only)

---

## 🔒 Production Security Requirements

### 1. **Strong Encryption Key**

**Current Issue:** The encryption key is hardcoded and predictable.

**Required Action:**
1. Generate a cryptographically secure 256-bit (32-byte) key
2. Store it securely (see Key Management below)
3. Never commit it to version control

**How to Generate a Secure Key:**
```bash
# Option 1: Using OpenSSL
openssl rand -base64 32

# Option 2: Using Python
python3 -c "import secrets; print(secrets.token_urlsafe(32))"

# Option 3: Using .NET
# Use System.Security.Cryptography.RandomNumberGenerator
```

**Minimum Requirements:**
- ✅ 256 bits (32 bytes) minimum
- ✅ Cryptographically random
- ✅ Unique per environment (dev/staging/prod)
- ✅ Never reused or shared

---

### 2. **Key Management**

**Current Issue:** Key is stored in plain text in configuration file.

**Production Options:**

#### Option A: Environment Variables (Recommended for Single Server)
```json
{
  "OASISProviderPrivateKeys": {
    "Rijndael256Key": "${OASIS_PRIVATE_KEY_ENCRYPTION_KEY}"
  }
}
```

**Setup:**
```bash
# Set environment variable
export OASIS_PRIVATE_KEY_ENCRYPTION_KEY="<generated-secure-key>"

# Or in .env file (never commit to git)
echo "OASIS_PRIVATE_KEY_ENCRYPTION_KEY=<generated-secure-key>" >> .env
```

#### Option B: Key Management Service (Recommended for Production)
- **AWS:** AWS Secrets Manager or AWS KMS
- **Azure:** Azure Key Vault
- **GCP:** Google Cloud Secret Manager
- **HashiCorp:** Vault

**Implementation:**
- Load key at application startup
- Cache in memory (never log or expose)
- Rotate keys periodically

#### Option C: Hardware Security Module (HSM)
- **Best for:** High-security environments
- **Use cases:** Financial services, healthcare, government
- **Providers:** AWS CloudHSM, Azure Dedicated HSM, Thales

---

### 3. **Storage Provider Security**

**Current Setup:** LocalFileOASIS (local filesystem)

**Security Considerations:**

#### ✅ Pros:
- Private keys stored locally only
- Not replicated to network providers
- Encrypted at rest

#### ⚠️ Risks:
- File system permissions must be strict
- Backup security (encrypted backups required)
- Single point of failure
- Not suitable for multi-server deployments

#### 🔒 Required File Permissions:
```bash
# Wallet files should be:
chmod 600 wallets_*.json  # Owner read/write only
chown oasis:oasis wallets_*.json  # Specific user/group
```

#### 📁 Recommended Storage Locations:
- **Linux:** `/var/lib/oasis/wallets/` (protected directory)
- **Windows:** `C:\ProgramData\OASIS\Wallets\` (protected directory)
- **macOS:** `~/Library/Application Support/OASIS/LocalFileOASIS/` (user-specific)

---

### 4. **Access Control**

**Current Issue:** No additional access controls beyond file permissions.

**Production Requirements:**

#### A. Application-Level Access Control
- ✅ Only authenticated users can access their own wallets
- ✅ Admin users require additional authorization
- ✅ Audit logging for all private key access

#### B. Network Security
- ✅ API endpoints must use HTTPS/TLS
- ✅ Private key endpoints should require additional authentication
- ✅ Rate limiting on sensitive endpoints

#### C. Database/Storage Security
- ✅ Encrypt wallet files at rest (already implemented)
- ✅ Use encrypted backups
- ✅ Secure deletion of old wallet files

---

### 5. **Key Rotation**

**Current Issue:** No key rotation mechanism.

**Production Requirements:**
1. **Periodic Rotation:** Rotate encryption keys every 90-180 days
2. **Re-encryption:** Re-encrypt all private keys with new key
3. **Key Versioning:** Support multiple key versions during transition
4. **Backup:** Keep old keys for decryption during transition

**Rotation Process:**
```
1. Generate new encryption key
2. Load all wallets from LocalFileOASIS
3. Decrypt with old key
4. Encrypt with new key
5. Save wallets back
6. Update OASIS_DNA.json with new key
7. Archive old key securely
8. After transition period, destroy old key
```

---

### 6. **Monitoring and Auditing**

**Required Monitoring:**
- ✅ Failed decryption attempts (potential key compromise)
- ✅ Unusual access patterns
- ✅ Wallet file access/modification
- ✅ Encryption key access

**Audit Logging:**
- Who accessed private keys
- When they were accessed
- What operations were performed
- Source IP address
- Success/failure status

---

### 7. **Backup and Recovery**

**Current Issue:** No documented backup strategy.

**Production Requirements:**

#### Backup Strategy:
1. **Encrypted Backups:** All wallet backups must be encrypted
2. **Offline Storage:** Keep backups in secure, offline location
3. **Access Control:** Limit backup access to authorized personnel
4. **Testing:** Regularly test backup restoration

#### Recovery Process:
1. Restore wallet files from encrypted backup
2. Ensure encryption key is available
3. Verify wallet integrity
4. Test wallet access

---

### 8. **Compliance Considerations**

**Regulations to Consider:**
- **GDPR:** Personal data protection (if wallets contain user data)
- **PCI DSS:** If handling payment information
- **SOC 2:** Security controls and monitoring
- **HIPAA:** If in healthcare context

**Requirements:**
- Data encryption at rest and in transit
- Access controls and audit trails
- Secure key management
- Incident response procedures

---

## 🚀 Migration Path from Testing to Production

### Step 1: Generate Production Key
```bash
# Generate secure key
PROD_KEY=$(openssl rand -base64 32)
echo "Production Key: $PROD_KEY"
# Store securely (see Key Management section)
```

### Step 2: Update Configuration
1. Update `OASIS_DNA.json` with production key (or use environment variable)
2. Restart API server
3. Test with a test wallet first

### Step 3: Re-encrypt Existing Wallets
```csharp
// Pseudo-code for re-encryption
foreach (var wallet in allWallets)
{
    var decrypted = Rijndael.Decrypt(wallet.PrivateKey, OLD_KEY);
    wallet.PrivateKey = Rijndael.Encrypt(decrypted, NEW_KEY);
    SaveWallet(wallet);
}
```

### Step 4: Verify
1. Test wallet loading
2. Test transaction signing
3. Verify encryption is working
4. Check audit logs

### Step 5: Secure Storage
1. Move wallet files to secure directory
2. Set proper file permissions
3. Configure backup strategy
4. Set up monitoring

---

## 📋 Security Checklist

### Before Going to Production:

- [ ] Generate cryptographically secure encryption key
- [ ] Store key in secure key management system (not in code/config)
- [ ] Set proper file permissions on wallet storage directory
- [ ] Enable HTTPS/TLS for all API endpoints
- [ ] Implement access control and authentication
- [ ] Set up audit logging
- [ ] Configure backup strategy
- [ ] Test key rotation process
- [ ] Document incident response procedures
- [ ] Review compliance requirements
- [ ] Security audit/penetration testing
- [ ] Remove test keys from all environments

---

## 🔍 Current Testing Configuration Details

### File Location:
```
~/Library/Application Support/OASIS/LocalFileOASIS/wallets_{avatarId}.json
```

### File Format:
```json
{
  "SolanaOASIS": [
    {
      "walletId": "ec42a998-9c87-4920-9b08-c82c4662ae03",
      "walletAddress": "JDJMNKRj8RyaPyf1k8eN2fhwz5U2aaJrbfk6dg233tT",
      "publicKey": "JDJMNKRj8RyaPyf1k8eN2fhwz5U2aaJrbfk6dg233tT",
      "privateKey": "<encrypted-with-test-key>",
      "providerType": 3,
      "isDefaultWallet": false
    }
  ]
}
```

### Encryption:
- **Method:** Rijndael256 (AES-256)
- **Key:** `TEST_KEY_FOR_DEVELOPMENT_ONLY_DO_NOT_USE_IN_PRODUCTION_256BIT`
- **Key Size:** 256 bits

---

## ⚠️ Warnings

1. **NEVER commit encryption keys to version control**
2. **NEVER use test keys in production**
3. **NEVER log private keys (even encrypted)**
4. **NEVER share encryption keys via insecure channels**
5. **ALWAYS use HTTPS for API endpoints**
6. **ALWAYS set proper file permissions**
7. **ALWAYS encrypt backups**

---

## 📚 Additional Resources

- [OWASP Key Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Key_Management_Cheat_Sheet.html)
- [NIST Key Management Guidelines](https://csrc.nist.gov/publications/detail/sp/800-57-part-1/rev-5/final)
- [AWS Key Management Best Practices](https://docs.aws.amazon.com/kms/latest/developerguide/best-practices.html)

---

## 🔄 Updates

- **2026-01-09:** Initial document created for testing configuration
- **TODO:** Update with production key management implementation
- **TODO:** Add key rotation procedures
- **TODO:** Add compliance checklist
