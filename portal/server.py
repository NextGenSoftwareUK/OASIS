#!/usr/bin/env python3
import http.server
import socketserver
import os

class MyHTTPRequestHandler(http.server.SimpleHTTPRequestHandler):
    def do_GET(self):
        # Handle /portal without extension
        print(f"DEBUG: Request path: {self.path}")  # Debug output
        if self.path == '/portal' or self.path == '/portal/':
            try:
                # Server runs from OASIS_CLEAN, file is at portal/portal
                script_dir = os.path.dirname(os.path.abspath(__file__))
                base_dir = os.path.dirname(script_dir)
                file_path = os.path.join(base_dir, 'portal', 'portal')
                print(f"DEBUG: Looking for file at: {file_path}")  # Debug output
                print(f"DEBUG: File exists: {os.path.exists(file_path)}")  # Debug output
                
                if os.path.exists(file_path):
                    with open(file_path, 'rb') as f:
                        content = f.read()
                        self.send_response(200)
                        self.send_header('Content-type', 'text/html; charset=utf-8')
                        self.end_headers()
                        self.wfile.write(content)
                    print("DEBUG: File served successfully")  # Debug output
                    return
                else:
                    print(f"DEBUG: File not found at: {file_path}")  # Debug output
                    self.send_error(404, f"File not found at: {file_path}")
                    return
            except Exception as e:
                print(f"DEBUG: Exception: {e}")  # Debug output
                import traceback
                traceback.print_exc()
                self.send_error(500, f"Error: {e}")
                return
        
        # Default behavior for other files
        print(f"DEBUG: Using default handler for: {self.path}")  # Debug output
        return super().do_GET()

PORT = 8080

# Change to OASIS_CLEAN directory
os.chdir(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

with socketserver.TCPServer(("", PORT), MyHTTPRequestHandler) as httpd:
    print(f"🚀 Server running at http://localhost:{PORT}/")
    print(f"📍 Portal: http://localhost:{PORT}/portal")
    httpd.serve_forever()

