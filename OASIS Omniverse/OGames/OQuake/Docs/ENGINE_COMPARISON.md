# OQuake engine comparison

Short comparison of Quake source ports for use as the OQuake engine (Quake + STAR API).

| | **vkQuake** | **QuakeSpasm** | **Ironwail** |
|---|-------------|----------------|---------------|
| **API** | **Vulkan** | OpenGL | **OpenGL 4** (modern) |
| **Performance** | Very high on Vulkan GPUs; multithreaded renderer | Good; single-threaded, lighter | Very high; compute culling, instancing, bindless |
| **GPU focus** | Strong Vulkan GPU | Broad compatibility | Modern GPU (e.g. 2012+ Radeon/GeForce, 2015+ Intel) |
| **2021 rerelease** | **Yes** | Varies | **Yes** |

**Takeaway:** vkQuake and Ironwail are the “fast” options; vkQuake for Vulkan, Ironwail for OpenGL. QuakeSpasm favors compatibility and simplicity.

| **Use case** | **Engine** |
|--------------|------------|
| Max FPS, Vulkan | **vkQuake** |
| Old / odd hardware | QuakeSpasm (OpenGL, lighter) |
| OpenGL, high quality | **Ironwail** |

**OQuake builtins:** All three are QuakeSpasm-family; the engine must register the two OQuake extension builtins and link star_api. vkQuake is documented in **vkquake_oquake\VKQUAKE_OQUAKE_INTEGRATION.md** and is the one **BUILD_OQUAKE.bat** automates. Ironwail and QuakeSpasm would need similar host.c/pr_ext-style hooks and build changes.

**Recommendation:** Use **vkQuake** for OQuake unless you specifically want OpenGL or maximum compatibility; then consider Ironwail or QuakeSpasm.
