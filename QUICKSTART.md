# OASIS Quick Start Guide

**Get started with OASIS in under 5 minutes**

---

## ⚡ 30-Second API Test

Verify the API is live right now:

```bash
curl https://api.oasisweb4.one/health
```

**Expected Response:**
```json
{
  "status": "Healthy",
  "version": "1.0"
}
```

✅ **If you see this, OASIS is running and you can continue!**

---

## 🚀 Your First OASIS Integration

### Step 1: Create an Avatar (User Account)

Every OASIS interaction needs an avatar. Think of it as your universal user ID.

```bash
curl -X POST "https://api.oasisweb4.one/api/avatar/register" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "myapp_'$(date +%s)'",
    "email": "test'$(date +%s)'@example.com",
    "password": "securePass123",
    "firstName": "Test",
    "lastName": "User",
    "avatarType": "User",
    "acceptTerms": true
  }'
```

**Response:**
```json
{
  "success": true,
  "avatar": {
    "id": "a1b2c3d4-...",
    "username": "myapp_1699...",
    "email": "test1699...@example.com"
  },
  "jwtToken": "eyJhbGciOi..."
}
```

**Save these values:**
- `avatar.id` - Your avatar ID
- `jwtToken` - Your authentication token

---

### Step 2: Authenticate (For Future Sessions)

```bash
curl -X POST "https://api.oasisweb4.one/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "YOUR_USERNAME",
    "password": "securePass123"
  }'
```

---

### Step 3: Save Data

Save data to OASIS. It automatically goes to multiple providers:

```bash
curl -X POST "https://api.oasisweb4.one/api/data/save-holon" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "name": "MyFirstData",
    "description": "Testing OASIS",
    "holonType": "TestData",
    "metadata": {
      "message": "Hello OASIS!",
      "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
    }
  }'
```

**Response:**
```json
{
  "result": {
    "id": "holon-uuid-here",
    "name": "MyFirstData",
    "metadata": {
      "message": "Hello OASIS!",
      "timestamp": "2025-11-06T..."
    }
  },
  "isError": false,
  "message": "Holon saved successfully"
}
```

**What just happened:**
- Your data was saved to MongoDB (fast primary database)
- Backed up to Arbitrum blockchain (immutable proof)
- Will failover to Ethereum if MongoDB goes down

**Save the holon ID** from the response.

---

### Step 4: Load Your Data Back

```bash
curl -X GET "https://api.oasisweb4.one/api/data/load-holon/YOUR_HOLON_ID" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Response:**
```json
{
  "result": {
    "id": "YOUR_HOLON_ID",
    "name": "MyFirstData",
    "metadata": {
      "message": "Hello OASIS!",
      "timestamp": "2025-11-06T..."
    }
  },
  "isError": false
}
```

---

## 🎉 That's It!

**You just:**
1. ✅ Created an avatar (universal user ID)
2. ✅ Saved data to multiple providers
3. ✅ Retrieved it with automatic failover

**Your data is now stored on:**
- MongoDB (fast access)
- Arbitrum blockchain (immutable backup)
- Will survive even if MongoDB goes down (auto-failover to Ethereum)

---

## 📱 Complete JavaScript Example

```javascript
// oasis-quickstart.js
const OASIS_API = 'https://api.oasisweb4.one';

async function quickstart() {
  
  // Step 1: Create avatar
  const registerResponse = await fetch(`${OASIS_API}/api/avatar/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      username: `testuser_${Date.now()}`,
      email: `test${Date.now()}@example.com`,
      password: 'securePass123',
      firstName: 'Test',
      lastName: 'User',
      avatarType: 'User',
      acceptTerms: true
    })
  });
  
  const { avatar, jwtToken } = await registerResponse.json();
  console.log('✓ Avatar created:', avatar.id);
  console.log('✓ JWT Token:', jwtToken.substring(0, 20) + '...');
  
  // Step 2: Save data
  const saveResponse = await fetch(`${OASIS_API}/api/data/save-holon`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${jwtToken}`
    },
    body: JSON.stringify({
      name: 'MyTestData',
      holonType: 'TestData',
      metadata: {
        message: 'Hello OASIS!',
        timestamp: new Date().toISOString()
      }
    })
  });
  
  const saveResult = await saveResponse.json();
  const holonId = saveResult.result.id;
  console.log('✓ Data saved with ID:', holonId);
  
  // Step 3: Load data back
  const loadResponse = await fetch(
    `${OASIS_API}/api/data/load-holon/${holonId}`,
    {
      headers: { 'Authorization': `Bearer ${jwtToken}` }
    }
  );
  
  const loadResult = await loadResponse.json();
  console.log('✓ Data loaded:', loadResult.result.metadata);
  
  console.log('\n🎉 Success! You just used OASIS multi-provider storage!');
}

