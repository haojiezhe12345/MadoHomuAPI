using Microsoft.Data.Sqlite;

namespace MadoHomuAPIv2
{
    public class UserAuthMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            string? token = context.Request.Headers["token"];
            bool IsTokenInvalid = false;

            if (token != null)
            {
                var user = context.DbConnection().GetUser("token", token);
                if (user == null)
                    IsTokenInvalid = true;
                else context.SetUser(user);
            }

            if (IsTokenInvalid)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid token");
            }
            else await next(context);
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
            public string? name { get; set; }
            public string? email { get; set; }
            public string? password { get; set; }
        }

        public class RegisterDTO
        {
            public required string name { get; set; }
            public string? email { get; set; }
            public string? password { get; set; }
        }

        public class FindUserDTO
        {
            public int? id { get; set; }
            public string? name { get; set; }
            public bool? hasEmail { get; set; }
        }

        public class ChangeEmailDTO
        {
            public required string email { get; set; }
        }

        public class ImageUploadDTO
        {
            public required string image { get; set; }
        }

        public static UserDTO? GetUser(this SqliteConnection connection, string key, object value, string ExtraParam = "")
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM users WHERE {key} = @{key} {ExtraParam}";
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

        public static int CheckEmail(this SqliteConnection connection, string email)
        {
            if (email.Length < 5)
                return (int)ResponseCode.EmailNotValid;
            if (connection.GetUser("email", email) != null)
                return (int)ResponseCode.EmailAlreadyRegistered;
            return 1;
        }
    }
}
