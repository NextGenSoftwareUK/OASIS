// Avatar Dashboard Module
// Full implementation for Avatar tab functionality

// State management
const avatarDashboardState = {
    avatar: null,
    avatarDetail: null,
    isLoading: false,
    isEditing: false,
    error: null,
    baseUrl: null
};

// Initialize base URL (use centralized function if available, otherwise define local)
function getAvatarDashboardAPIBaseURL() {
    if (avatarDashboardState.baseUrl) {
        return avatarDashboardState.baseUrl;
    }
    
    // Try to use centralized function first
    if (typeof getOASISAPIBaseURL === 'function') {
        return getOASISAPIBaseURL();
    }
    
    const hostname = window.location.hostname;
    const protocol = window.location.protocol;
    
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
        return 'https://localhost:5000';
    }
    
    if (protocol === 'https:') {
        return 'https://api.oasisplatform.world';
    }
    return 'http://api.oasisplatform.world';
}

// Get authentication token (use centralized function if available, otherwise local)
function getAvatarDashboardAuthToken() {
    // Try to use centralized function first
    if (typeof getAuthToken === 'function') {
        return getAuthToken();
    }
    
    // Fallback to local implementation
    try {
        const authData = localStorage.getItem('oasis_auth');
        if (authData) {
            const auth = JSON.parse(authData);
            return auth.token || auth.jwtToken;
        }
    } catch (error) {
        console.error('Error getting auth token:', error);
    }
    return null;
}

// Get current avatar ID
function getCurrentAvatarId() {
    try {
        const authData = localStorage.getItem('oasis_auth');
        if (authData) {
            const auth = JSON.parse(authData);
            return auth.avatar?.avatarId || auth.avatar?.id;
        }
    } catch (error) {
        console.error('Error getting avatar ID:', error);
    }
    return null;
}

// Fetch avatar profile
async function fetchAvatarProfile(avatarId) {
    const token = getAvatarDashboardAuthToken();
    if (!token) {
        throw new Error('Not authenticated. Please log in.');
    }

    const baseUrl = getAvatarDashboardAPIBaseURL();
    const response = await fetch(`${baseUrl}/api/avatar/get-by-id/${avatarId}`, {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        }
    });

    if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: `HTTP ${response.status}` }));
        throw new Error(errorData.message || errorData.error || `HTTP ${response.status}`);
    }

    const data = await response.json();
    return data.result?.result || data.result || data;
}

// Fetch avatar detail (includes karma, stats, etc.)
async function fetchAvatarDetail(avatarId) {
    const token = getAvatarDashboardAuthToken();
    if (!token) {
        throw new Error('Not authenticated. Please log in.');
    }

    const baseUrl = getAvatarDashboardAPIBaseURL();
    const response = await fetch(`${baseUrl}/api/avatar/get-avatar-detail-by-id/${avatarId}`, {
        method: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        }
    });

    if (!response.ok) {
        // If 403, user might not have permission for detail endpoint, return null
        if (response.status === 403) {
            return null;
        }
        const errorData = await response.json().catch(() => ({ message: `HTTP ${response.status}` }));
        throw new Error(errorData.message || errorData.error || `HTTP ${response.status}`);
    }

    const data = await response.json();
    return data.result?.result || data.result || data;
}

// Update avatar profile
async function updateAvatarProfile(avatarId, updateData) {
    const token = getAvatarDashboardAuthToken();
    if (!token) {
        throw new Error('Not authenticated. Please log in.');
    }

    const baseUrl = getAvatarDashboardAPIBaseURL();
    const response = await fetch(`${baseUrl}/api/avatar/update-by-id/${avatarId}`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        },
        body: JSON.stringify(updateData)
    });

    if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: `HTTP ${response.status}` }));
        throw new Error(errorData.message || errorData.error || `HTTP ${response.status}`);
    }

    const data = await response.json();
    return data.result?.result || data.result || data;
}

