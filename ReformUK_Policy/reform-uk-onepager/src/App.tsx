import { useState } from 'react';
import ReformUKOnePager from './ReformUKOnePager';
import NHSWaitingListsDashboard from './NHSWaitingListsDashboard';
import GovernmentWasteDashboard from './GovernmentWasteDashboard';
import DecentralizedIDDemo from './DecentralizedIDDemo';
import ReformTokenDemo from './ReformTokenDemo';

type View = 'onepager' | 'nhs' | 'government' | 'digitalid' | 'reformtoken';

function App() {
  const [currentView, setCurrentView] = useState<View>('onepager');

  return (
    <div className="min-h-screen">
      {/* Navigation Bar */}
      <nav className="fixed top-0 left-0 right-0 z-50 bg-slate-900/95 backdrop-blur-md border-b border-slate-700 print:hidden">
        <div className="max-w-7xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <img 
                src="/Logo_of_the_Reform_UK.svg.png" 
                alt="Reform UK" 
                className="h-10 w-auto"
              />
              <div className="border-l border-slate-600 pl-3">
                <p className="text-white font-bold text-lg">OASIS × Reform UK</p>
                <p className="text-cyan-400 text-xs">Blockchain for British Sovereignty</p>
              </div>
            </div>
            
            <div className="flex gap-2">
              <NavButton 
                active={currentView === 'onepager'} 
                onClick={() => setCurrentView('onepager')}
              >
                📄 Overview
              </NavButton>
              <NavButton 
                active={currentView === 'nhs'} 
                onClick={() => setCurrentView('nhs')}
              >
                🏥 NHS
              </NavButton>
              <NavButton 
                active={currentView === 'government'} 
                onClick={() => setCurrentView('government')}
              >
                🏛️ Spending
              </NavButton>
              <NavButton 
                active={currentView === 'digitalid'} 
                onClick={() => setCurrentView('digitalid')}
              >
                🆔 Digital ID
              </NavButton>
              <NavButton 
                active={currentView === 'reformtoken'} 
                onClick={() => setCurrentView('reformtoken')}
              >
                🇬🇧 $REFORM
              </NavButton>
            </div>
          </div>
        </div>
      </nav>

      {/* Content (with top padding for fixed nav) */}
      <div className="pt-20 print:pt-0">
        {currentView === 'onepager' && <ReformUKOnePager />}
        {currentView === 'nhs' && <NHSWaitingListsDashboard />}
        {currentView === 'government' && <GovernmentWasteDashboard />}
        {currentView === 'digitalid' && <DecentralizedIDDemo />}
        {currentView === 'reformtoken' && <ReformTokenDemo />}
      </div>
    </div>
  );
}

function NavButton({ 
  children, 
  active, 
  onClick 
}: { 
  children: React.ReactNode; 
  active: boolean; 
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      className={`px-4 py-2 rounded-lg font-medium transition-all ${
        active 
          ? 'bg-blue-600 text-white shadow-lg shadow-blue-500/50' 
          : 'bg-slate-800 text-slate-300 hover:bg-slate-700 hover:text-white'
      }`}
    >
      {children}
    </button>
  );
}

export default App;
