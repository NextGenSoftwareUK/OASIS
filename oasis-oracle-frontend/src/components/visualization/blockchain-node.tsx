"use client";

import { useRef, useState } from "react";
import { useFrame } from "@react-three/fiber";
import { Text, Sphere } from "@react-three/drei";
import * as THREE from "three";
import type { ChainNode3D } from "@/lib/visualization-data";
import { calculateNodeSize } from "@/lib/visualization-data";

type BlockchainNodeProps = {
  node: ChainNode3D;
  onClick: (node: ChainNode3D) => void;
  showLabels: boolean;
};

export function BlockchainNode({ node, onClick, showLabels }: BlockchainNodeProps) {
  const meshRef = useRef<THREE.Mesh>(null);
  const outerRef = useRef<THREE.Mesh>(null);
  const [hovered, setHovered] = useState(false);
  
  // Pulsing animation
  useFrame((state) => {
    if (meshRef.current) {
      const pulse = Math.sin(state.clock.elapsedTime * 2) * 0.05;
      meshRef.current.scale.setScalar(1 + pulse);
    }
    
    // Outer glow rotation
    if (outerRef.current) {
      outerRef.current.rotation.z += 0.005;
    }
  });
  
  const size = calculateNodeSize(node.tvl);
  const baseColor = node.health === "healthy" ? "#22d3ee" : 
                     node.health === "degraded" ? "#facc15" : 
                     "#ef4444";
  
  return (
    <group position={node.position}>
      {/* Main sphere */}
      <Sphere
        ref={meshRef}
        args={[size, 32, 32]}
        onClick={() => onClick(node)}
        onPointerOver={() => setHovered(true)}
        onPointerOut={() => setHovered(false)}
      >
        <meshStandardMaterial
          color={baseColor}
          emissive={baseColor}
          emissiveIntensity={hovered ? 1.2 : 0.8}
          metalness={0.3}
          roughness={0.4}
          transparent
          opacity={0.9}
        />
      </Sphere>
      
      {/* Outer glow ring */}
      <mesh ref={outerRef}>
        <ringGeometry args={[size * 1.3, size * 1.5, 32]} />
        <meshBasicMaterial
          color={baseColor}
          transparent
          opacity={hovered ? 0.4 : 0.2}
          side={THREE.DoubleSide}
        />
      </mesh>
      
      {/* Outer glow sphere (for depth) */}
      <Sphere args={[size * 1.2, 32, 32]}>
        <meshBasicMaterial
          color={baseColor}
          transparent
          opacity={0.15}
          side={THREE.BackSide}
        />
      </Sphere>
      
      {/* Labels */}
      {showLabels && (
        <>
          {/* Chain name */}
          <Text
            position={[0, size + 1.2, 0]}
            fontSize={0.7}
            color="#e2f4ff"
            anchorX="center"
            anchorY="middle"
            outlineWidth={0.02}
            outlineColor="#050510"
          >
            {node.name}
          </Text>
          
          {/* TVL */}
          <Text
            position={[0, size + 2, 0]}
            fontSize={0.5}
            color="#22d3ee"
            anchorX="center"
            anchorY="middle"
            outlineWidth={0.02}
            outlineColor="#050510"
          >
            ${(node.tvl / 1_000_000_000).toFixed(1)}B
          </Text>
        </>
      )}
      
      {/* Hover highlight */}
      {hovered && (
        <Sphere args={[size * 1.8, 32, 32]}>
          <meshBasicMaterial
            color={baseColor}
            transparent
            opacity={0.1}
            side={THREE.BackSide}
          />
        </Sphere>
      )}
    </group>
  );
}





