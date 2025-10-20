using Npgsql;

namespace Forum.Helpers
{
    public class AuthHelper
    {
        private readonly IConfiguration _configuration;

        public AuthHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetTenantSchema(string email)
        {
            var domain = email.Split('@')[1].ToLower();
            
            return domain switch
            {
                "gmail.com" => "tenant_gmail",
                "ucb.edu.bo" => "tenant_ucb",
                "upb.edu" => "tenant_upb",
                _ => throw new ArgumentException($"Unknown tenant domain: {domain}")
            };
        }

        public async Task<UserInfo?> GetUserInfoFromEmail(string email)
        {
            var tenantSchema = GetTenantSchema(email);
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var query = $@"
                SELECT id, nombre, apellido, email, rol 
                FROM {tenantSchema}_usuarios 
                WHERE email = @email 
                LIMIT 1";

            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@email", email);

            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return new UserInfo
                {
                    Id = reader.GetInt32(0).ToString(),
                    Nombre = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Apellido = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Email = reader.GetString(3),
                    Rol = reader.IsDBNull(4) ? "Estudiante" : reader.GetString(4)
                };
            }

            return null;
        }

        public string GetFullName(string nombre, string apellido)
        {
            var fullName = $"{nombre} {apellido}".Trim();
            return string.IsNullOrEmpty(fullName) ? "Usuario" : fullName;
        }
    }

    public class UserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = "Estudiante";
    }
}
