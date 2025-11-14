# MetaBricks x402 Production Deployment Plan

## 🎯 **Objective**

Integrate x402 revenue distribution into the live MetaBricks deployment at metabricks.xyz

---

## 📊 **Current Production Architecture**

```
┌─────────────────────────────────────────────────────────────────┐
│                    CURRENT PRODUCTION SETUP                     │
└─────────────────────────────────────────────────────────────────┘

    Frontend                Backend              OASIS API
    ┌─────────┐            ┌─────────┐          ┌─────────┐
    │ Surge   │───HTTP────>│ Heroku  │───API───>│ oasis   │
    │ .xyz    │            │         │          │ web4.one│
    └─────────┘            └─────────┘          └─────────┘
                                │
                                │
                                ▼
                           ┌─────────┐
                           │ Pinata  │
                           │ IPFS    │
                           └─────────┘

    Status: ✅ Live, no x402 integration
```

---

## 🚀 **Target Architecture with x402**

```
┌─────────────────────────────────────────────────────────────────┐
│                  TARGET PRODUCTION SETUP                        │
└─────────────────────────────────────────────────────────────────┘

    Frontend              Backend              x402 Service
    ┌─────────┐          ┌─────────┐          ┌──────────────┐
    │ Surge   │──HTTP──>│ Heroku  │──HTTP──>│ Heroku       │
    │ .xyz    │          │ Updated │          │ (new app)    │
    └─────────┘          └─────────┘          │ Port 4000    │
         │                    │               └──────────────┘
         │                    │                      │
         │                    ▼                      │
         │               ┌─────────┐                 │
         │               │ Pinata  │                 │
         │               │ IPFS    │                 │
         │               └─────────┘                 │
         │                                           │
         └──────────> Dashboard <───────────────────┘
                      (stats API)

                            │
                            ▼
                      ┌──────────┐
                      │ OASIS    │
                      │ web4.one │
                      └──────────┘
                            │
                            ▼
                      ┌──────────┐
                      │ Solana   │
                      │ Mainnet  │
                      └──────────┘


    Smart Contract Generator
    ┌────────────────┐
    │ SC-Gen API     │──webhook──> x402 Service
    └────────────────┘              /api/metabricks/sc-gen-webhook
```

---

## 📋 **Deployment Checklist**

### **Phase 1: Pre-Deployment Preparation** (Day 1)

- [ ] **1.1 Environment Audit**
  - [ ] Access Heroku dashboard for MetaBricks backend
  - [ ] Verify Surge deployment credentials
  - [ ] Confirm OASIS API endpoints (oasisweb4.one)
  - [ ] Check Pinata storage configuration
  - [ ] Document current environment variables

- [ ] **1.2 Code Preparation**
  - [ ] Review all x402 integration code
  - [ ] Create production environment configs
  - [ ] Set up error logging (Sentry/LogRocket)
  - [ ] Prepare rollback scripts

- [ ] **1.3 Testing Environment**
  - [ ] Deploy x402 service to Heroku staging
  - [ ] Test webhook endpoints on staging
  - [ ] Verify Solana devnet transactions
  - [ ] Load test with 432 concurrent recipients

---

### **Phase 2: Deploy x402 Service** (Day 2-3)

- [ ] **2.1 Create New Heroku App for x402**
  ```bash
  # Create new Heroku app
  heroku create metabricks-x402-service
  
  # Add to git remote
  cd /path/to/x402/backend-service
  git init
  git remote add heroku https://git.heroku.com/metabricks-x402-service.git
  ```

- [ ] **2.2 Configure Environment Variables**
  ```bash
  heroku config:set \
    NODE_ENV=production \
    SOLANA_RPC_URL=https://api.mainnet-beta.solana.com \
    X402_PORT=4000 \
    X402_USE_MOCK_DATA=false \
    X402_STORAGE=mongodb \
    MONGODB_URL=<your-mongodb-atlas-url> \
    MONGODB_DATABASE=metabricks_x402 \
    X402_WEBHOOK_SECRET=<generate-strong-secret> \
    OASIS_API_BASE_URL=https://oasisweb4.one/api \
    CORS_ORIGIN=https://metabricks.xyz
  ```

- [ ] **2.3 Set Up MongoDB (Production Storage)**
  - [ ] Create MongoDB Atlas cluster (free tier is fine for now)
  - [ ] Create database: `metabricks_x402`
  - [ ] Create collections: `distributions`, `configs`
  - [ ] Set up indexes for performance
  - [ ] Add connection string to Heroku config

