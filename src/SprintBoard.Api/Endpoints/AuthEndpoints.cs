// src/SprintBoard.Api/Endpoints/AuthEndpoints.cs
using Microsoft.AspNetCore.Identity;
using SprintBoard.Contracts;
using SprintBoard.Domain.Common;
using SprintBoard.Infrastructure;

namespace SprintBoard.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth");

        auth.MapPost("/register", async (RegisterRequest req, UserManager<AppUser> users, TokenService tokens) =>
        {
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = req.Email.Trim(),
                Email = req.Email.Trim(),
                DisplayName = (req.DisplayName ?? "").Trim()
            };

            var created = await users.CreateAsync(user, req.Password);
            return created.Succeeded
                ? Results.Ok(new AuthResponse(tokens.Create(user), user.Id, user.DisplayName))
                : Results.BadRequest(Errors.Validation(string.Join(" ", created.Errors.Select(e => e.Description))));
        });

        auth.MapPost("/login", async (LoginRequest req, UserManager<AppUser> users, TokenService tokens) =>
        {
            var user = await users.FindByEmailAsync(req.Email.Trim());
            if (user is null || !await users.CheckPasswordAsync(user, req.Password))
                return Results.Json(new Error("unauthorized", "Wrong email or password."),
                    statusCode: StatusCodes.Status401Unauthorized);

            return Results.Ok(new AuthResponse(tokens.Create(user), user.Id, user.DisplayName));
        });

        return app;
    }
}