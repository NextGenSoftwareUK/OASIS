/**
 * GeoNFT Interface - Spatial GeoNFT Management with Interactive Maps
 */

const geoNFTInterfaceState = {
    baseUrl: window.location.origin,
    authToken: null,
    avatarId: null,
    map: null,
    markers: [],
    geoNFTs: [],
    currentGeoNFT: null,
    view: 'map', // 'map', 'place', 'detail'
    selectedLocation: null
};

/**
 * Show GeoNFT Placement Interface
 */
function showGeoNFTPlacementInterface() {
    geoNFTInterfaceState.view = 'place';
    const container = document.getElementById('star-dashboard-content') || document.getElementById('geonft-interface-container');
    if (!container) {
        const starTab = document.getElementById('tab-star');
        if (starTab) {
            const newContainer = document.createElement('div');
            newContainer.id = 'geonft-interface-container';
            starTab.appendChild(newContainer);
            container = newContainer;
        } else {
            console.error('GeoNFT interface container not found');
            return;
        }
    }
    
    container.innerHTML = renderGeoNFTPlacementInterface();
    
    // Initialize map after a short delay to ensure container is rendered
    setTimeout(() => {
        initGeoNFTMap('placement');
    }, 100);
}

/**
 * Show GeoNFT Map View
 */
function showGeoNFTMap() {
    geoNFTInterfaceState.view = 'map';
    const container = document.getElementById('star-dashboard-content') || document.getElementById('geonft-interface-container');
    if (!container) {
        const starTab = document.getElementById('tab-star');
        if (starTab) {
            const newContainer = document.createElement('div');
            newContainer.id = 'geonft-interface-container';
            starTab.appendChild(newContainer);
            container = newContainer;
        } else {
            console.error('GeoNFT interface container not found');
            return;
        }
    }
    
    container.innerHTML = renderGeoNFTMapView();
    
    setTimeout(() => {
        initGeoNFTMap('view');
        loadGeoNFTs();
    }, 100);
}

/**
 * Render GeoNFT Placement Interface
 */
function renderGeoNFTPlacementInterface() {
    return `
        <div class="portal-section">
            <div style="margin-bottom: 2rem;">
                <h2 style="
                    font-size: 1.5rem;
                    font-weight: 300;
                    color: var(--text-primary);
                    margin-bottom: 0.5rem;
                ">Place GeoNFT</h2>
                <p style="
                    font-size: 0.875rem;
                    color: var(--text-secondary);
                ">Click on the map to select a location for your GeoNFT</p>
            </div>

            <div style="display: grid; grid-template-columns: 2fr 1fr; gap: 2rem;">
                <div>
                    <div class="portal-card" style="padding: 0; overflow: hidden;">
                        <div id="geonft-map" style="width: 100%; height: 500px; border-radius: 0.5rem 0.5rem 0 0;"></div>
                        <div style="padding: 1rem; background: rgba(255, 255, 255, 0.02); border-top: 1px solid var(--border-color);">
                            <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem;">
                                Selected Location:
                            </div>
                            <div id="selected-location-display" style="
                                font-size: 0.875rem;
                                color: var(--text-secondary);
                                font-family: 'Courier New', monospace;
                            ">
                                Click on the map to select a location
                            </div>
                        </div>
                    </div>
                </div>
                <div>
                    ${renderGeoNFTPlacementForm()}
                </div>
            </div>
        </div>
    `;
}

/**
 * Render GeoNFT Map View
 */
function renderGeoNFTMapView() {
    return `
        <div class="portal-section">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 2rem;">
                <div>
                    <h2 style="
                        font-size: 1.5rem;
                        font-weight: 300;
                        color: var(--text-primary);
                        margin-bottom: 0.5rem;
                    ">GeoNFT Map</h2>
                    <p style="
                        font-size: 0.875rem;
                        color: var(--text-secondary);
                    ">Discover and explore GeoNFTs around the world</p>
                </div>
                <button class="btn-primary" onclick="showGeoNFTPlacementInterface()">
                    Place New GeoNFT
                </button>
            </div>

            <div class="portal-card" style="padding: 0; overflow: hidden;">
                <div id="geonft-map" style="width: 100%; height: 600px;"></div>
            </div>

            <div style="margin-top: 1.5rem;">
                <h3 style="
                    font-size: 1rem;
                    font-weight: 500;
                    color: var(--text-primary);
                    margin-bottom: 1rem;
                ">Nearby GeoNFTs</h3>
                <div id="nearby-geonfts-list" style="display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 1rem;">
                    ${renderNearbyGeoNFTsList()}
                </div>
            </div>
        </div>
    `;
}

/**
 * Render GeoNFT Placement Form
 */
