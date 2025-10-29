"use client";

import { useRef } from "react";
import { Canvas, useFrame } from "@react-three/fiber";

function RotatingCube() {
  const meshRef = useRef<any>(null);
  
  // Rotate cube every frame
  useFrame((state, delta) => {
    if (meshRef.current) {
      meshRef.current.rotation.x += delta;
      meshRef.current.rotation.y += delta * 0.5;
      console.log("🔄 Rotating cube");
    }
  });
  
  return (
    <mesh ref={meshRef}>
      <boxGeometry args={[2, 2, 2]} />
      <meshStandardMaterial color="red" />
    </mesh>
  );
}

export default function Test3DPage() {
  console.log("🧪 Test 3D page loaded - should see SPINNING red cube");
  
  return (
    <div className="w-full h-screen bg-black">
      <h1 className="absolute top-4 left-4 text-white z-10 text-2xl font-bold">
        3D Test - Spinning Red Cube
      </h1>
      <p className="absolute top-16 left-4 text-cyan-400 z-10">
        If cube is spinning: R3F works! ✅<br/>
        If cube is static: Animation broken ❌<br/>
        If no cube: Rendering broken ❌
      </p>
      
      <Canvas camera={{ position: [3, 2, 5], fov: 75 }}>
        <ambientLight intensity={0.5} />
        <pointLight position={[10, 10, 10]} intensity={1} />
        <pointLight position={[-10, -10, -10]} intensity={0.5} color="#22d3ee" />
        
        <RotatingCube />
      </Canvas>
      
      <div className="absolute bottom-4 left-4 text-white/70 z-10">
        Check browser console (F12) for rotation logs
      </div>
    </div>
  );
}


