// src/SprintBoard.Api/Program.cs  (replace the template)
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SprintBoard.Api;
using SprintBoard.Api.Endpoints;
using SprintBoard.Api.Hubs;
using SprintBoard.Application;
using SprintBoard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
          ?? throw new InvalidOperationException("Missing Jwt configuration.");
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration.GetConnectionString("Default") ?? "Data Source=sprintboard.db");

builder.Services.AddIdentityCore<AppUser>(o =>
{
    o.Password.RequiredLength = 8;
    o.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddSingleton<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = "name"
        };

        // Browsers cannot set headers on WebSockets, so SignalR sends the token in the query string.
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    context.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddProblemDetails();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration["ClientOrigin"] ?? "https://localhost:7100")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

app.UseExceptionHandler();   // unexpected exceptions become a clean ProblemDetails response
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapBoardEndpoints();
app.MapHub<BoardHub>("/hubs/board");

app.Run();

public partial class Program { }   // lets integration tests start the app