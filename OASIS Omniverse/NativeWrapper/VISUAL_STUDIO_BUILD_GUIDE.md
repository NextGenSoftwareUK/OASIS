# Building in Visual Studio - Step by Step Guide

## Method 1: Using the Visual Studio Project File (Easiest)

### Step 1: Open the Project
1. **Open Visual Studio** (2019 or 2022)
2. **File** → **Open** → **Project/Solution...**
3. Navigate to: `C:\Source\OASIS-master\Game Integration\NativeWrapper\`
4. Select: **star_api.vcxproj**
5. Click **Open**

### Step 2: Configure Build
1. In the toolbar, find the **Solution Configurations** dropdown
2. Select: **Release**
3. Find the **Solution Platforms** dropdown
4. Select: **x64**

### Step 3: Build
1. **Right-click** on the `star_api` project in Solution Explorer
2. Select: **Build**
   - Or press **Ctrl+Shift+B**
   - Or go to **Build** → **Build Solution**

### Step 4: Verify Build
1. Check the **Output** window (View → Output)
2. Should see: "Build succeeded"
3. Check that the file exists:
   - `build\Release\star_api.dll`
   - `build\Release\star_api.lib`

## Method 2: Using CMake (If CMake is installed)

### Step 1: Open Developer Command Prompt
1. Search for "Developer Command Prompt for VS" in Start Menu
2. Or: Start → Visual Studio → Tools → Command Prompt

### Step 2: Navigate and Build
```cmd
cd C:\Source\OASIS-master\Game Integration\NativeWrapper
mkdir build
cd build
cmake .. -G "Visual Studio 17 2022" -A x64
cmake --build . --config Release
```

### Step 3: Open Generated Solution
1. In Visual Studio: **File** → **Open** → **Project/Solution**
2. Open: `build\star_api.sln`
3. Build as in Method 1

## Method 3: Create New Project from Existing Code

### Step 1: Create New Project
1. **File** → **New** → **Project**
2. Select: **Visual C++** → **Windows Desktop** → **Dynamic Link Library (.dll)**
3. Name: `star_api`
4. Location: `C:\Source\OASIS-master\Game Integration\NativeWrapper\`

### Step 2: Add Source Files
1. **Right-click** project → **Add** → **Existing Item**
2. Add: `star_api.cpp`
3. Add: `star_api.h` (as header)

### Step 3: Configure Project
1. **Right-click** project → **Properties**
2. **Configuration Properties** → **C/C++** → **General**
   - **Additional Include Directories**: Add `$(ProjectDir)`
3. **Configuration Properties** → **Linker** → **Input**
   - **Additional Dependencies**: Add `winhttp.lib`
4. **Configuration Properties** → **General**
   - **Configuration Type**: Dynamic Library (.dll)
   - **Platform Toolset**: v142 or v143

### Step 4: Build
- **Build** → **Build Solution** (or Ctrl+Shift+B)

## Troubleshooting

### "Cannot open include file 'winhttp.h'"
- **Solution**: Install Windows SDK
- Go to: Visual Studio Installer → Modify → Individual Components → Windows SDK

### "LNK2019: unresolved external symbol"
- **Solution**: Add `winhttp.lib` to linker dependencies
- Project Properties → Linker → Input → Additional Dependencies

### "Error: Platform Toolset not found"
- **Solution**: Install the required toolset
- Visual Studio Installer → Modify → Individual Components → MSVC v142 or v143

### Build Succeeds but DLL Not Found
- **Check**: `build\Release\star_api.dll` or `build\Debug\star_api.dll`
- The output location depends on your project configuration

## Quick Reference

**Project File**: `star_api.vcxproj`  
**Output DLL**: `build\Release\star_api.dll`  
**Output LIB**: `build\Release\star_api.lib`  
**Configuration**: Release, x64

## After Building

Once built, the library will be at:
- `C:\Source\OASIS-master\Game Integration\NativeWrapper\build\Release\star_api.dll`

You can then:
1. Build DOOM (it will link to this library)
2. Test the integration
3. Start using cross-game features!



