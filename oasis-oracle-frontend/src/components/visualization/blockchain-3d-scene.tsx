"use client";

import { Suspense } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls, Stars } from "@react-three/drei";
import { BlockchainNode } from "./blockchain-node";
import { CapitalFlowLine } from "./capital-flow-line";
import { FlowingParticles } from "./flowing-particles";
import { OracleCore } from "./oracle-core";
import { blockchain3DNodes, capitalFlows3D, getChainPosition } from "@/lib/visualization-data";
import type { ChainNode3D } from "@/lib/visualization-data";

type Blockchain3DSceneProps = {
  showLabels: boolean;
  showFlows: boolean;
  autoRotate: boolean;
  onNodeClick: (node: ChainNode3D) => void;
};

export function Blockchain3DScene({ 
  showLabels, 
  showFlows, 
  autoRotate, 
  onNodeClick 
}: Blockchain3DSceneProps) {
  // Debug logging
  console.log("🌐 3D Scene Render:", {
    totalNodes: blockchain3DNodes.length,
    totalFlows: capitalFlows3D.length,
    activeFlows: capitalFlows3D.filter(f => f.isActive).length,
    showFlows,
    showLabels,
    autoRotate
  });
  
  return (
    <Canvas
      camera={{ position: [0, 0, 45], fov: 75 }}
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
        
        {/* Oracle core (center) */}
        <OracleCore />
        
        {/* Blockchain nodes */}
        {blockchain3DNodes.map((node) => (
          <BlockchainNode
            key={node.id}
            node={node}
            onClick={onNodeClick}
            showLabels={showLabels}
          />
        ))}
        
        {/* Capital flow lines */}
        {capitalFlows3D.map((flow, index) => {
          const fromPos = getChainPosition(flow.from);
          const toPos = getChainPosition(flow.to);
          
          if (!fromPos || !toPos) {
            console.warn(`❌ Flow ${index}: Missing position for ${flow.from} → ${flow.to}`);
            return null;
          }
          
          console.log(`✓ Flow ${index}:`, flow.from, "→", flow.to, `$${(flow.amount / 1e9).toFixed(1)}B`, flow.isActive ? "ACTIVE" : "inactive");
          
          return (
            <CapitalFlowLine
              key={`flow-${index}`}
              from={fromPos}
              to={toPos}
              amount={flow.amount}
              isActive={flow.isActive}
              showFlows={showFlows}
            />
          );
        })}
        
        {/* Flowing particles (animated capital) */}
        {showFlows && capitalFlows3D
          .filter(flow => flow.isActive)
          .map((flow, index) => {
            const fromPos = getChainPosition(flow.from);
            const toPos = getChainPosition(flow.to);
            
            if (!fromPos || !toPos) return null;
            
            console.log(`⚡ Particles ${index}:`, flow.from, "→", flow.to, `${Math.max(10, Math.floor(flow.amount / 50_000_000))} particles`);
            
            return (
              <FlowingParticles
                key={`particles-${index}`}
                from={fromPos}
                to={toPos}
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
          maxDistance={80}
          enablePan={false}
        />
      </Suspense>
    </Canvas>
  );
}

