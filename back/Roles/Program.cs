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
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
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

app.UseCors("AllowAll");
app.UseAuthorization();



// Endpoints GET (para obtener usuarios reales de Supabase)
app.MapGet("/api/usuarios/ucb", async ([FromServices] Client supabase) =>
{
    try
    {
        Console.WriteLine("🔍 Obteniendo usuarios UCB reales de Supabase...");
        
        var response = await supabase
            .From<TempModel>()
            .Select("*")
            .Get();

        var usuarios = new List<Usuario>();
        
        if (response.Models != null)
        {
            foreach (var tempUser in response.Models)
            {
                var usuario = new Usuario
                {
                    Id = tempUser.Id,
                    Nombre = tempUser.Nombre,
                    Apellido = tempUser.Apellido,
                    Email = tempUser.Email,
                    Rol = tempUser.Rol ?? "Estudiante"
                };
                usuarios.Add(usuario);
            }
        }

        Console.WriteLine($"✅ UCB: {usuarios.Count} usuarios reales encontrados");
        return Results.Ok(usuarios);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error UCB: {ex.Message}");
        Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
        return Results.Problem($"Error interno: {ex.Message}");
    }
});

app.MapGet("/api/usuarios/upb", async ([FromServices] Client supabase) =>
{
    try
    {
        Console.WriteLine("🔍 Obteniendo usuarios UPB reales de Supabase...");
        
        var response = await supabase
            .From<TempModel>()
            .Select("*")
            .Get();

        var usuarios = new List<Usuario>();
        
        if (response.Models != null)
        {
            foreach (var tempUser in response.Models)
            {
                var usuario = new Usuario
                {
                    Id = tempUser.Id,
                    Nombre = tempUser.Nombre,
                    Apellido = tempUser.Apellido,
                    Email = tempUser.Email,
                    Rol = tempUser.Rol ?? "Estudiante"
                };
                usuarios.Add(usuario);
            }
        }

        Console.WriteLine($"✅ UPB: {usuarios.Count} usuarios reales encontrados");
        return Results.Ok(usuarios);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error UPB: {ex.Message}");
        return Results.Problem($"Error interno: {ex.Message}");
    }
});

app.MapGet("/api/usuarios/gmail", async ([FromServices] Client supabase) =>
{
    try
    {
        Console.WriteLine("🔍 Obteniendo usuarios Gmail reales de Supabase...");
        
        var response = await supabase
            .From<TempModel>()
            .Select("*")
            .Get();

        var usuarios = new List<Usuario>();
        
        if (response.Models != null)
        {
            foreach (var tempUser in response.Models)
            {
                var usuario = new Usuario
                {
                    Id = tempUser.Id,
                    Nombre = tempUser.Nombre,
                    Apellido = tempUser.Apellido,
                    Email = tempUser.Email,
                    Rol = tempUser.Rol ?? "Estudiante"
                };
                usuarios.Add(usuario);
            }
        }

        Console.WriteLine($"✅ Gmail: {usuarios.Count} usuarios reales encontrados");
        return Results.Ok(usuarios);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error Gmail: {ex.Message}");
        return Results.Problem($"Error interno: {ex.Message}");
    }
});

app.MapGet("/api/usuarios/todas-tablas", async ([FromServices] Client supabase) =>
{
    try
    {
        Console.WriteLine("🔍 Obteniendo todas las tablas reales de Supabase...");
        
        var ucbTask = supabase.From<TempModel>().Select("*").Get();
        var upbTask = supabase.From<TempModel>().Select("*").Get();
        var gmailTask = supabase.From<TempModel>().Select("*").Get();

        await Task.WhenAll(ucbTask, upbTask, gmailTask);

        List<Usuario> ConvertirUsuarios(List<TempModel> models)
        {
            var usuarios = new List<Usuario>();
            if (models != null)
            {
                foreach (var tempUser in models)
                {
                    var usuario = new Usuario
                    {
                        Id = tempUser.Id,
                        Nombre = tempUser.Nombre,
                        Apellido = tempUser.Apellido,
                        Email = tempUser.Email,
                        Rol = tempUser.Rol ?? "Estudiante"
                    };
                    usuarios.Add(usuario);
                }
            }
            return usuarios;
        }

        var resultado = new
        {
            ucbUsuarios = ConvertirUsuarios(ucbTask.Result.Models),
            upbUsuarios = ConvertirUsuarios(upbTask.Result.Models),
            gmailUsuarios = ConvertirUsuarios(gmailTask.Result.Models)
        };

        Console.WriteLine($"✅ Todas tablas: UCB={resultado.ucbUsuarios.Count}, UPB={resultado.upbUsuarios.Count}, Gmail={resultado.gmailUsuarios.Count}");
        return Results.Ok(resultado);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error todas las tablas: {ex.Message}");
        return Results.Problem($"Error interno: {ex.Message}");
    }
});