function renderGeoNFTPlacementForm() {
    return `
        <div class="portal-card" style="padding: 1.5rem;">
            <h3 style="
                font-size: 1rem;
                font-weight: 500;
                color: var(--text-primary);
                margin-bottom: 1rem;
            ">Place GeoNFT</h3>
            <form id="geonft-placement-form" onsubmit="handleGeoNFTPlacement(event)">
                <div style="margin-bottom: 1rem;">
                    <label style="
                        display: block;
                        font-size: 0.75rem;
                        color: var(--text-tertiary);
                        margin-bottom: 0.5rem;
                    ">GeoNFT ID</label>
                    <input 
                        type="text" 
                        id="geonft-id-input"
                        class="form-input"
                        placeholder="Enter GeoNFT ID or select from your NFTs"
                        required
                    />
                </div>

                <div style="margin-bottom: 1rem;">
                    <label style="
                        display: block;
                        font-size: 0.75rem;
                        color: var(--text-tertiary);
                        margin-bottom: 0.5rem;
                    ">Location</label>
                    <div id="location-coords-display" style="
                        padding: 0.75rem;
                        background: rgba(255, 255, 255, 0.02);
                        border: 1px solid var(--border-color);
                        border-radius: 0.5rem;
                        font-family: 'Courier New', monospace;
                        font-size: 0.875rem;
                        color: var(--text-secondary);
                        margin-bottom: 0.5rem;
                    ">
                        Click on map to select
                    </div>
                    <input 
                        type="text" 
                        id="location-address-input"
                        class="form-input"
                        placeholder="Or search for an address"
                        style="margin-bottom: 0.5rem;"
                    />
                    <button 
                        type="button"
                        class="btn-text"
                        onclick="geocodeAddress()"
                        style="font-size: 0.75rem;"
                    >
                        Search Location
                    </button>
                </div>

                <div style="margin-bottom: 1.5rem;">
                    <label style="
                        display: flex;
                        align-items: center;
                        gap: 0.5rem;
                        font-size: 0.875rem;
                        color: var(--text-secondary);
                        cursor: pointer;
                    ">
                        <input 
                            type="checkbox" 
                            id="allow-others-collect"
                            checked
                            style="cursor: pointer;"
                        />
                        <span>Allow others to collect</span>
                    </label>
                </div>

                <button 
                    type="submit"
                    class="btn-primary"
                    style="width: 100%;"
                    id="place-geonft-btn"
                    disabled
                >
                    Place GeoNFT
                </button>
            </form>
        </div>
    `;
}

/**
 * Initialize GeoNFT Map
 */
function initGeoNFTMap(mode) {
    const mapContainer = document.getElementById('geonft-map');
    if (!mapContainer || typeof L === 'undefined') {
        console.error('Map container or Leaflet not found');
        return;
    }

    // Destroy existing map if it exists
    if (geoNFTInterfaceState.map) {
        geoNFTInterfaceState.map.remove();
        geoNFTInterfaceState.map = null;
        geoNFTInterfaceState.markers = [];
    }

    // Default to London (or user's location if available)
    const defaultLat = 51.5074;
    const defaultLng = -0.1278;
    const defaultZoom = 13;

    // Create map
    geoNFTInterfaceState.map = L.map('geonft-map').setView([defaultLat, defaultLng], defaultZoom);

    // Add tile layer
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors',
        maxZoom: 19
    }).addTo(geoNFTInterfaceState.map);

    if (mode === 'placement') {
        // Placement mode: allow clicking to select location
        geoNFTInterfaceState.map.on('click', function(e) {
            handleMapClick(e.latlng);
        });

        // Add marker for selected location
        if (geoNFTInterfaceState.selectedLocation) {
            addLocationMarker(geoNFTInterfaceState.selectedLocation);
        }
    } else if (mode === 'view') {
        // View mode: show existing GeoNFTs
        // Markers will be added when GeoNFTs are loaded
    }
}

/**
 * Handle Map Click
 */
function handleMapClick(latlng) {
    geoNFTInterfaceState.selectedLocation = latlng;
    
    // Update marker
    addLocationMarker(latlng);
    
    // Update display
    const coordsDisplay = document.getElementById('location-coords-display') || document.getElementById('selected-location-display');
    if (coordsDisplay) {
        coordsDisplay.textContent = `${latlng.lat.toFixed(6)}, ${latlng.lng.toFixed(6)}`;
    }

    // Enable place button
    const placeBtn = document.getElementById('place-geonft-btn');
    if (placeBtn) {
        placeBtn.disabled = false;
    }

    // Reverse geocode to get address
    reverseGeocode(latlng);
}

/**
 * Add Location Marker
 */
