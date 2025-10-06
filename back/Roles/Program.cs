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

// CORS específico para tus puertos
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

// Función para obtener el tenant del email (sin public)
static string GetTenantFromEmail(string email)
{
    if (email.EndsWith("@ucb.edu.bo")) return "ucb.edu.bo";
    if (email.EndsWith("@upb.edu.bo")) return "upb.edu.bo";
    if (email.EndsWith("@gmail.com")) return "gmail.com";
    return "unknown";
}

// Endpoint PRINCIPAL - Obtiene usuarios del tenant del usuario autenticado
app.MapGet("/api/usuarios/mi-tenant", async (HttpContext context, [FromServices] Client supabase) =>
{
    try
    {
        // Obtener el email del usuario del header (debes enviarlo desde el frontend)
        if (!context.Request.Headers.TryGetValue("X-User-Email", out var userEmail) || string.IsNullOrEmpty(userEmail))
        {
            return Results.BadRequest("Email de usuario no proporcionado");
        }

        var tenant = GetTenantFromEmail(userEmail);
        Console.WriteLine($"🔍 Obteniendo usuarios para tenant: {tenant} (usuario: {userEmail})");
        
        var usuarios = new List<Usuario>();
        
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var responseUcb = await supabase.From<UsuarioUcb>().Select("*").Get();
                usuarios = responseUcb.Models?.Select(u => new Usuario
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Email = u.Email,
                    Rol = u.Rol ?? "Estudiante"
                }).ToList() ?? new List<Usuario>();
                break;
                
            case "upb.edu.bo":
                var responseUpb = await supabase.From<UsuarioUpb>().Select("*").Get();
                usuarios = responseUpb.Models?.Select(u => new Usuario
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Email = u.Email,
                    Rol = u.Rol ?? "Estudiante"
                }).ToList() ?? new List<Usuario>();
                break;
                
            case "gmail.com":
                var responseGmail = await supabase.From<UsuarioGmail>().Select("*").Get();
                usuarios = responseGmail.Models?.Select(u => new Usuario
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Email = u.Email,
                    Rol = u.Rol ?? "Estudiante"
                }).ToList() ?? new List<Usuario>();
                break;
                
            default:
                return Results.BadRequest($"Tenant no válido para el email: {userEmail}");
        }

        Console.WriteLine($"✅ {tenant}: {usuarios.Count} usuarios encontrados");
        return Results.Ok(new { 
            usuarios,
            tenant,
            total = usuarios.Count
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error obteniendo usuarios: {ex.Message}");
        return Results.Problem($"Error interno: {ex.Message}");
    }
});

// Endpoint para obtener tenant desde email
app.MapGet("/api/usuarios/tenant-from-email/{email}", (string email) =>
{
    try
    {
        var tenant = GetTenantFromEmail(email);
        return Results.Ok(new { email, tenant });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error obteniendo tenant: {ex.Message}");
        return Results.Problem($"Error interno: {ex.Message}");
    }
});

// Endpoint para actualizar rol en el tenant del usuario
app.MapPut("/api/usuarios/{id}/rol", async (HttpContext context, int id, [FromBody] ActualizarRolRequest request, [FromServices] Client supabase) =>
{
    try
    {
        // Obtener el email del usuario del header
        if (!context.Request.Headers.TryGetValue("X-User-Email", out var userEmail) || string.IsNullOrEmpty(userEmail))
        {
            return Results.BadRequest("Email de usuario no proporcionado");
        }

        var tenant = GetTenantFromEmail(userEmail);
        Console.WriteLine($"🔧 Actualizando rol en tenant {tenant} para usuario {id}");
        
        // Validar que el rol sea uno de los permitidos
        var rolesPermitidos = new[] { "Estudiante", "Profesor", "Director" };
        if (!rolesPermitidos.Contains(request.Rol))
        {
            return Results.BadRequest($"Rol no válido. Los roles permitidos son: {string.Join(", ", rolesPermitidos)}");
        }

        // Actualizar según el tenant
        object? response = null;
        // Actualizar según el tenant
switch (tenant.ToLower())
{
    case "ucb.edu.bo":
        var responseUcb = await supabase.From<UsuarioUcb>()
            .Where(x => x.Id == id)
            .Set(x => x.Rol!, request.Rol)
            .Update();
        if (responseUcb.Models?.Count > 0)
        {
            Console.WriteLine($"✅ Rol actualizado: Usuario {id} en {tenant} ahora es {request.Rol}");
            return Results.Ok(new { 
                mensaje = "Rol actualizado correctamente", 
                usuarioId = id, 
                tenant = tenant,
                nuevoRol = request.Rol 
            });
        }
        break;
        
    case "upb.edu.bo":
        var responseUpb = await supabase.From<UsuarioUpb>()
            .Where(x => x.Id == id)
            .Set(x => x.Rol!, request.Rol)
            .Update();
        if (responseUpb.Models?.Count > 0)
        {
            Console.WriteLine($"✅ Rol actualizado: Usuario {id} en {tenant} ahora es {request.Rol}");
            return Results.Ok(new { 
                mensaje = "Rol actualizado correctamente", 
                usuarioId = id, 
                tenant = tenant,
                nuevoRol = request.Rol 
            });
        }
        break;
        
    case "gmail.com":
        var responseGmail = await supabase.From<UsuarioGmail>()
            .Where(x => x.Id == id)
            .Set(x => x.Rol!, request.Rol)
            .Update();
        if (responseGmail.Models?.Count > 0)
        {
            Console.WriteLine($"✅ Rol actualizado: Usuario {id} en {tenant} ahora es {request.Rol}");
            return Results.Ok(new { 
                mensaje = "Rol actualizado correctamente", 
                usuarioId = id, 
                tenant = tenant,
                nuevoRol = request.Rol 
            });
        }
        break;
}

return Results.NotFound("Usuario no encontrado");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error actualizando rol: {ex.Message}");
        return Results.Problem($"Error interno: {ex.Message}");
    }
});

