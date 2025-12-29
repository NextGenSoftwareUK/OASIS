# Frontend Developer Documentation

**Start here!** This is your entry point to all frontend documentation.

---

## 📚 Main Documentation

### 1. **FRONTEND_DEVELOPER_GUIDE.md** ⭐ START HERE
Complete guide with:
- Quick start examples
- Working endpoints
- TypeScript API client
- Authentication flow
- Code examples

**👉 Read this first!**

---

### 2. **API_ROUTES_REFERENCE.md**
Complete route matrix showing:
- Which endpoints can be called directly from client
- Which should go through Next.js API routes
- Auth requirements for each endpoint

---

### 3. **API_USAGE_GUIDE.md**
Detailed usage examples with:
- cURL commands
- Request/response formats
- Error handling
- Frontend integration examples

---

### 4. **FRONTEND_ENDPOINTS_STATUS.md**
Current status of all endpoints:
- ✅ Working endpoints (14)
- ❌ Broken endpoints (5)
- ⏳ Not tested yet (22)

---

## 🔧 Setup & Configuration

### 5. **CREATE_ADMIN_ACCOUNT.md**
How to create admin accounts for testing/admin operations.

---

## 📊 Quick Summary

**Base URL:** `https://pangea-production-128d.up.railway.app/api`

**Working Endpoints:**
- ✅ Authentication (register, login, forgot-password)
- ✅ User profile (GET)
- ✅ Assets (list)
- ✅ Orders (all, open, history)
- ✅ Trades (all, history, statistics)
- ✅ Transactions (all)

**Architecture:**
- **Read (GET):** Call directly from frontend with JWT token
- **Write (POST/PUT/DELETE):** Proxy through Next.js API routes

---

## 🚀 Quick Start

1. Read **FRONTEND_DEVELOPER_GUIDE.md**
2. Check **API_ROUTES_REFERENCE.md** for endpoint access patterns
3. Review **FRONTEND_ENDPOINTS_STATUS.md** for what's working
4. Use **API_USAGE_GUIDE.md** for detailed examples

---

## 📞 Questions?

- Check Railway logs for backend errors
- Review error responses for detailed messages
- See individual documentation files for specific topics



