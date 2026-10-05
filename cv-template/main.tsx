import React from 'react'
import ReactDOM from 'react-dom/client'
import Resume from './max_cv_template.tsx'
import ReformUKProposal from './reform_uk_web3_advisor.tsx'
import './index.css'

// Toggle between Resume and ReformUKProposal by changing the component below
const ActiveComponent = ReformUKProposal; // or Resume

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ActiveComponent />
  </React.StrictMode>,
)

