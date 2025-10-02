using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Cargar .env
DotNetEnv.Env.Load();

// 🔹 Configuración Supabase para GMAIL
var supabaseUrlGmail = Environment.GetEnvironmentVariable("SUPABASE_URL_GMAIL") 
    ?? throw new ArgumentNullException("SUPABASE_URL_GMAIL no configurado");
var supabaseAnonKeyGmail = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY_GMAIL") 
    ?? throw new ArgumentNullException("SUPABASE_ANON_KEY_GMAIL no configurado");
var supabaseJwtSecretGmail = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET_GMAIL") 
    ?? throw new ArgumentNullException("SUPABASE_JWT_SECRET_GMAIL no configurado");
var supabaseServiceRoleKeyGmail = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY_GMAIL") 
    ?? throw new ArgumentNullException("SUPABASE_SERVICE_ROLE_KEY_GMAIL no configurado");

// 🔹 Configuración Supabase para UCB
var supabaseUrlUcb = Environment.GetEnvironmentVariable("SUPABASE_URL_UCB") 
    ?? throw new ArgumentNullException("SUPABASE_URL_UCB no configurado");
var supabaseAnonKeyUcb = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY_UCB") 
    ?? throw new ArgumentNullException("SUPABASE_ANON_KEY_UCB no configurado");
var supabaseJwtSecretUcb = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET_UCB") 
    ?? throw new ArgumentNullException("SUPABASE_JWT_SECRET_UCB no configurado");
var supabaseServiceRoleKeyUcb = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY_UCB") 
    ?? throw new ArgumentNullException("SUPABASE_SERVICE_ROLE_KEY_UCB no configurado");

// 🔹 DEBUG: Verificar configuración
Console.WriteLine("🔹 ===========================================");
Console.WriteLine("🔹 VERIFICACIÓN CREDENCIALES SUPABASE");
Console.WriteLine("🔹 ===========================================");
Console.WriteLine($"🔹 SUPABASE_URL_GMAIL: {supabaseUrlGmail}");
Console.WriteLine($"🔹 SUPABASE_URL_UCB: {supabaseUrlUcb}");
Console.WriteLine("🔹 ===========================================");

// 🔹 JWT Authentication con múltiples issuers
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = new[] 
            { 
                $"{supabaseUrlGmail}/auth/v1",
                $"{supabaseUrlUcb}/auth/v1"
            },
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                var issuer = parameters.ValidIssuer;
                if (issuer == $"{supabaseUrlGmail}/auth/v1")
                    return new[] { new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseJwtSecretGmail)) };
                else if (issuer == $"{supabaseUrlUcb}/auth/v1")
                    return new[] { new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseJwtSecretUcb)) };
                return null;
            },
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
// 🔹 FUNCIONES HELPER - MULTI-SUPABASE
// ═══════════════════════════════════════════════════════════════

// 🔹 Obtener configuración según el dominio
(string url, string anonKey, string serviceRoleKey, string schema) GetSupabaseConfig(string domain)
{
    return domain switch
    {
        "gmail.com" => (supabaseUrlGmail, supabaseAnonKeyGmail, supabaseServiceRoleKeyGmail, "public"),
        "ucb.edu.bo" => (supabaseUrlUcb, supabaseAnonKeyUcb, supabaseServiceRoleKeyUcb, "public"),
        _ => throw new ArgumentException($"Dominio no soportado: {domain}")
    };
}

// 🔹 Obtener información del tenant
(string schema, string domain)? GetTenantInfo(string domain)
{
    // Para dos bases de datos separadas, cada una tiene su propio schema 'public'
    return domain switch
    {
        "gmail.com" => ("public", "gmail.com"),
        "ucb.edu.bo" => ("public", "ucb.edu.bo"),
        _ => null
    };
}

// 🔹 Crear usuario en la base de datos correspondiente - MANTIENE CREACIÓN AUTOMÁTICA
async Task<bool> CreateUserViaSupabaseAPI(string email, string fullName, string domain)
{
    try
    {
        var config = GetSupabaseConfig(domain);
        
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("apikey", config.serviceRoleKey);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.serviceRoleKey}");
        httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

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

        Console.WriteLine($"🔹 Creando usuario en {domain} - Nombre: '{nombre}' | Apellido: '{apellido}'");

        var jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var jsonContent = JsonSerializer.Serialize(userData, jsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        
        // Usar tabla 'usuarios' en el schema public
        var response = await httpClient.PostAsync($"{config.url}/rest/v1/usuarios", content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"✅ Usuario creado exitosamente en {domain}: {email}");
            Console.WriteLine($"🔹 Respuesta: {responseContent}");
            return true;
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"❌ Error creando usuario en {domain}: {errorContent}");
            return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Exception en Supabase API para {domain}: {ex.Message}");
        return false;
    }
}

