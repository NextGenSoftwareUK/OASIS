import * as THREE from 'three';

export class CelestialBody extends THREE.Group {
    constructor(id, name, type, holons, metadata) {
        super();
        
        // Store in userData to avoid conflict with THREE.Group's read-only 'id' property
        this.userData.celestialId = id;
        this.userData.name = name;
        this.userData.type = type; // 'star', 'planet', 'moon'
        this.userData.holons = holons;
        this.userData.metadata = metadata;
        
        // Also store as regular properties for easier access
        this.celestialId = id;
        this.name = name;
        this.type = type;
        this.holons = holons;
        this.metadata = metadata;
        this.rotationSpeed = 0.001 + Math.random() * 0.002;
        this.orbitRadius = this.getOrbitRadius();
        this.orbitAngle = Math.random() * Math.PI * 2;
        this.orbitSpeed = 0.0001 + Math.random() * 0.0002;
        
        this.createBody();
        this.createHolonParticles();
        // No orbit lines - only holons visible
        this.createLabel();
    }

    getOrbitRadius() {
        // Scale orbit radius based on holon count (not just type)
        // More holons = larger orbit radius
        const holonCount = this.userData.holons ? this.userData.holons.length : this.holons.length;
        let baseRadius;
        
        if (this.type === 'star') {
            baseRadius = 40 + (holonCount / 100); // Scale with holon count
        } else if (this.type === 'planet') {
            baseRadius = 25 + (holonCount / 200);
        } else {
            baseRadius = 15 + (holonCount / 300);
        }
        
        // Cap the radius
        return Math.min(baseRadius, 100);
    }

    createBody() {
        // Create main celestial body based on type and holon count
        // Size scales with holon count to represent the app's scale
        const baseSizes = {
            'star': 12,
            'planet': 7,
            'moon': 4
        };
        
        const colors = {
            'star': 0xffff00,
            'planet': 0x00ffff,
            'moon': 0x888888
        };

        // Scale size based on holon count
        const holonCount = this.userData.holons ? this.userData.holons.length : this.holons.length;
        let baseSize = baseSizes[this.type] || 5;
        
        // Add size based on holon count (logarithmic scale to avoid huge objects)
        const sizeMultiplier = 1 + Math.log10(holonCount / 100) * 0.3;
        const size = Math.max(baseSize, baseSize * sizeMultiplier);
        const color = colors[this.type] || 0xffffff;

        // No outer glow - only holons will be visible
        // Create core made of densely packed holons
        this.createHolonCore(size, color, holonCount);

        this.bodySize = size;
    }

