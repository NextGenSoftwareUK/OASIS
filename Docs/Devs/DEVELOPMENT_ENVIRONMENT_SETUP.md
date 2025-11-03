# Development Environment Setup Guide

## 📋 **Overview**

This guide provides comprehensive instructions for setting up a complete development environment for OASIS and STARNET development.

## 🛠️ **Prerequisites**

### **System Requirements**
- **Operating System**: Windows 10/11, macOS 10.15+, or Linux (Ubuntu 20.04+)
- **RAM**: Minimum 8GB, Recommended 16GB+
- **Storage**: At least 20GB free space
- **Network**: Stable internet connection

### **Required Software**
- **Node.js**: Version 18.x or higher
- **.NET SDK**: Version 9.0 or higher
- **Git**: Latest version
- **Docker**: Optional but recommended
- **Visual Studio Code**: Recommended IDE

## 🚀 **Quick Setup**

### **1. Install Node.js**
```bash
# Download from https://nodejs.org/
# Or use package manager:

# Windows (Chocolatey)
choco install nodejs

# macOS (Homebrew)
brew install node

# Linux (Ubuntu)
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt-get install -y nodejs
```

### **2. Install .NET SDK**
```bash
# Download from https://dotnet.microsoft.com/download
# Or use package manager:

# Windows (Chocolatey)
choco install dotnet-sdk

# macOS (Homebrew)
brew install --cask dotnet-sdk

# Linux (Ubuntu)
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0
```

### **3. Install Git**
```bash
# Windows
choco install git

# macOS
brew install git

# Linux
sudo apt-get install git
```

## 🏗️ **Project Setup**

### **1. Clone Repository**
```bash
git clone https://github.com/oasisplatform/OASIS.git
cd OASIS
```

### **2. Install Dependencies**

#### **Backend (STAR Web API)**
```bash
cd "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI"
dotnet restore
dotnet build
```

#### **Frontend (STARNET Web UI)**
```bash
cd "STAR ODK/NextGenSoftware.OASIS.STAR.WebUI/ClientApp"
npm install
npm run build
```

### **3. Environment Configuration**

#### **API Configuration**
Create `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=oasis.db"
  },
  "OASIS": {
    "ProviderType": "SQLLiteDB",
    "ConnectionString": "Data Source=oasis.db"
  }
}
```

#### **UI Configuration**
Create `.env.local`:
```bash
REACT_APP_API_URL=http://localhost:5099
REACT_APP_WEB4_API_URL=http://localhost:5000
REACT_APP_HUB_URL=http://localhost:5001
```

## 🐳 **Docker Setup (Optional)**

### **1. Docker Compose**
Create `docker-compose.yml`:
```yaml
version: '3.8'
services:
  api:
    build: ./STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI
    ports:
      - "5099:5099"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    volumes:
      - ./data:/app/data

  ui:
    build: ./STAR ODK/NextGenSoftware.OASIS.STAR.WebUI
    ports:
      - "3000:3000"
    environment:
      - REACT_APP_API_URL=http://api:5099
    depends_on:
      - api

  database:
    image: postgres:15
    environment:
      POSTGRES_DB: oasis
      POSTGRES_USER: oasis
      POSTGRES_PASSWORD: oasis
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

volumes:
  postgres_data:
```

### **2. Run with Docker**
```bash
docker-compose up -d
```

## 🔧 **IDE Configuration**

### **Visual Studio Code**

#### **Recommended Extensions**
```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.vscode-dotnet-runtime",
    "bradlc.vscode-tailwindcss",
    "esbenp.prettier-vscode",
    "ms-vscode.vscode-typescript-next",
    "ms-vscode.vscode-json"
  ]
}
```

#### **Settings**
Create `.vscode/settings.json`:
```json
{
  "dotnet.defaultSolution": "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/NextGenSoftware.OASIS.STAR.WebAPI.sln",
  "typescript.preferences.importModuleSpecifier": "relative",
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.fixAll.eslint": true
  }
}
```

