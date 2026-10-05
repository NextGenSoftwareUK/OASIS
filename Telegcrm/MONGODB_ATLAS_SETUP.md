# MongoDB Atlas Setup for Telegcrm

## Why MongoDB Atlas?

MongoDB Atlas is a cloud-hosted MongoDB service that:
- ✅ No local installation needed
- ✅ Free tier available (512MB storage)
- ✅ Automatic backups
- ✅ Easy to set up
- ✅ Works from anywhere

Perfect for testing and development!

---

## Step 1: Create MongoDB Atlas Account

1. Go to https://www.mongodb.com/cloud/atlas/register
2. Sign up for a free account (no credit card needed for free tier)
3. Verify your email

---

## Step 2: Create a Cluster

1. **Log in** to MongoDB Atlas
2. Click **"Build a Database"** or **"Create"**
3. Choose **FREE** tier (M0 Sandbox)
4. Select a **Cloud Provider** (AWS, Google Cloud, or Azure)
5. Choose a **Region** (pick closest to you)
6. Click **"Create"**

Wait 3-5 minutes for cluster to be created.

---

## Step 3: Create Database User

1. In Atlas dashboard, go to **"Database Access"** (left sidebar)
2. Click **"Add New Database User"**
3. Choose **"Password"** authentication
4. Enter:
   - **Username**: `telegcrm` (or any username you like)
   - **Password**: Create a strong password (save this!)
5. Set privileges to **"Read and write to any database"**
6. Click **"Add User"**

---

## Step 4: Whitelist Your IP Address

1. Go to **"Network Access"** (left sidebar)
2. Click **"Add IP Address"**
3. For testing, click **"Allow Access from Anywhere"** (adds `0.0.0.0/0`)
   - ⚠️ For production, use specific IPs
4. Click **"Confirm"**

---

## Step 5: Get Connection String

1. Go to **"Database"** (left sidebar)
2. Click **"Connect"** on your cluster
3. Choose **"Connect your application"**
4. Select **"Driver"**: `.NET / C#`
5. Select **"Version"**: `5.0 or later`
6. Copy the connection string (looks like):
   ```
   mongodb+srv://<username>:<password>@cluster0.xxxxx.mongodb.net/?retryWrites=true&w=majority
   ```

---

## Step 6: Update Connection String

Replace `<username>` and `<password>` in the connection string with your database user credentials:

**Before:**
```
mongodb+srv://<username>:<password>@cluster0.xxxxx.mongodb.net/?retryWrites=true&w=majority
```

**After:**
```
mongodb+srv://telegcrm:YourPassword123@cluster0.xxxxx.mongodb.net/?retryWrites=true&w=majority
```

---

## Step 7: Configure Telegcrm

### Option A: Environment Variable (Recommended)

```bash
export MONGODB_CONNECTION_STRING="mongodb+srv://telegcrm:YourPassword@cluster0.xxxxx.mongodb.net/?retryWrites=true&w=majority"
```

Then run:
```bash
cd /Volumes/Storage/OASIS_CLEAN/Telegcrm/TestServer
dotnet run
```

### Option B: Configuration File

Create `appsettings.json` in TestServer folder:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb+srv://telegcrm:YourPassword@cluster0.xxxxx.mongodb.net/?retryWrites=true&w=majority"
  }
}
```

### Option C: Update Program.cs

Edit `TestServer/Program.cs` and replace the connection string directly (not recommended for production).

---

## Step 8: Test Connection

Run the server:

```bash
cd /Volumes/Storage/OASIS_CLEAN/Telegcrm/TestServer
dotnet run
```

You should see:
```
✅ MongoDB connected successfully
🚀 Telegram CRM Test Server Starting...
```

If you see errors, check:
- Username and password are correct
- IP address is whitelisted
- Connection string format is correct

---

## Security Best Practices

### For Production:

1. **Don't use "Allow Access from Anywhere"**
   - Add specific IP addresses only
   - Or use MongoDB Atlas VPC peering

2. **Use Strong Passwords**
   - At least 12 characters
   - Mix of letters, numbers, symbols

3. **Store Connection String Securely**
   - Use environment variables
   - Never commit to git
   - Use secrets management

4. **Enable Encryption**
   - Atlas encrypts data at rest by default
   - Use TLS/SSL for connections (included in connection string)

---

## Free Tier Limits

MongoDB Atlas Free Tier (M0) includes:
- ✅ 512 MB storage
- ✅ Shared RAM and vCPU
- ✅ No credit card required
- ✅ Perfect for development/testing

**Limits:**
- Storage: 512 MB max
- Connections: Limited
- No dedicated resources

For production with more data, consider paid tiers.

---

## Troubleshooting

### "Authentication failed"
- Check username and password are correct
- Make sure you replaced `<username>` and `<password>` in connection string

### "IP not whitelisted"
- Go to Network Access in Atlas
- Add your current IP address
- Or temporarily allow from anywhere (for testing)

### "Connection timeout"
- Check your internet connection
- Verify cluster is running (check Atlas dashboard)
- Try a different region

### "Database not found"
- This is normal! Telegcrm will create the database automatically
- The database name is `TelegramCRM`

---

## Quick Start Script

Save this as `setup-atlas.sh`:

```bash
#!/bin/bash

echo "🔧 MongoDB Atlas Setup Helper"
echo ""
read -p "Enter your MongoDB Atlas connection string: " CONN_STRING
echo ""
echo "Setting environment variable..."
export MONGODB_CONNECTION_STRING="$CONN_STRING"
echo ""
echo "✅ Connection string set!"
echo ""
echo "Now run: cd TestServer && dotnet run"
```

Make it executable:
```bash
chmod +x setup-atlas.sh
./setup-atlas.sh
```

---

## Next Steps

Once MongoDB Atlas is configured:

1. ✅ Server will connect automatically
2. ✅ Database will be created on first use
3. ✅ Collections will be created automatically
4. ✅ Indexes will be set up
5. ✅ Ready to track conversations!

---

## Need Help?

- MongoDB Atlas Docs: https://docs.atlas.mongodb.com/
- Free Tier Info: https://www.mongodb.com/cloud/atlas/pricing
- Support: Check Atlas dashboard for support options

---

**That's it! Your Telegcrm will now use MongoDB Atlas instead of local MongoDB.** 🎉

