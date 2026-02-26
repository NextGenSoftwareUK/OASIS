/**
 * OASIS auth for portal - login, token storage, avatar fetch.
 * Reuses same auth flow as games-dashboard / doom-browser.
 */
const STORAGE_KEY = 'oasis_portal_auth';

function getBaseUrl() {
    const host = typeof window !== 'undefined' ? window.location.hostname : '';
    if (host === 'localhost' || host === '127.0.0.1') return 'http://localhost:5003';
    const protocol = typeof window !== 'undefined' ? window.location.protocol : 'https:';
    return protocol === 'https:' ? 'https://api.oasisweb4.com' : 'http://api.oasisweb4.com';
}

const baseUrl = import.meta.env.VITE_OASIS_API_URL || getBaseUrl();

export async function login(username, password) {
    const res = await fetch(`${baseUrl}/api/avatar/authenticate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password })
    });
    const data = await res.json();
    const isError = data.isError === true || data.IsError === true;
    if (isError) {
        throw new Error(data.message || data.Message || 'Login failed');
    }
    const result = data.result?.result ?? data.result ?? data;
    const token = result.token || result.jwtToken || result.JwtToken;
    const avatarId = result.id || result.avatarId || result.AvatarId;
    if (typeof avatarId !== 'string') avatarId = String(avatarId);
    if (!token || !avatarId) throw new Error('No token or avatar ID in response');
    const auth = { token, avatarId, username };
    try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(auth));
    } catch (e) { /* ignore */ }
    return auth;
}

export function logout() {
    try {
        localStorage.removeItem(STORAGE_KEY);
    } catch (e) { /* ignore */ }
    return null;
}

export function getStoredAuth() {
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        if (!raw) return null;
        return JSON.parse(raw);
    } catch (e) {
        return null;
    }
}

export function getToken() {
    const auth = getStoredAuth();
    return auth?.token || null;
}

export function getAvatarId() {
    const auth = getStoredAuth();
    return auth?.avatarId || null;
}

export function getBaseURL() {
    return baseUrl;
}

export async function fetchAvatarDetail() {
    const auth = getStoredAuth();
    if (!auth?.token || !auth?.avatarId) return null;
    const res = await fetch(`${baseUrl}/api/avatar/get-avatar-detail-by-id/${auth.avatarId}`, {
        headers: { 'Authorization': 'Bearer ' + auth.token }
    });
    if (!res.ok) return null;
    const data = await res.json();
    const detail = data?.result ?? data;
    if (!detail) return null;
    return {
        displayName: [detail.firstName || detail.FirstName, detail.lastName || detail.LastName].filter(Boolean).join(' ') || detail.username || detail.Username,
        username: detail.username || detail.Username,
        level: detail.level ?? detail.Level ?? '?',
        karma: detail.karma ?? detail.Karma ?? '?',
        portraitUrl: detail.portrait && detail.portrait.startsWith('data:') ? detail.portrait : (detail.portrait || detail.Portrait || '')
    };
}
