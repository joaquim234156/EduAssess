using System.Globalization;
using EduAssess.Models;
using EduAssess.Models.Local;

namespace EduAssess.Web;

/// <summary>Formatação compartilhada pelas páginas (sempre em pt-BR).</summary>
public static class UiFormat
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public static string Initials(string? name)
    {
        var parts = (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "EA";
        return string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
    }

    public static string FirstName(string? name) =>
        (name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

    public static string RoleLabel(UserRole role) => role switch
    {
        UserRole.Admin => "Administrador",
        UserRole.Gestor => "Gestor",
        UserRole.Professor => "Professor",
        UserRole.Aluno => "Aluno",
        _ => "Conta sem perfil"
    };

    /// <summary>Página inicial de cada perfil depois do login.</summary>
    public static string HomePath(UserRole role) => role switch
    {
        UserRole.Admin or UserRole.Gestor => "/painel",
        UserRole.Professor => "/professor",
        UserRole.Aluno => "/aluno",
        _ => "/aguardando"
    };

    public static string Grade(decimal value) => value.ToString("0.0#", PtBr);

    public static string Grade(decimal? value) => value.HasValue ? Grade(value.Value) : "—";

    public static string Percent(decimal? grade) =>
        grade.HasValue ? (grade.Value * 10).ToString("0", PtBr) + "%" : "—";

    /// <summary>Faixa de desempenho usada para colorir notas: alta (7+), média (5 a 6,9) e baixa (&lt; 5).</summary>
    public static string GradeBand(decimal? value) => value switch
    {
        null => "none",
        >= 7m => "high",
        >= 5m => "mid",
        _ => "low"
    };

    public static string Date(DateTime utc) => utc.ToLocalTime().ToString("dd/MM/yyyy", PtBr);

    public static string DateTimeShort(DateTime utc) => utc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", PtBr);

    public static string LongDate(DateTime now)
    {
        var text = now.ToString("dddd, d 'de' MMMM 'de' yyyy", PtBr);
        return char.ToUpper(text[0], PtBr) + text[1..];
    }

    public static string Digits(string? value) => new((value ?? "").Where(char.IsDigit).ToArray());
}
