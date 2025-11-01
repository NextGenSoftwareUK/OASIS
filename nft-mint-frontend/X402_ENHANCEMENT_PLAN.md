# x402 Enhancement Plan for NFT Minting Frontend

## 🎯 Overview

Enhance your existing Next.js NFT minting frontend to support **x402 revenue-generating NFTs** - enabling automatic payment distribution to all NFT holders.

---

## 📊 Current State Analysis

**Existing Features:**
- ✅ 4-step wizard (Solana Config, Auth, Assets, Mint)
- ✅ OASIS API integration via `useOasisApi` hook
- ✅ Solana wallet connection (@solana/wallet-adapter)
- ✅ Multiple mint profiles (Metaplex, Editions, Compressed)
- ✅ IPFS/Pinata integration
- ✅ Provider registration/activation flow

**Tech Stack:**
- Next.js 15.5.4 with Turbopack
- React 19
- TailwindCSS 4
- TypeScript
- Solana Web3.js

---

## 🚀 Proposed x402 Enhancements

### **Enhancement 1: Add x402 Configuration Step**
Add a new wizard step between "Assets" and "Mint" for x402 configuration.

### **Enhancement 2: Revenue Model Selector**
Allow users to choose how revenue is distributed (equal/weighted/creator-split).

### **Enhancement 3: Payment Endpoint Configuration**
Let users specify their x402 payment webhook URL.

### **Enhancement 4: Distribution Dashboard**
Show real-time distribution statistics for minted NFTs.

### **Enhancement 5: Holder Analytics**
Display current NFT holders and payment history.

---

## 📁 Files to Create/Modify

### **New Files:**
```
src/components/x402/
├── x402-config-panel.tsx          # x402 configuration step
├── revenue-model-selector.tsx     # Revenue distribution model picker
├── distribution-dashboard.tsx     # View distribution stats
└── holder-analytics.tsx           # View holders and payments

src/hooks/
└── use-x402-distribution.ts       # Hook for x402 API calls

src/types/
└── x402.ts                        # x402 TypeScript types
```

### **Files to Modify:**
```
src/app/(routes)/page-content.tsx  # Add x402 step to wizard
src/components/mint/mint-review-panel.tsx  # Add x402 payload fields
src/hooks/use-oasis-api.ts         # Add x402 endpoints
```

---

## 💻 Implementation Details

### **Step 1: Create x402 Types**

```typescript
// src/types/x402.ts
export type RevenueModel = 'equal' | 'weighted' | 'creator-split';

export interface X402Config {
  enabled: boolean;
  paymentEndpoint: string;
  revenueModel: RevenueModel;
  metadata?: {
    contentType?: string;
    revenueSharePercentage?: number;
    distributionFrequency?: 'realtime' | 'daily' | 'weekly' | 'monthly';
  };
}

export interface X402Stats {
  nftMintAddress: string;
  totalDistributed: number;
  distributionCount: number;
  holderCount: number;
  averagePerDistribution: number;
}

export interface X402Distribution {
  timestamp: number;
  amount: number;
  recipients: number;
  transactionSignature: string;
}
```

### **Step 2: Add x402 Hook**

```typescript
// src/hooks/use-x402-distribution.ts
"use client";

import { useCallback } from "react";
import { useOasisApi } from "./use-oasis-api";

export function useX402Distribution(baseUrl: string, token?: string) {
  const { call } = useOasisApi({ baseUrl, token });

  const getStats = useCallback(
    async (nftMintAddress: string) => {
      return await call(`/api/x402/stats/${nftMintAddress}`, {
        method: "GET",
      });
    },
    [call]
  );

  const testDistribution = useCallback(
    async (nftMintAddress: string, amount: number) => {
      return await call("/api/x402/distribute-test", {
        method: "POST",
        body: JSON.stringify({ nftMintAddress, amount }),
      });
    },
    [call]
  );

  return { getStats, testDistribution };
}
```

### **Step 3: Create x402 Config Panel**

See next file for full component implementation...

---

## 🎨 UI/UX Design

