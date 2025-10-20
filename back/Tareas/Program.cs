using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using Supabase;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Configuración para Docker - variables de entorno
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") 
    ?? throw new ArgumentNullException("SUPABASE_URL no configurado");
var supabaseAnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") 
    ?? throw new ArgumentNullException("SUPABASE_ANON_KEY no configurado");
var supabaseJwtSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET") ?? supabaseAnonKey;

// 🔹 JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseJwtSecret!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar Supabase Client
builder.Services.AddScoped(provider => 
{
    try
    {
        Console.WriteLine($"🔗 Inicializando cliente Supabase: {supabaseUrl}");
        var options = new Supabase.SupabaseOptions
        {
            AutoConnectRealtime = true,
            AutoRefreshToken = true
        };
        
        var client = new Client(supabaseUrl, supabaseAnonKey, options);
        Console.WriteLine("✅ Cliente Supabase inicializado correctamente");
        return client;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error inicializando Supabase: {ex.Message}");
        throw;
    }
});

// 🔹 CORS para Docker
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", 
                "http://frontend:80", 
                "http://frontend:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configuración del pipeline
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health check endpoint
app.MapGet("/health", () => 
{
    return Results.Ok(new { 
        status = "Healthy", 
        service = "Tareas",
        timestamp = DateTime.UtcNow,
        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
    });
});
// ═══════════════════════════════════════════════════════════════
// 🔹 FUNCIONES HELPER
// ═══════════════════════════════════════════════════════════════

static string GetTenantFromEmail(string email)
{
    Console.WriteLine($"🔍 Obteniendo tenant para email: {email}");
    if (email.EndsWith("@ucb.edu.bo")) 
    {
        Console.WriteLine("✅ Tenant identificado: ucb.edu.bo");
        return "ucb.edu.bo";
    }
    if (email.EndsWith("@upb.edu.bo")) 
    {
        Console.WriteLine("✅ Tenant identificado: upb.edu.bo");
        return "upb.edu.bo";
    }
    if (email.EndsWith("@gmail.com")) 
    {
        Console.WriteLine("✅ Tenant identificado: gmail.com");
        return "gmail.com";
    }
    Console.WriteLine("❌ Tenant no identificado");
    return "unknown";
}

async Task<UserInfo?> GetUserFromToken(string email)
{
    Console.WriteLine($"👤 Obteniendo información del usuario: {email}");
    try
    {
        var tenant = GetTenantFromEmail(email);
        if (tenant == "unknown") return null;

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseAnonKey}");

        var tenantResponse = await httpClient.GetAsync($"{supabaseUrl}/rest/v1/tenants?domain=eq.{tenant}&select=*");
        if (!tenantResponse.IsSuccessStatusCode) return null;

        var tenantContent = await tenantResponse.Content.ReadAsStringAsync();
        var tenants = JsonSerializer.Deserialize<JsonElement[]>(tenantContent);
        if (tenants?.Length == 0) return null;

        var schema = tenants[0].GetProperty("schema_name").GetString();
        Console.WriteLine($"✅ Schema identificado: {schema}");

        var userResponse = await httpClient.GetAsync($"{supabaseUrl}/rest/v1/{schema}_usuarios?email=eq.{Uri.EscapeDataString(email)}&select=id,rol");
        if (!userResponse.IsSuccessStatusCode) return null;

        var userContent = await userResponse.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<JsonElement[]>(userContent);
        if (users?.Length == 0) return null;

        var user = users[0];
        var userId = user.GetProperty("id").GetInt32();
        var userRole = user.GetProperty("rol").GetString() ?? "Estudiante";

        Console.WriteLine($"✅ Usuario encontrado: ID={userId}, Rol={userRole}");
        return new UserInfo(userId, userRole);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo información del usuario: {ex.Message}");
        return null;
    }
}

async Task<List<AssignmentWithCourseInfo>> GetAllUserAssignments(Client supabase, int userId, string userRole, string tenant)
{
    Console.WriteLine($"📚 Obteniendo todas las tareas del usuario: {userId}, Rol: {userRole}, Tenant: {tenant}");
    var assignmentsWithCourse = new List<AssignmentWithCourseInfo>();
    
    try
    {
        if (userRole == "Profesor")
        {
            Console.WriteLine("👨‍🏫 Obteniendo tareas creadas por el profesor...");
            var assignedCourses = await GetProfessorCourses(supabase, userId, tenant);
            Console.WriteLine($"📖 Cursos asignados al profesor: {assignedCourses.Count}");
            
            foreach (var course in assignedCourses)
            {
                var courseAssignments = await GetCourseAssignments(supabase, course.Id, tenant);
                foreach (var assignment in courseAssignments)
                {
                    assignmentsWithCourse.Add(new AssignmentWithCourseInfo(
                        assignment, course.Nombre, course.Codigo
                    ));
                }
            }
        }
        else if (userRole == "Estudiante")
        {
            Console.WriteLine("👨‍🎓 Obteniendo tareas del estudiante...");
            var enrolledCourses = await GetStudentCourses(supabase, userId, tenant);
            Console.WriteLine($"📖 Cursos inscritos del estudiante: {enrolledCourses.Count}");
            
            foreach (var course in enrolledCourses)
            {
                var courseAssignments = await GetCourseAssignments(supabase, course.Id, tenant);
                foreach (var assignment in courseAssignments)
                {
                    assignmentsWithCourse.Add(new AssignmentWithCourseInfo(
                        assignment, course.Nombre, course.Codigo
                    ));
                }
            }
        }
        
        Console.WriteLine($"✅ Total de tareas obtenidas: {assignmentsWithCourse.Count}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo todas las tareas: {ex.Message}");
    }
    
    return assignmentsWithCourse;
}

