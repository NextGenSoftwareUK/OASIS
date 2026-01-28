#!/usr/bin/env node
/**
 * Generates reference/openapi-endpoint-inventory.md from the live OpenAPI spec.
 * Keeps the endpoint list in docs in sync with the API.
 * Usage: node scripts/generate-openapi-inventory.js [specUrl]
 * Default specUrl: http://api.oasisweb4.com/swagger/v1/swagger.json
 */

const https = require('https');
const http = require('http');
const fs = require('fs');
const path = require('path');

const DEFAULT_SPEC_URL = 'http://api.oasisweb4.com/swagger/v1/swagger.json';
const OUTPUT_PATH = path.join(__dirname, '..', 'reference', 'openapi-endpoint-inventory.md');

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

function loadSpec(specUrlOrPath) {
  if (specUrlOrPath && (specUrlOrPath.startsWith('http://') || specUrlOrPath.startsWith('https://'))) {
    return fetchUrl(specUrlOrPath).then((body) => JSON.parse(body));
  }
  const filePath = specUrlOrPath ? path.resolve(specUrlOrPath) : null;
  if (filePath && fs.existsSync(filePath)) {
    const body = fs.readFileSync(filePath, 'utf8');
    return Promise.resolve(JSON.parse(body));
  }
  if (filePath) return Promise.reject(new Error('Spec file not found: ' + filePath));
  return Promise.reject(new Error('Provide spec URL or path to swagger.json'));
}

function collectEndpoints(spec) {
  const paths = spec.paths || {};
  const list = [];
  for (const [pathKey, pathItem] of Object.entries(paths)) {
    if (typeof pathItem !== 'object' || pathItem === null) continue;
    for (const method of ['get', 'post', 'put', 'delete', 'patch', 'head', 'options']) {
      if (pathItem[method]) {
        const op = pathItem[method];
        const tags = Array.isArray(op.tags) ? op.tags : [];
        const summary = op.summary || op.operationId || '';
        list.push({ path: pathKey, method: method.toUpperCase(), tags, summary });
      }
    }
  }
  return list.sort((a, b) => (a.path + a.method).localeCompare(b.path + b.method));
}

function groupByTag(list) {
  const byTag = {};
  for (const item of list) {
    const tag = item.tags[0] || 'Other';
    if (!byTag[tag]) byTag[tag] = [];
    byTag[tag].push(item);
  }
  return byTag;
}

function toMarkdown(spec, endpoints) {
  const byTag = groupByTag(endpoints);
  const lines = [
    '# OpenAPI Endpoint Inventory',
    '',
    'This file is **generated** from the live OpenAPI spec. Do not edit by hand.',
    '',
    '**Source:** [OpenAPI spec](http://api.oasisweb4.com/swagger/v1/swagger.json)',
    '**Swagger UI:** [Interactive docs](http://api.oasisweb4.com/swagger/index.html)',
    '',
    `**Generated:** ${new Date().toISOString()}`,
    `**Total endpoints:** ${endpoints.length}`,
    '',
    '---',
    ''
  ];

  const tags = Object.keys(byTag).sort();
  for (const tag of tags) {
    lines.push(`## ${tag}`);
    lines.push('');
    lines.push('| Method | Path | Summary |');
    lines.push('|--------|------|---------|');
    for (const item of byTag[tag]) {
      const summary = (item.summary || '').slice(0, 80).replace(/\|/g, '\\|').replace(/\n/g, ' ');
      lines.push(`| ${item.method} | \`${item.path}\` | ${summary} |`);
    }
    lines.push('');
  }

  return lines.join('\n');
}

const specUrl = process.argv[2] || DEFAULT_SPEC_URL;

loadSpec(specUrl)
  .then((spec) => {
    const endpoints = collectEndpoints(spec);
    const markdown = toMarkdown(spec, endpoints);
    const outDir = path.dirname(OUTPUT_PATH);
    if (!fs.existsSync(outDir)) fs.mkdirSync(outDir, { recursive: true });
    fs.writeFileSync(OUTPUT_PATH, markdown, 'utf8');
    console.log('Wrote', OUTPUT_PATH, '(', endpoints.length, 'endpoints )');
  })
  .catch((err) => {
    console.error('Error:', err.message);
    process.exit(1);
  });
