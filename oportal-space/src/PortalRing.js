import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { SVGLoader } from 'three/addons/loaders/SVGLoader.js';
import { launchGame } from './lib/games.js';

/**
 * Stargate-style portal: heavy metallic frame, amber chevron lights,
 * swirling blue event horizon in the center.
 */
export class PortalRing {
    constructor(game, position, viewerPoint = new THREE.Vector3(0, 30, 80)) {
        this.game = game;
        this.mesh = null;
        this.basePosition = position.clone();
        this.viewerPoint = viewerPoint.clone();
        this.group = new THREE.Group();
        this.group.position.copy(position);
        this.group.userData = { game, portalRing: this };
        this.logoGroup = null;
        this.radius = 25;
        this.time = 0;
        this.build();
    }

    build() {
        const radius = 25;
        const innerRadius = radius - 6;

        // 1. Heavy metallic outer frame (bronze-tinted, aged metal)
        const frameGeo = new THREE.RingGeometry(innerRadius, radius, 48);
        const frameMat = new THREE.MeshStandardMaterial({
            color: 0x3d2e1f,
            metalness: 0.95,
            roughness: 0.35,
            emissive: 0x0a0806,
            side: THREE.DoubleSide
        });
        const frame = new THREE.Mesh(frameGeo, frameMat);
        frame.rotation.x = -Math.PI / 2;
        frame.userData = this.group.userData;
        this.group.add(frame);

        // 2. Inner ring with amber/orange glowing chevron lights
        this.chevrons = [];
        const chevronCount = 9;
        for (let i = 0; i < chevronCount; i++) {
            const angle = (i / chevronCount) * Math.PI * 2;
            const chevronRadius = radius - 2;
            const x = Math.cos(angle) * chevronRadius;
            const z = Math.sin(angle) * chevronRadius;
            const chevronGeo = new THREE.CylinderGeometry(1.2, 1.5, 2, 6);
            const chevronMat = new THREE.MeshStandardMaterial({
                color: 0xffaa44,
                emissive: 0xff6600,
                emissiveIntensity: 2.2,
                transparent: true,
                opacity: 0.98
            });
            const chevron = new THREE.Mesh(chevronGeo, chevronMat);
            chevron.position.set(x, 0, z);
            chevron.rotation.x = Math.PI / 2;
            chevron.rotation.z = -angle;
            chevron.userData = this.group.userData;
            this.group.add(chevron);
            this.chevrons.push(chevron);
        }

        // 3. Swirling blue event horizon (center aperture)
        const eventHorizon = this.createEventHorizon(innerRadius - 1);
        eventHorizon.userData = this.group.userData;
        this.group.add(eventHorizon);
        this.eventHorizon = eventHorizon;

        // 4. Thin inner ring edge (brighter blue accent)
        const edgeGeo = new THREE.TorusGeometry(innerRadius - 0.5, 0.8, 8, 48);
        const edgeMat = new THREE.MeshBasicMaterial({
            color: 0x66aaff,
            transparent: true,
            opacity: 0.9,
            side: THREE.DoubleSide
        });
        const edge = new THREE.Mesh(edgeGeo, edgeMat);
        edge.rotation.x = Math.PI / 2;
        edge.userData = this.group.userData;
        this.group.add(edge);

        // 5. Floating energy particles
        const particleCount = 80;
        const pGeo = new THREE.BufferGeometry();
        const positions = new Float32Array(particleCount * 3);
        for (let i = 0; i < particleCount * 3; i += 3) {
            const theta = Math.random() * Math.PI * 2;
            const r = innerRadius * 0.9 * Math.sqrt(Math.random());
            positions[i] = Math.cos(theta) * r;
            positions[i + 1] = (Math.random() - 0.5) * 2;
            positions[i + 2] = Math.sin(theta) * r;
        }
        pGeo.setAttribute('position', new THREE.BufferAttribute(positions, 3));
        const pMat = new THREE.PointsMaterial({
            color: 0xaaddff,
            size: 1.4,
            transparent: true,
            opacity: 0.9,
            sizeAttenuation: true,
            blending: THREE.AdditiveBlending
        });
        const particles = new THREE.Points(pGeo, pMat);
        particles.userData = this.group.userData;
        this.group.add(particles);
        this.particles = particles;

        // Orient portal to face toward the viewer (not each other)
        const towardViewer = this.viewerPoint.clone().sub(this.basePosition).normalize();
        this.group.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), towardViewer);

        // 6. Game logo - fixed straight above top of portal
        if (this.game.logoUrl) {
            this.addLogo(this.game.logoUrl);
        }
        if (this.game.svgLogoUrl) {
            this.addSvgLogo(this.game.svgLogoUrl);
        } else if (this.game.modelUrl) {
            this.addModelLogo(this.game.modelUrl);
        }
    }

    addLogo(url) {
        const loader = new THREE.TextureLoader();
        const self = this;
        loader.load(url, (texture) => {
            const logoHeight = 12;
            const aspect = texture.image ? texture.image.width / texture.image.height : 1;
            const logoWidth = logoHeight * aspect;
            const padding = 2;

            self.logoGroup = new THREE.Group();
            self.logoGroup.userData = self.group.userData;

            // Backplate for structure - dark metallic frame
            const backplateGeo = new THREE.PlaneGeometry(logoWidth + padding * 2, logoHeight + padding * 2);
            const backplateMat = new THREE.MeshBasicMaterial({
                color: 0x1a1a1a,
                side: THREE.DoubleSide
            });
            const backplate = new THREE.Mesh(backplateGeo, backplateMat);
            backplate.position.z = -0.5;
            self.logoGroup.add(backplate);

            // Logo plane
            const planeGeo = new THREE.PlaneGeometry(logoWidth, logoHeight);
            const planeMat = new THREE.MeshBasicMaterial({
                map: texture,
                transparent: true,
                opacity: 0.98,
                side: THREE.DoubleSide
            });
            const plane = new THREE.Mesh(planeGeo, planeMat);
            plane.userData = self.group.userData;
            self.logoGroup.add(plane);

            self.logoGroup.position.copy(self.basePosition);
            self.logoGroup.position.y += self.radius + logoHeight / 2 + padding + 4;

            if (self._scene) {
                self._scene.add(self.logoGroup);
            }
        });
    }

    addSvgLogo(url) {
        const loader = new SVGLoader();
        const self = this;
        loader.load(url, (data) => {
            if (!data.paths.length) {
                console.warn('Quake SVG: no paths found');
                return;
            }
            let shapeCount = 0;
            const svgGroup = new THREE.Group();
            svgGroup.scale.y = -1;
            const material = new THREE.MeshStandardMaterial({
                color: 0x4a90d9,
                metalness: 0.6,
                roughness: 0.4,
                emissive: 0x4488ff,
                emissiveIntensity: 0.4,
                side: THREE.DoubleSide
            });
            data.paths.forEach((path) => {
                const shapes = SVGLoader.createShapes(path);
                shapes.forEach((shape) => {
                    const geometry = new THREE.ExtrudeGeometry(shape, {
                        depth: 2,
                        bevelEnabled: true,
                        bevelThickness: 0.2,
                        bevelSize: 0.2,
                        bevelSegments: 2
                    });
                    geometry.center();
                    const mesh = new THREE.Mesh(geometry, material);
                    mesh.userData = self.group.userData;
                    svgGroup.add(mesh);
                });
            });
            if (shapeCount === 0) {
                console.warn('Quake SVG: no shapes created from paths');
                return;
            }
            const box = new THREE.Box3().setFromObject(svgGroup);
            const size = box.getSize(new THREE.Vector3());
            const maxDim = Math.max(size.x, size.y, size.z) || 1;
            const scale = 12 / maxDim;
            svgGroup.scale.multiplyScalar(scale);

            self.logoGroup = new THREE.Group();
            self.logoGroup.userData = self.group.userData;
            self.logoGroup.add(svgGroup);

            const logoHeight = 10;
            const padding = 2;
            self.logoGroup.position.copy(self.basePosition);
            self.logoGroup.position.y += self.radius + logoHeight / 2 + padding + 4;

            if (self._scene) {
                self._scene.add(self.logoGroup);
            }
        }, undefined, (err) => {
            console.warn('Quake SVG logo failed to load:', err);
        });
    }

    addModelLogo(url) {
        const loader = new GLTFLoader();
        const self = this;
        loader.load(url, (gltf) => {
            const model = gltf.scene;
            model.userData = self.group.userData;
            model.traverse((child) => {
                if (child.isMesh) child.userData = self.group.userData;
            });

            const box = new THREE.Box3().setFromObject(model);
            const size = box.getSize(new THREE.Vector3());
            const maxDim = Math.max(size.x, size.y, size.z);
            const scale = 10 / maxDim;
            model.scale.setScalar(scale);

            self.logoGroup = new THREE.Group();
            self.logoGroup.userData = self.group.userData;
            self.logoGroup.add(model);

            const logoHeight = 10;
            const padding = 2;
            self.logoGroup.position.copy(self.basePosition);
            self.logoGroup.position.y += self.radius + logoHeight / 2 + padding + 4;

            if (self._scene) {
                self._scene.add(self.logoGroup);
            }
        }, undefined, (err) => {
            console.warn('Quake logo model failed to load:', err);
        });
    }

    createEventHorizon(radius) {
        const segments = 64;
        const geometry = new THREE.CircleGeometry(radius, segments);

        const material = new THREE.ShaderMaterial({
            uniforms: {
                uTime: { value: 0 },
                uColor1: { value: new THREE.Color(0x1a4a8a) },
                uColor2: { value: new THREE.Color(0x44aaff) }
            },
            vertexShader: `
                varying vec2 vUv;
                void main() {
                    vUv = uv;
                    gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
                }
            `,
            fragmentShader: `
                uniform float uTime;
                uniform vec3 uColor1;
                uniform vec3 uColor2;
                varying vec2 vUv;
                void main() {
                    vec2 center = vUv - 0.5;
                    float dist = length(center);
                    float angle = atan(center.y, center.x);
                    float swirl = angle + uTime * 2.0 + dist * 4.0;
                    float pattern = sin(swirl * 3.0) * 0.5 + 0.5;
                    vec3 color = mix(uColor1, uColor2, pattern);
                    float alpha = 0.85 * (1.0 - smoothstep(0.4, 0.5, dist));
                    gl_FragColor = vec4(color, alpha);
                }
            `,
            transparent: true,
            side: THREE.DoubleSide,
            depthWrite: false,
            blending: THREE.AdditiveBlending
        });

        const mesh = new THREE.Mesh(geometry, material);
        mesh.rotation.x = -Math.PI / 2;
        return mesh;
    }

    update(time, camera) {
        this.time = time;
        const float = Math.sin(time * 0.5) * 3;
        this.group.position.x = this.basePosition.x;
        this.group.position.y = this.basePosition.y + float;
        this.group.position.z = this.basePosition.z;

        if (this.eventHorizon?.material?.uniforms?.uTime) {
            this.eventHorizon.material.uniforms.uTime.value = time;
        }
        if (this.particles) {
            this.particles.rotation.y += 0.012;
        }
        if (this.chevrons) {
            const pulse = 0.4 * Math.sin(time * 1.8) + 2.2;
            this.chevrons.forEach((c) => {
                if (c.material.emissiveIntensity !== undefined) {
                    c.material.emissiveIntensity = pulse;
                }
            });
        }

        // Logo: fixed straight above portal, always faces camera (billboard)
        if (this.logoGroup && camera) {
            this.logoGroup.position.copy(this.basePosition);
            this.logoGroup.position.y += this.radius + 8 + 6;
            this.logoGroup.lookAt(camera.position);
        }
    }

    addToScene(scene) {
        this._scene = scene;
        scene.add(this.group);
        if (this.logoGroup) {
            scene.add(this.logoGroup);
        }
    }

    onClick() {
        launchGame(this.game);
    }

    getRaycastTargets() {
        const targets = [];
        this.group.traverse((obj) => {
            if (obj.isMesh) targets.push(obj);
        });
        if (this.logoGroup) {
            this.logoGroup.traverse((obj) => {
                if (obj.isMesh) targets.push(obj);
            });
        }
        return targets;
    }
}
