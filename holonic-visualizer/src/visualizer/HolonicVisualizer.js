import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { HolonParticleSystem } from './HolonParticleSystem.js';
import { CelestialBody } from './CelestialBody.js';
import { CyberspaceEffects } from './CyberspaceEffects.js';

export class HolonicVisualizer {
    constructor(container) {
        this.container = container;
        this.scene = null;
        this.camera = null;
        this.renderer = null;
        this.controls = null;
        this.particleSystem = null;
        this.celestialBodies = new Map();
        this.holons = new Map();
        this.readyCallbacks = [];
        this.showOrbits = true;
        this.showLabels = true;
        this.animationId = null;
    }

    init() {
        // Create scene
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x000011);
        this.scene.fog = new THREE.FogExp2(0x000011, 0.001);

        // Create camera
        this.camera = new THREE.PerspectiveCamera(
            75,
            this.container.clientWidth / this.container.clientHeight,
            0.1,
            10000
        );
        this.camera.position.set(0, 50, 200);
        this.camera.lookAt(0, 0, 0);

        // Create renderer
        this.renderer = new THREE.WebGLRenderer({ antialias: true });
        this.renderer.setSize(this.container.clientWidth, this.container.clientHeight);
        this.renderer.setPixelRatio(window.devicePixelRatio);
        this.container.appendChild(this.renderer.domElement);

        // Create controls
        this.controls = new OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.05;
        this.controls.minDistance = 10;
        this.controls.maxDistance = 1000;
        this.controls.autoRotate = false;

        // Initialize particle system
        this.particleSystem = new HolonParticleSystem(this.scene);

        // Add cyberspace effects
        this.cyberspaceEffects = new CyberspaceEffects(this.scene);

        // Add ambient light
        const ambientLight = new THREE.AmbientLight(0x404040, 0.5);
        this.scene.add(ambientLight);

        // Add directional lights
        const light1 = new THREE.DirectionalLight(0x00ffff, 0.8);
        light1.position.set(100, 100, 100);
        this.scene.add(light1);

        const light2 = new THREE.DirectionalLight(0xff00ff, 0.5);
        light2.position.set(-100, 100, -100);
        this.scene.add(light2);

        // Handle window resize
        window.addEventListener('resize', () => this.onWindowResize());

        // Start animation loop
        this.animate();

