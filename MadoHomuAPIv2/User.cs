using Microsoft.Data.Sqlite;

namespace MadoHomuAPIv2
{
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

        public static UserDTO? GetUserById(this SqliteConnection connection, int id)
        {
            var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM users WHERE id = {id}";
            return command.ReadAsDTO<UserDTO>();
        }

        public static UserDTO? GetUserByEmail(this SqliteConnection connection, string email)
        {
            var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM users WHERE email = @email";
            command.Parameters.AddWithValue("@email", email);
            return command.ReadAsDTO<UserDTO>();
        }

        public static string GenerateTokenForUserById(this SqliteConnection connection, int id)
        {
            string token = Guid.NewGuid().ToString();
            var command = connection.CreateCommand();
            command.CommandText = $"UPDATE users SET token = '{token}' WHERE id = {id}";
            if (command.ExecuteNonQuery() == 1) 
                return token;
            else return "";
        }
    }
}
