# Quick MongoDB Atlas Setup

## 5-Minute Setup

### 1. Sign Up
Go to: https://www.mongodb.com/cloud/atlas/register
- Free account, no credit card needed

### 2. Create Cluster
- Click "Build a Database"
- Choose **FREE** tier
- Pick a region
- Click "Create" (wait 3-5 minutes)

### 3. Create User
- Go to "Database Access"
- Click "Add New Database User"
- Username: `telegcrm`
- Password: (create one, save it!)
- Click "Add User"

### 4. Allow Access
- Go to "Network Access"
- Click "Add IP Address"
- Click "Allow Access from Anywhere" (for testing)
- Click "Confirm"

### 5. Get Connection String
- Go to "Database"
- Click "Connect" on your cluster
- Choose "Connect your application"
- Copy the connection string

### 6. Update Connection String
Replace `<username>` and `<password>`:

```
mongodb+srv://telegcrm:YOUR_PASSWORD@cluster0.xxxxx.mongodb.net/?retryWrites=true&w=majority
```

### 7. Set Environment Variable

```bash
export MONGODB_CONNECTION_STRING="mongodb+srv://telegcrm:YOUR_PASSWORD@cluster0.xxxxx.mongodb.net/?retryWrites=true&w=majority"
```

### 8. Run Server

```bash
cd /Volumes/Storage/OASIS_CLEAN/Telegcrm/TestServer
dotnet run
```

**Done!** ✅

See `MONGODB_ATLAS_SETUP.md` for detailed instructions.

