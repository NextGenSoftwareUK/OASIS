#!/usr/bin/env python3
"""
Custom HTTP server that serves .html files without requiring the .html extension.
"""
import http.server
import socketserver
import os
import urllib.parse

class ExtensionlessHandler(http.server.SimpleHTTPRequestHandler):
    def do_GET(self):
        # Parse the path
        parsed_path = urllib.parse.urlparse(self.path)
        original_path = parsed_path.path
        path = original_path.lstrip('/')
        
        # If path is empty, serve index.html
        if path == '' or path == 'index.html':
            path = 'index.html'
        # If path ends with /, look for index.html in that directory
        elif path.endswith('/'):
            path = path.rstrip('/') + '/index.html'
        # If path doesn't have an extension and file doesn't exist, try adding .html
        elif not os.path.exists(path):
            # Check if it's a directory
            if os.path.isdir(path):
                path = os.path.join(path, 'index.html')
            # Check if .html version exists
            elif not '.' in os.path.basename(path):
                html_path = path + '.html'
                if os.path.exists(html_path) and os.path.isfile(html_path):
                    path = html_path
        
        # Update the path for the parent handler
        self.path = '/' + path
        
        # Call parent handler
        return super().do_GET()
    
    def log_message(self, format, *args):
        # Suppress default logging, or customize it
        pass

if __name__ == '__main__':
    PORT = 8082
    
    with socketserver.TCPServer(("", PORT), ExtensionlessHandler) as httpd:
        print(f"Server running on port {PORT}")
        print(f"Open http://localhost:{PORT} in your browser")
        print("Press Ctrl+C to stop the server")
        httpd.serve_forever()

