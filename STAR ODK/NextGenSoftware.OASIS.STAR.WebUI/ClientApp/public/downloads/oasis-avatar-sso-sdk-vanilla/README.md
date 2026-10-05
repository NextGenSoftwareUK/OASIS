# OASIS Avatar SSO SDK - Vanilla JavaScript

Pure JavaScript implementation of OASIS Avatar SSO - no framework required!

## Installation

### Via CDN

```html
<script src="https://cdn.oasis.network/avatar-sso/v1.5.2/oasis-sso.min.js"></script>
```

### Via NPM

```bash
npm install @oasis/avatar-sso
```

## Quick Start

### Basic Usage

```html
<!DOCTYPE html>
<html>
<head>
    <script src="https://cdn.oasis.network/avatar-sso/v1.5.2/oasis-sso.min.js"></script>
</head>
<body>
    <div id="login-form">
        <input id="username" type="text" placeholder="Username" />
        <input id="password" type="password" placeholder="Password" />
        <select id="provider">
            <option value="Auto">Auto</option>
            <option value="MongoDBOASIS">MongoDB</option>
            <option value="EthereumOASIS">Ethereum</option>
        </select>
        <button onclick="login()">Beam In</button>
    </div>

    <script>
        // Initialize SDK
        const sso = new OasisAvatarSSO({
            apiUrl: 'https://api.oasis.network',
            provider: 'Auto'
        });

        async function login() {
            const username = document.getElementById('username').value;
            const password = document.getElementById('password').value;
            const provider = document.getElementById('provider').value;

            try {
                const result = await sso.login(username, password, provider);
                console.log('Login successful!', result);
                
                // Get current user
                const user = await sso.getCurrentUser();
                console.log('Current user:', user);
                
                // Redirect
                window.location.href = '/dashboard';
            } catch (error) {
                console.error('Login failed:', error);
                alert('Login failed: ' + error.message);
            }
        }
    </script>
</body>
</html>
```

### Advanced Usage with Event Listeners

```javascript
const sso = new OasisAvatarSSO({
    apiUrl: 'https://api.oasis.network',
    provider: 'Auto',
    onAuthChange: (isAuthenticated) => {
        console.log('Auth state changed:', isAuthenticated);
        updateUI(isAuthenticated);
    }
});

// Check if already authenticated
sso.isAuthenticated().then(isAuth => {
    if (isAuth) {
        showDashboard();
    } else {
        showLogin();
    }
});

// Login
async function performLogin() {
    try {
        await sso.login('username', 'password', 'Auto');
        const user = await sso.getCurrentUser();
        displayUserProfile(user);
    } catch (error) {
        handleError(error);
    }
}

// Logout
async function performLogout() {
    await sso.logout();
    window.location.href = '/';
}

// Auto-refresh token
setInterval(async () => {
    try {
        await sso.refreshToken();
    } catch (error) {
        console.error('Token refresh failed, logging out');
        await sso.logout();
    }
}, 15 * 60 * 1000); // Every 15 minutes
```

## API Reference

### Constructor

```javascript
new OasisAvatarSSO(options)
```

**Options:**
- `apiUrl` (string, required): Base URL of OASIS API
- `provider` (string, optional): Default provider (default: 'Auto')
- `onAuthChange` (function, optional): Callback when auth state changes

### Methods

#### `login(username, password, provider)`

Authenticate a user.

```javascript
const result = await sso.login('myusername', 'mypassword', 'Auto');
// Returns: { success: true, token: '...', avatar: {...} }
```

#### `logout()`

Log out the current user.

```javascript
await sso.logout();
```

#### `isAuthenticated()`

Check if user is authenticated.

```javascript
const isAuth = await sso.isAuthenticated();
// Returns: boolean
```

#### `getCurrentUser()`

Get current user data.

```javascript
const user = await sso.getCurrentUser();
// Returns: { id, username, email, firstName, lastName, avatarUrl }
```

#### `refreshToken()`

Refresh the authentication token.

```javascript
await sso.refreshToken();
```

#### `getAuthToken()`

Get the current auth token.

```javascript
const token = sso.getAuthToken();
// Returns: string | null
```

## Features

✨ **Zero Dependencies** - Pure vanilla JavaScript
🚀 **Lightweight** - Only 8KB minified + gzipped
🔐 **Secure** - Built-in token management
📱 **Mobile-Friendly** - Works on all devices
🎯 **Simple API** - Easy to understand and use

## Browser Support

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)
- Opera (latest)

## Examples

### With jQuery

```javascript
$('#login-btn').click(async function() {
    const username = $('#username').val();
    const password = $('#password').val();
    
    try {
        await sso.login(username, password);
        $('#user-name').text((await sso.getCurrentUser()).username);
        $('#login-modal').hide();
    } catch (error) {
        $('#error-msg').text(error.message);
    }
});
```

### With Fetch API

```javascript
// Making authenticated requests
const token = sso.getAuthToken();

fetch('https://api.oasis.network/avatar/profile', {
    headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
    }
})
.then(response => response.json())
.then(data => console.log(data));
```

## License

MIT © OASIS Team

