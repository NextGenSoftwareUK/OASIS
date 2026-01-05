import json
import urllib.request
import urllib.error
import uuid
import random

def run_registration_test():
    url = "http://localhost:5002/api/Avatar/register"
    
    # Generate random user data
    random_id = str(uuid.uuid4())[:8]
    username = f"test_user_{random_id}"
    email = f"test_{random_id}@oasisplatform.world"
    
    payload = {
        "Title": "Mr",
        "FirstName": "Test",
        "LastName": "User",
        "Email": email,
        "Username": username,
        "Password": "password",
        "ConfirmPassword": "password",
        "AvatarType": "User",
        "AcceptTerms": True
    }
            
    print("[*] Registering random user with payload:")
    print(json.dumps(payload, indent=2))
    
    try:
        data = json.dumps(payload).encode('utf-8')
        headers = {'Content-Type': 'application/json'}
        req = urllib.request.Request(url, data=data, method='POST', headers=headers)
        
        print(f"[*] Sending POST request to {url}...")
        
        with urllib.request.urlopen(req) as response:
            status_code = response.getcode()
            response_body = response.read().decode('utf-8')
            
            if status_code == 200:
                print(f"[+] Registration successful (HTTP {status_code}).")
                print(f"[-] Response: {response_body}")
            else:
                print(f"[-] Registration returned HTTP {status_code}")
                print(f"[-] Response: {response_body}")
                
    except urllib.error.HTTPError as e:
        print(f"[-] Registration failed: {e.code}")
        print(f"[-] Error Response: {e.read().decode('utf-8')}")
    except Exception as e:
        print(f"[-] An error occurred: {e}")

if __name__ == "__main__":
    run_registration_test()
