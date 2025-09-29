using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Cargar .env
DotNetEnv.Env.Load();

// 🔹 Configuración Supabase
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") 
    ?? throw new ArgumentNullException("SUPABASE_URL no configurado");
var supabaseAnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") 
    ?? throw new ArgumentNullException("SUPABASE_ANON_KEY no configurado");
var supabaseJwtSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET") ?? supabaseAnonKey;
var supabaseServiceRoleKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY") 
    ?? throw new ArgumentNullException("SUPABASE_SERVICE_ROLE_KEY no configurado");

// 🔹 JWT Authentication con Supabase
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
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// 🔹 CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://frontend:80")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ═══════════════════════════════════════════════════════════════
// 🔹 FUNCIONES HELPER - USANDO SUPABASE REST API
// ═══════════════════════════════════════════════════════════════

// 🔹 Obtener información del tenant usando Supabase REST API
async Task<(string schema, string domain)?> GetTenantInfo(string domain)
{
    try
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseAnonKey}");

        var response = await httpClient.GetAsync(
            $"{supabaseUrl}/rest/v1/tenants?domain=eq.{domain}&select=*"
        );

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var tenants = JsonSerializer.Deserialize<JsonElement[]>(content);
            
            if (tenants?.Length > 0)
            {
                var tenant = tenants[0];
                var schema = tenant.GetProperty("schema_name").GetString();
                var tenantDomain = tenant.GetProperty("domain").GetString();
                
                Console.WriteLine($"✅ Tenant encontrado via API: {schema}");
                return (schema, tenantDomain);
            }
        }
        
        Console.WriteLine($"❌ Tenant no encontrado: {domain}");
        return null;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error obteniendo tenant info: {ex.Message}");
        return null;
    }
}

// 🔹 Crear usuario usando Supabase REST API
// 🔹 FUNCIÓN ACTUALIZADA: Usar SERVICE_ROLE_KEY para bypass RLS
async Task<bool> CreateUserViaSupabaseAPI(string email, string fullName, string schema)
{
    try
    {
        using var httpClient = new HttpClient();
        
        // 🔹 CAMBIAR: Usar SERVICE_ROLE_KEY en lugar de ANON_KEY
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseServiceRoleKey);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseServiceRoleKey}");
        httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

        // Parsear nombre completo
        var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var nombre = parts.Length > 0 ? parts[0] : "Usuario";
        var apellido = parts.Length > 1 ? parts[1] : "";

        // Crear objeto de usuario
        var userData = new
        {
            nombre = nombre,
            apellido = apellido,
            email = email,
            rol = "estudiante"
        };

        // Tabla name basada en el schema
        var tableName = $"{schema}_usuarios";
        var response = await httpClient.PostAsJsonAsync(
            $"{supabaseUrl}/rest/v1/{tableName}",
            userData,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"✅ Usuario creado via API: {email} en {tableName}");
            return true;
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"❌ Error creando usuario: {errorContent}");
            return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error en Supabase API: {ex.Message}");
        return false;
    }
}

// 🔹 También actualiza la función CheckUserExists para usar service role
async Task<bool> CheckUserExists(string email, string schema)
{
    try
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseServiceRoleKey);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseServiceRoleKey}");

        var tableName = $"{schema}_usuarios";
        var response = await httpClient.GetAsync(
            $"{supabaseUrl}/rest/v1/{tableName}?email=eq.{email}&select=id"
        );

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<JsonElement[]>(content);
            return users?.Length > 0;
        }
        
        return false;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error verificando usuario: {ex.Message}");
        return false;
    }
}

// ═══════════════════════════════════════════════════════════════
// 🔹 ENDPOINTS PRINCIPALES
// ═══════════════════════════════════════════════════════════════

