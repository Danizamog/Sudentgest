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
        
        // 🔹 IMPORTANTE: Extraer token de cookies también
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Intentar obtener token de Authorization header primero
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                
                // Si no hay token en header, buscar en cookies
                if (string.IsNullOrEmpty(token))
                {
                    token = context.Request.Cookies["session_token"];
                }
                
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
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
// 🔹 FUNCIONES HELPER OPTIMIZADAS
// ═══════════════════════════════════════════════════════════════

// 🔹 HttpClient configurado para Supabase
HttpClient CreateSupabaseClient(string apiKey)
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.Add("apikey", apiKey);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    return client;
}

// 🔹 Obtener información del tenant
async Task<(string schema, string domain)?> GetTenantInfo(string domain)
{
    try
    {
        using var httpClient = CreateSupabaseClient(supabaseAnonKey);
        var response = await httpClient.GetAsync($"{supabaseUrl}/rest/v1/tenants?domain=eq.{domain}&select=*");

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var tenants = JsonSerializer.Deserialize<JsonElement[]>(content);
            
            if (tenants?.Length > 0)
            {
                var tenant = tenants[0];
                return (tenant.GetProperty("schema_name").GetString(), tenant.GetProperty("domain").GetString());
            }
        }
        
        return null;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error obteniendo tenant info: {ex.Message}");
        return null;
    }
}

// 🔹 Split nombre simplificado
(string nombre, string apellido) SplitFullName(string fullName)
{
    if (string.IsNullOrWhiteSpace(fullName))
        return ("Usuario", "");

    var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    
    return parts.Length switch
    {
        0 => ("Usuario", ""),
        1 => (parts[0], ""),
        2 => (parts[0], parts[1]),
        _ => (parts[0], string.Join(" ", parts.Skip(1)))
    };
}

// 🔹 Verificar si usuario existe
async Task<bool> CheckUserExists(string email, string schema)
{
    try
    {
        using var httpClient = CreateSupabaseClient(supabaseServiceRoleKey);
        var tableName = $"{schema}_usuarios";
        var url = $"{supabaseUrl}/rest/v1/{tableName}?email=eq.{Uri.EscapeDataString(email)}&select=id";
        
        var response = await httpClient.GetAsync(url);
        return response.IsSuccessStatusCode && 
               (await response.Content.ReadAsStringAsync()).Contains("id");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Exception verificando usuario: {ex.Message}");
        return false;
    }
}

// 🔹 Crear usuario
async Task<bool> CreateUserViaSupabaseAPI(string email, string fullName, string schema)
{
    try
    {
        using var httpClient = CreateSupabaseClient(supabaseServiceRoleKey);
        httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

        var (nombre, apellido) = SplitFullName(fullName);
        var userData = new { nombre, apellido, email, rol = "Estudiante", created_at = DateTime.UtcNow };

        var jsonContent = JsonSerializer.Serialize(userData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync($"{supabaseUrl}/rest/v1/{schema}_usuarios", content);
        return response.IsSuccessStatusCode;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Exception creando usuario: {ex.Message}");
        return false;
    }
}

// 🔹 Obtener tenant del email
string GetTenantFromEmail(string email) => email switch
{
    string e when e.EndsWith("@ucb.edu.bo") => "ucb.edu.bo",
    string e when e.EndsWith("@upb.edu.bo") => "upb.edu.bo", 
    string e when e.EndsWith("@gmail.com") => "gmail.com",
    _ => null
};

// 🔹 Obtener usuario por email
async Task<JsonElement?> GetUserByEmail(string email, string schema)
{
    try
    {
        using var httpClient = CreateSupabaseClient(supabaseServiceRoleKey);
        var tableName = $"{schema}_usuarios";
        var url = $"{supabaseUrl}/rest/v1/{tableName}?email=eq.{Uri.EscapeDataString(email)}&select=*";
        
        var response = await httpClient.GetAsync(url);
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<JsonElement[]>(content);
            return users?.Length > 0 ? users[0] : null;
        }
        
        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Exception obteniendo usuario: {ex.Message}");
        return null;
    }
}

