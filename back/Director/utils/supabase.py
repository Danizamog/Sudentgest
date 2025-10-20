import os
import httpx

SUPABASE_URL = os.getenv("SUPABASE_URL")
SUPABASE_SERVICE_ROLE_KEY = os.getenv("SUPABASE_SERVICE_ROLE_KEY")

def get_supabase_headers():
    """Return headers for Supabase REST API calls with service role key"""
    return {
        "apikey": SUPABASE_SERVICE_ROLE_KEY,
        "Authorization": f"Bearer {SUPABASE_SERVICE_ROLE_KEY}",
        "Content-Type": "application/json",
        "Prefer": "return=representation"
    }

async def supabase_request(method: str, endpoint: str, data: dict = None):
    """Generic async request to Supabase REST API"""
    url = f"{SUPABASE_URL}/rest/v1/{endpoint}"
    headers = get_supabase_headers()
    
    async with httpx.AsyncClient() as client:
        if method == "GET":
            response = await client.get(url, headers=headers)
        elif method == "POST":
            response = await client.post(url, headers=headers, json=data)
        elif method == "PATCH":
            response = await client.patch(url, headers=headers, json=data)
        elif method == "DELETE":
            response = await client.delete(url, headers=headers)
        else:
            raise ValueError(f"Unsupported HTTP method: {method}")
        
        response.raise_for_status()
        return response.json() if response.text else None

def get_tenant_schema(email: str) -> str:
    """Extract tenant schema name from email domain"""
    if email.endswith("@ucb.edu.bo"):
        return "tenant_ucb"
    elif email.endswith("@upb.edu.bo"):
        return "tenant_upb"
    elif email.endswith("@gmail.com"):
        return "tenant_gmail"
    else:
        raise ValueError(f"Email domain not recognized: {email}")

def get_tenant_from_domain(domain: str) -> str:
    """Get tenant schema from domain string"""
    domain_lower = domain.lower()
    if "ucb.edu.bo" in domain_lower:
        return "tenant_ucb"
    elif "upb.edu.bo" in domain_lower:
        return "tenant_upb"
    elif "gmail.com" in domain_lower:
        return "tenant_gmail"
    else:
        raise ValueError(f"Domain not recognized: {domain}")