function addLocationMarker(latlng) {
    // Remove existing marker
    if (geoNFTInterfaceState.markers.length > 0 && geoNFTInterfaceState.view === 'place') {
        geoNFTInterfaceState.markers.forEach(marker => marker.remove());
        geoNFTInterfaceState.markers = [];
    }

    // Add new marker
    const marker = L.marker(latlng, {
        draggable: geoNFTInterfaceState.view === 'place'
    }).addTo(geoNFTInterfaceState.map);

    if (geoNFTInterfaceState.view === 'place') {
        marker.bindPopup('Selected Location<br>Drag to adjust').openPopup();
        
        marker.on('dragend', function(e) {
            const newLatLng = marker.getLatLng();
            handleMapClick(newLatLng);
        });
    }

    geoNFTInterfaceState.markers.push(marker);
}

/**
 * Add GeoNFT Marker to Map
 */
function addGeoNFTMarker(geoNFT) {
    if (!geoNFT.lat || !geoNFT.long) return;

    // Convert micro-degrees to degrees
    const lat = geoNFT.lat / 1000000;
    const lng = geoNFT.long / 1000000;

    const marker = L.marker([lat, lng]).addTo(geoNFTInterfaceState.map);

    marker.bindPopup(`
        <div style="min-width: 200px;">
            <h4 style="margin: 0 0 0.5rem 0; font-size: 0.875rem; font-weight: 500;">
                ${escapeHtml(geoNFT.title || 'Unnamed GeoNFT')}
            </h4>
            <p style="margin: 0 0 0.5rem 0; font-size: 0.75rem; color: #666;">
                ${escapeHtml((geoNFT.description || '').substring(0, 100))}
            </p>
            <button 
                onclick="showGeoNFTDetail('${geoNFT.id || ''}')"
                style="
                    padding: 0.25rem 0.75rem;
                    background: rgba(168, 85, 247, 0.2);
                    border: 1px solid rgba(168, 85, 247, 0.5);
                    border-radius: 4px;
                    color: var(--text-primary);
                    cursor: pointer;
                    font-size: 0.75rem;
                "
            >
                View Details
            </button>
        </div>
    `);

    geoNFTInterfaceState.markers.push(marker);
}

/**
 * Reverse Geocode
 */
async function reverseGeocode(latlng) {
    try {
        // Using Nominatim (OpenStreetMap geocoder)
        const response = await fetch(
            `https://nominatim.openstreetmap.org/reverse?format=json&lat=${latlng.lat}&lon=${latlng.lng}&zoom=18&addressdetails=1`,
            {
                headers: {
                    'User-Agent': 'OASIS Portal'
                }
            }
        );
        const data = await response.json();
        
        const addressInput = document.getElementById('location-address-input');
        if (addressInput && data.display_name) {
            addressInput.value = data.display_name;
        }
    } catch (error) {
        console.error('Error reverse geocoding:', error);
    }
}

/**
 * Geocode Address
 */
async function geocodeAddress() {
    const addressInput = document.getElementById('location-address-input');
    if (!addressInput || !addressInput.value.trim()) return;

    try {
        const response = await fetch(
            `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(addressInput.value)}&limit=1`,
            {
                headers: {
                    'User-Agent': 'OASIS Portal'
                }
            }
        );
        const data = await response.json();
        
        if (data.length > 0) {
            const lat = parseFloat(data[0].lat);
            const lng = parseFloat(data[0].lon);
            const latlng = L.latLng(lat, lng);
            
            if (geoNFTInterfaceState.map) {
                geoNFTInterfaceState.map.setView(latlng, 15);
                handleMapClick(latlng);
            }
        } else {
            alert('Location not found');
        }
    } catch (error) {
        console.error('Error geocoding:', error);
        alert('Error searching location');
    }
}

/**
 * Handle GeoNFT Placement
 */
