using Microsoft.Data.Sqlite;

namespace MadoHomuAPIv2
{
    public static class HttpContextUserExtensions
    {
        private static readonly string IsUserRetrievedKey = "IsUserRetrieved";
        private static readonly string UserKey = "User";
        public static User.UserDTO? User(this HttpContext context)
        {
            if (context.Items[IsUserRetrievedKey] != null) return context.Items[UserKey] as User.UserDTO;
            else
            {
                var user = context.DbConnection().GetUserByRequest(context.Request);
                context.Items[UserKey] = user;
                context.Items[IsUserRetrievedKey] = true;
                return user;
            }
        }
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
