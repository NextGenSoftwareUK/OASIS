#!/usr/bin/env node
/**
 * Repeat NFT image generation using Glif.app (same as MCP glif_generate_image).
 * Run from MCP directory: npx tsx glif-generate-image.ts [prompt]
 *
 * Chat context: generate image with Glif → preview → then mint as NFT.
 * Default prompt matches docs: "A futuristic Gundam robot in cyberpunk style"
 */

import fs from 'fs';
import path from 'path';
import axios from 'axios';
import { OASISClient } from './src/clients/oasisClient.js';
import { config } from './src/config.js';

const DEFAULT_PROMPT = 'A futuristic Gundam robot in cyberpunk style, neo-tokyo, neon lights, highly detailed';
const SAVE_DIR = path.join(process.cwd(), 'NFT_Content');

async function main() {
  const prompt = process.argv.slice(2).join(' ') || DEFAULT_PROMPT;

  if (!config.glifApiToken) {
    console.error('GLIF_API_TOKEN is not set. Add it to .env or set the env var.');
    process.exit(1);
  }

  console.log('Generating image with Glif.app...');
  console.log('Prompt:', prompt);

  const client = new OASISClient();
  const result = await client.generateImageWithGlif({ prompt });

  if (result.error) {
    console.error('Error:', result.error);
    process.exit(1);
  }

  if (!result.imageUrl) {
    console.error('No image URL in response');
    process.exit(1);
  }

  if (!fs.existsSync(SAVE_DIR)) {
    fs.mkdirSync(SAVE_DIR, { recursive: true });
  }

  const filename = `generated-${Date.now()}.png`;
  const savePath = path.join(SAVE_DIR, filename);

  const imageResponse = await axios.get(result.imageUrl, {
    responseType: 'arraybuffer',
    timeout: 30000,
  });
  fs.writeFileSync(savePath, imageResponse.data);

  console.log('Image saved to:', savePath);
  console.log('Image URL:', result.imageUrl);
  console.log('\nYou can preview it, then mint with:');
  console.log(`  Mint a Solana NFT using the image at ${savePath}`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
