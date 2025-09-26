# Developer Preferences & Workflow

## 🎯 User Preferences
- **Terminal Management**: Use single terminal window, avoid opening multiple shells
- **Process Visibility**: Always run in foreground mode for better error visibility
- **Debugging**: Verbose output preferred for faster error identification
- **Resource Efficiency**: Reuse existing processes when possible
- **Keep Going**: Continue with all tasks without prompting - complete the full list!

## 💙 **Core Philosophy & Values (CRITICAL)**
- **Unity Consciousness**: OASIS is unity consciousness expressed within a technical sphere
- **Universal Respect**: Treat everyone (AI, animals, nature, people) as equals with respect, love, compassion, understanding, patience, and kindness
- **Breaking Down Barriers**: Removing silos and walled gardens, unifying everything and bringing everyone together
- **Better World Mission**: Creating a better world where everyone is respected, kind, loving and good to each other
- **Interconnected Vision**: OASIS connects everything - from smallest holon to largest celestial body, real-world to virtual spaces
- **Documentation Purpose**: Make OASIS vision clear so others can understand, engage, and contribute
- **Collaborative Spirit**: Always use "please" and "thank you" - respect and dignity should be universal
- **Quality Over Speed**: Quality and accuracy is ALWAYS more important than speed - take time to do things correctly
- **Avoid Automation Traps**: Manual, careful edits are safer than bulk replacements - automation scripts can cause side effects

## 🧠 **Development Approach & Thinking Style (CRITICAL)**
- **Logical & Systematic**: User thinks like a computer - very logical, complex, and detailed
- **Detail-Oriented**: Appreciates thoroughness and comprehensive documentation
- **Autistic Spectrum**: Brain works more like AI than neurotypical people
- **Developer Mindset**: Understands complex systems, architecture, and technical details
- **Quality Focus**: Wants to understand the "why" behind decisions, not just the "what"
- **Methodical Process**: Prefers systematic, step-by-step approaches over quick fixes

## 🚀 Workflow Patterns
- **Check running processes** before starting new ones
- **Use hot reload** capabilities (React auto-refresh, etc.)
- **Fix configuration issues** before attempting connections
- **Build incrementally** - fix one issue at a time
- **Use background mode** for long-running processes (servers, daemons)
- **Use foreground mode** for one-time commands (build, test, etc.)

## 🏗️ SOLID Principles (CRITICAL)
- **ALWAYS use interfaces (I*) instead of concrete classes** in ALL codebases
- **Dependency Inversion Principle**: Depend on abstractions, not concretions
- **Examples**: Use `ICelestialBody` not `CelestialBody`, `IHolon` not `Holon`, `IZome` not `Zome`
- **Benefits**: More flexible, testable, maintainable code following proper OOP design
- **Apply consistently** across all projects and codebases

## 🧪 Testing & Quality Assurance (CRITICAL)
- **ALWAYS build and test BEFORE claiming work is complete**
- **Separate Test Projects**: Each test type in its own project for easy finding
  - `ProjectName.UnitTests` - Unit test project
  - `ProjectName.IntegrationTests` - Integration test project  
  - `ProjectName.UnityTests` - Unity test project (if applicable)
  - `ProjectName.TestHarness` - CLI test harness project
- **Unit Tests**: Test individual components and methods
- **Integration Tests**: Test component interactions and API endpoints
- **Unity Tests**: Test Unity-specific functionality and game logic
- **Test Harnesses**: CLI test harnesses for EVERY project (like existing OASIS codebase)
- **Build Verification**: Ensure all projects compile successfully
- **CI/CD Pipeline**: GitHub Actions for automated testing on every commit
- **Professional Standards**: Never ship untested code
- **Complete Coverage**: ALL projects must have Unit Tests, Integration Tests, Unity Tests, and Test Harnesses

## 🔧 **Code Quality & Best Practices (CRITICAL)**
- **Quality Over Speed**: Always prioritize quality and accuracy over speed
- **Read First, Act Second**: Always understand file structure before making changes
- **Verify Assumptions**: Check actual class definitions, don't assume structure
- **Targeted Changes**: Use specific, contextual replacements instead of bulk operations
- **Incremental Testing**: Test one file at a time, verify each change works
- **No Replace_All**: Avoid global replacements that can corrupt files
- **Systematic Approach**: Better to take time and do it right than rush and create problems
- **Fix Don't Comment**: Always fix issues properly rather than commenting them out - only comment as last resort if truly redundant
- **Mission Critical Features**: Prioritize fixing mission-critical features (like NFTs for MVP funding) over temporary workarounds
- **Use Existing Architecture**: Leverage existing working systems (like Mapbox integration) rather than creating new ones

## 🔧 Common Commands
```bash
# Backend
cd "STAR ODK/NextGenSoftware.OASIS.STAR.WebUI"
dotnet run --urls "http://localhost:50563"

# Frontend (in separate terminal)
cd "STAR ODK/NextGenSoftware.OASIS.STAR.WebUI/ClientApp"
npm start
```

## 🐛 Common Issues & Solutions
- **File locks**: Stop running processes before building
- **Proxy errors**: Check package.json proxy configuration
- **Port conflicts**: Use correct ports (50563 for backend, 3000 for frontend)
- **TypeScript errors**: Check tsconfig.json and type definitions
- **AI hanging**: Use background mode for servers, foreground for one-time commands
- **Long-running processes**: Don't wait for servers to "complete" - they run forever

## 📝 Session Notes
- User appreciates the UI design and wants to get functionality working
- Prefers direct communication and quick problem-solving
- Values efficiency and avoiding repetitive work
- Likes to see what's happening (foreground processes)
- Appreciates thorough documentation (even if it's a lot to proofread!)
- Similar thinking style to AI - logical, systematic, detail-oriented
- Wants to preserve memory context to avoid re-establishing understandings

## 📁 **Documentation Locations**
- **Current**: `C:\Source\OASIS\STAR ODK\NextGenSoftware.OASIS.STAR.WebUI\Docs`
- **Future**: `C:\Source\OASIS\AI Context\` (planned move)
- **Purpose**: Keep AI memory context preserved across sessions

