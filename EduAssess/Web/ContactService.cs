using System.ComponentModel.DataAnnotations;
using EduAssess.Data;
using EduAssess.Models;
using EduAssess.Models.Global;
using Microsoft.EntityFrameworkCore;

namespace EduAssess.Web;

/// <summary>Recebe as solicitações do formulário público "Vamos conversar".</summary>
public sealed class ContactService(IDbContextFactory<AppDbContext> dbFactory)
{
    public static readonly string[] Roles =
        ["Reitoria / Direção", "Coordenação de curso", "Docente", "Estudante", "Gestão acadêmica", "Outro"];

    public async Task SubmitAsync(string name, string email, string phone, string institution, string role, string? message)
    {
        name = (name ?? "").Trim();
        email = (email ?? "").Trim();
        institution = (institution ?? "").Trim();
        phone = (phone ?? "").Trim();
        message = (message ?? "").Trim();

        if (name.Length < 3 || name.Length > 100)
            throw new ArgumentException("Informe seu nome completo.");
        if (email.Length > 150 || !new EmailAddressAttribute().IsValid(email))
            throw new ArgumentException("Informe um e-mail válido.");
        if (institution.Length < 2 || institution.Length > 150)
            throw new ArgumentException("Informe o nome da instituição.");
        if (!Roles.Contains(role))
            throw new ArgumentException("Selecione o seu papel.");
        if (phone.Length > 30 || message.Length > 1000)
            throw new ArgumentException("Telefone ou mensagem acima do tamanho permitido.");

        await using var db = await dbFactory.CreateDbContextAsync();
        // Proteção simples contra abuso: no máximo 3 solicitações por e-mail nas últimas 24 horas.
        var since = DateTime.UtcNow.AddHours(-24);
        var normalized = email.ToLowerInvariant();
        if (await db.Contatos.CountAsync(c => c.Email == normalized && c.CriadoEm > since) >= 3)
            throw new ArgumentException("Já recebemos suas solicitações. Aguarde nosso retorno.");

        db.Contatos.Add(new ContatoModel(name, email, phone, institution, role, message));
        await db.SaveChangesAsync();
    }
}
