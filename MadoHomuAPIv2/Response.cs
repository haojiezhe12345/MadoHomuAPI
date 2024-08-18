namespace MadoHomuAPIv2
{
    public class ResponseVO
    {
        public int? code { get; set; }
        public string? message { get; set; }
        public object? data { get; set; }
    }

    public enum ResponseCode
    {
        Success = 1,

        EmailNotRegistered = 1000,
        EmailAlreadyRegistered = 1001,
        EmailNotValid = 1002,
        EmailSendFailed = 1003,

        UserNotFound = 1010,
        UserAlreadyExists = 1011,
        UserRegisterRequireName = 1012,
        UserNameChangeRequireEmail = 1013,
        UserNameChangeDuplicates = 1014,

        LoginPasswordIncorrect = 1020,
        LoginCredentialInsufficient = 1021,

        ActionNotExist = 1030,
        ActionExpired = 1031,
        ActionTypeNotRecognized = 1032,
        ActionDataInvalid = 1033,
    }

    public static class ResponseDTOExtensions
    {
        public static void SetCode(this ResponseVO response, ResponseCode code)
        {
            response.code = (int)code;
            response.message = code.ToString();
        }
    }
}
