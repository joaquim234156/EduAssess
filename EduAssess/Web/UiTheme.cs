using EduAssess.Models.Global;
using EduAssess.Models.Local;

namespace EduAssess.Web;

/// <summary>
/// NOVO: unidade (prédio) da pessoa logada, usada para escolher o visual exclusivo de cada prédio.
/// </summary>
public sealed record UnitInfo(Guid Id, string Name, string Slug, string Sigla, TipoUnidade Type, Guid InstitutionId, string InstitutionName);

/// <summary>
/// NOVO: catálogo de temas. O visual do painel é a combinação de DOIS eixos:
///   • PERFIL  (aluno, professor, gestor, admin): muda o "jeito" da interface: barra lateral, cantos, destaques.
///   • UNIDADE (Prédio Principal, Medicina, Odontologia): muda a paleta de cores e o emblema.
/// O resultado vira classes CSS no elemento raiz (ex.: "role-aluno unit-medicina"); todo o estilo está em
/// wwwroot/css/temas.css. Unidades novas (cadastradas depois) ganham uma cor automática derivada do nome.
/// </summary>
public static class UiTheme
{
    public static string RoleKey(UserRole? role) => role switch
    {
        UserRole.Admin => "admin",
        UserRole.Gestor => "gestor",
        UserRole.Professor => "professor",
        UserRole.Aluno => "aluno",
        _ => "visitante"
    };

    /// <summary>Chave do tema do curso de Medicina (é um PERFIL por curso, e não um prédio).</summary>
    public const string MedicineKey = "medicina";

    /// <summary>
    /// Converte o slug da unidade (prédio) na chave do tema. Sem unidade = "rede" (visual da instituição inteira).
    /// ALTERADO: "Medicina" deixou de ser prédio. O prédio que tinha esse nome agora é o "Anexo"; o visual de
    /// Medicina passou a valer para alunos e professores do CURSO de Medicina (veja UiSession.CourseThemeKey).
    /// </summary>
    public static string UnitKey(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return "rede";
        var s = slug.ToLowerInvariant();
        if (s.Contains("anexo")) return "anexo";
        if (s.Contains("odonto")) return "odontologia";
        if (s.Contains("principal")) return "principal";
        return "custom";
    }

    /// <summary>Classes do elemento raiz: "role-professor unit-medicina".</summary>
    public static string ShellClass(UserRole? role, UnitInfo? unit) =>
        $"role-{RoleKey(role)} unit-{UnitKey(unit?.Slug)}";

    /// <summary>Unidades fora do catálogo ganham um matiz próprio (0–359) calculado a partir do nome.</summary>
    public static string? CustomStyle(UnitInfo? unit)
    {
        if (unit is null || UnitKey(unit.Slug) != "custom") return null;
        var hash = 0;
        foreach (var c in unit.Slug) hash = (hash * 31 + c) % 360;
        return $"--unit-h:{hash}";
    }

    /// <summary>Mesma regra de <see cref="CustomStyle(UnitInfo?)"/>, a partir só do slug (usado nos cartões de unidade).</summary>
    public static string? CustomStyle(string? slug)
    {
        if (UnitKey(slug) != "custom") return null;
        var hash = 0;
        foreach (var c in slug!) hash = (hash * 31 + c) % 360;
        return $"--unit-h:{hash}";
    }

    /// <summary>Ícone do prédio (a partir do slug). Mantido para as telas que só conhecem o slug.</summary>
    public static string UnitIcon(string? slug) => KeyIcon(UnitKey(slug));

    /// <summary>Ícone de cada tema: estetoscópio (curso de Medicina), sorriso (Odontologia), prédio...</summary>
    public static string KeyIcon(string key) => key switch
    {
        MedicineKey => "stethoscope",
        "odontologia" => "smile",
        "principal" => "building",
        "anexo" => "map-pin",
        "custom" => "layers",
        _ => "globe"
    };

    public static string RoleIcon(UserRole? role) => role switch
    {
        UserRole.Admin => "shield",
        UserRole.Gestor => "briefcase",
        UserRole.Professor => "pen",
        UserRole.Aluno => "cap",
        _ => "user-circle"
    };

    /// <summary>Texto curto de boas-vindas, no tom de cada perfil.</summary>
    public static string Greeting(UserRole? role, string firstName) => role switch
    {
        UserRole.Admin => $"Console da rede, {firstName}",
        UserRole.Gestor => $"Visão executiva, {firstName}",
        UserRole.Professor => $"Bom trabalho, prof. {firstName}",
        UserRole.Aluno => $"Bons estudos, {firstName}",
        _ => $"Olá, {firstName}"
    };

    public static string RoleTag(UserRole? role) => role switch
    {
        UserRole.Admin => "Console",
        UserRole.Gestor => "Gestão",
        UserRole.Professor => "Docência",
        UserRole.Aluno => "Campus",
        _ => "Acesso"
    };

    /// <summary>
    /// Acessos oferecidos na tela de login (cada um abre o login já com as cores daquele prédio ou curso).
    /// Depois de entrar, o tema passa a seguir a lotação REAL da pessoa, e não o escolhido aqui.
    /// </summary>
    public static readonly (string Key, string Name, string Icon)[] LoginUnits =
    [
        ("principal", "Prédio Principal", "building"),
        ("anexo", "Anexo", "map-pin"),
        ("odontologia", "Prédio Odontologia", "smile"),
        (MedicineKey, "Curso de Medicina", "stethoscope"),
    ];

    /// <summary>Aceita só chaves conhecidas (vindas da URL); qualquer outra coisa vira "principal".</summary>
    public static string NormalizeLoginUnit(string? key)
    {
        var k = (key ?? "").Trim().ToLowerInvariant();
        return LoginUnits.Any(u => u.Key == k) ? k : "principal";
    }

    /// <summary>O perfil usa barra lateral clara? (professor: sim; os demais: escura/colorida.)</summary>
    public static bool LightSidebar(UserRole? role) => role == UserRole.Professor;
}