    createHolonCore(radius, color, totalHolons) {
        // Create a core group for holons
        this.holonCore = new THREE.Group();
        
        // MASSIVELY increase holon counts - show even more for subtle, numerous effect
        let coreHolonCount;
        
        if (totalHolons < 500) {
            // Moon: Show 90-100% of holons (more numerous)
            coreHolonCount = Math.floor(totalHolons * 0.95);
        } else if (totalHolons < 2000) {
            // Planet: Show 85-95% of holons (more numerous)
            coreHolonCount = Math.floor(totalHolons * 0.9);
        } else {
            // Star: Show 70-80% of holons (more numerous, but still dispersed)
            coreHolonCount = Math.floor(totalHolons * 0.75);
        }
        
        // EVEN HIGHER minimums for more numerous particles
        const minCounts = {
            'moon': 5000,      // Increased from 2000
            'planet': 20000,   // Increased from 10000
            'star': 25000      // Increased from 15000
        };
        coreHolonCount = Math.max(coreHolonCount, minCounts[this.type] || 10000);
        
        // EVEN HIGHER maximums for more numerous particles
        const maxCounts = {
            'moon': 20000,     // Increased from 10000
            'planet': 100000,  // Increased from 50000
            'star': 120000     // Increased from 80000
        };
        coreHolonCount = Math.min(coreHolonCount, maxCounts[this.type] || 100000);
        
        const holonsArray = this.userData.holons || this.holons;
        const step = Math.max(1, Math.floor(holonsArray.length / coreHolonCount));
        
        // Store particles for big bang animation
        this.holonParticles = [];
        // Stars: larger radius for more dispersion, others: 95% for full sphere
        const coreRadius = this.type === 'star' ? radius * 1.2 : radius * 0.95;
        
        // Create holon particles with proper sphere distribution
        for (let i = 0; i < holonsArray.length && this.holonParticles.length < coreHolonCount; i += step) {
            const holon = holonsArray[i];
            
            // Use improved sphere distribution - multiple methods for full coverage
            const index = this.holonParticles.length;
            let x, y, z;
            
            // Mix of Fibonacci sphere and random sphere for better coverage
            if (index % 3 === 0) {
                // Fibonacci sphere distribution
                const goldenAngle = Math.PI * (3 - Math.sqrt(5));
                const theta = goldenAngle * index;
                y = 1 - (index / coreHolonCount) * 2;
                const radiusAtY = Math.sqrt(1 - y * y);
                x = Math.cos(theta) * radiusAtY;
                z = Math.sin(theta) * radiusAtY;
            } else if (index % 3 === 1) {
                // Uniform random on sphere surface
                const theta = Math.random() * Math.PI * 2;
                const phi = Math.acos(2 * Math.random() - 1);
                x = Math.sin(phi) * Math.cos(theta);
                y = Math.cos(phi);
                z = Math.sin(phi) * Math.sin(theta);
            } else {
                // Layered sphere approach
                const layer = Math.floor(index / (coreHolonCount / 10));
                const layerProgress = (index % (coreHolonCount / 10)) / (coreHolonCount / 10);
                const layerRadius = (layer / 10) * 0.9 + 0.1;
                const angle = layerProgress * Math.PI * 2 * (layer + 1);
                const elevation = (Math.random() - 0.5) * Math.PI;
                x = Math.cos(angle) * Math.cos(elevation) * layerRadius;
                y = Math.sin(elevation) * layerRadius;
                z = Math.sin(angle) * Math.cos(elevation) * layerRadius;
            }
            
            // Scale to core radius - stars more dispersed, others tighter
            let distance;
            if (this.type === 'star') {
                distance = 0.5 + Math.random() * 0.5; // 50-100% of radius for more dispersion
            } else {
                distance = 0.7 + Math.random() * 0.3; // 70-100% of radius
            }
            const position = new THREE.Vector3(
                x * coreRadius * distance,
                y * coreRadius * distance,
                z * coreRadius * distance
            );
            
            // Create holon particle - more subtle (smaller and more transparent)
            const particleSize = 0.08 + Math.random() * 0.05; // Smaller: 0.08-0.13 (was 0.12-0.20)
            const particleGeometry = new THREE.SphereGeometry(particleSize, 6, 6);
            const particleColor = this.getLightColor();
            const particleMaterial = new THREE.MeshBasicMaterial({
                color: particleColor,
                transparent: true,
                opacity: 0.5 + Math.random() * 0.3, // More subtle: 0.5-0.8 (was 0.8-1.0)
                blending: THREE.AdditiveBlending
            });
            
            const particle = new THREE.Mesh(particleGeometry, particleMaterial);
            
            // Store target position for star formation (coalescence)
            // Calculate scattered start position (nebula cloud state)
            const targetDistance = position.length();
            const startDistance = targetDistance * 2.5; // Start 2.5x further out
            const startDirection = position.clone().normalize();
            const scatterAmount = targetDistance * 0.8; // More scatter for nebula effect
            
            // Create scattered start position
            const startPos = startDirection.clone().multiplyScalar(startDistance);
            startPos.x += (Math.random() - 0.5) * scatterAmount;
            startPos.y += (Math.random() - 0.5) * scatterAmount;
            startPos.z += (Math.random() - 0.5) * scatterAmount;
            
            particle.userData = {
                holon: holon,
                targetPosition: position.clone(),
                startPosition: startPos.clone(),
                startDistance: startDistance
            };
            
            // Initialize particle at scattered position
            particle.position.copy(startPos);
            particle.scale.set(0.2, 0.2, 0.2); // Start very small (nebula particles)
            
            this.holonCore.add(particle);
            this.holonParticles.push(particle);
        }
        
        this.add(this.holonCore);
        this.bigBangActive = true;
        this.bigBangStartTime = Date.now();
    }

