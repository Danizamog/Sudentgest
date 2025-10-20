import os
from dotenv import load_dotenv
import httpx
from typing import Optional, Dict, List

load_dotenv()

SUPABASE_URL = os.getenv("SUPABASE_URL")
SUPABASE_KEY = os.getenv("SUPABASE_KEY")


def get_tenant_schema(email: str) -> str:
    """Extract tenant schema from email domain"""
    domain = email.split("@")[1].lower()
    
    tenant_map = {
        "gmail.com": "tenant_gmail",
        "ucb.edu.bo": "tenant_ucb",
        "upb.edu": "tenant_upb"
    }
    
    tenant = tenant_map.get(domain)
    if not tenant:
        raise ValueError(f"Unknown tenant domain: {domain}")
    
    return tenant


async def supabase_request(method: str, endpoint: str, data: Optional[Dict] = None) -> List[Dict]:
    """Make request to Supabase REST API"""
    headers = {
        "apikey": SUPABASE_KEY,
        "Authorization": f"Bearer {SUPABASE_KEY}",
        "Content-Type": "application/json"
    }
    
    url = f"{SUPABASE_URL}/rest/v1/{endpoint}"
    
    async with httpx.AsyncClient(timeout=30.0) as client:
        if method == "GET":
            response = await client.get(url, headers=headers)
        elif method == "POST":
            response = await client.post(url, headers=headers, json=data)
        elif method == "PUT":
            response = await client.put(url, headers=headers, json=data)
        elif method == "PATCH":
            response = await client.patch(url, headers=headers, json=data)
        elif method == "DELETE":
            response = await client.delete(url, headers=headers)
        else:
            raise ValueError(f"Unsupported HTTP method: {method}")
        
        response.raise_for_status()
        return response.json()
