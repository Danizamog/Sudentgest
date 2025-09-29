using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Cargar .env
DotNetEnv.Env.Load();

// 🔹 Configuración Supabase
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
var supabaseAnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
var supabaseJwtSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET") ?? supabaseAnonKey;

// 🔹 Configuración DB
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPasswordUcb = Environment.GetEnvironmentVariable("DB_PASSWORD_UCB");
var dbPasswordUpb = Environment.GetEnvironmentVariable("DB_PASSWORD_UPB");
var dbPasswordGmail = Environment.GetEnvironmentVariable("DB_PASSWORD_GMAIL");

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

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    Console.WriteLine($"❌ Auth failed: {context.Exception.Message}");
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    var email = context.Principal?.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                    Console.WriteLine($"✅ Token validado para: {email}");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔹 CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
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

app.MapGet("/", () => "API Multi-tenant Running 🚀");

// 🔹 Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// ═══════════════════════════════════════════════════════════════
// 🔹 FUNCIONES HELPER
// ═══════════════════════════════════════════════════════════════

async Task<(string schema, string dbUser, string dbPassword)?> GetTenantInfo(string domain)
{
    var connString = $"Host={dbHost};Port=5432;Username={dbUser};Password={dbPasswordUcb};Database={dbName};SSL Mode=Require;Trust Server Certificate=true";
    try
    {
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        var cmd = new NpgsqlCommand(
            "SELECT schema_name, db_user, domain FROM public.tenant WHERE domain = @Domain",
            conn
        );
        cmd.Parameters.AddWithValue("Domain", domain);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var schema = reader.GetString(0);
            var tenantDbUser = reader.GetString(1);
            var tenantDomain = reader.GetString(2);
            string dbPassword = tenantDomain switch
            {
                "ucb.edu.bo" => dbPasswordUcb!,
                "upb.edu.bo" => dbPasswordUpb!,
                "gmail.com" => dbPasswordGmail!,
                _ => throw new Exception("Dominio no soportado")
            };
            return (schema, tenantDbUser, dbPassword);
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error obteniendo tenant info: {ex.Message}");
    }
    return null;
}

