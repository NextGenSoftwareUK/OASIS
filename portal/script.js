// Navigation
const nav = document.getElementById('nav');
const navToggle = document.getElementById('navToggle');
const navMenu = document.getElementById('navMenu');

// Mobile menu toggle
if (navToggle) {
    navToggle.addEventListener('click', () => {
        navMenu.classList.toggle('active');
    });
}

// Nav scroll effect
let lastScroll = 0;
window.addEventListener('scroll', () => {
    const currentScroll = window.pageYOffset;
    
    if (currentScroll <= 0) {
        nav.style.background = 'rgba(10, 10, 10, 0.8)';
    } else {
        nav.style.background = 'rgba(10, 10, 10, 0.95)';
    }
    
    lastScroll = currentScroll;
});

// Code tabs
const codeTabs = document.querySelectorAll('.code-tab');
const codeBlocks = document.querySelectorAll('.code-block');

codeTabs.forEach(tab => {
    tab.addEventListener('click', () => {
        const targetCode = tab.getAttribute('data-tab');
        
        // Update active tab
        codeTabs.forEach(t => t.classList.remove('active'));
        tab.classList.add('active');
        
        // Update active code block
        codeBlocks.forEach(block => {
            block.classList.remove('active');
            if (block.getAttribute('data-code') === targetCode) {
                block.classList.add('active');
            }
        });
    });
});

// Ecosystem filters
const filterBtns = document.querySelectorAll('.filter-btn');
const ecosystemItems = document.querySelectorAll('.ecosystem-item');

filterBtns.forEach(btn => {
    btn.addEventListener('click', () => {
        const filter = btn.getAttribute('data-filter');
        
        // Update active filter
        filterBtns.forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        
        // Filter items
        ecosystemItems.forEach(item => {
            const category = item.getAttribute('data-category');
            if (filter === 'all' || category === filter) {
                item.style.display = 'block';
            } else {
                item.style.display = 'none';
            }
        });
    });
});

// Smooth scroll for anchor links
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    });
});

// Section indicator update on scroll
const sections = document.querySelectorAll('.section[data-section]');
const updateSectionIndicator = () => {
    const scrollPos = window.scrollY + window.innerHeight / 2;
    
    sections.forEach(section => {
        const sectionTop = section.offsetTop;
        const sectionBottom = sectionTop + section.offsetHeight;
        const sectionNumber = section.getAttribute('data-section');
        
        if (scrollPos >= sectionTop && scrollPos < sectionBottom) {
            // Update scroll indicator if visible
            const scrollIndicator = document.querySelector('.scroll-section');
            if (scrollIndicator) {
                const numberSpan = scrollIndicator.querySelector('.section-number');
                if (numberSpan) {
                    numberSpan.textContent = sectionNumber.padStart(2, '0');
                }
            }
        }
    });
};

window.addEventListener('scroll', updateSectionIndicator);
updateSectionIndicator();

// Stats animation (simple counter)
const animateStats = () => {
    const statValues = document.querySelectorAll('[data-target]');
    
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const target = parseInt(entry.target.getAttribute('data-target'));
                const duration = 2000;
                const increment = target / (duration / 16);
                let current = 0;
                
                const updateCounter = () => {
                    current += increment;
                    if (current < target) {
                        entry.target.textContent = Math.floor(current);
                        requestAnimationFrame(updateCounter);
                    } else {
                        entry.target.textContent = target;
                    }
                };
                
                updateCounter();
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.5 });
    
    statValues.forEach(stat => observer.observe(stat));
};

animateStats();

// Code block hover effect
const codeBlocksWithHint = document.querySelectorAll('.code-block');
codeBlocksWithHint.forEach(block => {
    const hint = block.querySelector('.code-block-hint');
    if (hint) {
        block.addEventListener('mouseenter', () => {
            hint.style.opacity = '0';
        });
        block.addEventListener('mouseleave', () => {
            hint.style.opacity = '1';
        });
    }
});

// Hero carousel animation
const carouselWords = ['RWA', 'NFT', 'Tokenization', 'DeFi', 'Game', 'Metaverse', 'Identity', 'Bridge', 'Smart Contract', 'Data'];
let currentWordIndex = 0;
const carouselElement = document.getElementById('carouselWord');

if (carouselElement) {
    const rotateCarousel = () => {
        // Fade out
        carouselElement.classList.add('fade-out');
        
        setTimeout(() => {
            // Update word
            currentWordIndex = (currentWordIndex + 1) % carouselWords.length;
            carouselElement.textContent = carouselWords[currentWordIndex];
            
            // Remove fade-out, add fade-in
            carouselElement.classList.remove('fade-out');
            carouselElement.classList.add('fade-in');
            
            // Trigger active state for fade-in
            setTimeout(() => {
                carouselElement.classList.add('active');
            }, 10);
            
            // Clean up after animation
            setTimeout(() => {
                carouselElement.classList.remove('fade-in', 'active');
            }, 300);
        }, 300);
    };
    
    // Start rotation after initial delay
    setTimeout(() => {
        rotateCarousel(); // First rotation
        setInterval(rotateCarousel, 3000); // Then continue every 3 seconds (slower)
    }, 2000); // Wait 2 seconds before starting
}