// Endpoints PUT/PATCH para modificar roles en Supabase
app.MapPut("/api/usuarios/{tabla}/{id}/rol", async (string tabla, int id, [FromBody] ActualizarRolRequest request, [FromServices] Client supabase) =>
{
    try
    {
        Console.WriteLine($"🔧 Actualizando rol en tabla {tabla} para usuario {id}");
        
        // Validar que el rol sea uno de los permitidos
        var rolesPermitidos = new[] { "Estudiante", "Profesor", "Director" };
        if (!rolesPermitidos.Contains(request.Rol))
        {
            return Results.BadRequest($"Rol no válido. Los roles permitidos son: {string.Join(", ", rolesPermitidos)}");
        }

        // Actualizar en Supabase
        var response = await supabase
            .From<TempModel>()
            .Where(x => x.Id == id)
            .Set(x => x.Rol!, request.Rol)
            .Update();

        if (response.Models != null && response.Models.Count > 0)
        {
            Console.WriteLine($"✅ Rol actualizado en Supabase: Usuario {id} en {tabla} ahora es {request.Rol}");
            
            return Results.Ok(new { 
                mensaje = "Rol actualizado correctamente", 
                usuarioId = id, 
                tabla = tabla,
                nuevoRol = request.Rol 
            });
        }
        else
        {
            return Results.Problem("Error al actualizar el rol en la base de datos");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error actualizando rol: {ex.Message}");
        return Results.Problem($"Error interno: {ex.Message}");
    }
});

app.MapPatch("/api/usuarios/{tabla}/{id}/rol", async (string tabla, int id, [FromBody] ActualizarRolRequest request, [FromServices] Client supabase) =>
{
    try
    {
        Console.WriteLine($"🔧 Actualizando rol (PATCH) en tabla {tabla} para usuario {id}");
        
        // Validar que el rol sea uno de los permitidos
        var rolesPermitidos = new[] { "Estudiante", "Profesor", "Director" };
        if (!rolesPermitidos.Contains(request.Rol))
        {
            return Results.BadRequest($"Rol no válido. Los roles permitidos son: {string.Join(", ", rolesPermitidos)}");
        }

        // Actualizar en Supabase
        var response = await supabase
            .From<TempModel>()
            .Where(x => x.Id == id)
            .Set(x => x.Rol!, request.Rol)
            .Update();

        if (response.Models != null && response.Models.Count > 0)
        {
            Console.WriteLine($"✅ Rol actualizado en Supabase: Usuario {id} en {tabla} ahora es {request.Rol}");
            
            return Results.Ok(new { 
                mensaje = "Rol actualizado correctamente", 
                usuarioId = id, 
                tabla = tabla,
                nuevoRol = request.Rol 
            });
        }
        else
        {
            return Results.Problem("Error al actualizar el rol en la base de datos");
        }
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

// Endpoint de prueba simple
app.MapGet("/test", () => 
{
    Console.WriteLine("✅ Test endpoint funcionando");
    return Results.Ok(new { message = "Backend funcionando correctamente", status = "OK" });
});

app.MapGet("/health", () => 
{
    Console.WriteLine("🔍 Health check solicitado");
    return Results.Ok(new { status = "Healthy", service = "Roles", timestamp = DateTime.Now });
});

app.Run();

// Modelos al final
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
}// Modelo temporal para Supabase (debe heredar de BaseModel)
[Table("tenant_ucb_usuarios")]
public class TempModel : BaseModel
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