"use client";

import { useState, useRef } from "react";
import { Canvas, useFrame } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import * as THREE from "three";
import { OracleLayout } from "@/components/layout/oracle-layout";
import { blockchain3DNodes, capitalFlows3D, getChainPosition } from "@/lib/visualization-data";
import type { ChainNode3D } from "@/lib/visualization-data";

// Simple blockchain sphere (no drei dependencies)
function SimpleBlockchainNode({ node, onClick }: { node: ChainNode3D; onClick: (node: ChainNode3D) => void }) {
  const meshRef = useRef<THREE.Mesh>(null);
  
  useFrame((state) => {
    if (meshRef.current) {
      const pulse = Math.sin(state.clock.elapsedTime * 2) * 0.05;
      meshRef.current.scale.setScalar(1 + pulse);
    }
  });
  
  const size = Math.max(0.5, Math.log10(node.tvl / 100_000_000) * 0.8 + 0.5);
  
  return (
    <mesh 
      ref={meshRef}
      position={node.position}
      onClick={() => onClick(node)}
    >
      <sphereGeometry args={[size, 32, 32]} />
      <meshStandardMaterial
        color={node.color}
        emissive={node.color}
        emissiveIntensity={0.6}
        metalness={0.4}
        roughness={0.3}
      />
    </mesh>
  );
}

// Simple flow line (no drei dependencies)
function SimpleFlowLine({ from, to, isActive }: { from: [number, number, number]; to: [number, number, number]; isActive: boolean }) {
  const points = [
    new THREE.Vector3(...from),
    new THREE.Vector3(...to)
  ];
  
  const geometry = new THREE.BufferGeometry().setFromPoints(points);
  
  return (
    <line geometry={geometry}>
      <lineBasicMaterial 
        color={isActive ? "#22d3ee" : "#64748b"} 
        transparent
        opacity={isActive ? 0.8 : 0.3}
        linewidth={2}
      />
    </line>
  );
}

// Central oracle core
function OracleCore() {
  const meshRef = useRef<THREE.Mesh>(null);
  
  useFrame((state) => {
    if (meshRef.current) {
      meshRef.current.rotation.y = state.clock.elapsedTime * 0.5;
      const pulse = Math.sin(state.clock.elapsedTime * 3) * 0.1;
      meshRef.current.scale.setScalar(1 + pulse);
    }
  });
  
  return (
    <mesh ref={meshRef}>
      <sphereGeometry args={[0.8, 32, 32]} />
      <meshStandardMaterial
        color="#22d3ee"
        emissive="#22d3ee"
        emissiveIntensity={1.5}
        metalness={0.8}
        roughness={0.2}
      />
    </mesh>
  );
}

