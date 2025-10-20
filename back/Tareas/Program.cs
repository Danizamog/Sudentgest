using Supabase;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

var builder = WebApplication.CreateBuilder(args);

// Configuración
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar Supabase
var supabaseUrl = "https://nnqbpvbcdwcodnradhye.supabase.co";
var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im5ucWJwdmJjZHdjb2RucmFkaHllIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTg5MzYzMTcsImV4cCI6MjA3NDUxMjMxN30.ZYbcRG9D2J0SlhcT9XTzGX5AAW5wuTXPnzmkbC_pGPU";

builder.Services.AddScoped(provider => 
{
    var options = new Supabase.SupabaseOptions
    {
        AutoConnectRealtime = true
    };
    return new Client(supabaseUrl, supabaseKey, options);
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://frontend:80")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configurar pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthorization();

// Función para obtener el tenant del email
static string GetTenantFromEmail(string email)
{
    if (email.EndsWith("@ucb.edu.bo")) return "ucb.edu.bo";
    if (email.EndsWith("@upb.edu.bo")) return "upb.edu.bo";
    if (email.EndsWith("@gmail.com")) return "gmail.com";
    return "unknown";
}

async Task<UserInfo?> GetUserByEmail(Client supabase, string email, string tenant)
{
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<UsuarioUcb>()
                    .Where(x => x.Email == email)
                    .Get();
                var userUcb = responseUcb.Models.FirstOrDefault();
                return userUcb != null ? new UserInfo(userUcb.Id, userUcb.Rol ?? "Estudiante") : null;
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<UsuarioUpb>()
                    .Where(x => x.Email == email)
                    .Get();
                var userUpb = responseUpb.Models.FirstOrDefault();
                return userUpb != null ? new UserInfo(userUpb.Id, userUpb.Rol ?? "Estudiante") : null;
                
            case "gmail.com":
                var responseGmail = await supabase.From<UsuarioGmail>()
                    .Where(x => x.Email == email)
                    .Get();
                var userGmail = responseGmail.Models.FirstOrDefault();
                return userGmail != null ? new UserInfo(userGmail.Id, userGmail.Rol ?? "Estudiante") : null;
                
            default:
                return null;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error obteniendo usuario: {ex.Message}");
        return null;
    }
}

async Task<bool> IsUserEnrolledInCourse(Client supabase, int userId, int courseId, string tenant)
{
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<InscripcionUcb>()
                    .Where(x => x.UsuarioId == userId && x.CursoId == courseId)
                    .Get();
                return responseUcb.Models.Any();
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<InscripcionUpb>()
                    .Where(x => x.UsuarioId == userId && x.CursoId == courseId)
                    .Get();
                return responseUpb.Models.Any();
                
            case "gmail.com":
                var responseGmail = await supabase.From<InscripcionGmail>()
                    .Where(x => x.UsuarioId == userId && x.CursoId == courseId)
                    .Get();
                return responseGmail.Models.Any();
                
            default:
                return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error verificando inscripción: {ex.Message}");
        return false;
    }
}

async Task<List<AssignmentInfo>> GetCourseAssignments(Client supabase, int courseId, string tenant)
{
    var assignments = new List<AssignmentInfo>();
    
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<AssignmentUcb>()
                    .Where(x => x.CursoId == courseId && x.IsActive)
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();
                assignments = responseUcb.Models.Select(a => new AssignmentInfo(
                    a.Id, a.Title, a.Description, a.DueDate, a.Points, 
                    a.AssignmentType, a.CursoId, a.CreatedAt
                )).ToList();
                break;
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<AssignmentUpb>()
                    .Where(x => x.CursoId == courseId && x.IsActive)
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();
                assignments = responseUpb.Models.Select(a => new AssignmentInfo(
                    a.Id, a.Title, a.Description, a.DueDate, a.Points, 
                    a.AssignmentType, a.CursoId, a.CreatedAt
                )).ToList();
                break;
                
            case "gmail.com":
                var responseGmail = await supabase.From<AssignmentGmail>()
                    .Where(x => x.CursoId == courseId && x.IsActive)
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();
                assignments = responseGmail.Models.Select(a => new AssignmentInfo(
                    a.Id, a.Title, a.Description, a.DueDate, a.Points, 
                    a.AssignmentType, a.CursoId, a.CreatedAt
                )).ToList();
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error obteniendo tareas: {ex.Message}");
    }
    
    return assignments;
}

