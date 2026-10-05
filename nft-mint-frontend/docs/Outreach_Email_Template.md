# OASIS NFT Minting: Email Outreach Templates

Quick-copy templates for different audiences.

---

## Template 1: Investor Outreach (Cold Email)

**Subject:** Simplifying Solana NFT minting: 45 steps → 12 steps

Hi [Name],

I'm reaching out because [mutual connection/reason for contact]. We've built something that solves a major bottleneck in the Solana NFT ecosystem.

**The Problem:** Minting NFTs on Solana currently requires 45+ steps, 4-8 hours of setup, and developer expertise. This prevents ~500K creators from participating.

**Our Solution:** Browser-based NFT minting that reduces the process to 12 steps and 10 minutes — zero coding required.

**Key Metrics:**
- 73% reduction in steps vs. Metaplex CLI
- Live on Solana devnet today
- Multi-chain architecture ready for Arbitrum, Polygon, Base
- Dual interface: simple UI for creators + API for developers

**Traction:**
- Production-ready frontend deployed
- First beta users onboarding this month
- $2.6M ARR potential in Year 2 (conservative model)

We're raising a $500K-$1M seed round to accelerate multi-chain expansion and user acquisition.

**Ask:** Would you be open to a 20-minute call to see a demo and discuss the opportunity?

[Insert Calendly link]

Happy to send our full deck and financials if there's interest.

Best,  
[Your Name]

P.S. — You can try the live devnet version here: http://devnet.oasisweb4.one

---

## Template 2: Partnership Outreach (Ecosystem/Protocols)

**Subject:** Partnership opportunity: Multi-chain NFT infrastructure

Hi [Name],

I'm [Your Name] from the OASIS team. We've built browser-based NFT minting infrastructure that we think could be valuable to [Company/Protocol].

**What we've built:**
- The simplest NFT minting experience in crypto (12 steps, 10 minutes, zero code)
- Provider abstraction supporting multiple chains (Solana live, Arbitrum/Polygon/Base ready)
- Full API access for developer integrations
- White-label capability for branded experiences

**Why this matters to [Company]:**
[Customize based on recipient — examples below]

**For Metaplex:** 
- We're driving adoption of Metaplex standards (editions, collections, compression) by making them accessible to non-developers
- Early adopter of new features (Bubblegum compressed NFTs)
- Potential official "simplified minting" partner

**For Phantom/Solflare:**
- Pre-integrated wallet adapter driving mints through your wallet
- Co-marketing opportunity (we send users, you provide infrastructure)
- White-label version for "Mint with Phantom" campaigns

**For Solana Foundation:**
- Reduces barrier to ecosystem entry (500K+ potential new users)
- Drives transaction volume on Solana
- Multi-chain strategy brings users from other ecosystems to Solana

**For Web3 Agencies:**
- White-label our frontend for client NFT campaigns
- API integration for custom projects
- Revenue share model available

**Potential Collaboration:**
- Ecosystem grant for user acquisition
- Technical integration and co-development
- Joint marketing and content
- Revenue share or licensing arrangement

**Ask:** 30-minute call to explore how we might work together?

[Insert Calendly link]

Looking forward to connecting,  
[Your Name]

