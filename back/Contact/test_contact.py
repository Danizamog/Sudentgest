import requests
import json
from datetime import datetime

# Configuración
BASE_URL = "http://localhost:5013"

def test_health_check():
    """Test del health check"""
    response = requests.get(f"{BASE_URL}/health")
    print("Health Check:", response.json())

def test_get_tenants():
    """Test para obtener tenants soportados"""
    response = requests.get(f"{BASE_URL}/api/contact/tenants")
    print("Tenants soportados:", json.dumps(response.json(), indent=2))

def test_send_contact_message():
    """Test para enviar mensaje de contacto"""
    message = {
        "nombre": "Juan Pérez",
        "email": "juan.perez@ucb.edu.bo",
        "mensaje": "Necesito información sobre el sistema de gestión de estudiantes. ¿Podrían proporcionarme más detalles sobre las funcionalidades disponibles?",
        "asunto": "Consulta sobre StudentGest",
        "telefono": "+591 70123456"
    }
    
    response = requests.post(f"{BASE_URL}/api/contact/send", json=message)
    print("Envío de mensaje:", json.dumps(response.json(), indent=2))

def test_send_gmail_message():
    """Test para enviar mensaje con email de Gmail"""
    message = {
        "nombre": "María García",
        "email": "maria.garcia@gmail.com",
        "mensaje": "Hola, me interesa conocer más sobre la plataforma StudentGest y sus características.",
        "asunto": "Información sobre la plataforma"
    }
    
    response = requests.post(f"{BASE_URL}/api/contact/send", json=message)
    print("Mensaje Gmail:", json.dumps(response.json(), indent=2))

def test_unsupported_domain():
    """Test para dominio no soportado"""
    message = {
        "nombre": "Pedro López",
        "email": "pedro@ejemplo.com",
        "mensaje": "Test de dominio no soportado",
        "asunto": "Test"
    }
    
    response = requests.post(f"{BASE_URL}/api/contact/send", json=message)
    print("Dominio no soportado:", response.status_code, response.json() if response.headers.get('content-type', '').startswith('application/json') else response.text)

if __name__ == "__main__":
    print("=== Testing Contact Service ===")
    print(f"Fecha/Hora: {datetime.now()}")
    print()
    
    try:
        print("1. Testing Health Check...")
        test_health_check()
        print()
        
        print("2. Testing Get Tenants...")
        test_get_tenants()
        print()
        
        print("3. Testing Send Contact Message (UCB)...")
        test_send_contact_message()
        print()
        
        print("4. Testing Send Gmail Message...")
        test_send_gmail_message()
        print()
        
        print("5. Testing Unsupported Domain...")
        test_unsupported_domain()
        print()
        
    except requests.exceptions.ConnectionError:
        print("❌ Error: No se pudo conectar al servicio. Asegúrate de que esté ejecutándose en el puerto 5013")
    except Exception as e:
        print(f"❌ Error durante las pruebas: {e}")