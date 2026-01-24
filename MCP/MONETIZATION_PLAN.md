# OASIS Unified MCP Server - Monetization Plan

## Overview

This document outlines strategies for making the OASIS Unified MCP Server available as a subscription-based service. The MCP server provides comprehensive tools for interacting with OASIS, OpenSERV, and STAR platforms, including NFT minting, wallet management, smart contract generation, and A2A Protocol integration.

## Distribution Models

### Option 1: Self-Hosted with License Validation (Recommended)

**How it works:**
- Customers download the MCP server package (npm package or GitHub release)
- They install it locally and configure it in their Cursor IDE
- The server validates a subscription license on startup
- License validation happens via API call to your license server

**Pros:**
- Customers maintain control and privacy
- No dependency on your infrastructure for runtime
- Lower hosting costs
- Better performance (local execution)

**Cons:**
- Requires license validation infrastructure
- Customers need to manage updates
- Potential for license key sharing (mitigated with device fingerprinting)

### Option 2: Hosted MCP Server (SaaS)

**How it works:**
- You host the MCP server on your infrastructure
- Customers connect via HTTP/WebSocket to your hosted instance
- Each customer gets an API key for authentication
- Usage is tracked and billed

**Pros:**
- Centralized updates and maintenance
- Better control over usage
- Easier to prevent piracy
- Can offer premium features like rate limiting, analytics

**Cons:**
- Higher infrastructure costs
- Requires 24/7 uptime
- Latency concerns
- Privacy concerns (all requests go through your servers)

### Option 3: Hybrid Model (Best of Both Worlds)

**How it works:**
- Self-hosted MCP server with license validation
- Optional hosted proxy for premium features (analytics, rate limiting, etc.)
- Customers can choose self-hosted or hosted

**Pros:**
- Flexibility for customers
- Multiple revenue streams
- Better user experience options

**Cons:**
- More complex to maintain
- Requires both distribution models

## Recommended Approach: Self-Hosted with License Validation

We recommend **Option 1** (Self-Hosted with License Validation) because:
1. MCP servers are designed to run locally (stdio transport)
2. Better performance and privacy
3. Lower operational costs
4. Aligns with how Cursor/MCP is typically used

## Implementation Plan

### Phase 1: License Validation System

#### 1.1 License Server API

Create a license validation service that:
- Issues subscription licenses
- Validates license keys
- Tracks device activations
- Manages subscription status

**Endpoints needed:**
```
POST /api/licenses/validate
  - Validates license key
  - Returns: { valid: boolean, expiresAt: date, tier: string }

POST /api/licenses/activate
  - Activates license on a device
  - Returns: { activationId: string, deviceFingerprint: string }

GET /api/licenses/status
  - Checks subscription status
  - Returns: { active: boolean, tier: string, usage: object }
```

#### 1.2 License Validation in MCP Server

Add license validation to `src/index.ts`:

```typescript
// Add to config.ts
export const config = {
  // ... existing config
  licenseKey: process.env.OASIS_MCP_LICENSE_KEY || '',
  licenseServerUrl: process.env.LICENSE_SERVER_URL || 'https://licenses.oasis.com',
  deviceId: getDeviceFingerprint(), // Generate unique device ID
};

// Add validation function
async function validateLicense(): Promise<boolean> {
  if (!config.licenseKey) {
    throw new Error('License key required. Set OASIS_MCP_LICENSE_KEY environment variable.');
  }
  
  const response = await axios.post(`${config.licenseServerUrl}/api/licenses/validate`, {
    licenseKey: config.licenseKey,
    deviceId: config.deviceId,
  });
  
  if (!response.data.valid) {
    throw new Error('Invalid or expired license. Please renew your subscription.');
  }
  
  return true;
}

// Call in main() before starting server
await validateLicense();
```

#### 1.3 Device Fingerprinting

Generate a unique device ID to prevent license sharing:

```typescript
import { execSync } from 'child_process';
import os from 'os';
import crypto from 'crypto';

function getDeviceFingerprint(): string {
  const components = [
    os.hostname(),
    os.platform(),
    os.arch(),
    // Add more unique identifiers
  ];
  
  const fingerprint = crypto
    .createHash('sha256')
    .update(components.join('|'))
    .digest('hex');
  
  return fingerprint;
}
```