// Avatar Authentication Screen Functions (from zypherpunk-wallet-ui)
let currentAvatarAuthMode = 'login';
let privacyMode = true;

function openLoginModal() {
    const screen = document.getElementById('avatarAuthScreen');
    if (screen) {
        screen.style.display = 'flex';
        document.body.style.overflow = 'hidden';
        // Reset to login mode
        switchAvatarAuthMode('login');
    }
}

function closeLoginModal() {
    const screen = document.getElementById('avatarAuthScreen');
    if (screen) {
        screen.style.display = 'none';
        document.body.style.overflow = '';
    }
    // Reset form
    const form = document.getElementById('avatarAuthForm');
    if (form) {
        form.reset();
    }
    const errorDiv = document.getElementById('avatarAuthError');
    if (errorDiv) {
        errorDiv.style.display = 'none';
    }
    switchAvatarAuthMode('login');
}

function switchAvatarAuthMode(mode) {
    currentAvatarAuthMode = mode;
    const tabs = document.querySelectorAll('.avatar-auth-tab');
    const privacyModeDiv = document.getElementById('avatarAuthPrivacyMode');
    const emailField = document.getElementById('avatarAuthEmailField');
    const nameFields = document.getElementById('avatarAuthNameFields');
    const usernameLabel = document.getElementById('avatarUsernameLabel');
    const usernameHint = document.getElementById('avatarUsernameHint');
    const submitText = document.getElementById('avatarAuthSubmitText');
    
    tabs.forEach(tab => {
        if (tab.getAttribute('data-mode') === mode) {
            tab.classList.add('active');
        } else {
            tab.classList.remove('active');
        }
    });
    
    if (mode === 'register') {
        if (privacyModeDiv) privacyModeDiv.style.display = 'block';
        if (usernameLabel) usernameLabel.textContent = 'Username';
        if (usernameHint) usernameHint.style.display = privacyMode ? 'block' : 'none';
        if (emailField) emailField.style.display = privacyMode ? 'none' : 'block';
        if (nameFields) nameFields.style.display = privacyMode ? 'none' : 'block';
        if (submitText) submitText.textContent = 'Create avatar';
    } else {
        if (privacyModeDiv) privacyModeDiv.style.display = 'none';
        if (usernameLabel) usernameLabel.textContent = 'Username or email';
        if (usernameHint) usernameHint.style.display = 'none';
        if (emailField) emailField.style.display = 'none';
        if (nameFields) nameFields.style.display = 'none';
        if (submitText) submitText.textContent = 'Sign in';
    }
}

function togglePrivacyMode() {
    const checkbox = document.getElementById('privacyModeCheckbox');
    privacyMode = checkbox ? checkbox.checked : true;
    
    if (currentAvatarAuthMode === 'register') {
        const emailField = document.getElementById('avatarAuthEmailField');
        const nameFields = document.getElementById('avatarAuthNameFields');
        const usernameHint = document.getElementById('avatarUsernameHint');
        
        if (emailField) emailField.style.display = privacyMode ? 'none' : 'block';
        if (nameFields) nameFields.style.display = privacyMode ? 'none' : 'block';
        if (usernameHint) usernameHint.style.display = privacyMode ? 'block' : 'none';
    }
}

// Generate fake email for privacy mode
function generateFakeEmail(username) {
    const randomId = Math.random().toString(36).substring(2, 9);
    return `${username}_${randomId}@privacy.local`;
}

// Generate random username
function generateRandomUsername() {
    return `privacy_${Math.random().toString(36).substring(2, 11)}`;
}