async Task<CompletionInfo?> GetAssignmentCompletion(Client supabase, int assignmentId, int studentId, string tenant)
{
    Console.WriteLine($"✅ Obteniendo completion - Assignment: {assignmentId}, Student: {studentId}");
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<AssignmentCompletionUcb>()
                    .Where(x => x.AssignmentId == assignmentId && x.StudentId == studentId)
                    .Get();
                var completionUcb = responseUcb.Models.FirstOrDefault();
                Console.WriteLine(completionUcb != null ? "✅ Completion encontrado UCB" : "❌ Completion no encontrado UCB");
                return completionUcb != null ? new CompletionInfo(
                    completionUcb.CompletedAt, completionUcb.Status, completionUcb.SubmittedContent
                ) : null;
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<AssignmentCompletionUpb>()
                    .Where(x => x.AssignmentId == assignmentId && x.StudentId == studentId)
                    .Get();
                var completionUpb = responseUpb.Models.FirstOrDefault();
                Console.WriteLine(completionUpb != null ? "✅ Completion encontrado UPB" : "❌ Completion no encontrado UPB");
                return completionUpb != null ? new CompletionInfo(
                    completionUpb.CompletedAt, completionUpb.Status, completionUpb.SubmittedContent
                ) : null;
                
            case "gmail.com":
                var responseGmail = await supabase.From<AssignmentCompletionGmail>()
                    .Where(x => x.AssignmentId == assignmentId && x.StudentId == studentId)
                    .Get();
                var completionGmail = responseGmail.Models.FirstOrDefault();
                Console.WriteLine(completionGmail != null ? "✅ Completion encontrado Gmail" : "❌ Completion no encontrado Gmail");
                return completionGmail != null ? new CompletionInfo(
                    completionGmail.CompletedAt, completionGmail.Status, completionGmail.SubmittedContent
                ) : null;
                
            default:
                Console.WriteLine("❌ Tenant no soportado para obtener completion");
                return null;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo completion: {ex.Message}");
        return null;
    }
}

async Task<CompletionStats> GetAssignmentCompletionStats(Client supabase, int assignmentId, string tenant)
{
    Console.WriteLine($"📊 Obteniendo stats - Assignment: {assignmentId}");
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
                Console.WriteLine($"✅ Stats UCB: {completedUcb}/{completionsUcb.Count}");
                return new CompletionStats(completionsUcb.Count, completedUcb);
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<AssignmentCompletionUpb>()
                    .Where(x => x.AssignmentId == assignmentId)
                    .Get();
                var completionsUpb = responseUpb.Models;
                var completedUpb = completionsUpb.Count(c => c.Status == "completed");
                Console.WriteLine($"✅ Stats UPB: {completedUpb}/{completionsUpb.Count}");
                return new CompletionStats(completionsUpb.Count, completedUpb);
                
            case "gmail.com":
                var responseGmail = await supabase.From<AssignmentCompletionGmail>()
                    .Where(x => x.AssignmentId == assignmentId)
                    .Get();
                var completionsGmail = responseGmail.Models;
                var completedGmail = completionsGmail.Count(c => c.Status == "completed");
                Console.WriteLine($"✅ Stats Gmail: {completedGmail}/{completionsGmail.Count}");
                return new CompletionStats(completionsGmail.Count, completedGmail);
                
            default:
                Console.WriteLine("❌ Tenant no soportado para stats");
                return new CompletionStats(0, 0);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo stats: {ex.Message}");
        return new CompletionStats(0, 0);
    }
}

async Task<bool> IsUserEnrolledInCourse(Client supabase, int userId, int courseId, string tenant, string userRole)
{
    Console.WriteLine($"📚 Verificando acceso - User: {userId}, Course: {courseId}, Tenant: {tenant}, Role: {userRole}");
    
    try
    {
        if (userRole == "Profesor")
        {
            Console.WriteLine("👨‍🏫 Verificando asignación de profesor al curso...");
            switch (tenant.ToLower())
            {
                case "ucb.edu.bo":
                    var cursoUcb = await supabase.From<CursoUcb>()
                        .Where(x => x.Id == courseId && x.ProfesorId == userId)
                        .Get();
                    var isProfesorUcb = cursoUcb.Models.Any();
                    Console.WriteLine(isProfesorUcb ? "✅ Profesor asignado al curso UCB" : "❌ Profesor NO asignado al curso UCB");
                    return isProfesorUcb;
                    
                case "upb.edu.bo":
                    var cursoUpb = await supabase.From<CursoUpb>()
                        .Where(x => x.Id == courseId && x.ProfesorId == userId)
                        .Get();
                    var isProfesorUpb = cursoUpb.Models.Any();
                    Console.WriteLine(isProfesorUpb ? "✅ Profesor asignado al curso UPB" : "❌ Profesor NO asignado al curso UPB");
                    return isProfesorUpb;
                    
                case "gmail.com":
                    var cursoGmail = await supabase.From<CursoGmail>()
                        .Where(x => x.Id == courseId && x.ProfesorId == userId)
                        .Get();
                    var isProfesorGmail = cursoGmail.Models.Any();
                    Console.WriteLine(isProfesorGmail ? "✅ Profesor asignado al curso Gmail" : "❌ Profesor NO asignado al curso Gmail");
                    return isProfesorGmail;
                    
                default:
                    Console.WriteLine("❌ Tenant no soportado para verificación de profesor");
                    return false;
            }
        }
        else if (userRole == "Estudiante")
        {
            Console.WriteLine("👨‍🎓 Verificando inscripción de estudiante...");
            switch (tenant.ToLower())
            {
                case "ucb.edu.bo":
                    var responseUcb = await supabase.From<InscripcionUcb>()
                        .Where(x => x.UsuarioId == userId && x.CursoId == courseId)
                        .Get();
                    var isEnrolledUcb = responseUcb.Models.Any();
                    Console.WriteLine(isEnrolledUcb ? "✅ Estudiante inscrito en curso UCB" : "❌ Estudiante NO inscrito en curso UCB");
                    return isEnrolledUcb;
                    
                case "upb.edu.bo":
                    var responseUpb = await supabase.From<InscripcionUpb>()
                        .Where(x => x.UsuarioId == userId && x.CursoId == courseId)
                        .Get();
                    var isEnrolledUpb = responseUpb.Models.Any();
                    Console.WriteLine(isEnrolledUpb ? "✅ Estudiante inscrito en curso UPB" : "❌ Estudiante NO inscrito en curso UPB");
                    return isEnrolledUpb;
                    
                case "gmail.com":
                    var responseGmail = await supabase.From<InscripcionGmail>()
                        .Where(x => x.UsuarioId == userId && x.CursoId == courseId)
                        .Get();
                    var isEnrolledGmail = responseGmail.Models.Any();
                    Console.WriteLine(isEnrolledGmail ? "✅ Estudiante inscrito en curso Gmail" : "❌ Estudiante NO inscrito en curso Gmail");
                    return isEnrolledGmail;
                    
                default:
                    Console.WriteLine("❌ Tenant no soportado para verificación de estudiante");
                    return false;
            }
        }
        
        Console.WriteLine("❌ Rol no reconocido");
        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error verificando acceso: {ex.Message}");
        return false;
    }
}

