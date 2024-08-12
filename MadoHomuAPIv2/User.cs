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
                var user = context.DbConnection().GetUser("token", Utils.EncryptString(token));
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
        public class UserDecrypted
        {
            private string? _email;
            private string? _token;
            private string? _password;
            public string? email
            {
                get => _email;
                set => SetDecrypted(ref _email, value);
            }
            public string? token
                {
                get => _token;
                set => SetDecrypted(ref _token, value);
                }
            public string? password
            {
                get => _password;
                set => SetDecrypted(ref _password, value);
            }
            private void SetDecrypted(ref string? target, string? value)
            {
                if (target != null)
                    throw new Exception($"Cannot set property in {this.GetType().Name} for more than once because it stores a decrypted value");
                if (value != null) target = Utils.DecryptString(value);
        }
        }

        public class UserEncrypted
        {
            private string? _email;
            private string? _password;
            public string? email
            {
                get => _email;
                set => SetEncrypted(ref _email, value);
            }
            public string? password
                {
                get => _password;
                set => SetEncrypted(ref _password, value);
                }
            private void SetEncrypted(ref string? target, string? value)
            {
                if (target != null)
                    throw new Exception($"Cannot set property in {this.GetType().Name} for more than once because it stores an encrypted value");
                if (value != null) target = Utils.EncryptString(value);
            }
        }

        public class UserPO : UserDecrypted
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
            public long? create_time { get; set; }
        }

        public class LoginDTO : UserEncrypted
        {
            public string? name { get; set; }
        }

        public class UserUpdateDTO : UserEncrypted
        {
            public string? name { get; set; }
            public string? avatar { get; set; }
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
            if (connection.SetUserParamById(id, "token", Utils.EncryptString(token)) == 1)
                return token;
            else return "";
        }

        public static ResponseCode CheckEmailEncrypted(this SqliteConnection connection, string email)
        {
            if (Utils.DecryptString(email).Length < 1)
                return ResponseCode.EmailNotValid;
            if (connection.GetUser("email", email) != null)
                return ResponseCode.EmailAlreadyRegistered;
            return ResponseCode.Success;
        }
    }
}
