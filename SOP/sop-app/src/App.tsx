import { BrowserRouter, Routes, Route, NavLink, Navigate } from 'react-router-dom'
import { Home } from './pages/Home'
import { SOPRunner } from './pages/SOPRunner'
import { SOPIntel } from './pages/SOPIntel'
import { SOPAuthoring } from './pages/SOPAuthoring'
import { Activity, BarChart2, Sparkles, LayoutGrid, Cpu } from 'lucide-react'

const nav: React.CSSProperties = {
  position: 'fixed', top: 0, left: 0, right: 0, height: '52px',
  display: 'flex', alignItems: 'center', gap: '0',
  background: '#0d0d14', borderBottom: '1px solid #1e1e2a',
  zIndex: 100, padding: '0 24px',
}

const logo: React.CSSProperties = {
  fontFamily: 'JetBrains Mono, monospace',
  fontSize: '0.8rem', fontWeight: 700,
  color: '#6366f1', letterSpacing: '0.12em',
  textTransform: 'uppercase', marginRight: '32px',
  display: 'flex', alignItems: 'center', gap: '8px',
}

const navLinkStyle = ({ isActive }: { isActive: boolean }): React.CSSProperties => ({
  display: 'flex', alignItems: 'center', gap: '6px',
  padding: '6px 14px', borderRadius: '6px',
  fontFamily: 'Inter, sans-serif', fontSize: '0.8rem', fontWeight: 500,
  color: isActive ? '#e2e2f0' : '#5a5a72',
  background: isActive ? '#1e1e2a' : 'transparent',
  textDecoration: 'none', transition: 'color 0.15s, background 0.15s',
  whiteSpace: 'nowrap',
})

const main: React.CSSProperties = {
  paddingTop: '52px', minHeight: '100vh', background: '#0a0a0f',
}

export default function App() {
  return (
    <BrowserRouter>
      <nav style={nav}>
        <div style={logo}>
          <Cpu size={14} />
          OASIS SOP
        </div>
        <NavLink to="/" end style={navLinkStyle}>
          <LayoutGrid size={13} /> Home
        </NavLink>
        <NavLink to="/runner" style={navLinkStyle}>
          <Activity size={13} /> Runner
        </NavLink>
        <NavLink to="/intel" style={navLinkStyle}>
          <BarChart2 size={13} /> Intel
        </NavLink>
        <NavLink to="/authoring" style={navLinkStyle}>
          <Sparkles size={13} /> AI Authoring
        </NavLink>
        <div style={{ flex: 1 }} />
        <a
          href="http://localhost:5174"
          target="_blank"
          rel="noopener noreferrer"
          style={{ ...navLinkStyle({ isActive: false }), border: '1px solid #1e1e2a' }}
        >
          Workflow Builder ↗
        </a>
      </nav>
      <main style={main}>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/runner" element={<SOPRunner />} />
          <Route path="/runner/:runId" element={<SOPRunner />} />
          <Route path="/intel" element={<SOPIntel />} />
          <Route path="/intel/:sopId" element={<SOPIntel />} />
          <Route path="/authoring" element={<SOPAuthoring />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </BrowserRouter>
  )
}