async Task<List<AssignmentInfo>> GetCourseAssignments(Client supabase, int courseId, string tenant)
{
    Console.WriteLine($"🔍 BUSCANDO TAREAS - Curso: {courseId}, Tenant: {tenant}");
    var assignments = new List<AssignmentInfo>();
    
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                Console.WriteLine("🔍 Ejecutando query para UCB...");
                var responseUcb = await supabase.From<AssignmentUcb>()
                    .Where(x => x.CursoId == courseId && x.IsActive)
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();
                Console.WriteLine($"📊 Tareas UCB encontradas: {responseUcb.Models.Count}");
                assignments = responseUcb.Models.Select(a => new AssignmentInfo(
                    a.Id, a.Title, a.Description, a.DueDate, a.Points, 
                    a.AssignmentType, a.CursoId, a.CreatedAt
                )).ToList();
                break;
                
            case "upb.edu.bo":
                Console.WriteLine("🔍 Ejecutando query para UPB...");
                var responseUpb = await supabase.From<AssignmentUpb>()
                    .Where(x => x.CursoId == courseId && x.IsActive)
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();
                Console.WriteLine($"📊 Tareas UPB encontradas: {responseUpb.Models.Count}");
                assignments = responseUpb.Models.Select(a => new AssignmentInfo(
                    a.Id, a.Title, a.Description, a.DueDate, a.Points, 
                    a.AssignmentType, a.CursoId, a.CreatedAt
                )).ToList();
                break;
                
            case "gmail.com":
                Console.WriteLine("🔍 Ejecutando query para Gmail...");
                var responseGmail = await supabase.From<AssignmentGmail>()
                    .Where(x => x.CursoId == courseId && x.IsActive)
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();
                Console.WriteLine($"📊 Tareas Gmail encontradas: {responseGmail.Models.Count}");
                assignments = responseGmail.Models.Select(a => new AssignmentInfo(
                    a.Id, a.Title, a.Description, a.DueDate, a.Points, 
                    a.AssignmentType, a.CursoId, a.CreatedAt
                )).ToList();
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo tareas: {ex.Message}");
        Console.WriteLine($"💥 Stack trace: {ex.StackTrace}");
    }
    
    Console.WriteLine($"✅ Tareas retornadas: {assignments.Count}");
    return assignments;
}

async Task<List<CourseInfo>> GetProfessorCourses(Client supabase, int profesorId, string tenant)
{
    Console.WriteLine($"👨‍🏫 Obteniendo cursos del profesor: {profesorId}, Tenant: {tenant}");
    var courses = new List<CourseInfo>();
    
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<CursoUcb>()
                    .Where(x => x.ProfesorId == profesorId)
                    .Get();
                courses = responseUcb.Models.Select(c => new CourseInfo(c.Id, c.Nombre ?? "Sin nombre", c.Codigo ?? "Sin código")).ToList();
                break;
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<CursoUpb>()
                    .Where(x => x.ProfesorId == profesorId)
                    .Get();
                courses = responseUpb.Models.Select(c => new CourseInfo(c.Id, c.Nombre ?? "Sin nombre", c.Codigo ?? "Sin código")).ToList();
                break;
                
            case "gmail.com":
                var responseGmail = await supabase.From<CursoGmail>()
                    .Where(x => x.ProfesorId == profesorId)
                    .Get();
                courses = responseGmail.Models.Select(c => new CourseInfo(c.Id, c.Nombre ?? "Sin nombre", c.Codigo ?? "Sin código")).ToList();
                break;
        }
        
        Console.WriteLine($"✅ Cursos del profesor: {courses.Count}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo cursos del profesor: {ex.Message}");
    }
    
    return courses;
}