// Render avatar profile display
function renderAvatarProfile(avatar, avatarDetail) {
    const container = document.getElementById('avatar-dashboard-content');
    if (!container) return;

    const karma = avatarDetail?.karma || avatar?.karma || 0;
    const xp = avatarDetail?.xp || avatar?.xp || 0;
    const level = avatarDetail?.level || avatar?.level || 1;
    const registrationDate = avatar?.createdDate 
        ? new Date(avatar.createdDate).toLocaleDateString()
        : 'N/A';

    container.innerHTML = `
        <div class="portal-section">
            <div class="avatar-dashboard-header">
                <h2 class="portal-section-title">Avatar Profile</h2>
                <button class="portal-button portal-button-primary" onclick="avatarDashboardEditProfile()">
                    <span class="portal-button-icon">✏️</span>
                    Edit Profile
                </button>
            </div>

            <div class="avatar-dashboard-grid">
                <!-- Profile Card -->
                <div class="avatar-profile-card">
                    <div class="avatar-profile-header">
                        <div class="avatar-profile-image">
                            ${avatar?.avatarPortrait?.value 
                                ? `<img src="${avatar.avatarPortrait.value}" alt="Avatar" />`
                                : `<div class="avatar-profile-placeholder">${(avatar?.firstName?.[0] || avatar?.username?.[0] || 'A').toUpperCase()}</div>`
                            }
                        </div>
                        <div class="avatar-profile-info">
                            <h3 class="avatar-profile-name">
                                ${avatar?.fullName || `${avatar?.firstName || ''} ${avatar?.lastName || ''}`.trim() || avatar?.username || 'User'}
                            </h3>
                            <p class="avatar-profile-username">@${avatar?.username || 'username'}</p>
                            <p class="avatar-profile-email">${avatar?.email || 'N/A'}</p>
                        </div>
                    </div>
                    
                    <div class="avatar-profile-details">
                        <div class="avatar-detail-item">
                            <span class="avatar-detail-label">Avatar ID:</span>
                            <span class="avatar-detail-value avatar-id-link" title="${avatar?.avatarId || avatar?.id}">
                                ${(avatar?.avatarId || avatar?.id || '').substring(0, 8)}...
                            </span>
                        </div>
                        <div class="avatar-detail-item">
                            <span class="avatar-detail-label">Registration Date:</span>
                            <span class="avatar-detail-value">${registrationDate}</span>
                        </div>
                        ${avatar?.avatarType 
                            ? `<div class="avatar-detail-item">
                                <span class="avatar-detail-label">Avatar Type:</span>
                                <span class="avatar-detail-value">${avatar.avatarType}</span>
                               </div>`
                            : ''
                        }
                    </div>
                </div>

                <!-- Statistics Card -->
                <div class="avatar-stats-card">
                    <h3 class="avatar-stats-title">Statistics</h3>
                    <div class="avatar-stats-grid">
                        <div class="avatar-stat-item">
                            <div class="avatar-stat-icon">⭐</div>
                            <div class="avatar-stat-content">
                                <div class="avatar-stat-label">Karma</div>
                                <div class="avatar-stat-value">${karma.toLocaleString()}</div>
                            </div>
                        </div>
                        <div class="avatar-stat-item">
                            <div class="avatar-stat-icon">📈</div>
                            <div class="avatar-stat-content">
                                <div class="avatar-stat-label">XP</div>
                                <div class="avatar-stat-value">${xp.toLocaleString()}</div>
                            </div>
                        </div>
                        <div class="avatar-stat-item">
                            <div class="avatar-stat-icon">🎯</div>
                            <div class="avatar-stat-content">
                                <div class="avatar-stat-label">Level</div>
                                <div class="avatar-stat-value">${level}</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            ${avatarDetail 
                ? `<div class="avatar-additional-stats">
                    <h3>Additional Information</h3>
                    <div class="avatar-detail-grid">
                        ${avatarDetail.address 
                            ? `<div class="avatar-detail-item">
                                <span class="avatar-detail-label">Address:</span>
                                <span class="avatar-detail-value">${avatarDetail.address}</span>
                               </div>`
                            : ''
                        }
                        ${avatarDetail.dob 
                            ? `<div class="avatar-detail-item">
                                <span class="avatar-detail-label">Date of Birth:</span>
                                <span class="avatar-detail-value">${new Date(avatarDetail.dob).toLocaleDateString()}</span>
                               </div>`
                            : ''
                        }
                    </div>
                </div>`
                : ''
            }
        </div>
    `;
}

