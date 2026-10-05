# @oasis-web4/x402-service

> **Automatic revenue distribution for NFTs on Solana using the x402 payment protocol**

OASIS Web4's x402 service enables NFT holders to automatically receive payments when revenue is generated. Turn passive NFTs into revenue-generating assets.

---

## 🚀 **Quick Start**

### **Installation**

```bash
npm install @oasis-web4/x402-service
```

### **Usage as Standalone Service**

```bash
# Start the service
npx x402-service start

# Or with custom config
X402_PORT=4000 npx x402-service start
```

### **Usage as NPM Package**

```javascript
const { X402Service, FileStorage } = require('@oasis-web4/x402-service');

// Initialize with file storage
const storage = new FileStorage('./data');
const service = new X402Service({
  storage,
  solanaRpcUrl: 'https://api.devnet.solana.com'
});

// Start the service
await service.start({ port: 4000 });

// Or use programmatically
const result = await service.distributePayment({
  nftMintAddress: 'ABC123...',
  amount: 10 * 1e9, // 10 SOL in lamports
  metadata: { source: 'streaming' }
});

console.log(`Distributed to ${result.recipients} holders`);
```

---

## 📦 **What's Included**

- **Payment Distribution Engine** - Queries NFT holders and distributes payments
- **Express API** - Ready-to-use REST API endpoints
- **Pluggable Storage** - File, MongoDB, PostgreSQL, or custom adapters
- **Solana Integration** - Direct blockchain queries
- **Webhook Handler** - Receives x402 payment notifications
- **Statistics & History** - Track all distributions

---

## 🔌 **API Endpoints**

### **POST /api/x402/register**
Register an NFT for x402 revenue distribution

```bash
curl -X POST http://localhost:4000/api/x402/register \
  -H "Content-Type: application/json" \
  -d '{
    "nftMintAddress": "ABC123...",
    "paymentEndpoint": "https://api.example.com/x402/webhook",
    "revenueModel": "equal",
    "treasuryWallet": "WALLET123..."
  }'
```

### **POST /api/x402/webhook**
Receive x402 payment notifications

```bash
curl -X POST http://localhost:4000/api/x402/webhook \
  -H "Content-Type: application/json" \
  -H "X-X402-Signature: hmac_signature" \
  -d '{
    "amount": 10000000000,
    "metadata": {
      "nftMintAddress": "ABC123..."
    }
  }'
```

### **GET /api/x402/stats/:nftMintAddress**
Get distribution statistics

```bash
curl http://localhost:4000/api/x402/stats/ABC123...
```

### **POST /api/x402/distribute**
Manually trigger distribution (testing/demo)

```bash
curl -X POST http://localhost:4000/api/x402/distribute \
  -H "Content-Type: application/json" \
  -d '{
    "nftMintAddress": "ABC123...",
    "amount": 10.0
  }'
```

---

## 🗄️ **Storage Adapters**

### **File Storage (Default)**

```javascript
const { FileStorage } = require('@oasis-web4/x402-service');

const storage = new FileStorage('./data');
// Stores to ./data/x402-config.json and ./data/x402-distributions.json
```

### **MongoDB Storage**

```javascript
const { MongoStorage } = require('@oasis-web4/x402-service');

const storage = new MongoStorage({
  url: 'mongodb://localhost:27017',
  database: 'oasis_x402'
});
```

### **PostgreSQL Storage**

```javascript
const { PostgresStorage } = require('@oasis-web4/x402-service');

const storage = new PostgresStorage({
  host: 'localhost',
  database: 'oasis_x402',
  user: 'oasis',
  password: 'secret'
});
```

### **Custom Storage Adapter**

```javascript
class CustomStorage {
  async storeConfig(nftMintAddress, config) {
    // Your storage logic
  }
  
  async getConfig(nftMintAddress) {
    // Your retrieval logic
  }
  
  async recordDistribution(distribution) {
    // Your recording logic
  }
  
  async getDistributions(nftMintAddress, limit) {
    // Your query logic
  }
}

const storage = new CustomStorage();
const service = new X402Service({ storage });
```

---

## ⚙️ **Configuration**

### **Environment Variables**

