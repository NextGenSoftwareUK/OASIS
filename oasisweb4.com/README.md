# OASIS Web4 Marketing Site

Modern marketing site for OASIS Web4 built with Vite + React + TypeScript + Material-UI.

## Features

- **Plans Page**: Bronze, Silver, Gold, Enterprise pricing with checkout integration
- **Providers Page**: Lists all supported Web2/Web3 providers
- **APIs Page**: Core WEB4 APIs (AVATAR, KARMA, DATA, WALLET, NFT, KEYS)
- **Responsive Design**: Mobile-first Material-UI components
- **Checkout Integration**: Connects to existing SubscriptionController

## Development

```bash
# Install dependencies
npm install

# Start dev server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

## Environment

Copy `env.example` to `.env.local` and set:

```bash
VITE_WEB4_API_BASE=http://localhost:5000/api
```

## API Integration

The site connects to the existing SubscriptionController at:
- `GET /api/subscription/plans` - List available plans
- `POST /api/subscription/checkout/session` - Create checkout session
- `GET /api/subscription/subscriptions/me` - User subscriptions
- `GET /api/subscription/orders/me` - User orders

## Deployment

Build the site and serve the `dist/` folder with any static hosting service.