// Render edit profile form
function renderEditProfileForm(avatar) {
    const container = document.getElementById('avatar-dashboard-content');
    if (!container) return;

    container.innerHTML = `
        <div class="portal-section">
            <div class="avatar-dashboard-header">
                <h2 class="portal-section-title">Edit Profile</h2>
                <button class="portal-button" onclick="avatarDashboardCancelEdit()">
                    Cancel
                </button>
            </div>

            <form id="avatar-edit-form" class="avatar-edit-form" onsubmit="avatarDashboardSaveProfile(event)">
                <div class="form-group">
                    <label for="avatar-title">Title</label>
                    <select id="avatar-title" name="title" class="form-control">
                        <option value="Mr" ${avatar?.title === 'Mr' ? 'selected' : ''}>Mr</option>
                        <option value="Mrs" ${avatar?.title === 'Mrs' ? 'selected' : ''}>Mrs</option>
                        <option value="Ms" ${avatar?.title === 'Ms' ? 'selected' : ''}>Ms</option>
                        <option value="Dr" ${avatar?.title === 'Dr' ? 'selected' : ''}>Dr</option>
                    </select>
                </div>

                <div class="form-group">
                    <label for="avatar-firstname">First Name</label>
                    <input 
                        type="text" 
                        id="avatar-firstname" 
                        name="firstName" 
                        class="form-control"
                        value="${avatar?.firstName || ''}"
                        required
                    />
                </div>

                <div class="form-group">
                    <label for="avatar-lastname">Last Name</label>
                    <input 
                        type="text" 
                        id="avatar-lastname" 
                        name="lastName" 
                        class="form-control"
                        value="${avatar?.lastName || ''}"
                        required
                    />
                </div>

                <div class="form-group">
                    <label for="avatar-username">Username</label>
                    <input 
                        type="text" 
                        id="avatar-username" 
                        name="username" 
                        class="form-control"
                        value="${avatar?.username || ''}"
                        required
                    />
                </div>

                <div class="form-group">
                    <label for="avatar-email">Email</label>
                    <input 
                        type="email" 
                        id="avatar-email" 
                        name="email" 
                        class="form-control"
                        value="${avatar?.email || ''}"
                        required
                    />
                </div>

                <div class="form-group">
                    <label for="avatar-password">New Password (leave blank to keep current)</label>
                    <input 
                        type="password" 
                        id="avatar-password" 
                        name="password" 
                        class="form-control"
                        placeholder="Enter new password"
                    />
                </div>

                <div id="avatar-edit-error" class="error-message" style="display: none;"></div>
                <div id="avatar-edit-success" class="success-message" style="display: none;"></div>

                <div class="form-actions">
                    <button type="submit" class="portal-button portal-button-primary" id="avatar-save-btn">
                        Save Changes
                    </button>
                    <button type="button" class="portal-button" onclick="avatarDashboardCancelEdit()">
                        Cancel
                    </button>
                </div>
            </form>
        </div>
    `;
}

// Render loading state
function renderLoading() {
    const container = document.getElementById('avatar-dashboard-content');
    if (!container) return;

    container.innerHTML = `
        <div class="portal-section">
            <div class="empty-state">
                <div class="loading-spinner"></div>
                <p class="empty-state-text">Loading avatar profile...</p>
            </div>
        </div>
    `;
}

// Render error state
function renderError(error) {
    const container = document.getElementById('avatar-dashboard-content');
    if (!container) return;

    container.innerHTML = `
        <div class="portal-section">
            <div class="empty-state">
                <div class="error-icon">⚠️</div>
                <h3 class="empty-state-title">Error Loading Profile</h3>
                <p class="empty-state-text">${error || 'An error occurred while loading your avatar profile.'}</p>
                <button class="portal-button portal-button-primary" onclick="loadAvatarDashboard()">
                    Try Again
                </button>
            </div>
        </div>
    `;
}

// Render not authenticated state
function renderNotAuthenticated() {
    const container = document.getElementById('avatar-dashboard-content');
    if (!container) return;

    container.innerHTML = `
        <div class="portal-section">
            <div class="empty-state">
                <div class="error-icon">🔒</div>
                <h3 class="empty-state-title">Authentication Required</h3>
                <p class="empty-state-text">Please log in to view your avatar profile.</p>
                <button class="portal-button portal-button-primary" onclick="switchAvatarAuthMode('login')">
                    Log In
                </button>
            </div>
        </div>
    `;
}

