/**
 * Cyberspace effects - stars, grid, particles.
 * Reused from holonic-visualizer.
 */
import * as THREE from 'three';

export class CyberspaceEffects {
    constructor(scene) {
        this.scene = scene;
        this.time = 0;
        this.createGrid();
        this.createStars();
        this.createParticleField();
    }

    createGrid() {
        const gridHelper = new THREE.GridHelper(500, 50, 0x00ffff, 0x003333);
        gridHelper.material.opacity = 0.2;
        gridHelper.material.transparent = true;
        this.scene.add(gridHelper);
        this.grid = gridHelper;
    }

    createStars() {
        const starsGeometry = new THREE.BufferGeometry();
        const starsMaterial = new THREE.PointsMaterial({
            color: 0xffffff,
            size: 1,
            transparent: true,
            opacity: 0.8
        });
        const starsVertices = [];
        for (let i = 0; i < 10000; i++) {
            const x = (Math.random() - 0.5) * 2000;
            const y = (Math.random() - 0.5) * 2000;
            const z = (Math.random() - 0.5) * 2000;
            starsVertices.push(x, y, z);
        }
        starsGeometry.setAttribute('position', new THREE.Float32BufferAttribute(starsVertices, 3));
        const stars = new THREE.Points(starsGeometry, starsMaterial);
        this.scene.add(stars);
        this.stars = stars;
    }

    createParticleField() {
        const particleGeometry = new THREE.BufferGeometry();
        const particleCount = 1000;
        const positions = new Float32Array(particleCount * 3);
        const colors = new Float32Array(particleCount * 3);
        for (let i = 0; i < particleCount * 3; i += 3) {
            positions[i] = (Math.random() - 0.5) * 1000;
            positions[i + 1] = (Math.random() - 0.5) * 1000;
            positions[i + 2] = (Math.random() - 0.5) * 1000;
            const color = new THREE.Color();
            color.setHSL(0.5 + Math.random() * 0.2, 1, 0.5);
            colors[i] = color.r;
            colors[i + 1] = color.g;
            colors[i + 2] = color.b;
        }
        particleGeometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
        particleGeometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));
        const particleMaterial = new THREE.PointsMaterial({
            size: 0.5,
            vertexColors: true,
            transparent: true,
            opacity: 0.6,
            blending: THREE.AdditiveBlending
        });
        this.particleField = new THREE.Points(particleGeometry, particleMaterial);
        this.scene.add(this.particleField);
    }

    update() {
        this.time += 0.01;
        if (this.stars) this.stars.rotation.y += 0.0001;
        if (this.particleField) {
            const positions = this.particleField.geometry.attributes.position.array;
            for (let i = 1; i < positions.length; i += 3) {
                positions[i] += Math.sin(this.time + i) * 0.01;
            }
            this.particleField.geometry.attributes.position.needsUpdate = true;
        }
    }
}