async function handleGeoNFTPlacement(event) {
    event.preventDefault();
    
    if (!geoNFTInterfaceState.selectedLocation) {
        alert('Please select a location on the map');
        return;
    }

    const geoNFTId = document.getElementById('geonft-id-input')?.value;
    if (!geoNFTId) {
        alert('Please enter a GeoNFT ID');
        return;
    }

    const allowOthers = document.getElementById('allow-others-collect')?.checked || false;

    try {
        const authData = localStorage.getItem('oasis_auth');
        let authToken = null;
        let avatarId = null;
        if (authData) {
            const auth = JSON.parse(authData);
            authToken = auth.token;
            avatarId = auth.avatarId || auth.avatar?.id;
        }

        // Convert to micro-degrees
        const latMicroDegrees = Math.round(geoNFTInterfaceState.selectedLocation.lat * 1000000);
        const longMicroDegrees = Math.round(geoNFTInterfaceState.selectedLocation.lng * 1000000);

        const response = await fetch(`${geoNFTInterfaceState.baseUrl}/api/nft/place-geo-nft`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': authToken ? `Bearer ${authToken}` : ''
            },
            body: JSON.stringify({
                originalOASISNFTId: geoNFTId,
                lat: latMicroDegrees,
                long: longMicroDegrees,
                allowOtherPlayersToAlsoCollect: allowOthers,
                permSpawn: false,
                globalSpawnQuantity: 1,
                playerSpawnQuantity: 1,
                respawnDurationInSeconds: 0,
                geoNFTMetaDataProvider: 'MongoDBOASIS',
                placedByAvatarId: avatarId
            })
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Failed to place GeoNFT');
        }

        const result = await response.json();
        alert('GeoNFT placed successfully!');
        
        // Switch to map view
        showGeoNFTMap();
    } catch (error) {
        console.error('Error placing GeoNFT:', error);
        alert('Failed to place GeoNFT: ' + error.message);
    }
}

/**
 * Load GeoNFTs
 */
async function loadGeoNFTs() {
    try {
        const authData = localStorage.getItem('oasis_auth');
        if (authData) {
            const auth = JSON.parse(authData);
            geoNFTInterfaceState.authToken = auth.token;
            geoNFTInterfaceState.avatarId = auth.avatarId || auth.avatar?.id;
        }

        // Placeholder - would load from API
        // For now, add markers for any loaded GeoNFTs
        geoNFTInterfaceState.geoNFTs.forEach(geoNFT => {
            addGeoNFTMarker(geoNFT);
        });

        updateNearbyGeoNFTsList();
    } catch (error) {
        console.error('Error loading GeoNFTs:', error);
    }
}

/**
 * Render Nearby GeoNFTs List
 */
function renderNearbyGeoNFTsList() {
    const geoNFTs = geoNFTInterfaceState.geoNFTs || [];

    if (geoNFTs.length === 0) {
        return `
            <div class="empty-state" style="grid-column: 1 / -1;">
                <p class="empty-state-text">No GeoNFTs found nearby. Place your first GeoNFT to get started!</p>
            </div>
        `;
    }

    return geoNFTs.map(geoNFT => renderGeoNFTCard(geoNFT)).join('');
}

/**
 * Render GeoNFT Card
 */
function renderGeoNFTCard(geoNFT) {
    const lat = geoNFT.lat ? (geoNFT.lat / 1000000).toFixed(4) : 'N/A';
    const lng = geoNFT.long ? (geoNFT.long / 1000000).toFixed(4) : 'N/A';

    return `
        <div class="portal-card" style="padding: 1rem; cursor: pointer;" onclick="showGeoNFTDetail('${geoNFT.id || ''}')">
            <div style="font-size: 0.75rem; color: var(--text-tertiary); margin-bottom: 0.5rem;">GeoNFT</div>
            <h4 style="
                font-size: 0.875rem;
                font-weight: 500;
                color: var(--text-primary);
                margin: 0 0 0.5rem 0;
            ">${escapeHtml(geoNFT.title || 'Unnamed GeoNFT')}</h4>
            <p style="
                font-size: 0.8125rem;
                color: var(--text-secondary);
                margin: 0 0 0.75rem 0;
                line-height: 1.5;
            ">${escapeHtml((geoNFT.description || '').substring(0, 100))}${(geoNFT.description || '').length > 100 ? '...' : ''}</p>
            <div style="
                font-size: 0.75rem;
                color: var(--text-tertiary);
                font-family: 'Courier New', monospace;
            ">
                ${lat}, ${lng}
            </div>
        </div>
    `;
}

/**
 * Show GeoNFT Detail
 */
function showGeoNFTDetail(geoNFTId) {
    geoNFTInterfaceState.currentGeoNFT = geoNFTInterfaceState.geoNFTs.find(g => g.id === geoNFTId);
    
    // Would show detail modal or navigate to detail page
    console.log('Show GeoNFT detail:', geoNFTId);
}

/**
 * Update Nearby GeoNFTs List
 */
function updateNearbyGeoNFTsList() {
    const container = document.getElementById('nearby-geonfts-list');
    if (container) {
        container.innerHTML = renderNearbyGeoNFTsList();
    }
}

/**
 * Helper Functions
 */
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Export functions to window
if (typeof window !== 'undefined') {
    window.showGeoNFTPlacementInterface = showGeoNFTPlacementInterface;
    window.showGeoNFTMap = showGeoNFTMap;
    window.showGeoNFTDetail = showGeoNFTDetail;
    window.handleGeoNFTPlacement = handleGeoNFTPlacement;
    window.geocodeAddress = geocodeAddress;
}