- [ ] **2.4 Deploy x402 Service**
  ```bash
  cd /path/to/x402/backend-service
  
  # Create Procfile
  echo "web: node src/server.js" > Procfile
  
  # Commit and push
  git add .
  git commit -m "Production x402 service"
  git push heroku main
  
  # Check logs
  heroku logs --tail -a metabricks-x402-service
  ```

- [ ] **2.5 Verify x402 Service**
  ```bash
  # Test health endpoint
  curl https://metabricks-x402-service.herokuapp.com/health
  
  # Test stats endpoint
  curl https://metabricks-x402-service.herokuapp.com/api/metabricks/stats
  
  # Test holders endpoint
  curl https://metabricks-x402-service.herokuapp.com/api/metabricks/holders
  ```

- [ ] **2.6 Configure Custom Domain (Optional)**
  ```bash
  heroku domains:add x402.metabricks.xyz -a metabricks-x402-service
  # Add CNAME record in DNS: x402 -> metabricks-x402-service.herokuapp.com
  ```

---

### **Phase 3: Update MetaBricks Backend** (Day 3-4)

- [ ] **3.1 Update Heroku Backend Code**
  
  Location: `/Volumes/Storage 2/OASIS_CLEAN/meta-bricks-main/backend/server.js`
  
  Changes needed:
  ```javascript
  // Add x402 configuration to OASIS NFT minting request
  x402Config: {
    enabled: true,
    nftCollection: 'METABRICKS',
    revenueSource: 'AssetRail Smart Contract Generator API',
    revenueModel: 'equal',
    distributionPercentage: 90,
    totalNFTs: 432,
    webhookUrl: process.env.X402_WEBHOOK_URL || 
                'https://metabricks-x402-service.herokuapp.com/api/metabricks/sc-gen-webhook'
  }
  ```

- [ ] **3.2 Add Environment Variables to Heroku Backend**
  ```bash
  heroku config:set \
    X402_ENABLED=true \
    X402_WEBHOOK_URL=https://metabricks-x402-service.herokuapp.com/api/metabricks/sc-gen-webhook \
    X402_SERVICE_URL=https://metabricks-x402-service.herokuapp.com \
    -a metabricks-backend
  ```

- [ ] **3.3 Update NFT Metadata Template**
  
  Update Pinata metadata to include x402 information:
  ```json
  {
    "name": "MetaBrick #42",
    "description": "MetaBrick NFT: Brick #42. Earn passive income from AssetRail Smart Contract Generator API revenue.",
    "image": "https://gateway.pinata.cloud/ipfs/...",
    "attributes": [
      { "trait_type": "Brick Number", "value": "42" },
      { "trait_type": "Revenue Sharing", "value": "Enabled" },
      { "trait_type": "Distribution Model", "value": "Equal Split" },
      { "trait_type": "Annual Yield", "value": "~37.5% ROI" }
    ],
    "x402": {
      "enabled": true,
      "revenueSource": "AssetRail Smart Contract Generator",
      "distributionPercentage": 90
    }
  }
  ```

- [ ] **3.4 Deploy Updated Backend**
  ```bash
  cd /path/to/meta-bricks-main/backend
  git add .
  git commit -m "Add x402 integration to MetaBricks minting"
  git push heroku main
  
  # Verify deployment
  heroku logs --tail -a metabricks-backend
  ```

---

### **Phase 4: Update MetaBricks Frontend** (Day 4-5)

- [ ] **4.1 Add x402 Revenue Dashboard**
  
  Files to add to frontend:
  - `/src/components/x402-revenue-dashboard.tsx` (or .jsx if not using TypeScript)
  - `/src/services/x402-api.js` - API client for x402 service
  - `/src/pages/revenue.html` - New page for revenue stats

- [ ] **4.2 Update Main Navigation**
  
  Add link to revenue dashboard:
  ```html
  <nav>
    <a href="/">Home</a>
    <a href="/bricks">Bricks</a>
    <a href="/revenue">Revenue</a>  <!-- NEW -->
    <a href="/about">About</a>
  </nav>
  ```

- [ ] **4.3 Add Revenue Stats Widget**
  
  Display on main page:
  ```html
  <div class="revenue-stats">
    <h3>💰 Revenue Sharing Active</h3>
    <p>Total Distributed: <span id="total-distributed">0</span> SOL</p>
    <p>Your Next Payment: <span id="next-payment">TBD</span></p>
    <a href="/revenue">View Details →</a>
  </div>
  ```

