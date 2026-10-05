# OASIS NFT Mint Studio — White-Label Implementation Guide

**Get Your Branded NFT Platform Live in 2-4 Weeks**

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Pre-Implementation Checklist](#pre-implementation-checklist)
3. [Option 1: Managed SaaS Setup](#option-1-managed-saas-setup)
4. [Option 2: Self-Hosted Deployment](#option-2-self-hosted-deployment)
5. [Option 3: Custom Integration](#option-3-custom-integration)
6. [Branding Customization](#branding-customization)
7. [Testing & QA](#testing--qa)
8. [Launch Checklist](#launch-checklist)
9. [Post-Launch Support](#post-launch-support)
10. [Monetization Strategies](#monetization-strategies)

---

## Overview

This guide walks you through implementing the OASIS NFT Mint Studio as a white-label solution for your platform. By the end, you'll have a production-ready, branded NFT minting platform.

### What You're Getting

✅ **Complete NFT Minting Platform**
- Browser-based wizard interface
- Multi-chain support (Solana + EVM)
- Full Metaplex compliance
- Developer API
- Automatic IPFS integration

✅ **Production-Ready Technology**
- Next.js 15 frontend
- TypeScript for type safety
- Tailwind CSS 4 for styling
- Comprehensive error handling

✅ **White-Label Capability**
- Full branding customization
- Custom domain support
- Flexible UI/UX modifications

---

## Pre-Implementation Checklist

Before starting, gather the following:

### Business Requirements
- [ ] **Use Case Defined** — Who will use this? (creators, brands, gamers, etc.)
- [ ] **Pricing Model** — How will you charge users?
- [ ] **Budget Confirmed** — Which option fits your budget?
- [ ] **Timeline Set** — When do you need to launch?
- [ ] **Success Metrics** — How will you measure success?

### Technical Requirements (Self-Hosted Only)
- [ ] **Domain Name** — e.g., `mint.yourbrand.com`
- [ ] **SSL Certificate** — For HTTPS
- [ ] **Server Access** — Ubuntu 22.04+ Linux server
- [ ] **DevOps Resource** — Person who can deploy and maintain
- [ ] **RPC Node Access** — Alchemy, QuickNode, or similar
- [ ] **IPFS Account** — Pinata or alternative

### Branding Assets
- [ ] **Logo Files** — SVG preferred, PNG backup (transparent)
- [ ] **Color Palette** — Primary, secondary, accent colors (hex codes)
- [ ] **Typography** — Font choices (if custom)
- [ ] **Copy/Messaging** — Headlines, CTAs, error messages
- [ ] **Design Guidelines** — Brand guidelines document (if available)

### Legal/Compliance
- [ ] **Terms of Service** — Your platform's ToS
- [ ] **Privacy Policy** — GDPR/CCPA compliant
- [ ] **User Agreement** — NFT-specific terms (if applicable)
- [ ] **Disclaimers** — Crypto/blockchain disclaimers

---

## Option 1: Managed SaaS Setup

**Best for:** Quick launch, minimal technical involvement  
**Timeline:** 2 weeks  
**Cost:** $5K setup + $500-$2K/month

### Week 1: Configuration & Branding

#### Day 1-2: Kickoff & Requirements
1. **Kickoff Call** (60 minutes)
   - Review use case and goals
   - Confirm branding assets
   - Set timeline and milestones

2. **Share Assets**
   - Upload logo, colors, copy to shared folder
   - Provide domain name for setup
   - Submit any custom requirements

3. **Review Contract**
   - Sign service agreement
   - Initial payment ($5K setup fee)
   - Set recurring billing ($500-$2K/month based on tier)

#### Day 3-5: Platform Setup
**We handle:**
- ✅ Server provisioning and configuration
- ✅ Domain setup (DNS, SSL certificate)
- ✅ Branding implementation (logo, colors, copy)
- ✅ Environment configuration (Solana devnet/mainnet)
- ✅ RPC node and IPFS setup

**You do:**
- Provide domain access (DNS settings)
- Review staging environment
- Test basic functionality

#### Day 6-7: Review & Feedback
1. **Staging Review**
   - Access staging URL (e.g., `staging.mint.yourbrand.com`)
   - Test all wizard steps
   - Verify branding looks correct

2. **Feedback Round**
   - Submit change requests
   - Clarify any issues
   - Approve or request modifications

### Week 2: Testing & Launch

#### Day 8-10: Refinements
**We handle:**
- ✅ Implement feedback
- ✅ Fix any bugs or issues
- ✅ Optimize performance
- ✅ Security hardening

**You do:**
- Final review of changes
- Test with real use cases
- Prepare launch communications

#### Day 11-12: User Acceptance Testing
1. **Internal Testing**
   - Have your team test thoroughly
   - Try to break things (edge cases)
   - Document any issues

2. **Beta User Testing** (Optional)
   - Invite 5-10 beta users
   - Collect feedback
   - Verify user experience

#### Day 13: Production Deployment
**We handle:**
- ✅ Production deployment
- ✅ DNS cutover to production
- ✅ Final smoke tests
- ✅ Monitoring setup

**You do:**
- Approve go-live
- Prepare support resources
- Announce launch

#### Day 14: Launch & Monitoring
1. **Go Live**
   - Platform publicly accessible at `mint.yourbrand.com`
   - Monitoring dashboards active
   - Support channels open

2. **Post-Launch**
   - Monitor for issues
   - Respond to user feedback
   - Iterate based on usage

### Ongoing: Support & Maintenance
**We provide:**
- ✅ 24/7 server monitoring
- ✅ Security patches and updates
- ✅ Performance optimization
- ✅ New feature rollouts
- ✅ Email support (24-hour response)

**You handle:**
- User support (first line)
- Marketing and growth
- Customer success

---

## Option 2: Self-Hosted Deployment

**Best for:** Full control, technical team available  
**Timeline:** 3 weeks  
**Cost:** $15K-$25K one-time + optional support

### Prerequisites

#### Server Requirements
**Minimum Specs:**
- **OS:** Ubuntu 22.04 LTS
- **RAM:** 4GB (8GB recommended)
- **Storage:** 50GB SSD
- **Network:** Static IP, open ports 80, 443

**Recommended Providers:**
- DigitalOcean ($40-$80/month)
- AWS EC2 ($50-$100/month)
- Google Cloud Compute ($45-$90/month)
- Linode ($40-$80/month)

#### Third-Party Services
1. **RPC Node Provider** (Required)
   - Alchemy (Recommended) — $49-$199/month
   - QuickNode — $49-$299/month
   - Helius (Solana) — $49-$149/month

2. **IPFS Storage** (Required)
   - Pinata (Recommended) — $20-$100/month
   - Web3.Storage — Free tier available
   - NFT.Storage — Free for NFTs

3. **Domain & DNS**
   - Custom domain — $10-$50/year
   - Cloudflare (recommended for DNS/CDN) — Free

#### Technical Team Needs
- **DevOps Engineer:** Deploy and configure server
- **Frontend Developer:** Optional, for UI customizations
- **Backend Developer:** Optional, for API integrations

### Week 1: Setup & Installation

#### Day 1: License Purchase & Onboarding
1. **Purchase License**
   - Standard ($15K) or Premium ($25K)
   - Sign license agreement
   - Payment processing

2. **Receive Access**
   - GitHub repository access
   - Documentation portal
   - Slack/email support channel

3. **Onboarding Call** (Premium only)
   - Dedicated engineer assigned
   - Review architecture
   - Plan deployment

#### Day 2-3: Server Provisioning
```bash
# Create Ubuntu 22.04 server
# SSH into server

# Update system
sudo apt update && sudo apt upgrade -y

# Install Docker and Docker Compose
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo apt install docker-compose -y

# Install Node.js (for CLI tools)
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt install -y nodejs

# Clone repository (access provided)
git clone https://github.com/oasis/nft-mint-studio-white-label.git
cd nft-mint-studio-white-label
```

#### Day 4-5: Configuration
1. **Environment Variables**
   ```bash
   # Copy example environment file
   cp .env.example .env
   
   # Edit .env with your settings
   nano .env
   ```

   **Required Variables:**
   ```bash
   # App Configuration
   NEXT_PUBLIC_APP_NAME="Your Brand NFT Studio"
   NEXT_PUBLIC_APP_URL="https://mint.yourbrand.com"
   
   # OASIS API
   OASIS_API_URL="https://oasisweb4.one"
   # Or use your own OASIS API instance
   
   # Solana Configuration
   SOLANA_NETWORK="devnet"  # or "mainnet-beta"
   SOLANA_RPC_URL="https://your-alchemy-url.com"
   
   # IPFS/Pinata
   PINATA_JWT="your-pinata-jwt-token"
   PINATA_GATEWAY="https://gateway.pinata.cloud"
   
   # Authentication (if using OASIS auth)
   OASIS_BOT_USERNAME="your_bot_username"
   OASIS_BOT_PASSWORD="your_bot_password"
   OASIS_BOT_AVATAR_ID="your-avatar-id"
   ```

2. **Branding Configuration**
   ```typescript
   // config/branding.ts
   export const BRAND_CONFIG = {
     name: "Your Brand NFT Studio",
     logo: "/logos/your-logo.svg",
     colors: {
       primary: "#6366f1",      // Your primary color
       secondary: "#8b5cf6",    // Your secondary color
       accent: "#ec4899",       // Your accent color
     },
     typography: {
       headingFont: "Inter",
       bodyFont: "Inter",
     },
     domain: "mint.yourbrand.com",
   };
   ```

#### Day 6-7: Build & Test Locally
```bash
# Install dependencies
npm install

# Build the application
npm run build

# Test locally
npm run start

# Access at http://localhost:3000
```

**Testing Checklist:**
- [ ] Homepage loads correctly
- [ ] Branding shows your logo/colors
- [ ] Wizard steps work
- [ ] Authentication works (devnet)
- [ ] Asset upload works
- [ ] Test NFT mint (devnet)

### Week 2: Deployment & Customization

#### Day 8-9: Docker Deployment
```bash
# Build Docker image
docker-compose build

# Start services
docker-compose up -d

# Check logs
docker-compose logs -f
```

**docker-compose.yml:**
```yaml
version: '3.8'

services:
  nft-studio:
    build: .
    ports:
      - "3000:3000"
    environment:
      - NODE_ENV=production
    env_file:
      - .env
    restart: unless-stopped
    volumes:
      - ./public:/app/public
      - ./config:/app/config

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - nft-studio
    restart: unless-stopped
```

#### Day 10-12: SSL & Domain Setup
1. **DNS Configuration**
   ```
   # Add A record in your DNS
   Type: A
   Name: mint (or @)
   Value: YOUR_SERVER_IP
   TTL: 3600
   ```

2. **SSL Certificate (Let's Encrypt)**
   ```bash
   # Install Certbot
   sudo apt install certbot python3-certbot-nginx
   
   # Get certificate
   sudo certbot --nginx -d mint.yourbrand.com
   
   # Test auto-renewal
   sudo certbot renew --dry-run
   ```

3. **Nginx Configuration**
   ```nginx
   server {
       listen 80;
       server_name mint.yourbrand.com;
       return 301 https://$server_name$request_uri;
   }
   
   server {
       listen 443 ssl http2;
       server_name mint.yourbrand.com;
       
       ssl_certificate /etc/letsencrypt/live/mint.yourbrand.com/fullchain.pem;
       ssl_certificate_key /etc/letsencrypt/live/mint.yourbrand.com/privkey.pem;
       
       location / {
           proxy_pass http://nft-studio:3000;
           proxy_set_header Host $host;
           proxy_set_header X-Real-IP $remote_addr;
       }
   }
   ```

#### Day 13-14: Custom Branding
**Update Logo:**
```bash
# Replace logo files in public/logos/
cp /path/to/your-logo.svg public/logos/logo.svg
cp /path/to/your-logo-white.svg public/logos/logo-white.svg
```

**Update Colors:**
```css
/* styles/globals.css */
:root {
  --color-primary: #6366f1;      /* Your primary */
  --color-secondary: #8b5cf6;    /* Your secondary */
  --color-accent: #ec4899;        /* Your accent */
  
  /* Generated shades automatically */
}
```

**Update Copy:**
```typescript
// config/copy.ts
export const COPY = {
  hero: {
    title: "Mint NFTs in Minutes",
    subtitle: "The easiest way to create and launch your NFT collection",
  },
  wizard: {
    step1Title: "Choose Your NFT Type",
    step2Title: "Connect & Authenticate",
    // ... etc
  },
};
```

### Week 3: Testing & Launch

#### Day 15-17: Comprehensive Testing
**Functional Testing:**
- [ ] All wizard steps work end-to-end
- [ ] Authentication flows correctly
- [ ] Asset uploads succeed
- [ ] NFT minting works (devnet)
- [ ] Error handling displays properly
- [ ] Mobile responsive design works

**Security Testing:**
- [ ] SSL certificate valid
- [ ] HTTPS enforced
- [ ] Environment variables secure
- [ ] No sensitive data exposed
- [ ] Rate limiting configured

**Performance Testing:**
- [ ] Page load times < 3 seconds
- [ ] Image optimization working
- [ ] Caching configured
- [ ] CDN setup (if applicable)

#### Day 18-19: Mainnet Preparation
```bash
# Update environment for mainnet
SOLANA_NETWORK="mainnet-beta"
SOLANA_RPC_URL="https://your-mainnet-rpc-url"

# Rebuild and deploy
docker-compose down
docker-compose build
docker-compose up -d
```

**Mainnet Checklist:**
- [ ] RPC node upgraded to mainnet
- [ ] Test wallet funded with SOL
- [ ] Pinata account upgraded (if needed)
- [ ] Monitoring configured
- [ ] Backup strategy in place

#### Day 20-21: Launch
1. **Soft Launch**
   - Invite 10-20 beta users
   - Monitor for issues
   - Collect feedback

2. **Public Launch**
   - Announce on your channels
   - Monitor traffic and errors
   - Be ready for support requests

### Ongoing: Maintenance
**Weekly:**
- [ ] Review error logs
- [ ] Check server resources
- [ ] Monitor uptime
- [ ] Update dependencies (if needed)

**Monthly:**
- [ ] Security updates
- [ ] Performance review
- [ ] User feedback analysis
- [ ] Feature planning

---

## Option 3: Custom Integration

**Best for:** Unique requirements, existing platform integration  
**Timeline:** 4-6 weeks  
**Cost:** $30K-$50K one-time

### Week 1-2: Discovery & Planning

#### Kickoff & Requirements Gathering
1. **Discovery Call** (2-3 hours)
   - Review your existing platform
   - Identify integration points
   - Document custom requirements
   - Define success criteria

2. **Technical Architecture Review**
   - API integration needs
   - Authentication/SSO requirements
   - Database integration
   - Custom UI/UX needs

3. **Project Plan Creation**
   - Detailed scope of work
   - Timeline and milestones
   - Resource allocation
   - Communication plan

#### Week 1 Deliverables:
- [ ] Signed Statement of Work (SOW)
- [ ] Technical architecture document
- [ ] Integration specification
- [ ] Project timeline
- [ ] Dedicated Slack/Discord channel

### Week 2-4: Development

#### Our Team Handles:
**Week 2:**
- Authentication integration (your SSO/auth system)
- Database connections (if needed)
- Custom API endpoints
- UI component customization

**Week 3:**
- Advanced features development
- Multi-tenant architecture (if needed)
- Custom analytics integration
- Payment processing integration

**Week 4:**
- Quality assurance testing
- Security audit
- Performance optimization
- Documentation creation

#### Your Team Provides:
- API credentials/access
- Design mockups (if custom UI)
- Test environment access
- Stakeholder feedback

### Week 5: Testing & Refinement

#### Integration Testing
- [ ] End-to-end workflow testing
- [ ] Your platform → NFT studio integration
- [ ] SSO/authentication flows
- [ ] Data sync verification
- [ ] Error handling validation

#### User Acceptance Testing
- [ ] Internal stakeholder review
- [ ] Beta user testing
- [ ] Performance under load
- [ ] Mobile/responsive testing
- [ ] Cross-browser testing

### Week 6: Deployment & Launch

#### Production Deployment
- Coordinated deployment plan
- Rollback strategy defined
- Monitoring configured
- Support plan activated

#### Post-Launch Support (12 months included)
- Priority Slack/email support
- Bug fixes (SLA defined)
- Minor enhancements
- Quarterly check-ins

---

## Branding Customization

### Logo Specifications

**Format Requirements:**
- **Primary Logo:** SVG (vector format)
- **Backup:** PNG at 2x resolution (transparent background)
- **Sizes Needed:**
  - Header: 180px × 50px
  - Icon: 64px × 64px
  - Favicon: 32px × 32px

**File Locations:**
```
public/
  logos/
    logo.svg          # Primary logo
    logo-white.svg    # White version for dark backgrounds
    icon.svg          # Icon only
  favicon.ico         # Browser favicon
```

### Color Palette

**Required Colors:**
```css
:root {
  /* Primary Brand Colors */
  --color-primary: #6366f1;
  --color-primary-dark: #4f46e5;
  --color-primary-light: #818cf8;
  
  /* Secondary Colors */
  --color-secondary: #8b5cf6;
  --color-secondary-dark: #7c3aed;
  --color-secondary-light: #a78bfa;
  
  /* Accent Color */
  --color-accent: #ec4899;
  
  /* Neutral Colors (usually don't need to change) */
  --color-background: #0f172a;
  --color-card: #1e293b;
  --color-text: #f1f5f9;
  --color-muted: #94a3b8;
}
```

**Color Usage Guide:**
- **Primary:** CTAs, buttons, important actions
- **Secondary:** Links, secondary actions
- **Accent:** Highlights, notifications
- **Neutral:** Backgrounds, text, borders

### Typography

**Recommended Font Pairings:**
```css
/* Option 1: Modern & Clean */
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
font-family: 'Inter', sans-serif;

/* Option 2: Professional & Elegant */
@import url('https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700&display=swap');
font-family: 'Plus Jakarta Sans', sans-serif;

/* Option 3: Bold & Modern */
@import url('https://fonts.googleapis.com/css2?family=Outfit:wght@400;500;600;700&display=swap');
font-family: 'Outfit', sans-serif;
```

### Copy/Messaging

**Key Areas to Customize:**
```typescript
// config/copy.ts
export const COPY = {
  // Homepage
  hero: {
    title: "Your Custom Headline",
    subtitle: "Your value proposition",
    cta: "Get Started",
  },
  
  // Wizard Steps
  wizard: {
    step1: {
      title: "Step 1 Title",
      description: "What users do in this step",
    },
    // ... more steps
  },
  
  // Errors & Notifications
  errors: {
    generic: "Something went wrong. Please try again.",
    network: "Network error. Check your connection.",
    // ... more errors
  },
  
  // Success Messages
  success: {
    mintComplete: "NFT minted successfully!",
    uploaded: "Asset uploaded!",
  },
};
```

---

## Testing & QA

### Pre-Launch Testing Checklist

#### Functional Testing
- [ ] **Wizard Navigation**
  - All steps accessible
  - Back/forward navigation works
  - Progress tracking accurate

- [ ] **Authentication**
  - Login/signup works
  - JWT token management
  - Session persistence
  - Logout functionality

- [ ] **Asset Upload**
  - Image upload succeeds
  - Supported formats work (PNG, JPG, GIF)
  - File size limits enforced
  - Progress indicators working

- [ ] **NFT Minting**
  - Standard NFT minting
  - Editioned NFTs
  - Collection creation
  - Compressed NFTs (if Solana)

- [ ] **Error Handling**
  - Network errors shown
  - Validation errors clear
  - Retry mechanisms work
  - User-friendly error messages

#### Security Testing
- [ ] **SSL/HTTPS**
  - Certificate valid
  - HTTPS enforced
  - No mixed content warnings

- [ ] **Environment Variables**
  - No secrets exposed in frontend
  - API keys secure
  - Sensitive data encrypted

- [ ] **Input Validation**
  - SQL injection prevention
  - XSS prevention
  - CSRF protection
  - Rate limiting configured

#### Performance Testing
- [ ] **Page Load Times**
  - Homepage < 2 seconds
  - Wizard steps < 1 second
  - Image optimization working
  
- [ ] **Asset Optimization**
  - Images compressed
  - Lazy loading enabled
  - Code splitting working

- [ ] **Mobile Performance**
  - Touch interactions smooth
  - Responsive design working
  - No console errors

#### Browser Testing
- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)
- [ ] Mobile Safari (iOS)
- [ ] Mobile Chrome (Android)

#### Device Testing
- [ ] Desktop (1920×1080)
- [ ] Laptop (1366×768)
- [ ] Tablet (768×1024)
- [ ] Mobile (375×667)
- [ ] Mobile (414×896)

---

## Launch Checklist

### Pre-Launch (1 Week Before)

#### Technical
- [ ] All tests passing
- [ ] Security audit complete
- [ ] Performance optimized
- [ ] Monitoring configured
- [ ] Backups automated
- [ ] SSL certificate valid
- [ ] DNS configured
- [ ] CDN configured (if using)

#### Content
- [ ] Terms of Service published
- [ ] Privacy Policy published
- [ ] User documentation ready
- [ ] FAQ section complete
- [ ] Support email/chat configured

#### Marketing
- [ ] Launch announcement drafted
- [ ] Social media posts scheduled
- [ ] Email to waitlist prepared
- [ ] Press release (if applicable)
- [ ] Landing page optimized

### Launch Day

#### Morning
- [ ] Final smoke tests
- [ ] Confirm monitoring active
- [ ] Support team briefed
- [ ] Rollback plan ready

#### Go-Live
- [ ] DNS cutover (if needed)
- [ ] Announcement posted
- [ ] Social media live
- [ ] Monitor traffic and errors

#### Evening
- [ ] Review error logs
- [ ] Check performance metrics
- [ ] Respond to support requests
- [ ] Document any issues

### Post-Launch (Week 1)

#### Daily Monitoring
- [ ] Check error rates
- [ ] Monitor user signups
- [ ] Track NFT mints
- [ ] Review user feedback
- [ ] Respond to support tickets

#### Optimization
- [ ] Fix critical bugs immediately
- [ ] Plan minor improvements
- [ ] Collect feature requests
- [ ] Analyze usage patterns

---

## Post-Launch Support

### What We Provide

#### Standard Support (Included)
- **Response Time:** 24-48 hours
- **Channels:** Email
- **Coverage:** Bug fixes, technical questions
- **Hours:** Business hours (9 AM - 5 PM ET, Mon-Fri)

#### Premium Support (Optional)
- **Response Time:** 4-24 hours
- **Channels:** Email, Slack, phone
- **Coverage:** Priority bugs, feature requests, consultation
- **Hours:** Extended (9 AM - 9 PM ET, 7 days/week)

### Support Tiers

| Issue Type | Standard | Premium |
|------------|----------|---------|
| **Critical Bug** (site down) | 24 hours | 4 hours |
| **Major Bug** (feature broken) | 48 hours | 12 hours |
| **Minor Bug** (cosmetic) | 5 days | 24 hours |
| **Feature Request** | Best effort | 48 hours for quote |
| **Consultation** | Not included | 2 hours/month |

### Common Issues & Solutions

#### "NFT Minting Fails"
**Troubleshooting:**
1. Check RPC node status
2. Verify wallet has sufficient SOL/gas
3. Review error logs
4. Test with different wallet

**Support Process:**
1. Collect error logs
2. Reproduce issue
3. Identify root cause
4. Deploy fix within SLA

#### "Asset Upload Not Working"
**Troubleshooting:**
1. Check Pinata API key
2. Verify file size/format
3. Test network connectivity
4. Review CORS settings

#### "Branding Not Updating"
**Troubleshooting:**
1. Clear browser cache
2. Rebuild application
3. Check CSS priority
4. Verify config file updated

---

## Monetization Strategies

### Pricing Models for Your Users

#### Option 1: Transaction Fees
**Best for:** High-volume platforms

**Pricing:**
- $0.25 - $1.00 per NFT mint
- Tiered discounts for volume

**Implementation:**
```typescript
// In your payment logic
const MINT_FEE = 0.50; // $0.50 per mint
const userPayment = baseCost + MINT_FEE;
```

**Revenue Potential:**
- 1,000 mints/month × $0.50 = $500/month
- 10,000 mints/month × $0.50 = $5,000/month

#### Option 2: Subscription Plans
**Best for:** Creator platforms, SaaS products

**Tiers:**
- **Free:** 10 mints/month
- **Creator:** $29/month — unlimited mints
- **Business:** $99/month — unlimited + API access
- **Enterprise:** Custom — white-label + support

**Implementation:**
```typescript
// Check user's plan
if (user.plan === 'free' && user.monthlyMints >= 10) {
  showUpgradeModal();
}
```

**Revenue Potential:**
- 100 paid users × $29 avg = $2,900/month
- 500 paid users × $45 avg = $22,500/month

#### Option 3: Hybrid Model
**Best for:** Maximum flexibility

**Structure:**
- Free tier: 5 mints/month
- Transaction fees: $0.25/mint beyond free tier
- Unlimited plans: $29-$99/month
- Enterprise: Custom pricing

#### Option 4: White-Label Reselling
**Best for:** Agencies, B2B platforms

**Model:**
- Charge clients $5K-$25K per deployment
- Your cost: $15K (one license)
- Profit: $5K-$10K per client
- Recurring: $500-$2K/month managed hosting

### Payment Processing

**Recommended Providers:**
1. **Stripe** — Best for fiat payments
2. **Coinbase Commerce** — Crypto payments
3. **Circle** — USDC payments

**Integration:**
```typescript
// Stripe example
import Stripe from 'stripe';

const stripe = new Stripe(process.env.STRIPE_SECRET_KEY);

async function chargeMintFee(userId: string, amount: number) {
  const paymentIntent = await stripe.paymentIntents.create({
    amount: amount * 100, // cents
    currency: 'usd',
    metadata: { userId, type: 'nft_mint' },
  });
  return paymentIntent;
}
```

### Analytics & Tracking

**Key Metrics to Monitor:**
```typescript
// Track these events
analytics.track('nft_mint_started');
analytics.track('nft_mint_completed');
analytics.track('asset_uploaded');
analytics.track('user_upgraded_plan');
analytics.track('payment_succeeded');
```

**Revenue Dashboards:**
- Daily/monthly active users
- NFTs minted per day
- Conversion rate (free → paid)
- Average revenue per user (ARPU)
- Churn rate
- Lifetime value (LTV)

---

## Advanced Customizations

### Multi-Tenant Architecture

**For:** Agencies serving multiple clients

```typescript
// config/tenants.ts
export const TENANTS = {
  'client1.mint.youragency.com': {
    name: 'Client 1 NFT Studio',
    logo: '/tenants/client1/logo.svg',
    colors: { primary: '#FF6B6B' },
    apiKey: 'client1_key',
  },
  'client2.mint.youragency.com': {
    name: 'Client 2 NFT Studio',
    logo: '/tenants/client2/logo.svg',
    colors: { primary: '#4ECDC4' },
    apiKey: 'client2_key',
  },
};

// Detect tenant from subdomain
export function getTenant(hostname: string) {
  return TENANTS[hostname] || TENANTS['default'];
}
```

### Custom Workflows

**Example: Gaming Integration**

```typescript
// Auto-mint achievement NFTs when player completes level
async function onLevelComplete(playerId: string, level: number) {
  const achievement = {
    title: `Level ${level} Champion`,
    description: `Completed Level ${level}`,
    imageUrl: `https://yourgame.com/achievements/level-${level}.png`,
  };
  
  const wallet = await getPlayerWallet(playerId);
  
  await mintNFT({
    ...achievement,
    recipientWallet: wallet,
  });
  
  notifyPlayer(playerId, `You earned an NFT achievement!`);
}
```

### API Extensions

**Add Custom Endpoints:**

```typescript
// pages/api/custom/bulk-mint.ts
export default async function handler(req, res) {
  // Your custom bulk minting logic
  const { nfts, recipientWallet } = req.body;
  
  for (const nft of nfts) {
    await mintNFT({ ...nft, recipientWallet });
  }
  
  res.json({ success: true, count: nfts.length });
}
```

---

## Success Stories & Best Practices

### Case Study 1: Gaming Platform
**Client:** Indie gaming studio  
**Use Case:** In-game achievement NFTs  
**Implementation:** Self-hosted license  
**Timeline:** 3 weeks  
**Results:**
- 5,000+ NFTs minted in first month
- 80% player engagement with NFTs
- $8K/month revenue from NFT sales
- Recovered investment in 2 months

**Key Learnings:**
- Integrated NFT minting into existing auth system
- Automated minting triggered by game events
- Custom badge designs increased player interest

### Case Study 2: Creator Platform
**Client:** Art marketplace  
**Use Case:** Enable artists to mint directly  
**Implementation:** Managed SaaS  
**Timeline:** 2 weeks  
**Results:**
- 150+ artists onboarded first month
- 1,200 NFTs minted
- $3K MRR from transaction fees
- 45% artist retention rate

**Key Learnings:**
- Simple UI crucial for non-technical artists
- Educational content reduced support burden
- Transaction fees preferred over subscriptions

### Case Study 3: Enterprise Loyalty
**Client:** Retail brand (Fortune 1000)  
**Use Case:** NFT loyalty program  
**Implementation:** Custom integration  
**Timeline:** 6 weeks  
**Results:**
- 50,000+ loyalty NFTs minted
- 25% increase in customer engagement
- Multi-year contract ($100K+)

**Key Learnings:**
- Compliance and security critical
- Integration with existing CRM essential
- White-glove support justified premium pricing

---

## Need Help?

### Contact Support

**Email:** support@oasisnftstudio.com  
**Slack:** Join our partner Slack (invite sent with license)  
**Documentation:** https://docs.oasisnftstudio.com  
**Office Hours:** Tuesdays & Thursdays, 2-4 PM ET (Premium only)

### Resource Library

**Documentation:**
- API Reference
- Component Library
- Customization Guide
- Troubleshooting Guide

**Video Tutorials:**
- Self-Hosted Deployment Walkthrough
- Branding Customization Tutorial
- Multi-Tenant Setup Guide
- Performance Optimization Tips

---

## Conclusion

You now have a complete roadmap to implement your white-label NFT platform. Whether you choose Managed SaaS (2 weeks), Self-Hosted (3 weeks), or Custom Integration (4-6 weeks), you'll launch faster and cheaper than building from scratch.

**Next Steps:**
1. Choose your implementation option
2. Schedule kickoff call
3. Gather branding assets
4. Follow this guide step-by-step
5. Launch your NFT platform!

**Questions? Let's talk:** [Your Email] | [Your Calendar]

---

*OASIS NFT Mint Studio — White-Label Implementation Guide*  
*Version 1.0 — October 2025*  
*For licensed partners only — confidential*



