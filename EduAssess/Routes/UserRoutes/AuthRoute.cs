using EduAssess.Data;
using EduAssess.Models.UserModel;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Routes.UserRoutes
{
    public static class AuthRoute
    {
        public static void AuthRoutes(this WebApplication app)
        {
            var route = app.MapGroup("auth");

            route.MapPost("register",
                async(UserRequest req, AppDbContext context) =>
                {
                    var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == req.email);
                    if (existingUser != null)
                        return Results.BadRequest(new { message = "User already exists" });
                    
                    var user = new UserModel(req.email, req.password);
                    await context.Users.AddAsync(user);
                    await context.SaveChangesAsync();

                    return Results.Ok(new { message = "Usuário cadastrado com sucesso!", user.Id, user.Email });
                }
            );

            route.MapPost("login",
                async (UserRequest req, AppDbContext context) =>
                {
                    var user = await context.Users.FirstOrDefaultAsync(u => u.Email == req.email && u.Password == req.password);
                    if (user == null)
                        return Results.Unauthorized();

                    return Results.Ok(new { message = "Login successful", user.Id, user.Email });
                }
            );

            route.MapGet("users",
                async (AppDbContext context) =>
                {
                    var users = await context.Users.Where(u => u.Ativa == true).Select(u => new UserResponseDTO(u.Id, u.Email, u.Ativa)).ToListAsync();
                    return Results.Ok(users);
                }
            );

            route.MapGet("users/{id:guid}",
                async (Guid id, AppDbContext context) =>
                {
                    var user = await context.Users.FindAsync(id);
                    if (user == null)
                        return Results.NotFound(new { message = "User not found" });
                    if (!user.Ativa)
                        return Results.NotFound(new { message = "User not found" });

                    return Results.Ok(user);
                }
            );

            route.MapPut("users/{id:guid}",
                async (Guid id, UserRequest req, AppDbContext context) =>
                {
                    var user = await context.Users.FindAsync(id);
                    if (user == null)
                        return Results.NotFound(new { message = "User not found" });

                    user.Update(req.email, req.password);

                    await context.SaveChangesAsync();
                    return Results.Ok(new { message = "User updated successfully", user.Id, user.Email });
                }
            );

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
