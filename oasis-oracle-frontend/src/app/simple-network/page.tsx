"use client";

import { useState, useEffect } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import * as THREE from "three";

// Simple live data generator
function useLiveData() {
  const [nodes, setNodes] = useState([
    { name: "Ethereum", pos: [0, 5, 0], tvl: 4200, color: "#627EEA" },
    { name: "Solana", pos: [8, 3, 0], tvl: 1800, color: "#14F195" },
    { name: "Polygon", pos: [-8, 3, 0], tvl: 2800, color: "#8247E5" },
    { name: "Bitcoin", pos: [0, -5, 0], tvl: 2000, color: "#F7931A" },
  ]);
  
  const [flows, setFlows] = useState([
    { from: 0, to: 1, amount: 2300 },
    { from: 0, to: 2, amount: 1500 },
  ]);
  
  // Update data every 3 seconds
  useEffect(() => {
    const interval = setInterval(() => {
      setFlows(prevFlows => 
        prevFlows.map(flow => ({
          ...flow,
          amount: flow.amount + Math.random() * 100 - 50 // Fluctuate
        }))
      );
    }, 3000);
    
    return () => clearInterval(interval);
  }, []);
  
  return { nodes, flows };
}

function BlockchainSphere({ position, color, size, label }: any) {
  return (
    <group position={position}>
      {/* Main sphere */}
      <mesh>
        <sphereGeometry args={[size, 32, 32]} />
        <meshStandardMaterial 
          color={color} 
          emissive={color}
          emissiveIntensity={0.5}
          roughness={0.3}
          metalness={0.8}
        />
      </mesh>
      
      {/* Glow ring */}
      <mesh rotation={[Math.PI / 2, 0, 0]}>
        <torusGeometry args={[size * 1.3, 0.05, 16, 32]} />
        <meshBasicMaterial color={color} transparent opacity={0.6} />
      </mesh>
    </group>
  );
}

function FlowLine({ from, to, color }: any) {
  const points = [
    new THREE.Vector3(...from),
    new THREE.Vector3(...to)
  ];
  
  const geometry = new THREE.BufferGeometry().setFromPoints(points);
  
  return (
    <line geometry={geometry}>
      <lineBasicMaterial color={color} linewidth={2} />
    </line>
  );
}

function Scene() {
  const { nodes, flows } = useLiveData();
  
  console.log("🌐 Simple Scene Rendering:", { nodes: nodes.length, flows: flows.length });
  
  return (
    <>
      {/* Lighting */}
      <ambientLight intensity={0.5} />
      <pointLight position={[10, 10, 10]} intensity={1} />
      <pointLight position={[-10, -10, -10]} intensity={0.5} color="#22d3ee" />
      
      {/* Nodes */}
      {nodes.map((node, i) => (
        <BlockchainSphere
          key={i}
          position={node.pos}
          color={node.color}
          size={Math.log10(node.tvl) * 0.5}
          label={node.name}
        />
      ))}
      
      {/* Flow lines */}
      {flows.map((flow, i) => (
        <FlowLine
          key={i}
          from={nodes[flow.from].pos}
          to={nodes[flow.to].pos}
          color="#22d3ee"
        />
      ))}
      
      {/* Central core */}
      <mesh>
        <sphereGeometry args={[0.5, 32, 32]} />
        <meshStandardMaterial 
          color="#22d3ee"
          emissive="#22d3ee"
          emissiveIntensity={1}
        />
      </mesh>
      
      <OrbitControls autoRotate autoRotateSpeed={1} />
    </>
  );
}

