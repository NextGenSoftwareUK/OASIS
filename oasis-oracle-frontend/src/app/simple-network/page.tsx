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
      <div className="absolute top-6 left-6 z-10 text-white">
        <h1 className="text-3xl font-bold mb-2">OASIS Oracle Network (Simplified)</h1>
        <p className="text-cyan-400">Live Data • {nodes.length} Chains • {flows.length} Active Flows</p>
        <div className="mt-4 text-sm space-y-1">
          <div>🌐 Ethereum: ${nodes[0].tvl}B TVL</div>
          <div>⚡ Solana: ${nodes[1].tvl}B TVL</div>
          <div>💜 Polygon: ${nodes[2].tvl}B TVL</div>
          <div>🟠 Bitcoin: ${nodes[3].tvl}B TVL</div>
          <div className="mt-3 text-cyan-400">
            💫 Flow 1: ${flows[0].amount.toFixed(0)}M (live updating)
          </div>
        </div>
      </div>
      
      {/* Stats */}
      <div className="absolute top-6 right-6 z-10 bg-black/50 backdrop-blur-sm border border-cyan-400/30 rounded-lg p-4 text-white">
        <div className="text-xl font-bold text-cyan-400 mb-2">Network Stats</div>
        <div className="space-y-1 text-sm">
          <div>Total TVL: $10.8B</div>
          <div>Active Flows: {flows.length}</div>
          <div>Status: 🟢 Healthy</div>
        </div>
      </div>
      
      {/* 3D Canvas */}
      <Canvas camera={{ position: [0, 0, 20], fov: 75 }}>
        <Scene />
      </Canvas>
      
      {/* Instructions */}
      <div className="absolute bottom-6 left-6 z-10 text-white/70 text-sm">
        🖱️ Drag to rotate • 📜 Scroll to zoom • Data updates every 3 seconds
      </div>
    </div>
  );
}

