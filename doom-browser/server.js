/**
 * Serves the launcher and proxies the DOOM bundle so the game loads
 * same-origin (no CORS). Run: npm install && npm start
 * Then open http://localhost:8765 and play DOOM with your OASIS avatar in the page.
 */

require('dotenv').config();

const express = require('express');
const path = require('path');
const https = require('https');
const http = require('http');

const WANTED_PORT = parseInt(process.env.PORT, 10) || 8765;
const DOOM_BUNDLE_URL = 'https://cdn.dos.zone/custom/dos/doom.jsdos';

const app = express();

// Allow cross-origin requests so the portal (e.g. :5003) can load WASM/IWAD from this server
app.use('/doom-wasm-build', (req, res, next) => {
  res.setHeader('Access-Control-Allow-Origin', '*');
  next();
});

// Static files (index.html, doom-wasm-embed.html, etc.)
app.use(express.static(__dirname));
// Emscripten doom.js asks for "websockets-doom.wasm" but we have "doom.wasm" — serve it
app.get('/doom-wasm-build/websockets-doom.wasm', (req, res) => {
  res.setHeader('Content-Type', 'application/wasm');
  res.sendFile(path.join(__dirname, 'doom-wasm-build', 'doom.wasm'));
});
// WASM DOOM build (doom.js, doom.wasm, doom1.wad) — see doom-wasm/README.md
app.use('/doom-wasm-build', express.static(path.join(__dirname, 'doom-wasm-build')));

// Proxy DOOM bundle so the browser gets it same-origin (js-dos then works)
app.get('/doom.jsdos', (req, res) => {
  const client = DOOM_BUNDLE_URL.startsWith('https') ? https : http;
  client.get(DOOM_BUNDLE_URL, (upstream) => {
    if (upstream.statusCode === 404) {
      res.status(502).send('DOOM js-dos bundle not available from CDN. Use "Play DOOM" (local WASM) instead.');
      return;
    }
    res.setHeader('Content-Type', upstream.headers['content-type'] || 'application/octet-stream');
    upstream.pipe(res);
  }).on('error', (err) => {
    console.error('Proxy error:', err.message);
    res.status(502).send('Failed to fetch DOOM bundle. Use "Play DOOM" (local WASM) instead.');
  });
});

// ─── MagicBlock SOAR: submit score to leaderboard ─────────────────────────────
// POST /api/soar/submit-score  { playerWallet: string, score: number }
app.post('/api/soar/submit-score', express.json(), async (req, res) => {
  try {
    const { playerWallet, score } = req.body || {};
    if (!playerWallet || score == null) {
      return res.status(400).json({ success: false, error: 'playerWallet and score are required' });
    }

    const keypairEnv = process.env.SOAR_AUTH_KEYPAIR;
    const leaderboardPda = process.env.SOAR_LEADERBOARD_PDA;
    const rpcUrl = process.env.SOLANA_RPC_URL || 'https://api.devnet.solana.com';

    if (!keypairEnv || !leaderboardPda) {
      // Env not configured — log and return a success stub so the frontend demo
      // still works without a funded wallet on devnet.
      console.log('[SOAR] submit-score stub (env not configured) — player:', playerWallet, 'score:', score);
      return res.json({ success: true, stub: true, playerWallet, score });
    }

    const web3 = require('@solana/web3.js');
    const { SoarProgram } = require('@magicblock-labs/soar-sdk');
    const BN = require('bn.js');
    const bs58 = require('bs58');

    const connection = new web3.Connection(rpcUrl, 'confirmed');

    // Decode the authority keypair from base58 or JSON array
    let authKeypair;
    try {
      // Try JSON array first (e.g. "[1,2,3,...]")
      const arr = JSON.parse(keypairEnv);
      authKeypair = web3.Keypair.fromSecretKey(Uint8Array.from(arr));
    } catch {
      // Fall back to base58
      authKeypair = web3.Keypair.fromSecretKey(bs58.decode(keypairEnv));
    }

    const client = SoarProgram.getFromConnection(connection, authKeypair.publicKey);
    const playerPublicKey = new web3.PublicKey(playerWallet);
    const leaderboardPublicKey = new web3.PublicKey(leaderboardPda);

    // Ensure the player account is initialised (safe to call if already exists)
    try {
      const initTx = await client.initializePlayerAccount(
        playerPublicKey,
        playerWallet.slice(0, 16), // username truncated to 16 chars
        web3.PublicKey.default      // nft meta placeholder
      );
      await web3.sendAndConfirmTransaction(connection, initTx.transaction, [authKeypair]);
    } catch (initErr) {
      // Player already exists — ignore
      if (!initErr.message || !initErr.message.includes('already in use')) {
        console.warn('[SOAR] initializePlayerAccount warning:', initErr.message);
      }
    }

    // Register player entry on the leaderboard (safe to call if already registered)
    try {
      const regTx = await client.registerPlayerEntryForLeaderBoard(
        playerPublicKey,
        leaderboardPublicKey
      );
      await web3.sendAndConfirmTransaction(connection, regTx.transaction, [authKeypair]);
    } catch (regErr) {
      if (!regErr.message || !regErr.message.includes('already in use')) {
        console.warn('[SOAR] registerPlayerEntry warning:', regErr.message);
      }
    }

    // Submit the score
    const submitTx = await client.submitScoreToLeaderBoard(
      playerPublicKey,
      authKeypair.publicKey,
      leaderboardPublicKey,
      new BN(Math.round(score))
    );
    const sig = await web3.sendAndConfirmTransaction(
      connection,
      submitTx.transaction,
      [authKeypair]
    );

    console.log('[SOAR] Score submitted — player:', playerWallet, 'score:', score, 'sig:', sig);
    return res.json({ success: true, signature: sig, playerWallet, score });
  } catch (err) {
    console.error('[SOAR] submit-score error:', err && err.message ? err.message : err);
    return res.status(500).json({ success: false, error: err && err.message ? err.message : String(err) });
  }
});

