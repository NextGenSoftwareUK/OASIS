# OASIS Dev Environment Setup Guide Comparison

**Source:** [GitHub Wiki - OASIS Dev Environment Setup Guide](https://github.com/NextGenSoftwareUK/OASIS/wiki/OASIS-Dev-Environment-Setup-Guide)  
**Date:** January 2026

---

## 📋 GitHub Guide Requirements

According to the [official setup guide](https://github.com/NextGenSoftwareUK/OASIS/wiki/OASIS-Dev-Environment-Setup-Guide), the setup process should be:

### Step 1: Clone OASIS Repository
```bash
git clone <OASIS-repo-url>
cd OASIS
```

### Step 2: Clone NextGenSoftware Libraries
**Location:** Should be in the **SAME repo folder** as OASIS

The guide shows that NextGenSoftware-Libraries should be cloned into the same directory as the OASIS repo, so the structure looks like:
```
OASIS/
  ├── OASIS Architecture/
  ├── ONODE/
  ├── Providers/
  └── ...
NextGenSoftware-Libraries/
  └── NextGenSoftware Libraries/
      ├── NextGenSoftware.Logging/
      ├── NextGenSoftware.Utilities/
      ├── NextGenSoftware.WebSocket/
      └── ...
```

### Step 3: Clone HoloNET Libraries (holochain-client-csharp)
**Location:** Should be in the **SAME repo folder** as OASIS

The guide shows that holochain-client-csharp should be cloned into the same directory as the OASIS repo:
```
OASIS/
  └── ...
NextGenSoftware-Libraries/
  └── ...
holochain-client-csharp/
  ├── NextGenSoftware.Holochain.HoloNET.Client/
  ├── NextGenSoftware.Holochain.HoloNET.ORM/
  └── ...
```

### Step 4: Verify in Solution
When you open the OASIS solution, the libs should appear in the **External Libs** folder in Visual Studio.

---

## 🔍 Current Setup Analysis

### Current Directory Structure
```
OASIS_CLEAN/
  ├── External Libs/
  │   ├── net-ipfs-http-client-master/
  │   └── Spectre.Console/
  ├── NextGenSoftware-Libraries/          ✅ EXISTS
  │   └── NextGenSoftware Libraries/
  │       ├── NextGenSoftware.Logging/
  │       ├── NextGenSoftware.Utilities/
  │       ├── NextGenSoftware.WebSocket/
  │       ├── NextGenSoftware.ErrorHandling/
  │       ├── NextGenSoftware.CLI.Engine/
  │       └── NextGenSoftware.Logging.NLog/
  ├── holochain-client-csharp/            ✅ EXISTS
  │   ├── NextGenSoftware.Holochain.HoloNET.Client/
  │   ├── NextGenSoftware.Holochain.HoloNET.ORM/
  │   └── ...
  └── ...
```

### Solution File References

From `The OASIS.sln`, the libraries are referenced as:

1. **External Libs** (in External Libs folder):
   - `External Libs\Spectre.Console\Spectre.Console\Spectre.Console.csproj`
   - `External Libs\net-ipfs-http-client-master\src\IpfsHttpClient.csproj`

2. **HoloNET Libraries** (from parent directory):
   - `..\holochain-client-csharp\NextGenSoftware.Holochain.HoloNET.Client\...`
   - `..\holochain-client-csharp\NextGenSoftware.Holochain.HoloNET.ORM\...`

3. **NextGenSoftware Libraries** (from parent directory):
   - `..\NextGenSoftware-Libraries\NextGenSoftware Libraries\NextGenSoftware.Logging\...`
   - `..\NextGenSoftware-Libraries\NextGenSoftware Libraries\NextGenSoftware.Utilities\...`
   - `..\NextGenSoftware-Libraries\NextGenSoftware Libraries\NextGenSoftware.WebSocket\...`
   - `..\NextGenSoftware-Libraries\NextGenSoftware Libraries\NextGenSoftware.ErrorHandling\...`
   - `..\NextGenSoftware-Libraries\NextGenSoftware Libraries\NextGenSoftware.CLI.Engine\...`
   - `..\NextGenSoftware-Libraries\NextGenSoftware Libraries\NextGenSoftware.Logging.NLog\...`

---

## ✅ Status Assessment

### What's Correct
1. ✅ **NextGenSoftware-Libraries** exists at the root level
2. ✅ **holochain-client-csharp** exists at the root level
3. ✅ **External Libs** folder exists with some libraries
4. ✅ Solution file references are working (using `..\` to reference parent directories)

### Potential Issues
1. ⚠️ **Solution references use `..\`** - This suggests the solution expects the libraries to be **siblings** to the OASIS repo, not inside it
2. ⚠️ **GitHub guide says "SAME repo folder"** - This is ambiguous. It could mean:
   - Same parent directory (siblings) - **Current setup matches this**
   - Inside the OASIS repo - **Would require different structure**

### Interpretation
The solution file uses `..\` paths, which means:
- If the solution is at: `OASIS_CLEAN/The OASIS.sln`
- It expects libraries at: `OASIS_CLEAN/../holochain-client-csharp/` (parent of OASIS_CLEAN)
- OR: The solution assumes a workspace structure where OASIS and libraries are siblings

**Current setup appears correct** if the workspace structure is:
```
workspace/
  ├── OASIS_CLEAN/          (or OASIS/)
  │   ├── The OASIS.sln
  │   └── ...
  ├── NextGenSoftware-Libraries/
  └── holochain-client-csharp/
```

But if the repo is cloned as `OASIS_CLEAN`, then the libraries should be:
```
OASIS_CLEAN/
  ├── The OASIS.sln
  ├── NextGenSoftware-Libraries/    ✅ Current
  ├── holochain-client-csharp/      ✅ Current
  └── ...
```

---

## 🔧 Recommendations

### Option 1: Keep Current Structure (Recommended)
The current setup appears to be working correctly. The solution file references suggest the libraries should be siblings to the solution file, which they are.

**Action:** No changes needed if everything builds correctly.

### Option 2: Move to External Libs (If Needed)
If you want to match the GitHub guide's visual representation (showing libs in External Libs folder in Visual Studio), you could:

1. Create symlinks or move libraries to External Libs:
   ```bash
   # Create symlinks (recommended to preserve git structure)
   cd "External Libs"
   ln -s ../NextGenSoftware-Libraries NextGenSoftware-Libraries
   ln -s ../holochain-client-csharp holochain-client-csharp
   ```

2. Update solution file references (if needed)

**Note:** This may not be necessary if the current setup works.

### Option 3: Verify Setup
To verify your setup is correct:

1. **Open the solution in Visual Studio**
2. **Check if External Libs folder shows the libraries** (even if they're referenced from parent)
3. **Try building the solution**
4. **Check for any missing reference errors**

---

## 📝 GitHub Guide Repository URLs

Based on the guide and codebase references, the repositories should be:

1. **OASIS Repository:**
   - Main repo: `https://github.com/NextGenSoftwareUK/OASIS.git`

2. **NextGenSoftware Libraries:**
   - `https://github.com/NextGenSoftwareUK/NextGenSoftware-Libraries.git`

3. **HoloNET (holochain-client-csharp):**
   - `https://github.com/NextGenSoftwareUK/holochain-client-csharp.git`

---

## 🎯 Conclusion

**Current Status:** ✅ **Setup appears correct**

The current directory structure matches what the solution file expects. The GitHub guide's phrase "SAME repo folder" likely means "same parent directory" (siblings), which is what you have.

**Next Steps:**
1. Verify the solution builds successfully
2. Check if Visual Studio shows the libraries in the External Libs folder (even if referenced from parent)
3. If there are build errors, check the specific library paths

---

## 📚 Related Documentation

- [GitHub Wiki Setup Guide](https://github.com/NextGenSoftwareUK/OASIS/wiki/OASIS-Dev-Environment-Setup-Guide)
- [Local Development Environment Setup](./Devs/DEVELOPMENT_ENVIRONMENT_SETUP.md)
- [STAR-Mac-Build Setup Script](../STAR-Mac-Build/setup-dependencies.sh)
