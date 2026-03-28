import type { ReactNode } from 'react'

type BadgeVariant = 'default' | 'success' | 'warning' | 'error' | 'info' | 'purple'

const VARIANTS: Record<BadgeVariant, { bg: string; color: string; border: string }> = {
  default: { bg: '#18181f', color: '#9090a8', border: '#2a2a38' },
  success: { bg: '#22c55e15', color: '#22c55e', border: '#22c55e30' },
  warning: { bg: '#f59e0b15', color: '#f59e0b', border: '#f59e0b30' },
  error:   { bg: '#ef444415', color: '#ef4444', border: '#ef444430' },
  info:    { bg: '#6366f120', color: '#818cf8', border: '#6366f140' },
  purple:  { bg: '#a855f715', color: '#a855f7', border: '#a855f730' },
}

interface BadgeProps {
  children: ReactNode
  variant?: BadgeVariant
}

export function Badge({ children, variant = 'default' }: BadgeProps) {
  const v = VARIANTS[variant]
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center',
      padding: '2px 8px', borderRadius: '4px',
      fontSize: '0.7rem', fontWeight: 600, letterSpacing: '0.04em',
      background: v.bg, color: v.color, border: `1px solid ${v.border}`,
      whiteSpace: 'nowrap',
    }}>
      {children}
    </span>
  )
}
