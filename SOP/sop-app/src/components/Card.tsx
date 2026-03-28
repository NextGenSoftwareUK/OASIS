import type { ReactNode, CSSProperties } from 'react'

interface CardProps {
  children: ReactNode
  style?: CSSProperties
  className?: string
}

export function Card({ children, style, className }: CardProps) {
  return (
    <div
      className={className}
      style={{
        background: '#111118',
        border: '1px solid #1e1e2a',
        borderRadius: '10px',
        padding: '20px',
        ...style,
      }}
    >
      {children}
    </div>
  )
}

interface StatCardProps {
  label: string
  value: string | number
  sub?: string
  color?: string
}

export function StatCard({ label, value, sub, color = '#6366f1' }: StatCardProps) {
  return (
    <div style={{
      background: '#111118',
      border: '1px solid #1e1e2a',
      borderRadius: '10px',
      padding: '20px 24px',
      display: 'flex', flexDirection: 'column', gap: '4px',
    }}>
      <span style={{ fontSize: '0.75rem', color: '#5a5a72', textTransform: 'uppercase', letterSpacing: '0.07em' }}>
        {label}
      </span>
      <span style={{ fontSize: '2rem', fontWeight: 700, color, lineHeight: 1.1 }}>
        {value}
      </span>
      {sub && <span style={{ fontSize: '0.75rem', color: '#9090a8' }}>{sub}</span>}
    </div>
  )
}