// 🔹 Verificar si usuario existe - MANTIENE VERIFICACIÓN
async Task<bool> CheckUserExists(string email, string domain)
{
    try
    {
        var config = GetSupabaseConfig(domain);
        
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("apikey", config.serviceRoleKey);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.serviceRoleKey}");

        var encodedEmail = Uri.EscapeDataString(email);
        var url = $"{config.url}/rest/v1/usuarios?email=eq.{encodedEmail}&select=id";
        
        Console.WriteLine($"🔹 Verificando usuario en {domain}: {url}");
        
        var response = await httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"🔹 Respuesta check exists en {domain}: {content}");
            var users = JsonSerializer.Deserialize<JsonElement[]>(content);
            return users?.Length > 0;
        }
        else
        {
            Console.WriteLine($"❌ Error verificando usuario en {domain}. Status: {response.StatusCode}");
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"❌ Error details: {errorContent}");
            return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Exception verificando usuario en {domain}: {ex.Message}");
        return false;
    }
}

// 🔹 Función para separar nombre completo
(string nombre, string apellido) SplitFullName(string fullName)
{
    if (string.IsNullOrWhiteSpace(fullName))
        return ("Usuario", "");

    var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    
    Console.WriteLine($"🔹 SplitFullName - Partes: {parts.Length}, Original: '{fullName}'");
    
    switch (parts.Length)
    {
        case 0: return ("Usuario", "");
        case 1: return (parts[0], "");
        case 2: return (parts[0], parts[1]);
        case 3: return (parts[0], parts[2]);
        case 4: return ($"{parts[0]} {parts[1]}", $"{parts[2]} {parts[3]}");
        default: return ($"{parts[0]} {parts[1]}", parts[2]);
    }
}

// 🔹 Obtener tenant del email
string GetTenantFromEmail(string email)
{
    if (email.EndsWith("@ucb.edu.bo")) return "ucb.edu.bo";
    if (email.EndsWith("@gmail.com")) return "gmail.com";
    return null;
}

// ═══════════════════════════════════════════════════════════════
// 🔹 ENDPOINTS PRINCIPALES - MANTIENE SYNC-USER
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

        // Extraer full name
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

        // Verificar dominio permitido
        if (tenantDomain != "gmail.com" && tenantDomain != "ucb.edu.bo")
            return Results.BadRequest(new { error = "Dominio no permitido." });

        // Verificar si usuario existe
        Console.WriteLine($"🔹 Verificando si usuario existe: {email} en {tenantDomain}");
        var userExists = await CheckUserExists(email, tenantDomain);
        Console.WriteLine($"🔹 Usuario existe: {userExists}");

        if (!userExists)
        {
            Console.WriteLine($"🔹 Creando nuevo usuario automáticamente...");
            var success = await CreateUserViaSupabaseAPI(email, fullName, tenantDomain);

            if (success)
            {
                Console.WriteLine($"✅ USUARIO CREADO EXITOSAMENTE en {tenantDomain}");
                return Results.Ok(new
                {
                    success = true,
                    message = "Usuario creado exitosamente",
                    email,
                    domain = tenantDomain,
                    isNewUser = true
                });
            }
            else
            {
                Console.WriteLine($"❌ ERROR al crear usuario en {tenantDomain}");
                return Results.Problem("Error al crear usuario en la base de datos.");
            }
        }
        else
        {
            Console.WriteLine($"ℹ️ Usuario ya existe en {tenantDomain}, no se crea nuevo");
            return Results.Ok(new
            {
                success = true,
                message = "Usuario ya existe",
                email,
                domain = tenantDomain,
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
            return Results.NotFound(new { error = "Dominio de email no soportado." });

        var config = GetSupabaseConfig(tenant);

        // Buscar usuario en la base de datos correspondiente
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("apikey", config.serviceRoleKey);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.serviceRoleKey}");

        var encodedEmail = Uri.EscapeDataString(email);
        var url = $"{config.url}/rest/v1/usuarios?email=eq.{encodedEmail}&select=*";
        
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
                
                Console.WriteLine($"✅ Perfil encontrado en {tenant}: {profile.nombre} - Rol: {profile.rol}");
                Console.WriteLine("🔹 ===========================================");
                
                return Results.Ok(profile);
            }
        }

        Console.WriteLine($"❌ Usuario no encontrado en la base de datos de {tenant}");
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

// Endpoints básicos
app.MapGet("/", () => "API Multi-Supabase con dominios gmail.com y ucb.edu.bo 🚀");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

Console.WriteLine("🚀 API Multi-Supabase iniciada correctamente");
Console.WriteLine("🔹 Dominios soportados: gmail.com, ucb.edu.bo");
Console.WriteLine("🔹 Funcionalidad: Creación automática de usuarios habilitada");
app.Run();