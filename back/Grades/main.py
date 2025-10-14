from fastapi import FastAPI
from routes.grade_router import router as grade_router

app = FastAPI(
    title="Grades Microservice",
    description="Microservicio para gestionar las notas de los estudiantes",
    version="1.0.0"
)

# Registrar el router principal
app.include_router(grade_router)

@app.get("/")
async def root():
    return {"message": "Microservicio de Notas funcionando correctamente 🚀"}
