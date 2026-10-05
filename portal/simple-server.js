const http = require('http');
const fs = require('fs');
const path = require('path');

const PORT = 8080;
const BASE_DIR = path.join(__dirname, '..');

const server = http.createServer((req, res) => {
  if (req.url === '/portal' || req.url === '/portal/') {
    const filePath = path.join(BASE_DIR, 'portal', 'portal');
    fs.readFile(filePath, (err, data) => {
      if (err) {
        res.writeHead(404, { 'Content-Type': 'text/plain' });
        res.end('File not found');
        return;
      }
      res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
      res.end(data);
    });
  } else if (req.url === '/styles.css' || req.url === '/script.js') {
    // Serve CSS/JS from portal directory
    const filePath = path.join(BASE_DIR, 'portal', req.url.substring(1));
    fs.readFile(filePath, (err, data) => {
      if (err) {
        res.writeHead(404, { 'Content-Type': 'text/plain' });
        res.end('File not found');
        return;
      }
      const contentType = req.url.endsWith('.css') ? 'text/css' : 'application/javascript';
      res.writeHead(200, { 'Content-Type': contentType });
      res.end(data);
    });
  } else {
    // Serve other files - handle paths with spaces and relative paths
    let filePath = req.url;
    
    // Remove leading slash
    if (filePath.startsWith('/')) {
      filePath = filePath.substring(1);
    }
    
    // Handle relative paths like ../oasisweb4 site/new-v2/styles.css
    if (filePath.startsWith('../')) {
      filePath = filePath.replace(/^\.\.\//, '');
    }
    
    // Decode URL encoding
    filePath = decodeURIComponent(filePath);
    
    // Build full path
    const fullPath = path.join(BASE_DIR, filePath);
    
    // Security check - ensure path is within BASE_DIR
    if (!fullPath.startsWith(BASE_DIR)) {
      res.writeHead(403, { 'Content-Type': 'text/plain' });
      res.end('Forbidden');
      return;
    }
    
    // Determine content type
    let contentType = 'text/plain';
    if (filePath.endsWith('.css')) {
      contentType = 'text/css';
    } else if (filePath.endsWith('.js')) {
      contentType = 'application/javascript';
    } else if (filePath.endsWith('.html')) {
      contentType = 'text/html';
    } else if (filePath.endsWith('.png')) {
      contentType = 'image/png';
    } else if (filePath.endsWith('.jpg') || filePath.endsWith('.jpeg')) {
      contentType = 'image/jpeg';
    } else if (filePath.endsWith('.svg')) {
      contentType = 'image/svg+xml';
    }
    
    fs.readFile(fullPath, (err, data) => {
      if (err) {
        res.writeHead(404, { 'Content-Type': 'text/plain' });
        res.end('File not found: ' + filePath);
        return;
      }
      res.writeHead(200, { 'Content-Type': contentType });
      res.end(data);
    });
  }
});

server.listen(PORT, () => {
  console.log(`🚀 Server running at http://localhost:${PORT}/`);
  console.log(`📍 Portal: http://localhost:${PORT}/portal`);
});