- [ ] **4.4 Create Revenue API Client**
  
  `/src/services/x402-api.js`:
  ```javascript
  const X402_API_BASE = 'https://metabricks-x402-service.herokuapp.com';
  
  export async function getMetaBricksStats() {
    const response = await fetch(`${X402_API_BASE}/api/metabricks/stats`);
    return response.json();
  }
  
  export async function getHolders() {
    const response = await fetch(`${X402_API_BASE}/api/metabricks/holders`);
    return response.json();
  }
  ```

- [ ] **4.5 Update NFT Card Display**
  
  Add x402 badge to NFT cards:
  ```html
  <div class="nft-card">
    <img src="..." alt="MetaBrick #42" />
    <h3>MetaBrick #42</h3>
    <span class="x402-badge">💰 Revenue Sharing</span>  <!-- NEW -->
    <p>Earns from SC-Gen API</p>
  </div>
  ```

- [ ] **4.6 Build and Deploy Frontend**
  ```bash
  cd /path/to/metabricks-frontend
  
  # Build
  npm run build  # or your build command
  
  # Deploy to Surge
  surge ./dist metabricks.xyz
  
  # Verify deployment
  curl https://metabricks.xyz/revenue
  ```

---

### **Phase 5: Configure Smart Contract Generator** (Day 5)

- [ ] **5.1 Update SC-Gen Configuration**
  
  In SC-Gen's `appsettings.json` or environment:
  ```json
  {
    "X402": {
      "Enabled": true,
      "WebhookUrl": "https://metabricks-x402-service.herokuapp.com/api/metabricks/sc-gen-webhook",
      "WebhookSecret": "<same-secret-as-x402-service>",
      "DistributionPercentage": 90,
      "NFTCollection": "METABRICKS"
    },
    "Payment": {
      "Provider": "Solana",
      "TreasuryWallet": "<your-solana-wallet-address>",
      "NotifyOnPayment": true
    }
  }
  ```

- [ ] **5.2 Add Webhook Trigger to SC-Gen**
  
  Update payment processing code:
  ```csharp
  // After successful payment
  if (X402Config.Enabled) {
      await SendX402Webhook(new {
          signature = solanaTransaction.Signature,
          amount = paymentAmount,
          operation = "purchase-credits",
          distributionPercentage = 90,
          nftCollection = "METABRICKS",
          metadata = new {
              packName = creditPack.Name,
              credits = creditPack.Amount,
              customerId = customer.Id
          }
      });
  }
  ```

- [ ] **5.3 Test SC-Gen Webhook Integration**
  ```bash
  # Simulate a purchase in SC-Gen
  # Watch x402 service logs
  heroku logs --tail -a metabricks-x402-service
  
  # Verify distribution was recorded
  curl https://metabricks-x402-service.herokuapp.com/api/metabricks/stats
  ```

---

### **Phase 6: Testing & Verification** (Day 6-7)

- [ ] **6.1 End-to-End Testing**
  - [ ] Make test purchase in SC-Gen (devnet first)
  - [ ] Verify webhook received by x402 service
  - [ ] Check distribution calculation (90/10 split)
  - [ ] Verify 432 recipients queried correctly
  - [ ] Check distribution recorded in MongoDB
  - [ ] Confirm stats API returns correct data
  - [ ] Test frontend dashboard displays data

- [ ] **6.2 Load Testing**
  ```bash
  # Test with multiple concurrent payments
  for i in {1..10}; do
    curl -X POST https://metabricks-x402-service.herokuapp.com/api/metabricks/sc-gen-webhook \
      -H "Content-Type: application/json" \
      -d "{\"signature\":\"TEST_$i\",\"amount\":0.6,\"distributionPercentage\":90,\"nftCollection\":\"METABRICKS\"}" &
  done
  ```

- [ ] **6.3 Monitoring Setup**
  - [ ] Set up Heroku metrics monitoring
  - [ ] Configure error alerts (email/Slack)
  - [ ] Set up uptime monitoring (UptimeRobot)
  - [ ] Create dashboard for key metrics

- [ ] **6.4 Security Audit**
  - [ ] Verify webhook signature validation
  - [ ] Check CORS configuration
  - [ ] Review API rate limiting
  - [ ] Audit environment variable security
  - [ ] Test with invalid payloads

---

### **Phase 7: Go Live** (Day 7)

- [ ] **7.1 Pre-Launch Checklist**
  - [ ] All tests passing ✅
  - [ ] Monitoring active ✅
  - [ ] Rollback plan ready ✅
  - [ ] Team briefed ✅
  - [ ] Documentation complete ✅