    createHolonParticles() {
        // Create particles representing holons orbiting the celestial body
        // For performance, we sample holons rather than rendering all of them
        // But we scale the sample size based on total holon count
        this.holonParticlesOrbit = new THREE.Group();
        
        const totalHolons = this.userData.holons ? this.userData.holons.length : this.holons.length;
        const holonsArray = this.userData.holons || this.holons;
        
        // Sample size scales with holon count but is capped for performance
        // Moon (200-800): sample 50-150 particles
        // Planet (800-3000): sample 150-300 particles  
        // Star (3000-15000): sample 300-500 particles
        let sampleSize;
        if (totalHolons < 500) {
            sampleSize = Math.min(totalHolons, 50 + Math.floor(totalHolons / 10));
        } else if (totalHolons < 2000) {
            sampleSize = Math.min(totalHolons, 150 + Math.floor(totalHolons / 20));
        } else {
            sampleSize = Math.min(totalHolons, 300 + Math.floor(totalHolons / 50));
        }
        
        // Cap at 500 particles for performance
        sampleSize = Math.min(sampleSize, 500);
        
        // Sample holons evenly across the array
        const step = Math.max(1, Math.floor(totalHolons / sampleSize));
        
        for (let i = 0; i < totalHolons; i += step) {
            if (this.holonParticlesOrbit && this.holonParticlesOrbit.children.length >= sampleSize) break;
            
            const holon = holonsArray[i];
            const particleIndex = this.holonParticlesOrbit.children.length;
            const angle = (particleIndex / sampleSize) * Math.PI * 2;
            const radius = this.orbitRadius * 0.6 + Math.random() * this.orbitRadius * 0.4;
            
            // Orbiting particles should be smaller than core particles
            // Core particles are 0.12-0.20, so orbiting should be 0.06-0.10 (about half size)
            const particleSize = 0.06 + Math.random() * 0.04;
            const particleGeometry = new THREE.SphereGeometry(particleSize, 6, 6);
            const particleMaterial = new THREE.MeshBasicMaterial({
                color: this.getLightColor(), // Use light colors for orbiting particles too
                transparent: true,
                opacity: 0.6, // More transparent since they're smaller
                blending: THREE.AdditiveBlending
            });
            
            const particle = new THREE.Mesh(particleGeometry, particleMaterial);
            
            const x = Math.cos(angle) * radius;
            const y = (Math.random() - 0.5) * this.orbitRadius * 0.5;
            const z = Math.sin(angle) * radius;
            
            particle.position.set(x, y, z);
            particle.userData = { 
                holon: holon, 
                angle: angle, 
                radius: radius,
                totalHolons: totalHolons,
                representsCount: step // How many holons this particle represents
            };
            
            this.holonParticlesOrbit.add(particle);
        }
        
        this.add(this.holonParticlesOrbit);
    }

    getHolonColor(holon) {
        // For orbiting particles, use light-based colors
        return this.getLightColor();
    }

