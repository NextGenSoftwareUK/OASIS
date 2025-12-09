// @ts-nocheck - Three.js types have compatibility issues
"use client";

import { useRef } from "react";
import { useFrame } from "@react-three/fiber";
import { Sphere, Text } from "@react-three/drei";
import * as THREE from "three";

export function OracleCore() {
  const coreRef = useRef<THREE.Mesh>(null);
  const ringsRef = useRef<THREE.Group>(null);
  
  // Pulsing animation for core
  useFrame((state) => {
    if (coreRef.current) {
      const pulse = Math.sin(state.clock.elapsedTime * 1.5) * 0.1;
      coreRef.current.scale.setScalar(1 + pulse);
    }
    
    // Rotate rings
    if (ringsRef.current) {
      ringsRef.current.rotation.y += 0.01;
      ringsRef.current.rotation.x += 0.005;
    }
  });
  
  return (
    <group position={[0, 0, 0]}>
      {/* Central core */}
      <Sphere ref={coreRef} args={[1.5, 64, 64]}>
        <meshStandardMaterial
          color="#22d3ee"
          emissive="#22d3ee"
          emissiveIntensity={1.5}
          metalness={0.8}
          roughness={0.2}
        />
      </Sphere>
      
      {/* Outer glow */}
      <Sphere args={[2, 64, 64]}>
        <meshBasicMaterial
          color="#22d3ee"
          transparent
          opacity={0.2}
          side={THREE.BackSide}
        />
      </Sphere>
      
      {/* Rotating rings */}
      <group ref={ringsRef}>
        <mesh rotation={[Math.PI / 2, 0, 0]}>
          <torusGeometry args={[3, 0.05, 16, 100]} />
          <meshBasicMaterial color="#22d3ee" transparent opacity={0.4} />
        </mesh>
        <mesh rotation={[0, 0, Math.PI / 3]}>
          <torusGeometry args={[3.2, 0.05, 16, 100]} />
          <meshBasicMaterial color="#38bdf8" transparent opacity={0.3} />
        </mesh>
      </group>
      
      {/* Label */}
      <Text
        position={[0, -3, 0]}
        fontSize={0.8}
        color="#22d3ee"
        anchorX="center"
        anchorY="middle"
        font="/fonts/geist-sans.woff"
        outlineWidth={0.03}
        outlineColor="#050510"
      >
        OASIS ORACLE
      </Text>
    </group>
  );
}






