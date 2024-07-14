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
        public ResponseVO Register(RegisterDTO reg)
        {
            ResponseVO response = new();

            if (reg.email != null)
            {
                var check = HttpContext.DbConnection().CheckEmail(reg.email);
                if (check != 1)
                {
                    response.SetCode((ResponseCode)check);
                    return response;
                }
            }
            else
            {
                if (HttpContext.DbConnection().GetUser("name", reg.name, "AND email is NULL") != null)
                {
                    response.SetCode(ResponseCode.UserAlreadyExists);
                    return response;
                }
            }

            HttpContext.DbConnection().InsertDTO("users", reg);

            var user = reg.email == null
                ? HttpContext.DbConnection().GetUser("name", reg.name, "AND email is NULL")
                : HttpContext.DbConnection().GetUser("email", reg.email);

            if (user != null)
            {
                response.SetCode(ResponseCode.Success);
                response.data = HttpContext.DbConnection().GenerateTokenForUserById(user.id);
                Utils.Log($"New user registered: {user.name} (id={user.id})");
            }

            return response;
        }

        [HttpPost("changeEmail")]
        [UserLoginRequired]
        public ResponseVO ChangeEmail(SingleStringDTO data)
        {
            ResponseVO response = new();

            var user = HttpContext.User();

            var check = HttpContext.DbConnection().CheckEmail(data.data);
            if (check != 1)
            {
                response.SetCode((ResponseCode)check);
                return response;
            }

            response.SetCode(ResponseCode.Success);
            response.data = HttpContext.DbConnection().SetUserParamById(user.id, "email", data.data);

            Utils.Log($"User email changed: {user.name} (id={user.id})");

            return response;
        }

        [HttpPost("changeName")]
        [UserLoginRequired]
        public ResponseVO ChangeName(SingleStringDTO data)
        {
            ResponseVO response = new();

            var user = HttpContext.User();

            if (user.email == null)
            {
                response.SetCode(ResponseCode.UserNameChangeRequireEmail);
                return response;
            }

            response.SetCode(ResponseCode.Success);
            response.data = HttpContext.DbConnection().SetUserParamById(user.id, "name", data.data);

            Utils.Log($"User {user.name} (id={user.id}) changed name to {data.data}");

            return response;
        }

        [HttpPost("uploadAvatar")]
        [UserLoginRequired]
        public string UploadAvatar(SingleStringDTO data)
        {
            var user = HttpContext.User();

            var filename = Utils.WriteFileFromBase64WithRandomName(@"data\images\avatars\{0}.jpg", data.data);

            HttpContext.DbConnection().SetUserParamById(user.id, "avatar", filename + ".jpg");

            Utils.Log($"User {user.name} (id={user.id}) uploaded an avatar: {filename}.jpg");

            return filename + ".jpg";
        }

        [HttpGet("me")]
        [UserLoginRequired]
        public UserMeVO UserMe()
        {
            var user = HttpContext.User();
            return new UserMeVO
            {
                id = user.id,
                name = user.name,
                avatar = user.avatar,
                email = user.email,
            };
        }

        [HttpGet("find/{name}")]
        public List<UserFindVO> FindUser(string name)
        {
            List<UserFindVO> result = [];

            var command = HttpContext.DbConnection().CreateCommand();
            command.CommandText = "SELECT * FROM users WHERE name = @name";
            command.Parameters.AddWithValue("name", name);
            var foundlist = command.ReadAsDTOList<UserPO>();

            foundlist.ForEach(user =>
            {
                result.Add(new UserFindVO
                {
                    id = user.id,
                    name = user.name,
                    avatar = user.avatar,
                    hasEmail = user.email != null,
                });
            });

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
