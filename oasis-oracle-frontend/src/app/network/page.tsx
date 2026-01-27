// @ts-nocheck - Three.js types have compatibility issues
"use client";

import { useState, useRef } from "react";
import { Canvas, useFrame } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import * as THREE from "three";
import { OracleLayout } from "@/components/layout/oracle-layout";
import { blockchain3DNodes, capitalFlows3D, getChainPosition } from "@/lib/visualization-data";
import type { ChainNode3D } from "@/lib/visualization-data";
import { getCollateralForChain, formatTimeToMaturity } from "@/lib/collateral-data";

// Blockchain node with label
function BlockchainNodeWithLabel({ node, onClick, showLabels }: { node: ChainNode3D; onClick: (node: ChainNode3D) => void; showLabels: boolean }) {
  const meshRef = useRef<THREE.Mesh>(null);
  const [hovered, setHovered] = useState(false);
  
  // @ts-ignore - useFrame state type issue
  useFrame((state) => {
    if (meshRef.current) {
      const pulse = Math.sin(state.clock.elapsedTime * 2) * 0.05;
      meshRef.current.scale.setScalar(1 + pulse);
    }
  });
  
  const size = Math.max(0.8, Math.log10(node.tvl / 100_000_000) * 0.8 + 0.5);
  
  return (
    <group position={node.position}>
      {/* Main sphere */}
      <mesh 
        ref={meshRef}
        onClick={() => onClick(node)}
        onPointerOver={() => setHovered(true)}
        onPointerOut={() => setHovered(false)}
      >
        <sphereGeometry args={[size, 32, 32]} />
        <meshStandardMaterial
          color={node.color}
          emissive={node.color}
          emissiveIntensity={hovered ? 1.0 : 0.6}
          metalness={0.5}
          roughness={0.2}
        />
      </mesh>
      
      {/* Glow ring on ground */}
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, -0.1, 0]}>
        <ringGeometry args={[size * 1.2, size * 1.5, 32]} />
        <meshBasicMaterial
          color={node.color}
          transparent
          opacity={0.3}
          side={THREE.DoubleSide}
        />
      </mesh>
      
      {/* Hover effect */}
      {hovered && (
        <mesh>
          <sphereGeometry args={[size * 1.4, 32, 32]} />
          <meshBasicMaterial
            color={node.color}
            transparent
            opacity={0.2}
            side={THREE.BackSide}
          />
        </mesh>
      )}
    </group>
  );
}

// Animated flow line with particles
function FlowLineWithParticles({ from, to, isActive, amount }: { from: [number, number, number]; to: [number, number, number]; isActive: boolean; amount: number }) {
  const lineRef = useRef<THREE.Line>(null);
  const particlesRef = useRef<THREE.Points>(null);
  
  // Animate particles along the line
  useFrame((state) => {
    if (particlesRef.current && isActive) {
      const positions = particlesRef.current.geometry.attributes.position.array as Float32Array;
      const time = state.clock.elapsedTime;
      
      for (let i = 0; i < positions.length; i += 3) {
        const progress = ((time * 0.2 + i / positions.length) % 1);
        positions[i] = from[0] + (to[0] - from[0]) * progress;
        positions[i + 1] = from[1] + (to[1] - from[1]) * progress + Math.sin(progress * Math.PI) * 0.5; // Arc
        positions[i + 2] = from[2] + (to[2] - from[2]) * progress;
      }
      
      particlesRef.current.geometry.attributes.position.needsUpdate = true;
    }
  });
  
  // Create curved line path
  const curve = new THREE.CatmullRomCurve3([
    new THREE.Vector3(...from),
    new THREE.Vector3(
      (from[0] + to[0]) / 2,
      (from[1] + to[1]) / 2 + 2, // Arc height
      (from[2] + to[2]) / 2
    ),
    new THREE.Vector3(...to)
  ]);
  
  const linePoints = curve.getPoints(50);
  const lineGeometry = new THREE.BufferGeometry().setFromPoints(linePoints);
  
  // Create particles
  const particleCount = Math.max(5, Math.floor(amount / 200_000_000));
  const particlePositions = new Float32Array(particleCount * 3);
  const particleColors = new Float32Array(particleCount * 3);
  
  for (let i = 0; i < particleCount; i++) {
    const t = i / particleCount;
    particlePositions[i * 3] = from[0] + (to[0] - from[0]) * t;
    particlePositions[i * 3 + 1] = from[1] + (to[1] - from[1]) * t;
    particlePositions[i * 3 + 2] = from[2] + (to[2] - from[2]) * t;
    
    // Cyan color
    particleColors[i * 3] = 0.13;
    particleColors[i * 3 + 1] = 0.83;
    particleColors[i * 3 + 2] = 0.93;
  }
  
  const particleGeometry = new THREE.BufferGeometry();
  particleGeometry.setAttribute('position', new THREE.BufferAttribute(particlePositions, 3));
  particleGeometry.setAttribute('color', new THREE.BufferAttribute(particleColors, 3));
  
  return (
    <group>
      {/* Flow line */}
      <line ref={lineRef} geometry={lineGeometry}>
        <lineBasicMaterial 
          color={isActive ? "#22d3ee" : "#64748b"} 
          transparent
          opacity={isActive ? 0.6 : 0.2}
          linewidth={3}
        />
      </line>
      
      {/* Animated particles */}
      {isActive && (
        <points ref={particlesRef} geometry={particleGeometry}>
          <pointsMaterial
            size={0.4}
            vertexColors
            transparent
            opacity={1.0}
            sizeAttenuation={false}
          />
        </points>
      )}
    </group>
  );
}

