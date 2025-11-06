"use client";

import { useState, Suspense } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls, Stars } from "@react-three/drei";
import Link from "next/link";
import { NHSNode } from "./nhs-node-3d";
import { FlowParticles } from "./flow-particles-3d";
import { nhsNodes3D, patientFlows3D, getNHSStats, formatWaitTime } from "@/lib/reform-uk-data/nhs-visualization-data";
import type { NHSNode3D } from "@/lib/reform-uk-data/nhs-visualization-data";

export default function NHSDashboard() {
  const [selectedHospital, setSelectedHospital] = useState<NHSNode3D | null>(null);
  const [showLabels, setShowLabels] = useState(true);
  const [autoRotate, setAutoRotate] = useState(true);
  const stats = getNHSStats();

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-blue-900 to-slate-900">
      {/* Back Button */}
      <div className="absolute top-4 left-4 z-50">
        <Link 
          href="/reform-uk"
          className="px-4 py-2 bg-slate-800/90 backdrop-blur-sm text-white rounded-lg border border-slate-600 hover:bg-slate-700 transition-colors"
        >
          ← Back to Reform UK
        </Link>
      </div>

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
            <StatCard label="Patients Waiting" value={stats.totalWaitingList.toLocaleString()} color="text-red-400" subtitle="Across all hospitals" />
            <StatCard label="Hospitals" value={stats.hospitalsMonitored.toString()} color="text-cyan-400" subtitle="Real-time tracking" />
            <StatCard label="Bed Capacity" value={`${stats.avgCapacity}%`} color={stats.avgCapacity > 90 ? "text-red-400" : "text-yellow-400"} subtitle={`${stats.totalBedsAvailable} available`} />
            <StatCard label="Transfers" value={stats.totalTransfers.toString()} color="text-green-400" subtitle="Patients moving now" />
            <StatCard label="Total Patients" value={stats.totalPatients.toLocaleString()} color="text-blue-400" subtitle="In system" />
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto p-8 space-y-8">
        
        {/* 3D Visualization + Details */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          
          {/* 3D Hospital Network */}
          <div className="lg:col-span-2">
            <div className="rounded-2xl bg-slate-900 border border-cyan-500/30 h-[600px] relative overflow-hidden">
              {/* Controls */}
              <div className="absolute top-4 right-4 z-10 flex gap-2">
                <button
                  onClick={() => setShowLabels(!showLabels)}
                  className="px-3 py-2 bg-slate-800/90 backdrop-blur-sm text-white text-sm rounded-lg border border-slate-600 hover:bg-slate-700"
                >
                  {showLabels ? '🏷️ Labels ON' : '🏷️ Labels OFF'}
                </button>
                <button
                  onClick={() => setAutoRotate(!autoRotate)}
                  className="px-3 py-2 bg-slate-800/90 backdrop-blur-sm text-white text-sm rounded-lg border border-slate-600 hover:bg-slate-700"
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
                <ambientLight intensity={0.3} />
                <pointLight position={[20, 20, 20]} intensity={1} color="#ffffff" />
                <pointLight position={[-20, -20, -20]} intensity={0.5} color="#22d3ee" />
                
                <Suspense fallback={null}>
                  <Stars radius={100} depth={50} count={5000} factor={4} saturation={0} fade speed={0.5} />
                  
                  {nhsNodes3D.map((hospital) => (
                    <NHSNode
                      key={hospital.id}
                      node={hospital}
                      onClick={setSelectedHospital}
                      showLabels={showLabels}
                    />
                  ))}
                  
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
                          color="#22c55e"
                        />
                      );
                    })}
                  
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

          {/* Selected Hospital Details */}
          <div className="space-y-4">
            <h3 className="text-xl font-bold text-white">
              {selectedHospital ? selectedHospital.name : 'Click a Hospital'}
            </h3>
            
            {selectedHospital ? (
              <div className="space-y-3">
                <DetailCard label="Waiting List" value={selectedHospital.waitingList.toLocaleString()} color="red" subtitle={`Avg: ${formatWaitTime(selectedHospital.avgWaitTime)}`} />
                <DetailCard label="Capacity" value={`${selectedHospital.capacity}%`} color={selectedHospital.capacity > 90 ? 'red' : 'cyan'} subtitle={`${selectedHospital.bedsAvailable}/${selectedHospital.beds} beds`} />
                <DetailCard label="Staff on Duty" value={selectedHospital.staffAvailable.toLocaleString()} color="cyan" subtitle={`of ${selectedHospital.staff.toLocaleString()}`} />
              </div>
            ) : (
              <p className="text-slate-400 text-sm">Select a hospital sphere in the 3D view to see real-time details</p>
            )}
          </div>
        </div>

        {/* OASIS Impact */}
        <div className="rounded-2xl bg-gradient-to-br from-green-900/30 to-blue-900/30 border border-green-500/30 p-8">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="text-2xl font-bold text-white mb-2">OASIS Oracle NHS Impact</h3>
              <p className="text-green-300">Delivering Reform UK's Zero Waiting Lists pledge</p>
            </div>
            <div className="text-right">
              <p className="text-5xl font-bold text-green-400">£25-42bn</p>
              <p className="text-green-300">Annual Savings</p>
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
      <p className="text-xs uppercase text-slate-400 mb-1">{label}</p>
      <p className={`text-2xl font-bold ${color}`}>{value}</p>
      {subtitle && <p className="text-xs text-slate-500 mt-1">{subtitle}</p>}
    </div>
  );
}

function DetailCard({ label, value, color, subtitle }: { label: string; value: string; color: string; subtitle?: string }) {
  const colors = {
    red: 'text-red-400 border-red-500/30 bg-red-900/20',
    cyan: 'text-cyan-400 border-cyan-500/30 bg-cyan-900/20',
  };
  
  return (
    <div className={`rounded-xl p-4 border ${colors[color as keyof typeof colors]}`}>
      <p className="text-xs uppercase text-slate-400 mb-2">{label}</p>
      <p className="text-3xl font-bold mb-1">{value}</p>
      {subtitle && <p className="text-xs text-slate-400">{subtitle}</p>}
    </div>
  );
}



