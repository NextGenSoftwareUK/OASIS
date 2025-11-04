import { useRef, useMemo } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";

export function FlowParticles({ from, to, amount, isActive, color = "#22c55e" }: { 
  from: [number, number, number]; 
  to: [number, number, number];
  amount: number;
  isActive: boolean;
  color?: string;
}) {
  const particlesRef = useRef<THREE.Points>(null);
  const count = Math.max(8, Math.min(30, Math.floor(amount / 5)));
  
  const geometry = useMemo(() => {
    const positions = new Float32Array(count * 3);
    for (let i = 0; i < count; i++) {
      const t = i / count;
      positions[i * 3] = from[0] + (to[0] - from[0]) * t;
      positions[i * 3 + 1] = from[1] + (to[1] - from[1]) * t;
      positions[i * 3 + 2] = from[2] + (to[2] - from[2]) * t;
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
    return geo;
  }, [from, to, count]);
  
  useFrame((state) => {
    if (particlesRef.current && isActive) {
      const positions = particlesRef.current.geometry.attributes.position.array as Float32Array;
      for (let i = 0; i < count; i++) {
        const t = ((i / count) + state.clock.elapsedTime * 0.15) % 1;
        const arcHeight = Math.sin(t * Math.PI) * 1;
        positions[i * 3] = from[0] + (to[0] - from[0]) * t;
        positions[i * 3 + 1] = from[1] + (to[1] - from[1]) * t + arcHeight;
        positions[i * 3 + 2] = from[2] + (to[2] - from[2]) * t;
      }
      particlesRef.current.geometry.attributes.position.needsUpdate = true;
    }
  });
  
  if (!isActive) return null;
  
  return (
    <points ref={particlesRef} geometry={geometry}>
      <pointsMaterial
        size={0.5}
        color={color}
        sizeAttenuation={false}
        transparent
        opacity={1.0}
        blending={THREE.AdditiveBlending}
      />
    </points>
  );
}


