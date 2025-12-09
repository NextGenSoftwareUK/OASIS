#!/usr/bin/env node

const fs = require('fs');
const path = require('path');

console.log('🔍 Validating OASIS Portal...\n');

const portalHtml = path.join(__dirname, 'portal.html');

if (!fs.existsSync(portalHtml)) {
  console.error('❌ Error: portal.html not found');
  process.exit(1);
}

// Read and check portal.html
const content = fs.readFileSync(portalHtml, 'utf8');

let errors = [];
let warnings = [];

// Check for required elements
const checks = [
  {
    name: 'HTML structure',
    test: /<!DOCTYPE html>/i,
    error: 'Missing DOCTYPE declaration'
  },
  {
    name: 'Title tag',
    test: /<title>.*<\/title>/i,
    error: 'Missing title tag'
  },
  {
    name: 'Stylesheet link',
    test: /<link.*rel=["']stylesheet["']/i,
    error: 'Missing stylesheet link'
  },
  {
    name: 'Portal navigation',
    test: /portal-nav/i,
    error: 'Missing portal navigation'
  },
  {
    name: 'Portal tabs',
    test: /portal-tabs/i,
    error: 'Missing portal tabs'
  },
  {
    name: 'Dashboard tab',
    test: /data-tab=["']dashboard["']/i,
    error: 'Missing dashboard tab'
  }
];

checks.forEach(check => {
  if (!check.test.test(content)) {
    errors.push(`❌ ${check.name}: ${check.error}`);
  } else {
    console.log(`✅ ${check.name}: OK`);
  }
});

// Check for external dependencies
const externalChecks = [
  {
    name: 'Styles.css reference',
    test: /href=["']\.\.\/oasisweb4 site\/new-v2\/styles\.css["']/,
    error: 'Styles.css path may be incorrect'
  },
  {
    name: 'Script.js reference',
    test: /src=["']\.\.\/oasisweb4 site\/new-v2\/script\.js["']/,
    error: 'Script.js path may be incorrect'
  }
];

externalChecks.forEach(check => {
  if (!check.test.test(content)) {
    warnings.push(`⚠️  ${check.name}: ${check.error}`);
  } else {
    console.log(`✅ ${check.name}: OK`);
  }
});

// Check file size
const stats = fs.statSync(portalHtml);
const fileSizeKB = (stats.size / 1024).toFixed(2);
console.log(`\n📊 File size: ${fileSizeKB} KB`);

if (stats.size > 500 * 1024) {
  warnings.push('⚠️  File size is large (>500KB), consider optimization');
}

// Summary
console.log('\n' + '='.repeat(50));
if (errors.length === 0 && warnings.length === 0) {
  console.log('✅ Validation passed! All checks OK.');
  process.exit(0);
} else {
  if (errors.length > 0) {
    console.log('\n❌ Errors found:');
    errors.forEach(error => console.log(`  ${error}`));
  }
  if (warnings.length > 0) {
    console.log('\n⚠️  Warnings:');
    warnings.forEach(warning => console.log(`  ${warning}`));
  }
  process.exit(errors.length > 0 ? 1 : 0);
}

