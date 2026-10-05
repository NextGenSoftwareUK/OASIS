# MetaBricks x402 Quick Integration Plan

## 📊 **Current Status Analysis**

### ✅ **What's Already Done:**

1. **Backend Code Ready** (lines 665-673 in `server.js`):
   ```javascript
   x402Config: {
     enabled: true,
     nftCollection: 'METABRICKS',
     revenueSource: 'AssetRail Smart Contract Generator API',
     revenueModel: 'equal',
     distributionPercentage: 90,
     totalNFTs: 432
   }
   ```
   - x402 config is added to Solana NFT minting
   - x402 routes already mounted at `/api/x402`

2. **NPM Package Ready**:
   - Location: `/x402/backend-service/`
   - Package: `@oasis-web4/x402-service`
   - Standalone service with all distribution logic
   - Pluggable storage (File/MongoDB/Custom)

### ❌ **What's NOT Done Yet:**

1. **x402 Service Not Deployed** - Need to deploy standalone service
2. **No Distribution Mechanism** - MetaBricks routes exist but service isn't running
3. **Frontend Has No x402 UI** - No revenue dashboard or stats
4. **Metadata Not Updated** - Pinata NFTs don't mention x402 revenue

---

## 🚀 **Quick Integration Steps** (2-3 Hours)

### **Step 1: Use x402 Service as Library** ⭐ FASTEST

Instead of deploying a separate service, integrate x402 directly into the existing MetaBricks backend.

**Changes to `/meta-bricks-main/backend/server.js`:**

```javascript
// Add at top of file (around line 14)
const { X402Service, FileStorage } = require('@oasis-web4/x402-service');

// Initialize x402 service (around line 125, before app.use(cors()))
const x402Storage = new FileStorage('./data/x402');
const x402Service = new X402Service({
  storage: x402Storage,
  solanaRpcUrl: process.env.SOLANA_RPC_URL || 'https://api.mainnet-beta.solana.com',
  useMockData: false
});

// Make x402 available to app
app.locals.x402Service = x402Service;
app.locals.x402Distributor = x402Service.distributor;

console.log('✅ x402 service initialized inline');

// Replace line 900-904 with:
app.use('/api/x402', x402Service.router);

// Add MetaBricks-specific routes
const metabricksRoutes = require('./routes/metabricks-routes');
app.use('/api/metabricks', metabricksRoutes);
```

**Create `/meta-bricks-main/backend/routes/metabricks-routes.js`:**

Copy from: `/x402/backend-service/src/routes/metabricks-routes.js`

**Add to `package.json`:**

```json
"dependencies": {
  "@oasis-web4/x402-service": "file:../../x402/backend-service"
}
```

**Install:**

```bash
cd /meta-bricks-main/backend
npm install
```

---

### **Step 2: Test Locally** (5 minutes)

```bash
cd /meta-bricks-main/backend
npm start
```

**Test endpoints:**

```bash
# Health check
curl http://localhost:3001/health

# x402 stats
curl http://localhost:3001/api/metabricks/stats

# x402 holders
curl http://localhost:3001/api/metabricks/holders

# Test webhook
curl -X POST http://localhost:3001/api/metabricks/sc-gen-webhook \
  -H "Content-Type: application/json" \
  -d '{"signature":"TEST","amount":0.6,"distributionPercentage":90,"nftCollection":"METABRICKS","metadata":{}}'
```

---

### **Step 3: Deploy to Heroku** (10 minutes)

```bash
cd /meta-bricks-main/backend

# Commit changes
git add .
git commit -m "Integrate x402 service directly into MetaBricks backend"

# Push to Heroku
git push heroku main

# Check logs
heroku logs --tail -a metabricks-backend
```

**Test production endpoints:**

```bash
curl https://metabricks-backend.herokuapp.com/api/metabricks/stats
```

---

### **Step 4: Update Frontend** (30 minutes)

**Option A: Minimal Integration** (Recommended First)

Add simple stats display to existing MetaBricks frontend:

**Create `/meta-bricks-main/src/app/services/x402.service.ts`:**

```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class X402Service {
  private apiUrl = 'https://metabricks-backend.herokuapp.com/api/metabricks';

  constructor(private http: HttpClient) {}

  getStats(): Observable<any> {
    return this.http.get(`${this.apiUrl}/stats`);
  }

  getHolders(): Observable<any> {
    return this.http.get(`${this.apiUrl}/holders`);
  }
}
```

**Add stats widget to main page** (`src/app/components/...`):

```html
<div class="x402-stats-widget" *ngIf="x402Stats">
  <h3>💰 Revenue Sharing Active</h3>
  <p>Total Distributed: {{ x402Stats.totalDistributed }} SOL</p>
  <p>Distributions: {{ x402Stats.distributionCount }}</p>
  <p>Per Brick: {{ x402Stats.averagePerDistribution / 432 | number:'1.4-4' }} SOL</p>
  <a href="/revenue-dashboard">View Details →</a>
</div>
```

