from fastapi import FastAPI
from routes.note_router import router as note_router

app = FastAPI(
    title="Notes Microservice",
    description="Microservicio para gestionar las notas de los estudiantes",
    version="1.0.0"
)

# Registrar el router principal
app.include_router(note_router)

@app.get("/")
async def root():
    return {"message": "Microservicio de Notas funcionando correctamente 🚀"}
