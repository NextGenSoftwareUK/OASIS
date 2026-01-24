# OASIS Unified MCP Server - Customer Quick Start

## What is This?

The OASIS Unified MCP Server provides powerful tools for interacting with OASIS, OpenSERV, and STAR platforms directly from Cursor IDE. With this MCP server, you can:

- 🎨 Mint NFTs on Solana
- 💰 Manage wallets and send transactions
- 🔨 Generate and deploy smart contracts
- 🤖 Work with A2A Protocol agents
- 📊 Access avatar karma and NFT data
- And much more!

## Pricing

| Tier | Price | Features |
|------|-------|----------|
| **Free** | $0/month | 100 API calls/month, read-only operations |
| **Starter** | $29/month | 1,000 API calls/month, all basic operations |
| **Professional** | $99/month | 10,000 API calls/month, all features |
| **Enterprise** | Custom | Unlimited usage, dedicated support, custom features |

[View Full Pricing →](https://oasis.com/pricing)

## Quick Installation

### Step 1: Install the Package

```bash
npm install -g @oasis-unified/mcp-server
```

### Step 2: Get Your License Key

1. Sign up at [portal.oasis.com](https://portal.oasis.com)
2. Choose your subscription tier
3. Complete checkout
4. Receive license key via email

### Step 3: Configure Cursor

Edit `~/.cursor/mcp.json` (create if it doesn't exist):

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

**Important:** Replace `your-license-key-here` with your actual license key from the email.

### Step 4: Restart Cursor

Close and reopen Cursor IDE completely. The MCP server will start automatically.

### Step 5: Verify Installation

Ask Cursor: "Check the OASIS API health status"

If it works, you're all set! 🎉

## Common Use Cases

### Mint an NFT

```
"Mint an NFT with symbol 'MYNFT', title 'My First NFT', 
and metadata URL 'https://example.com/nft.json'"
```

### Create a Wallet

```
"Create a Solana wallet for my avatar"
```

### Generate a Smart Contract

```
"Generate a Solana smart contract for token vesting with 
these parameters: [your JSON spec]"
```

### Check NFT Collection

```
"Show me all NFTs for avatar username 'myusername'"
```

## Troubleshooting

### "License key is required" Error

**Solution:** Make sure you've set `OASIS_MCP_LICENSE_KEY` in your `mcp.json` file.

### "Invalid or expired license" Error

**Possible causes:**
- License key is incorrect
- Subscription has expired
- Device limit reached

**Solution:**
1. Visit [portal.oasis.com/licenses](https://portal.oasis.com/licenses)
2. Check your subscription status
3. Verify your license key
4. Contact support if issues persist

### MCP Server Not Found

**Solution:**
1. Verify installation: `which oasis-mcp`
2. Check path in `mcp.json` is correct
3. Try: `npm install -g @oasis-unified/mcp-server` again
4. Restart Cursor completely

### Connection Errors

**Solution:**
1. Verify OASIS API is accessible: `curl https://api.oasis.com/api/health`
2. Check your internet connection
3. Verify API URLs in `mcp.json` are correct

## Offline Mode

The MCP server supports offline operation for up to 7 days using cached license validation. This allows you to work without internet connection.

**Note:** After 7 days offline, you'll need to reconnect to validate your license.

## Managing Your Subscription

Visit [portal.oasis.com](https://portal.oasis.com) to:
- View usage statistics
- Update payment method
- Download invoices
- Manage devices
- Cancel subscription

## Support

- 📧 Email: support@oasis.com
- 💬 Discord: [Join our community](https://discord.gg/oasis)
- 📚 Documentation: [docs.oasis.com](https://docs.oasis.com)
- 🐛 Bug Reports: [GitHub Issues](https://github.com/oasis/mcp-server/issues)

## FAQ

### Can I use this on multiple devices?

Yes! Starter and Professional tiers allow up to 3 devices. Enterprise allows unlimited devices.

### What happens if I exceed my API call limit?

You'll receive a notification when you're approaching your limit. You can upgrade your tier or wait for the next billing cycle.

### Can I cancel anytime?

Yes, you can cancel your subscription at any time. You'll retain access until the end of your billing period.

### Do you offer refunds?

We offer a 30-day money-back guarantee for new subscriptions. Contact support for refund requests.

### Is my data private?

Yes! The MCP server runs locally on your machine. We only validate your license key with our servers. All API calls go directly to OASIS APIs, not through our servers.

## What's Next?

- Read the [Full Documentation](https://docs.oasis.com/mcp)
- Check out [Example Use Cases](https://docs.oasis.com/examples)
- Join our [Developer Community](https://discord.gg/oasis)
- Follow [@OASIS on Twitter](https://twitter.com/oasis)

---

**Need Help?** Contact us at support@oasis.com or visit [portal.oasis.com](https://portal.oasis.com)
