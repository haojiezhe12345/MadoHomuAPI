using Microsoft.Data.Sqlite;

namespace MadoHomuAPIv2
{
    public class UserMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            context.SetUser(context.DbConnection().GetUserByRequest(context.Request));
            await next(context);
        }
    }

    public static class HttpContextUserExtensions
    {
        private static readonly string DbConnectionKey = "User";
        public static User.UserDTO? User(this HttpContext context) => context.Items[DbConnectionKey] as User.UserDTO;
        public static void SetUser(this HttpContext context, object? value) => context.Items[DbConnectionKey] = value;
    }

    public static class User
    {
        public class UserDTO
        {
            public int? id { get; set; }
            public string? name { get; set; }
            public string? avatar { get; set; }
            public string? email { get; set; }
            public string? password { get; set; }
            public string? token { get; set; }
        }

        public class LoginDTO
        {
            public string? email { get; set; }
            public string? password { get; set; }
        }

        public static UserDTO? GetUser(this SqliteConnection connection, string key, object value)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM users WHERE {key} = @{key}";
            command.Parameters.AddWithValue($"@{key}", value);
            return command.ReadAsDTO<UserDTO>();
        }

        public static string GenerateTokenForUserById(this SqliteConnection connection, int id)
        {
            string token = Guid.NewGuid().ToString();
            using var command = connection.CreateCommand();
            command.CommandText = $"UPDATE users SET token = '{token}' WHERE id = {id}";
            if (command.ExecuteNonQuery() == 1)
                return token;
            else return "";
        }

        public static UserDTO? GetUserByRequest(this SqliteConnection connection, HttpRequest request)
        {
            string? token = request.Headers["token"];
            if (token != null)
                return connection.GetUser("token", token);
            else return null;
        }
    }
}
