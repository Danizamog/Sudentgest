import httpx
import os
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

SUPABASE_URL = os.getenv("SUPABASE_URL")
SUPABASE_ANON_KEY = os.getenv("SUPABASE_ANON_KEY")
SUPABASE_SERVICE_ROLE_KEY = os.getenv("SUPABASE_SERVICE_ROLE_KEY")

print("🔍 Testing Supabase Connection...")
print(f"URL: {SUPABASE_URL}")
print(f"Anon Key: {SUPABASE_ANON_KEY[:50]}...")
print(f"Service Key: {SUPABASE_SERVICE_ROLE_KEY[:50]}...")
print()

async def test_connection():
    """Test connection to Supabase"""
    try:
        async with httpx.AsyncClient(timeout=10.0) as client:
            # Test with anon key
            headers = {
                "apikey": SUPABASE_ANON_KEY,
                "Authorization": f"Bearer {SUPABASE_ANON_KEY}"
            }
            response = await client.get(
                f"{SUPABASE_URL}/rest/v1/",
                headers=headers
            )
            print(f"✅ Anon key test: {response.status_code}")
            
            # Test with service role key
            headers = {
                "apikey": SUPABASE_SERVICE_ROLE_KEY,
                "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}"
            }
            response = await client.get(
                f"{SUPABASE_URL}/rest/v1/",
                headers=headers
            )
            print(f"✅ Service role key test: {response.status_code}")
            
            # Check if tables exist
            for tenant in ["ucb", "upb", "gmail"]:
                table_name = f"tenant_{tenant}_contact_messages"
                response = await client.get(
                    f"{SUPABASE_URL}/rest/v1/{table_name}?limit=1",
                    headers=headers
                )
                if response.status_code == 200:
                    print(f"✅ Table {table_name} exists")
                elif response.status_code == 404:
                    print(f"❌ Table {table_name} NOT FOUND - need to create it")
                else:
                    print(f"⚠️  Table {table_name} status: {response.status_code}")
            
    except Exception as e:
        print(f"❌ Connection error: {e}")

if __name__ == "__main__":
    import asyncio
    asyncio.run(test_connection())