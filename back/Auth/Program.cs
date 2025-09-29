using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Cargar .env
DotNetEnv.Env.Load();

// 🔹 Configuración Supabase
var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
var supabaseAnonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");

// 🔹 Configuración DB
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbUser = "postgres"; // superuser que accede a tabla public.tenant

// Contraseñas tenants
var dbPasswordUcb = Environment.GetEnvironmentVariable("DB_PASSWORD_UCB");
var dbPasswordUpb = Environment.GetEnvironmentVariable("DB_PASSWORD_UPB");

// 🔹 JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseAnonKey!))
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
        policy.WithOrigins("http://localhost:5173") // tu frontend
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "API Running 🚀");

// 🔹 Función helper para obtener tenant info
async Task<(string schema, string dbUser, string dbPassword)?> GetTenantInfo(string domain)
{
    var connString =
        $"Host={dbHost};Port=5432;Username={dbUser};Password={dbPasswordUcb};Database={dbName};SSL Mode=Require;Trust Server Certificate=true";

    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    var cmd = new NpgsqlCommand("SELECT schema_name, db_user, domain FROM public.tenant WHERE domain = @Domain", conn);
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
            "gmail.com" => dbPasswordUpb!,
            _ => throw new Exception("Dominio no soportado")
        };

        return (schema, tenantDbUser, dbPassword);
    }

    return null;
}

// 🔹 Endpoint sync-user
app.MapPost("/api/auth/sync-user", async (HttpRequest req) =>
{
    try
    {
        var authHeader = req.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return Results.BadRequest(new { error = "Token no proporcionado." });

        var token = authHeader.Substring("Bearer ".Length).Trim();

        // Decodificar JWT de Supabase
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

        if (string.IsNullOrEmpty(email))
            return Results.BadRequest(new { error = "No se pudo obtener el correo del token." });

        // Obtener dominio
        var domain = email.Split('@').Last().ToLower();

        // 🔹 Mapear directamente dominio -> tenant
        string schema = domain switch
        {
            "gmail.com"   => "tenant_upb",
            "ucb.edu.bo"  => "tenant_ucb",
            _             => throw new Exception("Dominio no soportado")
        };

        string tenantDbUser = schema switch
        {
            "tenant_upb" => "upb_user", // debe coincidir con db_user de tu tabla tenant
            "tenant_ucb" => "ucb_user",
            _            => throw new Exception("Usuario no configurado")
        };

        string dbPassword = domain switch
        {
            "gmail.com"  => dbPasswordUpb!,
            "ucb.edu.bo" => dbPasswordUcb!,
            _            => throw new Exception("Password no configurado")
        };

        var connString =
            $"Host={dbHost};Port=5432;Username={tenantDbUser};Password={dbPassword};Database={dbName};SSL Mode=Require;Trust Server Certificate=true";

        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        // Verificar existencia del usuario
        var checkCmd = new NpgsqlCommand($"SELECT id FROM {schema}.usuarios WHERE email = @Email", conn);
        checkCmd.Parameters.AddWithValue("Email", email);
        var exists = await checkCmd.ExecuteScalarAsync();

        if (exists == null)
        {
            // Insertar con rol = usuario
            var insertCmd = new NpgsqlCommand(
                $@"INSERT INTO {schema}.usuarios 
                   (nombre, apellido, email, rol, created_at, updated_at) 
                   VALUES (@Nombre, @Apellido, @Email, 'usuario', NOW(), NOW())",
                conn
            );

            // Intentamos sacar nombre/apellido del claim "name"
            var fullName = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "";
            var parts = fullName.Split(' ', 2);
            var nombre = parts.Length > 0 ? parts[0] : "";
            var apellido = parts.Length > 1 ? parts[1] : "";

            insertCmd.Parameters.AddWithValue("Nombre", nombre);
            insertCmd.Parameters.AddWithValue("Apellido", apellido);
            insertCmd.Parameters.AddWithValue("Email", email);
            await insertCmd.ExecuteNonQueryAsync();
        }

        return Results.Ok(new { success = true, email, schema });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.RequireAuthorization();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
