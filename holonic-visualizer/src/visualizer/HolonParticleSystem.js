import * as THREE from 'three';

export class HolonParticleSystem {
    constructor(scene) {
        this.scene = scene;
        this.particles = [];
        this.geometry = new THREE.BufferGeometry();
        this.material = new THREE.PointsMaterial({
            size: 2,
            color: 0x00ffff,
            transparent: true,
            opacity: 0.8,
            blending: THREE.AdditiveBlending,
            sizeAttenuation: true
        });
        this.points = new THREE.Points(this.geometry, this.material);
        this.scene.add(this.points);
    }

    addHolon(holon, position) {
        const particle = {
            id: holon.id,
            holon: holon,
            position: position.clone(),
            velocity: new THREE.Vector3(
                (Math.random() - 0.5) * 0.1,
                (Math.random() - 0.5) * 0.1,
                (Math.random() - 0.5) * 0.1
            ),
            color: this.getHolonColor(holon),
            size: 1 + Math.random() * 2
        };

        this.particles.push(particle);
        this.updateGeometry();
    }

    getHolonColor(holon) {
        // Color based on holon type
        const typeColors = {
            'Mission': 0x00ff88,
            'NFT': 0xff00ff,
            'Inventory': 0xffff00,
            'CelestialBody': 0x00ffff,
            'Wallet': 0xff8800,
            'DeFi': 0x0088ff,
            'Identity': 0x88ff00,
            'default': 0x00ffff
        };

        return typeColors[holon.holonType] || typeColors['default'];
    }

    updateGeometry() {
        if (this.particles.length === 0) {
            this.geometry.setAttribute('position', new THREE.Float32BufferAttribute([], 3));
            this.geometry.setAttribute('color', new THREE.Float32BufferAttribute([], 3));
            this.geometry.setAttribute('size', new THREE.Float32BufferAttribute([], 1));
            return;
        }

        const positions = [];
        const colors = [];
        const sizes = [];

        this.particles.forEach(particle => {
            positions.push(particle.position.x, particle.position.y, particle.position.z);
            
            const color = new THREE.Color(particle.color);
            colors.push(color.r, color.g, color.b);
            
            sizes.push(particle.size);
        });

        this.geometry.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
        this.geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors, 3));
        this.geometry.setAttribute('size', new THREE.Float32BufferAttribute(sizes, 1));
        this.geometry.attributes.position.needsUpdate = true;
        this.geometry.attributes.color.needsUpdate = true;
        this.geometry.attributes.size.needsUpdate = true;

        this.material.vertexColors = true;
    }

    update() {
        // Update particle positions
        this.particles.forEach(particle => {
            particle.position.add(particle.velocity);
            
            // Add some drift
            particle.velocity.add(
                new THREE.Vector3(
                    (Math.random() - 0.5) * 0.01,
                    (Math.random() - 0.5) * 0.01,
                    (Math.random() - 0.5) * 0.01
                )
            );
            
            // Dampen velocity
            particle.velocity.multiplyScalar(0.98);
        });

        this.updateGeometry();
    }

    clear() {
        this.particles = [];
        this.updateGeometry();
    }
}

