using MadoHomuAPIv2;
using MadoHomuAPIv2.Response;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Authorization", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new() { Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Authorization" } },
        Array.Empty<string>()
    }});

    options.OperationFilter<AddAcceptLanguageParameter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ErrorLoggingMiddleware>();
//app.UseMiddleware<PerformanceMeasureMiddleware>();
app.UseMiddleware<DatabaseMiddleware>();
app.UseMiddleware<UserAuthMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