async Task<CompletionInfo?> GetAssignmentCompletion(Client supabase, int assignmentId, int studentId, string tenant)
{
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<AssignmentCompletionUcb>()
                    .Where(x => x.AssignmentId == assignmentId && x.StudentId == studentId)
                    .Get();
                var completionUcb = responseUcb.Models.FirstOrDefault();
                return completionUcb != null ? new CompletionInfo(
                    completionUcb.CompletedAt, completionUcb.Status, completionUcb.SubmittedContent
                ) : null;
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<AssignmentCompletionUpb>()
                    .Where(x => x.AssignmentId == assignmentId && x.StudentId == studentId)
                    .Get();
                var completionUpb = responseUpb.Models.FirstOrDefault();
                return completionUpb != null ? new CompletionInfo(
                    completionUpb.CompletedAt, completionUpb.Status, completionUpb.SubmittedContent
                ) : null;
                
            case "gmail.com":
                var responseGmail = await supabase.From<AssignmentCompletionGmail>()
                    .Where(x => x.AssignmentId == assignmentId && x.StudentId == studentId)
                    .Get();
                var completionGmail = responseGmail.Models.FirstOrDefault();
                return completionGmail != null ? new CompletionInfo(
                    completionGmail.CompletedAt, completionGmail.Status, completionGmail.SubmittedContent
                ) : null;
                
            default:
                return null;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error obteniendo completion: {ex.Message}");
        return null;
    }
}

async Task<CompletionStats> GetAssignmentCompletionStats(Client supabase, int assignmentId, string tenant)
{
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<AssignmentCompletionUcb>()
                    .Where(x => x.AssignmentId == assignmentId)
                    .Get();
                var completionsUcb = responseUcb.Models;
                var completedUcb = completionsUcb.Count(c => c.Status == "completed");
                return new CompletionStats(completionsUcb.Count, completedUcb);
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<AssignmentCompletionUpb>()
                    .Where(x => x.AssignmentId == assignmentId)
                    .Get();
                var completionsUpb = responseUpb.Models;
                var completedUpb = completionsUpb.Count(c => c.Status == "completed");
                return new CompletionStats(completionsUpb.Count, completedUpb);
                
            case "gmail.com":
                var responseGmail = await supabase.From<AssignmentCompletionGmail>()
                    .Where(x => x.AssignmentId == assignmentId)
                    .Get();
                var completionsGmail = responseGmail.Models;
                var completedGmail = completionsGmail.Count(c => c.Status == "completed");
                return new CompletionStats(completionsGmail.Count, completedGmail);
                
            default:
                return new CompletionStats(0, 0);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error obteniendo stats: {ex.Message}");
        return new CompletionStats(0, 0);
    }
}

