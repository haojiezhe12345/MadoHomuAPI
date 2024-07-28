using Microsoft.Data.Sqlite;

namespace MadoHomuAPIv2
{
    [AttributeUsage(AttributeTargets.Method)]
    public class UserLoginRequiredAttribute : Attribute;

    public class UserAuthMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            string? token = context.Request.Headers["token"];

            if (token != null)
            {
                var user = context.DbConnection().GetUser("token", token);
                if (user == null)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Invalid token");
                    return;
                }
                else context.SetUser(user);
            }

            if (context.GetEndpoint()?.Metadata.GetMetadata<UserLoginRequiredAttribute>() != null && !context.UserLoggedIn())
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Login required");
                return;
            }

            await next(context);
        }
    }

    public static class HttpContextUserExtensions
    {
        private static readonly string DbConnectionKey = "User";
        public static User.UserPO User(this HttpContext context)
        {
            if (context.Items[DbConnectionKey] is User.UserPO user)
                return user;
            else throw new Exception("Trying to access null user in a login required controller");
        }
        public static bool UserLoggedIn(this HttpContext context) => context.Items[DbConnectionKey] != null;
        public static void SetUser(this HttpContext context, object? value) => context.Items[DbConnectionKey] = value;
    }

    public static class User
    {
        public class UserEmailDecrypted
        {
            private string? _email;
            public string? email
            {
                get => _email;
                set
                {
                    if (_email != null)
                        throw new Exception($"Cannot set email in {this.GetType().Name} for more than once because it stores a decrypted value");
                    if (value != null) _email = Utils.DecryptString(value);
                }
            }
        }

        public class UserEmailEncrypted
        {
            private string? _email;
            public string? email
            {
                get => _email;
                set
                {
                    if (_email != null)
                        throw new Exception($"Cannot set email in {this.GetType().Name} for more than once because it stores an encrypted value");
                    if (value != null) _email = Utils.EncryptString(value);
                }
            }
        }

        public class UserPO : UserEmailDecrypted
        {
            private int? _id;
            private string? _avatar;
            public int id
            {
                get
                {
                    if (_id != null) return (int)_id;
                    else throw new Exception("User's 'id' attribute should not be null");
                }
                set => _id = value;
            }
            public string? name { get; set; }
            public string avatar
            {
                get => _avatar ?? "default.png";
                set => _avatar = value;
            }
            public string? password { get; set; }
            public string? token { get; set; }
            public long? create_time { get; set; }
        }

        public class LoginDTO : UserEmailEncrypted
        {
            public string? name { get; set; }
            public string? password { get; set; }
        }

        public class UserUpdateDTO : UserEmailEncrypted
        {
            public string? name { get; set; }
            public string? avatar { get; set; }
            public string? password { get; set; }
        }

        public class UserGetVO(UserPO? user = null)
        {
            public int? id { get; set; } = user?.id;
            public string? name { get; set; } = user?.name;
            public string? avatar { get; set; } = user?.avatar;
            public bool? hasEmail { get; set; } = user?.email != null;
            public long? create_time { get; set; } = user?.create_time;
        }

        public class UserMeVO(UserPO? user = null) : UserGetVO(user)
        {
            public string? email { get; set; } = user?.email;
        }

        public static UserPO? GetUser(this SqliteConnection connection, string key, object value, string ExtraParam = "")
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT * FROM users WHERE {key} = @{key} {ExtraParam}";
            command.Parameters.AddWithValue($"@{key}", value);
            return command.ReadAsDTO<UserPO>();
        }

        public static int SetUserParamById(this SqliteConnection connection, int id, string key, object value)
        {
            return connection.UpdateFieldById("users", id, key, value);
        }

        public static string GenerateTokenForUserById(this SqliteConnection connection, int id)
        {
            string token = Guid.NewGuid().ToString();
            if (connection.SetUserParamById(id, "token", token) == 1)
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
