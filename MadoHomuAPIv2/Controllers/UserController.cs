using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MadoHomuAPIv2.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        [HttpPost("login")]
        public void Login([FromBody] string value)
        {
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

            System.IO.File.AppendAllTextAsync(@".\data\log.txt", $"[{DateTime.Now}] Avatar {name} has been uploaded\n");

            return name;
        }
    }
}
