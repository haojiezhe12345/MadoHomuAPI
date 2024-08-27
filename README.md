# MadoHomuAPI
Backend for [MadoHomu.love](https://madohomu.love/)

**Demo:** http://haojiezhe12345.top:8001/api/swagger/index.html  
(Dedicated for testing purposes, you can post anything you want. It won't affect the main website)

### IDE
- Visual Studio 2022
- with `ASP.NET and web development` installed
- [Tutorial](https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-8.0&tabs=visual-studio)

### Files required to run
|      |      |
| ---- | ---- |
| `data/main.db` | Main database |
| `data/config_location.txt` | Location of `config.json` |
| `config.json` | Database encryption keys and email config, defined in: [Utils.cs:12](MadoHomuAPIv2/Utils.cs#L12) |

### Files generated upon running
|      |      |
| ---- | ---- |
| `data/log.txt` | Log file |
| `data/images/avatars` | Uploaded avatars |
| `data/images/posts` | Posted images |
