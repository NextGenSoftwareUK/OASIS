import { useState, Suspense } from 'react';
import { Canvas } from '@react-three/fiber';
import { OrbitControls, Stars } from '@react-three/drei';
import { departmentNodes3D, spendingFlows3D, getGovernmentStats, formatGBP, getTotalBlockchainSavings, calculateBlockchainSavings } from './data/government-spending-data';
import type { DepartmentNode3D } from './data/government-spending-data';
import { DepartmentNode } from './components/DepartmentNode3D';
import { FlowParticles } from './components/FlowParticles3D';
import { FlowLine } from './components/FlowLine3D';

export default function GovernmentWasteDashboard() {
  const [selectedDept, setSelectedDept] = useState<DepartmentNode3D | null>(null);
  const [showLabels, setShowLabels] = useState(true);
  const [autoRotate, setAutoRotate] = useState(true);
  const stats = getGovernmentStats();
  const potentialSavings = getTotalBlockchainSavings();

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-blue-900 to-slate-900">
      {/* Header */}
      <div className="bg-gradient-to-r from-blue-900 to-blue-800 text-white p-8 shadow-2xl">
        <div className="max-w-7xl mx-auto">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-4xl font-bold mb-2">Government Spending Transparency</h1>
              <p className="text-blue-200">Real-Time Blockchain Audit Trail via OASIS Oracle</p>
            </div>
            <div className="text-right">
              <p className="text-sm text-blue-300">Reform UK Policy Solution</p>
              <p className="text-lg font-semibold">Target: £50bn Waste Reduction</p>
            </div>
          </div>
        </div>
      </div>

      {/* Stats Bar */}
      <div className="bg-slate-800 border-b border-slate-700 py-4">
        <div className="max-w-7xl mx-auto px-8">
          <div className="grid grid-cols-2 md:grid-cols-6 gap-4">
            <StatCard 
              label="Spent Today" 
              value={formatGBP(stats.totalSpentToday, 'm')} 
              color="text-blue-400"
              subtitle="Across all departments"
            />
            <StatCard 
              label="Annual Budget" 
              value={formatGBP(stats.totalBudget)} 
              color="text-slate-300"
              subtitle="Total government"
            />
            <StatCard 
              label="Wasteful Spending" 
              value={formatGBP(stats.totalWastefulSpending, 'm')} 
              color="text-red-400"
              subtitle={`${stats.wastePercent}% of total`}
            />
            <StatCard 
              label="Potential Savings" 
              value={formatGBP(potentialSavings)} 
              color="text-green-400"
              subtitle="Via blockchain"
            />
            <StatCard 
              label="Transparency" 
              value={`${stats.avgTransparency}%`} 
              color={stats.avgTransparency > 50 ? "text-cyan-400" : "text-yellow-400"}
              subtitle="Avg across depts"
            />
            <StatCard 
              label="Departments" 
              value={stats.departmentsMonitored.toString()} 
              color="text-cyan-400"
              subtitle="Real-time tracking"
            />
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto p-8 space-y-8">
        
        {/* 3D Network Placeholder + Department Details */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          
          {/* Left: 3D Department Network Visualization */}
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
                <div className="px-3 py-2 bg-slate-800/90 backdrop-blur-sm rounded-lg border border-blue-500/30">
                  <p className="text-xs text-slate-400">Departments</p>
                  <p className="text-lg font-bold text-blue-400">{stats.departmentsMonitored}</p>
                </div>
                <div className="px-3 py-2 bg-slate-800/90 backdrop-blur-sm rounded-lg border border-red-500/30">
                  <p className="text-xs text-slate-400">Waste Today</p>
                  <p className="text-lg font-bold text-red-400">{formatGBP(stats.totalWastefulSpending, 'm')}</p>
                </div>
                <div className="px-3 py-2 bg-slate-800/90 backdrop-blur-sm rounded-lg border border-green-500/30">
                  <p className="text-xs text-slate-400">Potential Savings</p>
                  <p className="text-lg font-bold text-green-400">{formatGBP(potentialSavings)}</p>
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
                  
                  {/* Department nodes */}
                  {departmentNodes3D.map((dept) => (
                    <DepartmentNode
                      key={dept.id}
                      node={dept}
                      onClick={setSelectedDept}
                      showLabels={showLabels}
                    />
                  ))}
                  
                  {/* Spending flow lines */}
                  {spendingFlows3D.map((flow, index) => {
                    const fromNode = departmentNodes3D.find(n => n.id === flow.from);
                    
                    if (!fromNode) return null;
                    
                    // For government flows, "to" is a string (recipient), so we need to find a target position
                    // For now, flows go outward from departments
                    const toPos: [number, number, number] = [
                      fromNode.position[0] + (Math.random() - 0.5) * 15,
                      fromNode.position[1] - 8,
                      fromNode.position[2] + (Math.random() - 0.5) * 15
                    ];
                    
                    return (
                      <FlowLine
                        key={`line-${index}`}
                        from={fromNode.position}
                        to={toPos}
                        amount={flow.amount}
                        isActive={flow.isActive}
                        isWasteful={flow.isWasteful}
                      />
                    );
                  })}
                  
                  {/* Flowing particles */}
                  {spendingFlows3D
                    .filter(flow => flow.isActive)
                    .map((flow, index) => {
                      const fromNode = departmentNodes3D.find(n => n.id === flow.from);
                      
                      if (!fromNode) return null;
                      
                      const toPos: [number, number, number] = [
                        fromNode.position[0] + (Math.random() - 0.5) * 15,
                        fromNode.position[1] - 8,
                        fromNode.position[2] + (Math.random() - 0.5) * 15
                      ];
                      
                      return (
                        <FlowParticles
                          key={`particles-${index}`}
                          from={fromNode.position}
                          to={toPos}
                          amount={flow.amount}
                          isActive={flow.isActive}
                          isWasteful={flow.isWasteful}
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

          {/* Right: Selected Department Details */}
          <div className="space-y-4">
            <h3 className="text-xl font-bold text-white mb-4">
              {selectedDept ? selectedDept.shortName : 'Select a Department'}
            </h3>
            
            {selectedDept ? (
              <div className="space-y-4">
                <DetailCard 
                  label="Annual Budget" 
                  value={formatGBP(selectedDept.annualBudget)} 
                  color="blue"
                  subtitle={`${formatGBP(selectedDept.spentToday, 'm')} spent today`}
                />
                <DetailCard 
                  label="Waste Score" 
                  value={`${selectedDept.wasteScore}/100`} 
                  color={selectedDept.wasteScore > 75 ? 'red' : selectedDept.wasteScore > 50 ? 'yellow' : 'green'}
                  subtitle={`${selectedDept.efficiency}% efficiency`}
                />
                <DetailCard 
                  label="Blockchain Savings" 
                  value={formatGBP(calculateBlockchainSavings(selectedDept))} 
                  color="green"
                  subtitle="Potential annual reduction"
                />
                <DetailCard 
                  label="Transparency" 
                  value={`${selectedDept.transparency}%`} 
                  color={selectedDept.transparency > 50 ? 'cyan' : 'yellow'}
                  subtitle={`${selectedDept.contracts.toLocaleString()} active contracts`}
                />
                <DetailCard 
                  label="Frontline Spending" 
                  value={`${selectedDept.frontlinePercent}%`} 
                  color={selectedDept.frontlinePercent > 75 ? 'green' : 'yellow'}
                  subtitle={`${100 - selectedDept.frontlinePercent}% back-office`}
                />
              </div>
            ) : (
              <div className="text-slate-400 text-sm space-y-2">
                <p>Click a department in the 3D view or list below to see:</p>
                <ul className="list-disc list-inside space-y-1 pl-2">
                  <li>Real-time spending</li>
                  <li>Waste score & efficiency</li>
                  <li>Blockchain savings potential</li>
                  <li>Transparency level</li>
                  <li>Frontline vs back-office split</li>
                </ul>
              </div>
            )}
          </div>
        </div>

        {/* Department Spending Table */}
        <div className="rounded-2xl bg-slate-800/50 border border-slate-700 overflow-hidden">
          <div className="p-6 bg-slate-800 border-b border-slate-700">
            <h3 className="text-xl font-bold text-white">All Departments - Real-Time Spending</h3>
            <p className="text-sm text-slate-400 mt-1">Every pound tracked on blockchain ledger</p>
          </div>
          
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-slate-900/50">
                <tr className="border-b border-slate-700">
                  <th className="text-left p-4 text-sm font-semibold text-slate-300">Department</th>
                  <th className="text-right p-4 text-sm font-semibold text-slate-300">Annual Budget</th>
                  <th className="text-right p-4 text-sm font-semibold text-slate-300">Spent Today</th>
                  <th className="text-center p-4 text-sm font-semibold text-slate-300">Waste Score</th>
                  <th className="text-center p-4 text-sm font-semibold text-slate-300">Transparency</th>
                  <th className="text-right p-4 text-sm font-semibold text-slate-300">Potential Savings</th>
                </tr>
              </thead>
              <tbody>
                {departmentNodes3D
                  .sort((a, b) => b.annualBudget - a.annualBudget)
                  .map((dept, i) => {
                    const savings = calculateBlockchainSavings(dept);
                    return (
                      <tr 
                        key={dept.id}
                        onClick={() => setSelectedDept(dept)}
                        className={`border-b border-slate-700/50 hover:bg-slate-700/30 cursor-pointer transition-colors ${
                          i % 2 === 0 ? 'bg-slate-800/30' : 'bg-slate-800/10'
                        }`}
                      >
                        <td className="p-4">
                          <div>
                            <p className="font-medium text-white">{dept.shortName}</p>
                            <p className="text-xs text-slate-400">{dept.name}</p>
                          </div>
                        </td>
                        <td className="p-4 text-right font-mono text-slate-300 font-semibold">
                          {formatGBP(dept.annualBudget)}
                        </td>
                        <td className="p-4 text-right font-mono text-blue-400">
                          {formatGBP(dept.spentToday, 'm')}
                        </td>
                        <td className="p-4 text-center">
                          <div className="inline-flex flex-col items-center gap-1">
                            <div className="w-20 h-2 bg-slate-700 rounded-full overflow-hidden">
                              <div 
                                className={`h-full ${
                                  dept.wasteScore > 75 ? 'bg-red-500' : 
                                  dept.wasteScore > 50 ? 'bg-yellow-500' : 
                                  'bg-green-500'
                                }`}
                                style={{ width: `${dept.wasteScore}%` }}
                              />
                            </div>
                            <span className={`text-sm font-semibold ${
                              dept.wasteScore > 75 ? 'text-red-400' : 
                              dept.wasteScore > 50 ? 'text-yellow-400' : 
                              'text-green-400'
                            }`}>{dept.wasteScore}/100</span>
                          </div>
                        </td>
                        <td className="p-4 text-center">
                          <div className="inline-flex flex-col items-center gap-1">
                            <div className="w-20 h-2 bg-slate-700 rounded-full overflow-hidden">
                              <div 
                                className="h-full bg-cyan-500"
                                style={{ width: `${dept.transparency}%` }}
                              />
                            </div>
                            <span className="text-sm font-semibold text-cyan-400">{dept.transparency}%</span>
                          </div>
                        </td>
                        <td className="p-4 text-right font-mono text-green-400 font-bold">
                          {formatGBP(savings)}
                        </td>
                      </tr>
                    );
                  })}
              </tbody>
            </table>
          </div>
        </div>

        {/* Real-Time Spending Flows */}
        <div className="rounded-2xl bg-slate-800/50 border border-slate-700 overflow-hidden">
          <div className="p-6 bg-slate-800 border-b border-slate-700">
            <h3 className="text-xl font-bold text-white">Live Spending Flows</h3>
            <p className="text-sm text-slate-400 mt-1">Real-time transactions - wasteful spending highlighted in red</p>
          </div>
          
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-slate-900/50">
                <tr className="border-b border-slate-700">
                  <th className="text-left p-4 text-sm font-semibold text-slate-300">Department</th>
                  <th className="text-left p-4 text-sm font-semibold text-slate-300">Recipient</th>
                  <th className="text-right p-4 text-sm font-semibold text-slate-300">Amount</th>
                  <th className="text-center p-4 text-sm font-semibold text-slate-300">Type</th>
                  <th className="text-center p-4 text-sm font-semibold text-slate-300">Status</th>
                </tr>
              </thead>
              <tbody>
                {spendingFlows3D.filter(f => f.isActive).map((flow, i) => {
                  const dept = departmentNodes3D.find(d => d.id === flow.from);
                  return (
                    <tr 
                      key={i}
                      className={`border-b border-slate-700/50 ${
                        i % 2 === 0 ? 'bg-slate-800/30' : 'bg-slate-800/10'
                      }`}
                    >
                      <td className="p-4">
                        <span className="font-medium text-white">{dept?.shortName || flow.from}</span>
                      </td>
                      <td className="p-4 text-slate-300">{flow.to}</td>
                      <td className={`p-4 text-right font-mono font-bold ${flow.isWasteful ? 'text-red-400' : 'text-green-400'}`}>
                        {formatGBP(flow.amount, 'm')}
                      </td>
                      <td className="p-4 text-center">
                        <span className="text-xs px-2 py-1 rounded-full bg-slate-700 text-slate-300">
                          {flow.type}
                        </span>
                      </td>
                      <td className="p-4 text-center">
                        <span className={`inline-flex items-center gap-1 text-xs px-2 py-1 rounded-full ${
                          flow.isWasteful 
                            ? 'bg-red-900/30 text-red-400 border border-red-500/30' 
                            : 'bg-green-900/30 text-green-400 border border-green-500/30'
                        }`}>
                          <span className={`w-1.5 h-1.5 rounded-full ${flow.isWasteful ? 'bg-red-500' : 'bg-green-500'}`} />
                          {flow.isWasteful ? 'Wasteful' : 'Efficient'}
                        </span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>

        {/* Reform UK Solution Cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <SolutionCard
            icon="📊"
            title="Public Spending Ledger"
            before="Opaque, hidden spending"
            after="Every pound on blockchain"
            savings="£17-28bn/year"
            impact="Real-time citizen audit"
          />
          <SolutionCard
            icon="⚖️"
            title="Smart Contract Procurement"
            before="Corrupt backroom deals (PPE: £37bn waste)"
            after="Automated competitive bidding"
            savings="£27-45bn/year"
            impact="No more corruption"
          />
          <SolutionCard
            icon="🤖"
            title="Workflow Automation"
            before="Manual bureaucracy, bloat"
            after="Smart contract gov services"
            savings="£8-15bn/year"
            impact="75% admin reduction"
          />
        </div>

        {/* Blockchain Integration Progress */}
        <div className="rounded-2xl bg-gradient-to-br from-cyan-900/30 to-blue-900/30 border border-cyan-500/30 p-8">
          <h3 className="text-2xl font-bold text-white mb-6">Blockchain Integration Status</h3>
          
          <div className="space-y-4">
            {departmentNodes3D
              .sort((a, b) => b.transparency - a.transparency)
              .slice(0, 6)
              .map(dept => (
                <div key={dept.id} className="flex items-center justify-between">
                  <div className="flex-1">
                    <p className="text-white font-medium">{dept.shortName}</p>
                    <div className="mt-2 w-full h-3 bg-slate-700 rounded-full overflow-hidden">
                      <div 
                        className="h-full bg-gradient-to-r from-cyan-500 to-blue-500 transition-all duration-500"
                        style={{ width: `${dept.transparency}%` }}
                      />
                    </div>
                  </div>
                  <div className="ml-6 text-right">
                    <p className="text-2xl font-bold text-cyan-400">{dept.transparency}%</p>
                    <p className="text-xs text-slate-400">Transparent</p>
                  </div>
                </div>
              ))}
          </div>
          
          <div className="mt-8 pt-6 border-t border-cyan-500/20">
            <p className="text-sm text-slate-300 mb-2">
              <strong className="text-white">Reform UK Goal:</strong> 100% transparency across all departments
            </p>
            <p className="text-sm text-slate-300">
              <strong className="text-white">Current Progress:</strong> {stats.avgTransparency}% average
            </p>
          </div>
        </div>

        {/* Impact Summary */}
        <div className="rounded-2xl bg-gradient-to-br from-green-900/30 to-blue-900/30 border border-green-500/30 p-8">
          <div className="flex items-center justify-between mb-8">
            <div>
              <h3 className="text-2xl font-bold text-white mb-2">OASIS Blockchain Transparency Impact</h3>
              <p className="text-green-300">Delivering Reform UK's £50bn waste reduction target</p>
            </div>
            <div className="text-right">
              <p className="text-5xl font-bold text-green-400">{formatGBP(potentialSavings)}</p>
              <p className="text-green-300">Potential Annual Savings</p>
              <p className="text-xs text-slate-400 mt-1">(via transparency + smart contracts)</p>
            </div>
          </div>
          
          <div className="grid grid-cols-4 gap-6">
            <ImpactMetric label="Corruption Reduction" value="-80%" color="green" />
            <ImpactMetric label="Fraud Prevention" value="-90%" color="green" />
            <ImpactMetric label="Audit Cost" value="-95%" color="green" />
            <ImpactMetric label="Citizen Trust" value="+200%" color="cyan" />
          </div>
        </div>

        {/* How Blockchain Transparency Works */}
        <div className="rounded-2xl bg-slate-800/50 border border-blue-500/30 p-8">
          <h3 className="text-2xl font-bold text-white mb-6">How OASIS Oracle Tracks Government Spending</h3>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <div className="space-y-4">
              <h4 className="text-lg font-semibold text-cyan-400">Traditional System (Current)</h4>
              <ul className="space-y-2 text-sm text-slate-300">
                <li className="flex items-start gap-2">
                  <span className="text-red-400 mt-0.5">✗</span>
                  <span>Opaque spending (hidden from public)</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-red-400 mt-0.5">✗</span>
                  <span>Backroom procurement deals</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-red-400 mt-0.5">✗</span>
                  <span>Corruption (PPE scandal: £37bn wasted)</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-red-400 mt-0.5">✗</span>
                  <span>Audits take months/years</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-red-400 mt-0.5">✗</span>
                  <span>No accountability (politicians blame "the system")</span>
                </li>
              </ul>
            </div>
            
            <div className="space-y-4">
              <h4 className="text-lg font-semibold text-green-400">OASIS Oracle (Blockchain)</h4>
              <ul className="space-y-2 text-sm text-slate-300">
                <li className="flex items-start gap-2">
                  <span className="text-green-400 mt-0.5">✓</span>
                  <span>Every pound visible in real-time</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-green-400 mt-0.5">✓</span>
                  <span>Smart contracts enforce competitive bidding</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-green-400 mt-0.5">✓</span>
                  <span>Corruption impossible (immutable records)</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-green-400 mt-0.5">✓</span>
                  <span>Instant auditing (AI flags suspicious transactions)</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-green-400 mt-0.5">✓</span>
                  <span>Complete accountability (blockchain proof of who spent what)</span>
                </li>
              </ul>
            </div>
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
    green: 'text-green-400 border-green-500/30 bg-green-900/20',
    blue: 'text-blue-400 border-blue-500/30 bg-blue-900/20',
  };

  return (
    <div className={`rounded-xl p-4 border ${colorClasses[color as keyof typeof colorClasses] || colorClasses.cyan}`}>
      <p className="text-xs uppercase tracking-wide text-slate-400 mb-2">{label}</p>
      <p className={`text-3xl font-bold mb-1`}>{value}</p>
      {subtitle && <p className="text-xs text-slate-400">{subtitle}</p>}
    </div>
  );
}

function SolutionCard({ icon, title, before, after, savings, impact }: { icon: string; title: string; before: string; after: string; savings: string; impact: string }) {
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
          <p className="text-xs uppercase text-green-400 mb-1">Savings</p>
          <p className="text-lg font-bold text-green-400">{savings}</p>
        </div>
        <div>
          <p className="text-xs uppercase text-cyan-400 mb-1">Impact</p>
          <p className="text-sm text-cyan-400">{impact}</p>
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
      <p className="text-2xl font-bold">{value}</p>
    </div>
  );
}

