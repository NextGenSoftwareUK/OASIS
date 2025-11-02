# x402 Integration for OASIS Web4

Complete x402 payment distribution integration for revenue-generating NFTs on Solana.

---

## 📁 Folder Structure

```
x402/
├── README.md (this file)
│
├── docs/
│   ├── X402_README.md                    # Complete technical guide
│   ├── DOCUMENTATION_INDEX.md            # Documentation navigation
│   ├── X402_ONE_PAGER.md                 # Executive summary
│   ├── X402_HACKATHON_PITCH_DECK.html    # Presentation slides
│   └── README.md                         # Integration examples
│
├── backend-service/
│   ├── src/                              # x402 service source code
│   ├── package.json                      # NPM package config
│   └── README.md                         # Service documentation
│
└── frontend-components/
    └── (Components live in nft-mint-frontend/src/components/x402/)
```

---

## 🚀 Quick Start

### **1. Start Backend Service**

```bash
cd x402/backend-service
npm install
npm start
```

Service runs on: http://localhost:4000

### **2. Start Frontend**

```bash
cd nft-mint-frontend
npm run dev
```

Open: http://localhost:3002 or http://localhost:3000

### **3. View Dashboard**

Visit: http://localhost:3002/x402-dashboard

---

## 📚 Documentation

**Start here:** `docs/X402_README.md`

**For hackathon:** `docs/X402_HACKATHON_PITCH_DECK.html`

**Quick reference:** `docs/X402_ONE_PAGER.md`

---

## 🎯 What's Included

### **Backend Service:**
- Standalone NPM package `@oasis-web4/x402-service`
- Payment distribution engine
- Pluggable storage (file/MongoDB/custom)
- 5 API endpoints

### **Frontend Components:**
- x402 configuration wizard (in nft-mint-frontend)
- Manual distribution panel
- Treasury activity feed
- Revenue dashboard

### **Documentation:**
- Technical guides
- Deployment instructions
- Hackathon pitch materials
- Integration examples

---

## 🏆 Ready for Hackathon

Everything is built, tested, and documented. Ready to demo!

See `docs/X402_README.md` for complete guide.

