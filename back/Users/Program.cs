using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using DotNetEnv;
using System.Text;

Env.Load();
var builder = WebApplication.CreateBuilder(args);

var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");

Console.WriteLine("=== BACKEND DEBUG VERSION ===");
Console.WriteLine($"DB_HOST: {dbHost}");
Console.WriteLine($"DB_NAME: {dbName}");
Console.WriteLine("⚠️  VALIDACIONES JWT DESHABILITADAS PARA DEBUG");
Console.WriteLine("===============================");

// ⚠️ CONFIGURACIÓN TEMPORAL SIN VALIDACIÓN DE FIRMA
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,           // ⚠️ DESHABILITADO
            ValidateAudience = false,         // ⚠️ DESHABILITADO
            ValidateLifetime = false,         // ⚠️ DESHABILITADO
            ValidateIssuerSigningKey = false, // ⚠️ DESHABILITADO
            RequireSignedTokens = false,      // ⚠️ DESHABILITADO
            RequireExpirationTime = false     // ⚠️ DESHABILITADO
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                Console.WriteLine("✅ TOKEN VALIDADO (modo debug)");
                Console.WriteLine("Claims encontradas:");
                foreach (var claim in context.Principal.Claims)
                {
                    Console.WriteLine($"  {claim.Type}: {claim.Value}");
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"❌ AUTH FAILED: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.WebHost.UseUrls("http://+:5000");

var app = builder.Build();

app.UseCors("AllowVueApp");
app.UseAuthentication();
app.UseAuthorization();

// Test endpoint
app.MapGet("/", () => Results.Json(new { message = "Backend funcionando", timestamp = DateTime.Now }));

// Debug endpoint
app.MapGet("/debug-token", [Authorize] (HttpContext ctx) =>
{
    Console.WriteLine("=== DEBUG TOKEN ENDPOINT ===");
    var claims = ctx.User.Claims.Select(c => new { type = c.Type, value = c.Value }).ToList();
    return Results.Json(new { authenticated = ctx.User.Identity.IsAuthenticated, claims = claims });
});

// Users endpoint
app.MapGet("/users", [Authorize] async (HttpContext ctx) =>
{
    Console.WriteLine("=== ENDPOINT /users ACCEDIDO ===");
    
    try
    {
        var emailClaim = ctx.User.Claims.FirstOrDefault(c => c.Type == "email") 
                         ?? ctx.User.Claims.FirstOrDefault(c => c.Type == "user_email") 
                         ?? ctx.User.Claims.FirstOrDefault(c => c.Type == "sub");

        var email = emailClaim?.Value;
        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("❌ No se encontró email");
            return Results.Json(new { error = "Email no encontrado" }, statusCode: 401);
        }

        Console.WriteLine($"✅ Email: {email}");
        var domain = email.Split('@').Last();
        Console.WriteLine($"✅ Dominio: {domain}");

        string tenantSchema;
        string tenantPassword;

        switch (domain.ToLower())
        {
            case "ucb.edu.bo":
                tenantSchema = "tenant_ucb";
                tenantPassword = Environment.GetEnvironmentVariable("DB_PASSWORD_UCB");
                break;
            case "gmail.com":
                tenantSchema = "tenant_upb";
                tenantPassword = Environment.GetEnvironmentVariable("DB_PASSWORD_UPB");
                break;
            default:
                tenantSchema = "tenant_default";
                tenantPassword = Environment.GetEnvironmentVariable("DB_PASSWORD_DEFAULT");
                break;
        }

        Console.WriteLine($"✅ Tenant: {tenantSchema}");

        if (string.IsNullOrEmpty(tenantPassword))
        {
            Console.WriteLine($"❌ Password no configurado para {tenantSchema}");
            return Results.Json(new { error = $"Password no configurado para {tenantSchema}" }, statusCode: 500);
        }

        var connString = $"Host={dbHost};Database={dbName};Username=postgres;Password={tenantPassword};";
        var students = new List<object>();

        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        Console.WriteLine("✅ Conectado a DB");

        using var cmd = new NpgsqlCommand($@"
            SELECT id, nombre, apellido, email, rol
            FROM {tenantSchema}.students
            ORDER BY nombre
        ", conn);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            students.Add(new
            {
                id = reader["id"],
                nombre = reader["nombre"],
                apellido = reader["apellido"],
                email = reader["email"],
                rol = reader["rol"]
            });
        }

        Console.WriteLine($"✅ {students.Count} estudiantes encontrados");
        return Results.Json(students);
    }
    catch (Exception ex)
    
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
        return Results.Json(new { error = ex.Message }, statusCode: 500);
    }
}).RequireAuthorization();

Console.WriteLine("🚀 Backend iniciado en modo DEBUG");
app.Run();