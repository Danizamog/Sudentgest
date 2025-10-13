using Supabase;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// habilitar CORS para desarrollo
var frontOrigin = Environment.GetEnvironmentVariable("FRONT_ORIGIN") ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFront",
        policy => policy.WithOrigins(frontOrigin)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});

// Cargar variables del archivo .env
Env.Load();

// Add services to the container.
builder.Services.AddOpenApi();

// Configurar Supabase desde variables de entorno
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY")
                  ?? Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")
                  ?? Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");

if (!string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseKey))
{
    builder.Services.AddScoped(provider =>
    {
        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        };
        
        return new Client(supabaseUrl, supabaseKey, options);
    });
    
    var keyName = Environment.GetEnvironmentVariable("SUPABASE_KEY") != null
                  ? "SUPABASE_KEY"
                  : Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY") != null
                    ? "SUPABASE_SERVICE_ROLE_KEY"
                    : "SUPABASE_ANON_KEY";

    Console.WriteLine($"Supabase configurado correctamente (url={supabaseUrl}, keyVarUsed={keyName})");
}
else
{
    Console.WriteLine("ADVERTENCIA: No se encontraron las variables de Supabase en .env");
}

var app = builder.Build();

// usar CORS antes de los endpoints
app.UseCors("AllowFront");

app.UseHttpsRedirection();

//
// Endpoints para la nueva base de datos multi-tenant
//

// DTOs para creación
public class ThreadCreateDto
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? UserId { get; set; } // debe venir desde cliente (o auth) en un escenario real
    public string[]? Tags { get; set; }
    public string Visibility { get; set; } = "PUBLIC";
    public Guid? TenantTypeId { get; set; }
}

public class ReplyCreateDto
{
    public string? Content { get; set; }
    public Guid? UserId { get; set; } // en producción tomar desde auth
    public Guid? TenantTypeId { get; set; }
}

// Modelos que reflejan las columnas de la nueva DB
public class Thread : Supabase.Postgrest.Models.BaseModel
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Excerpt { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? UserId { get; set; }
    public string[]? Tags { get; set; }
    public bool IsPinned { get; set; }
    public int Views { get; set; }
    public int ReplyCount { get; set; }
    public DateTimeOffset LastActivity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? Visibility { get; set; }
    public Guid? TenantTypeId { get; set; }
}

public class Reply : Supabase.Postgrest.Models.BaseModel
{
    public Guid Id { get; set; }
    public string? Content { get; set; }
    public Guid ThreadId { get; set; }
    public Guid UserId { get; set; }
    public int Likes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? TenantTypeId { get; set; }
}

public class Category : Supabase.Postgrest.Models.BaseModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public Guid? TenantTypeId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class Profile : Supabase.Postgrest.Models.BaseModel
{
    public Guid Id { get; set; } // corresponde a auth.users.id
    public string? FullName { get; set; }
    public string? Role { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid? TenantTypeId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class TenantType : Supabase.Postgrest.Models.BaseModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public System.Text.Json.JsonElement? Permissions { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// GET /categories
app.MapGet("/categories", async (Client supabase) =>
{
    try
    {
        var resp = await supabase
            .From<Category>()
            .Select("*")
            .Order("created_at", Supabase.Postgrest.Constants.OrderType.Descending)
            .Get();

        return Results.Ok(resp.Models);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error al obtener categories: {ex.Message}");
    }
})
.WithName("GetCategories");

// GET /threads
app.MapGet("/threads", async (Client supabase) =>
{
    try
    {
        var resp = await supabase
            .From<Thread>()
            .Select("*")
            .Order("last_activity", Supabase.Postgrest.Constants.OrderType.Descending)
            .Get();

        return Results.Ok(resp.Models);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error al obtener threads: {ex.Message}");
    }
})
.WithName("GetThreads");

// POST /threads
app.MapPost("/threads", async (Client supabase, ThreadCreateDto dto) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content) || dto.UserId == null)
            return Results.BadRequest("title, content y userId son requeridos");

        var newThread = new Thread
        {
            Title = dto.Title,
            Content = dto.Content,
            Excerpt = dto.Content.Length > 200 ? dto.Content.Substring(0, 200) + "..." : dto.Content,
            CategoryId = dto.CategoryId,
            UserId = dto.UserId,
            Tags = dto.Tags ?? Array.Empty<string>(),
            IsPinned = false,
            Views = 0,
            ReplyCount = 0,
            LastActivity = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            Visibility = dto.Visibility,
            TenantTypeId = dto.TenantTypeId
        };

        var resp = await supabase
            .From<Thread>()
            .Insert(newThread)
            .Single();

        return Results.Created($"/threads/{resp.Models.FirstOrDefault()?.Id}", resp.Models.FirstOrDefault());
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error al crear thread: {ex.Message}");
    }
})
.WithName("CreateThread");

// GET /threads/{id}/replies
app.MapGet("/threads/{id:guid}/replies", async (Client supabase, Guid id) =>
{
    try
    {
        var resp = await supabase
            .From<Reply>()
            .Select("*")
            .Eq("thread_id", id)
            .Order("created_at", Supabase.Postgrest.Constants.OrderType.Ascending)
            .Get();

        return Results.Ok(resp.Models);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error al obtener replies: {ex.Message}");
    }
})
.WithName("GetReplies");

// POST /threads/{id}/replies
app.MapPost("/threads/{id:guid}/replies", async (Client supabase, Guid id, ReplyCreateDto dto) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(dto.Content) || dto.UserId == null)
            return Results.BadRequest("content y userId son requeridos");

        var newReply = new Reply
        {
            Content = dto.Content,
            ThreadId = id,
            UserId = dto.UserId.Value,
            Likes = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            TenantTypeId = dto.TenantTypeId
        };

        var insertResp = await supabase
            .From<Reply>()
            .Insert(newReply)
            .Single();

        // Actualizar counters en thread (views/reply_count/last_activity)
        try
        {
            // Intentamos incrementar reply_count y actualizar last_activity (no crítico si falla)
            await supabase
                .From<Thread>()
                .Update(new { reply_count = Supabase.Postgrest.Transformers.Numeric.Add(1), last_activity = DateTimeOffset.UtcNow })
                .Eq("id", id)
                .Execute();
        }
        catch
        {
            // ignorar error secundario
        }

        return Results.Created($"/threads/{id}/replies/{insertResp.Models.FirstOrDefault()?.Id}", insertResp.Models.FirstOrDefault());
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error al crear reply: {ex.Message}");
    }
})
.WithName("CreateReply");

app.Run();

// Modelos auxiliares / record mínimos (ya declarados arriba)
public class Post : Supabase.Postgrest.Models.BaseModel
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Author { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class User : Supabase.Postgrest.Models.BaseModel
{
    public int Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}