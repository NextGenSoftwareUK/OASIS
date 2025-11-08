// @ts-nocheck - Three.js types have compatibility issues
import { useRef, useState } from "react";
import { useFrame } from "@react-three/fiber";
import { Text, Sphere } from "@react-three/drei";
import * as THREE from "three";

export interface CelestialBody3D {
  id: string;
  name: string;
  type: 'Planet' | 'Star' | 'Moon' | 'Asteroid' | 'Comet' | 'Nebula';
  position: [number, number, number];
  radius: number;
  mass: number;
  temperature: number;
  isInhabited: boolean;
  orbitalPeriod: number;
}

type CelestialBodyNodeProps = {
  body: CelestialBody3D;
  onClick: (body: CelestialBody3D) => void;
  showLabels: boolean;
};

export function CelestialBodyNode({ body, onClick, showLabels }: CelestialBodyNodeProps) {
  const meshRef = useRef<THREE.Mesh>(null);
  const outerRef = useRef<THREE.Mesh>(null);
  const atmosphereRef = useRef<THREE.Mesh>(null);
  const [hovered, setHovered] = useState(false);
  
  // Rotation and pulsing animation
  useFrame((state) => {
    if (meshRef.current) {
      // Rotate based on rotation period
      meshRef.current.rotation.y += 0.001 * (100 / (body.orbitalPeriod || 100));
      
      // Pulse for stars
      if (body.type === 'Star') {
        const pulse = Math.sin(state.clock.elapsedTime * 3) * 0.08;
        meshRef.current.scale.setScalar(1 + pulse);
      }
    }
    
    // Outer glow rotation (opposite direction)
    if (outerRef.current) {
      outerRef.current.rotation.z += 0.01;
      outerRef.current.rotation.x = Math.sin(state.clock.elapsedTime * 0.5) * 0.3;
    }
    
    // Atmosphere shimmer
    if (atmosphereRef.current && body.isInhabited) {
      const shimmer = Math.sin(state.clock.elapsedTime * 2) * 0.1;
      atmosphereRef.current.scale.setScalar(1 + shimmer);
    }
  });
  
  // Calculate visual size based on actual radius (logarithmic scale for display)
  const visualSize = Math.log(body.radius + 1) * 0.3 + 0.5;
  
  // Color based on type and temperature
  const getBodyColor = () => {
    switch (body.type) {
      case 'Star':
        return body.temperature > 4000 ? "#ffeb3b" : body.temperature > 3000 ? "#ff9800" : "#f44336";
      case 'Planet':
        return body.isInhabited ? "#22d3ee" : body.temperature > 273 ? "#4caf50" : "#2196f3";
      case 'Moon':
        return "#b0bec5";
      case 'Asteroid':
        return "#795548";
      case 'Comet':
        return "#e1f5fe";
      case 'Nebula':
        return "#9c27b0";
      default:
        return "#22d3ee";
    }
  };
  
  const bodyColor = getBodyColor();
  const emissiveIntensity = body.type === 'Star' ? 1.5 : hovered ? 0.8 : 0.3;
  
  return (
    <group position={body.position}>
      {/* Main celestial body */}
      <Sphere
        ref={meshRef}
        args={[visualSize, 64, 64]}
        onClick={() => onClick(body)}
        onPointerOver={() => setHovered(true)}
        onPointerOut={() => setHovered(false)}
      >
        <meshStandardMaterial
          color={bodyColor}
          emissive={bodyColor}
          emissiveIntensity={emissiveIntensity}
          metalness={body.type === 'Star' ? 0.8 : 0.3}
          roughness={body.type === 'Star' ? 0.2 : 0.6}
          transparent
          opacity={body.type === 'Nebula' ? 0.6 : 0.95}
        />
      </Sphere>
      
      {/* Atmosphere for inhabited planets */}
      {body.isInhabited && (
        <Sphere ref={atmosphereRef} args={[visualSize * 1.15, 32, 32]}>
          <meshBasicMaterial
            color="#22d3ee"
            transparent
            opacity={0.2}
            side={THREE.BackSide}
          />
        </Sphere>
      )}
      
      {/* Outer glow ring */}
      <mesh ref={outerRef}>
        <ringGeometry args={[visualSize * 1.4, visualSize * 1.6, 32]} />
        <meshBasicMaterial
          color={bodyColor}
          transparent
          opacity={hovered ? 0.5 : body.type === 'Star' ? 0.4 : 0.2}
          side={THREE.DoubleSide}
        />
      </mesh>
      
      {/* Corona for stars */}
      {body.type === 'Star' && (
        <Sphere args={[visualSize * 1.3, 32, 32]}>
          <meshBasicMaterial
            color={bodyColor}
            transparent
            opacity={0.2}
            side={THREE.BackSide}
          />
        </Sphere>
      )}
      
      {/* Labels */}
      {showLabels && (
        <>
          {/* Name */}
          <Text
            position={[0, visualSize + 1.5, 0]}
            fontSize={0.6}
            color="#e2f4ff"
            anchorX="center"
            anchorY="middle"
            outlineWidth={0.02}
            outlineColor="#050510"
          >
            {body.name}
          </Text>
          
          {/* Type */}
          <Text
            position={[0, visualSize + 2.2, 0]}
            fontSize={0.4}
            color="#22d3ee"
            anchorX="center"
            anchorY="middle"
            outlineWidth={0.02}
            outlineColor="#050510"
          >
            {body.type}
          </Text>
          
          {/* Population for inhabited bodies */}
          {body.isInhabited && (
            <Text
              position={[0, visualSize + 2.8, 0]}
              fontSize={0.35}
              color="#4caf50"
              anchorX="center"
              anchorY="middle"
              outlineWidth={0.02}
              outlineColor="#050510"
            >
              👥 Inhabited
            </Text>
          )}
        </>
      )}
      
      {/* Hover highlight */}
      {hovered && (
        <Sphere args={[visualSize * 2, 32, 32]}>
          <meshBasicMaterial
            color={bodyColor}
            transparent
            opacity={0.08}
            side={THREE.BackSide}
          />
        </Sphere>
      )}
    </group>
  );
}