bool IsValidSchemaName(string schema)
{
    return Regex.IsMatch(schema, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
}

string BuildConnectionString(string user, string password)
{
    return $"Host={dbHost};Port=5432;Username={user};Password={password};Database={dbName};SSL Mode=Require;Trust Server Certificate=true";
}

// ═══════════════════════════════════════════════════════════════
// 🔹 ENDPOINTS
// ═══════════════════════════════════════════════════════════════

app.MapPost("/api/auth/sync-user", async (HttpContext context) =>
{
    try
    {
        var email = context.User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        var userId = context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        var fullName = context.User.Claims.FirstOrDefault(c => c.Type == "user_metadata")?.Value
                    ?? context.User.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                    ?? context.User.Claims.FirstOrDefault(c => c.Type == "full_name")?.Value
                    ?? "";

        if (string.IsNullOrEmpty(email))
            return Results.BadRequest(new { error = "Email no encontrado en el token." });

        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(body))
            return Results.BadRequest(new { error = "Body vacío. Se requiere tenant." });

        var json = System.Text.Json.JsonDocument.Parse(body);
        if (!json.RootElement.TryGetProperty("tenant", out var tenantElement))
            return Results.BadRequest(new { error = "Propiedad 'tenant' no encontrada." });

        var tenantDomain = tenantElement.GetString();
        if (string.IsNullOrEmpty(tenantDomain))
            return Results.BadRequest(new { error = "Tenant requerido." });

        var tenantInfo = await GetTenantInfo(tenantDomain);
        if (tenantInfo == null)
            return Results.NotFound(new { error = "Tenant no encontrado." });

        var (schema, tenantDbUser, dbPassword) = tenantInfo.Value;

        if (!IsValidSchemaName(schema))
        {
            Console.Error.WriteLine($"⚠️ Schema inválido detectado: {schema}");
            return Results.BadRequest(new { error = "Schema inválido." });
        }

        var connString = BuildConnectionString(tenantDbUser, dbPassword);
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        await using var transaction = await conn.BeginTransactionAsync();
        try
        {
            var checkCmd = new NpgsqlCommand(
                $"SELECT id FROM \"{schema}\".usuarios WHERE email = @Email",
                conn,
                transaction
            );
            checkCmd.Parameters.AddWithValue("Email", email);

            var existingId = await checkCmd.ExecuteScalarAsync();

            if (existingId == null)
            {
                var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var nombre = parts.Length > 0 ? parts[0] : "Usuario";
                var apellido = parts.Length > 1 ? parts[1] : "";

                var insertCmd = new NpgsqlCommand(
                    $@"INSERT INTO ""{schema}"".usuarios
                       (nombre, apellido, email, rol, created_at, updated_at)
                       VALUES (@Nombre, @Apellido, @Email, 'usuario', NOW(), NOW())
                       RETURNING id",
                    conn,
                    transaction
                );
                insertCmd.Parameters.AddWithValue("Nombre", nombre);
                insertCmd.Parameters.AddWithValue("Apellido", apellido);
                insertCmd.Parameters.AddWithValue("Email", email);

                var newId = await insertCmd.ExecuteScalarAsync();
                await transaction.CommitAsync();

                Console.WriteLine($"✅ Usuario creado: {email} en schema {schema}");
                return Results.Ok(new
                {
                    success = true,
                    message = "Usuario creado exitosamente",
                    email,
                    schema,
                    userId = newId,
                    isNewUser = true
                });
            }
            else
            {
                await transaction.CommitAsync();
                Console.WriteLine($"ℹ️ Usuario ya existe: {email} en schema {schema}");
                return Results.Ok(new
                {
                    success = true,
                    message = "Usuario ya existe",
                    email,
                    schema,
                    userId = existingId,
                    isNewUser = false
                });
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.Error.WriteLine($"❌ Error en transacción: {ex.Message}");
            throw;
        }
    }
    catch (System.Text.Json.JsonException)
    {
        return Results.BadRequest(new { error = "JSON inválido en el body." });
    }
    catch (NpgsqlException ex)
    {
        Console.Error.WriteLine($"❌ Error de BD: {ex.Message}");
        return Results.Problem("Error de conexión con la base de datos.");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error inesperado: {ex.Message}");
        Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
        return Results.Problem("Error interno del servidor.");
    }
})
.RequireAuthorization()
.WithName("SyncUser")
.WithOpenApi();

app.MapGet("/api/auth/me", async (HttpContext context) =>
{
    try
    {
        var email = context.User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        var userId = context.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (string.IsNullOrEmpty(email))
            return Results.Unauthorized();

        var claims = context.User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Results.Ok(new
        {
            email,
            userId,
            claims // ⚠️ Remover en producción si contiene info sensible
        });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error en /me: {ex.Message}");
        return Results.Problem("Error al obtener información del usuario.");
    }
})
.RequireAuthorization()
.WithName("GetCurrentUser")
.WithOpenApi();

app.MapGet("/api/tenants", async () =>
{
    try
    {
        var connString = $"Host={dbHost};Port=5432;Username={dbUser};Password={dbPasswordUcb};Database={dbName};SSL Mode=Require;Trust Server Certificate=true";
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        var cmd = new NpgsqlCommand("SELECT domain, schema_name FROM public.tenant ORDER BY domain", conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        var tenants = new List<object>();
        while (await reader.ReadAsync())
        {
            tenants.Add(new
            {
                domain = reader.GetString(0),
                schema = reader.GetString(1)
            });
        }
        return Results.Ok(tenants);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Error listando tenants: {ex.Message}");
        return Results.Problem("Error al obtener lista de tenants.");
    }
})
.WithName("GetTenants")
.WithOpenApi();
Console.WriteLine("🚀 API Multi-tenant iniciada correctamente");
Console.WriteLine($"📍 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"🔗 Supabase URL: {supabaseUrl}");
Console.WriteLine($"🗄️ Database: {dbName}@{dbHost}");
app.Run();