# License Validation Implementation Guide

This guide shows how to integrate license validation into the OASIS Unified MCP Server.

## Step 1: Update package.json

Add license validation dependencies:

```json
{
  "dependencies": {
    // ... existing dependencies
    "axios": "^1.6.5" // Already included
  }
}
```

## Step 2: Integrate License Validation

Update `src/index.ts` to include license validation:

```typescript
import { createLicenseValidator } from './license.js';

async function main() {
  // Validate license before starting server
  const validator = createLicenseValidator();
  
  console.error('[MCP] Validating license...');
  const licenseResult = await validator.validate();
  
  if (!licenseResult.valid) {
    console.error(`[MCP] License Error: ${licenseResult.message}`);
    console.error('[MCP] Please visit https://portal.oasis.com/licenses to get your license key.');
    console.error('[MCP] Set OASIS_MCP_LICENSE_KEY environment variable with your license key.');
    process.exit(1);
  }
  
  console.error(`[MCP] License validated successfully (Tier: ${licenseResult.tier || 'Unknown'})`);
  
  // Continue with server startup...
  const server = new Server(/* ... */);
  // ... rest of server setup
}
```

## Step 3: Update Environment Configuration

Update `env.example`:

```bash
# License Configuration
OASIS_MCP_LICENSE_KEY=your-license-key-here
LICENSE_SERVER_URL=https://licenses.oasis.com

# OASIS API Configuration
OASIS_API_URL=http://localhost:5000
OASIS_API_KEY=

# Smart Contract Generator API Configuration
SMART_CONTRACT_API_URL=http://localhost:5000

# MCP Server Configuration
MCP_MODE=stdio
PORT=3000
```

## Step 4: Build License Server (Backend)

You'll need to create a license server. Here's a basic Express.js example:

```typescript
// license-server/src/index.ts
import express from 'express';
import Stripe from 'stripe';

const app = express();
const stripe = new Stripe(process.env.STRIPE_SECRET_KEY!);

// License validation endpoint
app.post('/api/licenses/validate', async (req, res) => {
  const { licenseKey, deviceId } = req.body;
  
  // Look up license in database
  const license = await db.licenses.findOne({ key: licenseKey });
  
  if (!license) {
    return res.json({ valid: false, message: 'Invalid license key' });
  }
  
  // Check if expired
  if (new Date() > license.expiresAt) {
    return res.json({ valid: false, message: 'License has expired' });
  }
  
  // Check device activation limit
  const activations = await db.activations.count({ licenseId: license.id });
  if (activations >= license.maxDevices) {
    // Check if this device is already activated
    const existing = await db.activations.findOne({ 
      licenseId: license.id, 
      deviceId 
    });
    if (!existing) {
      return res.json({ 
        valid: false, 
        message: 'Device limit reached. Please deactivate a device.' 
      });
    }
  }
  
  // Record or update activation
  await db.activations.upsert({
    licenseId: license.id,
    deviceId,
    lastValidated: new Date(),
  });
  
  return res.json({
    valid: true,
    tier: license.tier,
    expiresAt: license.expiresAt,
  });
});

// License activation endpoint
app.post('/api/licenses/activate', async (req, res) => {
  const { licenseKey, deviceId, deviceInfo } = req.body;
  
  // Similar validation logic
  // Record device activation
  // Return success/failure
});

app.listen(3000, () => {
  console.log('License server running on port 3000');
});
```

## Step 5: Stripe Integration

Set up Stripe for subscription management:

```typescript
// Create subscription checkout
app.post('/api/subscriptions/create', async (req, res) => {
  const { tier, customerEmail } = req.body;
  
  const session = await stripe.checkout.sessions.create({
    payment_method_types: ['card'],
    line_items: [{
      price: getStripePriceId(tier), // e.g., 'price_starter_monthly'
      quantity: 1,
    }],
    mode: 'subscription',
    success_url: 'https://portal.oasis.com/success?session_id={CHECKOUT_SESSION_ID}',
    cancel_url: 'https://portal.oasis.com/cancel',
    customer_email: customerEmail,
    metadata: {
      tier,
    },
  });
  
  res.json({ checkoutUrl: session.url });
});

// Handle Stripe webhooks
app.post('/api/webhooks/stripe', async (req, res) => {
  const event = stripe.webhooks.constructEvent(
    req.body,
    req.headers['stripe-signature'],
    process.env.STRIPE_WEBHOOK_SECRET!
  );
  
  if (event.type === 'checkout.session.completed') {
    const session = event.data.object;
    // Create license in database
    const license = await createLicense({
      customerId: session.customer,
      tier: session.metadata.tier,
      expiresAt: calculateExpirationDate(session.metadata.tier),
    });
    
    // Send license key to customer via email
    await sendLicenseEmail(session.customer_email, license.key);
  }
  
  if (event.type === 'customer.subscription.deleted') {
    // Mark license as expired
    await expireLicense(event.data.object.customer);
  }
  
  res.json({ received: true });
});
```

## Step 6: Customer Portal

Create a simple customer portal for license management:

**Features:**
- View current subscription
- Download license key
- View usage statistics
- Manage devices
- Update payment method
- Cancel subscription

**Tech Stack:**
- Next.js or React
- Stripe Customer Portal API
- Your license server API

## Step 7: Testing

### Test License Validation

```bash
# Set test license key
export OASIS_MCP_LICENSE_KEY=test-license-key-123

# Run MCP server
npm run dev

# Should see: "[MCP] License validated successfully"
```

### Test Offline Mode

1. Disconnect from internet
2. Run MCP server
3. Should use cached validation (if available)
4. Should show grace period message

### Test Invalid License

```bash
export OASIS_MCP_LICENSE_KEY=invalid-key
npm run dev
# Should exit with error message
```

## Step 8: Distribution

### NPM Package

1. Update `package.json`:
```json
{
  "name": "@oasis-unified/mcp-server",
  "version": "1.0.0",
  "bin": {
    "oasis-mcp": "./dist/index.js"
  },
  "publishConfig": {
    "access": "restricted"
  }
}
```

2. Build and publish:
```bash
npm run build
npm publish --access restricted
```

### Customer Installation

Customers install via:
```bash
npm install -g @oasis-unified/mcp-server
```

Then configure in `~/.cursor/mcp.json`:
```json
{
  "mcpServers": {
    "oasis-unified": {
      "command": "oasis-mcp",
      "env": {
        "OASIS_MCP_LICENSE_KEY": "customer-license-key",
        "OASIS_API_URL": "https://api.oasis.com"
      }
    }
  }
}
```

## Step 9: Monitoring & Analytics

Track the following metrics:
- License validations (success/failure)
- Device activations
- API usage per customer
- Subscription churn
- Error rates

Use tools like:
- Google Analytics
- Mixpanel
- Custom analytics dashboard

## Security Considerations

1. **HTTPS Only**: License server must use HTTPS
2. **Rate Limiting**: Prevent brute force on license keys
3. **Device Fingerprinting**: Use multiple factors (not just MAC address)
4. **Token Rotation**: Consider rotating license keys periodically
5. **Audit Logging**: Log all validation attempts

## Next Steps

1. Build license server backend
2. Integrate Stripe
3. Create customer portal
4. Test thoroughly
5. Launch beta program
6. Gather feedback
7. Iterate and improve

## Support

For implementation questions:
- Check the main [MONETIZATION_PLAN.md](./MONETIZATION_PLAN.md)
- Review license validation code in `src/license.ts`
- Contact: dev@oasis.com
