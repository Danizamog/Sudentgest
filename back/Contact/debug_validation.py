import requests
import json

# Test the exact payload that might be causing 422 error
BASE_URL = "http://localhost:5013"

def test_simple_payload():
    """Test with a simple valid payload"""
    payload = {
        "nombre": "Test User",
        "email": "test@ucb.edu.bo",
        "mensaje": "This is a test message for validation"
    }
    
    print("Testing simple payload:")
    print(json.dumps(payload, indent=2))
    
    try:
        # Test validation endpoint first
        response = requests.post(f"{BASE_URL}/api/contact/test", json=payload)
        print(f"Validation test status: {response.status_code}")
        print(f"Validation response: {response.json()}")
        print()
        
        # Test actual send endpoint
        response = requests.post(f"{BASE_URL}/api/contact/send", json=payload)
        print(f"Send status: {response.status_code}")
        if response.status_code == 422:
            print(f"Validation errors: {response.json()}")
        else:
            print(f"Send response: {response.json()}")
            
    except Exception as e:
        print(f"Error: {e}")

def test_complete_payload():
    """Test with complete payload"""
    payload = {
        "nombre": "Juan Pérez",
        "email": "juan.perez@gmail.com",
        "mensaje": "Este es un mensaje de prueba completo con todos los campos necesarios.",
        "asunto": "Prueba completa",
        "telefono": "+591 70123456"
    }
    
    print("\nTesting complete payload:")
    print(json.dumps(payload, indent=2))
    
    try:
        response = requests.post(f"{BASE_URL}/api/contact/send", json=payload)
        print(f"Status: {response.status_code}")
        if response.status_code == 422:
            print(f"Validation errors: {response.json()}")
        else:
            print(f"Response: {response.json()}")
            
    except Exception as e:
        print(f"Error: {e}")

def test_edge_cases():
    """Test edge cases that might cause validation errors"""
    test_cases = [
        {
            "name": "Empty message",
            "payload": {
                "nombre": "Test",
                "email": "test@ucb.edu.bo",
                "mensaje": ""
            }
        },
        {
            "name": "Short message",
            "payload": {
                "nombre": "Test", 
                "email": "test@ucb.edu.bo",
                "mensaje": "Hi"
            }
        },
        {
            "name": "Invalid email",
            "payload": {
                "nombre": "Test",
                "email": "invalid-email",
                "mensaje": "This is a test message"
            }
        },
        {
            "name": "Missing required fields",
            "payload": {
                "nombre": "Test"
            }
        }
    ]
    
    for test_case in test_cases:
        print(f"\n--- Testing: {test_case['name']} ---")
        try:
            response = requests.post(f"{BASE_URL}/api/contact/test", json=test_case['payload'])
            print(f"Status: {response.status_code}")
            print(f"Response: {response.json()}")
        except Exception as e:
            print(f"Error: {e}")

if __name__ == "__main__":
    print("=== Contact Service Validation Tests ===")
    
    # Test health first
    try:
        response = requests.get(f"{BASE_URL}/health")
        print(f"Health check: {response.status_code} - {response.json()}")
    except:
        print("❌ Service not running or not accessible")
        exit(1)
    
    test_simple_payload()
    test_complete_payload()
    test_edge_cases()