from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from datetime import datetime
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from dotenv import load_dotenv
load_dotenv()

SUPABASE_URL = os.getenv("SUPABASE_URL")
SUPABASE_ANON_KEY = os.getenv("SUPABASE_ANON_KEY")
SUPABASE_SERVICE_ROLE_KEY = os.getenv("SUPABASE_SERVICE_ROLE_KEY")

if not all([SUPABASE_URL, SUPABASE_ANON_KEY, SUPABASE_SERVICE_ROLE_KEY]):
    raise RuntimeError("❌ Variables de entorno de Supabase no configuradas")

from routes.attendance_routes import router as attendance_router

app = FastAPI(
    title="Attendance Microservice",
    version="1.0.0",
    description="API para gestión de asistencias y excusas multi-tenant"
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=[
        "http://localhost:5173",
        "http://localhost:3000",
        "http://frontend:80",
        "http://localhost:5001"
    ],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(attendance_router)

@app.get("/")
async def root():
    return {"service": "Attendance API", "version": "1.0.0", "status": "running"}

@app.get("/health")
async def health():
    return {"status": "healthy", "timestamp": datetime.utcnow().isoformat(), "service": "attendance"}

if __name__ == "__main__":
    import uvicorn
    print("🚀 Iniciando Attendance Microservice...")
    uvicorn.run(app, host="0.0.0.0", port=5000, log_level="info")
