# Cloudflare Setup for billionshealed.com

## 🔒 Enable HTTPS with Cloudflare

### Step 1: Create Cloudflare Account
1. Go to https://cloudflare.com
2. Sign up for a free account (if you don't have one)
3. Click "Add a Site"

### Step 2: Add Your Domain
1. Enter: `billionshealed.com`
2. Click "Add Site"
3. Select the **Free Plan** ($0/month)
4. Click "Continue"

### Step 3: Review DNS Records
Cloudflare will scan your existing DNS records. You should see:
- `A` record: `@` → `138.197.235.123`
- `CNAME` record: `www` → `na-west1.surge.sh`

Make sure both are **orange cloud enabled** (proxied through Cloudflare).

### Step 4: Update Nameservers in GoDaddy
Cloudflare will give you two nameservers like:
```
bob.ns.cloudflare.com
jane.ns.cloudflare.com
```

1. Go to your GoDaddy account
2. Navigate to your domain settings for billionshealed.com
3. Find "Nameservers" section
4. Click "Change Nameservers"
5. Select "Custom" or "I'll use my own nameservers"
6. **Replace the GoDaddy nameservers with Cloudflare's nameservers**:
   - Remove: `ns73.domaincontrol.com` and `ns74.domaincontrol.com`
   - Add: The two nameservers Cloudflare provided
7. Save changes

### Step 5: Wait for Nameserver Propagation
- This takes **2-24 hours** (usually 2-4 hours)
- Cloudflare will email you when it's active
- You can check status in Cloudflare dashboard

### Step 6: Configure SSL in Cloudflare
Once nameservers are active:

1. Go to **SSL/TLS** tab in Cloudflare
2. Set encryption mode to: **Flexible** (recommended for Surge)
3. Enable **Always Use HTTPS**:
   - Go to SSL/TLS → Edge Certificates
   - Toggle "Always Use HTTPS" to ON

### Step 7: Enable Additional Security (Optional)
In Cloudflare dashboard:

**Speed:**
- Auto Minify: Enable for HTML, CSS, JS
- Brotli: Enable

**Security:**
- Security Level: Medium
- Browser Integrity Check: ON

**Caching:**
- Caching Level: Standard

## ✅ After Setup

Once complete, your site will have:
- 🔒 **HTTPS/SSL** enabled automatically
- 🚀 **Global CDN** for faster loading
- 🛡️ **DDoS protection**
- 📊 **Analytics** built-in
- ⚡ **Performance optimization**

## 🧪 Test HTTPS

After nameservers propagate:
1. Visit: `https://billionshealed.com`
2. Check for the 🔒 padlock in browser
3. Your site should automatically redirect HTTP → HTTPS

## 🔍 Troubleshooting

**Nameserver Update in GoDaddy:**
- Make sure you **completely replace** GoDaddy's nameservers
- Don't add Cloudflare's nameservers in addition to GoDaddy's
- Remove the old ones first, then add Cloudflare's

**SSL Not Working:**
- Wait 24 hours for full propagation
- Make sure SSL mode is set to "Flexible"
- Clear browser cache and try incognito mode

**Site Not Loading:**
- Check that DNS records are orange-clouded in Cloudflare
- Verify A record points to: `138.197.235.123`
- Wait for nameserver propagation to complete

## 📧 Notification

Cloudflare will email you at each step:
1. When you add the site
2. When nameservers are detected
3. When the site is active
4. When SSL certificate is issued

---

**Total Time:** 2-4 hours for nameservers + instant SSL once active  
**Cost:** $0 (Free plan includes SSL)  
**Result:** Secure HTTPS site with global CDN! 🔒✨

