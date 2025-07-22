using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using static MadoHomuAPIv2.User;
using static MadoHomuAPIv2.Action;
using MadoHomuAPIv2.Response;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MadoHomuAPIv2.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        [HttpPost("login")]
        public ApiResponse<string> Login(LoginDTO login)
        {
            UserPO? user = null;
            ApiResponse<string> response;

            if (login.email != null)
            {
                user = HttpContext.DbConnection().GetUser("email", login.email);
                response = this.ApiResponse(ResponseCode.EmailNotRegistered);
            }
            else if (login.name != null)
            {
                user = HttpContext.DbConnection().GetUser("name", login.name, "AND email is NULL");
                response = this.ApiResponse(ResponseCode.UserNotFound);
            }
            else
            {
                response = this.ApiResponse(ResponseCode.LoginCredentialInsufficient);
            }

            if (user == null) return response;

            if (user.password != null && user.password != login.password)
            {
                return this.ApiResponse(ResponseCode.LoginPasswordIncorrect);
            }

            response = this.ApiResponse(
                ResponseCode.Success,
                (user.token == null || user.token.Length < 8)
                    ? HttpContext.DbConnection().GenerateTokenForUserById(user.id)
                    : user.token
            );
            Utils.Log($"User logged on successfully: '{user.name}' (id={user.id})");

            return response;
        }

        [HttpPost("register")]
        public ApiResponse<string> Register(UserUpdateDTO reg)
        {
            if (reg.email != null && reg.name != null)
            {
                var check = HttpContext.DbConnection().CheckEmailEncrypted(reg.email);
                if (check != ResponseCode.Success)
                {
                    return this.ApiResponse(check);
                }
            }
            else if (reg.name != null)
            {
                if (HttpContext.DbConnection().GetUser("name", reg.name, "AND email is NULL") != null)
                {
                    return this.ApiResponse(ResponseCode.UserAlreadyExists);
                }
            }
            else
            {
                return this.ApiResponse(ResponseCode.UserRegisterRequireName);
            }

            if (reg.avatar != null)
            {
                var filename = Utils.EscapeFilename($"{reg.name}.{DateTime.UtcNow.Ticks}.jpg");
                Utils.WriteFileFromBase64($"data/images/avatars/{filename}", reg.avatar);
                reg.avatar = filename;
            }

            HttpContext.DbConnection().InsertDTO("users", reg);

            var user = reg.email == null
                ? HttpContext.DbConnection().GetUser("name", reg.name, "AND email is NULL")
                : HttpContext.DbConnection().GetUser("email", reg.email);

            ApiResponse<string> response = this.ApiResponse(
                ResponseCode.Success,
                HttpContext.DbConnection().GenerateTokenForUserById(user.id)
            );
            Utils.Log($"New user registered: '{user.name}' (id={user.id}, avatar={reg.avatar}, email={user.email != null}, password={user.password != null})");

            return response;
        }

        [HttpPut("update")]
        [UserLoginRequired]
        public async Task<ApiResponse<int>> UpdateUser(UserUpdateDTO update)
        {
            int updated = 0;

            var user = HttpContext.User();

            if (update.email != null)
            {
                var check = HttpContext.DbConnection().CheckEmailEncrypted(update.email);
                if (check != ResponseCode.Success)
                {
                    return this.ApiResponse(check);
                }
                Utils.Log($"User '{user.name}' (id={user.id}) requested to change email");

                var actionId = Guid.NewGuid().ToString();
                const int expireHours = 24;

                var result = await Utils.SendEmail
                (
                    Utils.DecryptString(update.email),
                    "确认修改邮箱 | Verify your email",
                    String.Format(System.IO.File.ReadAllText("emails/ConfirmEmail.html"), actionId, expireHours)
                );

                if (result != ResponseCode.Success)
                {
                    return this.ApiResponse(result);
                }

                HttpContext.DbConnection().CreateAction(
                    ActionType.EmailConfirm,
                    JsonSerializer.Serialize(new UserParamUpdateDTO
                    {
                        id = user.id,
                        data = update.email,
                    }),
                    expireHours * 60 * 60,
                    actionId
                );

                return this.ApiResponse(ResponseCode.Success, expireHours);
            }

            if (update.name != null)
            {
                if (user.email == null)
                {
                    if (Config.Data.AllowUserNameChangeWithoutEmail == true)
                    {
                        if (HttpContext.DbConnection().GetUser("name", update.name, "AND email is NULL") != null)
                        {
                            return this.ApiResponse(ResponseCode.UserNameChangeDuplicates);
                        }
                    }
                    else
                    {
                        return this.ApiResponse(ResponseCode.UserNameChangeRequireEmail);
                    }
                }
                updated += HttpContext.DbConnection().SetUserParamById(user.id, "name", update.name);
                Utils.Log($"User '{user.name}' (id={user.id}) changed name to '{update.name}'");
            }

            if (update.avatar != null)
            {
                var filename = Utils.EscapeFilename($"{user.name}.{DateTime.UtcNow.Ticks}.jpg");
                Utils.WriteFileFromBase64($"data/images/avatars/{filename}", update.avatar);
                updated += HttpContext.DbConnection().SetUserParamById(user.id, "avatar", filename);
                Utils.Log($"User '{user.name}' (id={user.id}) uploaded an avatar: '{filename}'");
            }

            if (update.password != null)
            {
                updated += HttpContext.DbConnection().SetUserParamById(user.id, "password", update.password);
                Utils.Log($"User '{user.name}' (id={user.id}) changed password");
                if (user.email != null)
                    _ = Utils.SendEmail(
                        user.email, "您的密码已更改 | Your password has been updated",
                        string.Format(System.IO.File.ReadAllText("emails/PasswordUpdated.html"))
                    );
            }

            return this.ApiResponse(ResponseCode.Success, updated);
        }

        [HttpPost("resetpassword")]
        public async Task<ApiResponse<int>> ResetPassword(string email)
        {
            var uid = HttpContext.DbConnection().GetUser("email", Utils.EncryptString(email))?.id;

            if (uid == null)
            {
                return this.ApiResponse(ResponseCode.EmailNotRegistered);
            }

            Utils.Log($"User (id={uid}) requested resetting password");

            var actionId = Guid.NewGuid().ToString();
            const int expireMinutes = 60;

            var result = await Utils.SendEmail(
                email, "重置密码 | Reset your password",
                String.Format(System.IO.File.ReadAllText("emails/ResetPassword.html"), actionId, expireMinutes)
            );

            if (result != ResponseCode.Success)
            {
                return this.ApiResponse(result);
            }

            HttpContext.DbConnection().CreateAction(ActionType.PasswordReset, uid.ToString(), expireMinutes * 60, actionId);

            return this.ApiResponse(ResponseCode.Success, expireMinutes);
        }

        [HttpPost("resettoken")]
        [UserLoginRequired]
        public bool ResetToken()
        {
            var user = HttpContext.User();
            Utils.Log($"Resetting token for user '{user.name}' (id={user.id})");
            return HttpContext.DbConnection().GenerateTokenForUserById(user.id) != "";
        }

        [HttpGet("me")]
        [UserLoginRequired]
        public UserMeVO UserMe()
        {
            return new UserMeVO(HttpContext.User());
        }

        [HttpGet("find")]
        public List<UserGetVO> FindUser(string? name, int? id, string? email)
        {
            List<UserGetVO> result = [];

            if (email != null)
            {
                var foundUser = HttpContext.DbConnection().GetUser("email", Utils.EncryptString(email));
                if (foundUser != null) result.Add(new(foundUser));
            }
            if (name != null)
            {
                using var command = HttpContext.DbConnection().CreateCommand();
                command.CommandText = "SELECT * FROM users WHERE name = @name";
                command.Parameters.AddWithValue("name", name);
                command.ReadAsDTOList<UserPO>().ForEach(user => result.Add(new(user)));
            }
            if (id != null)
            {
                var foundUser = HttpContext.DbConnection().GetUser("id", id);
                if (foundUser != null) result.Add(new(foundUser));
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

            using (var stream = new FileStream($"data/images/avatars/{name}", FileMode.Create))
            {
                Request.Form.Files[0].CopyTo(stream);
            }

            Utils.Log($"Avatar '{name}' has been uploaded");

            return name;
        }
    }
}