async Task<int> CreateAssignment(Client supabase, AssignmentRequest request, int profesorId, string tenant)
{
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var assignmentUcb = new AssignmentUcb
                {
                    Title = request.Title,
                    Description = request.Description,
                    DueDate = request.DueDate,
                    Points = request.Points,
                    AssignmentType = request.AssignmentType,
                    CursoId = request.CursoId,
                    ProfesorId = profesorId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Status = "active"
                };
                var responseUcb = await supabase.From<AssignmentUcb>().Insert(assignmentUcb);
                return responseUcb.Models.FirstOrDefault()?.Id ?? 0;
                
            case "upb.edu.bo":
                var assignmentUpb = new AssignmentUpb
                {
                    Title = request.Title,
                    Description = request.Description,
                    DueDate = request.DueDate,
                    Points = request.Points,
                    AssignmentType = request.AssignmentType,
                    CursoId = request.CursoId,
                    ProfesorId = profesorId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Status = "active"
                };
                var responseUpb = await supabase.From<AssignmentUpb>().Insert(assignmentUpb);
                return responseUpb.Models.FirstOrDefault()?.Id ?? 0;
                
            case "gmail.com":
                var assignmentGmail = new AssignmentGmail
                {
                    Title = request.Title,
                    Description = request.Description,
                    DueDate = request.DueDate,
                    Points = request.Points,
                    AssignmentType = request.AssignmentType,
                    CursoId = request.CursoId,
                    ProfesorId = profesorId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Status = "active"
                };
                var responseGmail = await supabase.From<AssignmentGmail>().Insert(assignmentGmail);
                return responseGmail.Models.FirstOrDefault()?.Id ?? 0;
                
            default:
                return 0;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creando tarea: {ex.Message}");
        return 0;
    }
}

async Task<AssignmentInfo?> GetAssignmentById(Client supabase, int assignmentId, string tenant)
{
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<AssignmentUcb>()
                    .Where(x => x.Id == assignmentId)
                    .Get();
                var assignmentUcb = responseUcb.Models.FirstOrDefault();
                return assignmentUcb != null ? new AssignmentInfo(
                    assignmentUcb.Id, assignmentUcb.Title, assignmentUcb.Description, 
                    assignmentUcb.DueDate, assignmentUcb.Points, assignmentUcb.AssignmentType,
                    assignmentUcb.CursoId, assignmentUcb.CreatedAt
                ) : null;
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<AssignmentUpb>()
                    .Where(x => x.Id == assignmentId)
                    .Get();
                var assignmentUpb = responseUpb.Models.FirstOrDefault();
                return assignmentUpb != null ? new AssignmentInfo(
                    assignmentUpb.Id, assignmentUpb.Title, assignmentUpb.Description, 
                    assignmentUpb.DueDate, assignmentUpb.Points, assignmentUpb.AssignmentType,
                    assignmentUpb.CursoId, assignmentUpb.CreatedAt
                ) : null;
                
            case "gmail.com":
                var responseGmail = await supabase.From<AssignmentGmail>()
                    .Where(x => x.Id == assignmentId)
                    .Get();
                var assignmentGmail = responseGmail.Models.FirstOrDefault();
                return assignmentGmail != null ? new AssignmentInfo(
                    assignmentGmail.Id, assignmentGmail.Title, assignmentGmail.Description, 
                    assignmentGmail.DueDate, assignmentGmail.Points, assignmentGmail.AssignmentType,
                    assignmentGmail.CursoId, assignmentGmail.CreatedAt
                ) : null;
                
            default:
                return null;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error obteniendo tarea: {ex.Message}");
        return null;
    }
}

