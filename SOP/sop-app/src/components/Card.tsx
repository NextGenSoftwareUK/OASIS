import type { ReactNode, CSSProperties } from 'react'

export function Card({ children, style }: { children: ReactNode; style?: CSSProperties }) {
  return (
    <div style={{ background: '#181818', border: '1px solid #222222', borderRadius: 8, overflow: 'hidden', ...style }}>
      {children}
    </div>
  )
}

export function CardHeader({ title, sub, right }: { title: string; sub?: string; right?: ReactNode }) {
  return (
    <div style={{ padding: '14px 20px', borderBottom: '1px solid #1E1E1E', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
      <div>
        <div style={{ fontFamily: 'var(--font-sans)', fontSize: '0.9rem', fontWeight: 600, color: '#C4C4C4', letterSpacing: '-0.005em' }}>{title}</div>
        {sub && <div style={{ fontSize: '0.8rem', color: '#525252', marginTop: 3 }}>{sub}</div>}
      </div>
      {right && <div>{right}</div>}
    </div>
  )
}

export function StatCard({ label, value, sub, trend }: { label: string; value: string | number; sub?: string; trend?: { direction: 'up'|'down'; label: string } }) {
  return (
    <div>
      <div style={{ fontSize: '0.7rem', color: '#525252', textTransform: 'uppercase', letterSpacing: '0.08em', fontWeight: 600, marginBottom: 8 }}>{label}</div>
      <div style={{ fontSize: '2rem', fontWeight: 400, fontFamily: 'var(--font-serif)', color: '#E0E0E0', letterSpacing: '-0.02em', lineHeight: 1, marginBottom: 6 }}>{value}</div>
      {(sub || trend) && (
        <div style={{ display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}>
          {trend && <span style={{ fontSize: '0.75rem', fontWeight: 600, color: trend.direction === 'up' ? '#22C55E' : '#F59E0B' }}>{trend.direction === 'up' ? '↑' : '↓'} {trend.label}</span>}
          {sub && <span style={{ fontSize: '0.75rem', color: '#525252' }}>{sub}</span>}
        </div>
      )}
    </div>
  )
}