async Task<List<CourseInfo>> GetStudentCourses(Client supabase, int studentId, string tenant)
{
    Console.WriteLine($"👨‍🎓 Obteniendo cursos del estudiante: {studentId}, Tenant: {tenant}");
    var courses = new List<CourseInfo>();
    
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<InscripcionUcb>()
                    .Where(x => x.UsuarioId == studentId)
                    .Get();
                var inscripcionesUcb = responseUcb.Models;
                Console.WriteLine($"📋 Inscripciones UCB encontradas: {inscripcionesUcb.Count}");
                
                foreach (var insc in inscripcionesUcb)
                {
                    var courseUcb = await supabase.From<CursoUcb>()
                        .Where(x => x.Id == insc.CursoId)
                        .Get();
                    var cursoUcb = courseUcb.Models.FirstOrDefault();
                    if (cursoUcb != null)
                        courses.Add(new CourseInfo(cursoUcb.Id, cursoUcb.Nombre ?? "Sin nombre", cursoUcb.Codigo ?? "Sin código"));
                }
                break;
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<InscripcionUpb>()
                    .Where(x => x.UsuarioId == studentId)
                    .Get();
                var inscripcionesUpb = responseUpb.Models;
                Console.WriteLine($"📋 Inscripciones UPB encontradas: {inscripcionesUpb.Count}");
                
                foreach (var insc in inscripcionesUpb)
                {
                    var courseUpb = await supabase.From<CursoUpb>()
                        .Where(x => x.Id == insc.CursoId)
                        .Get();
                    var cursoUpb = courseUpb.Models.FirstOrDefault();
                    if (cursoUpb != null)
                        courses.Add(new CourseInfo(cursoUpb.Id, cursoUpb.Nombre ?? "Sin nombre", cursoUpb.Codigo ?? "Sin código"));
                }
                break;
                
            case "gmail.com":
                var responseGmail = await supabase.From<InscripcionGmail>()
                    .Where(x => x.UsuarioId == studentId)
                    .Get();
                var inscripcionesGmail = responseGmail.Models;
                Console.WriteLine($"📋 Inscripciones Gmail encontradas: {inscripcionesGmail.Count}");
                
                foreach (var insc in inscripcionesGmail)
                {
                    var courseGmail = await supabase.From<CursoGmail>()
                        .Where(x => x.Id == insc.CursoId)
                        .Get();
                    var cursoGmail = courseGmail.Models.FirstOrDefault();
                    if (cursoGmail != null)
                        courses.Add(new CourseInfo(cursoGmail.Id, cursoGmail.Nombre ?? "Sin nombre", cursoGmail.Codigo ?? "Sin código"));
                }
                break;
        }
        
        Console.WriteLine($"✅ Cursos del estudiante: {courses.Count}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo cursos del estudiante: {ex.Message}");
    }
    
    return courses;
}

async Task<string?> GetCourseName(Client supabase, int courseId, string tenant)
{
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<CursoUcb>()
                    .Where(x => x.Id == courseId)
                    .Get();
                return responseUcb.Models.FirstOrDefault()?.Nombre;
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<CursoUpb>()
                    .Where(x => x.Id == courseId)
                    .Get();
                return responseUpb.Models.FirstOrDefault()?.Nombre;
                
            case "gmail.com":
                var responseGmail = await supabase.From<CursoGmail>()
                    .Where(x => x.Id == courseId)
                    .Get();
                return responseGmail.Models.FirstOrDefault()?.Nombre;
                
            default:
                return null;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo nombre del curso: {ex.Message}");
        return null;
    }
}

async Task<bool> VerifyCourseExists(Client supabase, int courseId, string tenant)
{
    try
    {
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<CursoUcb>()
                    .Where(x => x.Id == courseId)
                    .Get();
                return responseUcb.Models.Any();
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<CursoUpb>()
                    .Where(x => x.Id == courseId)
                    .Get();
                return responseUpb.Models.Any();
                
            case "gmail.com":
                var responseGmail = await supabase.From<CursoGmail>()
                    .Where(x => x.Id == courseId)
                    .Get();
                return responseGmail.Models.Any();
                
            default:
                return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error verificando curso: {ex.Message}");
        return false;
    }
}