// Main load function
async function loadAvatarDashboard() {
    console.log('Avatar dashboard module loaded');
    const container = document.getElementById('avatar-dashboard-content');
    if (!container) {
        console.error('Avatar dashboard container not found');
        return;
    }

    // Check authentication
    const avatarId = getCurrentAvatarId();
    if (!avatarId) {
        renderNotAuthenticated();
        return;
    }

    // Set loading state
    avatarDashboardState.isLoading = true;
    avatarDashboardState.error = null;
    renderLoading();

    try {
        // Fetch avatar profile and detail in parallel
        const [avatar, avatarDetail] = await Promise.all([
            fetchAvatarProfile(avatarId),
            fetchAvatarDetail(avatarId).catch(() => null) // Detail might fail if not authorized
        ]);

        avatarDashboardState.avatar = avatar;
        avatarDashboardState.avatarDetail = avatarDetail;
        avatarDashboardState.isLoading = false;

        renderAvatarProfile(avatar, avatarDetail);
    } catch (error) {
        console.error('Error loading avatar dashboard:', error);
        avatarDashboardState.error = error.message;
        avatarDashboardState.isLoading = false;
        renderError(error.message);
    }
}

// Edit profile function
function avatarDashboardEditProfile() {
    if (!avatarDashboardState.avatar) {
        loadAvatarDashboard();
        return;
    }
    avatarDashboardState.isEditing = true;
    renderEditProfileForm(avatarDashboardState.avatar);
}

// Cancel edit function
function avatarDashboardCancelEdit() {
    avatarDashboardState.isEditing = false;
    if (avatarDashboardState.avatar) {
        renderAvatarProfile(avatarDashboardState.avatar, avatarDashboardState.avatarDetail);
    } else {
        loadAvatarDashboard();
    }
}

// Save profile function
async function avatarDashboardSaveProfile(event) {
    event.preventDefault();
    
    const avatarId = getCurrentAvatarId();
    if (!avatarId) {
        alert('Not authenticated. Please log in.');
        return;
    }

    const form = event.target;
    const formData = new FormData(form);
    const updateData = {
        title: formData.get('title') || undefined,
        firstName: formData.get('firstName') || undefined,
        lastName: formData.get('lastName') || undefined,
        username: formData.get('username') || undefined,
        email: formData.get('email') || undefined,
        password: formData.get('password') || undefined
    };

    // Remove undefined values
    Object.keys(updateData).forEach(key => {
        if (updateData[key] === undefined || updateData[key] === '') {
            delete updateData[key];
        }
    });

    const errorDiv = document.getElementById('avatar-edit-error');
    const successDiv = document.getElementById('avatar-edit-success');
    const saveBtn = document.getElementById('avatar-save-btn');

    // Hide previous messages
    if (errorDiv) errorDiv.style.display = 'none';
    if (successDiv) successDiv.style.display = 'none';

    // Disable save button
    if (saveBtn) {
        saveBtn.disabled = true;
        saveBtn.textContent = 'Saving...';
    }

    try {
        const updatedAvatar = await updateAvatarProfile(avatarId, updateData);
        avatarDashboardState.avatar = updatedAvatar;

        // Update localStorage auth data
        try {
            const authData = localStorage.getItem('oasis_auth');
            if (authData) {
                const auth = JSON.parse(authData);
                auth.avatar = { ...auth.avatar, ...updatedAvatar };
                localStorage.setItem('oasis_auth', JSON.stringify(auth));
            }
        } catch (e) {
            console.warn('Could not update localStorage:', e);
        }

        // Show success message
        if (successDiv) {
            successDiv.textContent = 'Profile updated successfully!';
            successDiv.style.display = 'block';
        }

        // Reload profile after a short delay
        setTimeout(() => {
            avatarDashboardState.isEditing = false;
            loadAvatarDashboard();
        }, 1500);
    } catch (error) {
        console.error('Error updating profile:', error);
        if (errorDiv) {
            errorDiv.textContent = error.message || 'Failed to update profile. Please try again.';
            errorDiv.style.display = 'block';
        }
        if (saveBtn) {
            saveBtn.disabled = false;
            saveBtn.textContent = 'Save Changes';
        }
    }
}

// Export to window
if (typeof window !== 'undefined') {
    window.loadAvatarDashboard = loadAvatarDashboard;
    window.avatarDashboardEditProfile = avatarDashboardEditProfile;
    window.avatarDashboardCancelEdit = avatarDashboardCancelEdit;
    window.avatarDashboardSaveProfile = avatarDashboardSaveProfile;
}