// ═══════════════════════════════════════════════════════════════
// 🔹 ENDPOINTS PRINCIPALES - RUTAS /auth/
// ═══════════════════════════════════════════════════════════════

// 🔹 Health check
app.MapGet("/", () => "Auth API Multi-tenant con Supabase 🚀");
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    service = "auth-api"
}));

// 🔹 Sincronizar usuario - RUTA /auth/sync-user
app.MapPost("/auth/sync-user", async (HttpContext context) =>
{
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        var fullName = context.User.FindFirst("name")?.Value ?? 
                      context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value ?? 
                      "Usuario";

        if (string.IsNullOrEmpty(email))
            return Results.BadRequest(new { error = "Email no encontrado en el token." });

        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = JsonDocument.Parse(body);

        if (!json.RootElement.TryGetProperty("tenant", out var tenantElement))
            return Results.BadRequest(new { error = "Propiedad 'tenant' no encontrada." });

        var tenantDomain = tenantElement.GetString();
        if (string.IsNullOrEmpty(tenantDomain))
            return Results.BadRequest(new { error = "Tenant requerido." });

        var tenantInfo = await GetTenantInfo(tenantDomain);
        if (tenantInfo == null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        var (schema, domain) = tenantInfo.Value;

        if (!await CheckUserExists(email, schema))
        {
            var success = await CreateUserViaSupabaseAPI(email, fullName, schema);
            return success ? 
                Results.Ok(new { success = true, message = "Usuario creado", email, schema, isNewUser = true }) :
                Results.Problem("Error al crear usuario.");
        }

        return Results.Ok(new { success = true, message = "Usuario ya existe", email, schema, isNewUser = false });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error en sync-user: {ex.Message}");
        return Results.Problem("Error interno del servidor.");
    }
})
.RequireAuthorization()
.WithName("SyncUser")
.WithOpenApi();

// 🔹 Verificar estado de la cookie HttpOnly
app.MapGet("/auth/check-cookie", async (HttpContext context) =>
{
    try
    {
        var sessionToken = context.Request.Cookies["session_token"];
        
        if (string.IsNullOrEmpty(sessionToken))
        {
            return Results.Json(new { authenticated = false }, statusCode: 200);
        }

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {sessionToken}");
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
        
        var response = await httpClient.GetAsync($"{supabaseUrl}/auth/v1/user");
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var userData = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (userData.TryGetProperty("id", out _) && !userData.TryGetProperty("error", out _))
            {
                return Results.Json(new { 
                    authenticated = true,
                    user = new {
                        id = userData.GetProperty("id").GetString(),
                        email = userData.GetProperty("email").GetString()
                    }
                }, statusCode: 200);
            }
        }

        context.Response.Cookies.Delete("session_token");
        return Results.Json(new { authenticated = false }, statusCode: 200);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error verificando cookie: {ex.Message}");
        return Results.Json(new { authenticated = false }, statusCode: 200);
    }
})
.WithName("CheckCookie")
.WithOpenApi();

