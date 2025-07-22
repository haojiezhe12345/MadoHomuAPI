namespace MadoHomuAPIv2.Response
{
    public static class ResponseMessages
    {
        public const string DefaultLanguage = "en";

        public static string Get(ResponseCode responseCode, string? languageCode = DefaultLanguage)
        {
            languageCode ??= DefaultLanguage;

            if (messages.TryGetValue(languageCode, out var languageMessages))
            {
                if (languageMessages.TryGetValue(responseCode, out var message))
                {
                    return message;
                }
            }

            if (messages.TryGetValue(DefaultLanguage, out var defaultLanguageMessages))
            {
                if (defaultLanguageMessages.TryGetValue(responseCode, out var message))
                {
                    return message;
                }
            }

            return responseCode.ToString();
        }

        public static readonly Dictionary<string, Dictionary<ResponseCode, string>> messages = new()
        {
            {
                "en", new()
                {
                    { ResponseCode.Success, "Success" },

                    { ResponseCode.EmailNotRegistered, "Email is not registered" },
                    { ResponseCode.EmailAlreadyRegistered, "Email is already registered" },
                    { ResponseCode.EmailNotValid, "Email format is not valid" },
                    { ResponseCode.EmailSendFailed, "Failed to send email" },

                    { ResponseCode.UserNotFound, "User not found" },
                    { ResponseCode.UserAlreadyExists, "User already exists" },
                    { ResponseCode.UserRegisterRequireName, "Name is required for registration" },
                    { ResponseCode.UserNameChangeRequireEmail, "Email is required to change username" },
                    { ResponseCode.UserNameChangeDuplicates, "The username already exists" },

                    { ResponseCode.LoginPasswordIncorrect, "Incorrect password" },
                    { ResponseCode.LoginCredentialInsufficient, "Login credentials insufficient" },
                    { ResponseCode.LoginRequired, "Login required" },
                    { ResponseCode.InvalidToken, "Invalid token" },

                    { ResponseCode.ActionNotExist, "Action does not exist" },
                    { ResponseCode.ActionExpired, "Action has expired" },
                    { ResponseCode.ActionTypeNotRecognized, "Action type not recognized" },
                    { ResponseCode.ActionDataInvalid, "Action data is invalid" },
                }
            },
            {
                "zh", new()
                {
                    { ResponseCode.Success, "成功" },

                    { ResponseCode.EmailNotRegistered, "邮箱不存在" },
                    { ResponseCode.EmailAlreadyRegistered, "该邮箱已被注册" },
                    { ResponseCode.EmailNotValid, "邮箱格式错误" },
                    { ResponseCode.EmailSendFailed, "邮件发送失败" },

                    { ResponseCode.UserNotFound, "找不到该用户" },
                    { ResponseCode.UserAlreadyExists, "该用户已存在" },
                    { ResponseCode.UserRegisterRequireName, "需要昵称才能注册账号" },
                    { ResponseCode.UserNameChangeRequireEmail, "需要邮箱才能修改昵称" },
                    { ResponseCode.UserNameChangeDuplicates, "用户名已存在" },

                    { ResponseCode.LoginPasswordIncorrect, "密码错误" },
                    { ResponseCode.LoginCredentialInsufficient, "登录凭据不足" },
                    { ResponseCode.LoginRequired, "需要登录后才能操作" },
                    { ResponseCode.InvalidToken, "登录状态失效，请重新登录" },

                    { ResponseCode.ActionNotExist, "操作不存在" },
                    { ResponseCode.ActionExpired, "操作已过期" },
                    { ResponseCode.ActionTypeNotRecognized, "操作类型无法识别" },
                    { ResponseCode.ActionDataInvalid, "操作数据无效" },
                }
            },
        };
    }
}