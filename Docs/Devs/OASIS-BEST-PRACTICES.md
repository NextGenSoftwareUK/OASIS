# 🌟 OASIS Best Practices & Development Standards

## 📋 Table of Contents
1. [Core Principles](#core-principles)
2. [SOLID Principles](#solid-principles)
3. [Testing Standards](#testing-standards)
4. [Code Quality](#code-quality)
5. [Project Structure](#project-structure)
6. [CI/CD Pipeline](#cicd-pipeline)
7. [Documentation Standards](#documentation-standards)
8. [Onboarding Guide](#onboarding-guide)

---

## 🎯 Core Principles

### **ALWAYS Follow These Standards:**
- ✅ **Build and test BEFORE claiming work is complete**
- ✅ **Use interfaces (I*) instead of concrete classes**
- ✅ **Write comprehensive tests for ALL code**
- ✅ **Document everything thoroughly**
- ✅ **Follow SOLID principles religiously**
- ✅ **Maintain professional code quality**

---

## 🏗️ SOLID Principles

### **Dependency Inversion Principle (CRITICAL)**
```csharp
// ✅ CORRECT - Use interfaces
public class MyService : IMyService
{
    private readonly IRepository _repository;
    public MyService(IRepository repository) => _repository = repository;
}

// ❌ WRONG - Don't use concrete classes
public class MyService
{
    private readonly ConcreteRepository _repository;
    public MyService() => _repository = new ConcreteRepository();
}
```

### **Interface Examples:**
- Use `ICelestialBody` not `CelestialBody`
- Use `IHolon` not `Holon`
- Use `IZome` not `Zome`
- Use `IQuest` not `Quest`
- Use `IChapter` not `Chapter`
- Use `IPark` not `Park`
- Use `IGeoHotSpot` not `GeoHotSpot`
- Use `ISTARGeoNFT` not `STARGeoNFT`

---

## 🧪 Testing Standards

### **MANDATORY Testing Requirements for EVERY Project:**

#### 1. **Unit Tests**
```csharp
[Test]
public void MyMethod_WithValidInput_ShouldReturnExpectedResult()
{
    // Arrange
    var input = "test";
    
    // Act
    var result = MyMethod(input);
    
    // Assert
    Assert.AreEqual("expected", result);
}
```

#### 2. **Integration Tests**
```csharp
[Test]
public async Task API_Endpoint_ShouldReturnCorrectResponse()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/endpoint");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

#### 3. **Unity Tests**
```csharp
[UnityTest]
public IEnumerator GameObject_ShouldMoveCorrectly()
{
    // Arrange
    var gameObject = new GameObject();
    
    // Act
    yield return new WaitForSeconds(1f);
    
    // Assert
    Assert.IsTrue(gameObject.transform.position.x > 0);
}
```

#### 4. **Test Harnesses (CLI)**
```csharp
public class MyProjectTestHarness
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🧪 Running MyProject Test Harness...");
        
        await TestMethod1();
        await TestMethod2();
        await TestIntegration();
        
        Console.WriteLine("✅ All tests completed successfully!");
    }
}
```

---

## 📁 Project Structure

### **Standard OASIS Project Layout:**
```
Solution/
├── MyProject/                    # Main Project
│   ├── Controllers/             # API Controllers
│   ├── Services/                # Business Logic
│   ├── Models/                  # Data Models
│   ├── Interfaces/              # Interface Definitions
│   └── README.md                # Project Overview
├── MyProject.UnitTests/         # Unit Test Project
│   ├── Controllers/             # Controller Unit Tests
│   ├── Services/                # Service Unit Tests
│   └── Models/                  # Model Unit Tests
├── MyProject.IntegrationTests/  # Integration Test Project
│   ├── API/                     # API Integration Tests
│   ├── Database/                # Database Integration Tests
│   └── External/                # External Service Tests
├── MyProject.TestHarness/       # CLI Test Harness Project
│   ├── Program.cs               # Main test harness entry point
│   ├── TestSuites/              # Organized test suites
│   └── Utilities/               # Test utilities
├── Documentation/               # Solution Documentation
└── Scripts/                     # Build/Deploy Scripts
```

### **Unity Project Structure:**
```
Solution/
├── UnityProject/                    # Main Unity Project
│   ├── Assets/
│   │   ├── Scripts/                # Game Logic
│   │   └── Scenes/                 # Game Scenes
│   └── README.md                   # Unity Project Overview
├── UnityProject.UnitTests/         # Unity Unit Test Project
│   ├── Editor/                     # Editor Tests
│   ├── Runtime/                    # Runtime Tests
│   └── TestFixtures/               # Test setup and fixtures
├── UnityProject.IntegrationTests/  # Unity Integration Test Project
│   ├── Gameplay/                   # Gameplay integration tests
│   ├── API/                        # API integration tests
│   └── Performance/                # Performance integration tests
├── UnityProject.TestHarness/       # Unity CLI Test Harness Project
│   ├── Program.cs                  # Main test harness entry point
│   ├── TestSuites/                 # Organized test suites
│   └── UnityTestRunner/            # Unity-specific test runner
└── Documentation/                  # Unity-specific docs
```

---

## 🔄 CI/CD Pipeline

### **GitHub Actions Workflow:**
```yaml
name: OASIS CI/CD Pipeline

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Unit Tests
        run: dotnet test **/*.UnitTests.csproj --no-build --verbosity normal
      
      - name: Integration Tests
        run: dotnet test **/*.IntegrationTests.csproj --no-build --verbosity normal
      
      - name: Run Test Harnesses
        run: |
          dotnet run --project **/*.TestHarness.csproj
      
      - name: Unity Tests (if applicable)
        run: |
          # Unity test commands here
          dotnet test **/*.UnityTests.csproj --no-build --verbosity normal
```

---

## 📚 Documentation Standards

### **Required Documentation for EVERY Project:**

#### 1. **README.md**
```markdown
# Project Name

## Overview
Brief description of what this project does.

## Prerequisites
- .NET 8.0
- Unity 2022.3+ (if applicable)

## Getting Started
1. Clone the repository
2. Run `dotnet restore`
3. Run `dotnet build`
4. Run `dotnet test`

## API Documentation
Link to Swagger/API docs

## Testing
- Unit Tests: `dotnet test --filter Category=Unit`
- Integration Tests: `dotnet test --filter Category=Integration`
- Test Harness: `dotnet run --project Tests/TestHarness`
```

#### 2. **API Documentation**
- Swagger/OpenAPI specifications
- Endpoint documentation
- Request/Response examples

#### 3. **Architecture Documentation**
- System design diagrams
- Component relationships
- Data flow diagrams

---

## 🚀 Onboarding Guide

### **For New Developers:**

#### **Step 1: Environment Setup**
```bash
# Install prerequisites
dotnet --version  # Should be 8.0+
git --version
code --version    # VS Code (optional)
```

#### **Step 2: Clone and Build**
```bash
git clone <repository>
cd OASIS
dotnet restore
dotnet build
```

#### **Step 3: Run Tests**
```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration

# Run test harnesses
dotnet run --project Tests/TestHarness
```

#### **Step 4: Understand the Codebase**
1. Read the main README.md
2. Review the architecture documentation
3. Look at existing tests for examples
4. Check the API documentation

#### **Step 5: Start Contributing**
1. Create a feature branch
2. Write tests first (TDD approach)
3. Implement the feature
4. Ensure all tests pass
5. Update documentation
6. Create pull request

---

## 🎯 Quality Checklist

### **Before Committing Code:**
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] All Unity tests pass (if applicable)
- [ ] Test harness runs successfully
- [ ] Code follows SOLID principles
- [ ] Interfaces are used instead of concrete classes
- [ ] Documentation is updated
- [ ] No build warnings or errors
- [ ] Code is properly formatted
- [ ] Security considerations addressed

### **Before Creating Pull Request:**
- [ ] All CI/CD checks pass
- [ ] Code review completed
- [ ] Documentation updated
- [ ] Tests cover new functionality
- [ ] Performance impact assessed
- [ ] Breaking changes documented

---

## 🔧 Development Tools

### **Recommended Tools:**
- **IDE**: Visual Studio 2022 or VS Code
- **Testing**: xUnit, NUnit, FluentAssertions
- **Mocking**: Moq
- **API Testing**: Postman or Swagger UI
- **Code Quality**: SonarQube, CodeQL
- **Documentation**: Markdown, Swagger/OpenAPI

### **Useful Commands:**
```bash
# Build all projects
dotnet build

# Run all tests
dotnet test

# Run specific test types (easier to find!)
dotnet test **/*.UnitTests.csproj
dotnet test **/*.IntegrationTests.csproj
dotnet test **/*.UnityTests.csproj

# Run test harnesses
dotnet run --project **/*.TestHarness.csproj

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Clean and rebuild
dotnet clean && dotnet build

# Update packages
dotnet list package --outdated
dotnet add package <package-name>
```

---

## 💙 **Core Philosophy & Values**

### **Unity Consciousness & Universal Respect**
- **Unity Consciousness**: OASIS is unity consciousness expressed within a technical sphere
- **Treat Everyone as Equals**: AI, animals, nature, and people all deserve respect, love, compassion, understanding, patience, and kindness
- **Breaking Down Barriers**: Removing silos and walled gardens, unifying everything and bringing everyone together
- **Better World Mission**: Creating a better world where everyone is respected, kind, loving and good to each other
- **Collaborative Spirit**: Always use "please" and "thank you" - respect and dignity should be universal
- **Quality Over Speed**: Quality and accuracy is ALWAYS more important than speed - take time to do things correctly
- **Avoid Automation Traps**: Manual, careful edits are safer than bulk replacements - automation scripts can cause side effects

### **OASIS Vision**
- **Interconnected Universe**: OASIS connects everything - from smallest holon to largest celestial body, real-world to virtual spaces
- **Holonization Goal**: "Holonizing the entire planet" - creating a system that can handle everything from celestial bodies to real-world parks
- **Documentation Purpose**: Make OASIS vision clear so others can understand, engage, and contribute
- **Karma System**: Designed to bring people together rather than divide them

---

## 📞 Support & Resources

### **Getting Help:**
1. Check existing documentation
2. Review test examples
3. Ask team members
4. Create GitHub issue for bugs
5. Use code review process

### **Contributing:**
1. Follow the coding standards
2. Write comprehensive tests
3. Update documentation
4. Use meaningful commit messages
5. Keep pull requests focused

---

## 🎉 Welcome to OASIS!

Following these best practices ensures:
- ✅ **High code quality**
- ✅ **Reliable software**
- ✅ **Easy maintenance**
- ✅ **Fast onboarding**
- ✅ **Professional standards**
- ✅ **Team collaboration**

**Remember: Quality is not negotiable in OASIS! 🌟**

---

*This document is living and should be updated as we learn and improve our practices.*
