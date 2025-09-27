using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using DotNetEnv;
using System.Text.Json;
using System.Text;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// 🔹 Variables de entorno
string? supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
string? supabaseAnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");

if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseAnonKey))
    throw new Exception("Debe definir SUPABASE_URL y SUPABASE_ANON_KEY en el .env");

// 🔹 JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{supabaseUrl}/auth/v1";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",
            ValidateAudience = false,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

// 🔹 CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// 🔹 Middleware global para proteger endpoints
app.Use(async (context, next) =>
{
    var endpoint = context.GetEndpoint();
    if (endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null)
    {
        await next();
    }
    else
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "No autorizado" }));
            return;
        }
        await next();
    }
});

// 🔹 Endpoint público: login
app.MapPost("/auth/login", async (LoginRequest body) =>
{
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("apikey", supabaseAnonKey!);

    var payload = new { email = body.Email, password = body.Password };
    var response = await client.PostAsync(
        $"{supabaseUrl}/auth/v1/token?grant_type=password",
        new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
    );

    var json = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
        return Results.Json(new { error = "Credenciales incorrectas" }, statusCode: 401);

    return Results.Content(json, "application/json");
}).AllowAnonymous();

// 🔹 Endpoint protegido: info del usuario
app.MapGet("/auth/me", (HttpContext ctx) =>
{
    var email = ctx.User.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
    return Results.Ok(new { email });
});

app.Run();

// 🔹 Record para login
record LoginRequest(string Email, string Password);
