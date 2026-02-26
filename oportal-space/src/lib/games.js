/**
 * Game config and launch URL builder.
 * Passes onode + token so games connect with same OASIS identity.
 */
import { getBaseURL, getToken } from './auth.js';

const DOOM_URL = import.meta.env.VITE_DOOM_URL || 'http://localhost:8765';
const QUAKE_URL = import.meta.env.VITE_QUAKE_URL || 'http://localhost:8767';

export const GAMES = [
    {
        id: 'doom',
        name: 'DOOM',
        playPath: DOOM_URL + '/',
        supportsStar: true,
        color: 0xc41e1e,
        logoUrl: '/doom-logo.png'
    },
    {
        id: 'quake',
        name: 'Quake',
        playPath: QUAKE_URL + '/',
        supportsStar: true,
        color: 0x4a90d9,
        logoUrl: '/quake-logo.svg',
        svgLogoUrl: null
    }
];

export function getGameLaunchUrl(game) {
    const baseUrl = getBaseURL();
    const token = getToken();
    let url = game.playPath;
    if (!url) return '#';
    if (url.startsWith('/') && !url.startsWith('//')) {
        url = (typeof window !== 'undefined' ? window.location.origin : '') + url;
    }
    if (game.supportsStar && baseUrl && token) {
        const sep = url.indexOf('?') >= 0 ? '&' : '?';
        url += sep + 'onode=' + encodeURIComponent(baseUrl) + '&token=' + encodeURIComponent(token);
    }
    return url;
}

export function launchGame(game) {
    const url = getGameLaunchUrl(game);
    if (typeof window !== 'undefined') window.open(url, '_blank', 'noopener');
}