async Task<bool> CreateOrUpdateCompletion(Client supabase, int assignmentId, int studentId, string? submittedContent, string tenant)
{
    try
    {
        // Primero verificar si ya existe
        var existingCompletion = await GetAssignmentCompletion(supabase, assignmentId, studentId, tenant);
        
        if (existingCompletion != null)
        {
            // Actualizar existente
            return await UpdateCompletion(supabase, assignmentId, studentId, submittedContent, tenant);
        }
        else
        {
            // Crear nuevo
            return await CreateCompletion(supabase, assignmentId, studentId, submittedContent, tenant);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error en CreateOrUpdateCompletion: {ex.Message}");
        return false;
    }
}

async Task<bool> CreateCompletion(Client supabase, int assignmentId, int studentId, string? submittedContent, string tenant)
{
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var completionUcb = new AssignmentCompletionUcb
                {
                    AssignmentId = assignmentId,
                    StudentId = studentId,
                    CompletedAt = DateTime.UtcNow,
                    Status = "completed",
                    SubmittedContent = submittedContent
                };
                var responseUcb = await supabase.From<AssignmentCompletionUcb>().Insert(completionUcb);
                return responseUcb.Models.Any();
                
            case "upb.edu.bo":
                var completionUpb = new AssignmentCompletionUpb
                {
                    AssignmentId = assignmentId,
                    StudentId = studentId,
                    CompletedAt = DateTime.UtcNow,
                    Status = "completed",
                    SubmittedContent = submittedContent
                };
                var responseUpb = await supabase.From<AssignmentCompletionUpb>().Insert(completionUpb);
                return responseUpb.Models.Any();
                
            case "gmail.com":
                var completionGmail = new AssignmentCompletionGmail
                {
                    AssignmentId = assignmentId,
                    StudentId = studentId,
                    CompletedAt = DateTime.UtcNow,
                    Status = "completed",
                    SubmittedContent = submittedContent
                };
                var responseGmail = await supabase.From<AssignmentCompletionGmail>().Insert(completionGmail);
                return responseGmail.Models.Any();
                
            default:
                return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creando completion: {ex.Message}");
        return false;
    }
}

async Task<bool> UpdateCompletion(Client supabase, int assignmentId, int studentId, string? submittedContent, string tenant)
{
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<AssignmentCompletionUcb>()
                    .Where(x => x.AssignmentId == assignmentId && x.StudentId == studentId)
                    .Set(x => x.CompletedAt!, DateTime.UtcNow)
                    .Set(x => x.Status!, "completed")
                    .Set(x => x.SubmittedContent!, submittedContent)
                    .Update();
                return responseUcb.Models.Any();
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<AssignmentCompletionUpb>()
                    .Where(x => x.AssignmentId == assignmentId && x.StudentId == studentId)
                    .Set(x => x.CompletedAt!, DateTime.UtcNow)
                    .Set(x => x.Status!, "completed")
                    .Set(x => x.SubmittedContent!, submittedContent)
                    .Update();
                return responseUpb.Models.Any();
                
            case "gmail.com":
                var responseGmail = await supabase.From<AssignmentCompletionGmail>()
                    .Where(x => x.AssignmentId == assignmentId && x.StudentId == studentId)
                    .Set(x => x.CompletedAt!, DateTime.UtcNow)
                    .Set(x => x.Status!, "completed")
                    .Set(x => x.SubmittedContent!, submittedContent)
                    .Update();
                return responseGmail.Models.Any();
                
            default:
                return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error actualizando completion: {ex.Message}");
        return false;
    }
}

// ========== ENDPOINTS ==========

// Health check
app.MapGet("/", () => "Tareas Service is running!");
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "Tareas", timestamp = DateTime.Now }));

