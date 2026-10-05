# OASIS Avatar SSO SDK - React

Complete React integration for OASIS Avatar Single Sign-On authentication with hooks and context.

## Installation

```bash
npm install @oasis/avatar-sso-react
```

## Quick Start

### 1. Wrap Your App with Provider

```jsx
import { OasisSSOProvider } from '@oasis/avatar-sso-react';

function App() {
  return (
    <OasisSSOProvider
      apiUrl="https://api.oasis.network"
      provider="Auto"
    >
      <YourApp />
    </OasisSSOProvider>
  );
}
```

### 2. Use the Hook in Components

```jsx
import { useOasisSSO } from '@oasis/avatar-sso-react';

function LoginPage() {
  const { login, logout, user, isAuthenticated, isLoading } = useOasisSSO();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [provider, setProvider] = useState('Auto');

  const handleLogin = async () => {
    try {
      await login(username, password, provider);
      // Redirect on success
    } catch (error) {
      console.error('Login failed:', error);
    }
  };

  if (isLoading) return <div>Loading...</div>;

  if (isAuthenticated) {
    return (
      <div>
        <h2>Welcome, {user?.username}!</h2>
        <button onClick={logout}>Beam Out</button>
      </div>
    );
  }

  return (
    <div>
      <h2>OASIS Avatar Login</h2>
      <input
        value={username}
        onChange={(e) => setUsername(e.target.value)}
        placeholder="Username"
      />
      <input
        type="password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        placeholder="Password"
      />
      <select value={provider} onChange={(e) => setProvider(e.target.value)}>
        <option value="Auto">Auto</option>
        <option value="MongoDBOASIS">MongoDB</option>
        <option value="EthereumOASIS">Ethereum</option>
      </select>
      <button onClick={handleLogin}>Beam In</button>
    </div>
  );
}
```

### 3. Protected Routes

```jsx
import { Navigate } from 'react-router-dom';
import { useOasisSSO } from '@oasis/avatar-sso-react';

function ProtectedRoute({ children }) {
  const { isAuthenticated, isLoading } = useOasisSSO();

  if (isLoading) return <div>Loading...</div>;
  
  return isAuthenticated ? children : <Navigate to="/login" />;
}

// Usage
<Route
  path="/dashboard"
  element={
    <ProtectedRoute>
      <Dashboard />
    </ProtectedRoute>
  }
/>
```

## Features

✨ **React Hooks** - Modern hooks-based API
🎯 **Context Provider** - Global state management
🚀 **TypeScript** - Full type safety
🔄 **Auto Token Refresh** - Seamless session management
🎨 **Pre-built Components** - Ready-to-use UI components

## API Reference

### useOasisSSO Hook

Returns an object with:

```typescript
{
  // State
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  
  // Methods
  login: (username: string, password: string, provider?: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<void>;
}
```

### OasisSSOProvider Props

```typescript
{
  apiUrl: string;
  provider?: string;
  onAuthChange?: (isAuthenticated: boolean) => void;
  children: ReactNode;
}
```

## Pre-built Components

### OasisLoginButton

```jsx
import { OasisLoginButton } from '@oasis/avatar-sso-react';

<OasisLoginButton
  onSuccess={() => navigate('/dashboard')}
  onError={(error) => console.error(error)}
/>
```

### OasisAvatarDisplay

```jsx
import { OasisAvatarDisplay } from '@oasis/avatar-sso-react';

<OasisAvatarDisplay
  showName={true}
  showLogout={true}
/>
```

## License

MIT © OASIS Team