```bash
# Server
X402_PORT=4000
X402_HOST=0.0.0.0

# Solana
SOLANA_RPC_URL=https://api.devnet.solana.com
X402_USE_MOCK_DATA=false

# Security
X402_WEBHOOK_SECRET=your_secret_here

# Storage (if using MongoDB)
MONGODB_URL=mongodb://localhost:27017
MONGODB_DATABASE=oasis_x402

# Storage (if using PostgreSQL)
POSTGRES_HOST=localhost
POSTGRES_DATABASE=oasis_x402
POSTGRES_USER=oasis
POSTGRES_PASSWORD=secret
```

### **Programmatic Configuration**

```javascript
const service = new X402Service({
  // Solana
  solanaRpcUrl: 'https://api.mainnet-beta.solana.com',
  useMockData: false,
  
  // Storage
  storage: new FileStorage('./data'),
  
  // Security
  webhookSecret: 'your_secret',
  
  // Custom options
  platformFeePercent: 2.5,
  defaultRevenueModel: 'equal'
});
```

---

## 🧪 **Testing**

```bash
# Run tests
npm test

# Run tests in watch mode
npm run test:watch

# Test distribution
curl -X POST http://localhost:4000/api/x402/distribute \
  -H "Content-Type: application/json" \
  -d '{"nftMintAddress":"TEST","amount":1.0}'
```

---

## 🔐 **Security**

### **Webhook Signature Verification**

The service validates x402 webhooks using HMAC SHA256:

```javascript
const crypto = require('crypto');

const signature = crypto
  .createHmac('sha256', webhookSecret)
  .update(JSON.stringify(payload))
  .digest('hex');

// Include in webhook request header:
// X-X402-Signature: <signature>
```

### **Treasury Wallet Security**

- User controls their own wallet (decentralized)
- Pre-authorization via Solana PDA (Program Derived Address)
- No platform custody of funds
- Revocable permissions

---

## 🏗️ **Architecture**

```
┌─────────────────────────────────────────────────────┐
│                  Revenue Sources                     │
│  (Spotify, YouTube, Rental Systems, APIs, etc.)     │
└─────────────────┬───────────────────────────────────┘
                  │ POST payment notification
                  ↓
┌─────────────────────────────────────────────────────┐
│              x402 Payment Protocol                   │
│                                                      │
│  • Validates payment                                │
│  • Sends webhook to registered endpoint             │
└─────────────────┬───────────────────────────────────┘
                  │ POST /api/x402/webhook
                  ↓
┌─────────────────────────────────────────────────────┐
│           OASIS x402 Service (This Package)         │
│                                                      │
│  1. Receives webhook notification                   │
│  2. Fetches NFT configuration                       │
│  3. Queries Solana for current holders              │
│  4. Calculates distribution amounts                 │
│  5. Creates & signs transaction                     │
│  6. Submits to Solana blockchain                    │
│  7. Records distribution history                    │
└─────────────────┬───────────────────────────────────┘
                  │ SPL token transfers
                  ↓
┌─────────────────────────────────────────────────────┐
│              Solana Blockchain                       │
│                                                      │
│  • Executes multi-recipient transaction             │
│  • All NFT holders receive payment                  │
│  • ~30 seconds, ultra-low fees                      │
└─────────────────────────────────────────────────────┘
```

---

## 📊 **Revenue Models**

### **Equal Split**
All holders receive equal share
```javascript
{ revenueModel: 'equal' }
```

### **Weighted**
Distribution proportional to token balance
```javascript
{ revenueModel: 'weighted' }
```

### **Creator Split**
Creator gets fixed %, remainder split among holders
```javascript
{ 
  revenueModel: 'creator-split',
  creatorPercentage: 50 
}
```

---

## 🔄 **Data Storage**

### **What Gets Stored:**

**NFT Configuration:**
- NFT mint address
- x402 payment endpoint
- Revenue model
- Treasury wallet
- Registration timestamp

**Distribution History:**
- Distribution ID
- NFT mint address
- Total amount
- Recipients count
- Amount per holder
- Transaction signature
- Timestamp

### **Where It's Stored:**

