/**
 * Simple Hash-based Router
 */

class Router {
    constructor() {
        this.routes = {};
        this.currentRoute = null;
        this.init();
    }

    init() {
        // Listen for hash changes
        window.addEventListener('hashchange', () => this.handleRoute());
        
        // Handle initial route
        this.handleRoute();
    }

    /**
     * Register a route
     */
    route(path, handler) {
        this.routes[path] = handler;
    }

    /**
     * Handle current route
     */
    handleRoute() {
        const hash = window.location.hash.slice(1) || 'dashboard';
        const [path, ...params] = hash.split('/');
        
        // Hide all screens
        document.querySelectorAll('.screen').forEach(screen => {
            screen.classList.remove('active');
        });

        // Update nav links
        document.querySelectorAll('.nav-link').forEach(link => {
            link.classList.remove('active');
            if (link.getAttribute('href') === `#${path}`) {
                link.classList.add('active');
            }
        });

        // Show appropriate screen
        const screenId = `${path}Screen`;
        const screen = document.getElementById(screenId);
        if (screen) {
            screen.classList.add('active');
        } else if (path === 'confirm') {
            // Handle confirm screen
            const confirmScreen = document.getElementById('confirmScreen');
            if (confirmScreen) {
                confirmScreen.classList.add('active');
            }
        }

        // Call route handler
        if (this.routes[path]) {
            this.routes[path](params);
        } else if (this.routes['*']) {
            this.routes['*'](params);
        }

        this.currentRoute = path;
    }

    /**
     * Navigate to route
     */
    navigate(path) {
        window.location.hash = path;
    }
}

// Initialize router
const router = new Router();