async Task<int> CreateAssignment(Client supabase, AssignmentRequest request, int profesorId, string tenant)
{
    Console.WriteLine($"➕ CREANDO TAREA - Validando datos...");
    Console.WriteLine($"📦 Request: Title='{request.Title}', CourseId={request.CursoId}, Type='{request.AssignmentType}'");

    try
    {
        // Validación más robusta
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            Console.WriteLine("❌ ERROR: Título vacío");
            return 0;
        }

        if (request.CursoId <= 0)
        {
            Console.WriteLine($"❌ ERROR: CourseId inválido: {request.CursoId}");
            return 0;
        }

        // Verificar que el curso existe
        var courseExists = await VerifyCourseExists(supabase, request.CursoId, tenant);
        if (!courseExists)
        {
            Console.WriteLine($"❌ ERROR: El curso {request.CursoId} no existe");
            return 0;
        }

        var assignmentType = string.IsNullOrEmpty(request.AssignmentType) ? "tarea" : request.AssignmentType;

        Console.WriteLine($"📦 Insertando en tenant: {tenant}");
        Console.WriteLine($"📝 Datos: Title='{request.Title}', Type='{assignmentType}', Course={request.CursoId}, Profesor={profesorId}");

        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var assignmentUcb = new AssignmentUcb
                {
                    Title = request.Title.Trim(),
                    Description = request.Description?.Trim() ?? "",
                    DueDate = request.DueDate,
                    Points = request.Points,
                    AssignmentType = assignmentType,
                    CursoId = request.CursoId,
                    ProfesorId = profesorId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Status = "active"
                };

                Console.WriteLine("📤 Ejecutando INSERT en UCB...");
                var responseUcb = await supabase.From<AssignmentUcb>().Insert(assignmentUcb);

                if (responseUcb.ResponseMessage?.IsSuccessStatusCode == true)
                {
                    var newAssignment = responseUcb.Models.FirstOrDefault();
                    if (newAssignment != null && newAssignment.Id > 0)
                    {
                        Console.WriteLine($"✅ Tarea UCB creada con ID: {newAssignment.Id}");
                        return newAssignment.Id;
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Tarea creada pero no se pudo obtener ID, verificando directamente...");
                        // Intentar obtener la tarea recién creada
                        var verifyResponse = await supabase.From<AssignmentUcb>()
                            .Where(x => x.ProfesorId == profesorId && x.CursoId == request.CursoId)
                            .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                            .Limit(1)
                            .Get();

                        var latest = verifyResponse.Models.FirstOrDefault();
                        if (latest != null)
                        {
                            Console.WriteLine($"✅ Tarea verificada con ID: {latest.Id}");
                            return latest.Id;
                        }
                    }
                }

                Console.WriteLine($"❌ Error en UCB: {responseUcb.ResponseMessage?.StatusCode}");
                return 0;

            case "upb.edu.bo":
                var assignmentUpb = new AssignmentUpb
                {
                    Title = request.Title.Trim(),
                    Description = request.Description?.Trim() ?? "",
                    DueDate = request.DueDate,
                    Points = request.Points,
                    AssignmentType = assignmentType,
                    CursoId = request.CursoId,
                    ProfesorId = profesorId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Status = "active"
                };

                Console.WriteLine("📤 Ejecutando INSERT en UPB...");
                var responseUpb = await supabase.From<AssignmentUpb>().Insert(assignmentUpb);

                if (responseUpb.ResponseMessage?.IsSuccessStatusCode == true)
                {
                    var newAssignment = responseUpb.Models.FirstOrDefault();
                    if (newAssignment != null && newAssignment.Id > 0)
                    {
                        Console.WriteLine($"✅ Tarea UPB creada con ID: {newAssignment.Id}");
                        return newAssignment.Id;
                    }
                }

                Console.WriteLine($"❌ Error en UPB: {responseUpb.ResponseMessage?.StatusCode}");
                return 0;

            case "gmail.com":
                var assignmentGmail = new AssignmentGmail
                {
                    Title = request.Title.Trim(),
                    Description = request.Description?.Trim() ?? "",
                    DueDate = request.DueDate,
                    Points = request.Points,
                    AssignmentType = assignmentType,
                    CursoId = request.CursoId,
                    ProfesorId = profesorId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Status = "active"
                };

                Console.WriteLine("📤 Ejecutando INSERT en Gmail...");
                var responseGmail = await supabase.From<AssignmentGmail>().Insert(assignmentGmail);

                if (responseGmail.ResponseMessage?.IsSuccessStatusCode == true)
                {
                    var newAssignment = responseGmail.Models.FirstOrDefault();
                    if (newAssignment != null && newAssignment.Id > 0)
                    {
                        Console.WriteLine($"✅ Tarea Gmail creada con ID: {newAssignment.Id}");
                        return newAssignment.Id;
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Tarea creada pero ID no retornado, verificando...");
                        // Verificar la tarea recién creada
                        var verifyResponse = await supabase.From<AssignmentGmail>()
                            .Where(x => x.ProfesorId == profesorId && x.CursoId == request.CursoId)
                            .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                            .Limit(1)
                            .Get();

                        var latest = verifyResponse.Models.FirstOrDefault();
                        if (latest != null)
                        {
                            Console.WriteLine($"✅ Tarea Gmail verificada con ID: {latest.Id}");
                            return latest.Id;
                        }
                    }
                }

                Console.WriteLine($"❌ Error en Gmail: {responseGmail.ResponseMessage?.StatusCode}");
                if (responseGmail.Content != null)
                {
                    Console.WriteLine($"❌ Contenido de error: {responseGmail.Content}");
                }
                return 0;

            default:
                Console.WriteLine("❌ Tenant no soportado");
                return 0;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error CRÍTICO creando tarea: {ex.Message}");
        Console.WriteLine($"💥 Stack trace: {ex.StackTrace}");
        Console.WriteLine($"💥 Inner exception: {ex.InnerException?.Message}");
        return 0;
    }
}

// ═══════════════════════════════════════════════════════════════
// 🔹 ENDPOINTS PRINCIPALES
// ═══════════════════════════════════════════════════════════════

app.MapGet("/", () => "Tareas Service is running!");

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "Tareas", timestamp = DateTime.Now }));

