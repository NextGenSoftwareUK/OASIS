import { useState, Suspense } from 'react';
import { Canvas } from '@react-three/fiber';
import { OrbitControls, Stars } from '@react-three/drei';
import { nhsNodes3D, patientFlows3D, getNHSStats, formatWaitTime } from './data/nhs-visualization-data';
import type { NHSNode3D } from './data/nhs-visualization-data';
import { NHSNode } from './components/NHSNode3D';
import { FlowParticles } from './components/FlowParticles3D';
import { FlowLine } from './components/FlowLine3D';

export default function NHSWaitingListsDashboard() {
  const [selectedHospital, setSelectedHospital] = useState<NHSNode3D | null>(null);
  const [showLabels, setShowLabels] = useState(true);
  const [autoRotate, setAutoRotate] = useState(true);
  const stats = getNHSStats();

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-blue-900 to-slate-900">
      {/* Header */}
      <div className="bg-gradient-to-r from-blue-900 to-blue-800 text-white p-8 shadow-2xl">
        <div className="max-w-7xl mx-auto">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-4xl font-bold mb-2">NHS Waiting Lists Dashboard</h1>
              <p className="text-blue-200">Real-Time Resource Tracking via OASIS Oracle</p>
            </div>
            <div className="text-right">
              <p className="text-sm text-blue-300">Reform UK Policy Solution</p>
              <p className="text-lg font-semibold">Pledge #3: Zero NHS Waiting Lists</p>
            </div>
          </div>
        </div>
      </div>

      {/* Stats Bar */}
      <div className="bg-slate-800 border-b border-slate-700 py-4">
        <div className="max-w-7xl mx-auto px-8">
          <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
            <StatCard 
              label="Patients Waiting" 
              value={stats.totalWaitingList.toLocaleString()} 
              color="text-red-400"
              subtitle="Across all hospitals"
            />
            <StatCard 
              label="Hospitals Monitored" 
              value={stats.hospitalsMonitored.toString()} 
              color="text-cyan-400"
              subtitle="Real-time tracking"
            />
            <StatCard 
              label="Bed Capacity" 
              value={`${stats.avgCapacity}%`} 
              color={stats.avgCapacity > 90 ? "text-red-400" : "text-yellow-400"}
              subtitle={`${stats.totalBedsAvailable} available`}
            />
            <StatCard 
              label="Active Transfers" 
              value={stats.totalTransfers.toString()} 
              color="text-green-400"
              subtitle="Patients moving now"
            />
            <StatCard 
              label="Total Patients" 
              value={stats.totalPatients.toLocaleString()} 
              color="text-blue-400"
              subtitle="In system"
            />
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto p-8 space-y-8">
        
        {/* 3D Network Placeholder + Hospital Cards */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          
          {/* Left: 3D Hospital Network Visualization */}
          <div className="lg:col-span-2">
            <div className="rounded-2xl bg-slate-900 border border-cyan-500/30 h-[600px] relative overflow-hidden">
              {/* Controls */}
              <div className="absolute top-4 right-4 z-10 flex gap-2">
                <button
                  onClick={() => setShowLabels(!showLabels)}
                  className="px-3 py-2 bg-slate-800/90 backdrop-blur-sm text-white text-sm rounded-lg border border-slate-600 hover:bg-slate-700 transition-colors"
                >
                  {showLabels ? '🏷️ Labels ON' : '🏷️ Labels OFF'}
                </button>
                <button
                  onClick={() => setAutoRotate(!autoRotate)}
                  className="px-3 py-2 bg-slate-800/90 backdrop-blur-sm text-white text-sm rounded-lg border border-slate-600 hover:bg-slate-700 transition-colors"
                >
                  {autoRotate ? '⏸️ Pause' : '▶️ Rotate'}
                </button>
              </div>

              {/* Stats Overlay */}
              <div className="absolute bottom-4 left-4 z-10 flex gap-3">
                <div className="px-3 py-2 bg-slate-800/90 backdrop-blur-sm rounded-lg border border-cyan-500/30">
                  <p className="text-xs text-slate-400">Hospitals</p>
                  <p className="text-lg font-bold text-cyan-400">{stats.hospitalsMonitored}</p>
                </div>
                <div className="px-3 py-2 bg-slate-800/90 backdrop-blur-sm rounded-lg border border-red-500/30">
                  <p className="text-xs text-slate-400">Waiting</p>
                  <p className="text-lg font-bold text-red-400">{(stats.totalWaitingList / 1000).toFixed(1)}k</p>
                </div>
                <div className="px-3 py-2 bg-slate-800/90 backdrop-blur-sm rounded-lg border border-green-500/30">
                  <p className="text-xs text-slate-400">Transfers</p>
                  <p className="text-lg font-bold text-green-400">{stats.totalTransfers}</p>
                </div>
              </div>

              {/* 3D Canvas */}
              <Canvas
                camera={{ position: [0, 5, 35], fov: 75 }}
                style={{ background: "#050510" }}
              >
                {/* Lighting */}
                <ambientLight intensity={0.3} />
                <pointLight position={[20, 20, 20]} intensity={1} color="#ffffff" />
                <pointLight position={[-20, -20, -20]} intensity={0.5} color="#22d3ee" />
                <pointLight position={[0, 20, -20]} intensity={0.3} color="#818cf8" />
                
                <Suspense fallback={null}>
                  {/* Background starfield */}
                  <Stars 
                    radius={100} 
                    depth={50} 
                    count={5000} 
                    factor={4} 
                    saturation={0} 
                    fade 
                    speed={0.5}
                  />
                  
                  {/* Hospital nodes */}
                  {nhsNodes3D.map((hospital) => (
                    <NHSNode
                      key={hospital.id}
                      node={hospital}
                      onClick={setSelectedHospital}
                      showLabels={showLabels}
                    />
                  ))}
                  
                  {/* Patient flow lines */}
                  {patientFlows3D.map((flow, index) => {
                    const fromNode = nhsNodes3D.find(n => n.id === flow.from);
                    const toNode = nhsNodes3D.find(n => n.id === flow.to);
                    
                    if (!fromNode || !toNode) return null;
                    
                    return (
                      <FlowLine
                        key={`line-${index}`}
                        from={fromNode.position}
                        to={toNode.position}
                        amount={flow.amount}
                        isActive={flow.isActive}
                      />
                    );
                  })}
                  
                  {/* Flowing particles */}
                  {patientFlows3D
                    .filter(flow => flow.isActive)
                    .map((flow, index) => {
                      const fromNode = nhsNodes3D.find(n => n.id === flow.from);
                      const toNode = nhsNodes3D.find(n => n.id === flow.to);
                      
                      if (!fromNode || !toNode) return null;
                      
                      return (
                        <FlowParticles
                          key={`particles-${index}`}
                          from={fromNode.position}
                          to={toNode.position}
                          amount={flow.amount}
                          isActive={flow.isActive}
                        />
                      );
                    })}
                  
                  {/* Camera controls */}
                  <OrbitControls
                    autoRotate={autoRotate}
                    autoRotateSpeed={0.3}
                    enableDamping
                    dampingFactor={0.05}
                    minDistance={20}
                    maxDistance={60}
                    enablePan={false}
                  />
                </Suspense>
              </Canvas>
            </div>
          </div>

          {/* Right: Selected Hospital Details */}
          <div className="space-y-4">
            <h3 className="text-xl font-bold text-white mb-4">
              {selectedHospital ? selectedHospital.name : 'Select a Hospital'}
            </h3>
            
            {selectedHospital ? (
              <div className="space-y-4">
                <DetailCard 
                  label="Waiting List" 
                  value={selectedHospital.waitingList.toLocaleString()} 
                  color="red"
                  subtitle={`Avg Wait: ${formatWaitTime(selectedHospital.avgWaitTime)}`}
                />
                <DetailCard 
                  label="Bed Capacity" 
                  value={`${selectedHospital.capacity}%`} 
                  color={selectedHospital.capacity > 90 ? 'red' : selectedHospital.capacity > 85 ? 'yellow' : 'cyan'}
                  subtitle={`${selectedHospital.bedsAvailable}/${selectedHospital.beds} available`}
                />
                <DetailCard 
                  label="Staff on Duty" 
                  value={`${selectedHospital.staffAvailable.toLocaleString()}`} 
                  color="cyan"
                  subtitle={`of ${selectedHospital.staff.toLocaleString()} total`}
                />
                <DetailCard 
                  label="Equipment" 
                  value={`${Object.values(selectedHospital.equipment).reduce((a, b) => a + b, 0)}`} 
                  color="cyan"
                  subtitle={`${selectedHospital.equipment.mri} MRI, ${selectedHospital.equipment.ct} CT scanners`}
                />
              </div>
            ) : (
              <div className="text-slate-400 text-sm space-y-2">
                <p>Click a hospital in the 3D view or list below to see:</p>
                <ul className="list-disc list-inside space-y-1 pl-2">
                  <li>Real-time bed availability</li>
                  <li>Current waiting list size</li>
                  <li>Staff on duty</li>
                  <li>Equipment status</li>
                  <li>Patient transfers</li>
                </ul>
              </div>
            )}
          </div>
        </div>

        {/* Hospital List */}
        <div className="rounded-2xl bg-slate-800/50 border border-slate-700 overflow-hidden">
          <div className="p-6 bg-slate-800 border-b border-slate-700">
            <h3 className="text-xl font-bold text-white">All Hospitals - Real-Time Status</h3>
            <p className="text-sm text-slate-400 mt-1">Click any hospital for detailed view</p>
          </div>
          
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-slate-900/50">
                <tr className="border-b border-slate-700">
                  <th className="text-left p-4 text-sm font-semibold text-slate-300">Hospital</th>
                  <th className="text-right p-4 text-sm font-semibold text-slate-300">Waiting List</th>
                  <th className="text-right p-4 text-sm font-semibold text-slate-300">Avg Wait</th>
                  <th className="text-right p-4 text-sm font-semibold text-slate-300">Beds</th>
                  <th className="text-center p-4 text-sm font-semibold text-slate-300">Capacity</th>
                  <th className="text-center p-4 text-sm font-semibold text-slate-300">Status</th>
                </tr>
              </thead>
              <tbody>
                {nhsNodes3D.map((hospital, i) => (
                  <tr 
                    key={hospital.id}
                    onClick={() => setSelectedHospital(hospital)}
                    className={`border-b border-slate-700/50 hover:bg-slate-700/30 cursor-pointer transition-colors ${
                      i % 2 === 0 ? 'bg-slate-800/30' : 'bg-slate-800/10'
                    }`}
                  >
                    <td className="p-4">
                      <div>
                        <p className="font-medium text-white">{hospital.name}</p>
                        <p className="text-xs text-slate-400">{hospital.type} • {hospital.region}</p>
                      </div>
                    </td>
                    <td className="p-4 text-right font-mono text-red-400 font-semibold">
                      {hospital.waitingList.toLocaleString()}
                    </td>
                    <td className="p-4 text-right text-yellow-400">
                      {formatWaitTime(hospital.avgWaitTime)}
                    </td>
                    <td className="p-4 text-right font-mono text-slate-300">
                      <span className="text-cyan-400">{hospital.bedsAvailable}</span> / {hospital.beds}
                    </td>
                    <td className="p-4 text-center">
                      <div className="inline-flex items-center gap-2">
                        <div className="w-24 h-2 bg-slate-700 rounded-full overflow-hidden">
                          <div 
                            className={`h-full ${
                              hospital.capacity > 95 ? 'bg-red-500' : 
                              hospital.capacity > 85 ? 'bg-yellow-500' : 
                              'bg-cyan-500'
                            }`}
                            style={{ width: `${hospital.capacity}%` }}
                          />
                        </div>
                        <span className="text-sm font-semibold text-white">{hospital.capacity}%</span>
                      </div>
                    </td>
                    <td className="p-4 text-center">
                      <span className={`inline-block w-2 h-2 rounded-full ${
                        hospital.capacity > 95 ? 'bg-red-500' : 
                        hospital.capacity > 85 ? 'bg-yellow-500' : 
                        'bg-cyan-500'
                      }`} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* Reform UK Solution Cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <SolutionCard
            icon="🔐"
            title="Patient-Owned Records"
            before="Fragmented records across systems"
            after="Single blockchain health record"
            savings="£4.5-8bn/year"
          />
          <SolutionCard
            icon="🤖"
            title="AI Resource Optimization"
            after="Real-time bed/equipment tracking"
            before="Manual resource management"
            savings="£4.5-6.5bn/year"
          />
          <SolutionCard
            icon="🎟️"
            title="Private Overflow Vouchers"
            before="7.6M waiting list"
            after="Use private capacity via tokenized vouchers"
            savings="50-70% reduction"
          />
        </div>

        {/* Impact Summary */}
        <div className="rounded-2xl bg-gradient-to-br from-green-900/30 to-blue-900/30 border border-green-500/30 p-8">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="text-2xl font-bold text-white mb-2">OASIS Oracle NHS Impact</h3>
              <p className="text-green-300">Blockchain-enabled resource optimization delivering on Reform UK's pledge</p>
            </div>
            <div className="text-right">
              <p className="text-4xl font-bold text-green-400">£25-42bn</p>
              <p className="text-green-300">Annual Savings</p>
              <p className="text-xs text-slate-400 mt-1">(exceeds £17bn investment)</p>
            </div>
          </div>
          
          <div className="grid grid-cols-3 gap-6 mt-8">
            <ImpactMetric label="Waiting List Reduction" value="50-70%" color="green" />
            <ImpactMetric label="Bed Utilization" value="95%+" color="cyan" />
            <ImpactMetric label="Admin Overhead" value="-75%" color="green" />
          </div>
        </div>

      </div>
    </div>
  );
}

function StatCard({ label, value, color, subtitle }: { label: string; value: string; color: string; subtitle?: string }) {
  return (
    <div className="bg-slate-800/50 rounded-lg p-4 border border-slate-700">
      <p className="text-xs uppercase tracking-wide text-slate-400 mb-1">{label}</p>
      <p className={`text-2xl font-bold ${color}`}>{value}</p>
      {subtitle && <p className="text-xs text-slate-500 mt-1">{subtitle}</p>}
    </div>
  );
}

function DetailCard({ label, value, color, subtitle }: { label: string; value: string; color: string; subtitle?: string }) {
  const colorClasses = {
    red: 'text-red-400 border-red-500/30 bg-red-900/20',
    yellow: 'text-yellow-400 border-yellow-500/30 bg-yellow-900/20',
    cyan: 'text-cyan-400 border-cyan-500/30 bg-cyan-900/20',
  };

  return (
    <div className={`rounded-xl p-4 border ${colorClasses[color as keyof typeof colorClasses] || colorClasses.cyan}`}>
      <p className="text-xs uppercase tracking-wide text-slate-400 mb-2">{label}</p>
      <p className={`text-3xl font-bold mb-1`}>{value}</p>
      {subtitle && <p className="text-xs text-slate-400">{subtitle}</p>}
    </div>
  );
}

function SolutionCard({ icon, title, before, after, savings }: { icon: string; title: string; before: string; after: string; savings: string }) {
  return (
    <div className="rounded-2xl bg-slate-800/50 border border-blue-500/30 p-6 hover:border-blue-500/50 transition-colors">
      <div className="text-4xl mb-3">{icon}</div>
      <h4 className="text-lg font-bold text-white mb-4">{title}</h4>
      <div className="space-y-3">
        <div>
          <p className="text-xs uppercase text-red-400 mb-1">Before</p>
          <p className="text-sm text-slate-300">{before}</p>
        </div>
        <div>
          <p className="text-xs uppercase text-green-400 mb-1">With OASIS</p>
          <p className="text-sm text-white font-medium">{after}</p>
        </div>
        <div className="pt-3 border-t border-slate-700">
          <p className="text-xs uppercase text-cyan-400 mb-1">Impact</p>
          <p className="text-lg font-bold text-cyan-400">{savings}</p>
        </div>
      </div>
    </div>
  );
}

function ImpactMetric({ label, value, color }: { label: string; value: string; color: string }) {
  const colorClasses = {
    green: 'text-green-400 bg-green-900/30 border-green-500/30',
    cyan: 'text-cyan-400 bg-cyan-900/30 border-cyan-500/30',
  };

  return (
    <div className={`rounded-lg p-4 border ${colorClasses[color as keyof typeof colorClasses]}`}>
      <p className="text-xs uppercase tracking-wide text-slate-400 mb-2">{label}</p>
      <p className="text-3xl font-bold">{value}</p>
    </div>
  );
}

