# TimoRides Workspace

This folder consolidates the TimoRides backend (`ride-scheduler-be`) and frontend (`ride-scheduler-fe`) projects. Use this README as the entry point for local setup, development, and deployment references.

## Project Overview

- `ride-scheduler-be`: Node.js + Express REST API connected to MongoDB with integrations for Twilio, SendGrid, Cloudinary, and Swagger docs.
- `ride-scheduler-fe`: Angular 15 single-page application with NgRx state management, Google Places autocomplete, and Flutterwave payment integration.

Both applications expect environment variables for external services and deploy to separate hosts (Render/AWS for frontend, MongoDB-backed service for backend).

## Prerequisites

- Node.js 18+
- npm 9+
- MongoDB database (Atlas or self-hosted)
- Optional: Render CLI/AWS CLI for deployment workflows

## Backend Setup (`ride-scheduler-be`)

1. Copy `.env.example` to `.env` and populate values (see template below).
2. Install dependencies:

   ```bash
   cd ride-scheduler-be
   npm install
   ```

3. Start the development server:

   ```bash
   npm run start
   ```

- API base URL defaults to `http://localhost:4205`.
- Swagger docs available at `http://localhost:4205/api-docs` when `NODE_ENV` is not `production`.

### Backend Environment Template (`ride-scheduler-be/.env.example`)

```
# Application
NODE_ENV=development
PORT=4205
BASE_SERVER_URL=http://localhost:4205
BASE_CLIENT_URL=http://localhost:4200

# Database
Database_Url=mongodb://localhost:27017/timorides

# Authentication & Tokens
ACCESS_TOKEN_SECRET=replace_me
REFRESH_TOKEN_SECRET=replace_me
VALIDATION_TOKEN_SECRET=replace_me

# Email (SendGrid)
SENDGRID_API_KEY=replace_me
SENDER_MAIL=noreply@timorides.com

# SMS (Twilio)
TWILIO_ACCOUNT_SID=replace_me
TWILIO_AUTH_TOKEN=replace_me
TWILIO_PHONE_NUMBER=replace_me

# Cloudinary
CLOUDINARY_NAME=replace_me
CLOUDINARY_KEY=replace_me
CLOUDINARY_secret=replace_me

# Distance / Maps
DISTANCE_METRIX=replace_me
GOOGLE_CLOUD=replace_me

# Payments / Wallet
PAYMENT_WEBHOOK_SECRET=replace_me

# Misc
SUPPORT_MAIL=support@timorides.com
```

## Frontend Setup (`ride-scheduler-fe`)

1. Copy `.env.example` to `.env` at the project root.
2. Install dependencies:

   ```bash
   cd ride-scheduler-fe
   npm install
   ```

3. Run the Angular dev server:

   ```bash
   npm run start
   ```

- Frontend serves at `http://localhost:4200` by default with proxy configuration targeting the backend.

### Frontend Environment Template (`ride-scheduler-fe/.env.example`)

```
GOOGLE_MAPS_API_KEY=replace_me
BASE_URL=http://localhost:4205
FLUTTER_KEY=replace_me
SECRET_KEY=replace_me
MAPS_URL=https://maps.googleapis.com/maps/api/js
```

Ensure the Angular build pipeline loads these environment variables (the project uses `extra-webpack.config.js` and `dotenv` to inject them at build time).

## Development Tips

- Use `proxy.conf.json` in the frontend to avoid CORS issues during local development.
- NgRx DevTools is enabled in dev mode; install the Redux browser extension for state inspection.
- Backend uses `nodemon` for live reloads when running `npm run start`.
- Address reported npm vulnerabilities via `npm audit fix` once you confirm the impact.

## Deployment Notes

- Backend: Configure environment variables on the hosting platform (Render, Heroku, etc.) and provide MongoDB connection strings and API keys.
- Frontend: For AWS EC2/Render deployments, build with `ng build --configuration production` and serve the `dist/` output. Remember to set environment variables before building.

Refer to individual project READMEs for more detailed deployment walkthroughs.

---

## PathPulse.ai Integration Documentation

As part of our strategic partnerships, we've prepared comprehensive documentation for integrating **PathPulse.ai** as a routing and optimization provider within the TimoRides/OASIS ecosystem.

### Integration Documents

1. **[PathPulse OASIS Integration Guide](./PathPulse_OASIS_Integration_Guide.md)** (Technical Specification)
   - Detailed technical implementation guide
   - C#/.NET and Node.js code examples
   - API specifications and data models
   - Testing strategies and monitoring

2. **[PathPulse Integration Executive Summary](./PathPulse_Integration_Executive_Summary.md)** (Business Overview)
   - High-level integration approach
   - Business benefits and ROI analysis
   - Timeline and success metrics
   - Partnership opportunities

3. **[PathPulse Architecture Diagrams](./PathPulse_Architecture_Diagram.md)** (Visual Reference)
   - Integration architecture diagrams
   - Request flow visualizations
   - Cost optimization strategies
   - Decision matrices

### Quick Links
- **OASIS Provider Architecture**: See `/NextGenSoftware.OASIS.API.Providers.*/` directories
- **Existing Map Providers**: MapboxOASIS, GOMapOASIS
- **TimoRides MVP Priorities**: See `Timo_MVP_Core_Priorities.md`