// Grid floor for reference
function GridFloor() {
  return (
    <>
      <gridHelper args={[50, 50, "#22d3ee", "#1a1a2e"]} position={[0, -0.5, 0]} />
      <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, -0.51, 0]}>
        <planeGeometry args={[50, 50]} />
        <meshBasicMaterial color="#050510" transparent opacity={0.8} />
      </mesh>
    </>
  );
}

// Vertical beam from node to show it's active
function NodeBeam({ position, color, active }: { position: [number, number, number]; color: string; active: boolean }) {
  const meshRef = useRef<THREE.Mesh>(null);
  
  useFrame((state) => {
    if (meshRef.current && active) {
      meshRef.current.material.opacity = 0.3 + Math.sin(state.clock.elapsedTime * 3) * 0.2;
    }
  });
  
  if (!active) return null;
  
  return (
    <mesh ref={meshRef} position={[position[0], 5, position[2]]}>
      <cylinderGeometry args={[0.1, 0.1, 10, 8]} />
      <meshBasicMaterial color={color} transparent opacity={0.3} />
    </mesh>
  );
}

export default function NetworkPage() {
  const [selectedChain, setSelectedChain] = useState<ChainNode3D | null>(null);
  const [showFlows, setShowFlows] = useState(true);
  const [showLabels, setShowLabels] = useState(true);
  const [showGrid, setShowGrid] = useState(true);
  
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
              className={`px-4 py-2 rounded-lg text-sm font-semibold ${
                showFlows 
                  ? 'bg-cyan-400 text-black' 
                  : 'bg-gray-700 text-white'
              }`}
            >
              Capital Flows
            </button>
            <button
              onClick={() => setShowGrid(!showGrid)}
              className={`px-4 py-2 rounded-lg text-sm font-semibold ${
                showGrid 
                  ? 'bg-cyan-400 text-black' 
                  : 'bg-gray-700 text-white'
              }`}
            >
              Grid
            </button>
          </div>
        </div>
        
        {/* 3D Visualization */}
        <div className="relative h-[700px] w-full rounded-2xl overflow-hidden border border-cyan-400/30 shadow-[0_20px_50px_rgba(13,148,136,0.25)] bg-[#050510]">
          <Canvas camera={{ position: [0, 30, 35], fov: 60 }}>
            {/* Lighting */}
            <ambientLight intensity={0.5} />
            <pointLight position={[0, 20, 0]} intensity={1.5} color="#ffffff" />
            <pointLight position={[20, 10, 20]} intensity={0.8} color="#22d3ee" />
            <pointLight position={[-20, 10, -20]} intensity={0.8} color="#8247E5" />
            
            {/* Grid floor */}
            {showGrid && <GridFloor />}
            
            {/* Blockchain nodes */}
            {blockchain3DNodes.map((node) => (
              <BlockchainNodeWithLabel
                key={node.id}
                node={node}
                onClick={setSelectedChain}
                showLabels={showLabels}
              />
            ))}
            
            {/* Node beams */}
            {blockchain3DNodes.map((node) => (
              <NodeBeam
                key={`beam-${node.id}`}
                position={node.position}
                color={node.color}
                active={node.health === "healthy"}
              />
            ))}
            
            {/* Flow lines with particles */}
            {showFlows && capitalFlows3D.map((flow, index) => {
              const fromPos = getChainPosition(flow.from);
              const toPos = getChainPosition(flow.to);
              
              if (!fromPos || !toPos) return null;
              
              return (
                <FlowLineWithParticles
                  key={`flow-${index}`}
                  from={fromPos}
                  to={toPos}
                  amount={flow.amount}
                  isActive={flow.isActive}
                />
              );
            })}
            
            <OrbitControls 
              autoRotate 
              autoRotateSpeed={0.3}
              enableDamping
              dampingFactor={0.05}
              minPolarAngle={Math.PI / 6}
              maxPolarAngle={Math.PI / 2}
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
          
          {/* Selected Chain Info with REAL Institutional Collateral Data */}
          {selectedChain && (() => {
            const collateral = getCollateralForChain(selectedChain.name);
            const encumberedAssets = collateral.assets.filter(a => a.encumbered);
            const availableAssets = collateral.assets.filter(a => !a.encumbered);
            
            return (
              <div className="absolute bottom-4 left-4 bg-[rgba(8,11,26,0.95)] backdrop-blur-xl border border-[var(--color-card-border)] rounded-2xl p-6 text-[var(--color-foreground)] max-w-3xl shadow-[0_20px_60px_rgba(13,148,136,0.4)] overflow-y-auto max-h-[80vh]">
                <button 
                  onClick={() => setSelectedChain(null)}
                  className="absolute top-3 right-3 text-[var(--muted)] hover:text-[var(--color-foreground)] text-lg transition-colors"
                >
                  ✕
                </button>
                
                {/* Chain Header */}
                <div className="mb-4 pb-4 border-b border-[var(--color-card-border)]">
                  <h3 className="text-2xl font-bold text-[var(--accent)] mb-1">{selectedChain.name} Collateral Position</h3>
                  <div className="grid grid-cols-3 gap-3 text-sm mt-3">
                    <div className="bg-[rgba(34,211,238,0.08)] rounded-lg p-2 border border-[var(--color-card-border)]">
                      <span className="text-[var(--muted)] text-xs block mb-1">Total Value</span>
                      <div className="text-[var(--accent)] font-mono font-bold text-lg">${(collateral.totalValue / 1e9).toFixed(2)}B</div>
                    </div>
                    <div className="bg-[rgba(34,197,94,0.08)] rounded-lg p-2 border border-[rgba(34,197,94,0.2)]">
                      <span className="text-[var(--muted)] text-xs block mb-1">Available</span>
                      <div className="text-[var(--positive)] font-mono font-bold text-lg">${(collateral.availableValue / 1e9).toFixed(2)}B</div>
                    </div>
                    <div className="bg-[rgba(250,204,21,0.08)] rounded-lg p-2 border border-[rgba(250,204,21,0.2)]">
                      <span className="text-[var(--muted)] text-xs block mb-1">Encumbered</span>
                      <div className="text-[var(--warning)] font-mono font-bold text-lg">${(collateral.encumberedValue / 1e9).toFixed(2)}B</div>
                    </div>
                  </div>
                </div>
                
                <div className="grid grid-cols-2 gap-4">
                  {/* Encumbered Assets (Left Column) */}
                  <div className="bg-[rgba(250,204,21,0.05)] border border-[rgba(250,204,21,0.15)] rounded-xl p-3 backdrop-blur-sm">
                    <h4 className="text-xs font-semibold text-[var(--warning)] mb-3 uppercase tracking-wide flex items-center gap-2">
                      <span className="text-base">🔒</span> Encumbered Assets ({encumberedAssets.length})
                    </h4>
                    <div className="space-y-2 max-h-64 overflow-y-auto pr-2">
                      {encumberedAssets.map((asset, i) => (
                        <div key={i} className="bg-[rgba(5,5,16,0.6)] rounded-lg px-3 py-2 text-xs border border-[var(--color-card-border)] hover:border-[var(--accent)] transition-colors">
                          <div className="flex justify-between items-start mb-1.5">
                            <span className="font-semibold text-[var(--color-foreground)]">{asset.symbol}</span>
                            <span className="text-[var(--warning)] font-mono font-bold">${(asset.valueUSD / 1e6).toFixed(0)}M</span>
                          </div>
                          <div className="text-[var(--muted)] text-[10px] space-y-0.5 leading-relaxed">
                            <div className="text-[var(--color-foreground)]">{asset.assetType}</div>
                            <div className="text-[var(--negative)]">{asset.encumbranceType} • {asset.counterparty}</div>
                            <div className="text-[var(--accent)]">Matures: {formatTimeToMaturity(asset.maturityTime!)}</div>
                            {asset.haircut && <div className="text-[var(--muted)]">Haircut: {(asset.haircut * 100).toFixed(0)}%</div>}
                          </div>
                        </div>
                      ))}
                      {encumberedAssets.length === 0 && (
                        <div className="text-[var(--muted)] text-center py-6 text-xs">No encumbered assets</div>
                      )}
                    </div>
                  </div>
                  
                  {/* Available Assets (Right Column) */}
                  <div className="bg-[rgba(34,197,94,0.05)] border border-[rgba(34,197,94,0.15)] rounded-xl p-3 backdrop-blur-sm">
                    <h4 className="text-xs font-semibold text-[var(--positive)] mb-3 uppercase tracking-wide flex items-center gap-2">
                      <span className="text-base">✅</span> Available Assets ({availableAssets.length})
                    </h4>
                    <div className="space-y-2 max-h-64 overflow-y-auto pr-2">
                      {availableAssets.map((asset, i) => (
                        <div key={i} className="bg-[rgba(5,5,16,0.6)] rounded-lg px-3 py-2 text-xs border border-[var(--color-card-border)] hover:border-[var(--positive)] transition-colors">
                          <div className="flex justify-between items-start mb-1.5">
                            <span className="font-semibold text-[var(--color-foreground)]">{asset.symbol}</span>
                            <span className="text-[var(--positive)] font-mono font-bold">${(asset.valueUSD / 1e6).toFixed(0)}M</span>
                          </div>
                          <div className="text-[var(--muted)] text-[10px] space-y-0.5 leading-relaxed">
                            <div className="text-[var(--color-foreground)]">{asset.assetType}</div>
                            {asset.yieldRate && <div className="text-[var(--positive)]">Yield: {asset.yieldRate.toFixed(2)}%</div>}
                          </div>
                        </div>
                      ))}
                      {availableAssets.length === 0 && (
                        <div className="text-[var(--muted)] text-center py-6 text-xs">No available assets</div>
                      )}
                    </div>
                  </div>
                </div>
                
                {/* Active Transfers */}
                {collateral.activeTransfers.length > 0 && (
                  <div className="mt-4 bg-[rgba(34,211,238,0.05)] border border-[var(--color-card-border)] rounded-xl p-3 backdrop-blur-sm">
                    <h4 className="text-xs font-semibold text-[var(--accent)] mb-3 uppercase tracking-wide flex items-center gap-2">
                      <span className="text-base">⚡</span> Active Transfers ({collateral.activeTransfers.length})
                    </h4>
                    <div className="space-y-2">
                      {collateral.activeTransfers.map((transfer, i) => (
                        <div key={i} className={`flex items-center gap-2 rounded-lg px-3 py-2 text-xs border backdrop-blur-sm ${
                          transfer.direction === 'incoming' 
                            ? 'bg-[rgba(34,197,94,0.08)] border-[rgba(34,197,94,0.2)]' 
                            : 'bg-[rgba(251,146,60,0.08)] border-[rgba(251,146,60,0.2)]'
                        }`}>
                          <span className={`text-lg ${transfer.direction === 'incoming' ? 'text-[var(--positive)]' : 'text-[rgba(251,146,60,0.9)]'}`}>
                            {transfer.direction === 'incoming' ? '←' : '→'}
                          </span>
                          <div className="flex-1">
                            <div className="text-[var(--color-foreground)] font-semibold">{transfer.assetType} • ${(transfer.amountUSD / 1e6).toFixed(0)}M</div>
                            <div className="text-[var(--muted)] text-[10px] mt-0.5">
                              {transfer.purpose} • {transfer.counterparty}
                            </div>
                            <div className="text-[var(--accent)] text-[10px] mt-0.5">
                              ETA: {formatTimeToMaturity(transfer.expectedCompletion)} • {transfer.status}
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
                
                {/* Real-time Oracle Stats */}
                <div className="mt-4 pt-4 border-t border-[var(--color-card-border)]">
                  <div className="flex justify-between items-center text-xs">
                    <div className="flex items-center gap-2">
                      <span className="w-2 h-2 rounded-full bg-[var(--positive)] animate-pulse shadow-[0_0_8px_rgba(34,197,94,0.6)]"></span>
                      <span className="text-[var(--muted)]">Last Update:</span>
                      <span className="text-[var(--accent)] font-mono">{new Date().toLocaleTimeString()}</span>
                    </div>
                    <div className="text-[var(--muted)]">
                      Oracle Consensus: <span className="text-[var(--accent)] font-mono">99.8%</span>
                    </div>
                  </div>
                </div>
              </div>
            );
          })()}
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
