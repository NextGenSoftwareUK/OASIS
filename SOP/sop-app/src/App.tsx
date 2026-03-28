import { BrowserRouter, Routes, Route, NavLink, Navigate } from 'react-router-dom'
import { Home } from './pages/Home'
import { SOPRunner } from './pages/SOPRunner'
import { SOPIntel } from './pages/SOPIntel'
import { SOPAuthoring } from './pages/SOPAuthoring'
import { Connections } from './pages/Connections'
import { SquaresFour, Pulse, ChartBar, Sparkle, ArrowSquareOut, Plug } from '@phosphor-icons/react'

const NAV = [
  { to: '/',            end: true,  icon: <SquaresFour size={16} weight="regular" />, label: 'Home'        },
  { to: '/runner',      end: false, icon: <Pulse       size={16} weight="regular" />, label: 'SOPRunner'   },
  { to: '/intel',       end: false, icon: <ChartBar    size={16} weight="regular" />, label: 'SOPIntel'    },
  { to: '/authoring',   end: false, icon: <Sparkle     size={16} weight="regular" />, label: 'AI Authoring'},
  { to: '/connections', end: false, icon: <Plug        size={16} weight="regular" />, label: 'Connections' },
]

export default function App() {
  return (
    <BrowserRouter>
      <div style={{ display: 'flex', height: '100vh', overflow: 'hidden', position: 'relative', zIndex: 1 }}>

        {/* ── Sidebar ── */}
        <aside style={{
          width: 216, flexShrink: 0,
          background: 'rgba(6,6,6,0.92)',
          borderRight: '1px solid rgba(255,255,255,0.07)',
          backdropFilter: 'blur(12px)',
          display: 'flex', flexDirection: 'column',
          position: 'relative', zIndex: 2,
        }}>
          {/* Logo */}
          <div style={{ padding: '18px 16px 14px', borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
              {/* Logo mark — accent teal square */}
              <div style={{
                width: 28, height: 28, borderRadius: 7,
                background: 'rgba(45,212,191,0.12)',
                border: '1px solid rgba(45,212,191,0.3)',
                display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
              }}>
                <span style={{ color: '#2DD4BF', fontSize: '0.55rem', fontWeight: 700, fontFamily: 'var(--font-sans)', letterSpacing: '0.02em' }}>SOP</span>
              </div>
              <div>
                <div style={{ fontFamily: 'var(--font-serif)', fontWeight: 400, fontSize: '1rem', color: '#E0E0E0', letterSpacing: '-0.01em', lineHeight: 1.2 }}>OASIS SOP</div>
                <div style={{ fontSize: '0.68rem', color: '#424242', marginTop: 2, fontFamily: 'var(--font-sans)' }}>SOP Platform</div>
              </div>
            </div>
          </div>

          {/* Nav */}
          <nav style={{ flex: 1, padding: '10px 8px', overflowY: 'auto' }}>
            {NAV.map(({ to, end, icon, label }) => (
              <NavLink
                key={to}
                to={to}
                end={end}
                style={({ isActive }) => ({
                  display: 'flex', alignItems: 'center', gap: 9,
                  padding: '8px 10px', borderRadius: 6, marginBottom: 2,
                  fontSize: '0.875rem', fontWeight: isActive ? 500 : 400,
                  color: isActive ? '#2DD4BF' : '#606060',
                  background: isActive ? 'rgba(45,212,191,0.07)' : 'transparent',
                  textDecoration: 'none', transition: 'all 0.12s',
                  borderLeft: isActive ? '2px solid rgba(45,212,191,0.5)' : '2px solid transparent',
                  paddingLeft: isActive ? 8 : 10,
                })}
                onMouseEnter={e => {
                  if (e.currentTarget.getAttribute('aria-current') !== 'page') {
                    e.currentTarget.style.background = 'rgba(255,255,255,0.04)'
                    e.currentTarget.style.color = '#A0A0A0'
                  }
                }}
                onMouseLeave={e => {
                  if (e.currentTarget.getAttribute('aria-current') !== 'page') {
                    e.currentTarget.style.background = 'transparent'
                    e.currentTarget.style.color = '#606060'
                  }
                }}
              >
                <span style={{ display: 'flex' }}>{icon}</span>
                {label}
              </NavLink>
            ))}

            <div style={{ height: 1, background: 'rgba(255,255,255,0.05)', margin: '12px 2px' }} />

            <a
              href="http://localhost:5174"
              target="_blank"
              rel="noopener noreferrer"
              style={{ display: 'flex', alignItems: 'center', gap: 9, padding: '8px 10px', borderRadius: 6, fontSize: '0.875rem', color: '#484848', textDecoration: 'none', transition: 'color 0.12s', borderLeft: '2px solid transparent' }}
              onMouseEnter={e => (e.currentTarget.style.color = '#888')}
              onMouseLeave={e => (e.currentTarget.style.color = '#484848')}
            >
              <span style={{ display: 'flex' }}><ArrowSquareOut size={15} weight="regular" /></span>
              Workflow Builder
            </a>
          </nav>

          {/* User */}
          <div style={{ padding: '12px 14px', borderTop: '1px solid rgba(255,255,255,0.05)', display: 'flex', alignItems: 'center', gap: 10 }}>
            <div style={{
              width: 28, height: 28, borderRadius: '50%',
              background: 'rgba(45,212,191,0.06)',
              border: '1px solid rgba(45,212,191,0.2)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              fontSize: '0.6rem', fontWeight: 700, color: '#2DD4BF', flexShrink: 0,
            }}>
              OA
            </div>
            <div>
              <div style={{ fontSize: '0.875rem', color: '#C0C0C0', fontWeight: 500 }}>OASIS</div>
              <div style={{ fontSize: '0.72rem', color: '#484848' }}>SOP Platform</div>
            </div>
          </div>
        </aside>

        {/* ── Main content ── */}
        <main style={{ flex: 1, overflow: 'auto', background: 'transparent', minWidth: 0, position: 'relative', zIndex: 1 }}>
          <Routes>
            <Route path="/"              element={<Home />} />
            <Route path="/runner"        element={<SOPRunner />} />
            <Route path="/runner/:runId" element={<SOPRunner />} />
            <Route path="/intel"         element={<SOPIntel />} />
            <Route path="/intel/:sopId"  element={<SOPIntel />} />
            <Route path="/authoring"     element={<SOPAuthoring />} />
            <Route path="/connections"   element={<Connections />} />
            <Route path="*"              element={<Navigate to="/" replace />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  )
}
