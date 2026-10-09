using EduAssess.Data;
using EduAssess.EduAssess.Aplication.DTOs.User;
using EduAssess.EduAssess.Aplication.Services;
using EduAssess.Models.UserModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace EduAssess.EduAssess.Aplication.Routes.UserRoutes
{
    public static class AuthRoute
    {
        public static void AuthRoutes(this WebApplication app)
        {
            var route = app.MapGroup("auth").WithTags("Autenticação"); ;

            route.MapPost("register",
                async(RegisterRequest req, AppDbContext context, IConfiguration config) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    if (req.password != req.confirmPassword)
                        return Results.BadRequest(new { message = "A senha e a confirmação de senha não coincidem." });

                    if (await context.Users.AnyAsync(u => u.Email == req.email))
                        return Results.BadRequest(new { message = "E-mail já cadastrado." });

                    bool isFirstUser = !await context.Users.AnyAsync();
                    UserRole assignedRole = isFirstUser ? UserRole.Admin : req.role;

                    // Gerando o token para o novo usuário registrado

                    var user = new UserModel(req.name, req.email, req.cpf, req.tel, req.dataNascimento, req.password, assignedRole);

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
                    var users = await context.Users.Where(u => u.Ativa == true).Select(u => new UserResponseDTO(u.Id, u.Name, u.Email, u.Cpf, u.Telefone, u.DataNascimento, u.Ativa)).ToListAsync();
                    return Results.Ok(users);
                }
            ).RequireAuthorization("Admin");

            route.MapGet("users/{id:guid}",
                async (Guid id, AppDbContext context) =>
                {
                    var user = await context.Users.FindAsync(id);
                    if (user == null)
                        return Results.NotFound(new { message = "User not found" });
                    if (!user.Ativa)
                        return Results.NotFound(new { message = "User not found" });
                    var userNow = new UserResponseDTO(user.Id, user.Name, user.Email, user.Cpf, user.Telefone, user.DataNascimento, user.Ativa);
                    return Results.Ok(userNow);
                }
            ).RequireAuthorization();

            route.MapPut("users/{id:guid}/update",
                async (Guid id, UserRequestPut req, AppDbContext context, ClaimsPrincipal userLogado) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    var currentUserId = userLogado.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var isAdmin = userLogado.IsInRole("Admin");

                    if (!isAdmin && currentUserId != id.ToString())
                    {
                        return Results.Forbid();
                    }

                    var user = await context.Users.FindAsync(id);
                    if (user == null || !user.Ativa)
                        return Results.NotFound(new { message = "User not found" });

                    if (await context.Users.AnyAsync(u => u.Email == req.email))
                        return Results.BadRequest(new { message = "E-mail já cadastrado." });

                    user.Update(req.email, req.name);

                    await context.SaveChangesAsync();
                    return Results.Ok(new
                    {
                        message = "User updated successfully",
                        user = new { user.Id, user.Email, user.Role }
                    });
                }
            ).RequireAuthorization();

            route.MapPut("users/{id:guid}/updateAdmin",
                async (Guid id, UserRequestPutDetails req, AppDbContext context) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    var user = await context.Users.FindAsync(id);
                    if (user == null || !user.Ativa)
                        return Results.NotFound(new { message = "User not found" });

                    if (await context.Users.AnyAsync(u => u.Email == req.email))
                        return Results.BadRequest(new { message = "E-mail já cadastrado." });

                    user.UpdateAdmin(req.name, req.email, req.role);

                    await context.SaveChangesAsync();
                    return Results.Ok(new
                    {
                        message = "User updated successfully",
                        user = new { user.Id, user.Email, user.Role }
                    });
                }
            ).RequireAuthorization("Admin");

            route.MapPut("users/{id:guid}/UpdatePassword",
                async (Guid id, PasswordRequest req, AppDbContext context, ClaimsPrincipal userLogado) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    var currentUserId = userLogado.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (currentUserId != id.ToString())
                    {
                        return Results.Forbid();
                    }

                    var user = await context.Users.FindAsync(id);
                    if (user == null || !user.Ativa)
                        return Results.NotFound(new { message = "User not found" });

                    user.UpdatePassword(req.password, req.confirmPassword);

                    await context.SaveChangesAsync();
                    return Results.Ok(new
                    {
                        message = "User updated successfully",
                        user = new { user.Id, user.Email, user.Role }
                    });
                }
            ).RequireAuthorization();

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