// 🔹 OBTENER TODAS LAS TAREAS DEL USUARIO (VISTA GENERAL)
app.MapGet("/api/assignments", async (HttpContext context, [FromServices] Client supabase) =>
{
    Console.WriteLine("📝 Endpoint: Obtener todas las tareas del usuario");
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("❌ Usuario no autenticado");
            return Results.Unauthorized();
        }

        Console.WriteLine($"🎯 Obteniendo tareas para usuario: {email}");

        var user = await GetUserFromToken(email);
        if (user == null) 
        {
            Console.WriteLine("❌ Usuario no encontrado en base de datos");
            return Results.NotFound("Usuario no encontrado");
        }

        var tenant = GetTenantFromEmail(email);
        if (tenant == "unknown")
        {
            Console.WriteLine("❌ Tenant no identificado");
            return Results.BadRequest("Tenant no identificado");
        }

        var userId = user.Id;
        var userRole = user.Rol;

        Console.WriteLine($"👤 Usuario: {userId}, Rol: {userRole}, Tenant: {tenant}");

        var allAssignments = await GetAllUserAssignments(supabase, userId, userRole, tenant);
        
        var result = new List<object>();

        foreach (var assignmentWithCourse in allAssignments)
        {
            var assignment = assignmentWithCourse.Assignment;
            
            if (userRole == "Estudiante")
            {
                var completion = await GetAssignmentCompletion(supabase, assignment.Id, userId, tenant);
                result.Add(new
                {
                    assignment = new {
                        id = assignment.Id,
                        title = assignment.Title,
                        description = assignment.Description,
                        due_date = assignment.DueDate,
                        points = assignment.Points,
                        assignment_type = assignment.AssignmentType,
                        curso_id = assignment.CursoId,
                        created_at = assignment.CreatedAt,
                        curso_nombre = assignmentWithCourse.CursoNombre,
                        curso_codigo = assignmentWithCourse.CursoCodigo
                    },
                    completion = completion
                });
            }
            else if (userRole == "Profesor")
            {
                var completionStats = await GetAssignmentCompletionStats(supabase, assignment.Id, tenant);
                result.Add(new
                {
                    assignment = new {
                        id = assignment.Id,
                        title = assignment.Title,
                        description = assignment.Description,
                        due_date = assignment.DueDate,
                        points = assignment.Points,
                        assignment_type = assignment.AssignmentType,
                        curso_id = assignment.CursoId,
                        created_at = assignment.CreatedAt,
                        curso_nombre = assignmentWithCourse.CursoNombre,
                        curso_codigo = assignmentWithCourse.CursoCodigo
                    },
                    completions = completionStats
                });
            }
        }

        Console.WriteLine($"✅ Tareas obtenidas: {result.Count}");

        return Results.Ok(new { 
            assignments = result, 
            userRole,
            total = result.Count 
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo todas las tareas: {ex.Message}");
        Console.WriteLine($"💥 Stack trace: {ex.StackTrace}");
        return Results.Problem("Error interno del servidor");
    }
}).RequireAuthorization();

// 🔹 OBTENER TAREAS DE UN CURSO ESPECÍFICO
app.MapGet("/api/courses/{courseId}/assignments", async (HttpContext context, int courseId, [FromServices] Client supabase) =>
{
    Console.WriteLine($"📝 Endpoint: Obtener tareas del curso {courseId}");
    
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("❌ Usuario no autenticado");
            return Results.Unauthorized();
        }

        var user = await GetUserFromToken(email);
        if (user == null) 
        {
            Console.WriteLine("❌ Usuario no encontrado");
            return Results.NotFound("Usuario no encontrado");
        }

        var tenant = GetTenantFromEmail(email);
        if (tenant == "unknown") 
        {
            Console.WriteLine("❌ Tenant no identificado");
            return Results.BadRequest("Tenant no identificado");
        }

        // Verificar que el usuario tiene acceso al curso
        var hasAccess = await IsUserEnrolledInCourse(supabase, user.Id, courseId, tenant, user.Rol);
        if (!hasAccess)
        {
            Console.WriteLine("❌ Usuario no tiene acceso al curso");
            return Results.Problem("No tienes acceso a este curso");
        }

        Console.WriteLine($"🔍 Obteniendo tareas para curso {courseId}");
        var assignments = await GetCourseAssignments(supabase, courseId, tenant);
        
        // Obtener nombre del curso
        var courseName = await GetCourseName(supabase, courseId, tenant);
        
        // Para estudiantes, agregar información de completion
        var result = new List<object>();
        foreach (var assignment in assignments)
        {
            if (user.Rol == "Estudiante")
            {
                var completion = await GetAssignmentCompletion(supabase, assignment.Id, user.Id, tenant);
                result.Add(new
                {
                    assignment = new {
                        id = assignment.Id,
                        title = assignment.Title,
                        description = assignment.Description,
                        due_date = assignment.DueDate,
                        points = assignment.Points,
                        assignment_type = assignment.AssignmentType,
                        curso_id = assignment.CursoId,
                        created_at = assignment.CreatedAt,
                        curso_nombre = courseName
                    },
                    completion = completion
                });
            }
            else if (user.Rol == "Profesor")
            {
                var completionStats = await GetAssignmentCompletionStats(supabase, assignment.Id, tenant);
                result.Add(new
                {
                    assignment = new {
                        id = assignment.Id,
                        title = assignment.Title,
                        description = assignment.Description,
                        due_date = assignment.DueDate,
                        points = assignment.Points,
                        assignment_type = assignment.AssignmentType,
                        curso_id = assignment.CursoId,
                        created_at = assignment.CreatedAt,
                        curso_nombre = courseName
                    },
                    completions = completionStats
                });
            }
        }
        
        Console.WriteLine($"✅ Tareas encontradas: {result.Count}");
        return Results.Ok(new { 
            assignments = result,
            userRole = user.Rol,
            total = result.Count 
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo tareas: {ex.Message}");
        return Results.Problem("Error interno del servidor");
    }
}).RequireAuthorization();