// Endpoint para obtener los roles disponibles
app.MapGet("/api/roles", () =>
{
    var roles = new[] { "Estudiante", "Profesor", "Director" };
    return Results.Ok(roles);
});

// Endpoints de health check
app.MapGet("/health", () => 
{
    Console.WriteLine("🔍 Health check solicitado");
    return Results.Ok(new { status = "Healthy", service = "Roles", timestamp = DateTime.Now });
});

app.MapGet("/test", () => 
{
    Console.WriteLine("✅ Test endpoint funcionando");
    return Results.Ok(new { message = "Backend funcionando correctamente", status = "OK" });
});
// Endpoint para crear usuario en el tenant actual
// Endpoint para crear usuario en el tenant actual
app.MapPost("/api/crear", async (HttpContext context, [FromBody] CrearUsuarioRequest request, [FromServices] Client supabase) =>
{
    try
    {
        // Obtener el email del usuario del header
        if (!context.Request.Headers.TryGetValue("X-User-Email", out var userEmail) || string.IsNullOrEmpty(userEmail))
        {
            return Results.BadRequest("Email de usuario no proporcionado");
        }

        var tenant = GetTenantFromEmail(userEmail);
        Console.WriteLine($"👤 Creando usuario en tenant: {tenant}");

        // Validar campos requeridos
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Nombre) || string.IsNullOrEmpty(request.Apellido))
        {
            return Results.BadRequest("Todos los campos son requeridos");
        }

        // Verificar que el email pertenezca al mismo tenant
        var nuevoUsuarioTenant = GetTenantFromEmail(request.Email);
        if (nuevoUsuarioTenant != tenant)
        {
            return Results.BadRequest($"El email debe pertenecer al dominio de {tenant}");
        }

        // Crear usuario según el tenant
        switch (tenant.ToLower())
        {
            case "ucb.edu.bo":
                var nuevoUcb = new UsuarioUcb
                {
                    Nombre = request.Nombre,
                    Apellido = request.Apellido,
                    Email = request.Email,
                    Rol = request.Rol ?? "Estudiante"
                };
                var responseUcb = await supabase.From<UsuarioUcb>().Insert(nuevoUcb);
                if (responseUcb.Models?.Count > 0)
                {
                    var usuarioCreado = responseUcb.Models[0];
                    // Devolver un DTO simple en lugar del modelo completo
                    return Results.Ok(new
                    {
                        mensaje = "Usuario creado correctamente",
                        usuario = new
                        {
                            Id = usuarioCreado.Id,
                            Nombre = usuarioCreado.Nombre,
                            Apellido = usuarioCreado.Apellido,
                            Email = usuarioCreado.Email,
                            Rol = usuarioCreado.Rol
                        }
                    });
                }
                break;

            case "upb.edu.bo":
                var nuevoUpb = new UsuarioUpb
                {
                    Nombre = request.Nombre,
                    Apellido = request.Apellido,
                    Email = request.Email,
                    Rol = request.Rol ?? "Estudiante"
                };
                var responseUpb = await supabase.From<UsuarioUpb>().Insert(nuevoUpb);
                if (responseUpb.Models?.Count > 0)
                {
                    var usuarioCreado = responseUpb.Models[0];
                    return Results.Ok(new
                    {
                        mensaje = "Usuario creado correctamente",
                        usuario = new
                        {
                            Id = usuarioCreado.Id,
                            Nombre = usuarioCreado.Nombre,
                            Apellido = usuarioCreado.Apellido,
                            Email = usuarioCreado.Email,
                            Rol = usuarioCreado.Rol
                        }
                    });
                }
                break;

            case "gmail.com":
                var nuevoGmail = new UsuarioGmail
                {
                    Nombre = request.Nombre,
                    Apellido = request.Apellido,
                    Email = request.Email,
                    Rol = request.Rol ?? "Estudiante"
                };
                var responseGmail = await supabase.From<UsuarioGmail>().Insert(nuevoGmail);
                if (responseGmail.Models?.Count > 0)
                {
                    var usuarioCreado = responseGmail.Models[0];
                    return Results.Ok(new
                    {
                        mensaje = "Usuario creado correctamente",
                        usuario = new
                        {
                            Id = usuarioCreado.Id,
                            Nombre = usuarioCreado.Nombre,
                            Apellido = usuarioCreado.Apellido,
                            Email = usuarioCreado.Email,
                            Rol = usuarioCreado.Rol
                        }
                    });
                }
                break;
        }

        return Results.Problem("Error al crear el usuario");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error creando usuario: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        return Results.Problem($"Error interno: {ex.Message}");
    }
});
app.Run();
public class CrearUsuarioRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Rol { get; set; }
}

// Modelos para respuesta
public class Usuario
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Email { get; set; }
    public string? Rol { get; set; }
}

public class ActualizarRolRequest
{
    public string Rol { get; set; } = string.Empty;
}

// Modelos para cada tenant
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
