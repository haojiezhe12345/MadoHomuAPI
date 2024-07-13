namespace MadoHomuAPIv2
{
    public class ResponseDTO
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
        EmailChangeNotLoggedOn = 1003,
        UserNotFound = 1010,
        UserAlreadyExists = 1011,
    }

    public static class ResponseDTOExtensions
    {
        public static void SetCode(this ResponseDTO response, ResponseCode code)
        {
            response.code = (int)code;
            response.message = code.ToString();
        }
    }
}