- [ ] **7.2 Switch to Mainnet**
  ```bash
  # Update x402 service
  heroku config:set \
    SOLANA_RPC_URL=https://api.mainnet-beta.solana.com \
    X402_USE_MOCK_DATA=false \
    -a metabricks-x402-service
  
  # Restart service
  heroku restart -a metabricks-x402-service
  ```

- [ ] **7.3 Update SC-Gen to Production**
  - [ ] Switch SC-Gen to mainnet Solana
  - [ ] Update webhook URL to production x402 service
  - [ ] Enable real payments

- [ ] **7.4 Announce Launch**
  - [ ] Update metabricks.xyz homepage
  - [ ] Send email to existing brick holders
  - [ ] Post on social media
  - [ ] Update documentation

---

## 🔧 **Environment Variables Summary**

### **x402 Service (Heroku: metabricks-x402-service)**
```bash
NODE_ENV=production
SOLANA_RPC_URL=https://api.mainnet-beta.solana.com
X402_PORT=4000
X402_USE_MOCK_DATA=false
X402_STORAGE=mongodb
MONGODB_URL=mongodb+srv://...
MONGODB_DATABASE=metabricks_x402
X402_WEBHOOK_SECRET=<strong-secret>
OASIS_API_BASE_URL=https://oasisweb4.one/api
CORS_ORIGIN=https://metabricks.xyz
```

### **MetaBricks Backend (Heroku: metabricks-backend)**
```bash
X402_ENABLED=true
X402_WEBHOOK_URL=https://metabricks-x402-service.herokuapp.com/api/metabricks/sc-gen-webhook
X402_SERVICE_URL=https://metabricks-x402-service.herokuapp.com
OASIS_API_URL=https://oasisweb4.one/api
PINATA_API_KEY=<your-key>
PINATA_SECRET_KEY=<your-secret>
```

### **Smart Contract Generator**
```bash
X402_ENABLED=true
X402_WEBHOOK_URL=https://metabricks-x402-service.herokuapp.com/api/metabricks/sc-gen-webhook
X402_WEBHOOK_SECRET=<same-as-x402-service>
SOLANA_RPC_URL=https://api.mainnet-beta.solana.com
TREASURY_WALLET=<your-wallet-address>
```

---

## 📊 **Success Metrics**

After deployment, track:

- ✅ **Uptime:** 99.9% target for x402 service
- ✅ **Webhook Success Rate:** >99% of SC-Gen payments processed
- ✅ **Distribution Latency:** <30 seconds from payment to distribution
- ✅ **Error Rate:** <0.1% of transactions
- ✅ **Revenue Distributed:** Track total SOL distributed per week/month

---

## 🚨 **Rollback Plan**

If issues arise:

### **Immediate Rollback:**
```bash
# Stop x402 service
heroku ps:scale web=0 -a metabricks-x402-service

# Revert MetaBricks backend
cd /path/to/meta-bricks-main/backend
git revert HEAD
git push heroku main

# Revert frontend
surge ./previous-build metabricks.xyz
```

### **Disable x402 Without Full Rollback:**
```bash
# Just disable x402 features
heroku config:set X402_ENABLED=false -a metabricks-backend
heroku config:set X402_ENABLED=false -a metabricks-x402-service
```

---

## 💰 **Cost Estimate**

### **Monthly Hosting Costs:**

| Service | Provider | Plan | Cost |
|---------|----------|------|------|
| x402 Backend | Heroku | Eco Dyno | $5 |
| MongoDB Storage | Atlas | Free Tier | $0 |
| Frontend | Surge | Free | $0 |
| MetaBricks Backend | Heroku | Existing | $0 (no change) |
| **TOTAL** | | | **~$5/month** |

### **Optional Upgrades:**
- Heroku Standard Dyno ($25/mo) - for 99.95% uptime SLA
- MongoDB Atlas M2 ($9/mo) - for better performance
- Custom domain SSL ($20/yr) - x402.metabricks.xyz

---

## 📞 **Support & Monitoring**

### **Monitoring Tools:**
- Heroku Metrics Dashboard
- MongoDB Atlas Monitoring
- UptimeRobot (for endpoint health)
- LogDNA/Papertrail (for log aggregation)

### **Alert Channels:**
- Email: on critical errors
- Slack: #metabricks-alerts
- PagerDuty: for 24/7 coverage (optional)

---

## ✅ **Next Steps**

**Ready to start?**

1. Review this plan
2. Confirm access to all services (Heroku, Surge, MongoDB)
3. Set up staging environment
4. Begin Phase 1 checklist

**Estimated Timeline:** 7-10 days to full production deployment

**Let's do this! 🚀**

