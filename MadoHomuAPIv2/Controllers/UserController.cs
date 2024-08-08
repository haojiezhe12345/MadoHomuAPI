using Microsoft.AspNetCore.Mvc;
using static MadoHomuAPIv2.User;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MadoHomuAPIv2.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        [HttpPost("login")]
        public ResponseVO Login(LoginDTO login)
        {
            UserPO? user = null;
            ResponseVO response = new();

            if (login.email != null)
            {
                user = HttpContext.DbConnection().GetUser("email", login.email);
                response.SetCode(ResponseCode.EmailNotRegistered);
            }
            else if (login.name != null)
            {
                user = HttpContext.DbConnection().GetUser("name", login.name, "AND email is NULL");
                response.SetCode(ResponseCode.UserNotFound);
            }
            else
            {
                response.SetCode(ResponseCode.LoginCredentialInsufficient);
            }

            if (user != null)
            {
                response.SetCode(ResponseCode.Success);
                response.data = (user.token == null || user.token.Length < 8)
                    ? HttpContext.DbConnection().GenerateTokenForUserById(user.id)
                    : user.token;
                Utils.Log($"User logged on successfully: {user.name} (id={user.id})");
            }

            return response;
        }

        [HttpPost("register")]
        public ResponseVO Register(UserUpdateDTO reg)
        {
            ResponseVO response = new();

            if (reg.email != null && reg.name != null)
            {
                var check = HttpContext.DbConnection().CheckEmail(reg.email);
                if (check != 1)
                {
                    response.SetCode((ResponseCode)check);
                    return response;
                }
            }
            else if (reg.name != null)
            {
                if (HttpContext.DbConnection().GetUser("name", reg.name, "AND email is NULL") != null)
                {
                    response.SetCode(ResponseCode.UserAlreadyExists);
                    return response;
                }
            }
            else
            {
                response.SetCode(ResponseCode.UserRegisterRequireName);
                return response;
            }

            if (reg.avatar != null)
            {
                var filename = Utils.EscapeFilename($"{reg.name}.{DateTime.UtcNow.Ticks}.jpg");
                Utils.WriteFileFromBase64(@$"data\images\avatars\{filename}", reg.avatar);
                reg.avatar = filename;
            }

            HttpContext.DbConnection().InsertDTO("users", reg);

            var user = reg.email == null
                ? HttpContext.DbConnection().GetUser("name", reg.name, "AND email is NULL")
                : HttpContext.DbConnection().GetUser("email", reg.email);

            if (user != null)
            {
                response.SetCode(ResponseCode.Success);
                response.data = HttpContext.DbConnection().GenerateTokenForUserById(user.id);
                Utils.Log($"New user registered: {user.name} (id={user.id}, avatar={reg.avatar})");
            }

            return response;
        }

        [HttpPut("update")]
        [UserLoginRequired]
        public ResponseVO UpdateUser(UserUpdateDTO update)
        {
            bool allowUserNameChangeWithoutEmail = true;
            ResponseVO response = new();
            int updated = 0;

            var user = HttpContext.User();

            if (update.email != null)
            {
                var check = HttpContext.DbConnection().CheckEmail(update.email);
                if (check != 1)
                {
                    response.SetCode((ResponseCode)check);
                    return response;
                }
                updated += HttpContext.DbConnection().SetUserParamById(user.id, "email", update.email);
                Utils.Log($"User email changed: {user.name} (id={user.id})");
            }

            if (update.name != null)
            {
                if (user.email == null)
                {
                    if (allowUserNameChangeWithoutEmail)
                    {
                        if (HttpContext.DbConnection().GetUser("name", update.name, "AND email is NULL") != null)
                        {
                            response.SetCode(ResponseCode.UserNameChangeDuplicates);
                            return response;
                        }
                    }
                    else
                    {
                        response.SetCode(ResponseCode.UserNameChangeRequireEmail);
                        return response;
                    }
                }
                updated += HttpContext.DbConnection().SetUserParamById(user.id, "name", update.name);
                Utils.Log($"User {user.name} (id={user.id}) changed name to {update.name}");
            }

            if (update.avatar != null)
            {
                var filename = Utils.EscapeFilename($"{user.name}.{DateTime.UtcNow.Ticks}.jpg");
                Utils.WriteFileFromBase64(@$"data\images\avatars\{filename}", update.avatar);
                updated += HttpContext.DbConnection().SetUserParamById(user.id, "avatar", filename);
                Utils.Log($"User {user.name} (id={user.id}) uploaded an avatar: {filename}");
            }

            response.SetCode(ResponseCode.Success);
            response.data = updated;
            return response;
        }

        [HttpGet("me")]
        [UserLoginRequired]
        public UserMeVO UserMe()
        {
            return new UserMeVO(HttpContext.User());
        }

        [HttpGet("find")]
        public List<UserGetVO> FindUser(string? name, int? id)
        {
            List<UserGetVO> result = [];

            if (name != null)
            {
                using var command = HttpContext.DbConnection().CreateCommand();
                command.CommandText = "SELECT * FROM users WHERE name = @name";
                command.Parameters.AddWithValue("name", name);
                var foundlist = command.ReadAsDTOList<UserPO>();

                foundlist.ForEach(user =>
                {
                    result.Add(new(user));
                });
            }
            if (id != null)
            {
                result.Add(new(HttpContext.DbConnection().GetUser("id", id)));
            }

            return result;
        }

        [HttpPost("/upload")]
        public string UploadAvatarOld()
        {
            //app.Logger.LogInformation(request.Form.Files.Count.ToString());
            if (Request.Form.Files.Count == 0)
            {
                return "Please select a file!";
            }

            //var ContentType = request.Form.Files[0].ContentType;
            var name = Request.Form.Files[0].Name;

            var invalidFileChars = "\\/:*?\"<>|";
            var validFileChars = "＼／：＊？＂＜＞｜";
            for (int i = 0; i < invalidFileChars.Length; i++)
            {
                name = name.Replace(invalidFileChars[i], validFileChars[i]);
            }

            if (name == "" || name == ".jpg" || name == "匿名用户.jpg" || name.Contains('/') || name.Contains('\\'))
            {
                return "invalid username";
            }

            using (var stream = new FileStream(@$"data\images\avatars\{name}", FileMode.Create))
            {
                Request.Form.Files[0].CopyTo(stream);
            }

            Utils.Log($"Avatar {name} has been uploaded");

            return name;
        }
    }
}
