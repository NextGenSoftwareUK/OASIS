# OASIS IDE — Keyboard Shortcuts

> The OASIS IDE is a full-featured Electron-based code editor located at `C:\Source\IDE`.
> Press **`?`** at any time inside the editor to open the in-app shortcut reference panel.

---

## File & Navigation

| Shortcut | Action |
|----------|--------|
| `Ctrl+P` | **Go to File** — fuzzy-open any file in the workspace |
| `Ctrl+S` | Save current file |
| `Ctrl+Shift+F` | **Search in workspace** — supports Replace All, regex, case-sensitive, whole word, file-type and exclude filters |
| `Alt+←` | Navigate back in tab history |
| `Alt+→` | Navigate forward in tab history |

---

## Editor

| Shortcut | Action |
|----------|--------|
| `Ctrl+H` | Find & replace within the current file |
| `Ctrl+Shift+I` | Format document (via LSP formatter) |
| `Ctrl+Shift+D` | Toggle **inline diff** — current buffer vs last saved version |
| `Ctrl+Shift+V` | Toggle **Markdown preview** side-by-side (`.md` / `.mdx` files) |
| `Ctrl+K Z` | **Zen mode** — hide all panels, focus editor only |
| `Ctrl+=` | Zoom in |
| `Ctrl+-` | Zoom out |
| `Ctrl+0` | Reset zoom |
| `?` | Show keyboard shortcuts reference |
| `Escape` | Close any open modal, palette, or inline widget |

---

## AI — Inline Chat

| Shortcut | Action |
|----------|--------|
| `Ctrl+K` | **Open Inline AI Chat** — select a block of code first (or the current line is selected automatically), type a natural-language prompt, and the AI streams a replacement. A before/after diff view appears; use **✓ Accept**, **✗ Discard**, or **↩ Edit prompt** buttons. |

The inline chat uses the Web6 AI endpoint configured in Settings.

---

## Bookmarks

| Shortcut | Action |
|----------|--------|
| `Ctrl+F2` | Toggle bookmark on the current line |
| `F7` | Jump to **next** bookmark in the current file (wraps around) |
| `Shift+F7` | Jump to **previous** bookmark in the current file (wraps around) |

Bookmarks appear as `⬥` glyphs in the editor gutter and are listed in the **Bookmarks** tab of the bottom panel. Click any entry to jump to that line.

---

## Code Intelligence (LSP)

| Shortcut | Action |
|----------|--------|
| `F12` | Go to Definition |
| `Alt+F12` | Peek Definition inline (no navigation away from current file) |
| `Shift+F12` | Find all References |
| `F2` | Rename symbol across the entire workspace |
| `Ctrl+.` | Code actions — quick fixes, auto-imports, refactors |
| `Ctrl+Shift+O` | **Go to Symbol** — search functions, classes, variables in the workspace |

---

## Navigate & Palettes

| Shortcut | Action |
|----------|--------|
| `Ctrl+Shift+P` | **Command Palette** — run any IDE action by name |
| `Ctrl+Shift+M` | Focus the **Problems** panel (errors & warnings) |

---

## Debugger

The debugger supports Node.js processes via the Chrome DevTools Protocol (CDP).

| Shortcut / UI Action | Description |
|----------------------|-------------|
| Click the gutter margin | Toggle breakpoint on that line |
| **▶ Launch** (Debug panel) | Start the debugger with the selected launch config (reads `.vscode/launch.json`) |
| **Continue** button | Resume execution after a pause |
| **Step Over** | Execute current line, stay in current function |
| **Step Into** | Step into function call on current line |
| **Step Out** | Run until current function returns |
| **■ Stop** | Kill the debugged process |

Breakpoints appear as red circles `●` in the gutter. The current execution line is highlighted in amber. Use the **Debug** tab in the bottom panel for the call stack, local variables, and console output.

---

## Merge Conflict Resolution

When a file contains Git merge conflict markers (`<<<<<<<` / `=======` / `>>>>>>>`), the IDE automatically detects them and displays coloured zones with inline action buttons:

