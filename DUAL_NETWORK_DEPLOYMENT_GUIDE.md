# OASIS API Dual Network Deployment Guide

This guide explains how to deploy and use separate devnet and mainnet OASIS API instances.

## Overview

We now have separate OASIS API instances for:
- **Devnet**: `http://oasisweb4-devnet.one` - For testing and development
- **Mainnet**: `http://oasisweb4-mainnet.one` - For production

## Configuration Files

### Devnet Configuration
- `OASIS_DNA_devnet.json` - Devnet OASIS DNA configuration
- `oasis-api-task-definition-devnet.json` - ECS task definition for devnet
- `deploy_devnet_oasis.sh` - Deployment script for devnet

### Mainnet Configuration
- `OASIS_DNA_mainnet.json` - Mainnet OASIS DNA configuration  
- `oasis-api-task-definition-mainnet.json` - ECS task definition for mainnet
- `deploy_mainnet_oasis.sh` - Deployment script for mainnet

## Key Differences

| Configuration | Devnet | Mainnet |
|---------------|--------|---------|
| Solana RPC | `https://api.devnet.solana.com` | `https://api.mainnet-beta.solana.com` |
| ECS Service | `oasis-api-service-devnet` | `oasis-api-service-mainnet` |
| Log Group | `/ecs/oasis-api-devnet` | `/ecs/oasis-api-mainnet` |
| Task Family | `oasis-api-task-devnet` | `oasis-api-task-mainnet` |
| Docker Tag | `devnet` | `mainnet` |

## Deployment Steps

### 1. Deploy Devnet Instance
```bash
chmod +x deploy_devnet_oasis.sh
./deploy_devnet_oasis.sh
```

### 2. Deploy Mainnet Instance
```bash
chmod +x deploy_mainnet_oasis.sh
./deploy_mainnet_oasis.sh
```

## Backend Configuration

The backend is currently configured to use devnet for testing:

```javascript
const OASIS_API_URL = process.env.OASIS_API_URL || 'http://oasisweb4-devnet.one';
```

To switch to mainnet for production, set the environment variable:
```bash
export OASIS_API_URL=http://oasisweb4-mainnet.one
```

## Testing

### Devnet Testing
- Use `http://oasisweb4-devnet.one` for all API calls
- Solana transactions will be on devnet
- No real SOL required for testing

### Mainnet Production
- Use `http://oasisweb4-mainnet.one` for all API calls
- Solana transactions will be on mainnet
- Requires real SOL for transaction fees

## Benefits

1. **No More Switching**: Both networks are always available
2. **Easy Testing**: Devnet is always ready for testing
3. **Production Ready**: Mainnet is always ready for production
4. **Isolated Environments**: Changes to one don't affect the other
5. **Separate Logging**: Each network has its own CloudWatch logs

## Wallet Configuration

Both networks use the same wallet credentials:
- **Public Key**: `Be51B1n3m1MCtZYvH8JEX3LnZZwoREyH4rYoyhMrkxJs`
- **Private Key**: `kNln1+y3r9Xa1HbiakTDUmdpyzImmnpEs/+et8D6Jr2eE+KoOZJtHXdOOoNyP1NRDcfa44LE4y6llK9JaMpCEA==`
- **Mnemonic**: `adapt afford abandon above age adult ahead accident aim advice agree accuse`

## Next Steps

1. Deploy both instances
2. Configure Application Load Balancer (ALB) to route traffic
3. Update DNS records for `oasisweb4-devnet.one` and `oasisweb4-mainnet.one`
4. Test NFT minting on devnet
5. Switch to mainnet when ready for production

## Troubleshooting

### Check Service Status
```bash
# Devnet
aws ecs describe-services --cluster oasis-api-cluster --services oasis-api-service-devnet --region us-east-1

# Mainnet  
aws ecs describe-services --cluster oasis-api-cluster --services oasis-api-service-mainnet --region us-east-1
```

### Check Logs
```bash
# Devnet logs
aws logs describe-log-streams --log-group-name "/ecs/oasis-api-devnet" --region us-east-1

# Mainnet logs
aws logs describe-log-streams --log-group-name "/ecs/oasis-api-mainnet" --region us-east-1
```

