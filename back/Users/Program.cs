using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using DotNetEnv;

Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Variables de entorno
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

// JWT Supabase
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://TU_SUPABASE_URL/auth/v1";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://TU_SUPABASE_URL/auth/v1",
            ValidateAudience = false,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

builder.WebHost.UseUrls("http://+:5003"); // 🔹 Importante para Docker

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

// Endpoint usuarios según dominio
app.MapGet("/users", [Authorize] async (HttpContext ctx) =>
{
    try
    {
        var email = ctx.User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        if (string.IsNullOrEmpty(email))
            return Results.Json(new { error = "Usuario no autenticado" }, statusCode: 401);

        Console.WriteLine($"Solicitud /users recibida por {email}");

        var domain = email.Split('@').Last();
        var tenantSchema = domain switch
        {
            "ucb.edu.bo" => "tenant_ucb",
            "gmail.com" => "tenant_upb",
            _ => "tenant_default"
        };

        var students = new List<object>();
        var connString = $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPassword};";

        using var conn = new NpgsqlConnection(connString);
        conn.Open();

        using var cmd = new NpgsqlCommand($@"
            SELECT id, first_name, last_name, email, subject, grade
            FROM {tenantSchema}.students
            ORDER BY first_name
        ", conn);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            students.Add(new
            {
                id = reader["id"],
                first_name = reader["first_name"],
                last_name = reader["last_name"],
                email = reader["email"],
                subject = reader["subject"] is DBNull ? null : reader["subject"],
                grade = reader["grade"] is DBNull ? null : reader["grade"]
            });
        }

        if (!students.Any())
            return Results.Json(new { message = "No hay estudiantes en este tenant." });

        return Results.Json(students);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error backend: " + ex.Message);
        return Results.Json(new { error = "Error interno del servidor: " + ex.Message }, statusCode: 500);
    }
}).RequireAuthorization();

app.Run();