async function handleAvatarAuthSubmit(event) {
    event.preventDefault();
    const errorDiv = document.getElementById('avatarAuthError');
    const submitBtn = document.getElementById('avatarAuthSubmitBtn');
    const submitText = document.getElementById('avatarAuthSubmitText');
    
    if (!errorDiv || !submitBtn || !submitText) {
        console.error('Avatar auth form elements not found');
        return;
    }
    
    errorDiv.style.display = 'none';
    submitBtn.disabled = true;
    submitText.textContent = currentAvatarAuthMode === 'login' ? 'Connecting...' : 'Creating avatar...';
    
    try {
        const username = document.getElementById('avatarUsername').value.trim();
        const password = document.getElementById('avatarPassword').value;
        
        if (!password) {
            throw new Error('Password is required');
        }
        
        let authResult;
        
        if (currentAvatarAuthMode === 'login') {
            if (!username) {
                throw new Error('Enter your username/email and password.');
            }
            authResult = await authAPI.login(username, password);
        } else {
            // Register
            let finalUsername = username;
            let email = '';
            let firstName = '';
            let lastName = '';
            
            if (privacyMode) {
                // Privacy mode: Generate fake email and random username if needed
                finalUsername = username || generateRandomUsername();
                email = generateFakeEmail(finalUsername);
                firstName = 'Privacy';
                lastName = 'User';
            } else {
                email = document.getElementById('avatarEmail').value.trim();
                firstName = document.getElementById('avatarFirstName')?.value.trim() || '';
                lastName = document.getElementById('avatarLastName')?.value.trim() || '';
                
                if (!email) {
                    throw new Error('Email is required when privacy mode is off.');
                }
            }
            
            authResult = await authAPI.register({
                username: finalUsername,
                email: email,
                password: password,
                firstName: firstName || undefined,
                lastName: lastName || undefined
            });
        }
        
        // Store auth data using centralized store
        authStore.setAuth({
            avatar: authResult.avatar,
            token: authResult.token,
            refreshToken: authResult.refreshToken
        });
        
        // Close modal and update UI
        closeLoginModal();
        updateUserUI(authResult.avatar);
        
        // Dispatch custom event for other modules to listen to
        window.dispatchEvent(new CustomEvent('oasis:auth:login', {
            detail: { avatar: authResult.avatar }
        }));
        
    } catch (error) {
        console.error('Avatar authentication error:', error);
        errorDiv.textContent = error.message || 'An error occurred. Please try again.';
        errorDiv.style.display = 'block';
    } finally {
        submitBtn.disabled = false;
        submitText.textContent = currentAvatarAuthMode === 'login' ? 'Sign in' : 'Create avatar';
    }
}

function switchAuthMode(mode) {
    currentAuthMode = mode;
    const tabs = document.querySelectorAll('.auth-tab');
    const registerFields = document.getElementById('registerFields');
    const usernameLabel = document.getElementById('usernameLabel');
    const submitText = document.getElementById('authSubmitText');
    
    tabs.forEach(tab => {
        if (tab.getAttribute('data-mode') === mode) {
            tab.classList.add('active');
        } else {
            tab.classList.remove('active');
        }
    });
    
    if (mode === 'register') {
        if (registerFields) registerFields.style.display = 'block';
        if (usernameLabel) usernameLabel.textContent = 'Username';
        if (submitText) submitText.textContent = 'Create avatar';
        const emailField = document.getElementById('email');
        if (emailField) emailField.required = true;
    } else {
        if (registerFields) registerFields.style.display = 'none';
        if (usernameLabel) usernameLabel.textContent = 'Username or email';
        if (submitText) submitText.textContent = 'Sign in';
        const emailField = document.getElementById('email');
        if (emailField) emailField.required = false;
    }
}

async function handleAuthSubmit(event) {
    event.preventDefault();
    const errorDiv = document.getElementById('authError');
    const submitBtn = document.getElementById('authSubmitBtn');
    const submitText = document.getElementById('authSubmitText');
    
    if (!errorDiv || !submitBtn || !submitText) {
        console.error('Auth form elements not found');
        return;
    }
    
    errorDiv.style.display = 'none';
    submitBtn.disabled = true;
    submitText.textContent = currentAuthMode === 'login' ? 'Signing in...' : 'Creating avatar...';
    
    try {
        const username = document.getElementById('username').value.trim();
        const password = document.getElementById('password').value;
        
        if (!username || !password) {
            throw new Error('Please fill in all required fields');
        }
        
        let authResult;
        
        if (currentAuthMode === 'login') {
            // Login using centralized auth module
            authResult = await authAPI.login(username, password);
        } else {
            // Register
            const email = document.getElementById('email').value.trim();
            const firstName = document.getElementById('firstName')?.value.trim() || '';
            const lastName = document.getElementById('lastName')?.value.trim() || '';
            
            if (!email) {
                throw new Error('Email is required for registration');
            }
            
            authResult = await authAPI.register({
                username,
                email,
                password,
                firstName: firstName || undefined,
                lastName: lastName || undefined
            });
        }
        
        // Store auth data using centralized store
        authStore.setAuth({
            avatar: authResult.avatar,
            token: authResult.token,
            refreshToken: authResult.refreshToken
        });
        
        // Close modal and update UI
        closeLoginModal();
        updateUserUI(authResult.avatar);
        
        // Dispatch custom event for other modules to listen to
        window.dispatchEvent(new CustomEvent('oasis:auth:login', {
            detail: { avatar: authResult.avatar }
        }));
        
    } catch (error) {
        console.error('Authentication error:', error);
        errorDiv.textContent = error.message || 'An error occurred. Please try again.';
        errorDiv.style.display = 'block';
    } finally {
        submitBtn.disabled = false;
        submitText.textContent = currentAuthMode === 'login' ? 'Sign in' : 'Create avatar';
    }
}

