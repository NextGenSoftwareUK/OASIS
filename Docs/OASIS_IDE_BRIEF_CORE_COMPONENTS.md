# OASIS IDE: Core Components Brief

**For:** Core IDE Components Development Team  
**Status:** 📋 Implementation Brief  
**Reference:** Master Brief (`OASIS_IDE_MASTER_BRIEF.md`)

---

## 🎯 Objective

Build the **core IDE functionality** that provides the foundation for all other features:
- Code editor (Monaco)
- File system management
- Terminal integration
- Git support
- Language servers
- Debugger

---

## 📦 Components to Build

### 1. Monaco Editor Integration

**Technology:** Monaco Editor (VS Code's editor)

**Requirements:**
- [ ] Integrate Monaco Editor into React app
- [ ] Support syntax highlighting for major languages:
  - TypeScript/JavaScript
  - Python
  - Rust
  - Solidity
  - C#/.NET
  - And more...
- [ ] Implement IntelliSense (code completion)
- [ ] Multi-cursor editing
- [ ] Find/replace with regex
- [ ] Code folding
- [ ] Minimap
- [ ] Bracket matching
- [ ] Auto-indentation
- [ ] Code formatting
- [ ] Theme support (light/dark)

**Reference:**
- Monaco Editor Docs: https://microsoft.github.io/monaco-editor/
- Cursor Codebase: https://github.com/getcursor/cursor (editor integration)

**Implementation:**
```typescript
import * as monaco from 'monaco-editor';

class EditorManager {
  private editor: monaco.editor.IStandaloneCodeEditor;
  
  createEditor(container: HTMLElement, language: string) {
    this.editor = monaco.editor.create(container, {
      value: '',
      language: language,
      theme: 'vs-dark',
      automaticLayout: true,
      minimap: { enabled: true },
      fontSize: 14,
      wordWrap: 'on'
    });
  }
  
  setLanguage(language: string) {
    monaco.editor.setModelLanguage(this.editor.getModel()!, language);
  }
}
```

### 2. File Explorer

**Requirements:**
- [ ] Tree view of file system
- [ ] Workspace root selection
- [ ] File/folder creation
- [ ] File/folder deletion
- [ ] File/folder renaming
- [ ] File search
- [ ] File watching (auto-refresh)
- [ ] Context menu (right-click)
- [ ] Drag-and-drop support
- [ ] Multi-select

**Implementation:**
```typescript
class FileExplorer {
  private workspaceRoot: string;
  
  async loadWorkspace(root: string) {
    this.workspaceRoot = root;
    const tree = await this.buildFileTree(root);
    return tree;
  }
  
  async createFile(path: string, content: string) {
    await fs.writeFile(path, content);
    this.refresh();
  }
  
  async deleteFile(path: string) {
    await fs.unlink(path);
    this.refresh();
  }
}
```

### 3. Terminal Integration

**Technology:** xterm.js

**Requirements:**
- [ ] Terminal emulator in IDE
- [ ] Multiple terminal tabs
- [ ] Shell integration (bash, zsh, PowerShell)
- [ ] Command history
- [ ] Copy/paste support
- [ ] Terminal themes
- [ ] Resizable terminal
- [ ] Clear terminal command

**Reference:**
- xterm.js: https://xtermjs.org/
- Cursor Codebase: Terminal implementation

**Implementation:**
```typescript
import { Terminal } from 'xterm';
import { FitAddon } from 'xterm-addon-fit';

class TerminalManager {
  private terminals: Map<string, Terminal> = new Map();
  
  createTerminal(id: string, container: HTMLElement) {
    const term = new Terminal({
      theme: { background: '#1e1e1e' },
      fontSize: 14
    });
    
    const fitAddon = new FitAddon();
    term.loadAddon(fitAddon);
    term.open(container);
    fitAddon.fit();
    
    this.terminals.set(id, term);
    return term;
  }
}
```

### 4. Git Integration

**Requirements:**
- [ ] Git status indicator
- [ ] File diff viewer
- [ ] Commit interface
- [ ] Branch management
- [ ] Merge conflict resolution
- [ ] Git history viewer
- [ ] Staging/unstaging files
- [ ] Remote repository management

**Implementation:**
```typescript
import { simpleGit } from 'simple-git';

class GitManager {
  private git = simpleGit();
  
  async getStatus() {
    return await this.git.status();
  }
  
  async commit(message: string) {
    await this.git.add('.');
    await this.git.commit(message);
  }
  
  async getDiff(file: string) {
    return await this.git.diff([file]);
  }
}
```

### 5. Language Server Protocol (LSP)

**Requirements:**
- [ ] LSP client implementation
- [ ] Support for multiple language servers:
  - TypeScript/JavaScript (built-in)
  - Python (Pylance)
  - Rust (rust-analyzer)
  - Solidity (Solidity LSP)
- [ ] Code completion via LSP
- [ ] Hover information
- [ ] Go to definition
- [ ] Find references
- [ ] Symbol search
- [ ] Error diagnostics

**Reference:**
- LSP Specification: https://microsoft.github.io/language-server-protocol/
- Monaco LSP Integration: https://github.com/TypeFox/monaco-languageclient

**Implementation:**
```typescript
import { LanguageClient } from 'vscode-languageclient/browser';

class LSPManager {
  private clients: Map<string, LanguageClient> = new Map();
  
  async startLanguageServer(language: string) {
    const client = new LanguageClient(
      language,
      language,
      () => this.createServerProcess(language),
      {
        documentSelector: [{ scheme: 'file', language }]
      }
    );
    
    await client.start();
    this.clients.set(language, client);
  }
}
```

### 6. Debugger Integration

**Requirements:**
- [ ] Debug configuration UI
- [ ] Breakpoint management
- [ ] Variable inspection
- [ ] Call stack viewer
- [ ] Step through code
- [ ] Watch expressions
- [ ] Debug console
- [ ] Support for Node.js, Python, etc.

**Reference:**
- VS Code Debugger: https://code.visualstudio.com/docs/editor/debugging
- Debug Adapter Protocol: https://microsoft.github.io/debug-adapter-protocol/

---

## 🔧 Technical Requirements

### Dependencies

```json
{
  "dependencies": {
    "monaco-editor": "^0.45.0",
    "xterm": "^5.3.0",
    "xterm-addon-fit": "^0.8.0",
    "simple-git": "^3.20.0",
    "vscode-languageclient": "^8.1.0",
    "vscode-languageserver": "^8.1.0"
  }
}
```

### File Structure

```
src/
├── components/
│   ├── Editor/
│   │   ├── MonacoEditor.tsx
│   │   └── EditorManager.ts
│   ├── FileExplorer/
│   │   ├── FileTree.tsx
│   │   └── FileManager.ts
│   ├── Terminal/
│   │   ├── TerminalPanel.tsx
│   │   └── TerminalManager.ts
│   └── Git/
│       ├── GitPanel.tsx
│       └── GitManager.ts
├── services/
│   ├── LSPManager.ts
│   └── DebuggerManager.ts
└── types/
    └── index.ts
```

---

## ✅ Acceptance Criteria

### Editor
- [ ] Can open and edit files
- [ ] Syntax highlighting works for all supported languages
- [ ] Code completion works
- [ ] Find/replace works with regex
- [ ] Multi-cursor editing works

### File Explorer
- [ ] Can navigate file system
- [ ] Can create/delete/rename files
- [ ] File tree updates automatically
- [ ] Context menu works

### Terminal
- [ ] Terminal opens and accepts input
- [ ] Multiple terminals can be opened
- [ ] Command history works
- [ ] Copy/paste works

### Git
- [ ] Git status shows correctly
- [ ] Can stage and commit files
- [ ] Can view diffs
- [ ] Branch switching works

### LSP
- [ ] Code completion works via LSP
- [ ] Hover information shows
- [ ] Go to definition works
- [ ] Error diagnostics show

---

## 📚 Resources

- **Monaco Editor:** https://microsoft.github.io/monaco-editor/
- **xterm.js:** https://xtermjs.org/
- **LSP Spec:** https://microsoft.github.io/language-server-protocol/
- **Cursor Codebase:** https://github.com/getcursor/cursor
- **VS Code Source:** https://github.com/microsoft/vscode

---

## 🎯 Success Metrics

- Editor loads in < 1 second
- File operations complete in < 100ms
- Terminal responds in real-time
- Git operations complete in < 2 seconds
- LSP completions appear in < 200ms

---

*This brief covers core IDE functionality. Integration with OASIS features will be handled by other teams.*