export default function NetworkPage() {
  const [selectedChain, setSelectedChain] = useState<ChainNode3D | null>(null);
  const [showFlows, setShowFlows] = useState(true);
  
  const totalTVL = blockchain3DNodes.reduce((sum, node) => sum + node.tvl, 0);
  const activeFlows = capitalFlows3D.filter(f => f.isActive).length;
  
  console.log("🌐 Complex Network Rendering:", {
    nodes: blockchain3DNodes.length,
    flows: capitalFlows3D.length,
    activeFlows
  });
  
  return (
    <OracleLayout>
      <div className="space-y-6">
        {/* Hero Section */}
        <div className="flex items-start justify-between">
          <div className="space-y-3">
            <h1 className="text-4xl font-bold tracking-tight text-[var(--color-foreground)]">
              Oracle Network Visualization
            </h1>
            <p className="text-lg text-[var(--muted)] max-w-3xl">
              Real-time 3D visualization of the OASIS oracle network across 20+ blockchains
            </p>
          </div>
          
          {/* Controls */}
          <div className="flex gap-2">
            <button
              onClick={() => setShowFlows(!showFlows)}
              className={`px-4 py-2 rounded-lg ${
                showFlows 
                  ? 'bg-cyan-400 text-black' 
                  : 'bg-gray-700 text-white'
              }`}
            >
              Flows: {showFlows ? 'ON' : 'OFF'}
            </button>
          </div>
        </div>
        
        {/* 3D Visualization */}
        <div className="relative h-[700px] w-full rounded-2xl overflow-hidden border border-cyan-400/30 shadow-[0_20px_50px_rgba(13,148,136,0.25)]">
          <Canvas camera={{ position: [0, 0, 50], fov: 75 }}>
            {/* Lighting */}
            <ambientLight intensity={0.4} />
            <pointLight position={[20, 20, 20]} intensity={1} color="#ffffff" />
            <pointLight position={[-20, -20, -20]} intensity={0.5} color="#22d3ee" />
            
            {/* Oracle core */}
            <OracleCore />
            
            {/* Blockchain nodes */}
            {blockchain3DNodes.map((node) => (
              <SimpleBlockchainNode
                key={node.id}
                node={node}
                onClick={setSelectedChain}
              />
            ))}
            
            {/* Flow lines */}
            {showFlows && capitalFlows3D.map((flow, index) => {
              const fromPos = getChainPosition(flow.from);
              const toPos = getChainPosition(flow.to);
              
              if (!fromPos || !toPos) return null;
              
              return (
                <SimpleFlowLine
                  key={`flow-${index}`}
                  from={fromPos}
                  to={toPos}
                  isActive={flow.isActive}
                />
              );
            })}
            
            <OrbitControls 
              autoRotate 
              autoRotateSpeed={0.5}
              enableDamping
              dampingFactor={0.05}
            />
          </Canvas>
          
          {/* Stats Overlay */}
          <div className="absolute top-4 right-4 bg-black/70 backdrop-blur-sm border border-cyan-400/30 rounded-xl p-4 text-white">
            <div className="text-lg font-bold text-cyan-400 mb-2">Network Stats</div>
            <div className="space-y-1 text-sm">
              <div>Nodes: <span className="text-cyan-400">{blockchain3DNodes.length}</span></div>
              <div>Total TVL: <span className="text-cyan-400">${(totalTVL / 1e9).toFixed(1)}B</span></div>
              <div>Active Flows: <span className="text-cyan-400">{activeFlows}</span></div>
              <div>Status: <span className="text-green-400">🟢 Live</span></div>
            </div>
          </div>
          
          {/* Selected Chain Info */}
          {selectedChain && (
            <div className="absolute bottom-4 left-4 bg-black/70 backdrop-blur-sm border border-cyan-400/30 rounded-xl p-4 text-white max-w-sm">
              <button 
                onClick={() => setSelectedChain(null)}
                className="absolute top-2 right-2 text-white/50 hover:text-white"
              >
                ✕
              </button>
              <h3 className="text-xl font-bold text-cyan-400 mb-2">{selectedChain.name}</h3>
              <div className="space-y-1 text-sm">
                <div>TVL: <span className="text-cyan-400">${(selectedChain.tvl / 1e9).toFixed(2)}B</span></div>
                <div>TPS: <span className="text-cyan-400">{selectedChain.tps}</span></div>
                <div>Health: <span className="text-green-400">{selectedChain.health}</span></div>
              </div>
            </div>
          )}
        </div>
        
        {/* Info Card */}
        <div className="rounded-2xl border border-cyan-400/30 bg-[rgba(34,211,238,0.05)] p-6">
          <h2 className="text-2xl font-bold text-white mb-3">
            20+ Blockchain Network
          </h2>
          <p className="text-[var(--muted)]">
            This visualization shows all {blockchain3DNodes.length} blockchain nodes in the OASIS oracle network. 
            Each sphere represents a blockchain, sized by total value locked. 
            {showFlows ? ` Cyan lines show ${activeFlows} active capital flows between chains.` : ' Toggle flows to see capital movement.'}
          </p>
        </div>
      </div>
    </OracleLayout>
  );
}
