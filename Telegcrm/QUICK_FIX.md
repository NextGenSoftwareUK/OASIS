# Quick Fix - Server Startup

## The Issue
Your terminal is stuck in a quote state showing `cmdand cmdand dquote>`. 

## Solution

**Press `Ctrl+C` in your terminal to exit the stuck command.**

Then run:

```bash
cd /Volumes/Storage/OASIS_CLEAN/Telegcrm
./run.sh
```

Or manually:

```bash
cd /Volumes/Storage/OASIS_CLEAN/Telegcrm/TestServer
dotnet run
```

## Alternative: Check if Already Running

The server might already be running in the background. Check:

```bash
curl http://localhost:5001/api/telegram-crm/contacts
```

If you get JSON back, it's running! Then open:
- http://localhost:5001/swagger

## If Build Still Fails

If you see build errors, run:

```bash
cd /Volumes/Storage/OASIS_CLEAN/Telegcrm/TestServer
dotnet clean
dotnet restore
dotnet build
dotnet run
```

---

**The server should start on http://localhost:5001 once the build succeeds!**