    getLightColor() {
        // Generate light-based colors like moons and stars
        // Whites, pale yellows, pale blues, pale oranges
        const lightColors = [
            0xffffff,  // Pure white
            0xffffcc,  // Pale yellow
            0xffffaa,  // Light yellow
            0xffffee,  // Warm white
            0xccffff,  // Pale cyan
            0xaaffff,  // Light blue
            0xffddcc,  // Pale orange
            0xffeebb,  // Warm yellow
            0xeeeeff,  // Cool white
            0xffffdd,  // Cream
            0xccffcc,  // Pale green-white
            0xfff5e6,  // Warm white
        ];
        
        // Weight towards brighter colors for stars, cooler for moons
        if (this.type === 'star') {
            // Stars: Less white, more warm colors (yellows, oranges, golds)
            const starColors = [
                0xffffaa,  // Light yellow
                0xffeebb,  // Warm yellow
                0xffdd99,  // Golden yellow
                0xffcc88,  // Light orange
                0xffbb77,  // Warm orange
                0xffeeaa,  // Pale gold
                0xffdd88,  // Gold
                0xffff99,  // Bright yellow
                0xffcc99,  // Peach
                0xffaa66,  // Light amber
            ];
            return starColors[Math.floor(Math.random() * starColors.length)];
        } else if (this.type === 'planet') {
            // Planets: Mix of all light colors
            return lightColors[Math.floor(Math.random() * lightColors.length)];
        } else {
            // Moons: Cooler whites and pale blues
            const moonColors = [0xffffff, 0xeeeeff, 0xccffff, 0xaaffff, 0xffffee, 0xffffcc];
            return moonColors[Math.floor(Math.random() * moonColors.length)];
        }
    }

    createOrbit() {
        // No orbit lines - only holons are visible
        // Orbit line removed for cleaner holon-only visualization
        this.orbitLine = null;
    }

    createLabel() {
        // Create text label using canvas
        const canvas = document.createElement('canvas');
        const context = canvas.getContext('2d');
        canvas.width = 256;
        canvas.height = 80;

        context.fillStyle = 'rgba(0, 0, 0, 0.8)';
        context.fillRect(0, 0, canvas.width, canvas.height);

        context.fillStyle = '#00ffff';
        context.font = 'bold 20px Arial';
        context.textAlign = 'center';
        context.textBaseline = 'middle';
        const name = this.userData.name || this.name;
        const holonCount = (this.userData.holons ? this.userData.holons.length : this.holons.length);
        
        context.fillText(name, canvas.width / 2, canvas.height / 2 - 10);
        
        // Add holon count
        context.fillStyle = '#00ff88';
        context.font = '14px Arial';
        context.fillText(`${holonCount.toLocaleString()} holons`, canvas.width / 2, canvas.height / 2 + 15);

        const texture = new THREE.CanvasTexture(canvas);
        const spriteMaterial = new THREE.SpriteMaterial({
            map: texture,
            transparent: true,
            opacity: 0.9
        });
        
        this.label = new THREE.Sprite(spriteMaterial);
        this.label.scale.set(25, 6, 1);
        this.label.position.set(0, this.orbitRadius + 5, 0);
        this.add(this.label);
    }

    setOrbitVisible(visible) {
        if (this.orbitLine) {
            this.orbitLine.visible = visible;
        }
    }

    setLabelVisible(visible) {
        if (this.label) {
            this.label.visible = visible;
        }
    }

