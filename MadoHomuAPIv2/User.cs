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
    }
}
