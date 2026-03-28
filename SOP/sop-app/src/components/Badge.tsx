import type { ReactNode } from 'react'

type V = 'default' | 'success' | 'warning' | 'error' | 'active' | 'ai'

const VARIANTS: Record<V, { bg: string; color: string; border: string }> = {
  default: { bg: 'rgba(255,255,255,0.04)', color: '#686868', border: 'rgba(255,255,255,0.08)'  },
  success: { bg: 'rgba(34,197,94,0.07)',   color: '#4ADE80', border: 'rgba(34,197,94,0.15)'    },
  warning: { bg: 'rgba(245,158,11,0.07)',  color: '#FCD34D', border: 'rgba(245,158,11,0.15)'   },
  error:   { bg: 'rgba(239,68,68,0.07)',   color: '#F87171', border: 'rgba(239,68,68,0.15)'    },
  active:  { bg: 'rgba(45,212,191,0.07)',  color: '#2DD4BF', border: 'rgba(45,212,191,0.18)'   },
  ai:      { bg: 'rgba(139,92,246,0.08)',  color: '#A78BFA', border: 'rgba(139,92,246,0.18)'   },
}

export function Badge({ children, variant = 'default' }: { children: ReactNode; variant?: keyof typeof VARIANTS }) {
  const s = VARIANTS[variant]
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center',
      padding: '2px 7px', borderRadius: '4px',
      fontSize: '0.68rem', fontWeight: 600, letterSpacing: '0.03em',
      background: s.bg, color: s.color,
      border: `0.5px solid ${s.border}`,
      /* pangea-style inset top gloss */
      boxShadow: 'inset 0 0.5px 0 rgba(255,255,255,0.07)',
      whiteSpace: 'nowrap',
      fontFamily: 'var(--font-sans)',
    }}>
      {children}
    </span>
  )
}