app.MapPost("/api/auth/sync-user", async (HttpContext context) =>
{
    Console.WriteLine("🔹 ===========================================");
    Console.WriteLine("🔹 SYNC-USER ENDPOINT INICIADO");
    Console.WriteLine("🔹 ===========================================");

    try
    {
        // Extraer email del token
        var email = context.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

        // 🔹 EXTRAER FULL NAME CORRECTAMENTE
        var fullName = "";
        var userMetadataClaim = context.User.Claims.FirstOrDefault(c => c.Type == "user_metadata")?.Value;
        if (!string.IsNullOrEmpty(userMetadataClaim))
        {
            try
            {
                var userMetadata = JsonDocument.Parse(userMetadataClaim);
                if (userMetadata.RootElement.TryGetProperty("full_name", out var fullNameElement))
                {
                    fullName = fullNameElement.GetString() ?? "";
                }
                else if (userMetadata.RootElement.TryGetProperty("name", out var nameElement))
                {
                    fullName = nameElement.GetString() ?? "";
                }
            }
            catch (JsonException)
            {
                fullName = "Usuario";
            }
        }

        Console.WriteLine($"🔹 Email: {email}");
        Console.WriteLine($"🔹 FullName: {fullName}");

        if (string.IsNullOrEmpty(email))
            return Results.BadRequest(new { error = "Email no encontrado en el token." });

        // Leer tenant del body
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        Console.WriteLine($"🔹 Body recibido: {body}");

        var json = JsonDocument.Parse(body);

        if (!json.RootElement.TryGetProperty("tenant", out var tenantElement))
            return Results.BadRequest(new { error = "Propiedad 'tenant' no encontrada." });

        var tenantDomain = tenantElement.GetString();
        if (string.IsNullOrEmpty(tenantDomain))
            return Results.BadRequest(new { error = "Tenant requerido." });

        Console.WriteLine($"🔹 Tenant Domain: {tenantDomain}");

        // Obtener información del tenant
        var tenantInfo = await GetTenantInfo(tenantDomain);
        if (tenantInfo == null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        var (schema, domain) = tenantInfo.Value;
        Console.WriteLine($"🔹 Schema: {schema}");

        // Verificar si usuario existe
        Console.WriteLine($"🔹 Verificando si usuario existe: {email} en {schema}");
        var userExists = await CheckUserExists(email, schema);
        Console.WriteLine($"🔹 Usuario existe: {userExists}");

        if (!userExists)
        {
            Console.WriteLine($"🔹 Creando nuevo usuario...");
            // Crear usuario
            var success = await CreateUserViaSupabaseAPI(email, fullName, schema);

            if (success)
            {
                Console.WriteLine($"✅ USUARIO CREADO EXITOSAMENTE");
                return Results.Ok(new
                {
                    success = true,
                    message = "Usuario creado exitosamente",
                    email,
                    schema,
                    isNewUser = true
                });
            }
            else
            {
                Console.WriteLine($"❌ ERROR al crear usuario");
                return Results.Problem("Error al crear usuario en la base de datos.");
            }
        }
        else
        {
            Console.WriteLine($"ℹ️ Usuario ya existe, no se crea nuevo");
            return Results.Ok(new
            {
                success = true,
                message = "Usuario ya existe",
                email,
                schema,
                isNewUser = false
            });
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error en sync-user: {ex.Message}");
        Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
        return Results.Problem("Error interno del servidor.");
    }
    finally
    {
        Console.WriteLine("🔹 ===========================================");
        Console.WriteLine("🔹 SYNC-USER ENDPOINT FINALIZADO");
        Console.WriteLine("🔹 ===========================================");
    }
})
.RequireAuthorization()
.WithName("SyncUser")
.WithOpenApi();


// 🔹 Endpoint para obtener usuarios de un tenant
app.MapGet("/api/usuarios/{tenantDomain}", async (string tenantDomain) =>
{
    try
    {
        var tenantInfo = await GetTenantInfo(tenantDomain);
        if (tenantInfo == null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        var (schema, domain) = tenantInfo.Value;

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseAnonKey}");
        var tableName = $"{schema}_usuarios";
        var response = await httpClient.GetAsync(
            $"{supabaseUrl}/rest/v1/{tableName}?select=*&order=created_at.desc"
        );

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var usuarios = JsonSerializer.Deserialize<JsonElement[]>(content) ?? Array.Empty<JsonElement>();
            
            return Results.Ok(new
            {
                tenant = schema,
                total = usuarios.Length,
                usuarios
            });
        }
        else
        {
            return Results.Problem("Error al obtener usuarios.");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error obteniendo usuarios: {ex.Message}");
        return Results.Problem("Error al obtener usuarios.");
    }
})
.WithName("GetUsuariosByTenant")
.WithOpenApi();

// Endpoints básicos
app.MapGet("/", () => "API Multi-tenant con Supabase REST API 🚀");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

Console.WriteLine("🚀 API Multi-tenant con Supabase REST API iniciada correctamente");
app.Run();