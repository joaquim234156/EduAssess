using EduAssess.Data;
using EduAssess.Models.UserModel;
using EduAssess.Services;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Routes.UserRoutes
{
    public static class AuthRoute
    {
        public static void AuthRoutes(this WebApplication app)
        {
            var route = app.MapGroup("auth");

            route.MapPost("register",
                async(UserRequest req, AppDbContext context, IConfiguration config) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == req.email);
                    if (existingUser != null)
                        return Results.BadRequest(new { message = "User already exists" });

                    bool isFirstUser = !await context.Users.AnyAsync();

                    UserRole assignedRole = isFirstUser ? UserRole.Admin : req.role;

                    // Gerando o token para o novo usuário registrado

                    var user = new UserModel(req.email, req.password, assignedRole);
                    await context.Users.AddAsync(user);
                    await context.SaveChangesAsync();

                    var secret = config.GetValue<string>("JwtSettings:Secret");
                    var token = TokenService.GenerateToken(user, secret!);

                    return Results.Ok(new 
                    {
                        message = isFirstUser
                        ? "Primeiro usuário cadastrado com sucesso como Administrador!"
                        : "Usuário cadastrado com sucesso!",
                        user = new { user.Id, user.Email, Role = user.Role.ToString() },
                        token
                    });
                }
            );

            route.MapPost("login",
                async (LoginRequest req, AppDbContext context, IConfiguration config) =>
                {
                    var user = await context.Users.FirstOrDefaultAsync(u => u.Email == req.email && u.Ativa == true);
                    if (user == null || !user.VerifyPassword(req.password))
                        return Results.Unauthorized();

                    bool isValidPassword = user.VerifyPassword(req.password);
                    if (!isValidPassword)
                        return Results.Unauthorized();

                    var secret = config.GetValue<string>("JwtSettings:Secret");
                    var token = TokenService.GenerateToken(user, secret!);

                    return Results.Ok(new 
                    {
                        message = "Login successful",
                        user = new { user.Id, user.Email, user.Role },
                        token
                    });
                });

            route.MapGet("users",
                async (AppDbContext context) =>
                {
                    var users = await context.Users.Where(u => u.Ativa == true).Select(u => new UserResponseDTO(u.Id, u.Email, u.Ativa)).ToListAsync();
                    return Results.Ok(users);
                }
            ).RequireAuthorization("AdminOnly");

            route.MapGet("users/{id:guid}",
                async (Guid id, AppDbContext context) =>
                {
                    var user = await context.Users.FindAsync(id);
                    if (user == null)
                        return Results.NotFound(new { message = "User not found" });
                    if (!user.Ativa)
                        return Results.NotFound(new { message = "User not found" });
                    var userNow = new UserResponseDTO(user.Id, user.Email, user.Ativa);
                    return Results.Ok(userNow);
                }
            ).RequireAuthorization();

            route.MapPut("users/{id:guid}",
                async (Guid id, UserRequestPut req, AppDbContext context) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    var user = await context.Users.FindAsync(id);
                    if (user == null || !user.Ativa)
                        return Results.NotFound(new { message = "User not found" });

                    var emailExists = await context.Users.AnyAsync(u => u.Email == req.email && u.Id != id);
                    if (emailExists)
                        return Results.BadRequest(new { message = "Email is already in use by another account." });

                    user.Update(req.email, req.role, req.password);

                    await context.SaveChangesAsync();
                    return Results.Ok(new
                    {
                        message = "User updated successfully",
                        user = new { user.Id, user.Email, user.Role }
                    });
                }
            ).RequireAuthorization("AdminOnly");

            route.MapDelete("users/{id:guid}",
                async (Guid id, AppDbContext context) =>
                {
                    var user = await context.Users.FindAsync(id);

                    if (user == null)
                        return Results.NotFound(new { message = "User not found" });
                    if (!user.Ativa)
                        return Results.NotFound(new { message = "User not found" });

                    user.Disable();

                    await context.SaveChangesAsync();
                    return Results.Ok(new { message = "User deleted successfully" });
                }
            );
        }
    }
}
