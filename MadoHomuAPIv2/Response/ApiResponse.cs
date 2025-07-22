using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MadoHomuAPIv2.Response
{
    public class ApiResponse
    {
        public required int code { get; set; }
        public required string message { get; set; }

        public static implicit operator ApiResponse(ApiResponse<object> response) => new()
        {
            code = response.code,
            message = response.message,
        };
    }

    public class ApiResponse<T>
    {
        public required int code { get; set; }
        public required string message { get; set; }
        public required T? data { get; set; }

        public static implicit operator ApiResponse<T>(ApiResponse response) => new()
        {
            code = response.code,
            message = response.message,
            data = default,
        };
    }

    public static class ControllerBaseExtensions
    {
        public static ApiResponse ApiResponse(this ControllerBase controllerBase, ResponseCode code) => new()
        {
            code = (int)code,
            message = ResponseMessages.Get(code, controllerBase.HttpContext.Request.Headers.AcceptLanguage),
        };

        public static ApiResponse<T> ApiResponse<T>(this ControllerBase controllerBase, ResponseCode code, T data) => new()
        {
            code = (int)code,
            message = ResponseMessages.Get(code, controllerBase.HttpContext.Request.Headers.AcceptLanguage),
            data = data,
        };
    }

    public class AddAcceptLanguageParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters ??= [];

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Accept-Language",
                In = ParameterLocation.Header,
                Required = false
            });
        }
    }
}
