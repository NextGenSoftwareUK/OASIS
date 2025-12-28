# Services Status

**Last Updated:** $(date)

## ✅ Running Services

### 1. Smart Contract Generator API
- **Status:** ✅ Running
- **URL:** http://localhost:5000
- **Swagger:** http://localhost:5000/swagger
- **Port:** 5000
- **Log:** `/tmp/scgen-api.log`
- **PID:** Check with `lsof -ti :5000`

### 2. Portal Server
- **Status:** ✅ Running  
- **URL:** http://localhost:8080/portal.html
- **Port:** 8080
- **Log:** `/tmp/portal-server.log`
- **PID:** Check with `lsof -ti :8080`

### 3. Smart Contract Generator UI
- **Status:** ⏳ Starting (Next.js compilation takes 30-60 seconds)
- **URL:** http://localhost:3001
- **Port:** 3001
- **Log:** `/tmp/scgen-ui.log`
- **PID:** Check with `lsof -ti :3001`

---

## 🚀 Quick Start

### Option 1: Use the Startup Script
```bash
./start-services.sh
```

### Option 2: Start Manually

**Terminal 1 - API:**
```bash
cd SmartContractGenerator
dotnet run --project src/SmartContractGen/ScGen.API/ScGen.API.csproj
```

**Terminal 2 - UI:**
```bash
cd SmartContractGenerator/ScGen.UI
npm run dev
```

**Terminal 3 - Portal:**
```bash
cd portal
python3 -m http.server 8080
```

---

## 🔍 Check Status

### Check if services are responding:
```bash
curl http://localhost:5000/swagger/index.html  # API
curl http://localhost:3001                     # UI
curl http://localhost:8080/portal.html         # Portal
```

### View logs:
```bash
tail -f /tmp/scgen-api.log      # API logs
tail -f /tmp/scgen-ui.log       # UI logs
tail -f /tmp/portal-server.log  # Portal logs
```

### Stop services:
```bash
lsof -ti :5000 | xargs kill -9  # API
lsof -ti :3001 | xargs kill -9  # UI
lsof -ti :8080 | xargs kill -9  # Portal
```

---

## 📝 Usage

1. **Open Portal:** http://localhost:8080/portal.html
2. **Log in** with your OASIS avatar
3. **Click "Smart Contracts" tab**
4. **Generate, compile, and deploy** contracts!

---

## ⚠️ Notes

- **UI startup:** Next.js takes 30-60 seconds to compile on first run
- **API startup:** Usually takes 5-10 seconds
- **Portal:** Starts immediately (static files)
- **Dependencies:** Ensure Node.js and .NET 9.0 are installed


