import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { EffectComposer } from 'three/addons/postprocessing/EffectComposer.js';
import { RenderPass } from 'three/addons/postprocessing/RenderPass.js';
import { UnrealBloomPass } from 'three/addons/postprocessing/UnrealBloomPass.js';
import { CyberspaceEffects } from './lib/cyberspace.js';
import { GAMES } from './lib/games.js';
import { PortalRing } from './PortalRing.js';

/**
 * Space-themed 3D portal scene.
 * User drops in, orbits around, clicks portal O to enter DOOM or Quake.
 */
export class PortalScene {
    constructor(container) {
        this.container = container;
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x0a0a12);
        this.camera = null;
        this.renderer = null;
        this.controls = null;
        this.cyberspace = null;
        this.portals = [];
        this.raycaster = new THREE.Raycaster();
        this.mouse = new THREE.Vector2();
        this.clock = new THREE.Clock();
        this.hoveredPortal = null;
        this.onPortalHover = null;
        this.init();
    }

    init() {
        this.camera = new THREE.PerspectiveCamera(60, this.container.clientWidth / this.container.clientHeight, 0.1, 2000);
        this.camera.position.set(0, 40, 120);
        this.camera.lookAt(0, 10, -20);

        this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
        this.renderer.setSize(this.container.clientWidth, this.container.clientHeight);
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
        this.renderer.toneMappingExposure = 1.1;
        this.renderer.outputColorSpace = THREE.SRGBColorSpace;
        this.container.appendChild(this.renderer.domElement);

        this.composer = new EffectComposer(this.renderer);
        this.composer.addPass(new RenderPass(this.scene, this.camera));
        const bloomPass = new UnrealBloomPass(
            new THREE.Vector2(this.container.clientWidth, this.container.clientHeight),
            0.9, 0.4, 0.25
        );
        this.composer.addPass(bloomPass);
        this.bloomPass = bloomPass;

        this.controls = new OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.05;
        this.controls.minDistance = 30;
        this.controls.maxDistance = 400;

        this.cyberspace = new CyberspaceEffects(this.scene);

        // Lighting for metallic portal frames
        const ambient = new THREE.AmbientLight(0x4466aa, 0.3);
        this.scene.add(ambient);
        const pt1 = new THREE.PointLight(0x88aaff, 0.8, 300);
        pt1.position.set(50, 50, 50);
        this.scene.add(pt1);
        const pt2 = new THREE.PointLight(0xffaa66, 0.4, 200);
        pt2.position.set(-55, 30, -25);
        this.scene.add(pt2);
        const pt3 = new THREE.PointLight(0x88aaff, 0.4, 200);
        pt3.position.set(55, 30, -25);
        this.scene.add(pt3);

        // Portal positions - arc formation, spaced naturally
        const viewerPoint = new THREE.Vector3(0, 30, 80);
        const positions = [
            new THREE.Vector3(-55, 15, -25),
            new THREE.Vector3(55, 15, -25)
        ];
        GAMES.forEach((game, i) => {
            const pos = positions[i] || new THREE.Vector3(0, 0, (i + 1) * 100);
            const portal = new PortalRing(game, pos, viewerPoint);
            portal.addToScene(this.scene);
            this.portals.push(portal);
        });

        window.addEventListener('resize', () => this.onResize());
        this.container.addEventListener('click', (e) => this.onClick(e));
        this.container.addEventListener('mousemove', (e) => this.onMouseMove(e));
    }

    onResize() {
        const w = this.container.clientWidth;
        const h = this.container.clientHeight;
        this.camera.aspect = w / h;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(w, h);
        this.composer.setSize(w, h);
        this.composer.setPixelRatio(this.renderer.getPixelRatio());
        this.composer.reset();
        if (this.bloomPass) this.bloomPass.resolution.set(w, h);
    }

    onMouseMove(event) {
        const rect = this.container.getBoundingClientRect();
        this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
        this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
    }

    onClick(event) {
        const portal = this.portalAtMouse();
        if (portal) {
            portal.onClick();
        }
    }

    portalAtMouse() {
        this.raycaster.setFromCamera(this.mouse, this.camera);
        const allTargets = [];
        this.portals.forEach((p) => {
            p.getRaycastTargets().forEach((m) => allTargets.push(m));
        });
        const hits = this.raycaster.intersectObjects(allTargets);
        if (hits.length > 0) {
            const obj = hits[0].object;
            const userData = obj.userData;
            if (userData?.portalRing) return userData.portalRing;
        }
        return null;
    }

    updateHover() {
        const portal = this.portalAtMouse();
        if (portal !== this.hoveredPortal) {
            this.hoveredPortal = portal;
            if (this.onPortalHover) {
                this.onPortalHover(portal ? portal.game : null);
            }
        }
    }

    animate() {
        requestAnimationFrame(() => this.animate());
        const dt = this.clock.getDelta();
        const time = this.clock.getElapsedTime();
        this.cyberspace.update();
        this.portals.forEach((p) => p.update(time, this.camera));
        this.updateHover();
        this.controls.update();
        this.composer.render();
    }

    start() {
        this.animate();
    }
}
