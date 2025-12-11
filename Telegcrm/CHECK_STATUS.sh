#!/bin/bash
echo "Checking Telegram CRM Server Status..."
echo ""

if curl -s http://localhost:5001/api/telegram-crm/contacts > /dev/null 2>&1; then
    echo "✅ Server is RUNNING!"
    echo ""
    echo "📡 API: http://localhost:5001/api/telegram-crm/contacts"
    echo "📚 Swagger: http://localhost:5001/swagger"
    echo ""
    echo "Test it:"
    echo "  curl http://localhost:5001/api/telegram-crm/contacts"
else
    echo "⏳ Server is not responding yet"
    echo ""
    echo "To start it, run:"
    echo "  cd /Volumes/Storage/OASIS_CLEAN/Telegcrm/TestServer"
    echo "  dotnet run"
fi