Depends on your storage adapter:
- **File:** `./data/x402-config.json` and `./data/x402-distributions.json`
- **MongoDB:** Collections `x402_configs` and `x402_distributions`
- **PostgreSQL:** Tables `x402_configs` and `x402_distributions`
- **Custom:** Your implementation

### **Data Persistence:**

- ✅ Survives service restarts
- ✅ Can be backed up
- ✅ Can be migrated between storage types
- ✅ Queryable for analytics

---

## 🚀 **Production Deployment**

### **As Standalone Service**

```bash
# Install globally
npm install -g @oasis-web4/x402-service

# Create systemd service
sudo nano /etc/systemd/system/x402.service
```

```ini
[Unit]
Description=OASIS x402 Service
After=network.target

[Service]
Type=simple
User=oasis
WorkingDirectory=/opt/x402-service
Environment="NODE_ENV=production"
Environment="X402_PORT=4000"
Environment="SOLANA_RPC_URL=https://api.mainnet-beta.solana.com"
ExecStart=/usr/bin/x402-service start
Restart=always

[Install]
WantedBy=multi-user.target
```

```bash
# Enable and start
sudo systemctl enable x402
sudo systemctl start x402
```

### **As Part of Your Application**

```javascript
// server.js
const express = require('express');
const { X402Service, MongoStorage } = require('@oasis-web4/x402-service');

const app = express();

// Initialize x402
const storage = new MongoStorage({ 
  url: process.env.MONGODB_URL 
});
const x402 = new X402Service({ storage });

// Mount x402 routes
app.use('/api/x402', x402.router);

// Your other routes
app.get('/api/health', (req, res) => {
  res.json({ status: 'ok' });
});

app.listen(3000);
```

---

## 📈 **Monitoring**

### **Health Check**

```bash
curl http://localhost:4000/health
```

### **Metrics**

```bash
curl http://localhost:4000/api/x402/metrics
```

**Returns:**
- Total NFTs registered
- Total distributions processed
- Total amount distributed
- Success rate
- Average processing time

---

## 🤝 **Integration Examples**

### **With OASIS C# Backend**

```csharp
// In your NFT minting controller
public async Task<IActionResult> MintNFTWithX402(MintRequest request)
{
    // Mint NFT via OASIS
    var nft = await _oasisService.MintNFT(request);
    
    // Register with x402 service
    var x402Response = await _httpClient.PostAsync(
        "http://localhost:4000/api/x402/register",
        new StringContent(JsonSerializer.Serialize(new {
            nftMintAddress = nft.MintAddress,
            paymentEndpoint = request.X402Config.PaymentEndpoint,
            revenueModel = request.X402Config.RevenueModel,
            treasuryWallet = request.X402Config.TreasuryWallet
        }))
    );
    
    return Ok(new { nft, x402 = await x402Response.Content.ReadAsStringAsync() });
}
```

### **With Express.js Backend**

```javascript
app.post('/api/nft/mint-x402', async (req, res) => {
  // Mint NFT
  const nft = await oasisApi.mintNFT(req.body);
  
  // Register with x402
  const x402Result = await x402Service.register({
    nftMintAddress: nft.mintAddress,
    paymentEndpoint: req.body.x402Config.paymentEndpoint,
    revenueModel: req.body.x402Config.revenueModel,
    treasuryWallet: req.body.x402Config.treasuryWallet
  });
  
  res.json({ nft, x402: x402Result });
});
```

---

## 🛠️ **Development**

```bash
# Clone repository
git clone https://github.com/oasis-web4/x402-service.git
cd x402-service

# Install dependencies
npm install

# Run in development mode
npm run dev

# Run tests
npm test

# Build for production
npm run build
```

---

## 📝 **License**

MIT © OASIS Web4

---

## 🙏 **Credits**

Built with:
- [Solana Web3.js](https://github.com/solana-labs/solana-web3.js)
- [SPL Token](https://spl.solana.com/token)
- [Express.js](https://expressjs.com/)

---

## 📞 **Support**

- Documentation: https://docs.oasisweb4.one/x402
- Issues: https://github.com/oasis-web4/x402-service/issues
- Discord: https://discord.gg/oasis-web4

---

**Turn your NFTs into revenue-generating assets with OASIS Web4 x402!** 🚀