quickstart().catch(console.error);
```

**Run it:**
```bash
node oasis-quickstart.js
```

---

## 🐍 Python Example

```python
import requests
import time

OASIS_API = 'https://api.oasisweb4.one'

# Step 1: Create avatar
register_response = requests.post(
    f'{OASIS_API}/api/avatar/register',
    json={
        'username': f'testuser_{int(time.time())}',
        'email': f'test{int(time.time())}@example.com',
        'password': 'securePass123',
        'firstName': 'Test',
        'lastName': 'User',
        'avatarType': 'User',
        'acceptTerms': True
    }
)

data = register_response.json()
avatar_id = data['avatar']['id']
jwt_token = data['jwtToken']
print(f'✓ Avatar created: {avatar_id}')

# Step 2: Save data
save_response = requests.post(
    f'{OASIS_API}/api/data/save-holon',
    headers={
        'Authorization': f'Bearer {jwt_token}',
        'Content-Type': 'application/json'
    },
    json={
        'name': 'MyTestData',
        'holonType': 'TestData',
        'metadata': {
            'message': 'Hello OASIS!',
            'timestamp': time.strftime('%Y-%m-%dT%H:%M:%SZ')
        }
    }
)

save_data = save_response.json()
holon_id = save_data['result']['id']
print(f'✓ Data saved: {holon_id}')

# Step 3: Load data
load_response = requests.get(
    f'{OASIS_API}/api/data/load-holon/{holon_id}',
    headers={'Authorization': f'Bearer {jwt_token}'}
)

load_data = load_response.json()
print(f'✓ Data loaded: {load_data["result"]["metadata"]}')
print('\n🎉 Success! OASIS is working!')
```

---

## 🔍 What's Actually Happening?

### When You Save Data:

```
Your API Call
     ↓
OASIS API (api.oasisweb4.one)
     ↓
Automatically saves to:
  1. MongoDBOASIS (primary - fast)
  2. ArbitrumOASIS (blockchain backup)
  3. EthereumOASIS (if Arbitrum fails)
```

**Current Configuration:**
- **Primary:** MongoDB (millisecond response)
- **Failover:** Arbitrum → Ethereum
- **Auto-Replication:** MongoDB only (others on request)

### When You Load Data:

```
Your API Call
     ↓
OASIS tries MongoDB → If down, tries Arbitrum → If down, tries Ethereum
     ↓
Returns data from first available provider
```

**Result:** Your app keeps working even if MongoDB is down.

---

## ❓ Common Questions

### Q: Is this free?

**A:** Currently yes for developers. Rate limits apply (check with @maxgershfield on Telegram for production use).

### Q: Where is my data stored?

**A:** 
- Primary: MongoDB (OASIS managed)
- Backup: Arbitrum blockchain (Sepolia testnet currently)
- Optional: Can configure additional providers

### Q: How do I add more providers?

**A:** Contact @maxgershfield on Telegram. Additional providers can be configured per your needs (Solana, Polygon, IPFS, etc.)

### Q: Is this production-ready?

**A:** The core API is stable. Multi-provider replication is currently configured for MongoDB + Arbitrum. For production deployments with specific provider requirements, contact @maxgershfield.

### Q: What if I want to use ONLY Solana?

**A:** You can specify the provider in your API calls:
```bash
curl -X POST "https://api.oasisweb4.one/api/data/save-holon/SolanaOASIS/false" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{ ... }'
```

### Q: Can I self-host OASIS?

**A:** Yes! See the [Deployment Guide](DOCKER_DEPLOYMENT_GUIDE.md) for full instructions.

---

## 📚 Next Steps

**Now that you've tested the basics:**

1. Read the [Provider Architecture Guide](OASIS_PROVIDER_ARCHITECTURE_GUIDE.md) for deep dive
2. Explore [API Reference](OASIS_API_COMPLETE_ENDPOINTS_SUMMARY.md) for all 500+ endpoints
3. Check [HyperDrive Architecture](HYPERDRIVE_ARCHITECTURE_DIAGRAM.md) to understand how it works
4. Contact @maxgershfield on Telegram for production setup

---

## 🆘 Getting Help

**If something doesn't work:**

1. Check the response for error messages
2. Verify your JWT token hasn't expired (24 hour lifetime)
3. Check API status: `curl https://api.oasisweb4.one/health`
4. Contact @maxgershfield on Telegram

---

## 🎯 Real-World Use Cases

**Once you're comfortable with the basics, OASIS enables:**

- **Multi-chain games:** Save progress once, play from any blockchain
- **Cross-chain social:** Users on different chains can interact
- **Decentralized apps:** Data persists even if your primary database fails
- **Blockchain backups:** Automatic immutable backups to multiple chains
- **Future-proof apps:** When OASIS adds new providers, your app gets them automatically

---

**Ready to build? Start with the curl commands above and see it work in 30 seconds!**

**Questions?** Telegram: @maxgershfield