// 🔹 CREAR TAREA
app.MapPost("/api/courses/{courseId}/assignments", async (HttpContext context, int courseId, [FromBody] AssignmentRequest request, [FromServices] Client supabase) =>
{
    Console.WriteLine($"➕ Endpoint: Crear tarea en curso {courseId}");
    
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("❌ Usuario no autenticado");
            return Results.Unauthorized();
        }

        Console.WriteLine($"📦 Datos recibidos: Title='{request.Title}', AssignmentType='{request.AssignmentType}', CursoId={request.CursoId}");

        var user = await GetUserFromToken(email);
        if (user == null) 
        {
            Console.WriteLine("❌ Usuario no encontrado");
            return Results.NotFound("Usuario no encontrado");
        }

        if (user.Rol != "Profesor")
        {
            Console.WriteLine("❌ Usuario no es profesor");
            return Results.Problem("Solo los profesores pueden crear tareas");
        }

        var tenant = GetTenantFromEmail(email);
        if (tenant == "unknown") 
        {
            Console.WriteLine("❌ Tenant no identificado");
            return Results.BadRequest("Tenant no identificado");
        }

        // Verificar que el profesor está asignado al curso
        var hasAccess = await IsUserEnrolledInCourse(supabase, user.Id, courseId, tenant, user.Rol);
        if (!hasAccess)
        {
            Console.WriteLine("❌ Profesor no asignado al curso");
            return Results.Problem("No estás asignado a este curso");
        }

        // 🔥 CREAR TAREA
        var assignmentId = await CreateAssignment(supabase, request, user.Id, tenant);
        
        if (assignmentId > 0)
        {
            Console.WriteLine($"✅ Tarea creada exitosamente con ID: {assignmentId}");
            return Results.Ok(new { 
                success = true, 
                assignmentId = assignmentId,
                message = "Tarea creada correctamente"
            });
        }

        Console.WriteLine("❌ Error al crear la tarea - assignmentId = 0");
        return Results.Problem("Error al crear la tarea en la base de datos");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error en endpoint crear tarea: {ex.Message}");
        Console.WriteLine($"💥 Stack trace: {ex.StackTrace}");
        return Results.Problem("Error interno del servidor");
    }
}).RequireAuthorization();

// 🔹 OBTENER CURSOS DEL USUARIO
app.MapGet("/api/my-courses", async (HttpContext context, [FromServices] Client supabase) =>
{
    Console.WriteLine("📚 Endpoint: Obtener cursos del usuario");
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("❌ Usuario no autenticado");
            return Results.Unauthorized();
        }

        var user = await GetUserFromToken(email);
        if (user == null) 
        {
            Console.WriteLine("❌ Usuario no encontrado");
            return Results.NotFound("Usuario no encontrado");
        }

        var tenant = GetTenantFromEmail(email);
        if (tenant == "unknown")
        {
            Console.WriteLine("❌ Tenant no identificado");
            return Results.BadRequest("Tenant no identificado");
        }

        List<CourseInfo> courses;
        if (user.Rol == "Profesor")
        {
            courses = await GetProfessorCourses(supabase, user.Id, tenant);
        }
        else
        {
            courses = await GetStudentCourses(supabase, user.Id, tenant);
        }

        return Results.Ok(new { 
            cursos = courses,
            userRole = user.Rol
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error obteniendo cursos: {ex.Message}");
        return Results.Problem("Error interno del servidor");
    }
}).RequireAuthorization();

