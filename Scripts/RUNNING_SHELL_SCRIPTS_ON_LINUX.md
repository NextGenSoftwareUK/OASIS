# Running .sh scripts on Linux (double-click like Windows .bat)

On Windows you can double-click a `.bat` to run it. On Linux, behaviour depends on your file manager.

---

## 1. Terminal (always works)

```bash
chmod +x path/to/script.sh   # once, if needed
./path/to/script.sh
```

Example from the repo:

```bash
cd ~/Source/OASIS/Scripts/STAR\ CLI
./RUN_STAR_CLI.sh
```

---

## 2. GNOME Files / Pop!_OS / COSMIC (Nautilus and similar)

**Why `gsettings … executable-text-activation` fails:**  
GNOME **removed** that setting from Nautilus around **GNOME 40** (late 2020). There is **no** `org.gnome.nautilus.preferences executable-text-activation` key anymore — **`No such key` is normal**, not a mistake on your machine.

**What to do instead:**

| Method | How |
|--------|-----|
| **Right-click** | **Right-click** the `.sh` → **Run as a Program** (wording may be **Run**, **Execute**, or **Open** → choose run/terminal). |
| **Double-click** | Some versions show a dialog: pick **Run in Terminal** or **Run** instead of opening in an editor. |
| **`.desktop` launcher** | See §3 — this is the closest to “double-click like .bat” on modern GNOME. |

There is **no** supported way to turn “double-click always runs .sh” back on in current GNOME Files (by design, for security).

### KDE Dolphin

**Properties → Permissions →** check **Is executable**. Double-click may run or offer **Run in Terminal**.

### Older distros (very old Nautilus only)

Only on **old** Nautilus might this still exist:

```bash
gsettings set org.gnome.nautilus.preferences executable-text-activation launch
```

If you get **No such key**, your Files app no longer supports it — use §1 or §3.

---

## 3. `.desktop` launcher (best double-click experience on GNOME)

Create a shortcut that runs your script (double-click the **`.desktop`** file, not the `.sh`):

1. Example `~/Desktop/Run STAR CLI.desktop`:

```ini
[Desktop Entry]
Type=Application
Name=Run STAR CLI
Exec=bash -lc '/home/YOURUSER/Source/OASIS/Scripts/STAR CLI/RUN_STAR_CLI.sh'
Terminal=true
```

2. Replace `YOURUSER` and paths as needed. Paths with spaces (e.g. `STAR CLI`) are fine inside the quotes.

3. Mark trusted + executable:

```bash
chmod +x ~/Desktop/Run\ STAR\ CLI.desktop
```

4. First launch: choose **Trust and launch** / **Allow launching** if prompted.

---

## 4. Summary

| Goal | What to do |
|------|------------|
| Run now | Terminal: `./script.sh` |
| From file manager (GNOME) | **Right-click → Run as a Program** (no global “double-click .sh” setting) |
| Double-click like `.bat` | **`.desktop`** file pointing at the script (`Terminal=true`) |
| `gsettings executable-text-activation` | **Removed** in modern GNOME — ignore if you see “No such key” |

All OASIS `.sh` scripts use a shebang so they run correctly when executed.