### **Color Scheme:**
- Use existing accent color `var(--accent)` (#22d3ee - Solana green)
- Add x402 purple accent: `#9945ff`
- Revenue indicators: Green for positive, amber for pending

### **Layout:**
- Insert x402 step between "Assets" (step 3) and "Mint" (step 4)
- New step: "Revenue Sharing" (step 4)
- Mint becomes step 5

### **User Flow:**
```
1. Solana Config → 
2. Auth & Providers → 
3. Assets & Metadata → 
4. ✨ x402 Revenue Sharing [NEW] → 
5. Review & Mint
```

---

## 🔄 Integration with Existing Code

### **Modify page-content.tsx:**

```typescript
// Add new step to WIZARD_STEPS array
const WIZARD_STEPS = [
  {
    id: "solana-config",
    title: "Solana Configuration",
    description: "Select the minting profile you want to use.",
  },
  {
    id: "auth",
    title: "Authenticate & Providers",
    description: "Login and activate providers.",
  },
  {
    id: "assets",
    title: "Assets & Metadata",
    description: "Upload artwork and metadata.",
  },
  // NEW STEP
  {
    id: "x402-config",
    title: "x402 Revenue Sharing",
    description: "Enable automatic payment distribution to NFT holders.",
  },
  {
    id: "mint",
    title: "Review & Mint",
    description: "Generate payload and mint NFT.",
  },
];

// Add state for x402 config
const [x402Config, setX402Config] = useState<X402Config>({
  enabled: false,
  paymentEndpoint: "",
  revenueModel: "equal",
});

// Add render logic
{activeStep === "x402-config" ? (
  <X402ConfigPanel
    config={x402Config}
    onChange={setX402Config}
  />
) : null}
```

### **Modify mint-review-panel.tsx:**

```typescript
// Add x402Config to payload
const payload = useMemo(() => {
  const basePayload = {
    // ... existing fields ...
    
    // Add x402 configuration
    ...(x402Config.enabled && {
      x402Config: {
        enabled: true,
        paymentEndpoint: x402Config.paymentEndpoint,
        revenueModel: x402Config.revenueModel,
        metadata: x402Config.metadata,
      }
    }),
  };

  return basePayload;
}, [assetDraft, x402Config, /* other deps */]);

// Update mint endpoint
const response = await call("/api/nft/mint-nft-x402", {
  method: "POST",
  body: JSON.stringify(payload),
});
```

---

## 🎯 Key Features

### **1. Revenue Model Selector**
- **Equal Split:** All holders receive same amount
- **Weighted:** Distribution based on token holdings
- **Creator Split:** Fixed % to creator, rest to holders

### **2. Payment Endpoint Configuration**
- Input field for webhook URL
- Auto-generate endpoint option
- Validation and testing

### **3. Distribution Preview**
- Show estimated revenue per holder
- Calculate based on holder count
- Display payment frequency

### **4. Post-Mint Dashboard**
- View distribution history
- See current holders
- Track total revenue distributed
- Export transaction history

---

## 📱 Responsive Design

All new components should follow your existing responsive patterns:
```tsx
className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
```

---

## 🧪 Testing Strategy

### **Unit Tests:**
- x402 config validation
- Revenue model calculations
- API endpoint mocking

### **Integration Tests:**
- Full wizard flow with x402
- Mint with x402 enabled
- Distribution simulation

### **E2E Tests:**
- User completes full flow
- Receives minted NFT
- Views distribution stats

---

## 📊 Success Metrics

After implementation, track:
- % of NFTs minted with x402 enabled
- Average revenue distributed per NFT
- User satisfaction (survey)
- Distribution success rate

---

## 🔒 Security Considerations

1. **Webhook Validation:** Validate x402 signatures
2. **Rate Limiting:** Prevent distribution spam
3. **Access Control:** Only NFT owners can trigger distributions
4. **Audit Trail:** Log all distribution events

---

## 🚀 Rollout Plan

### **Phase 1: Core Integration (Week 1)**
- Add x402 wizard step
- Implement revenue model selector
- Update mint payload

### **Phase 2: Dashboard (Week 2)**
- Build distribution dashboard
- Add holder analytics
- Create history view

### **Phase 3: Polish (Week 3)**
- Add animations
- Improve error handling
- Write documentation

### **Phase 4: Launch (Week 4)**
- Beta test with 10 users
- Fix bugs
- Public release

---

## 💡 Quick Wins

Start with these for immediate impact:

1. **Add toggle switch** in mint-review-panel: "Enable x402 Revenue Sharing"
2. **Simple input field** for payment endpoint
3. **Dropdown** for revenue model selection
4. **Test button** to simulate distribution

---

## 📚 Documentation Needed

- [ ] User guide: "How to create revenue-generating NFTs"
- [ ] Developer docs: x402 API integration
- [ ] Video tutorial: Full walkthrough
- [ ] FAQ: Common questions about revenue sharing

---

## 🔗 Related Files

Refer to these existing patterns:
- `src/components/wizard/chain-step.tsx` - Card selection UI
- `src/components/auth/provider-toggle-panel.tsx` - Toggle pattern
- `src/components/mint/mint-review-panel.tsx` - Form inputs

---

## Next Steps

1. Review this plan
2. I'll create the actual components
3. Test integration
4. Deploy to staging
5. Launch! 🚀