// Obtener tareas de un curso
app.MapGet("/api/courses/{courseId}/assignments", async (HttpContext context, int courseId, [FromServices] Client supabase) =>
{
    try
    {
        // Obtener el email del usuario del header
        if (!context.Request.Headers.TryGetValue("X-User-Email", out var userEmail) || string.IsNullOrEmpty(userEmail))
        {
            return Results.BadRequest("Email de usuario no proporcionado");
        }

        var tenant = GetTenantFromEmail(userEmail);
        if (tenant == "unknown") 
            return Results.BadRequest("Tenant no identificado");

        Console.WriteLine($"📚 Obteniendo tareas para curso {courseId}, usuario: {userEmail}, tenant: {tenant}");

        // Obtener información del usuario
        var user = await GetUserByEmail(supabase, userEmail, tenant);
        if (user == null) 
            return Results.NotFound("Usuario no encontrado");

        var userId = user.Id;
        var userRole = user.Rol;

        // Verificar que el usuario está inscrito en el curso
        var isEnrolled = await IsUserEnrolledInCourse(supabase, userId, courseId, tenant);
        if (!isEnrolled)
            return Results.Problem("No estás inscrito en este curso");

        // Obtener tareas del curso
        var assignments = await GetCourseAssignments(supabase, courseId, tenant);
        
        var result = new List<object>();

        foreach (var assignment in assignments)
        {
            if (userRole == "Estudiante")
            {
                var completion = await GetAssignmentCompletion(supabase, assignment.Id, userId, tenant);
                result.Add(new
                {
                    assignment = assignment,
                    completion = completion
                });
            }
            else if (userRole == "Profesor")
            {
                var completionStats = await GetAssignmentCompletionStats(supabase, assignment.Id, tenant);
                result.Add(new
                {
                    assignment = assignment,
                    completions = completionStats
                });
            }
        }

        return Results.Ok(new { 
            assignments = result, 
            userRole,
            total = assignments.Count 
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error obteniendo tareas: {ex.Message}");
        return Results.Problem("Error interno del servidor");
    }
});

// Crear nueva tarea (solo profesores)
app.MapPost("/api/courses/{courseId}/assignments", async (HttpContext context, int courseId, [FromBody] AssignmentRequest request, [FromServices] Client supabase) =>
{
    try
    {
        // Obtener el email del usuario del header
        if (!context.Request.Headers.TryGetValue("X-User-Email", out var userEmail) || string.IsNullOrEmpty(userEmail))
        {
            return Results.BadRequest("Email de usuario no proporcionado");
        }

        var tenant = GetTenantFromEmail(userEmail);
        if (tenant == "unknown") 
            return Results.BadRequest("Tenant no identificado");

        Console.WriteLine($"➕ Creando tarea para curso {courseId}, usuario: {userEmail}");

        // Obtener información del usuario
        var user = await GetUserByEmail(supabase, userEmail, tenant);
        if (user == null) 
            return Results.NotFound("Usuario no encontrado");

        // Verificar que es profesor
        if (user.Rol != "Profesor")
            return Results.Problem("Solo los profesores pueden crear tareas");

        // Verificar que el profesor está asignado al curso
        var isEnrolled = await IsUserEnrolledInCourse(supabase, user.Id, courseId, tenant);
        if (!isEnrolled)
            return Results.Problem("No estás asignado a este curso");

        // Crear la tarea
        var assignmentId = await CreateAssignment(supabase, request, user.Id, tenant);
        
        if (assignmentId > 0)
        {
            var newAssignment = await GetAssignmentById(supabase, assignmentId, tenant);
            return Results.Ok(new { 
                success = true, 
                assignment = newAssignment,
                message = "Tarea creada correctamente"
            });
        }

        return Results.Problem("Error al crear la tarea");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error creando tarea: {ex.Message}");
        return Results.Problem("Error interno del servidor");
    }
});

// Marcar tarea como completada (estudiantes)
app.MapPost("/api/assignments/{assignmentId}/complete", async (HttpContext context, int assignmentId, [FromBody] AssignmentCompletionRequest? request, [FromServices] Client supabase) =>
{
    try
    {
        // Obtener el email del usuario del header
        if (!context.Request.Headers.TryGetValue("X-User-Email", out var userEmail) || string.IsNullOrEmpty(userEmail))
        {
            return Results.BadRequest("Email de usuario no proporcionado");
        }

        var tenant = GetTenantFromEmail(userEmail);
        if (tenant == "unknown") 
            return Results.BadRequest("Tenant no identificado");

        Console.WriteLine($"✅ Completando tarea {assignmentId}, usuario: {userEmail}");

        // Obtener información del usuario
        var user = await GetUserByEmail(supabase, userEmail, tenant);
        if (user == null) 
            return Results.NotFound("Usuario no encontrado");

        // Verificar que es estudiante
        if (user.Rol != "Estudiante")
            return Results.Problem("Solo los estudiantes pueden completar tareas");

        // Verificar que la tarea existe
        var assignment = await GetAssignmentById(supabase, assignmentId, tenant);
        if (assignment == null)
            return Results.NotFound("Tarea no encontrada");

        // Verificar que el estudiante está inscrito en el curso
        var isEnrolled = await IsUserEnrolledInCourse(supabase, user.Id, assignment.CursoId, tenant);
        if (!isEnrolled)
            return Results.Problem("No estás inscrito en este curso");

        // Crear o actualizar completion
        var success = await CreateOrUpdateCompletion(supabase, assignmentId, user.Id, request?.SubmittedContent, tenant);
        
        if (success)
        {
            return Results.Ok(new { 
                success = true, 
                message = "Tarea marcada como completada" 
            });
        }

        return Results.Problem("Error al completar la tarea");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error completando tarea: {ex.Message}");
        return Results.Problem("Error interno del servidor");
    }
});

// Obtener detalles de una tarea específica
app.MapGet("/api/assignments/{assignmentId}", async (HttpContext context, int assignmentId, [FromServices] Client supabase) =>
{
    try
    {
        // Obtener el email del usuario del header
        if (!context.Request.Headers.TryGetValue("X-User-Email", out var userEmail) || string.IsNullOrEmpty(userEmail))
        {
            return Results.BadRequest("Email de usuario no proporcionado");
        }

        var tenant = GetTenantFromEmail(userEmail);
        if (tenant == "unknown") 
            return Results.BadRequest("Tenant no identificado");

        var assignment = await GetAssignmentById(supabase, assignmentId, tenant);
        if (assignment == null)
            return Results.NotFound("Tarea no encontrada");

        return Results.Ok(new { assignment });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error obteniendo tarea: {ex.Message}");
        return Results.Problem("Error interno del servidor");
    }
});

app.Run();


// Modelos para la request
public record AssignmentRequest(
    string Title,
    string Description,
    DateTime? DueDate,
    decimal? Points,
    string AssignmentType,
    int CursoId,
    string CreatedBy
);

public class AssignmentCompletionRequest
{
    public string? SubmittedContent { get; set; }
}

// Records para datos internos
public record UserInfo(int Id, string Rol);
public record AssignmentInfo(
    int Id, 
    string Title, 
    string Description, 
    DateTime? DueDate, 
    decimal? Points, 
    string AssignmentType, 
    int CursoId, 
    DateTime CreatedAt
);
public record CompletionInfo(DateTime? CompletedAt, string Status, string? SubmittedContent);
public record CompletionStats(int Total, int Completed);

// ========== MODELOS SUPABASE ==========

// Modelos para usuarios
[Table("tenant_ucb_usuarios")]
public class UsuarioUcb : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }
    
    [Column("nombre")]
    public string? Nombre { get; set; }
    
    [Column("apellido")]
    public string? Apellido { get; set; }
    
    [Column("email")]
    public string? Email { get; set; }
    
    [Column("rol")]
    public string? Rol { get; set; }
}