// 🔹 ENDPOINTS DE DIAGNÓSTICO
app.MapGet("/api/debug/supabase", async ([FromServices] Client supabase) =>
{
    try
    {
        Console.WriteLine("🔍 Probando conexión a Supabase...");
        
        if (supabase == null)
        {
            return Results.Problem("Cliente Supabase es null");
        }

        var responseUcb = await supabase.From<AssignmentUcb>().Limit(1).Get();
        var responseUpb = await supabase.From<AssignmentUpb>().Limit(1).Get();
        var responseGmail = await supabase.From<AssignmentGmail>().Limit(1).Get();
        
        return Results.Ok(new { 
            status = "Conectado",
            ucb_count = responseUcb.Models?.Count ?? 0,
            upb_count = responseUpb.Models?.Count ?? 0,
            gmail_count = responseGmail.Models?.Count ?? 0
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error en diagnóstico: {ex.Message}");
        return Results.Problem($"Error: {ex.Message}");
    }
});

app.MapGet("/api/debug/assignments-detailed", async ([FromServices] Client supabase) =>
{
    try
    {
        Console.WriteLine("🔍 Diagnóstico detallado de tareas...");
        
        var assignmentsUcb = await supabase.From<AssignmentUcb>().Get();
        var assignmentsUpb = await supabase.From<AssignmentUpb>().Get();
        var assignmentsGmail = await supabase.From<AssignmentGmail>().Get();
        
        // Verificar estructura de tablas
        var sampleUcb = assignmentsUcb.Models.FirstOrDefault();
        var sampleUpb = assignmentsUpb.Models.FirstOrDefault();
        var sampleGmail = assignmentsGmail.Models.FirstOrDefault();
        
        return Results.Ok(new {
            status = "Conectado",
            tables = new {
                ucb = new {
                    count = assignmentsUcb.Models.Count,
                    columns = sampleUcb != null ? GetColumnNames(sampleUcb) : new List<string> { "No data" }
                },
                upb = new {
                    count = assignmentsUpb.Models.Count,
                    columns = sampleUpb != null ? GetColumnNames(sampleUpb) : new List<string> { "No data" }
                },
                gmail = new {
                    count = assignmentsGmail.Models.Count,
                    columns = sampleGmail != null ? GetColumnNames(sampleGmail) : new List<string> { "No data" }
                }
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error en diagnóstico detallado: {ex.Message}");
        return Results.Problem($"Error: {ex.Message}");
    }
});

app.MapGet("/api/debug/courses/{courseId}/assignments", async (HttpContext context, int courseId, [FromServices] Client supabase) =>
{
    Console.WriteLine($"🔍 DIAGNÓSTICO TAREAS CURSO {courseId}");
    
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return Results.Ok(new { error = "No autenticado" });
        }

        var user = await GetUserFromToken(email);
        var tenant = GetTenantFromEmail(email);

        Console.WriteLine($"👤 Usuario: {user?.Id}, Rol: {user?.Rol}, Tenant: {tenant}");

        // 1. Verificar acceso al curso
        var hasAccess = user != null ? await IsUserEnrolledInCourse(supabase, user.Id, courseId, tenant, user.Rol) : false;
        Console.WriteLine($"🔐 Tiene acceso al curso: {hasAccess}");

        // 2. Obtener tareas directamente
        var assignments = await GetCourseAssignments(supabase, courseId, tenant);
        Console.WriteLine($"📝 Tareas encontradas: {assignments.Count}");

        // 3. Verificar curso existe
        var courseName = await GetCourseName(supabase, courseId, tenant);
        Console.WriteLine($"📚 Curso: {courseName} (ID: {courseId})");

        return Results.Ok(new
        {
            user_info = new { id = user?.Id, rol = user?.Rol, email = email },
            course_info = new { id = courseId, name = courseName, exists = courseName != null },
            access_info = new { has_access = hasAccess },
            assignments_info = new
            {
                count = assignments.Count,
                assignments = assignments.Select(a => new
                {
                    id = a.Id,
                    title = a.Title,
                    course_id = a.CursoId,
                    type = a.AssignmentType,
                    due_date = a.DueDate
                })
            },
            tenant_info = tenant
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error en diagnóstico: {ex.Message}");
        return Results.Problem($"Error: {ex.Message}");
    }
}).RequireAuthorization();

// Método helper para obtener nombres de columnas
static List<string> GetColumnNames(object obj)
{
    return obj.GetType().GetProperties().Select(p => p.Name).ToList();
}

app.Run();
// ═══════════════════════════════════════════════════════════════
// 🔹 MODELOS Y RECORDS
// ═══════════════════════════════════════════════════════════════

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
public record AssignmentWithCourseInfo(
    AssignmentInfo Assignment,
    string CursoNombre,
    string CursoCodigo
);
public record CourseInfo(int Id, string Nombre, string Codigo);
public record CompletionInfo(DateTime? CompletedAt, string Status, string? SubmittedContent);
public record CompletionStats(int Total, int Completed);


// ═══════════════════════════════════════════════════════════════
// 🔹 MODELOS SUPABASE
// ═══════════════════════════════════════════════════════════════

// Modelos para UCB
[Table("tenant_ucb_usuarios")]
public class UsuarioUcb : BaseModel
{
    [PrimaryKey("id", true)]
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

[Table("tenant_ucb_cursos")]
public class CursoUcb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("nombre")]
    public string? Nombre { get; set; }
    
    [Column("codigo")]
    public string? Codigo { get; set; }
    
    [Column("profesor_id")]
    public int? ProfesorId { get; set; }
}

[Table("tenant_ucb_assignments")]
public class AssignmentUcb : BaseModel
{
    [PrimaryKey("id", true)]
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
    public string AssignmentType { get; set; } = "tarea";
    
    [Column("curso_id")]
    public int CursoId { get; set; }
    
    [Column("profesor_id")]
    public int ProfesorId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("status")]
    public string Status { get; set; } = "active";
}

[Table("tenant_ucb_assignment_completions")]
public class AssignmentCompletionUcb : BaseModel
{
    [PrimaryKey("id", true)]
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

[Table("tenant_ucb_inscripciones")]
public class InscripcionUcb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("usuario_id")]
    public int UsuarioId { get; set; }
    
    [Column("curso_id")]
    public int CursoId { get; set; }
}

// Modelos para UPB
[Table("tenant_upb_usuarios")]
public class UsuarioUpb : BaseModel
{
    [PrimaryKey("id", true)]
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

[Table("tenant_upb_cursos")]
public class CursoUpb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("nombre")]
    public string? Nombre { get; set; }
    
    [Column("codigo")]
    public string? Codigo { get; set; }
    
    [Column("profesor_id")]
    public int? ProfesorId { get; set; }
}

[Table("tenant_upb_assignments")]
public class AssignmentUpb : BaseModel
{
    [PrimaryKey("id", true)]
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
    public string AssignmentType { get; set; } = "tarea";
    
    [Column("curso_id")]
    public int CursoId { get; set; }
    
    [Column("profesor_id")]
    public int ProfesorId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("status")]
    public string Status { get; set; } = "active";
}

[Table("tenant_upb_assignment_completions")]
public class AssignmentCompletionUpb : BaseModel
{
    [PrimaryKey("id", true)]
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

[Table("tenant_upb_inscripciones")]
public class InscripcionUpb : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("usuario_id")]
    public int UsuarioId { get; set; }
    
    [Column("curso_id")]
    public int CursoId { get; set; }
}

// Modelos para Gmail
[Table("tenant_gmail_usuarios")]
public class UsuarioGmail : BaseModel
{
    [PrimaryKey("id", true)]
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

[Table("tenant_gmail_cursos")]
public class CursoGmail : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("nombre")]
    public string? Nombre { get; set; }
    
    [Column("codigo")]
    public string? Codigo { get; set; }
    
    [Column("profesor_id")]
    public int? ProfesorId { get; set; }
}

[Table("tenant_gmail_assignments")]
public class AssignmentGmail : BaseModel
{
    [PrimaryKey("id", true)]
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
    public string AssignmentType { get; set; } = "tarea";
    
    [Column("curso_id")]
    public int CursoId { get; set; }
    
    [Column("profesor_id")]
    public int ProfesorId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("status")]
    public string Status { get; set; } = "active";
}

[Table("tenant_gmail_assignment_completions")]
public class AssignmentCompletionGmail : BaseModel
{
    [PrimaryKey("id", true)]
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

[Table("tenant_gmail_inscripciones")]
public class InscripcionGmail : BaseModel
{
    [PrimaryKey("id", true)]
    public int Id { get; set; }
    
    [Column("usuario_id")]
    public int UsuarioId { get; set; }
    
    [Column("curso_id")]
    public int CursoId { get; set; }
}
