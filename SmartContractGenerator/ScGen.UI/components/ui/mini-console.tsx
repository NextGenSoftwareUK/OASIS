"use client";

import { useState, useEffect, useRef } from "react";
import { Terminal, X, Minimize2, Maximize2 } from "lucide-react";
import { cn } from "@/lib/utils";

export type ConsoleLog = {
  type: 'info' | 'success' | 'warning' | 'error' | 'data';
  message: string;
  timestamp: Date;
  icon?: string;
};

type MiniConsoleProps = {
  logs: ConsoleLog[];
  isActive?: boolean;
  onClear?: () => void;
  className?: string;
  title?: string;
};

export function MiniConsole({ logs, isActive = false, onClear, className, title = "Compilation Console" }: MiniConsoleProps) {
  const [isMinimized, setIsMinimized] = useState(false);
  const [isExpanded, setIsExpanded] = useState(false);
  const logsEndRef = useRef<HTMLDivElement>(null);

  // Auto-scroll to bottom when new logs appear
  useEffect(() => {
    logsEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [logs]);

  const getLogColor = (type: ConsoleLog['type']) => {
    switch (type) {
      case 'success':
        return 'text-green-400';
      case 'error':
        return 'text-red-400';
      case 'warning':
        return 'text-yellow-400';
      case 'data':
        return 'text-[var(--accent)]';
      default:
        return 'text-[var(--muted)]';
    }
  };

  const getLogIcon = (log: ConsoleLog) => {
    if (log.icon) return log.icon;
    switch (log.type) {
      case 'success':
        return '✅';
      case 'error':
        return '❌';
      case 'warning':
        return '⚠️';
      case 'data':
        return '📊';
      default:
        return '💬';
    }
  };

  if (isMinimized) {
    return (
      <div className={cn("fixed bottom-4 right-4 z-50", className)}>
        <button
          onClick={() => setIsMinimized(false)}
          className="flex items-center gap-2 rounded-lg border border-[var(--accent)]/50 bg-[rgba(6,11,26,0.95)] px-4 py-2 backdrop-blur-xl transition hover:border-[var(--accent)] hover:bg-[rgba(34,211,238,0.15)]"
        >
          <Terminal className="h-4 w-4 text-[var(--accent)]" />
          <span className="text-sm text-[var(--foreground)]">
            {title}
          </span>
          {isActive && (
            <div className="h-2 w-2 animate-pulse rounded-full bg-green-400" />
          )}
        </button>
      </div>
    );
  }

  return (
    <div className={cn(
      "fixed z-50 rounded-2xl border border-[var(--accent)]/50 bg-[rgba(6,11,26,0.95)] backdrop-blur-xl shadow-2xl transition-all",
      isExpanded ? "inset-4" : "bottom-4 right-4 w-[500px] h-[400px]",
      className
    )}>
      {/* Header */}
      <div className="flex items-center justify-between border-b border-[var(--accent)]/30 px-4 py-3">
        <div className="flex items-center gap-3">
          <div className="rounded-lg bg-[var(--accent)]/20 p-2">
            <Terminal className="h-4 w-4 text-[var(--accent)]" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-[var(--foreground)]">
              {title}
            </h3>
            <div className="text-xs text-[var(--muted)] flex items-center gap-2">
              <span>{logs.length} log{logs.length !== 1 ? 's' : ''}</span>
              {isActive && (
                <span className="inline-flex items-center gap-1">
                  <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-green-400 inline-block" />
                  <span className="text-green-400">Active</span>
                </span>
              )}
            </div>
          </div>
        </div>
        
        <div className="flex items-center gap-2">
          {onClear && logs.length > 0 && (
            <button
              onClick={onClear}
              className="rounded-lg px-2 py-1 text-xs text-[var(--muted)] transition hover:bg-[var(--accent)]/10 hover:text-[var(--accent)]"
            >
              Clear
            </button>
          )}
          <button
            onClick={() => setIsExpanded(!isExpanded)}
            className="rounded-lg p-1.5 text-[var(--muted)] transition hover:bg-[var(--accent)]/10 hover:text-[var(--accent)]"
          >
            {isExpanded ? (
              <Minimize2 className="h-4 w-4" />
            ) : (
              <Maximize2 className="h-4 w-4" />
            )}
          </button>
          <button
            onClick={() => setIsMinimized(true)}
            className="rounded-lg p-1.5 text-[var(--muted)] transition hover:bg-red-500/10 hover:text-red-400"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
      </div>

      {/* Console Logs */}
      <div className="h-[calc(100%-60px)] overflow-y-auto p-4 font-mono text-xs">
        {logs.length === 0 ? (
          <div className="flex h-full items-center justify-center">
            <p className="text-[var(--muted)]">
              Waiting for compilation...
            </p>
          </div>
        ) : (
          <div className="space-y-1">
            {logs.map((log, index) => (
              <div
                key={index}
                className={cn(
                  "flex items-start gap-2 rounded px-2 py-1 transition",
                  log.type === 'error' && "bg-red-500/10",
                  log.type === 'success' && "bg-green-500/10",
                  log.type === 'data' && "bg-[var(--accent)]/10"
                )}
              >
                <span className="flex-shrink-0">{getLogIcon(log)}</span>
                <div className="flex-1">
                  <div className="flex items-center justify-between gap-2">
                    <span className={getLogColor(log.type)}>
                      {log.message}
                    </span>
                    <span className="flex-shrink-0 text-[10px] text-[var(--muted)]">
                      {log.timestamp.toLocaleTimeString()}
                    </span>
                  </div>
                </div>
              </div>
            ))}
            <div ref={logsEndRef} />
          </div>
        )}
      </div>
    </div>
  );
}

// Hook to manage console logs
export function useConsoleLogger() {
  const [logs, setLogs] = useState<ConsoleLog[]>([]);
  const [isActive, setIsActive] = useState(false);

  const addLog = (type: ConsoleLog['type'], message: string, icon?: string) => {
    setLogs(prev => [
      ...prev,
      {
        type,
        message,
        timestamp: new Date(),
        icon
      }
    ]);
  };

  const clearLogs = () => {
    setLogs([]);
  };

  const log = {
    info: (message: string, icon?: string) => addLog('info', message, icon),
    success: (message: string, icon?: string) => addLog('success', message, icon),
    warning: (message: string, icon?: string) => addLog('warning', message, icon),
    error: (message: string, icon?: string) => addLog('error', message, icon),
    data: (message: string, icon?: string) => addLog('data', message, icon),
  };

  return {
    logs,
    isActive,
    setIsActive,
    log,
    clearLogs
  };
}


