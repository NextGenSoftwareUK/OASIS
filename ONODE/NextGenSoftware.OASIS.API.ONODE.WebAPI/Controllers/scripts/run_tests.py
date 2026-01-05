import json
import urllib.request
import urllib.error
import os
import glob

# Configuration
API_BASE_URL = "http://localhost:5002/api"
TEST_RESULTS_DIR = os.path.join(os.path.dirname(__file__), '..', 'ApiTestResults')
TEST_RESULTS_DIR = os.path.normpath(TEST_RESULTS_DIR)

def get_admin_auth_headers():
    print("[*] Authenticating Super Admin...")
    url = f"{API_BASE_URL}/Avatar/Authenticate"
    payload = {
        "Username": "super_admin",
        "Password": "password"
    }

    try:
        data = json.dumps(payload).encode('utf-8')
        headers = {'Content-Type': 'application/json'}
        req = urllib.request.Request(url, data=data, method='POST', headers=headers)
        
        with urllib.request.urlopen(req) as response:
            if response.getcode() == 200:
                print("[+] Admin Authentication successful.")
                response_body = response.read().decode('utf-8')
                json_response = json.loads(response_body)
                
                auth_headers = {'Content-Type': 'application/json'}
                
                # 1. Try body (various casings) for Bearer token
                result_obj = json_response.get("Result") or json_response.get("result") or {}
                jwt_token = (
                    result_obj.get("JwtToken") or 
                    result_obj.get("jwtToken") or 
                    result_obj.get("jwt_token") or
                    result_obj.get("token")
                )

                if jwt_token:
                    print(f"[+] Bearer Token found: {jwt_token[:20]}...")
                    auth_headers['Authorization'] = f'Bearer {jwt_token}'

                # 2. Try cookies (refreshToken)
                set_cookie_header = response.getheader('Set-Cookie')
                if set_cookie_header:
                    print(f"[*] Found Set-Cookie header: {set_cookie_header}")
                    auth_headers['Cookie'] = set_cookie_header 
                    # Note: We can just pass back the full Set-Cookie string as 'Cookie' or parse it. 
                    # Passing it back directly often works if it's a single cookie, 
                    # but technically we should strip 'Path', 'Expires' etc.
                    # Let's clean it up to be safe.
                    import http.cookies
                    cookie = http.cookies.SimpleCookie()
                    cookie.load(set_cookie_header)
                    if 'refreshToken' in cookie:
                         cleaned_cookie = f"refreshToken={cookie['refreshToken'].value}"
                         auth_headers['Cookie'] = cleaned_cookie
                         print(f"[+] Cookie set for requests: {cleaned_cookie[:20]}...")

                if 'Authorization' in auth_headers or 'Cookie' in auth_headers:
                    return auth_headers
                else:
                    print("[-] Token NOT found in response body or cookies.")
                    print(f"[*] Response Headers: {response.info()}")
                    print(f"[*] Response Body: {response_body}")
                    return None
            else:
                print(f"[-] Admin Authentication returned HTTP {response.getcode()}")
                try:
                    print(f"[-] Response Body: {response.read().decode('utf-8')}")
                except:
                    pass
                return None
                
    except Exception as e:
        print(f"[-] Admin Authentication exception: {e}")
        return None

def run_tests():
    auth_headers = get_admin_auth_headers()
    if not auth_headers:
        print("[-] Cannot proceed without authentication.")
        return

    print(f"[*] Starting regression tests walking through {TEST_RESULTS_DIR}...")
    
    pass_count = 0
    fail_count = 0

    # Walk through all directories in ApiTestResults
    for root, dirs, files in os.walk(TEST_RESULTS_DIR):
        if "request.json" in files:
            # Construct API endpoint from path
            # Path structure: .../ApiTestResults/ControllerName/ActionName/request.json
            rel_path = os.path.relpath(root, TEST_RESULTS_DIR)
            path_parts = rel_path.split(os.sep)
            
            if len(path_parts) >= 2:
                controller_dir = path_parts[0]
                action_name = path_parts[1]
                
                # Strip 'Controller' suffix from controller name for URL
                if controller_dir.endswith("Controller"):
                    controller_name = controller_dir[:-10]
                else:
                    controller_name = controller_dir
                    
                url = f"{API_BASE_URL}/{controller_name}/{action_name}"
                
                # Determine HTTP Method
                method = "POST" # Default
                method_file = os.path.join(root, "method.json")
                if os.path.exists(method_file):
                    try:
                        with open(method_file, 'r') as f:
                            method_data = json.load(f)
                            if isinstance(method_data, dict):
                                method = method_data.get("method", "POST").upper()
                            elif isinstance(method_data, str):
                                method = method_data.upper()
                    except:
                        pass # Default to POST
                
                print(f"[*] Testing {method} {url}...")
                
                # Read Request Body
                request_file = os.path.join(root, "request.json")
                try:
                    with open(request_file, 'r') as f:
                        payload = json.load(f)
                except Exception as e:
                    print(f"    [-] Error reading request.json: {e}")
                    continue

                # Execute Request
                try:
                    data = json.dumps(payload).encode('utf-8')
                    
                    # Merge auth headers with request headers
                    req_headers = auth_headers.copy()
                    
                    req = urllib.request.Request(url, data=data, method=method, headers=req_headers)
                    
                    with urllib.request.urlopen(req) as response:
                        status_code = response.getcode()
                        response_body = response.read().decode('utf-8')
                        
                        # Write response to file
                        with open(os.path.join(root, "response.json"), "w") as f:
                            try:
                                json_resp = json.loads(response_body)
                                json.dump(json_resp, f, indent=2)
                            except:
                                f.write(response_body)

                        if 200 <= status_code < 300:
                            print(f"    [+] PASS (HTTP {status_code})")
                            with open(os.path.join(root, "test_report.txt"), "w") as f:
                                f.write(f"PASS: HTTP {status_code}\nDate: {os.path.getmtime(request_file)}")
                            pass_count += 1
                        else:
                            print(f"    [-] FAIL (HTTP {status_code})")
                            with open(os.path.join(root, "test_report.txt"), "w") as f:
                                f.write(f"FAIL: HTTP {status_code}\n")
                            fail_count += 1

                except urllib.error.HTTPError as e:
                    print(f"    [-] FAIL (HTTP {e.code})")
                    error_body = e.read().decode('utf-8')
                    with open(os.path.join(root, "error.log"), "w") as f:
                        f.write(f"HTTP {e.code}\n{error_body}")
                    with open(os.path.join(root, "test_report.txt"), "w") as f:
                        f.write(f"FAIL: HTTP {e.code}\n")
                    fail_count += 1
                except Exception as e:
                    print(f"    [-] EXCEPTION: {e}")
                    with open(os.path.join(root, "error.log"), "w") as f:
                        f.write(str(e))
                    with open(os.path.join(root, "test_report.txt"), "w") as f:
                        f.write(f"FAIL: Exception {e}\n")
                    fail_count += 1

    print("-" * 30)
    print(f"Test Run Complete.")
    print(f"Passed: {pass_count}")
    print(f"Failed: {fail_count}")

if __name__ == "__main__":
    run_tests()
