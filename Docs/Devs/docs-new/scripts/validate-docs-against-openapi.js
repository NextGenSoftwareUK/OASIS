#!/usr/bin/env node
/**
 * Validates that documented paths still exist in the OpenAPI spec.
 * Use in CI to fail when docs reference removed or wrong endpoints.
 * Usage: node scripts/validate-docs-against-openapi.js [specUrl]
 * Reads paths to validate from scripts/documented-paths.txt (one path per line, # = comment).
 */

const https = require('https');
const http = require('http');
const fs = require('fs');
const path = require('path');

const DEFAULT_SPEC_URL = 'http://api.oasisweb4.com/swagger/v1/swagger.json';
const DOCUMENTED_PATHS_FILE = path.join(__dirname, 'documented-paths.txt');

function fetchUrl(url) {
  return new Promise((resolve, reject) => {
    const protocol = url.startsWith('https') ? https : http;
    protocol.get(url, { timeout: 15000 }, (res) => {
      const chunks = [];
      res.on('data', (chunk) => chunks.push(chunk));
      res.on('end', () => resolve(Buffer.concat(chunks).toString('utf8')));
    }).on('error', reject);
  });
}

function loadSpec(specUrl) {
  if (specUrl.startsWith('http://') || specUrl.startsWith('https://')) {
    return fetchUrl(specUrl).then((body) => JSON.parse(body));
  }
  const body = fs.readFileSync(path.resolve(specUrl), 'utf8');
  return Promise.resolve(JSON.parse(body));
}

function loadDocumentedPaths() {
  if (!fs.existsSync(DOCUMENTED_PATHS_FILE)) {
    console.warn('No', DOCUMENTED_PATHS_FILE, 'found; create it with one path per line to validate.');
    return [];
  }
  const content = fs.readFileSync(DOCUMENTED_PATHS_FILE, 'utf8');
  return content
    .split('\n')
    .map((line) => line.replace(/#.*/, '').trim())
    .filter(Boolean);
}

function normalizePath(p) {
  return p.toLowerCase().replace(/\/+$/, '');
}

function specPathMatches(specPaths, documentedPath) {
  const docNorm = normalizePath(documentedPath);
  // Exact match (case-insensitive)
  for (const specPath of specPaths) {
    if (normalizePath(specPath) === docNorm) return true;
  }
  // Prefix match: documented path may be a prefix (e.g. /api/avatar) and spec has /api/Avatar/get-by-id/{id}
  for (const specPath of specPaths) {
    const specNorm = normalizePath(specPath);
    if (specNorm === docNorm || specNorm.startsWith(docNorm + '/')) return true;
  }
  return false;
}

const specUrl = process.argv[2] || DEFAULT_SPEC_URL;

Promise.all([
  loadSpec(specUrl).then((spec) => new Set(Object.keys(spec.paths || {}))),
  Promise.resolve(loadDocumentedPaths())
])
  .then(([specPaths, documentedPaths]) => {
    if (documentedPaths.length === 0) {
      console.log('No documented paths to validate; skipping.');
      process.exit(0);
    }
    const missing = documentedPaths.filter((p) => !specPathMatches(specPaths, p));
    if (missing.length > 0) {
      console.error('Documented paths not found (or no matching path) in OpenAPI spec:');
      missing.forEach((p) => console.error('  -', p));
      console.error('Update docs or add the path to the API. Spec URL:', specUrl);
      process.exit(1);
    }
    console.log('All', documentedPaths.length, 'documented path(s) exist in the spec.');
    process.exit(0);
  })
  .catch((err) => {
    console.error('Error:', err.message);
    process.exit(1);
  });
