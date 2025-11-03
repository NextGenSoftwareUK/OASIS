import { useRef, useMemo } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";

type FlowParticlesProps = {
  from: [number, number, number];
  to: [number, number, number];
  amount: number;
  isActive: boolean;
  isWasteful?: boolean; // For government spending (red vs green)
};

export function FlowParticles({ from, to, amount, isActive, isWasteful = false }: FlowParticlesProps) {
  const particlesRef = useRef<THREE.Points>(null);
  
  // Number of particles based on amount
  const count = Math.max(8, Math.min(40, Math.floor(amount / 10)));
  
  const { positions, colors } = useMemo(() => {
    const positions = new Float32Array(count * 3);
    const colors = new Float32Array(count * 3);
    
    // Color: green for efficient, red for wasteful
    const [r, g, b] = isWasteful ? [0.94, 0.27, 0.27] : [0.13, 0.83, 0.33]; // Red or green
    
    for (let i = 0; i < count; i++) {
      const t = i / count;
      positions[i * 3] = from[0] + (to[0] - from[0]) * t;
      positions[i * 3 + 1] = from[1] + (to[1] - from[1]) * t;
      positions[i * 3 + 2] = from[2] + (to[2] - from[2]) * t;
      
      colors[i * 3] = r;
      colors[i * 3 + 1] = g;
      colors[i * 3 + 2] = b;
    }
    
    return { positions, colors };
  }, [from, to, count, isWasteful]);
  
  // Animate particles flowing from -> to
  useFrame((state) => {
    if (particlesRef.current && isActive) {
      const posAttr = particlesRef.current.geometry.attributes.position;
      const positions = posAttr.array as Float32Array;
      
      for (let i = 0; i < count; i++) {
        // Calculate progress (0 to 1, loops)
        let t = (i / count + state.clock.elapsedTime * 0.15) % 1;
        
        // Update position along path with slight arc
        const arcHeight = Math.sin(t * Math.PI) * 1.5;
        positions[i * 3] = from[0] + (to[0] - from[0]) * t;
        positions[i * 3 + 1] = from[1] + (to[1] - from[1]) * t + arcHeight;
        positions[i * 3 + 2] = from[2] + (to[2] - from[2]) * t;
      }
      
      posAttr.needsUpdate = true;
    }
  });
  
  if (!isActive) return null;
  
  const geometry = useMemo(() => {
    const geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
    geo.setAttribute('color', new THREE.Float32BufferAttribute(colors, 3));
    return geo;
  }, [positions, colors]);
  
  return (
    <points ref={particlesRef} geometry={geometry}>
      <pointsMaterial
        size={0.7}
        vertexColors
        sizeAttenuation={false}
        transparent
        opacity={1.0}
        blending={THREE.AdditiveBlending}
      />
    </points>
  );
}

