# Python Proxy Classes - Execution Requirements

## Answer: Do C# Proxy Classes Need Python.NET Runtime?

### ✅ **YES - Proxy classes NEED Python.NET runtime to execute Python code**

However, they work in **two modes**:

---

## Mode 1: Signature Extraction Only (No Runtime Required) ✅

**What Works:**
- ✅ Proxy classes are **generated** correctly
- ✅ **IntelliSense** works (function names, parameters, return types)
- ✅ **Compilation** succeeds
- ✅ **Signature extraction** from Python source code works

**What Doesn't Work:**
- ❌ **Code execution** - Python functions cannot be called
- ❌ `InvokeAsync` returns an error: "Python.NET runtime is not available"

**Use Case:** When you only need IntelliSense/autocomplete, but don't need to actually run Python code.

---

## Mode 2: Full Execution (Python.NET Runtime Required) ✅

**What Works:**
- ✅ Everything from Mode 1
- ✅ **Python code execution** - Functions actually run
- ✅ **Access to Python libraries** (NumPy, Pandas, etc.)
- ✅ **Runtime type validation**

**Requirements:**
1. Install Python.NET package:
   ```bash
   dotnet add package pythonnet
   ```

2. Install Python runtime (3.7+):
   - Windows: Download from python.org
   - Linux: `sudo apt-get install python3`
   - macOS: `brew install python3`

3. (Optional) Enable Python.NET in project:
   ```xml
   <PropertyGroup>
     <DefineConstants>PYTHONNET_AVAILABLE</DefineConstants>
   </PropertyGroup>
   ```

---

## How It Works

### Proxy Class Generation Flow

```
1. User adds Python library as dependency to OAPP
   ↓
2. LibraryProxyGenerator generates C# proxy class
   ↓
3. Proxy class calls: _interopManager.InvokeAsync<T>(libraryId, functionName, params)
   ↓
4. LibraryInteropManager routes to PythonInteropProvider
   ↓
5. PythonInteropProvider checks: Is Python.NET runtime available?
   ├─ YES → Execute Python function ✅
   └─ NO  → Return error: "Python.NET runtime not available" ❌
```

### Generated Proxy Class Example

```csharp
public class MyPythonLibraryProxy
{
    private LibraryInteropManager _interopManager;
    private string _libraryId = "...";
    
    public async Task<OASISResult<int>> AddAsync(int a, int b)
    {
        await EnsureInitializedAsync();
        var parameters = new object[] { a, b };
        return await _interopManager.InvokeAsync<int>(_libraryId, "add", parameters);
        // ↑ This calls PythonInteropProvider.InvokeAsync
        //   Which checks if Python.NET runtime is available
    }
}
```

---

## Current Implementation Status

### ✅ Implemented
- **Graceful fallback** - Works without runtime (signatures only)
- **Runtime detection** - Automatically detects if Python.NET is available
- **Clear error messages** - Tells user what's missing
- **Source code parsing** - Extracts signatures without runtime

### ✅ Python.NET Integration (When Available)
- **Full execution** - Calls Python functions via Python.NET
- **Parameter conversion** - C# types → Python types
- **Return value conversion** - Python types → C# types
- **Module loading** - Imports Python modules correctly
- **GIL management** - Thread-safe Python execution

---

## Installation Guide

### Option 1: Signature Extraction Only (No Installation)

**Nothing to install!** Proxy classes will be generated and IntelliSense will work, but execution will fail with a helpful error message.

### Option 2: Full Execution Support

**Step 1:** Install Python runtime
```bash
# Windows (download from python.org)
# Linux
sudo apt-get install python3 python3-pip

# macOS
brew install python3
```

**Step 2:** Install Python.NET NuGet package
```bash
dotnet add package pythonnet
```

**Step 3:** (Optional) Enable compile-time support
Add to your `.csproj`:
```xml
<PropertyGroup>
  <DefineConstants>PYTHONNET_AVAILABLE</DefineConstants>
</PropertyGroup>
```

**Step 4:** Verify installation
```csharp
var manager = await LibraryInteropFactory.CreateDefaultManagerAsync();
var loadResult = await manager.Result.LoadLibraryAsync("path/to/library.py", InteropProviderType.Python);
// Should see: "Python interop provider initialized with Python.NET runtime"
```

---

## Error Messages

### Without Python.NET Runtime
```
Error: Python.NET runtime is not available. Cannot execute Python code.
Install Python.NET (dotnet add package pythonnet) and ensure Python runtime is installed.
Signature extraction works without runtime, but code execution requires it.
```

### With Python.NET but No Python Runtime
```
Error: Python runtime not found. Make sure Python 3.7+ is installed and in PATH.
```

---

## Summary

| Feature | Without Python.NET | With Python.NET |
|---------|-------------------|----------------|
| Proxy Generation | ✅ Yes | ✅ Yes |
| IntelliSense | ✅ Yes | ✅ Yes |
| Signature Extraction | ✅ Yes (from source) | ✅ Yes (from source + runtime) |
| Code Execution | ❌ No | ✅ Yes |
| Python Libraries | ❌ No | ✅ Yes (NumPy, Pandas, etc.) |

**Bottom Line:**
- **Proxy classes are generated correctly** ✅
- **IntelliSense works without runtime** ✅
- **Code execution REQUIRES Python.NET runtime** ⚠️
- **Graceful fallback** - Clear error if runtime missing ✅