// ─── SOAR: leaderboard top scores (for leaderboard.html embed) ────────────────
// GET /api/soar/top-scores
app.get('/api/soar/top-scores', async (req, res) => {
  try {
    const leaderboardPda = process.env.SOAR_LEADERBOARD_PDA;
    const rpcUrl = process.env.SOLANA_RPC_URL || 'https://api.devnet.solana.com';

    if (!leaderboardPda) {
      // Return stub scores so leaderboard.html works before setup
      return res.json({
        success: true,
        stub: true,
        scores: [
          { rank: 1, player: 'DOOMSLAYER', score: 3600 },
          { rank: 2, player: 'OASIS_PILOT', score: 2400 },
          { rank: 3, player: 'QUAKE_LORD', score: 1800 },
        ]
      });
    }

    const web3 = require('@solana/web3.js');
    const { SoarProgram } = require('@magicblock-labs/soar-sdk');

    const connection = new web3.Connection(rpcUrl, 'confirmed');
    const client = SoarProgram.getFromConnection(connection, web3.PublicKey.default);
    const leaderboardPublicKey = new web3.PublicKey(leaderboardPda);

    const account = await client.fetchLeaderBoardAccount(leaderboardPublicKey);
    const topEntries = (account && account.topEntries && account.topEntries.topScores)
      ? account.topEntries.topScores
      : [];

    const scores = topEntries
      .filter(e => e && e.player)
      .map((e, i) => ({
        rank: i + 1,
        player: e.player.toBase58().slice(0, 8) + '…',
        score: e.entry && e.entry.score != null ? e.entry.score.toNumber() : 0,
      }));

    return res.json({ success: true, scores });
  } catch (err) {
    console.error('[SOAR] top-scores error:', err && err.message ? err.message : err);
    return res.status(500).json({ success: false, error: err && err.message ? err.message : String(err) });
  }
});

function tryListen(port) {
  const server = app.listen(port, () => {
    console.log('DOOM + OASIS Avatar launcher: http://localhost:' + port);
    console.log('DOOM only (no portal): http://localhost:' + port + '/doom-only.html');
    console.log('Log in, then click "Play DOOM" to run the game with your avatar in the page.');
  });
  server.on('error', (err) => {
    if (err.code === 'EADDRINUSE' && port < 8770) {
      tryListen(port + 1);
    } else {
      console.error(err);
      process.exit(1);
    }
  });
}
tryListen(WANTED_PORT);