### Phase 2: Subscription Management

#### 2.1 Pricing Tiers

**Free Tier (Limited)**
- 100 API calls/month
- Read-only operations
- Community support
- Basic NFT operations

**Starter - $29/month**
- 1,000 API calls/month
- All read operations
- Basic write operations (NFT minting, wallet creation)
- Email support
- Self-hosted

**Professional - $99/month**
- 10,000 API calls/month
- All operations including smart contract generation
- A2A Protocol tools
- Priority email support
- Self-hosted + optional hosted proxy

**Enterprise - Custom Pricing**
- Unlimited API calls
- Custom integrations
- Dedicated support
- SLA guarantees
- On-premise deployment options
- Custom feature development

#### 2.2 Payment Integration

**Recommended: Stripe**

1. **Stripe Checkout** for subscription signup
2. **Stripe Webhooks** for subscription events
3. **Stripe Customer Portal** for self-service management

**Implementation:**
```typescript
// License server endpoint
POST /api/subscriptions/create
  - Creates Stripe checkout session
  - Returns: { checkoutUrl: string }

POST /api/webhooks/stripe
  - Handles Stripe webhook events
  - Updates license status in database
```

### Phase 3: Distribution

#### 3.1 NPM Package Distribution

**Package name:** `@oasis-unified/mcp-server`

**Package structure:**
```
oasis-unified-mcp-server/
├── package.json
├── dist/
│   └── index.js (compiled)
├── README.md
├── LICENSE
└── .npmignore
```

**Package.json updates:**
```json
{
  "name": "@oasis-unified/mcp-server",
  "version": "1.0.0",
  "description": "OASIS Unified MCP Server - Subscription Required",
  "bin": {
    "oasis-mcp": "./dist/index.js"
  },
  "keywords": ["mcp", "oasis", "nft", "blockchain", "cursor"],
  "license": "PROPRIETARY",
  "publishConfig": {
    "access": "restricted"
  }
}
```

**Installation:**
```bash
npm install -g @oasis-unified/mcp-server
```

#### 3.2 GitHub Releases (Alternative)

- Create private GitHub repository
- Use GitHub Releases for version distribution
- Customers download from releases page
- Include license key in download email

#### 3.3 Docker Image (Optional)

For customers who prefer containerized deployment:

```dockerfile
FROM node:20-alpine
WORKDIR /app
COPY dist/ ./dist/
COPY package.json ./
RUN npm install --production
ENTRYPOINT ["node", "dist/index.js"]
```

### Phase 4: Customer Onboarding

#### 4.1 Signup Flow

1. Customer visits your website
2. Selects subscription tier
3. Completes Stripe checkout
4. Receives email with:
   - License key
   - Installation instructions
   - Configuration guide
   - Support contact

#### 4.2 Installation Instructions

Create customer-facing documentation:

```markdown
# OASIS Unified MCP Server - Installation Guide

## Step 1: Install the Package

```bash
npm install -g @oasis-unified/mcp-server
```

## Step 2: Get Your License Key

Your license key was sent to your email. If you need it again, visit:
https://portal.oasis.com/licenses

## Step 3: Configure Cursor

Edit `~/.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "oasis-unified": {
      "command": "oasis-mcp",
      "env": {
        "OASIS_MCP_LICENSE_KEY": "your-license-key-here",
        "OASIS_API_URL": "https://api.oasis.com",
        "SMART_CONTRACT_API_URL": "https://scgen.oasis.com"
      }
    }
  }
}
```

## Step 4: Restart Cursor

Restart Cursor IDE to load the MCP server.

## Troubleshooting

- **License Error**: Verify your license key is correct and subscription is active
- **Connection Issues**: Check that OASIS API URLs are correct
- **Support**: Email support@oasis.com
```

### Phase 5: Usage Tracking & Analytics

#### 5.1 Usage Metrics

Track the following:
- API calls per customer
- Tool usage (which tools are most popular)
- Error rates
- Performance metrics
- Feature adoption

#### 5.2 Analytics Dashboard

Create a customer portal where subscribers can:
- View usage statistics
- Manage subscription
- Download invoices
- Request support
- Access documentation

### Phase 6: Security & Anti-Piracy

#### 6.1 License Validation Strategies

