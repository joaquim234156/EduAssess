using EduAssess.Data;
using EduAssess.EduAssess.Aplication.DTOs.User;
using EduAssess.EduAssess.Aplication.Services;
using EduAssess.Models;
using EduAssess.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduAssess.Application.Routes
{
    public static class AuthRoute
    {
        public static void AuthRoutes(this WebApplication app)
        {
            var route = app.MapGroup("auth").WithTags("Autenticação");

            route.MapPost("register",
                async (RegisterRequest req, AccountRegistrationService accounts, CancellationToken cancellationToken) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    try
                    {
                        var user = await accounts.RegisterUserAsync(
                            req.name, req.email, req.cpf, req.tel, req.dataNascimento,
                            req.password, req.confirmPassword, cancellationToken);
                        return Results.Created($"/auth/users/{user.Id}", new
                        {
                            message = "Conta criada. Um usuário autorizado poderá atribuir seu perfil acadêmico.",
                            user = new { user.Id, user.Email, Role = user.Role.ToString() }
                        });
                    }
                    catch (AccountConflictException ex)
                    {
                        return Results.Conflict(new { message = ex.Message });
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new { message = ex.Message });
                    }
                })
                .WithSummary("Cria uma conta sem privilégios")
                .Produces(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status409Conflict)
                .RequireRateLimiting("public-auth");

            route.MapPost("users/managers",
                async (RegisterRequest req, AccountRegistrationService accounts, CancellationToken cancellationToken) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    try
                    {
                        var user = await accounts.CreateManagerAsync(
                            UserRole.Admin, req.name, req.email, req.cpf, req.tel, req.dataNascimento,
                            req.password, req.confirmPassword, cancellationToken);
                        return Results.Created($"/auth/users/{user.Id}", new
                        {
                            message = "Gestor criado com sucesso.",
                            user = new { user.Id, user.Email, Role = user.Role.ToString() }
                        });
                    }
                    catch (AccountConflictException ex)
                    {
                        return Results.Conflict(new { message = ex.Message });
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new { message = ex.Message });
                    }
                })
                .RequireAuthorization("Admin")
                .WithSummary("Cria uma conta Gestor (somente Admin)")
                .Produces(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden);

            route.MapPost("users/professors",
                async (RegisterRequest req, AccountRegistrationService accounts, CancellationToken cancellationToken) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    try
                    {
                        var user = await accounts.CreateProfessorAsync(
                            UserRole.Gestor, req.name, req.email, req.cpf, req.tel, req.dataNascimento,
                            req.password, req.confirmPassword, cancellationToken);
                        return Results.Created($"/auth/users/{user.Id}", new
                        {
                            message = "Professor criado com sucesso.",
                            user = new { user.Id, user.Email, Role = user.Role.ToString() }
                        });
                    }
                    catch (AccountConflictException ex)
                    {
                        return Results.Conflict(new { message = ex.Message });
                    }
                    catch (ArgumentException ex)
                    {
                        return Results.BadRequest(new { message = ex.Message });
                    }
                })
                .RequireAuthorization("Gestor")
                .WithSummary("Cria uma conta Professor (somente Gestor)")
                .Produces(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden);

            route.MapGet("users/pending-students",
                async (AppDbContext context, CancellationToken cancellationToken) =>
                    await context.Users.AsNoTracking()
                        .Where(user => user.Ativa && user.Role == UserRole.User)
                        .OrderBy(user => user.Name)
                        .Select(user => new { user.Id, user.Name, user.Email })
                        .ToListAsync(cancellationToken))
                .RequireAuthorization("AtribuirAluno")
                .WithSummary("Lista contas sem perfil acadêmico para atribuição como aluno");

            route.MapPost("users/{id:guid}/assign-student",
                async (Guid id, AccountRegistrationService accounts, ClaimsPrincipal principal, CancellationToken cancellationToken) =>
                {
                    var actorRole = principal.IsInRole("Admin") ? UserRole.Admin
                        : principal.IsInRole("Gestor") ? UserRole.Gestor
                        : UserRole.Professor;
                    try
                    {
                        await accounts.AssignAsStudentAsync(actorRole, id, cancellationToken);
                        return Results.NoContent();
                    }
                    catch (KeyNotFoundException ex)
                    {
                        return Results.NotFound(new { message = ex.Message });
                    }
                    catch (InvalidOperationException ex)
                    {
                        return Results.Conflict(new { message = ex.Message });
                    }
                })
                .RequireAuthorization("AtribuirAluno")
                .WithSummary("Atribui o perfil Aluno a uma conta existente")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status409Conflict);

            route.MapPost("login",
                async Task<IResult> (LoginRequest req, AppDbContext context, IConfiguration config, CancellationToken cancellationToken) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    var normalizedEmail = req.email.Trim().ToLowerInvariant();
                    var user = await context.Users.FirstOrDefaultAsync(
                        u => u.Email.ToLower() == normalizedEmail && u.Ativa,
                        cancellationToken);
                    if (user == null || !user.VerifyPassword(req.password))
                        return Results.Unauthorized();

                    var secret = config["JwtSettings:Secret"]!;
                    var token = TokenService.GenerateToken(user, secret);

                    return Results.Ok(new
                    {
                        message = "Login successful",
                        user = new { user.Id, user.Email, user.Role },
                        token
                    });
                })
                .RequireRateLimiting("public-auth");

            route.MapGet("users",
                async (AppDbContext context) =>
                {
                    var users = await context.Users.Where(u => u.Ativa == true).Select(u => new UserResponseDTO(u.Id, u.Name, u.Email, u.Cpf, u.Telefone, u.DataNascimento, u.Ativa)).ToListAsync();
                    return Results.Ok(users);
                }
            ).RequireAuthorization("Admin");

            route.MapGet("users/{id:guid}",
                async (Guid id, AppDbContext context, ClaimsPrincipal principal, CancellationToken cancellationToken) =>
                {
                    var currentUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!principal.IsInRole("Admin") && currentUserId != id.ToString())
                        return Results.Forbid();

                    var user = await context.Users.FindAsync([id], cancellationToken);
                    if (user == null || !user.Ativa)
                        return Results.NotFound(new { message = "User not found" });
                    var userNow = new UserResponseDTO(user.Id, user.Name, user.Email, user.Cpf, user.Telefone, user.DataNascimento, user.Ativa);
                    return Results.Ok(userNow);
                }
            ).RequireAuthorization();

            route.MapPut("users/{id:guid}/update",
                async (Guid id, UserRequestPut req, AppDbContext context, ClaimsPrincipal userLogado, CancellationToken cancellationToken) =>
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

                    var normalizedEmail = req.email.Trim().ToLowerInvariant();
                    if (await context.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == normalizedEmail, cancellationToken))
                        return Results.BadRequest(new { message = "E-mail já cadastrado." });

                    user.Update(req.name, normalizedEmail);

                    await context.SaveChangesAsync(cancellationToken);
                    return Results.Ok(new
                    {
                        message = "User updated successfully",
                        user = new { user.Id, user.Email, user.Role }
                    });
                }
            ).RequireAuthorization();

            route.MapPut("users/{id:guid}/updateAdmin",
                async (Guid id, UserRequestPutDetails req, AppDbContext context, CancellationToken cancellationToken) =>
                {
                    if (!MiniValidation.MiniValidator.TryValidate(req, out var errors))
                        return Results.BadRequest(errors);

                    var user = await context.Users.FindAsync([id], cancellationToken);
                    if (user == null || !user.Ativa)
                        return Results.NotFound(new { message = "User not found" });

                    if (req.role != user.Role)
                        return Results.BadRequest(new { message = "O perfil não pode ser alterado por esta rota." });

                    var normalizedEmail = req.email.Trim().ToLowerInvariant();
                    if (await context.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == normalizedEmail, cancellationToken))
                        return Results.BadRequest(new { message = "E-mail já cadastrado." });

                    user.Update(req.name, normalizedEmail);

                    await context.SaveChangesAsync(cancellationToken);
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
            ).RequireAuthorization("Admin");
        }
    }
}