| Colour | Zone |
|--------|------|
| Amber border | Your changes (Current / HEAD) |
| Grey border | Separator line |
| Blue border | Incoming changes |

| Button | Action |
|--------|--------|
| **✓ Accept Current** | Keep your side, discard incoming |
| **✓ Accept Incoming** | Keep incoming side, discard yours |
| **✓ Accept Both** | Concatenate both sides in order |

All resolutions are fully undoable with `Ctrl+Z`. After resolving all conflicts, save and commit from the Git panel.

---

## Git Panel

| UI Action | Description |
|-----------|-------------|
| Click file in **Changes** tab | Open a side-by-side Monaco diff for that file |
| Checkbox next to file | Stage / unstage file for the next commit |
| **Stage all** button | Stage every changed file at once |
| Commit message box + **Commit** | Commit staged files |
| Branch dropdown | Switch to an existing branch |
| `+` button | Create and switch to a new branch |
| `↑` Push button | Push current branch to `origin` |
| `↓` Pull button | Pull from `origin` |
| **Log** tab | Repo-wide commit history |
| **Stash** tab | Save changes to a stash, pop (apply+remove) or drop stashes |
| **File History** tab | Per-file commit history — click any commit to see a diff of that file at that commit |

---

## Terminal

| Action | Description |
|--------|-------------|
| `+` button in terminal header | Open a new session — choose **OS Shell** or **STAR CLI** |
| Click tab | Switch between terminal sessions |
| `×` on a tab | Close that session |
| **Clear** button | Clear the current terminal's scrollback |

Multiple terminal sessions can run simultaneously (e.g. dev server in Shell 1, tests in Shell 2).

---

## REST Client

Accessible from the **REST Client** tab in the bottom panel.

| Action | Description |
|--------|-------------|
| Method dropdown + URL | Set HTTP method and endpoint |
| Headers section | Add/remove/toggle request headers |
| Body section | Request body (raw text or JSON) |
| **Send** button | Execute the request |
| History sidebar | Re-run a previous request |

---

## OASIS Code Snippets

Type the prefix in the editor and press `Tab` to expand:

| Prefix | Expands to |
|--------|-----------|
| `oasis-avatar` | Load OASIS Avatar boilerplate |
| `oasis-holon` | Create / save a Holon |
| `oasis-provider` | Activate a Provider |
| `oasis-oapp` | OAPP entry-point class skeleton |
| `oasis-web6-complete` | Web6 AI completion call |
| `oasis-mcp-tool` | Execute an MCP tool |
| `oasis-search` | Search Holons |
| `oasis-nft-mint` | Mint an NFT |

---

## Bottom Panel Tabs

| Tab | Description |
|-----|-------------|
| **Terminal** | One or more shell sessions (OS Shell + STAR CLI) |
| **Scripts** | Run `package.json` npm scripts with live output |
| **Output** | MCP tool call / result log from the AI chat |
| **Problems** | LSP errors and warnings — click to jump to the source line |
| **References** | Find-all-references results from LSP |
| **TODOs** | Workspace-wide scanner for `TODO`, `FIXME`, `HACK`, `NOTE` comments |
| **Debug** | Debugger output, call stack, local variables |
| **REST Client** | HTTP request builder with request history |
| **Notes** | Scratch pad — auto-saved Markdown file (persisted across sessions) |
| **Bookmarks** | All bookmarks across all open files, grouped by file |

---

## Settings

Open via the gear icon or the Command Palette (`Ctrl+Shift+P` → *Settings*).

| Setting | Options |
|---------|---------|
| Auto Save | Off / After Delay / On Focus Change |
| Auto Save Delay | Milliseconds (default 1500) |
| Format on Save | On / Off |
| Editor Theme | OASIS Dark, VS Dark, VS Light, High Contrast, Monokai, One Dark |

---

*OASIS IDE — located at `C:\Source\IDE`*