## 🧪 **Testing Setup**

### **1. Backend Testing**
```bash
cd "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI"
dotnet test
```

### **2. Frontend Testing**
```bash
cd "STAR ODK/NextGenSoftware.OASIS.STAR.WebUI/ClientApp"
npm test
```

### **3. Integration Testing**
```bash
# Run API tests
cd "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI"
dotnet test --filter "Category=Integration"

# Run UI tests
cd "STAR ODK/NextGenSoftware.OASIS.STAR.WebUI/ClientApp"
npm run test:integration
```

## 🚀 **Running the Applications**

### **1. Start Backend API**
```bash
cd "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI"
dotnet run --urls "http://localhost:5099"
```

### **2. Start Frontend UI**
```bash
cd "STAR ODK/NextGenSoftware.OASIS.STAR.WebUI/ClientApp"
npm start
```

### **3. Verify Setup**
- **API**: http://localhost:5099/swagger
- **UI**: http://localhost:3000
- **Health Check**: http://localhost:5099/health

## 🔍 **Debugging**

### **Backend Debugging**
1. Set breakpoints in Visual Studio Code
2. Press F5 to start debugging
3. Use the debug console for inspection

### **Frontend Debugging**
1. Open browser developer tools
2. Use React Developer Tools extension
3. Set breakpoints in VS Code

### **Common Issues**

#### **Port Conflicts**
```bash
# Check what's using port 5099
netstat -ano | findstr :5099

# Kill process if needed
taskkill /PID <PID> /F
```

#### **Node Modules Issues**
```bash
# Clear cache and reinstall
rm -rf node_modules package-lock.json
npm install
```

#### **.NET Build Issues**
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

## 📊 **Performance Optimization**

### **Backend Optimization**
- Enable response compression
- Configure caching
- Optimize database queries
- Use async/await patterns

### **Frontend Optimization**
- Enable code splitting
- Optimize bundle size
- Use lazy loading
- Implement caching strategies

## 🔐 **Security Configuration**

### **Development Security**
```json
{
  "Security": {
    "CORS": {
      "AllowedOrigins": ["http://localhost:3000"],
      "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
      "AllowedHeaders": ["*"]
    },
    "Authentication": {
      "JWT": {
        "SecretKey": "your-secret-key",
        "ExpiryMinutes": 60
      }
    }
  }
}
```

### **Environment Variables**
```bash
# Never commit these to version control
export JWT_SECRET_KEY="your-secret-key"
export DATABASE_URL="your-database-url"
export API_KEY="your-api-key"
```

## 📚 **Additional Resources**

### **Documentation**
- **[API Documentation](./API%20Documentation/)** - Complete API reference
- **[Tutorials](./TUTORIALS/)** - Step-by-step guides
- **[Best Practices](./OASIS-BEST-PRACTICES.md)** - Development guidelines

### **Community**
- **Discord**: [Join our Discord](https://discord.gg/oasis)
- **GitHub**: [Contribute on GitHub](https://github.com/oasisplatform)
- **Documentation**: [docs.oasisplatform.world](https://docs.oasisplatform.world)

## 🆘 **Troubleshooting**

### **Common Problems**

#### **Build Failures**
- Check .NET SDK version
- Verify Node.js version
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Clear npm cache: `npm cache clean --force`

#### **Runtime Errors**
- Check environment variables
- Verify database connections
- Review log files
- Check firewall settings

#### **Performance Issues**
- Monitor memory usage
- Check CPU utilization
- Review database queries
- Optimize asset loading

### **Getting Help**
- **Documentation**: Check the docs first
- **Community**: Ask on Discord
- **Issues**: Report on GitHub
- **Support**: Contact support@oasisplatform.world

---

*This setup guide ensures you have a complete development environment for OASIS and STARNET development.*