// 🔹 Obtener perfil de usuario - COMPATIBLE CON COOKIES
app.MapGet("/auth/user-profile", async (HttpContext context) =>
{
    try
    {
        string email = null;
        string userId = null;

        email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        userId = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        if (string.IsNullOrEmpty(email))
            return Results.Unauthorized();

        var tenant = GetTenantFromEmail(email);
        if (tenant == null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        var tenantInfo = await GetTenantInfo(tenant);
        if (tenantInfo == null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        var (schema, domain) = tenantInfo.Value;

        var user = await GetUserByEmail(email, schema);
        if (user == null)
            return Results.NotFound(new { error = "Usuario no encontrado." });

        var profile = new
        {
            id = userId ?? user.Value.GetProperty("id").GetString(),
            nombre = user.Value.GetProperty("nombre").GetString() ?? "Usuario",
            apellido = user.Value.GetProperty("apellido").GetString() ?? "",
            email = user.Value.GetProperty("email").GetString() ?? email,
            rol = user.Value.GetProperty("rol").GetString()?.ToLower() ?? "Estudiante"
        };
        
        return Results.Ok(profile);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error obteniendo perfil: {ex.Message}");
        return Results.Problem("Error interno del servidor.");
    }
})
.RequireAuthorization()
.WithName("GetUserProfile")
.WithOpenApi();
// 🔹 DEBUG: Verificar cookies recibidas
app.MapGet("/auth/debug-cookies", (HttpContext context) =>
{
    var requestCookies = context.Request.Cookies;
    var response = new
    {
        requestCookies = requestCookies.Keys.ToArray(),
        hasSessionCookie = requestCookies.ContainsKey("session_token"),
        sessionTokenValue = requestCookies.ContainsKey("session_token") ? "***PRESENTE***" : "AUSENTE",
        headers = new {
            origin = context.Request.Headers["Origin"].ToString(),
            host = context.Request.Headers["Host"].ToString()
        }
    };
    
    Console.WriteLine($"🔍 Debug Cookies: {JsonSerializer.Serialize(response)}");
    
    return Results.Ok(response);
}).RequireAuthorization();
// 🔹 Establecer cookie de sesión
// 🔹 Establecer cookie de sesión - VERSIÓN CORREGIDA
app.MapPost("/auth/session-cookie", async (HttpContext context) =>
{
    try
    {
        var tokenHeader = context.Request.Headers["Authorization"].ToString();
        var token = tokenHeader.StartsWith("Bearer ") ? tokenHeader["Bearer ".Length..] : tokenHeader;

        if (string.IsNullOrEmpty(token))
            return Results.BadRequest(new { error = "Authorization Bearer token requerido." });

        // Validar token con Supabase
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
        
        var validationResponse = await httpClient.GetAsync($"{supabaseUrl}/auth/v1/user");
        
        if (!validationResponse.IsSuccessStatusCode)
        {
            return Results.BadRequest(new { error = "Token inválido" });
        }

        var userData = await validationResponse.Content.ReadAsStringAsync();
        var userJson = JsonSerializer.Deserialize<JsonElement>(userData);
        
        var expiresIn = 3600; // 1 hora por defecto
        if (userJson.TryGetProperty("expires_in", out var expiresInProp) && expiresInProp.ValueKind != JsonValueKind.Null)
        {
            expiresIn = expiresInProp.GetInt32();
        }

        // 🔹 CONFIGURACIÓN CRÍTICA - COOKIE HTTPONLY
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,        // ✅ IMPEDIR acceso desde JavaScript
            Secure = false,         // ✅ En desarrollo false (http), en producción true
            Path = "/",             // ✅ Disponible en todas las rutas
            SameSite = SameSiteMode.Lax, // ✅ Para autenticación
            Expires = DateTimeOffset.UtcNow.AddSeconds(expiresIn),
            MaxAge = TimeSpan.FromSeconds(expiresIn),
            // Domain = null        // ✅ No especificar dominio para localhost
        };

        // 🔹 ESTABLECER LA COOKIE
        context.Response.Cookies.Append("session_token", token, cookieOptions);

        Console.WriteLine($"✅ Cookie HttpOnly establecida para usuario: {userJson.GetProperty("email").GetString()}");

        return Results.Ok(new { 
            success = true, 
            message = "Cookie HttpOnly establecida correctamente",
            expiresIn = expiresIn,
            httpOnly = true
        });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error estableciendo cookie HttpOnly: {ex.Message}");
        return Results.Problem("Error interno del servidor.");
    }
})
.WithName("SetSessionCookie")
.WithOpenApi();

// 🔹 Limpiar cookie (logout)
app.MapPost("/auth/clear-cookie", (HttpContext context) =>
{
    try
    {
        var cookieOptions = new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            Secure = context.Request.Scheme == "https",
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
        };

        context.Response.Cookies.Delete("session_token", cookieOptions);

        return Results.Ok(new { 
            success = true, 
            message = "Cookie eliminada correctamente" 
        });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error eliminando cookie: {ex.Message}");
        return Results.Problem("Error interno del servidor.");
    }
})
.WithName("ClearSessionCookie")
.WithOpenApi();

