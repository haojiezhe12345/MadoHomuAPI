namespace MadoHomuAPIv2.Response
{
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
        LoginRequired = 1022,
        InvalidToken = 1023,

        ActionNotExist = 1030,
        ActionExpired = 1031,
        ActionTypeNotRecognized = 1032,
        ActionDataInvalid = 1033,
    }
}