[Table("tenant_upb_usuarios")]
public class UsuarioUpb : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }
    
    [Column("nombre")]
    public string? Nombre { get; set; }
    
    [Column("apellido")]
    public string? Apellido { get; set; }
    
    [Column("email")]
    public string? Email { get; set; }
    
    [Column("rol")]
    public string? Rol { get; set; }
}

[Table("tenant_gmail_usuarios")]
public class UsuarioGmail : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }
    
    [Column("nombre")]
    public string? Nombre { get; set; }
    
    [Column("apellido")]
    public string? Apellido { get; set; }
    
    [Column("email")]
    public string? Email { get; set; }
    
    [Column("rol")]
    public string? Rol { get; set; }
}

// Modelos para assignments
[Table("tenant_ucb_assignments")]
public class AssignmentUcb : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }
    
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    
    [Column("description")]
    public string Description { get; set; } = string.Empty;
    
    [Column("due_date")]
    public DateTime? DueDate { get; set; }
    
    [Column("points")]
    public decimal? Points { get; set; }
    
    [Column("assignment_type")]
    public string AssignmentType { get; set; } = string.Empty;
    
    [Column("curso_id")]
    public int CursoId { get; set; }
    
    [Column("profesor_id")]
    public int ProfesorId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "active";
}

[Table("tenant_upb_assignments")]
public class AssignmentUpb : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }
    
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    
    [Column("description")]
    public string Description { get; set; } = string.Empty;
    
    [Column("due_date")]
    public DateTime? DueDate { get; set; }
    
    [Column("points")]
    public decimal? Points { get; set; }
    
    [Column("assignment_type")]
    public string AssignmentType { get; set; } = string.Empty;
    
    [Column("curso_id")]
    public int CursoId { get; set; }
    
    [Column("profesor_id")]
    public int ProfesorId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "active";
}

