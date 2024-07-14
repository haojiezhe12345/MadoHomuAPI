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

        UserNotFound = 1010,
        UserAlreadyExists = 1011,
        UserRegisterRequireName = 1012,
        UserNameChangeRequireEmail = 1013,

        LoginRequired = 1020,
        LoginTokenInvalid = 1021,
        LoginCredentialInsufficient = 1022,
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
