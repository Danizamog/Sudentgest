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

// 🔹 DEBUG: Verificar configuración
Console.WriteLine("🔹 ===========================================");
Console.WriteLine("🔹 VERIFICACIÓN CREDENCIALES SUPABASE");
Console.WriteLine("🔹 ===========================================");
Console.WriteLine($"🔹 SUPABASE_URL: {supabaseUrl}");
Console.WriteLine($"🔹 SUPABASE_ANON_KEY length: {supabaseAnonKey?.Length}");
Console.WriteLine($"🔹 SUPABASE_SERVICE_ROLE_KEY length: {supabaseServiceRoleKey?.Length}");
Console.WriteLine($"🔹 SUPABASE_JWT_SECRET length: {supabaseJwtSecret?.Length}");
Console.WriteLine("🔹 ===========================================");

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

// 🔹 FUNCIÓN CORREGIDA: Usar AMBOS headers (apikey Y Authorization)
// 🔹 FUNCIÓN CORREGIDA: Separar nombre y apellido según especificaciones
async Task<bool> CreateUserViaSupabaseAPI(string email, string fullName, string schema)
{
    try
    {
        using var httpClient = new HttpClient();
        
        // Headers
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseServiceRoleKey);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseServiceRoleKey}");
        httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

        // 🔹 USAR FUNCIÓN CORREGIDA
        var (nombre, apellido) = SplitFullName(fullName);

        // Crear objeto de usuario
        var userData = new
        {
            nombre = nombre,
            apellido = apellido,
            email = email,
            rol = "estudiante",
            created_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };

        // Tabla name basada en el schema
        var tableName = $"{schema}_usuarios";
        
        Console.WriteLine($"🔹 Creando usuario - Nombre: '{nombre}' | Apellido: '{apellido}'");

        var jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var jsonContent = JsonSerializer.Serialize(userData, jsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync($"{supabaseUrl}/rest/v1/{tableName}", content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"✅ Usuario creado exitosamente: {email}");
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
        Console.WriteLine($"❌ Exception en Supabase API: {ex.Message}");
        return false;
    }
}

// 🔹 FUNCIÓN MEJORADA: Según tus especificaciones exactas
(string nombre, string apellido) SplitFullName(string fullName)
{
    if (string.IsNullOrWhiteSpace(fullName))
        return ("Usuario", "");

    var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    
    Console.WriteLine($"🔹 SplitFullName - Partes: {parts.Length}, Original: '{fullName}'");
    
    switch (parts.Length)
    {
        case 0:
            return ("Usuario", "");
        case 1:
            // 1 parte: "Daniel" → Nombre: "Daniel", Apellido: ""
            return (parts[0], "");
        case 2:
            // 2 partes: "Daniel Zamorano" → Nombre: "Daniel", Apellido: "Zamorano"
            return (parts[0], parts[1]);
        case 3:
            // 3 partes: "Daniel Alejandro Zamorano" → Nombre: "Daniel", Apellido: "Zamorano"
            return (parts[0], parts[2]);
        case 4:
            // 4 partes: "Daniel Alejandro Zamorano Gamarra" → Nombre: "Daniel Alejandro", Apellido: "Zamorano Gamarra"
            return ($"{parts[0]} {parts[1]}", $"{parts[2]} {parts[3]}");
        default:
            // 5+ partes: "Daniel Alejandro Zamorano Gamarra Lopez" → Nombre: "Daniel Alejandro", Apellido: "Zamorano"
            return ($"{parts[0]} {parts[1]}", parts[2]);
    }
}
// 🔹 FUNCIÓN CORREGIDA: CheckUserExists con AMBOS headers
async Task<bool> CheckUserExists(string email, string schema)
{
    try
    {
        using var httpClient = new HttpClient();
        // 🔹 CORRECCIÓN: Usar AMBOS headers
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseServiceRoleKey);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseServiceRoleKey}");

        var tableName = $"{schema}_usuarios";
        var encodedEmail = Uri.EscapeDataString(email);
        var url = $"{supabaseUrl}/rest/v1/{tableName}?email=eq.{encodedEmail}&select=id";
        
        Console.WriteLine($"🔹 Verificando usuario en: {url}");
        
        var response = await httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"🔹 Respuesta check exists: {content}");
            var users = JsonSerializer.Deserialize<JsonElement[]>(content);
            return users?.Length > 0;
        }
        else
        {
            Console.WriteLine($"❌ Error verificando usuario. Status: {response.StatusCode}");
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"❌ Error details: {errorContent}");
            return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Exception verificando usuario: {ex.Message}");
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

// 🔹 Endpoint para obtener perfil de usuario
// 🔹 Endpoint para obtener perfil de usuario
app.MapGet("/api/auth/user-profile", async (HttpContext context) =>
{
    try
    {
        Console.WriteLine("🔹 ===========================================");
        Console.WriteLine("🔹 USER-PROFILE ENDPOINT INICIADO");
        Console.WriteLine("🔹 ===========================================");

        var email = context.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        
        Console.WriteLine($"🔹 Email del token: {email}");

        if (string.IsNullOrEmpty(email))
            return Results.Unauthorized();

        var tenant = GetTenantFromEmail(email);
        Console.WriteLine($"🔹 Tenant detectado: {tenant}");

        if (tenant == null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        var tenantInfo = await GetTenantInfo(tenant);
        if (tenantInfo == null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        var (schema, domain) = tenantInfo.Value;
        Console.WriteLine($"🔹 Schema: {schema}");

        // Buscar usuario en la base de datos
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseServiceRoleKey);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseServiceRoleKey}");

        var tableName = $"{schema}_usuarios";
        var encodedEmail = Uri.EscapeDataString(email);
        var url = $"{supabaseUrl}/rest/v1/{tableName}?email=eq.{encodedEmail}&select=*";
        
        Console.WriteLine($"🔹 Buscando usuario en: {url}");

        var response = await httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"🔹 Respuesta de la API: {content}");
            
            var users = JsonSerializer.Deserialize<JsonElement[]>(content);
            
            if (users?.Length > 0)
            {
                var user = users[0];
                var profile = new
                {
                    nombre = user.GetProperty("nombre").GetString() ?? "Usuario",
                    apellido = user.GetProperty("apellido").GetString() ?? "",
                    email = user.GetProperty("email").GetString() ?? email,
                    rol = user.GetProperty("rol").GetString() ?? "estudiante"
                };
                
                Console.WriteLine($"✅ Perfil encontrado: {profile.nombre} - Rol: {profile.rol}");
                Console.WriteLine("🔹 ===========================================");
                
                return Results.Ok(profile);
            }
        }

        Console.WriteLine("❌ Usuario no encontrado en la base de datos");
        Console.WriteLine("🔹 ===========================================");
        return Results.NotFound(new { error = "Usuario no encontrado." });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error obteniendo perfil: {ex.Message}");
        Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
        return Results.Problem("Error interno del servidor.");
    }
})
.RequireAuthorization()
.WithName("GetUserProfile")
.WithOpenApi();

// 🔹 Función helper para obtener tenant del email
string GetTenantFromEmail(string email)
{
    if (email.EndsWith("@ucb.edu.bo")) return "ucb.edu.bo";
    if (email.EndsWith("@upb.edu.bo")) return "upb.edu.bo";
    if (email.EndsWith("@gmail.com")) return "gmail.com";
    return null;
}
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