[Table("tenant_gmail_assignments")]
public class AssignmentGmail : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }
    
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    
    [Column("description")]
    public string Description { get; set; } = string.Empty;
    
    [Column("due_date")]
    public DateTime? DueDate { get; set; }
    
    [Column("points")]
    public decimal? Points { get; set; }
    
    [Column("assignment_type")]
    public string AssignmentType { get; set; } = string.Empty;
    
    [Column("curso_id")]
    public int CursoId { get; set; }
    
    [Column("profesor_id")]
    public int ProfesorId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "active";
}

// Modelos para assignment completions
[Table("tenant_ucb_assignment_completions")]
public class AssignmentCompletionUcb : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }
    
    [Column("assignment_id")]
    public int AssignmentId { get; set; }
    
    [Column("student_id")]
    public int StudentId { get; set; }
    
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "pending";
    
    [Column("submitted_content")]
    public string? SubmittedContent { get; set; }
    
    [Column("grade")]
    public decimal? Grade { get; set; }
    
    [Column("feedback")]
    public string? Feedback { get; set; }
}

[Table("tenant_upb_assignment_completions")]
public class AssignmentCompletionUpb : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }
    
    [Column("assignment_id")]
    public int AssignmentId { get; set; }
    
    [Column("student_id")]
    public int StudentId { get; set; }
    
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "pending";
    
    [Column("submitted_content")]
    public string? SubmittedContent { get; set; }
    
    [Column("grade")]
    public decimal? Grade { get; set; }
    
    [Column("feedback")]
    public string? Feedback { get; set; }
}

[Table("tenant_gmail_assignment_completions")]
public class AssignmentCompletionGmail : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }
    
    [Column("assignment_id")]
    public int AssignmentId { get; set; }
    
    [Column("student_id")]
    public int StudentId { get; set; }
    
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "pending";
    
    [Column("submitted_content")]
    public string? SubmittedContent { get; set; }
    
    [Column("grade")]
    public decimal? Grade { get; set; }
    
    [Column("feedback")]
    public string? Feedback { get; set; }
}

// Modelos para inscripciones
[Table("tenant_ucb_inscripciones")]
public class InscripcionUcb : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }
    
    [Column("usuario_id")]
    public int UsuarioId { get; set; }
    
    [Column("curso_id")]
    public int CursoId { get; set; }
}

[Table("tenant_upb_inscripciones")]
public class InscripcionUpb : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }
    
    [Column("usuario_id")]
    public int UsuarioId { get; set; }
    
    [Column("curso_id")]
    public int CursoId { get; set; }
}

[Table("tenant_gmail_inscripciones")]
public class InscripcionGmail : BaseModel
{
    [PrimaryKey("id")]
    public int Id { get; set; }
    
    [Column("usuario_id")]
    public int UsuarioId { get; set; }
    
    [Column("curso_id")]
    public int CursoId { get; set; }
}

// ========== FUNCIONES AUXILIARES ==========
