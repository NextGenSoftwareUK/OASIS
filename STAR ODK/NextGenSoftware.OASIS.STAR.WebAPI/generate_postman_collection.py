#!/usr/bin/env python3
"""
Script to generate comprehensive Postman collection for STAR API
Extracts all endpoints from controllers and creates complete collection with examples
"""

import json
import os
import re
from pathlib import Path

def extract_endpoints_from_controller(file_path):
    """Extract HTTP endpoints from a controller file"""
    endpoints = []
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Extract route
    route_match = re.search(r'\[Route\("([^"]+)"\)\]', content)
    base_route = route_match.group(1) if route_match else ""
    
    # Extract all HTTP method attributes
    http_methods = re.finditer(r'\[Http(Get|Post|Put|Delete|Patch)\(([^)]*)\)\]', content)
    
    for match in http_methods:
        method = match.group(1).upper()
        route_param = match.group(2).strip('"')
        
        # Get method name
        method_name_match = re.search(rf'{match.group(0)}\s+.*?public.*?(\w+)\s*\(', content[match.end():match.end()+500])
        method_name = method_name_match.group(1) if method_name_match else "Unknown"
        
        # Build full route
        if route_param:
            full_route = f"{base_route}/{route_param}".replace("//", "/")
        else:
            full_route = base_route
        
        endpoints.append({
            'method': method,
            'route': full_route,
            'name': method_name
        })
    
    return endpoints

def create_postman_request(name, method, url, auth=True, body=None, query_params=None):
    """Create a Postman request object"""
    request = {
        "name": name,
        "request": {
            "method": method,
            "header": [],
            "url": {
                "raw": f"{{{{host}}}}{url}",
                "host": ["{{host}}"],
                "path": [p for p in url.split("/") if p]
            }
        },
        "response": []
    }
    
    if auth:
        request["request"]["auth"] = {
            "type": "bearer",
            "bearer": [{
                "key": "token",
                "value": "{{token}}",
                "type": "string"
            }]
        }
    
    if query_params:
        request["request"]["url"]["query"] = query_params
        query_string = "&".join([f"{q['key']}={q['value']}" for q in query_params])
        request["request"]["url"]["raw"] = f"{{{{host}}}}{url}?{query_string}"
    
    if body:
        request["request"]["header"].append({
            "key": "Content-Type",
            "value": "application/json"
        })
        request["request"]["body"] = {
            "mode": "raw",
            "raw": json.dumps(body, indent=2),
            "options": {
                "raw": {
                    "language": "json"
                }
            }
        }
    
    return request

def main():
    controllers_dir = Path("Controllers")
    collection = {
        "info": {
            "_postman_id": "star-api-collection-2024-complete",
            "name": "STAR API v5.0.0 - Complete",
            "description": "Complete STAR API collection with ALL endpoints and examples from all 28 controllers",
            "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
        },
        "item": [],
        "variable": [
            {
                "key": "host",
                "value": "http://localhost:5001",
                "type": "string"
            },
            {
                "key": "token",
                "value": "",
                "type": "string"
            }
        ]
    }
    
    # Controller groups
    controller_groups = {}
    
    for controller_file in sorted(controllers_dir.glob("*Controller.cs")):
        controller_name = controller_file.stem.replace("Controller", "")
        endpoints = extract_endpoints_from_controller(controller_file)
        
        if endpoints:
            controller_groups[controller_name] = endpoints
    
    # Add endpoints to collection
    for controller_name, endpoints in sorted(controller_groups.items()):
        group = {
            "name": controller_name,
            "item": []
        }
        
        for endpoint in endpoints:
            # Create request based on endpoint
            url = endpoint['route']
            method = endpoint['method']
            name = endpoint['name']
            
            # Determine if auth is needed (most endpoints need it except health)
            auth_needed = controller_name != "Health"
            
            # Create example body for POST/PUT
            body = None
            if method in ["POST", "PUT"]:
                body = {"Name": f"Example {controller_name}", "Description": "Example description"}
            
            request = create_postman_request(name, method, url, auth_needed, body)
            group["item"].append(request)
        
        collection["item"].append(group)
    
    # Write collection
    output_file = "STAR_API.postman_collection.json"
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(collection, f, indent=2, ensure_ascii=False)
    
    print(f"Generated comprehensive Postman collection: {output_file}")
    print(f"Total controllers: {len(controller_groups)}")
    print(f"Total endpoints: {sum(len(e) for e in controller_groups.values())}")

if __name__ == "__main__":
    main()

