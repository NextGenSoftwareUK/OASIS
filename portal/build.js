#!/usr/bin/env node

const fs = require('fs');
const path = require('path');

console.log('🚀 Building OASIS Portal...\n');

// Create dist directory
const distDir = path.join(__dirname, 'dist');
if (!fs.existsSync(distDir)) {
  fs.mkdirSync(distDir, { recursive: true });
  console.log('✅ Created dist directory');
}

// Copy portal.html to dist
const portalHtml = path.join(__dirname, 'portal.html');
const distHtml = path.join(distDir, 'portal.html');

if (fs.existsSync(portalHtml)) {
  fs.copyFileSync(portalHtml, distHtml);
  console.log('✅ Copied portal.html to dist/');
} else {
  console.error('❌ Error: portal.html not found');
  process.exit(1);
}

// Create a simple index.html that redirects to portal.html
const indexHtml = `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta http-equiv="refresh" content="0; url=portal.html">
    <title>OASIS Portal</title>
    <script>window.location.href = 'portal.html';</script>
</head>
<body>
    <p>Redirecting to <a href="portal.html">portal.html</a>...</p>
</body>
</html>`;

fs.writeFileSync(path.join(distDir, 'index.html'), indexHtml);
console.log('✅ Created index.html redirect');

// Create README for dist
const readme = `# OASIS Portal - Build Output

This directory contains the built portal files.

## Files
- \`portal.html\` - Main portal page
- \`index.html\` - Redirect to portal.html

## Deployment

### Static Hosting
You can deploy this directory to any static hosting service:
- **Vercel**: \`vercel --prod\`
- **Netlify**: \`netlify deploy --prod\`
- **GitHub Pages**: Push to gh-pages branch
- **AWS S3 + CloudFront**: Upload to S3 bucket

### Local Testing
\`\`\`bash
cd dist
python3 -m http.server 8080
\`\`\`

Then visit: http://localhost:8080/portal.html
`;

fs.writeFileSync(path.join(distDir, 'README.md'), readme);
console.log('✅ Created README.md');

// Create .gitignore for dist
const gitignore = `# Build output
dist/
node_modules/
*.log
.DS_Store
`;

if (!fs.existsSync(path.join(__dirname, '.gitignore'))) {
  fs.writeFileSync(path.join(__dirname, '.gitignore'), gitignore);
  console.log('✅ Created .gitignore');
}

console.log('\n✅ Build completed successfully!');
console.log('📁 Build output is in the \'dist\' directory');
console.log('\n📋 Next steps:');
console.log('   - Test locally: cd dist && python3 -m http.server 8080');
console.log('   - Deploy to Vercel: vercel --prod');
console.log('   - Deploy to Netlify: netlify deploy --prod');

