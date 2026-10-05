# OASIS API Subdomain Configuration Guide

This guide explains how to deploy OASIS API with subdomain-based network configuration.

## Overview

The OASIS API now supports automatic network detection based on the subdomain:
- **Mainnet**: `https://oasisweb4.one` - Production Solana mainnet
- **Devnet**: `https://devnet.oasisweb4.one` - Testing Solana devnet

## How It Works

### 1. Network Detection
The `OASISMiddleware` detects the network from the request hostname:
- `devnet.oasisweb4.one` → Loads `OASIS_DNA_devnet.json`
- `oasisweb4.one` → Loads `OASIS_DNA.json` (mainnet)

### 2. Configuration Files
- **`OASIS_DNA.json`** - Mainnet configuration (Solana mainnet RPC)
- **`OASIS_DNA_devnet.json`** - Devnet configuration (Solana devnet RPC)

### 3. Middleware Logic
```csharp
// In OASISMiddleware constructor
if (!OASISBootLoader.OASISBootLoader.IsOASISBooted)
{
    string network = DetermineNetworkFromRequest();
    
    if (network == "devnet")
    {
        OASISBootLoader.OASISBootLoader.BootOASIS("OASIS_DNA_devnet.json");
    }
    else
    {
        OASISBootLoader.OASISBootLoader.BootOASIS("OASIS_DNA.json");
    }
}
```

## Configuration Differences

| Setting | Mainnet | Devnet |
|---------|---------|--------|
| Solana RPC | `https://api.mainnet-beta.solana.com` | `https://api.devnet.solana.com` |
| Network | Production | Testing |
| SOL Required | Real SOL | Test SOL (free) |

## Deployment Steps

### 1. Deploy with Subdomain Configuration
```bash
chmod +x deploy_subdomain_oasis.sh
./deploy_subdomain_oasis.sh
```

### 2. Configure DNS
Add a DNS record for the devnet subdomain:
```
devnet.oasisweb4.one → [ALB DNS Name]
```

### 3. Update ALB (if needed)
Ensure the Application Load Balancer can handle both domains:
- `oasisweb4.one`
- `devnet.oasisweb4.one`

## Backend Configuration

The backend is configured to use the devnet subdomain for testing:

```javascript
const OASIS_API_URL = process.env.OASIS_API_URL || 'http://devnet.oasisweb4.one';
```

To switch to mainnet for production:
```bash
export OASIS_API_URL=http://oasisweb4.one
```

## Testing

### Devnet Testing
```bash
# Test devnet endpoint
curl -X POST "https://devnet.oasisweb4.one/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"email": "anorak@oasisweb4.com", "password": "Uppermall1!"}'
```

### Mainnet Production
```bash
# Test mainnet endpoint
curl -X POST "https://oasisweb4.one/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"email": "anorak@oasisweb4.com", "password": "Uppermall1!"}'
```

## Environment Variables

You can also override the network detection with environment variables:

```bash
# Force devnet
export OASIS_NETWORK=devnet

# Force mainnet
export OASIS_NETWORK=mainnet
```

## Benefits

1. **Single Deployment**: One API serves both networks
2. **Automatic Detection**: Network determined by subdomain
3. **Easy Testing**: Devnet always available
4. **Production Ready**: Mainnet always available
5. **Clean URLs**: `devnet.oasisweb4.one` vs `oasisweb4.one`
6. **No Switching**: Both networks always available

## Current Limitations

- **Static Configuration**: Network is determined at startup, not per request
- **Single Instance**: One ECS service handles both networks
- **Future Enhancement**: Could implement dynamic network switching per request

## Troubleshooting

### Check Network Detection
Look for these log messages:
```
🌐 Detected network: devnet
🔧 Booting OASIS with devnet configuration
```

### Check Configuration Loading
```
🔧 Overridden SolanaOASIS ConnectionString from environment variable
✅ SolanaOASIS configuration override completed. Using wallet: Be51B1n3m1MCtZYvH8JEX3LnZZwoREyH4rYoyhMrkxJs
```

### Check Provider Registration
```
🔧 Registering SolanaOASIS provider...
✅ SolanaOASIS provider registered successfully
✅ SolanaOASIS provider activated successfully
```

## Next Steps

1. Deploy the subdomain configuration
2. Configure DNS for `devnet.oasisweb4.one`
3. Test both endpoints
4. Update frontend to use appropriate endpoint
5. Test NFT minting on devnet
6. Switch to mainnet when ready for production

