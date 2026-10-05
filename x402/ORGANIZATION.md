# x402 Project Organization

All x402-related files are now organized in the `/x402/` folder.

---

## 📁 Structure

```
x402/
│
├── README.md                          # Main overview
├── ORGANIZATION.md                    # This file
│
├── docs/                              # All documentation
│   ├── X402_README.md                 # Complete technical guide
│   ├── DOCUMENTATION_INDEX.md         # Doc navigation
│   ├── X402_ONE_PAGER.md              # Executive summary
│   ├── X402_HACKATHON_PITCH_DECK.html # Presentation
│   ├── README.md                      # Integration guide
│   ├── X402PaymentDistributor.ts      # TypeScript version
│   ├── x402-oasis-middleware.ts       # Middleware
│   ├── example-usage.ts               # Code examples
│   ├── demo-frontend.html             # Demo UI
│   └── solana-program/                # Smart contract
│
├── backend-service/                   # Standalone NPM package
│   ├── package.json
│   ├── README.md
│   ├── .gitignore
│   ├── .env.example
│   ├── bin/                           # CLI executable
│   ├── src/                           # Source code
│   │   ├── X402Service.js
│   │   ├── server.js
│   │   ├── distributor/
│   │   ├── storage/
│   │   └── routes/
│   └── tests/
│
└── frontend-components/               # Reference only
    └── (Actual components in nft-mint-frontend/src/components/x402/)
```

---

## 🚀 Quick Start

**Backend:**
```bash
cd x402/backend-service
npm install
npm start
```

**Frontend:**
```bash
cd nft-mint-frontend
npm run dev
```

**Dashboard:**
http://localhost:3002/x402-dashboard

---

## 📍 Frontend Components Location

Frontend components remain in their proper location:
```
nft-mint-frontend/src/
├── components/x402/
│   ├── x402-config-panel.tsx
│   ├── manual-distribution-panel.tsx
│   ├── distribution-dashboard.tsx
│   └── treasury-activity-feed.tsx
│
├── app/(routes)/x402-dashboard/
│   └── page.tsx
│
├── hooks/
│   └── use-x402-distribution.ts
│
└── types/
    └── x402.ts
```

This keeps frontend code with the frontend where it belongs.

---

## 📚 Documentation

All docs are in `x402/docs/`

**Main guide:** `docs/X402_README.md`

---

**Everything x402 is now in the `/x402/` folder!** ✅

