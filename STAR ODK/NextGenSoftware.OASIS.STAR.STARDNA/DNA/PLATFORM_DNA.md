# Platform-specific STAR DNA

STAR uses **`DNA/STAR_DNA.json`** by default. Platform-specific files are copied over the base file so the right config is loaded for the OS.

---

## JSON (at STAR boot)

On `IgniteStar()` / `IgniteStarAsync()`, **before** loading DNA, STAR copies:

| OS | Source | Target |
|----|--------|--------|
| Windows | `STAR_DNA.Windows.json` | `STAR_DNA.json` |
| Linux | `STAR_DNA.Linux.json` | `STAR_DNA.json` |
| macOS | `STAR_DNA.OSX.json` if present, else `STAR_DNA.Linux.json` | `STAR_DNA.json` |

The same pattern applies for **`STARDNA.json`** (sources: `STARDNA.Windows.json`, `STARDNA.Linux.json`, optional `STARDNA.OSX.json`). All in the same folder as the base file (e.g. `DNA/`).

**Naming:** Use a **dot** before the OS suffix (e.g. `STAR_DNA.Windows.json`), not spaces or hyphens.

**macOS:** Paths are usually the same as Linux. `STAR_DNA.OSX.json` / `STARDNA.OSX.json` are optional; if missing on Mac, the Linux variant is used.

**Skip:** Set `OASIS_SKIP_PLATFORM_DNA=1` to disable runtime JSON copying.

---

## C# (at build time)

Only **`DNA/Code/STARDNA.cs`** is compiled. The project **NextGenSoftware.OASIS.STAR.DNA.csproj** runs target **`PrepareSTARDNACS`** before compile and overwrites that file from:

| Build OS | Source |
|----------|--------|
| Windows | `DNA/Code/STARDNA.Windows.cs` |
| macOS (if file exists) | `DNA/Code/STARDNA.OSX.cs` |
| macOS (no OSX file) or Linux | `DNA/Code/STARDNA.Linux.cs` |

**`STARDNA.Windows.cs`**, **`STARDNA.Linux.cs`**, and optional **`STARDNA.OSX.cs`** are not compiled (Build Action: None). Keep these in source control; **`STARDNA.cs`** is overwritten each build.

---

## STAR CLI publish

Only **`DNA/**/*.json`** is copied next to the `star` binary. C# DNA is in the compiled assembly.
