# Quick Authentication Guide

## 🚀 Easy Authentication

### Option 1: Use the Auth Helper (Recommended)

```bash
cd /Volumes/Storage/OASIS_CLEAN
./auth-oasis.sh
```

This will:
- ✅ Prompt for username/password
- ✅ Authenticate with the API
- ✅ Save token to `~/.oasis_token`
- ✅ Export token to `OASIS_TOKEN` environment variable
- ✅ Test the token to verify it works

### Option 2: Load Saved Token

If you've already authenticated:

```bash
source ./load-oasis-token.sh
# or
export OASIS_TOKEN=$(cat ~/.oasis_token)
```

### Option 3: Manual Authentication

```bash
# Authenticate
curl -k -X POST "https://localhost:5004/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username":"your_username","password":"your_password"}'

# Extract token from response and export
export OASIS_TOKEN="your_token_here"
```

## 📝 Usage Examples

### After Authentication

Once authenticated, you can run any test script:

```bash
# Test wallet addresses
cd zypherpunk-wallet-ui
./test-wallet-addresses.sh

# Test address derivation
cd ..
./test-address-derivation.sh
```

### Token Persistence

The token is saved to `~/.oasis_token` and will be automatically loaded by test scripts.

To use in a new terminal:
```bash
source /Volumes/Storage/OASIS_CLEAN/load-oasis-token.sh
```

## 🔍 Troubleshooting

### "Unauthorized" Error

1. **Check if token is set:**
   ```bash
   echo $OASIS_TOKEN
   ```

2. **Re-authenticate:**
   ```bash
   ./auth-oasis.sh
   ```

3. **Check token format:**
   - Should be a JWT token (starts with `eyJ`)
   - Should have 3 parts separated by dots

4. **Verify API is running:**
   ```bash
   curl -k https://localhost:5004/api/avatar/health
   ```

### Token Expired

Tokens expire after a certain time. Just re-authenticate:
```bash
./auth-oasis.sh
```

## 💡 Tips

- The token is saved to `~/.oasis_token` for convenience
- Test scripts will automatically try to load saved tokens
- You can manually export the token: `export OASIS_TOKEN="..."`
- Token file is chmod 600 for security

