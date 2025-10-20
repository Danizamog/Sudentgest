from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from routes import router
from datetime import datetime

app = FastAPI(
    title="Reports Microservice",
    description="Generate detailed reports for teachers and directors",
    version="1.0.0"
)

# CORS configuration
app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:5173", "http://localhost:3000"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Include routers
app.include_router(router)


@app.get("/health")
async def health_check():
    """Health check endpoint"""
    return {
        "status": "healthy",
        "timestamp": datetime.utcnow().isoformat(),
        "service": "reports"
    }


@app.get("/")
async def root():
    """Root endpoint"""
    return {
        "message": "Reports Microservice API",
        "version": "1.0.0",
        "endpoints": {
            "health": "/health",
            "docs": "/docs",
            "course_grades": "/api/reports/course-grades/{curso_id}",
            "student_performance": "/api/reports/student-performance/{usuario_id}",
            "system_overview": "/api/reports/system-overview"
        }
    }