**Deploy frontend:**

```bash
cd /meta-bricks-main
ng build --configuration production
surge ./dist metabricks.xyz
```

---

### **Step 5: Connect SC-Gen** (15 minutes)

**In Smart Contract Generator's backend:**

```csharp
// Add to SC-Gen payment processing
public async Task ProcessPayment(Payment payment)
{
    // ... existing payment logic ...
    
    // Trigger x402 distribution
    var webhookUrl = "https://metabricks-backend.herokuapp.com/api/metabricks/sc-gen-webhook";
    var webhookPayload = new {
        signature = payment.SolanaTransactionHash,
        amount = payment.Amount, // in SOL
        operation = "purchase-credits",
        distributionPercentage = 90,
        nftCollection = "METABRICKS",
        metadata = new {
            packName = payment.PackName,
            credits = payment.Credits,
            customerId = payment.CustomerId
        }
    };
    
    await _httpClient.PostAsJsonAsync(webhookUrl, webhookPayload);
}
```

---

## 📦 **File Changes Summary**

### **Backend Changes:**

```
/meta-bricks-main/backend/
├── server.js                           # Add x402 initialization (10 lines)
├── package.json                        # Add x402 dependency (1 line)
├── routes/
│   └── metabricks-routes.js           # NEW FILE (copy from x402)
└── data/
    └── x402/                          # NEW FOLDER (created automatically)
        ├── x402-config.json           # Storage files
        └── x402-distributions.json    # Storage files
```

### **Frontend Changes:**

```
/meta-bricks-main/src/app/
├── services/
│   └── x402.service.ts                # NEW FILE (30 lines)
└── components/
    └── x402-stats-widget/             # NEW COMPONENT
        ├── x402-stats-widget.component.ts
        ├── x402-stats-widget.component.html
        └── x402-stats-widget.component.scss
```

---

## ⚡ **Alternative: Even Faster** (1 Hour)

### **Just Use Mock Data for Now:**

1. **Skip x402 NPM package integration**
2. **Use existing `/api/x402` routes** (already in backend)
3. **Return mock stats for demo:**

```javascript
// In server.js, replace x402 routes with:
app.get('/api/metabricks/stats', (req, res) => {
  res.json({
    success: true,
    stats: {
      collectionSymbol: 'MBRICK',
      totalBricks: 432,
      totalDistributed: 0, // Will update when real distributions happen
      distributionCount: 0,
      averagePerDistribution: 0,
      recentDistributions: []
    }
  });
});

app.get('/api/metabricks/holders', (req, res) => {
  res.json({
    success: true,
    totalHolders: 432,
    holders: [] // Will populate from OASIS API later
  });
});
```

4. **Add simple UI widget** showing "x402 Revenue Sharing: Coming Soon"
5. **Deploy in 30 minutes**

---

## 🎯 **Recommended Approach**

**For Fastest Production Integration:**

1. **Use Step 1 approach** (integrate as library) - ✅ No new Heroku app needed
2. **Start with mock data** for holders - ✅ Can update later when querying OASIS
3. **Add minimal frontend widget** - ✅ Just shows "Revenue Sharing Enabled"
4. **Configure SC-Gen webhook** - ✅ Start collecting distributions

**Timeline:**
- Backend integration: 30 min
- Local testing: 15 min
- Heroku deployment: 15 min
- Frontend widget: 30 min
- SC-Gen webhook: 15 min
- **Total: ~2 hours**

---

## 📝 **Deployment Checklist**

- [ ] Add x402 service to `package.json`
- [ ] Run `npm install` in backend
- [ ] Update `server.js` with x402 initialization
- [ ] Copy `metabricks-routes.js` to backend routes
- [ ] Test locally (all 4 endpoints)
- [ ] Commit changes
- [ ] Push to Heroku
- [ ] Verify production endpoints
- [ ] Add frontend widget
- [ ] Build and deploy frontend to Surge
- [ ] Configure SC-Gen webhook URL
- [ ] Test end-to-end with SC-Gen payment

---

## 🚨 **Important Notes**

1. **Storage**: Using file storage for now (fine for Heroku hobby dyno)
2. **Holder Query**: Currently returns mock 432 holders (can integrate OASIS API later)
3. **Distributions**: Ready to receive webhooks from SC-Gen immediately
4. **Mainnet**: Make sure to set `SOLANA_RPC_URL=https://api.mainnet-beta.solana.com`

---

## 🆘 **Need Help?**

If any issues arise:
1. Check Heroku logs: `heroku logs --tail`
2. Test endpoints with curl
3. Verify package installation: `npm ls @oasis-web4/x402-service`
4. Check file permissions for `./data/x402` directory

---

**Ready to start? Let's begin with Step 1!** 🚀

