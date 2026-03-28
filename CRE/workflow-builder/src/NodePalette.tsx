import type { ConnectorType } from './types'
import { CONNECTORS, CATEGORIES } from './connectors'

interface NodePaletteProps {
  onDragStart: (e: React.DragEvent, connectorType: ConnectorType) => void
}

const mono: React.CSSProperties = { fontFamily: "'IBM Plex Mono', monospace" }
const syne: React.CSSProperties = { fontFamily: "'Syne', sans-serif" }

// Which categories start open
const DEFAULT_OPEN = new Set(['trigger', 'ai', 'financial', 'integration', 'sop'])

export function NodePalette({ onDragStart }: NodePaletteProps) {
  const byCategory = Object.entries(CATEGORIES).map(([catKey, cat]) => ({
    ...cat,
    key: catKey,
    connectors: Object.values(CONNECTORS).filter(c => c.category === catKey),
  }))

  return (
    <div
      style={{
        position: 'absolute', left: 0, top: 0, bottom: 0,
        width: '214px',
        display: 'flex', flexDirection: 'column',
        zIndex: 20,
        background: 'var(--bg)',
        borderRight: '1px solid var(--border)',
        overflow: 'hidden',
      }}
    >
      {/* Header */}
      <div style={{
        flexShrink: 0,
        padding: '9px 12px',
        borderBottom: '1px solid var(--border)',
        background: 'var(--panel)',
      }}>
        <div style={{ display: 'flex', alignItems: 'baseline', gap: '6px' }}>
          <span style={{ ...syne, fontWeight: 700, fontSize: '0.75rem', color: 'var(--text)', textTransform: 'uppercase', letterSpacing: '0.08em' }}>
            Connectors
          </span>
          <span style={{ ...mono, fontSize: '0.52rem', color: 'var(--muted)' }}>
            {Object.keys(CONNECTORS).length} types
          </span>
        </div>
        <p style={{ ...mono, fontSize: '0.56rem', color: 'var(--muted)', marginTop: '2px' }}>
          Drag onto canvas to add
        </p>
      </div>

      {/* Accordion sections */}
      <div style={{ flex: 1, overflowY: 'auto', padding: '8px 8px 8px' }}>
        {byCategory.map(cat => (
          <details
            key={cat.key}
            className="palette-sec"
            open={DEFAULT_OPEN.has(cat.key)}
          >
            <summary style={{ color: cat.color as string }}>
              {cat.label}
            </summary>
            <div className="palette-sec-body">
              {cat.connectors.map(connector => (
                <div
                  key={connector.type}
                  draggable
                  onDragStart={e => onDragStart(e, connector.type as ConnectorType)}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: '7px',
                    padding: '5px 9px',
                    background: connector.bgColor,
                    border: `1px solid ${connector.borderColor}45`,
                    borderRadius: '4px',
                    cursor: 'grab',
                    userSelect: 'none',
                    transition: 'border-color 0.1s, box-shadow 0.1s',
                  }}
                  onMouseEnter={e => {
                    e.currentTarget.style.borderColor = connector.borderColor + 'aa'
                    e.currentTarget.style.boxShadow = `0 0 6px ${connector.color}20`
                  }}
                  onMouseLeave={e => {
                    e.currentTarget.style.borderColor = connector.borderColor + '45'
                    e.currentTarget.style.boxShadow = 'none'
                  }}
                >
                  <span style={{ fontSize: '0.8rem', flexShrink: 0 }}>{connector.icon}</span>
                  <span style={{ ...mono, fontSize: '0.62rem', color: 'var(--text)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                    {connector.label}
                  </span>
                </div>
              ))}
            </div>
          </details>
        ))}
      </div>

      {/* Footer */}
      <div style={{ flexShrink: 0, padding: '8px 12px', borderTop: '1px solid var(--border)', background: 'var(--panel)' }}>
        <a
          href="https://hessmeister.github.io/oasis-holonic-staging/"
          target="_blank"
          rel="noopener noreferrer"
          style={{ ...mono, fontSize: '0.56rem', textTransform: 'uppercase', letterSpacing: '0.06em', color: 'var(--teal)', textDecoration: 'none' }}
        >
          ↗ OASIS Architecture
        </a>
      </div>
    </div>
  )
}
