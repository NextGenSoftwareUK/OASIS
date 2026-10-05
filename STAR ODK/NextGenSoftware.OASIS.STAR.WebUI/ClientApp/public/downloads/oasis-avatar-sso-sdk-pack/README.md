# OASIS Avatar SSO SDK Pack

Welcome to the official **OASIS Avatar Single Sign-On (SSO) SDK** bundle. This pack includes everything you need to integrate the OASIS Avatar identity layer into your applications, along with ready-to-use UI widgets, framework adapters, and detailed documentation.

## What’s Inside

- `core/`
  - `avatar-sso-client.ts` – TypeScript client for the Avatar SSO API
  - `avatar-sso-widget/` – Drop-in web widget with configurable theme & provider options
  - `types/` – Shared TypeScript definitions for sessions, providers, and auth flows
- `docs/`
  - `integration-guide.pdf` – Step-by-step integration walkthrough
  - `api-quickstart.md` – REST/GraphQL reference snippets and Postman collection links
  - `session-management.md` – Guidance for remote beam-in/out, session audits, and provider replication policies
- `examples/`
  - `react-example/` – Minimal React app showing widget integration and session controls
  - `node-server/` – Express server demo handling SSO callbacks and token exchange
- `tools/`
  - `scripts/setup-env.ps1` – PowerShell helper for configuring `.env` secrets
  - `postman/OASIS-Avatar-SSO.postman_collection.json`

## Quick Start

1. Install dependencies:
   ```bash
   npm install @oasis/avatar-sso
   ```
2. Initialize the client:
   ```ts
   import { OasisAvatarSSO } from '@oasis/avatar-sso';

   const sso = new OasisAvatarSSO({
     clientId: process.env.REACT_APP_OASIS_CLIENT_ID!,
     redirectUri: 'https://app.example.com/auth/callback',
     provider: 'Auto'
   });
   ```
3. Launch the widget:
   ```ts
   sso.render({
     containerId: 'oasis-avatar-widget',
     theme: 'starnet-dark',
     providers: ['Auto', 'EthereumOASIS', 'IPFSOASIS']
   });
   ```
4. Fetch the active session:
   ```ts
   const session = await sso.getCurrentSession();
   console.log('Avatar karma:', session.avatar.karma);
   ```

## Documentation
- Full docs: `docs/integration-guide.pdf`
- API reference: `docs/api-quickstart.md`
- Session management best practices: `docs/session-management.md`

## Support & Feedback
- Email: support@oasis.network
- Discord: https://discord.gg/oasis
- Issues: https://github.com/oasis/avatar-sso-sdk

Happy building ✨

