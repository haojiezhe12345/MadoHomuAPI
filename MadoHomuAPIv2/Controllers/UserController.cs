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
        public ResponseDTO Login(LoginDTO login)
        {
            UserDTO? user = null;
            ResponseDTO response = new();

            if (login.email != null)
            {
                user = HttpContext.DbConnection().GetUser("email", login.email);
                response.code = 1000;
                response.message = "Email not registered";
            }
            else if (login.name != null)
            {
                user = HttpContext.DbConnection().GetUser("name", login.name, "AND email is NULL");
                response.code = 1001;
                response.message = "User not found";
            }

            if (user != null && user.id != null)
            {
                response.code = 1;
                response.message = "Success";
                response.data = (user.token == null || user.token.Length < 8)
                    ? HttpContext.DbConnection().GenerateTokenForUserById((int)user.id)
                    : user.token;
                Utils.Log($"User logged on successfully: {user.name} (id={user.id})");
            }

            return response;
        }

        [HttpPost("register")]
        public ResponseDTO Register(RegisterDTO reg)
        {
            ResponseDTO response = new();

            if (reg.email == null)
            {
                if (HttpContext.DbConnection().GetUser("name", reg.name, "AND email is NULL") != null)
                {
                    response.code = 1000;
                    response.message = "User already exists";
                    return response;
                }
            }
            else
            {
                if (reg.email.Length < 5)
                {
                    response.code = 1001;
                    response.message = "Email is not valid";
                    return response;
                }
                if (HttpContext.DbConnection().GetUser("email", reg.email) != null)
                {
                    response.code = 1002;
                    response.message = "Email already registered";
                    return response;
                }
            }

            HttpContext.DbConnection().InsertDTO("users", reg);

            var user = reg.email == null
                ? HttpContext.DbConnection().GetUser("name", reg.name, "AND email is NULL")
                : HttpContext.DbConnection().GetUser("email", reg.email);

            if (user != null && user.id != null)
            {
                response.code = 1;
                response.message = "Success";
                response.data = HttpContext.DbConnection().GenerateTokenForUserById((int)user.id);
                Utils.Log($"New user registered: {user.name} (id={user.id})");
            }

            return response;
        }

        [HttpPost("/upload")]
        public string UploadAvatar()
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
