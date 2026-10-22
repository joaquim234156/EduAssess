using System.ComponentModel.DataAnnotations;
using System.Text;
using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Models.Local;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Web;

public sealed record ProfileData(string Name, string Email, string Phone, UserRole Role);

/// <summary>"Minha conta": dados pessoais e troca de senha do usuário logado.</summary>
public sealed class AccountProfileService(IDbContextFactory<AppDbContext> dbFactory, UiSession session)
{
    public async Task<ProfileData> GetAsync()
    {
        var me = await session.RequireAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = me.Id;
        var row = await db.Users.AsNoTracking().Where(u => u.Id == id)
            .Select(u => new ProfileData(u.Name, u.Email, u.Telefone, u.Role)).FirstAsync();
        return row;
    }

    public async Task UpdateAsync(string name, string email, string phone)
    {
        var me = await session.RequireAsync();
        name = (name ?? "").Trim();
        email = (email ?? "").Trim().ToLowerInvariant();
        phone = UiFormat.Digits(phone);

        if (name.Length < 3 || name.Length > 100)
            throw new ArgumentException("O nome deve ter entre 3 e 100 caracteres.");
        if (email.Length > 150 || !new EmailAddressAttribute().IsValid(email))
            throw new ArgumentException("Informe um e-mail válido.");
        if (phone.Length is < 10 or > 11)
            throw new ArgumentException("O telefone deve conter 10 ou 11 números.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var id = me.Id;
        if (await db.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == email))
            throw new AccountConflictException("Já existe uma conta com esse e-mail.");

        var user = await db.Users.FirstAsync(u => u.Id == id);
        user.Update(name, email);
        user.Telefone = phone;
        await db.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(string current, string newPassword, string confirm)
    {
        var me = await session.RequireAsync();
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8 || Encoding.UTF8.GetByteCount(newPassword) > 72)
            throw new ArgumentException("A nova senha deve ter pelo menos 8 caracteres e no máximo 72 bytes.");
        if (newPassword != confirm)
            throw new ArgumentException("A nova senha e a confirmação não coincidem.");

        await using var db = await dbFactory.CreateDbContextAsync();
        var id = me.Id;
        var user = await db.Users.FirstAsync(u => u.Id == id);
        if (!user.VerifyPassword(current ?? ""))
            throw new ArgumentException("A senha atual está incorreta.");

        user.UpdatePassword(newPassword, confirm);
        await db.SaveChangesAsync();
    }
}