function useDemoAvatar() {
    const demoAvatar = {
        avatarId: 'demo-123',
        id: 'demo-123',
        username: 'demo.explorer',
        firstName: 'Demo',
        lastName: 'Explorer'
    };
    
    authStore.setAuth({
        avatar: demoAvatar,
        token: null,
        refreshToken: null
    });
    
    closeLoginModal();
    updateUserUI(demoAvatar);
    
    // Dispatch custom event
    window.dispatchEvent(new CustomEvent('oasis:auth:login', {
        detail: { avatar: demoAvatar }
    }));
}

function logout() {
    authStore.clearAuth();
    updateUserUI(null);
    
    // Dispatch custom event
    window.dispatchEvent(new CustomEvent('oasis:auth:logout'));
}

function updateUserUI(avatar) {
    // Update login button in main navigation (index.html)
    const loginBtn = document.querySelector('.nav-actions .btn-text');
    if (loginBtn) {
        if (avatar) {
            loginBtn.textContent = avatar.username || avatar.email || 'Account';
            loginBtn.onclick = () => {
                // Redirect to portal
                window.location.href = 'portal.html';
            };
        } else {
            loginBtn.textContent = 'Sign in';
            loginBtn.onclick = openLoginModal;
        }
    }
    
    // Update login button in portal navigation (portal.html)
    const portalLoginBtn = document.getElementById('userMenuBtn');
    if (portalLoginBtn) {
        if (avatar) {
            portalLoginBtn.textContent = avatar.username || avatar.email || 'Account';
            portalLoginBtn.onclick = () => {
                // Show user menu or logout option
                if (confirm('Would you like to log out?')) {
                    logout();
                }
            };
            portalLoginBtn.style.background = 'rgba(255, 255, 255, 0.15)';
        } else {
            portalLoginBtn.textContent = 'Sign in';
            portalLoginBtn.onclick = openLoginModal;
            portalLoginBtn.style.background = 'rgba(255, 255, 255, 0.1)';
        }
    }
    
    // Update portal header if on portal page
    const portalAvatar = document.querySelector('.portal-avatar');
    const portalName = document.querySelector('.portal-name');
    const avatarIdSpan = document.getElementById('avatarId');
    
    if (avatar) {
        if (portalAvatar) {
            // Generate initials from full name or username
            const firstName = avatar.firstName || avatar.FirstName || '';
            const lastName = avatar.lastName || avatar.LastName || '';
            const fullName = avatar.fullName || avatar.FullName || 
                           (firstName && lastName ? `${firstName} ${lastName}`.trim() : null);
            const initials = (firstName?.[0] || '') + (lastName?.[0] || '') || 
                            (fullName ? fullName.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase() : null) ||
                            (avatar.username?.[0]?.toUpperCase() || '?');
            portalAvatar.textContent = initials;
        }
        if (portalName) {
            // Show full name if available, otherwise username/email
            const firstName = avatar.firstName || avatar.FirstName || '';
            const lastName = avatar.lastName || avatar.LastName || '';
            const fullName = avatar.fullName || avatar.FullName || 
                           (firstName && lastName ? `${firstName} ${lastName}`.trim() : null) ||
                           firstName || lastName ||
                           avatar.username || avatar.Username || 
                           avatar.email || avatar.Email || 
                           'Avatar';
            portalName.textContent = fullName;
        }
        if (avatarIdSpan) {
            avatarIdSpan.textContent = avatar.id || avatar.avatarId || '-';
        }
    } else {
        if (portalAvatar) {
            portalAvatar.textContent = 'OA';
        }
        if (portalName) {
            portalName.textContent = 'Loading...';
        }
        if (avatarIdSpan) {
            avatarIdSpan.textContent = '-';
        }
    }
}

function showUserMenu(avatar) {
    // Redirect to portal dashboard
    window.location.href = 'portal.html';
}

// Check for existing auth on page load
window.addEventListener('DOMContentLoaded', () => {
    if (authStore.isAuthenticated()) {
        const avatar = authStore.getAvatar();
        if (avatar) {
            updateUserUI(avatar);
        }
    }
});

// Close modal on Escape key
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        const screen = document.getElementById('avatarAuthScreen');
        if (screen && screen.style.display !== 'none') {
            closeLoginModal();
        }
    }
});

// Product card toggle for mobile
function toggleProductCard(button) {
    const card = button.closest('.product-card');
    const isExpanded = card.classList.contains('expanded');
    
    if (isExpanded) {
        card.classList.remove('expanded');
        button.classList.remove('active');
    } else {
        card.classList.add('expanded');
        button.classList.add('active');
    }
}

