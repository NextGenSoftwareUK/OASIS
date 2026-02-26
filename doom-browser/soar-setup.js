/**
 * soar-setup.js — One-time SOAR game + leaderboard initialisation
 *
 * Run ONCE to create the on-chain SOAR game account and leaderboard, then
 * copy the printed addresses into your .env file.
 *
 * Usage:
 *   node soar-setup.js
 *
 * Required env vars (in .env or exported):
 *   SOAR_AUTH_KEYPAIR  — base58 or JSON-array secret key of the authority wallet
 *   SOLANA_RPC_URL     — optional, defaults to devnet
 */

require('dotenv').config();

const web3 = require('@solana/web3.js');
const { SoarProgram, GameType, Genre } = require('@magicblock-labs/soar-sdk');

async function main() {
  const rpcUrl = process.env.SOLANA_RPC_URL || 'https://api.devnet.solana.com';
  const keypairEnv = process.env.SOAR_AUTH_KEYPAIR;

  if (!keypairEnv) {
    console.error('ERROR: SOAR_AUTH_KEYPAIR is not set in .env');
    console.error('  Generate a new keypair: solana-keygen new --outfile auth-keypair.json');
    console.error('  Export it as JSON: SOAR_AUTH_KEYPAIR=$(cat auth-keypair.json)');
    process.exit(1);
  }

  // Decode keypair
  let authKeypair;
  try {
    const arr = JSON.parse(keypairEnv);
    authKeypair = web3.Keypair.fromSecretKey(Uint8Array.from(arr));
  } catch {
    const bs58 = require('bs58');
    authKeypair = web3.Keypair.fromSecretKey(bs58.decode(keypairEnv));
  }

  console.log('Authority wallet:', authKeypair.publicKey.toBase58());
  console.log('RPC URL:', rpcUrl);

  const connection = new web3.Connection(rpcUrl, 'confirmed');

  // Check balance
  const balance = await connection.getBalance(authKeypair.publicKey);
  console.log('Balance:', balance / web3.LAMPORTS_PER_SOL, 'SOL');
  if (balance < 0.05 * web3.LAMPORTS_PER_SOL) {
    console.warn('WARNING: Low balance. Airdrop on devnet: solana airdrop 1 ' + authKeypair.publicKey.toBase58() + ' --url devnet');
  }

  const client = SoarProgram.getFromConnection(connection, authKeypair.publicKey);

  // ── Step 1: Create game ───────────────────────────────────────────────────
  console.log('\n[1/2] Creating SOAR game account...');
  const gameKeypair = web3.Keypair.generate();
  const nftMeta = web3.Keypair.generate().publicKey; // placeholder NFT meta

  const { transaction: gameTx } = await client.initializeNewGame(
    gameKeypair.publicKey,
    'OASIS Omniverse DOOM',
    'Cross-game metaverse DOOM powered by OASIS STAR API on Solana',
    Genre.Action,
    GameType.Web,
    nftMeta,
    [authKeypair.publicKey]
  );

  const gameSig = await web3.sendAndConfirmTransaction(
    connection,
    gameTx,
    [authKeypair, gameKeypair]
  );

  console.log('  Game account:   ', gameKeypair.publicKey.toBase58());
  console.log('  Transaction:    ', gameSig);

  // ── Step 2: Create leaderboard ───────────────────────────────────────────
  console.log('\n[2/2] Creating SOAR leaderboard...');
  const leaderboardNft = web3.Keypair.generate().publicKey;

  const { transaction: lbTx, newLeaderBoard } = await client.addNewGameLeaderBoard(
    gameKeypair.publicKey,
    authKeypair.publicKey,
    'OASIS DOOM Leaderboard',
    leaderboardNft,
    100,    // max entries
    false   // isAscending = false → high score wins
  );

  const lbSig = await web3.sendAndConfirmTransaction(
    connection,
    lbTx,
    [authKeypair]
  );

  console.log('  Leaderboard PDA:', newLeaderBoard.toBase58());
  console.log('  Transaction:    ', lbSig);

  // ── Done: print .env values ──────────────────────────────────────────────
  console.log('\n✅ Setup complete! Add these to your .env file:\n');
  console.log('SOAR_GAME_ADDRESS=' + gameKeypair.publicKey.toBase58());
  console.log('SOAR_LEADERBOARD_PDA=' + newLeaderBoard.toBase58());
  console.log('\nKeep SOAR_AUTH_KEYPAIR set to the same value you used here.');
}

main().catch(err => {
  console.error('Setup failed:', err && err.message ? err.message : err);
  process.exit(1);
});
