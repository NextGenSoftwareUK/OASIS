# OASIS Documentation Hub

**Welcome to the OASIS documentation!** This directory contains comprehensive documentation for developers, AI agents, and users of the OASIS platform.

---

## 🎯 Quick Navigation

### For AI Agents
👉 **[Start Here: Agent Documentation](agent/README.md)**

Designed specifically for AI agents with structured, machine-readable content:
- [Quick Start](agent/QUICK_START.md) - OASIS in 5 minutes
- **[API Overview](agent/API_OVERVIEW.md)** - **Complete API catalog (500+ endpoints)**
- [Core Concepts](agent/CORE_CONCEPTS.md) - Key concepts explained
- [Architecture Overview](agent/ARCHITECTURE_OVERVIEW.md) - System architecture
- [How OASIS Works](agent/HOW_OASIS_WORKS.md) - Implementation details
- [Common Tasks](agent/COMMON_TASKS.md) - Integration patterns

### For Developers
👉 **[Developer Documentation](../Docs/Devs/README.md)**

Comprehensive developer resources:
- Getting Started guides
- API Reference documentation
- Tutorials and examples
- Architecture documentation
- Best practices

### For Concept Deep Dives
👉 **[Core Concepts](concepts/)**

Detailed explanations of OASIS concepts:
- [Providers](concepts/PROVIDERS.md) - Provider system
- [HyperDrive](concepts/HYPERDRIVE.md) - Auto-failover system
- [Managers](concepts/MANAGERS.md) - Manager APIs
- [Holons](concepts/HOLONS.md) - Data model
- [Avatars](concepts/AVATARS.md) - Identity system
- [DNA](concepts/DNA.md) - Configuration system

---

## 📁 Documentation Structure

```
docs/
├── README.md (this file)              # Documentation hub
├── DOCUMENTATION_ANALYSIS_REPORT.md   # Analysis of current docs
│
├── agent/                             # AI Agent Documentation
│   ├── README.md                      # Agent docs index
│   ├── QUICK_START.md                 # 5-minute overview
│   ├── API_OVERVIEW.md                # Complete API catalog (500+ endpoints)
│   ├── CORE_CONCEPTS.md               # Key concepts
│   ├── ARCHITECTURE_OVERVIEW.md       # System architecture
│   ├── HOW_OASIS_WORKS.md            # Implementation details
│   └── COMMON_TASKS.md                # Integration patterns
│
├── developers/                        # Developer Documentation
│   └── (links to ../Docs/Devs/)
│
├── concepts/                          # Core Concepts
│   ├── PROVIDERS.md                   # Provider system
│   ├── HYPERDRIVE.md                  # HyperDrive system
│   ├── MANAGERS.md                    # Manager system
│   ├── HOLONS.md                      # Holon data model
│   ├── AVATARS.md                     # Avatar system
│   └── DNA.md                         # OASIS DNA
│
├── reference/                         # Reference Documentation
│   ├── API/                           # API reference
│   ├── PROVIDERS/                     # Provider reference
│   ├── CONFIGURATION.md               # Configuration reference
│   └── ERRORS.md                      # Error codes
│
└── guides/                            # How-To Guides
    ├── DEPLOYMENT.md                  # Deployment guides
    ├── PROVIDER_SETUP.md              # Provider setup
    ├── TROUBLESHOOTING.md             # Troubleshooting
    └── MIGRATION.md                   # Migration guides
```

---

## 📊 Documentation Status

### ✅ Complete
- [Agent Quick Start](agent/QUICK_START.md) - High-level overview for agents
- [API Overview](agent/API_OVERVIEW.md) - Complete API catalog (500+ endpoints)
- [Documentation Analysis Report](DOCUMENTATION_ANALYSIS_REPORT.md) - Comprehensive analysis

### ⚠️ In Progress
- Core concept documents (PROVIDERS, HYPERDRIVE, etc.)
- Agent architecture and implementation docs
- Reference documentation consolidation

### 📝 Planned
- Detailed concept explanations
- Complete API reference reorganization
- Troubleshooting guides
- Migration documentation

---

## 🚀 Getting Started

### I'm an AI Agent
1. Read [Agent Quick Start](agent/QUICK_START.md)
2. Review [Core Concepts](agent/CORE_CONCEPTS.md)
3. Check [Architecture Overview](agent/ARCHITECTURE_OVERVIEW.md) for system design
4. Use [Common Tasks](agent/COMMON_TASKS.md) for integration examples

