import { PortalScene } from './PortalScene.js';
import { login, logout, getStoredAuth, fetchAvatarDetail } from './lib/auth.js';

const container = document.getElementById('canvas-container');
const loginPanel = document.getElementById('login-panel');
const hud = document.getElementById('hud');
const loginForm = document.getElementById('login-form');
const loginError = document.getElementById('login-error');
const usernameInput = document.getElementById('username');
const passwordInput = document.getElementById('password');
const avatarName = document.getElementById('avatar-name');
const avatarMeta = document.getElementById('avatar-meta');
const avatarPortrait = document.getElementById('avatar-portrait');
const portalLabel = document.getElementById('portal-label');
const logoutBtn = document.getElementById('logout-btn');
const demoBtn = document.getElementById('demo-btn');

let portalScene = null;

function showLogin() {
    if (loginPanel) loginPanel.classList.remove('hidden');
    if (hud) hud.classList.add('hidden');
}

function hideLogin() {
    if (loginPanel) loginPanel.classList.add('hidden');
    if (hud) hud.classList.remove('hidden');
}

function setLoginError(msg) {
    if (loginError) {
        loginError.textContent = msg || '';
        loginError.style.display = msg ? 'block' : 'none';
    }
}

function updateHud(profile) {
    if (!profile) return;
    if (avatarName) avatarName.textContent = profile.displayName || profile.username || '—';
    if (avatarMeta) avatarMeta.textContent = `LVL ${profile.level ?? '?'}  KARMA ${profile.karma ?? '?'}`;
    if (avatarPortrait && profile.portraitUrl) {
        avatarPortrait.src = profile.portraitUrl;
        avatarPortrait.style.display = 'block';
    } else if (avatarPortrait) {
        avatarPortrait.style.display = 'none';
    }
}

function handleLoginSuccess() {
    hideLogin();
    fetchAvatarDetail().then(updateHud).catch(() => {});
    startPortal();
}

function startPortal() {
    if (portalScene) return;
    portalScene = new PortalScene(container);
    portalScene.onPortalHover = (game) => {
        if (portalLabel) {
            portalLabel.textContent = game ? `Click to enter ${game.name}` : '';
            portalLabel.classList.toggle('visible', !!game);
        }
    };
    portalScene.start();
}

loginForm?.addEventListener('submit', async (e) => {
    e.preventDefault();
    setLoginError('');
    const username = usernameInput?.value?.trim();
    const password = passwordInput?.value;
    if (!username || !password) {
        setLoginError('Enter username and password');
        return;
    }
    try {
        await login(username, password);
        handleLoginSuccess();
    } catch (err) {
        setLoginError(err.message || 'Login failed');
    }
});

logoutBtn?.addEventListener('click', () => {
    logout();
    showLogin();
    setLoginError('');
});

demoBtn?.addEventListener('click', () => {
    handleLoginSuccess();
});

// Check for existing auth
const auth = getStoredAuth();
if (auth?.token && auth?.avatarId) {
    handleLoginSuccess();
} else {
    showLogin();
}