    update() {
        // Rotate the celestial body
        this.rotation.y += this.rotationSpeed;

        // Star formation animation - particles coalesce from scattered state
        if (this.bigBangActive && this.holonParticles && Array.isArray(this.holonParticles)) {
            const elapsed = (Date.now() - this.bigBangStartTime) / 1000; // seconds
            const duration = 4.0; // 4 second formation
            const progress = Math.min(elapsed / duration, 1);
            
            if (progress < 1) {
                // Star formation phase - particles coalesce under "gravity"
                this.holonParticles.forEach((particle, index) => {
                    if (!particle || !particle.userData) return;
                    
                    const userData = particle.userData;
                    const targetPos = userData.targetPosition;
                    const startPos = userData.startPosition;
                    
                    // Stagger the coalescence - outer particles start later
                    const distanceFromCenter = targetPos.length();
                    const maxDistance = Math.max(...this.holonParticles.map(p => 
                        p.userData?.targetPosition?.length() || 0
                    ));
                    const normalizedDistance = distanceFromCenter / maxDistance;
                    const stagger = normalizedDistance * 0.4; // Outer particles start later
                    
                    const particleProgress = Math.max(0, Math.min(1, (progress - stagger) / (1 - stagger)));
                    
                    // Gravity-like acceleration: slow start, fast middle, slow end (ease in-out cubic)
                    // This mimics gravitational acceleration as particles fall inward
                    let eased;
                    if (particleProgress < 0.5) {
                        // Slow start (distant particles begin to fall)
                        eased = 4 * particleProgress * particleProgress * particleProgress;
                    } else {
                        // Fast middle, slow end (particles accelerate then decelerate as they approach)
                        const t = 2 * particleProgress - 1;
                        eased = 0.5 * (1 + t * t * t);
                    }
                    
                    // Add orbital/spiral motion during coalescence (angular momentum conservation)
                    // Particles spiral inward as they fall toward the center
                    const spiralTurns = 2.5; // Number of spiral rotations
                    const angle = particleProgress * Math.PI * 2 * spiralTurns;
                    const spiralTightness = (1 - eased) * 0.4; // Spiral tightens as particles approach
                    const spiralRadius = spiralTightness * targetPos.length();
                    
                    // Calculate spiral offset perpendicular to radial direction
                    const radialDir = targetPos.clone().normalize();
                    const perpendicular1 = new THREE.Vector3(1, 0, 0).cross(radialDir).normalize();
                    if (perpendicular1.length() < 0.1) {
                        perpendicular1.set(0, 1, 0).cross(radialDir).normalize();
                    }
                    const perpendicular2 = radialDir.clone().cross(perpendicular1).normalize();
                    
                    const spiralOffset = perpendicular1
                        .clone()
                        .multiplyScalar(Math.cos(angle) * spiralRadius)
                        .add(perpendicular2.clone().multiplyScalar(Math.sin(angle) * spiralRadius));
                    
                    // Interpolate from scattered start to target with spiral motion
                    const currentPos = startPos.clone().lerp(targetPos, eased);
                    currentPos.add(spiralOffset);
                    particle.position.copy(currentPos);
                    
                    // Scale grows as particles coalesce (density increases)
                    const scale = 0.2 + eased * 0.8; // Start very small, grow to full size
                    particle.scale.set(scale, scale, scale);
                    
                    // Rotation increases as particles get closer (conservation of angular momentum)
                    const rotationSpeed = 0.003 * (1 + eased * 3);
                    particle.rotation.y += rotationSpeed;
                    particle.rotation.x += rotationSpeed * 0.5;
                });
            } else {
                // Formation complete - settle into final positions
                this.bigBangActive = false;
                if (this.holonParticles) {
                    this.holonParticles.forEach(particle => {
                        if (particle && particle.userData) {
                            // Ensure particles are at exact target positions
                            particle.position.copy(particle.userData.targetPosition);
                            particle.scale.set(1, 1, 1);
                        }
                    });
                }
            }
        } else if (this.holonCore) {
            // Normal rotation after big bang
            this.holonCore.rotation.y += this.rotationSpeed * 0.5;
            this.holonCore.rotation.x += this.rotationSpeed * 0.3;
            
            // Slight individual particle rotation
            if (this.holonParticles && Array.isArray(this.holonParticles)) {
                this.holonParticles.forEach(particle => {
                    if (particle) {
                        particle.rotation.y += 0.002;
                        particle.rotation.x += 0.001;
                    }
                });
            }
        }

        // Update orbiting holon particles (separate from core)
        if (this.holonParticlesOrbit) {
            this.holonParticlesOrbit.children.forEach((particle, i) => {
                const userData = particle.userData;
                userData.angle += this.orbitSpeed * (1 + i * 0.1);
                
                const radius = userData.radius;
                particle.position.x = Math.cos(userData.angle) * radius;
                particle.position.z = Math.sin(userData.angle) * radius;
                particle.position.y = Math.sin(userData.angle * 2) * this.orbitRadius * 0.2;
            });
        }
    }
}

