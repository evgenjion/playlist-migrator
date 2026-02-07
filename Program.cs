using System.Text.Json;
using Autofac;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Migrator.Controllers;
using Migrator.Core;
using Migrator.HTTPClient;
using Migrator.Repositories;
using Migrator.Services;

// This will automatically look for a .env file in the current directory or higher ancestor directories
Env.Load();
Console.WriteLine("Requesting spotify!");

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddEndpointsApiExplorer();

// new instance every time
builder.Services.AddTransient<MyInterceptHttpClient>();
builder.Services.AddTransient<AuthRepository>();
builder.Services.AddTransient<PlaylistsRepository>();
builder.Services.AddTransient<TrackRepository>();
builder.Services.AddTransient<MigrateService>();

// one intance
builder.Services.AddSingleton<Config>();

var app = builder.Build();
app.UseExceptionHandler("/api");

var apiV1 = app.MapGroup("/api/v1");
apiV1.MapGet("/ping", GetPong);

var oAuthGroup = apiV1.MapGroup("/oauth");
oAuthGroup.MapGet("/login", OAuthControllers.Login);
oAuthGroup.MapGet("/callback", OAuthControllers.Callback);

app.Run();

static async Task<IResult> GetPong()
{
    return TypedResults.Ok("Pong");
}
