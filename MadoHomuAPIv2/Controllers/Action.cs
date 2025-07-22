using MadoHomuAPIv2.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using static MadoHomuAPIv2.Action;
using static MadoHomuAPIv2.User;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MadoHomuAPIv2.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ActionController : ControllerBase
    {
        [HttpPost]
        public ApiResponse TakeAction(ActionDTO actionDTO)
        {
            using var command = HttpContext.DbConnection().CreateCommand();
            command.CommandText = "SELECT * FROM actions WHERE id = @id";
            command.Parameters.AddWithValue("id", actionDTO.id ?? "");
            var action = command.ReadAsDTO<ActionRead>();

            if (action == null || actionDTO.id == null)
            {
                return this.ApiResponse(ResponseCode.ActionNotExist);
            }

            if (action.expired == 1 || (action.expire_time != null && DateTimeOffset.Now > DateTimeOffset.FromUnixTimeSeconds((long)action.expire_time)))
            {
                return this.ApiResponse(ResponseCode.ActionExpired);
            }

            if (!Enum.TryParse(action.type, out ActionType type))
            {
                return this.ApiResponse(ResponseCode.ActionTypeNotRecognized);
            }

            switch (type)
            {
                case ActionType.EmailConfirm:
                    {
                        var actionData = JsonSerializer.Deserialize<UserParamUpdateDTO>(action.data ?? "null");
                        if (actionData == null)
                        {
                            return this.ApiResponse(ResponseCode.ActionDataInvalid);
                        }

                        var oldEmail = HttpContext.DbConnection().GetUser("id", actionData.id)?.email;

                        HttpContext.DbConnection().SetUserParamById(actionData.id, "email", actionData.data);
                        Utils.Log($"User (id={actionData.id}) confirmed new email");

                        if (oldEmail != null)
                            _ = Utils.SendEmail(
                                oldEmail, "您的邮箱已更改 | Your email has been updated",
                                string.Format(System.IO.File.ReadAllText("emails/EmailUpdated.html"), Utils.DecryptString(actionData.data))
                            );

                        break;
                    }

                case ActionType.PasswordReset:
                    {
                        if (!int.TryParse(action.data, out int uid))
                        {
                            return this.ApiResponse(ResponseCode.ActionDataInvalid);
                        }

                        HttpContext.DbConnection().SetUserParamById(
                            uid, "password",
                            new UserUpdateDTO() { password = actionDTO.data }.password ?? (object)DBNull.Value
                        );
                        Utils.Log($"User (id={uid}) reset password by email link");

                        var user = HttpContext.DbConnection().GetUser("id", uid);
                        if (user != null && user.email != null)
                            _ = Utils.SendEmail(
                                user.email, "您的密码已更改 | Your password has been updated",
                                string.Format(System.IO.File.ReadAllText("emails/PasswordUpdated.html"))
                            );

                        break;
                    }
            }

            HttpContext.DbConnection().ExpireActionEncrypted(actionDTO.id);
            return this.ApiResponse(ResponseCode.Success);
        }
    }

}

namespace MadoHomuAPIv2
{
    public static class Action
    {
        public class ActionEncrypted
        {
            private string? _id = null;
            public string? id
            {
                get => _id;
                set => Utils.SetEncryptedValue(ref _id, value);
            }
        }

        public class ActionRead
        {
            public string? type { get; set; }
            public string? data { get; set; }
            public int? expired { get; set; }
            public long? create_time { get; set; }
            public long? expire_time { get; set; }
        }

        public class ActionWrite : ActionEncrypted
        {
            public string? type { get; set; }
            public string? data { get; set; }
            public long? expire_time { get; set; }
        }

        public class ActionDTO : ActionEncrypted
        {
            public string? data { get; set; }
        }

        public enum ActionType
        {
            EmailConfirm,
            PasswordReset,
        }

        public static string CreateAction(this SqliteConnection connection, ActionType type, string? data, int? expireSeconds = null, string? id = null)
        {
            id ??= Guid.NewGuid().ToString();
            connection.InsertDTO("actions", new ActionWrite
            {
                id = id,
                type = type.ToString(),
                data = data,
                expire_time = expireSeconds != null ? DateTimeOffset.Now.ToUnixTimeSeconds() + expireSeconds : null,
            });
            Utils.Log($"Action '{type}' created successfully");
            return id;
        }

        public static int ExpireActionEncrypted(this SqliteConnection connection, string id)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE actions SET expired = 1 where id = @id";
            command.Parameters.AddWithValue("id", id);
            var result = command.ExecuteNonQuery();
            if (result == 1) Utils.Log("Action expired successfully");
            return result;
        }
    }
}