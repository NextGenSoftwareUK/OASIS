"use client";

import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";

export default function Test3DPage() {
  console.log("🧪 Test 3D page loaded");
  
  return (
    <div className="w-full h-screen bg-black">
      <h1 className="absolute top-4 left-4 text-white z-10 text-2xl">
        3D Test Page - You should see a spinning red cube
      </h1>
      
      <Canvas camera={{ position: [0, 0, 5] }}>
        <ambientLight intensity={0.5} />
        <pointLight position={[10, 10, 10]} />
        
        {/* Simple red cube */}
        <mesh rotation={[0, 0, 0]}>
          <boxGeometry args={[2, 2, 2]} />
          <meshStandardMaterial color="red" />
        </mesh>
        
        <OrbitControls />
      </Canvas>
    </div>
  );
}

