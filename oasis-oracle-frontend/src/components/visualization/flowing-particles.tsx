"use client";

import { useRef, useMemo } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";

type FlowingParticlesProps = {
  from: [number, number, number];
  to: [number, number, number];
  amount: number;
  isActive: boolean;
};

export function FlowingParticles({ from, to, amount, isActive }: FlowingParticlesProps) {
  const particlesRef = useRef<THREE.Points>(null);
  
  // Number of particles based on amount (1 particle per $50M for better visibility)
  const count = Math.max(10, Math.floor(amount / 50_000_000));
  
  console.log("✨ FlowingParticles render:", { from, to, amount, isActive, count });
  
  const { positions, colors } = useMemo(() => {
    const positions = new Float32Array(count * 3);
    const colors = new Float32Array(count * 3);
    
    for (let i = 0; i < count; i++) {
      const t = i / count;
      positions[i * 3] = from[0] + (to[0] - from[0]) * t;
      positions[i * 3 + 1] = from[1] + (to[1] - from[1]) * t;
      positions[i * 3 + 2] = from[2] + (to[2] - from[2]) * t;
      
      // Cyan color (#22d3ee)
      colors[i * 3] = 0.13;     // R
      colors[i * 3 + 1] = 0.83; // G
      colors[i * 3 + 2] = 0.93; // B
    }
    
    return { positions, colors };
  }, [from, to, count]);
  
  // Animate particles flowing from -> to
  useFrame((state) => {
    if (particlesRef.current && isActive) {
      const posAttr = particlesRef.current.geometry.attributes.position;
      const positions = posAttr.array as Float32Array;
      
      for (let i = 0; i < count; i++) {
        // Calculate progress (0 to 1, loops)
        let t = (i / count + state.clock.elapsedTime * 0.15) % 1;
        
        // Update position along path
        positions[i * 3] = from[0] + (to[0] - from[0]) * t;
        positions[i * 3 + 1] = from[1] + (to[1] - from[1]) * t;
        positions[i * 3 + 2] = from[2] + (to[2] - from[2]) * t;
      }
      
      posAttr.needsUpdate = true;
    }
  });
  
  if (!isActive) return null;
  
  return (
    <points ref={particlesRef}>
      <bufferGeometry>
        <bufferAttribute
          attach="attributes-position"
          count={count}
          array={positions}
          itemSize={3}
        />
        <bufferAttribute
          attach="attributes-color"
          count={count}
          array={colors}
          itemSize={3}
        />
      </bufferGeometry>
      <pointsMaterial
        size={0.8}
        vertexColors
        sizeAttenuation={false}
        transparent
        opacity={1.0}
        blending={THREE.AdditiveBlending}
      />
    </points>
  );
}