// ═══════════════════════════════════════════════════════════════
// 🔹 ENDPOINTS DE COMPATIBILIDAD - RUTAS /api/auth/
// ═══════════════════════════════════════════════════════════════

// 🔹 Sincronizar usuario - RUTA COMPATIBILIDAD /api/auth/sync-user
app.MapPost("/api/auth/sync-user", async (HttpContext context) =>
{
    try
    {
        var email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        var fullName = context.User.FindFirst("name")?.Value ?? 
                      context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value ?? 
                      "Usuario";

        if (string.IsNullOrEmpty(email))
            return Results.BadRequest(new { error = "Email no encontrado en el token." });

        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        var json = JsonDocument.Parse(body);

        if (!json.RootElement.TryGetProperty("tenant", out var tenantElement))
            return Results.BadRequest(new { error = "Propiedad 'tenant' no encontrada." });

        var tenantDomain = tenantElement.GetString();
        if (string.IsNullOrEmpty(tenantDomain))
            return Results.BadRequest(new { error = "Tenant requerido." });

        var tenantInfo = await GetTenantInfo(tenantDomain);
        if (tenantInfo == null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        var (schema, domain) = tenantInfo.Value;

        if (!await CheckUserExists(email, schema))
        {
            var success = await CreateUserViaSupabaseAPI(email, fullName, schema);
            return success ? 
                Results.Ok(new { success = true, message = "Usuario creado", email, schema, isNewUser = true }) :
                Results.Problem("Error al crear usuario.");
        }

        return Results.Ok(new { success = true, message = "Usuario ya existe", email, schema, isNewUser = false });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error en sync-user: {ex.Message}");
        return Results.Problem("Error interno del servidor.");
    }
})
.RequireAuthorization()
.WithName("SyncUserApi")
.WithOpenApi();

// 🔹 Verificar estado de la cookie HttpOnly - COMPATIBILIDAD
app.MapGet("/api/auth/check-cookie", async (HttpContext context) =>
{
    try
    {
        var sessionToken = context.Request.Cookies["session_token"];
        
        if (string.IsNullOrEmpty(sessionToken))
        {
            return Results.Json(new { authenticated = false }, statusCode: 200);
        }

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {sessionToken}");
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
        
        var response = await httpClient.GetAsync($"{supabaseUrl}/auth/v1/user");
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var userData = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (userData.TryGetProperty("id", out _) && !userData.TryGetProperty("error", out _))
            {
                return Results.Json(new { 
                    authenticated = true,
                    user = new {
                        id = userData.GetProperty("id").GetString(),
                        email = userData.GetProperty("email").GetString()
                    }
                }, statusCode: 200);
            }
        }

        context.Response.Cookies.Delete("session_token");
        return Results.Json(new { authenticated = false }, statusCode: 200);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error verificando cookie: {ex.Message}");
        return Results.Json(new { authenticated = false }, statusCode: 200);
    }
})
.WithName("CheckCookieApi")
.WithOpenApi();

