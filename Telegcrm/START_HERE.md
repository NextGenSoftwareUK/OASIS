# 🚀 Start Telegram CRM Server

## Quick Start

**From the Telegcrm directory, run:**

```bash
./start-crm-test.sh
```

**Or manually:**

```bash
cd TestServer
dotnet run
```

## Important Note

⚠️ **Don't run `dotnet run` from the main `Telegcrm` folder** - that's a library project, not executable.

✅ **Run from `Telegcrm/TestServer`** - that's the runnable server project.

## Once Running

- **API**: http://localhost:5001/api/telegram-crm/contacts
- **Swagger UI**: http://localhost:5001/swagger

## Check Status

```bash
curl http://localhost:5001/api/telegram-crm/contacts
```

If you see JSON (even `[]`), it's working! 🎉

