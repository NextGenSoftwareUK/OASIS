# Library Interop System

## Overview

The Library Interop System enables OASIS to use libraries from **any language or framework** seamlessly. It provides a unified interface with multiple interop providers optimized for different scenarios.

## Architecture

### Core Components

1. **ILibraryInteropProvider** - Unified interface for all interop providers
2. **LibraryInteropManager** - Routes calls to appropriate providers
3. **Interop Providers** - Language/framework-specific implementations
4. **LibraryInteropFactory** - Factory for easy setup

### Supported Providers

| Provider | Language/Framework | Performance | Status |
|----------|-------------------|-------------|--------|
| **NativePInvoke** | C/C++/Rust (native) | ⚡⚡⚡ Best | ✅ Ready |
| **Python** | Python | ⚡⚡ Good | 🔧 Requires pythonnet |
| **JavaScript** | JavaScript/Node.js | ⚡⚡ Good | 🔧 Requires ClearScript |
| **WebAssembly** | Rust/C/C++/Go (WASM) | ⚡⚡ Very Good | 🔧 Requires Wasmtime |
| **Java** | Java | ⚡⚡ Good | 📋 Planned |
| **Go** | Go | ⚡⚡⚡ Best | 📋 Planned |
| **REST API** | Any (remote) | ⚡ Slower | 📋 Planned |
| **gRPC** | Any (remote) | ⚡⚡ Good | 📋 Planned |

## Performance Comparison

### Native P/Invoke (Best Performance)
- **Direct native code execution** - No interpretation overhead
- **Minimal marshalling** - Direct memory access
- **Best for**: C/C++/Rust libraries, performance-critical code
- **Overhead**: ~0-5% compared to direct native calls

### Python.NET
- **Good performance** - Compiled Python code
- **Easy integration** - Full Python ecosystem access
- **Best for**: Data science, ML libraries (NumPy, Pandas, etc.)
- **Overhead**: ~10-20% compared to native

### JavaScript (ClearScript)
- **Good performance** - V8 engine
- **Rich ecosystem** - Access to npm packages
- **Best for**: Web libraries, Node.js modules
- **Overhead**: ~15-25% compared to native

### WebAssembly
- **Very good performance** - Near-native speed
- **Cross-platform** - Works everywhere
- **Best for**: Rust/C/C++ compiled to WASM, portable libraries
- **Overhead**: ~5-15% compared to native

## Usage Examples

### Basic Usage

```csharp
// Create interop manager with default providers
var managerResult = await LibraryInteropFactory.CreateDefaultManagerAsync();
var manager = managerResult.Result;

// Load a native C library
var loadResult = await manager.LoadLibraryAsync("path/to/mylib.dll");
var library = loadResult.Result;

// Invoke a function
var result = await manager.InvokeAsync<int>(
    library.LibraryId, 
    "add", 
    5, 
    3
);
// result.Result = 8
```

### Python Library

```csharp
// Load Python library
var loadResult = await manager.LoadLibraryAsync("path/to/mathlib.py");
var library = loadResult.Result;

// Invoke Python function
var result = await manager.InvokeAsync<double>(
    library.LibraryId, 
    "calculate", 
    10.5
);
```

### JavaScript Library

```csharp
// Load JavaScript library
var loadResult = await manager.LoadLibraryAsync("path/to/utils.js");
var library = loadResult.Result;

// Invoke JavaScript function
var result = await manager.InvokeAsync<string>(
    library.LibraryId, 
    "formatDate", 
    DateTime.Now
);
```

### WebAssembly Library

```csharp
// Load WASM library (compiled from Rust/C/C++)
var loadResult = await manager.LoadLibraryAsync("path/to/compute.wasm");
var library = loadResult.Result;

// Invoke WASM function
var result = await manager.InvokeAsync<int>(
    library.LibraryId, 
    "fibonacci", 
    10
);
```

## Advanced Usage

### Custom Provider Registration

```csharp
var manager = new LibraryInteropManager();

// Register specific providers
var nativeProvider = new NativePInvokeProvider();
await manager.RegisterProviderAsync(nativeProvider);

var pythonProvider = new PythonInteropProvider();
await manager.RegisterProviderAsync(pythonProvider);
```

### Get Available Functions

```csharp
var functionsResult = await manager.GetAvailableFunctionsAsync(libraryId);
var functions = functionsResult.Result;
// functions = ["add", "subtract", "multiply", "divide"]
```

### Library Metadata

```csharp
var metadataResult = await manager.GetLibraryMetadataAsync(libraryId);
var metadata = metadataResult.Result;
// metadata.Name, metadata.Version, metadata.AvailableFunctions, etc.
```

## Performance Best Practices

1. **Use Native P/Invoke for Performance-Critical Code**
   - Best performance for C/C++/Rust libraries
   - Minimal overhead

2. **Cache Loaded Libraries**
   - Libraries are cached automatically
   - Reuse library instances when possible

3. **Batch Operations**
   - Load multiple libraries upfront
   - Reuse library IDs

4. **Choose the Right Provider**
   - Native for performance
   - Python for data science
   - JavaScript for web libraries
   - WASM for cross-platform portability

5. **Async Operations**
   - All operations are async
   - Use async/await for best performance

## Installation Requirements

### Required (Always Available)
- None - Native P/Invoke works out of the box

### Optional (For Additional Providers)

**Python Support:**
```bash
dotnet add package pythonnet
```

**JavaScript Support:**
```bash
dotnet add package Microsoft.ClearScript.V8
```

**WebAssembly Support:**
```bash
dotnet add package Wasmtime
```

## Integration with STARNET Libraries

The interop system integrates seamlessly with STARNET's Library system:

```csharp
// Load a STARNET library
var libraryManager = new LibraryManager(avatarId, stardna, oasisdna);
var installResult = await libraryManager.DownloadAndInstallAsync(libraryId);

// Use interop manager to load and use the library
var interopManager = await LibraryInteropFactory.CreateDefaultManagerAsync();
var loadResult = await interopManager.LoadLibraryAsync(installResult.Result.InstallPath);

// Invoke functions
var result = await interopManager.InvokeAsync<object>(
    loadResult.Result.LibraryId, 
    "functionName", 
    parameters
);
```

## Architecture Benefits

1. **Unified Interface** - Same API for all languages
2. **Performance Optimized** - Best provider for each use case
3. **Extensible** - Easy to add new providers
4. **Type Safe** - Strong typing with generics
5. **Async First** - Non-blocking operations
6. **Error Handling** - Comprehensive error handling with OASISResult

## Future Enhancements

- [ ] Performance benchmarking and caching layer
- [ ] Java interop via JNI
- [ ] Go interop via CGO
- [ ] REST/gRPC remote library support
- [ ] Function signature discovery and validation
- [ ] Hot reloading of libraries
- [ ] Library dependency resolution
- [ ] Performance metrics and monitoring