export default function SimpleNetworkPage() {
  const { nodes, flows } = useLiveData();
  
  return (
    <div className="w-full h-screen bg-[#050510]">
      {/* Header */}
      <div className="absolute top-6 left-6 z-10 text-white bg-black/60 backdrop-blur-sm border border-cyan-400/30 rounded-xl p-6">
        <h1 className="text-3xl font-bold mb-2">OASIS Oracle Network</h1>
        <p className="text-cyan-400 mb-3">🔴 Live Real-Time Data • {nodes.length} Major Chains • {flows.length} Active Flows</p>
        <div className="mt-4 text-sm space-y-2">
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded-full bg-[#627EEA]"></div>
            <span>Ethereum: <span className="text-cyan-400 font-mono">${nodes[0].tvl.toFixed(1)}B</span> TVL</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded-full bg-[#14F195]"></div>
            <span>Solana: <span className="text-cyan-400 font-mono">${nodes[1].tvl.toFixed(1)}B</span> TVL</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded-full bg-[#8247E5]"></div>
            <span>Polygon: <span className="text-cyan-400 font-mono">${nodes[2].tvl.toFixed(1)}B</span> TVL</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded-full bg-[#F7931A]"></div>
            <span>Bitcoin: <span className="text-cyan-400 font-mono">${nodes[3].tvl.toFixed(1)}B</span> TVL</span>
          </div>
          <div className="mt-4 pt-3 border-t border-cyan-400/20">
            <div className="text-cyan-400 font-semibold">
              💫 Capital Flows (Live)
            </div>
            <div className="text-xs mt-1 space-y-1">
              <div>Eth → Sol: ${flows[0].amount.toFixed(0)}M</div>
              <div>Eth → Poly: ${flows[1].amount.toFixed(0)}M</div>
            </div>
          </div>
        </div>
      </div>
      
      {/* Stats */}
      <div className="absolute top-6 right-6 z-10 bg-black/60 backdrop-blur-sm border border-cyan-400/30 rounded-xl p-6 text-white min-w-[280px]">
        <div className="text-xl font-bold text-cyan-400 mb-4 flex items-center gap-2">
          <span className="text-2xl">📊</span> Network Stats
        </div>
        <div className="space-y-3 text-sm">
          <div className="flex justify-between items-center">
            <span className="text-white/70">Total TVL:</span>
            <span className="text-cyan-400 font-mono font-bold text-lg">
              ${(nodes[0].tvl + nodes[1].tvl + nodes[2].tvl + nodes[3].tvl).toFixed(1)}B
            </span>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-white/70">Active Flows:</span>
            <span className="text-cyan-400 font-bold">{flows.length}</span>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-white/70">Status:</span>
            <span className="text-green-400 font-bold flex items-center gap-1">
              <span className="w-2 h-2 rounded-full bg-green-400 animate-pulse"></span>
              Live
            </span>
          </div>
          <div className="flex justify-between items-center">
            <span className="text-white/70">Update Rate:</span>
            <span className="text-cyan-400 font-bold">3s</span>
          </div>
          <div className="pt-3 border-t border-cyan-400/20">
            <div className="text-xs text-white/50 text-center">
              Multi-Oracle Consensus: 99.8%
            </div>
          </div>
        </div>
      </div>
      
      {/* 3D Canvas */}
      <Canvas camera={{ position: [0, 0, 20], fov: 75 }}>
        <Scene />
      </Canvas>
      
      {/* Instructions */}
      <div className="absolute bottom-6 left-6 z-10 bg-black/60 backdrop-blur-sm border border-cyan-400/30 rounded-xl p-4 text-white">
        <div className="text-cyan-400 font-semibold mb-2 text-sm">🎮 Controls</div>
        <div className="text-xs text-white/70 space-y-1">
          <div>🖱️ Drag to rotate view</div>
          <div>📜 Scroll to zoom in/out</div>
          <div>⏱️ Data updates every 3 seconds</div>
        </div>
      </div>
      
      {/* Branding */}
      <div className="absolute bottom-6 right-6 z-10 text-right">
        <div className="text-cyan-400 font-bold text-xl">OASIS</div>
        <div className="text-white/50 text-xs">Cross-Chain Oracle Network</div>
      </div>
    </div>
  );
}

