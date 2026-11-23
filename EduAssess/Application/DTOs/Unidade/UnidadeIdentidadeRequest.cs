namespace EduAssess.Application.DTOs.Unidade;

/// <summary>
/// Nulo em cada campo significa herdar o valor da instituição. Para cores próprias, envie #RRGGBB.
/// </summary>
public record UnidadeIdentidadeRequest(string? CorPrimaria, string? CorSecundaria);
