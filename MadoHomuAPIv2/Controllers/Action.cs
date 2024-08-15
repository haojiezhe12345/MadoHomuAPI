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
        public ResponseVO TakeAction(ActionDTO actionDTO)
        {
            ResponseVO response = new();
            using var command = HttpContext.DbConnection().CreateCommand();
            command.CommandText = "SELECT * FROM actions WHERE id = @id";
            command.Parameters.AddWithValue("id", actionDTO.id ?? "");
            var action = command.ReadAsDTO<ActionRead>();

            if (action == null || actionDTO.id == null)
            {
                response.SetCode(ResponseCode.ActionNotExist);
                return response;
            }

            if (action.expired == 1 || (action.expire_time != null && DateTimeOffset.Now > DateTimeOffset.FromUnixTimeSeconds((long)action.expire_time)))
            {
                response.SetCode(ResponseCode.ActionExpired);
                return response;
            }

            if (!Enum.TryParse(action.type, out ActionType type))
            {
                response.SetCode(ResponseCode.ActionTypeNotRecognized);
                return response;
            }

            switch (type)
            {
                case ActionType.EmailConfirm:
                    var actionData = JsonSerializer.Deserialize<UserParamUpdateDTO>(action.data ?? "null");
                    if (actionData == null)
                    {
                        response.SetCode(ResponseCode.ActionDataInvalid);
                        return response;
                    }
                    HttpContext.DbConnection().SetUserParamById(actionData.id, "email", actionData.data);
                    Utils.Log($"User (id={actionData.id}) confirmed new email");
                    HttpContext.DbConnection().ExpireActionEncrypted(actionDTO.id);
                    response.SetCode(ResponseCode.Success);
                    break;
            }
            return response;
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

        public static string CreateAction(this SqliteConnection connection, ActionType type, string data, int? validTimeSeconds = null)
        {
            var id = Guid.NewGuid().ToString();
            connection.InsertDTO("actions", new ActionWrite
            {
                id = id,
                type = type.ToString(),
                data = data,
                expire_time = validTimeSeconds != null ? DateTimeOffset.Now.ToUnixTimeSeconds() + validTimeSeconds : null,
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