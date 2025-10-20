from models.contact import ContactMessage

# Simple test
test_data = {
    "nombre": "Test User",
    "email": "test@ucb.edu.bo",
    "mensaje": "Test message"
}

try:
    message = ContactMessage(**test_data)
    print(f"✅ SUCCESS: {message}")
    print(f"Model dump: {message.model_dump()}")
except Exception as e:
    print(f"❌ FAILED: {e}")
    print(f"Error type: {type(e)}")

# Test with empty strings
test_empty = {
    "nombre": "",
    "email": "",
    "mensaje": ""
}

try:
    message = ContactMessage(**test_empty)
    print(f"✅ EMPTY SUCCESS: {message}")
except Exception as e:
    print(f"❌ EMPTY FAILED: {e}")