// 🔹 Obtener perfil de usuario - COMPATIBILIDAD
app.MapGet("/api/auth/user-profile", async (HttpContext context) =>
{
    try
    {
        string email = null;
        string userId = null;

        email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        userId = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        if (string.IsNullOrEmpty(email))
            return Results.Unauthorized();

        var tenant = GetTenantFromEmail(email);
        if (tenant == null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        var tenantInfo = await GetTenantInfo(tenant);
        if (tenantInfo == null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        var (schema, domain) = tenantInfo.Value;

        var user = await GetUserByEmail(email, schema);
        if (user == null)
            return Results.NotFound(new { error = "Usuario no encontrado." });

        var profile = new
        {
            id = userId ?? user.Value.GetProperty("id").GetString(),
            nombre = user.Value.GetProperty("nombre").GetString() ?? "Usuario",
            apellido = user.Value.GetProperty("apellido").GetString() ?? "",
            email = user.Value.GetProperty("email").GetString() ?? email,
            rol = user.Value.GetProperty("rol").GetString()?.ToLower() ?? "Estudiante"
        };
        
        return Results.Ok(profile);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error obteniendo perfil: {ex.Message}");
        return Results.Problem("Error interno del servidor.");
    }
})
.RequireAuthorization()
.WithName("GetUserProfileApi")
.WithOpenApi();

// 🔹 Establecer cookie de sesión - COMPATIBILIDAD
app.MapPost("/api/auth/session-cookie", async (HttpContext context) =>
{
    try
    {
        var tokenHeader = context.Request.Headers["Authorization"].ToString();
        var token = tokenHeader.StartsWith("Bearer ") ? tokenHeader["Bearer ".Length..] : tokenHeader;

        if (string.IsNullOrEmpty(token))
            return Results.BadRequest(new { error = "Authorization Bearer token requerido." });

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        httpClient.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
        
        var validationResponse = await httpClient.GetAsync($"{supabaseUrl}/auth/v1/user");
        
        if (!validationResponse.IsSuccessStatusCode)
        {
            return Results.BadRequest(new { error = "Token inválido" });
        }

        var userData = await validationResponse.Content.ReadAsStringAsync();
        var userJson = JsonSerializer.Deserialize<JsonElement>(userData);
        
        var expiresIn = 3600;
        if (userJson.TryGetProperty("expires_in", out var expiresInProp) && expiresInProp.ValueKind != JsonValueKind.Null)
        {
            expiresIn = expiresInProp.GetInt32();
        }

        var isSecure = context.Request.Scheme == "https";
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            Path = "/",
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddSeconds(expiresIn),
            MaxAge = TimeSpan.FromSeconds(expiresIn)
        };

        context.Response.Cookies.Append("session_token", token, cookieOptions);

        return Results.Ok(new { 
            success = true, 
            message = "Cookie establecida correctamente",
            expiresIn = expiresIn
        });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error estableciendo cookie: {ex.Message}");
        return Results.Problem("Error interno del servidor.");
    }
})
.WithName("SetSessionCookieApi")
.WithOpenApi();

// 🔹 Limpiar cookie (logout) - COMPATIBILIDAD
app.MapPost("/api/auth/clear-cookie", (HttpContext context) =>
{
    try
    {
        var cookieOptions = new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            Secure = context.Request.Scheme == "https",
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
        };

        context.Response.Cookies.Delete("session_token", cookieOptions);

        return Results.Ok(new { 
            success = true, 
            message = "Cookie eliminada correctamente" 
        });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error eliminando cookie: {ex.Message}");
        return Results.Problem("Error interno del servidor.");
    }
})
.WithName("ClearSessionCookieApi")
.WithOpenApi();

// 🔹 Obtener usuarios por tenant
app.MapGet("/api/usuarios/{tenantDomain}", async (string tenantDomain) =>
{
    try
    {
        var tenantInfo = await GetTenantInfo(tenantDomain);
        if (tenantInfo == null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        var (schema, domain) = tenantInfo.Value;

        using var httpClient = CreateSupabaseClient(supabaseAnonKey);
        var response = await httpClient.GetAsync($"{supabaseUrl}/rest/v1/{schema}_usuarios?select=*&order=created_at.desc");

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

Console.WriteLine("🚀 Auth API iniciada en puerto 8080 (Docker)");
app.Run();