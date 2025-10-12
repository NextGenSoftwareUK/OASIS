# ✅ Ready to Test: Custom Badge NFT Minting via Telegram!

## 🎉 Configuration Complete!

Your Pinata API is now configured and ready to upload images to IPFS!

### ✅ What's Configured:
- ✅ Pinata JWT: Configured in Startup.cs
- ✅ API Key: 9bed1345079b159c1443
- ✅ Storage Regions: FRA1 (France), NYC1 (New York)
- ✅ Token Expiry: 2026-10-11 (valid for ~1 year!)

---

## 🚀 How to Test

### Step 1: Start Your API

```bash
cd /Volumes/Storage/OASIS_CLEAN/ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
```

Wait for: `Now listening on: http://localhost:5000` or similar

### Step 2: Open Telegram

Open your Telegram bot (the one with token: 7927576561:...)

### Step 3: Test Custom Badge NFT

#### Option A: Quick Test with Any Image

1. **Find any image** on your phone (photo, screenshot, anything)
2. **Send it to your bot**
3. **Add this caption**:
   ```
   YOUR_SOLANA_WALLET | Test Badge | My first custom NFT from Telegram!
   ```
4. **Send!**

#### Option B: Design a Real Badge

1. **Create a badge image** (use Canva, Figma, or any design tool)
   - Recommended: 500x500px PNG
   - Add text, colors, icons
   - Save as PNG

2. **Send to bot** with caption:
   ```
   YOUR_SOLANA_WALLET | 30-Day Champion | Completed 30-day accountability challenge!
   ```

3. **Watch the magic happen!**

---

## 📸 What You'll See

### Bot Response (Step by Step):

```
You: [Send photo]
Caption: 7vXZK6SQYZu5... | Achievement Badge | I did it!

Bot: 🎨 Processing your image...
     1️⃣ Uploading to IPFS via Pinata...
     2️⃣ Minting your NFT...
     
     ⏳ This may take 1-2 minutes...

Bot: ✅ Image uploaded to IPFS!
     🔗 https://gateway.pinata.cloud/ipfs/QmXXXXXXXXXXXXXX
     
     🎨 Now minting your NFT...

Bot: ✅ NFT Minted Successfully! 🎉
     
     🎨 Title: Achievement Badge
     📝 Description: I did it!
     🖼️ Image: https://gateway.pinata.cloud/ipfs/QmXXX...
     💰 Sent to: 7vXZK6SQ...kxJs
     
     🔍 Check your Solana wallet!
     Your custom badge NFT is now on-chain! 🎊

Bot: ✨ Bonus: +100 Karma for creating a custom badge NFT!
```

---

## 🔍 Verify Your NFT

### Check IPFS:
1. Copy the IPFS URL from bot message
2. Open in browser: `https://gateway.pinata.cloud/ipfs/QmXXX...`
3. Your image should load!

### Check Pinata Dashboard:
1. Go to https://app.pinata.cloud/pinmanager
2. See your uploaded files
3. View storage stats

### Check Solana Wallet:
1. Open Phantom/Solflare wallet
2. Go to "Collectibles" or "NFTs"
3. See your custom badge NFT!

---

## 🎯 Test Scenarios

### Test 1: Simple Photo
- Use any photo from your camera roll
- Quick test to verify upload works

### Test 2: Designed Badge
- Create a proper achievement badge
- Test the full user experience

### Test 3: Different Formats
- PNG (transparent background)
- JPG (photo quality)
- JPEG (compatibility)

### Test 4: Different Sizes
- Small: 100x100px
- Medium: 500x500px
- Large: 2000x2000px

---

## 📊 Behind the Scenes (Logs to Watch)

When you test, you'll see logs like:

```
[TelegramBot] User 123456 sent photo with caption: 7vX... | Badge | Description
[TelegramBot] Downloaded image: 125678 bytes
[PinataService] Uploading image: badge_123456_638123456789.png (125678 bytes)
[PinataService] Upload successful: https://gateway.pinata.cloud/ipfs/QmXXX...
[NFTService] Authenticating bot avatar...
[NFTService] Successfully authenticated
[NFTService] Providers activated successfully
[NFTService] Minting NFT: Badge for wallet 7vXZK...
[NFTService] Response Status: 200
[TelegramBot] NFT minted successfully!
```

