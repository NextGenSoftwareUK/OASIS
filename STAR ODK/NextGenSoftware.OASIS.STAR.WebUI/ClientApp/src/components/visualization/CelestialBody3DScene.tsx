// @ts-nocheck - Three.js types have compatibility issues
import { Suspense } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls, Stars } from "@react-three/drei";
import { CelestialBodyNode } from "./CelestialBodyNode";
import type { CelestialBody3D } from "./CelestialBodyNode";

type CelestialBody3DSceneProps = {
  bodies: CelestialBody3D[];
  showLabels: boolean;
  autoRotate: boolean;
  onBodyClick: (body: CelestialBody3D) => void;
};

export function CelestialBody3DScene({ 
  bodies, 
  showLabels, 
  autoRotate, 
  onBodyClick 
}: CelestialBody3DSceneProps) {
  
  return (
    <Canvas
      camera={{ position: [0, 15, 35], fov: 75 }}
      style={{ background: "#050510", width: "100%", height: "600px" }}
    >
      {/* Lighting - Cosmic theme */}
      <ambientLight intensity={0.2} />
      <pointLight position={[0, 0, 0]} intensity={2} color="#ffeb3b" /> {/* Central star */}
      <pointLight position={[30, 20, 30]} intensity={0.5} color="#22d3ee" />
      <pointLight position={[-30, -20, -30]} intensity={0.3} color="#818cf8" />
      <pointLight position={[0, 30, -20]} intensity={0.4} color="#ffffff" />
      
      <Suspense fallback={null}>
        {/* Background starfield */}
        <Stars 
          radius={150} 
          depth={80} 
          count={8000} 
          factor={5} 
          saturation={0} 
          fade 
          speed={0.3}
        />
        
        {/* Central star glow */}
        <mesh position={[0, 0, 0]}>
          <sphereGeometry args={[2, 32, 32]} />
          <meshBasicMaterial
            color="#ffeb3b"
            transparent
            opacity={0.3}
          />
        </mesh>
        
        {/* Celestial bodies */}
        {bodies.map((body) => (
          <CelestialBodyNode
            key={body.id}
            body={body}
            onClick={onBodyClick}
            showLabels={showLabels}
          />
        ))}
        
        {/* Orbital paths */}
        {bodies.map((body) => {
          const distance = Math.sqrt(
            body.position[0] ** 2 + 
            body.position[1] ** 2 + 
            body.position[2] ** 2
          );
          
          if (distance < 1) return null; // Skip central bodies
          
          return (
            <mesh key={`orbit-${body.id}`} rotation={[Math.PI / 2, 0, 0]}>
              <ringGeometry args={[distance - 0.1, distance + 0.1, 128]} />
              <meshBasicMaterial
                color="#22d3ee"
                transparent
                opacity={0.1}
                side={2}
              />
            </mesh>
          );
        })}
        
        {/* Orbit controls */}
        <OrbitControls 
          enableZoom={true}
          enablePan={true}
          enableRotate={true}
          autoRotate={autoRotate}
          autoRotateSpeed={0.5}
          minDistance={10}
          maxDistance={100}
        />
      </Suspense>
    </Canvas>
  );
}

