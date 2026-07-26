using FlightStatus.Api.Domain.Interfaces;
using FlightStatus.Api.Endpoints;
using FlightStatus.Api.Infrastructure.Providers;
using FlightStatus.Api.Infrastructure.Services;
using FlightStatus.Api.Middleware;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IFlightStatusProvider, AeroTrackProvider>();
builder.Services.AddSingleton<IFlightStatusProvider, QuickFlightProvider>();

builder.Services.AddScoped<IFlightMergeService, FlightMergeService>();

builder.Services.AddCors(corsOptions =>
{
    corsOptions.AddDefaultPolicy(corsPolicy =>
    {
        corsPolicy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.ConfigureHttpJsonOptions(httpJsonOptions =>
{
    httpJsonOptions.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    httpJsonOptions.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddControllers()
    .AddJsonOptions(jsonOptions =>
    {
        jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(swaggerUiOptions =>
{
    swaggerUiOptions.SwaggerEndpoint("/swagger/v1/swagger.json", "Flight Status API v1");
    swaggerUiOptions.RoutePrefix = string.Empty;
});

app.MapFlightEndpoints();
app.Run();

public partial class Program { }