[Demo link: http://devnet.oasisweb4.one]  
[Documentation: http://oasisweb4.one/swagger]

---

## Template 3: Enterprise/Brand Outreach

**Subject:** NFT minting for [Brand] — 10 minutes vs. 4 hours

Hi [Name],

I noticed [Brand] has been [exploring NFTs / launched an NFT campaign / mentioned Web3 in earnings]. We've built infrastructure that could significantly simplify your NFT operations.

**The Challenge:**
Most brands spend $50K-$200K and 3-6 months building custom NFT minting infrastructure, then face ongoing maintenance costs of $20K-$50K/year.

**Our Solution:**
Browser-based NFT minting platform with:
- ✅ White-label capability (your branding, not ours)
- ✅ Multi-chain support (Solana, Arbitrum, Polygon, Base)
- ✅ 10-minute setup vs. 3-month development cycle
- ✅ 99% cost reduction vs. custom development
- ✅ No blockchain expertise required on your team

**Use Cases for [Brand]:**
- Limited edition product drops (connect physical to digital)
- Loyalty and rewards programs (NFT-gated perks)
- Event ticketing (verifiable, transferable, fraud-resistant)
- Customer engagement campaigns (collectible series)
- Influencer collaborations (co-branded NFTs)

**Pilot Program:**
We're offering 3 enterprise clients discounted pilot programs:
- Free setup and customization
- 1,000 free mints on mainnet
- Dedicated support and white-glove onboarding
- Custom feature development based on your needs

**Investment:** $5K-$15K vs. $100K+ for custom solution

**Timeline:** 2 weeks to launch vs. 3-6 months

**Ask:** 20-minute demo to show the platform and discuss your specific needs?

[Insert Calendly link]

Best regards,  
[Your Name]

P.S. — Here's our live demo if you want to explore first: http://devnet.oasisweb4.one

---

## Template 4: Developer/Technical Outreach

**Subject:** Multi-chain NFT minting API — integrate in 10 lines of code

Hey [Name],

Fellow builder here. We've abstracted away the complexity of NFT minting across multiple chains.

**Problem you might relate to:**
- Metaplex integration takes days and requires Rust/Anchor knowledge
- Each new chain means rewriting minting logic
- Managing IPFS uploads, metadata formatting, and retry logic is tedious
- Users expect simple UIs but you're focused on core product

**Our Solution:**
Provider abstraction system with:
- **One API endpoint** for NFT minting across 5 chains
- **Automatic IPFS uploads** via Pinata integration
- **Metaplex-compliant metadata** generation
- **Built-in retry logic** and error handling
- **Pre-built UI** you can white-label or use as reference

**Integration Example:**
```typescript
const response = await fetch('https://oasisweb4.one/api/nft/mint-nft', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    Title: "My NFT",
    OnChainProvider: { value: 3, name: "SolanaOASIS" },
    // ... rest of payload
  })
});
```

**vs. 200+ lines of Metaplex/Anchor boilerplate**

**What you get:**
- Full Swagger documentation: http://oasisweb4.one/swagger
- TypeScript SDK (React hooks included)
- Free devnet access for testing
- Production-ready UI you can fork or embed
- Multi-chain support (swap `SolanaOASIS` → `ArbitrumOASIS`)

**Pricing:**
- Free tier: 100 API calls/month
- Startup: $99/month (10K calls)
- Growth: $499/month (100K calls)
- Enterprise: Custom (unlimited, SLA, dedicated support)

**Ask:** Want early access to the API docs and $500 in free credits?

Reply with "interested" and I'll send credentials.

Cheers,  
[Your Name]

[Demo: http://devnet.oasisweb4.one]  
[Docs: http://oasisweb4.one/swagger]

---

## Template 5: Follow-Up After Demo

**Subject:** Thanks for the call — next steps for [Name/Company]

Hi [Name],

Great connecting today! As discussed, here's a summary and proposed next steps:

**What We Discussed:**
- [Bullet point summary of their needs/interests]
- [Specific features or use cases they mentioned]
- [Any concerns or questions raised]

**Relevant Materials:**
- **Full Value Proposition:** [Attach OASIS_NFT_Minting_Value_Proposition.md]
- **Executive Summary:** [Attach Executive_Summary.md]
- **Live Demo:** http://devnet.oasisweb4.one
- **API Docs:** http://oasisweb4.one/swagger
- **Multi-Chain Roadmap:** [Attach multi-chain-plan.md if technical audience]

**Proposed Next Steps:**

**Option 1 — Investor Track:**
1. Share deck and financial model (this week)
2. Technical deep-dive with CTO (next week)
3. Diligence and term sheet discussion (within 30 days)

**Option 2 — Partnership Track:**
1. Draft partnership proposal (this week)
2. Technical integration call with both teams (next week)
3. Pilot program or collaboration agreement (within 45 days)

**Option 3 — Enterprise/Client Track:**
1. Custom demo with your branding (this week)
2. Pilot program proposal and SOW (next week)
3. Kickoff and onboarding (within 2 weeks)

**My Availability:**
[Insert 2-3 specific time slots or Calendly link]

Looking forward to working together!

Best,  
[Your Name]

---

## Template 6: Warm Introduction Request

**Subject:** Quick intro to [Target Name]?

Hi [Connector Name],

Hope you're doing well! Quick ask:

I'm working on OASIS, a browser-based NFT minting platform that reduces the Solana minting process from 45 steps to 12 steps. We're live on devnet and expanding to multi-chain.

I noticed you know [Target Name] at [Company]. We're [raising a seed round / looking for partners / exploring enterprise pilots], and [Company] could be a great fit because [specific reason].

**Would you be comfortable making a warm intro?**

**What I'd ask for:**
- 20-minute exploratory call
- Show demo and discuss potential collaboration
- No pressure — just exploring mutual fit

**One-liner for the intro (feel free to adapt):**
> "[Your Name] built a tool that simplifies NFT minting for Solana (and soon Arbitrum, Polygon, Base). It reduces a 4-hour, 45-step process to 10 minutes and 12 steps. Thought you two should connect given [Company's] work in [relevant area]."

**Optional:**
- Live demo: http://devnet.oasisweb4.one
- One-pager: [Attach Executive_Summary.md]

Totally understand if the timing isn't right — no worries either way!

Thanks,  
[Your Name]

---

## Social Media Post Templates

### Twitter/X Thread Starter:

```
We just reduced Solana NFT minting from 45 steps to 12 steps.

Here's how traditional Metaplex minting works (and why it's broken) 🧵
```

### LinkedIn Post:

```
After months of development, we're launching OASIS NFT Minting on Solana devnet.

The problem: Minting NFTs requires developer expertise, 45+ manual steps, and 4-8 hours of setup.

Our solution: Browser-based minting in 12 steps and 10 minutes — zero coding required.

Built with:
✅ Next.js 15 + TypeScript
✅ Solana Web3.js + Metaplex standards
✅ Provider abstraction for multi-chain expansion
✅ Automatic Pinata IPFS integration
✅ Full API access for developers

Live demo: http://devnet.oasisweb4.one

This is just the beginning. Arbitrum, Polygon, and Base integrations coming Q3 2025.

Would love feedback from the Web3 community — drop a comment or DM if you want to be an early beta tester!

#Solana #NFTs #Web3 #BuildInPublic
```

---

## Call-to-Action Options

**For Investors:**
- "Would you be open to a 20-minute call to see a demo?"
- "I'd love to send our deck — what email should I use?"
- "Are you taking meetings for seed-stage infrastructure companies?"

**For Partners:**
- "Could we schedule 30 minutes to explore collaboration?"
- "Would [Company] be interested in a pilot program?"
- "Any chance you're attending [conference]? Would love to connect there."

**For Customers:**
- "Want to be one of our first 10 enterprise clients?"
- "Can I show you a 5-minute demo tailored to [use case]?"
- "Would a free pilot program be valuable for [upcoming project]?"

**For Developers:**
- "Reply 'interested' and I'll send early API access."
- "Want $500 in free credits to test the API?"
- "Looking for beta testers — interested in being one of the first?"

---

## Tips for Personalization

**Do:**
- ✅ Reference specific work they've done (tweet, article, project)
- ✅ Mention mutual connections or shared interests
- ✅ Customize use cases to their industry/company
- ✅ Keep it under 200 words for cold outreach
- ✅ Include one clear call-to-action
- ✅ Make it easy to say yes (Calendly link, one-click reply)

**Don't:**
- ❌ Send generic mass emails
- ❌ Use jargon they won't understand
- ❌ Make multiple asks in one email
- ❌ Write more than 3 paragraphs for cold outreach
- ❌ Attach large files without asking
- ❌ Follow up more than 2 times if no response

---

**Remember:** The goal is to start a conversation, not close a deal in the first email. Keep it brief, valuable, and easy to respond to.

