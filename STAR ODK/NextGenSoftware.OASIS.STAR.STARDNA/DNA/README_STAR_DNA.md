# STAR_DNA.json

STAR DNA config (templates, paths, defaults). Use **forward slashes** in paths on Linux/macOS.

## Path resolution (no manual paths needed)

- **STARBasePath** (blank): Resolved at runtime to the folder containing the STAR executable (publish output or `dotnet run` bin). Set to override with an absolute path or a path relative to that folder.
- **STARNETBasePath** (default `"STARNET"`): Root for STARNET user data (OAPPs, runtimes, libs, etc.). Blank → `STARBasePath/STARNET` so data sits in a STARNET subfolder. Set to override (absolute or relative to app folder).

All other path keys are **relative to STARBasePath or STARNETBasePath** (or absolute if you use rooted paths). No need to set STARBasePath/STARNETBasePath for publish or installer—they resolve automatically.

## Files

- **STAR_DNA.json** – Main config (Linux/macOS: forward slashes).
- **OASIS_DNA.json** – OASIS provider/config (see OASIS DNA docs).
