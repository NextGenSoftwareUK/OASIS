import { useRef, useState } from "react";
import { useFrame } from "@react-three/fiber";
import { Text } from "@react-three/drei";
import * as THREE from "three";
import type { NHSNode3D } from "@/lib/reform-uk-data/nhs-visualization-data";

export function NHSNode({ node, onClick, showLabels }: { 
  node: NHSNode3D; 
  onClick: (node: NHSNode3D) => void;
  showLabels: boolean;
}) {
  const meshRef = useRef<THREE.Mesh>(null);
  const [hovered, setHovered] = useState(false);
  
  useFrame((state) => {
    if (meshRef.current) {
      const pulse = Math.sin(state.clock.elapsedTime * 2) * 0.05;
      meshRef.current.scale.setScalar(1 + pulse);
    }
  });
  
  const size = Math.max(0.8, Math.log10(node.waitingList + 1) * 0.6 + 0.5);
  
  return (
    <group position={node.position}>
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
      
      {showLabels && (
        <Text
          position={[0, size + 1.5, 0]}
          fontSize={0.6}
          color="#e2f4ff"
          anchorX="center"
          anchorY="middle"
        >
          {node.name.split(' ')[0]}
        </Text>
      )}
      
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