### I'm a Developer
1. Read the [Quick Start Guide](../QUICKSTART.md) in the root directory
2. Review [Developer Documentation](../Docs/Devs/README.md)
3. Check [API Reference](../Docs/Devs/API%20Documentation/)
4. Explore [Tutorials](../Docs/Devs/TUTORIALS/)

### I Want to Understand OASIS
1. Start with [Agent Quick Start](agent/QUICK_START.md) - Clear, structured overview
2. Dive into [Core Concepts](concepts/) for detailed explanations
3. Read [Architecture Overview](agent/ARCHITECTURE_OVERVIEW.md)
4. Explore existing [whitepapers](../Docs/)

---

## 🔍 Finding What You Need

### By Topic

**Understanding OASIS:**
- What is OASIS? → [Agent Quick Start](agent/QUICK_START.md)
- How does it work? → [How OASIS Works](agent/HOW_OASIS_WORKS.md)
- Architecture → [Architecture Overview](agent/ARCHITECTURE_OVERVIEW.md)

**Providers:**
- Overview → [Agent Quick Start](agent/QUICK_START.md) → Providers section
- Detailed → [Providers Concept](concepts/PROVIDERS.md)
- Reference → [Provider Status](../Docs/Devs/OASIS_Provider_Development_Guide.md)

**HyperDrive:**
- Overview → [Agent Quick Start](agent/QUICK_START.md) → HyperDrive section
- Detailed → [HyperDrive Concept](concepts/HYPERDRIVE.md)
- Whitepaper → [HyperDrive Whitepaper](../Docs/OASIS_HYPERDRIVE_WHITEPAPER.md)

**Integration:**
- Quick Start → [Agent Quick Start](agent/QUICK_START.md) → Common Use Cases
- Examples → [Common Tasks](agent/COMMON_TASKS.md)
- API Docs → [API Reference](../Docs/Devs/API%20Documentation/)
- Tutorials → [Developer Tutorials](../Docs/Devs/TUTORIALS/)

**Configuration:**
- Overview → [DNA Concept](concepts/DNA.md)
- Reference → [Configuration Reference](reference/CONFIGURATION.md)
- File: `OASIS_DNA.json` in root directory

---

## 📚 Related Documentation

### Root Level Documentation
- **[README.md](../README.md)** - Main project README
- **[QUICKSTART.md](../QUICKSTART.md)** - Quick start guide
- **[OASIS_PROVIDER_ARCHITECTURE_GUIDE.md](../OASIS_PROVIDER_ARCHITECTURE_GUIDE.md)** - Provider guide

### Existing Documentation
- **[Docs/Devs/](../Docs/Devs/)** - Comprehensive developer documentation
- **[Docs/Strategic/](../Docs/Strategic/)** - Strategic and business documentation
- **[Docs/OASIS_HYPERDRIVE_WHITEPAPER.md](../Docs/OASIS_HYPERDRIVE_WHITEPAPER.md)** - HyperDrive whitepaper

---

## ✅ Verifying Documentation Accuracy

We have verification tools to ensure documentation accuracy:

**Verification Guide:** [`DOCUMENTATION_VERIFICATION.md`](DOCUMENTATION_VERIFICATION.md)

**Quick Verification:**
```bash
# Linux/Mac
bash docs/scripts/verify_docs.sh

# Windows (PowerShell)
powershell -ExecutionPolicy Bypass -File docs/scripts/verify_docs.ps1
```

The verification script checks:
- Provider enum vs. implementations
- Code file references exist
- HyperDrive methods exist
- Manager classes exist
- Configuration files are valid
- Documentation dates are current

---

## ⚠️ Important Notes

1. **Documentation is Evolving:** This structure is being improved. Some documents may be in progress.
2. **Code is Source of Truth:** When documentation conflicts with code, code takes precedence.
3. **Provider Status Changes:** Provider availability depends on OASIS_DNA configuration.
4. **API Versions:** API documentation may vary between mainnet/devnet configurations.

---

## 🤝 Contributing to Documentation

See the [Documentation Analysis Report](DOCUMENTATION_ANALYSIS_REPORT.md) for:
- Current documentation status
- Identified gaps and issues
- Recommendations for improvement
- Accuracy verification checklist

---

## 📞 Getting Help

- **Documentation Issues:** Check [Documentation Analysis Report](DOCUMENTATION_ANALYSIS_REPORT.md)
- **Technical Questions:** See [Developer Documentation](../Docs/Devs/)
- **API Questions:** Check [API Reference](../Docs/Devs/API%20Documentation/)

---

**Last Updated:** December 2025  
**Documentation Version:** 1.0

