import { useRef, useState } from "react";
import { useFrame } from "@react-three/fiber";
import { Text } from "@react-three/drei";
import * as THREE from "three";
import type { DepartmentNode3D } from "../data/government-spending-data";

type DeptNodeProps = {
  node: DepartmentNode3D;
  onClick: (node: DepartmentNode3D) => void;
  showLabels: boolean;
};

export function DepartmentNode({ node, onClick, showLabels }: DeptNodeProps) {
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
  
  // Size based on annual budget
  const size = Math.max(0.8, Math.log10((node.annualBudget + 1) * 100) * 0.6 + 0.3);
  
  return (
    <group position={node.position}>
      {/* Main sphere */}
      <mesh
        ref={meshRef}
        onClick={() => onClick(node)}
        onPointerOver={() => setHovered(true)}
        onPointerOut={() => setHovered(false)}
      >
        <sphereGeometry args={[size, 32, 32]} />
        <meshStandardMaterial
          color={node.color}
          emissive={node.color}
          emissiveIntensity={hovered ? 1.2 : 0.8}
          metalness={0.3}
          roughness={0.4}
          transparent
          opacity={0.9}
        />
      </mesh>
      
      {/* Outer glow ring */}
      <mesh ref={outerRef}>
        <ringGeometry args={[size * 1.3, size * 1.5, 32]} />
        <meshBasicMaterial
          color={node.color}
          transparent
          opacity={hovered ? 0.4 : 0.2}
          side={THREE.DoubleSide}
        />
      </mesh>
      
      {/* Outer glow sphere (for depth) */}
      <mesh>
        <sphereGeometry args={[size * 1.2, 32, 32]} />
        <meshBasicMaterial
          color={node.color}
          transparent
          opacity={0.15}
          side={THREE.BackSide}
        />
      </mesh>
      
      {/* Labels */}
      {showLabels && (
        <>
          {/* Department name */}
          <Text
            position={[0, size + 1.2, 0]}
            fontSize={0.6}
            color="#e2f4ff"
            anchorX="center"
            anchorY="middle"
            outlineWidth={0.02}
            outlineColor="#050510"
          >
            {node.shortName}
          </Text>
          
          {/* Budget */}
          <Text
            position={[0, size + 1.8, 0]}
            fontSize={0.45}
            color={node.wasteScore > 75 ? "#ef4444" : node.wasteScore > 50 ? "#facc15" : "#22c55e"}
            anchorX="center"
            anchorY="middle"
            outlineWidth={0.02}
            outlineColor="#050510"
          >
            £{node.annualBudget.toFixed(1)}bn
          </Text>
        </>
      )}
      
      {/* Hover highlight */}
      {hovered && (
        <mesh>
          <sphereGeometry args={[size * 1.8, 32, 32]} />
          <meshBasicMaterial
            color={node.color}
            transparent
            opacity={0.1}
            side={THREE.BackSide}
          />
        </mesh>
      )}
    </group>
  );
}