---

## ⚠️ Important Notes

### Before Production:
1. **Move credentials to environment variables**:
   ```csharp
   var pinataJwt = Environment.GetEnvironmentVariable("PINATA_JWT");
   var botPassword = Environment.GetEnvironmentVariable("BOT_PASSWORD");
   ```

2. **Don't commit credentials to git**:
   - Add to `.gitignore`
   - Use secrets management
   - Rotate keys regularly

3. **Monitor Pinata usage**:
   - Free tier: 1GB storage, 100 pins/month
   - Track uploads in dashboard
   - Upgrade plan if needed

### Security:
- ✅ Images are public on IPFS (by design)
- ✅ Don't upload sensitive information
- ✅ Validate image content before production
- ✅ Consider image moderation for public use

---

## 🎨 Example Badge Ideas to Test

### 1. Achievement Badge
- Title: "30-Day Consistency Champion"
- Description: "Completed 30 consecutive check-ins"
- Image: Gold star badge

### 2. Group Member Badge
- Title: "Metamasons Founder"
- Description: "Original member #42"
- Image: Group logo with member number

### 3. Milestone Badge
- Title: "First NFT Minted"
- Description: "Minted first achievement NFT on experiences.fun"
- Image: Rocket or celebration graphic

### 4. Proof of Work
- Title: "MVP Launched"
- Description: "Successfully launched my minimum viable product"
- Image: Screenshot of launch

---

## 💡 Next Steps After Testing

Once you verify it works:

1. **Test with your Metamasons group**
   - Get feedback on UX
   - See what badges people create
   - Iterate on features

2. **Create badge templates**
   - Pre-designed backgrounds
   - Text overlay options
   - Icon library

3. **Add auto-minting**
   - Auto-mint on achievement completion
   - Pre-configured badge designs
   - Automatic karma triggers

4. **Build badge gallery**
   - `/mynfts` command
   - View all minted badges
   - Share to social media

5. **Analytics dashboard**
   - Track mints per day
   - Popular badge types
   - IPFS storage usage

---

## 🆘 Troubleshooting

### "Failed to upload image to IPFS"
**Check:**
- Pinata JWT is correct in Startup.cs (line 243)
- Your Pinata account is active
- You haven't exceeded storage limits

**Fix:**
- Verify JWT at https://app.pinata.cloud/developers/api-keys
- Check dashboard for storage usage

### "Image received! To mint an NFT..."
**Problem:** Caption format is wrong

**Fix:** Use exactly this format:
```
WALLET | Title | Description
```
Don't forget the `|` separators!

### "Authentication failed"
**Problem:** Bot avatar credentials not set

**Fix:** Update Startup.cs lines 227-229 with your bot avatar:
- Username
- Password  
- Avatar ID

### No response from bot
**Check:**
- API is running (`dotnet run`)
- Bot token is correct
- Telegram webhook is set (or use ngrok for local)

---

## 🎊 You're Ready!

Everything is configured and ready to test!

### What You Have Now:
✅ **Full custom NFT minting** from Telegram  
✅ **IPFS storage** via Pinata  
✅ **On-chain proof** on Solana  
✅ **Instant delivery** to wallets  
✅ **Bonus karma** rewards  

### Try It Right Now:
1. Start your API: `dotnet run`
2. Open Telegram
3. Send a photo with caption
4. Watch your NFT get minted!

**This is going to blow people's minds!** 🚀🎨✨

---

## 📞 Need Help?

If something doesn't work:
1. Check the logs for error messages
2. Verify all credentials are correct
3. Test Pinata directly: https://app.pinata.cloud/pinmanager
4. Ensure Solana wallet address is valid

**Good luck with your first custom badge NFT!** 🎉

