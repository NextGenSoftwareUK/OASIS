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

// Login Modal Functions
const OASIS_API_URL = 'https://api.oasisplatform.world';

let currentAuthMode = 'login';

function openLoginModal() {
    const modal = document.getElementById('loginModal');
    if (modal) {
        modal.style.display = 'flex';
        document.body.style.overflow = 'hidden';
    }
}

function closeLoginModal() {
    const modal = document.getElementById('loginModal');
    if (modal) {
        modal.style.display = 'none';
        document.body.style.overflow = '';
    }
    // Reset form
    document.getElementById('authForm').reset();
    document.getElementById('authError').style.display = 'none';
    switchAuthMode('login');
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
        registerFields.style.display = 'block';
        usernameLabel.textContent = 'Username';
        submitText.textContent = 'Create avatar';
        document.getElementById('email').required = true;
    } else {
        registerFields.style.display = 'none';
        usernameLabel.textContent = 'Username or email';
        submitText.textContent = 'Sign in';
        document.getElementById('email').required = false;
    }
}

async function handleAuthSubmit(event) {
    event.preventDefault();
    const errorDiv = document.getElementById('authError');
    const submitBtn = document.getElementById('authSubmitBtn');
    const submitText = document.getElementById('authSubmitText');
    
    errorDiv.style.display = 'none';
    submitBtn.disabled = true;
    submitText.textContent = currentAuthMode === 'login' ? 'Signing in...' : 'Creating avatar...';
    
    try {
        const username = document.getElementById('username').value.trim();
        const password = document.getElementById('password').value;
        
        if (currentAuthMode === 'login') {
            // Login
            const response = await fetch(`${OASIS_API_URL}/api/avatar/authenticate`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    username: username,
                    password: password,
                    ...(username.includes('@') && { email: username })
                })
            });
            
            const data = await response.json();
            
            if (!response.ok || (data.isError && data.isError)) {
                throw new Error(data.message || 'Authentication failed');
            }
            
            // Store auth data
            const authData = {
                avatar: data.result?.avatar || data.avatar || data.result,
                token: data.result?.jwtToken || data.jwtToken || data.token,
                refreshToken: data.result?.refreshToken || data.refreshToken
            };
            
            localStorage.setItem('oasis_auth', JSON.stringify(authData));
            
            // Close modal and update UI
            closeLoginModal();
            updateUserUI(authData.avatar);
            
        } else {
            // Register
            const email = document.getElementById('email').value.trim();
            const firstName = document.getElementById('firstName').value.trim();
            const lastName = document.getElementById('lastName').value.trim();
            
            const response = await fetch(`${OASIS_API_URL}/api/avatar/register`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    username: username,
                    email: email,
                    password: password,
                    firstName: firstName || undefined,
                    lastName: lastName || undefined
                })
            });
            
            const data = await response.json();
            
            if (!response.ok || (data.isError && data.isError)) {
                throw new Error(data.message || 'Registration failed');
            }
            
            // Store auth data
            const authData = {
                avatar: data.result?.avatar || data.avatar || data.result,
                token: data.result?.jwtToken || data.jwtToken || data.token,
                refreshToken: data.result?.refreshToken || data.refreshToken
            };
            
            localStorage.setItem('oasis_auth', JSON.stringify(authData));
            
            // Close modal and update UI
            closeLoginModal();
            updateUserUI(authData.avatar);
        }
    } catch (error) {
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
        username: 'demo.explorer',
        firstName: 'Demo',
        lastName: 'Explorer'
    };
    
    localStorage.setItem('oasis_auth', JSON.stringify({
        avatar: demoAvatar,
        token: null,
        refreshToken: null
    }));
    
    closeLoginModal();
    updateUserUI(demoAvatar);
}

function updateUserUI(avatar) {
    // Update login button to show user info
    const loginBtn = document.querySelector('.nav-actions .btn-text');
    if (loginBtn && avatar) {
        loginBtn.textContent = avatar.username || avatar.email || 'Account';
        loginBtn.onclick = () => {
            // Redirect to portal
            window.location.href = 'portal.html';
        };
    }
}

function showUserMenu(avatar) {
    // Redirect to portal dashboard
    window.location.href = 'portal.html';
}

// Check for existing auth on page load
window.addEventListener('DOMContentLoaded', () => {
    const authData = localStorage.getItem('oasis_auth');
    if (authData) {
        try {
            const auth = JSON.parse(authData);
            if (auth.avatar) {
                updateUserUI(auth.avatar);
            }
        } catch (e) {
            // Invalid auth data
            localStorage.removeItem('oasis_auth');
        }
    }
});

// Close modal on Escape key
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        closeLoginModal();
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