1. **Online Validation** (Primary)
   - Validate on startup
   - Periodic re-validation (every 24 hours)
   - Grace period for offline use (7 days)

2. **Device Limits**
   - Allow 2-3 devices per license
   - Track device fingerprints
   - Require deactivation before new activation

3. **Rate Limiting**
   - Enforce API call limits per tier
   - Track usage server-side
   - Block excessive usage

#### 6.2 Code Obfuscation

For additional protection:
- Use tools like `javascript-obfuscator` for compiled code
- Minify and obfuscate sensitive logic
- Keep license validation server-side

### Phase 7: Marketing & Sales

#### 7.1 Landing Page

Create a marketing site with:
- Feature highlights
- Pricing tiers
- Use cases
- Customer testimonials
- Documentation links
- "Start Free Trial" CTA

#### 7.2 Content Marketing

- Blog posts about MCP, OASIS, blockchain development
- Tutorial videos
- Case studies
- Developer community engagement

#### 7.3 Partnerships

- Partner with Cursor IDE
- Partner with blockchain development platforms
- Integrate with popular developer tools

## Technical Implementation Checklist

### License Server (Backend)

- [ ] Set up license validation API
- [ ] Integrate Stripe for payments
- [ ] Create customer database
- [ ] Implement device fingerprinting
- [ ] Build customer portal
- [ ] Set up webhook handlers
- [ ] Create analytics dashboard
- [ ] Implement usage tracking

### MCP Server Updates

- [ ] Add license validation logic
- [ ] Implement device fingerprinting
- [ ] Add usage tracking
- [ ] Create graceful error messages
- [ ] Add offline grace period
- [ ] Update documentation
- [ ] Create installation scripts

### Distribution

- [ ] Set up NPM package
- [ ] Create GitHub releases
- [ ] Build Docker image (optional)
- [ ] Create installation documentation
- [ ] Set up customer email templates

### Marketing

- [ ] Create landing page
- [ ] Set up payment flow
- [ ] Write documentation
- [ ] Create video tutorials
- [ ] Set up support system

## Revenue Projections

### Conservative Estimates

**Year 1:**
- 50 Starter subscriptions: $29 × 50 = $1,450/month
- 10 Professional subscriptions: $99 × 10 = $990/month
- 2 Enterprise: $500/month average = $1,000/month
- **Total: ~$3,440/month = ~$41,280/year**

**Year 2 (with growth):**
- 200 Starter: $5,800/month
- 50 Professional: $4,950/month
- 5 Enterprise: $2,500/month
- **Total: ~$13,250/month = ~$159,000/year**

### Costs

- License server hosting: ~$50-100/month
- Stripe fees: 2.9% + $0.30 per transaction
- Support: Consider hiring part-time support at scale
- Marketing: Variable

## Next Steps

1. **Build License Server** (2-3 weeks)
   - Set up backend infrastructure
   - Implement validation API
   - Integrate Stripe

2. **Update MCP Server** (1 week)
   - Add license validation
   - Test thoroughly
   - Update documentation

3. **Create Distribution** (1 week)
   - Set up NPM package
   - Create installation docs
   - Test customer flow

4. **Launch** (1 week)
   - Create landing page
   - Set up payment flow
   - Announce to community

**Total Timeline: 5-6 weeks to launch**

## Alternative: Freemium Model

Consider offering a free tier to:
- Build user base
- Generate word-of-mouth
- Upsell to paid tiers
- Collect usage data for product improvement

Free tier could include:
- Limited API calls (100/month)
- Read-only operations
- Community support
- Watermarked outputs (optional)

## Support & Maintenance

### Support Channels

- Email support (tiered by subscription level)
- Discord/Slack community (free tier)
- Documentation site
- Video tutorials

### Update Strategy

- Monthly feature updates
- Security patches as needed
- Major version releases quarterly
- Backward compatibility for 2 major versions

## Legal Considerations

1. **Terms of Service**: Define usage rights, restrictions
2. **Privacy Policy**: How customer data is handled
3. **License Agreement**: Software license terms
4. **Refund Policy**: Define refund terms
5. **SLA**: For Enterprise tier, define uptime guarantees

## Conclusion

The self-hosted model with license validation provides the best balance of:
- Customer control and privacy
- Revenue generation
- Operational simplicity
- Scalability

Start with a simple implementation and iterate based on customer feedback.