        // Notify ready
        setTimeout(() => {
            this.readyCallbacks.forEach(cb => cb());
        }, 100);
    }

    onReady(callback) {
        this.readyCallbacks.push(callback);
    }

    loadData(data) {
        try {
            console.log('📊 Loading data into visualizer...', data);
            
            if (!data || !data.oapps || !data.holons) {
                throw new Error('Invalid data structure. Expected { oapps: [], holons: [] }');
            }

            this.clear();

            // Group holons by OAPP
            const holonsByOAPP = new Map();
            
            data.holons.forEach(holon => {
                const oappId = holon.oappId || 'unassigned';
                if (!holonsByOAPP.has(oappId)) {
                    holonsByOAPP.set(oappId, []);
                }
                holonsByOAPP.get(oappId).push(holon);
            });

            console.log(`📦 Grouped ${data.holons.length} holons into ${holonsByOAPP.size} groups`);

            // Create celestial bodies for each OAPP
            data.oapps.forEach((oapp, index) => {
                const holons = holonsByOAPP.get(oapp.id) || [];
                console.log(`🌍 Creating celestial body ${index + 1}/${data.oapps.length}: ${oapp.name} with ${holons.length} holons`);
                this.createCelestialBody(oapp, holons);
            });

            // Create unassigned holons as free particles
            const unassignedHolons = holonsByOAPP.get('unassigned') || [];
            if (unassignedHolons.length > 0) {
                console.log(`✨ Creating ${unassignedHolons.length} free-floating holons`);
                unassignedHolons.forEach(holon => {
                    this.createFreeHolon(holon);
                });
            }

            console.log(`✅ Data loaded successfully: ${this.celestialBodies.size} celestial bodies created`);
        } catch (error) {
            console.error('❌ Error loading data:', error);
            throw error;
        }
    }

    createCelestialBody(oapp, holons) {
        try {
            // Determine celestial type based on holon count
            // Holons are basic data points, so apps need many of them
            let celestialType = 'moon';
            if (holons.length >= 3000) {
                celestialType = 'star'; // Major app: 3000+ holons
            } else if (holons.length >= 800) {
                celestialType = 'planet'; // Medium app: 800-2999 holons
            } else {
                celestialType = 'moon'; // Basic app: 200-799 holons
            }

            // Override if specified in OAPP data
            if (oapp.celestialType) {
                celestialType = oapp.celestialType;
            }

            console.log(`   Creating ${celestialType}: ${oapp.name} (${holons.length} holons)`);

            // Create celestial body
            const celestialBody = new CelestialBody(
                oapp.id,
                oapp.name,
                celestialType,
                holons,
                oapp.metadata || {}
            );

            // Position close to camera view - in front of camera
            const cameraDirection = new THREE.Vector3();
            this.camera.getWorldDirection(cameraDirection);
            
            // Position in front of camera, slightly offset
            const distanceFromCamera = 40 + Math.random() * 20; // 40-60 units from camera
            const offsetX = (Math.random() - 0.5) * 30; // Random horizontal offset
            const offsetY = (Math.random() - 0.5) * 20 + 10; // Random vertical offset, slightly up
            
            // Calculate position relative to camera
            const cameraPosition = this.camera.position.clone();
            const forward = cameraDirection.clone().multiplyScalar(distanceFromCamera);
            const right = new THREE.Vector3(1, 0, 0).cross(cameraDirection).normalize();
            const up = cameraDirection.clone().cross(right).normalize();
            
            const position = cameraPosition
                .clone()
                .add(forward)
                .add(right.multiplyScalar(offsetX))
                .add(up.multiplyScalar(offsetY));
            
            celestialBody.position.copy(position);

            this.scene.add(celestialBody);
            this.celestialBodies.set(oapp.id, celestialBody);
            
            // SPECTACULAR entrance animation
            this.createSpectacularEntrance(celestialBody, celestialType);

            // Store holons
            holons.forEach(holon => {
                this.holons.set(holon.id, holon);
            });

            console.log(`   ✅ ${celestialType} created at position:`, celestialBody.position);
        } catch (error) {
            console.error(`❌ Error creating celestial body ${oapp.name}:`, error);
            throw error;
        }
    }

    createSpectacularEntrance(celestialBody, type) {
        // No spheres or lines - only holons animate
        // The big bang animation is handled in CelestialBody.update()
        // Just ensure the body starts at proper scale
        celestialBody.scale.set(1, 1, 1);
        celestialBody.visible = true;
    }

    createFreeHolon(holon) {
        // Create a free-floating holon particle
        const position = new THREE.Vector3(
            (Math.random() - 0.5) * 200,
            (Math.random() - 0.5) * 200,
            (Math.random() - 0.5) * 200
        );

        this.particleSystem.addHolon(holon, position);
        this.holons.set(holon.id, holon);
    }

    clear() {
        // Remove all celestial bodies
        this.celestialBodies.forEach(body => {
            this.scene.remove(body);
        });
        this.celestialBodies.clear();

        // Clear particle system
        this.particleSystem.clear();

        // Clear holons
        this.holons.clear();
    }

    resetCamera() {
        this.camera.position.set(0, 50, 200);
        this.controls.target.set(0, 0, 0);
        this.controls.update();
    }

    toggleOrbits() {
        this.showOrbits = !this.showOrbits;
        this.celestialBodies.forEach(body => {
            body.setOrbitVisible(this.showOrbits);
        });
    }

    toggleLabels() {
        this.showLabels = !this.showLabels;
        this.celestialBodies.forEach(body => {
            body.setLabelVisible(this.showLabels);
        });
    }

    onWindowResize() {
        this.camera.aspect = this.container.clientWidth / this.container.clientHeight;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(this.container.clientWidth, this.container.clientHeight);
    }

    animate() {
        this.animationId = requestAnimationFrame(() => this.animate());

        // Update controls
        this.controls.update();

        // Update celestial bodies
        this.celestialBodies.forEach(body => {
            body.update();
        });

        // Update particle system
        this.particleSystem.update();

        // Update cyberspace effects
        this.cyberspaceEffects.update();

        // Render
        this.renderer.render(this.scene, this.camera);
    }

    destroy() {
        if (this.animationId) {
            cancelAnimationFrame(this.animationId);
        }
        window.removeEventListener('resize', this.onWindowResize);
        this.clear();
    }
}

