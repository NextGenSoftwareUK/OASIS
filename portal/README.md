# OASIS Portal

The OASIS Portal is a comprehensive dashboard for managing wallets, NFTs, smart contracts, data, bridges, and more.

## Quick Start

```bash
# Serve from parent directory (so relative paths work)
cd ..
python3 -m http.server 8080
```

Then open: **http://localhost:8080/portal/portal.html**

That's it! No npm, no build, just a simple Python server.

## Features

- **Dashboard**: Overview with stats, installed OAPPs, active quests, transaction history, and messages
- **Wallets**: Multi-chain wallet management
- **NFTs**: NFT collection management
- **Smart Contracts**: Deploy and manage smart contracts
- **Data**: Data Holons management
- **Bridges**: Cross-chain bridge transactions
- **Developer**: Developer tools and OAPP management
- **Settings**: Avatar profile and preferences

## Development

### Simple Local Server

The portal needs to be served from the `OASIS_CLEAN` directory (parent of portal) so the relative paths to `oasisweb4 site/new-v2/` files work:

```bash
# From OASIS_CLEAN directory
python3 -m http.server 8080
```

Then navigate to: **http://localhost:8080/portal/portal.html**

### Build (Optional)

```bash
npm install
npm run build
```

The build output will be in the `dist/` directory.

### Validate

```bash
npm run validate
```

## File Structure

```
portal/
├── portal.html          # Main portal file
├── package.json         # Build configuration (optional)
├── build.js            # Build script (optional)
└── validate.js         # Validation script (optional)
```

## Dependencies

The portal depends on files from the oasisweb4 site:
- `../oasisweb4 site/new-v2/styles.css`
- `../oasisweb4 site/new-v2/script.js`

These paths are relative, so the server must run from the `OASIS_CLEAN` directory.

## License

MIT

