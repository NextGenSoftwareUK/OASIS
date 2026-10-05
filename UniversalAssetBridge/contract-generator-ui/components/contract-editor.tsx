'use client';

import { useEffect, useState } from 'react';
import Editor from '@monaco-editor/react';

interface ContractEditorProps {
  code: string;
  language: 'rust' | 'solidity' | 'typescript';
  onChange?: (value: string | undefined) => void;
  readOnly?: boolean;
  height?: string;
}

export function ContractEditor({
  code,
  language,
  onChange,
  readOnly = false,
  height = '600px',
}: ContractEditorProps) {
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  if (!mounted) {
    return (
      <div
        className="flex items-center justify-center rounded-xl border border-[var(--card-border)] bg-[rgba(8,11,26,0.6)]"
        style={{ height }}
      >
        <div className="text-[var(--muted)]">Loading editor...</div>
      </div>
    );
  }

  return (
    <div className="rounded-xl border border-[var(--card-border)] overflow-hidden">
      <Editor
        height={height}
        language={language}
        theme="vs-dark"
        value={code}
        onChange={onChange}
        options={{
          readOnly,
          minimap: { enabled: true },
          fontSize: 14,
          wordWrap: 'on',
          scrollBeyondLastLine: false,
          automaticLayout: true,
          tabSize: 2,
          insertSpaces: true,
          cursorBlinking: 'smooth',
          renderLineHighlight: 'all',
          padding: { top: 16, bottom: 16 },
        }}
      />
    </div>
  );
}


