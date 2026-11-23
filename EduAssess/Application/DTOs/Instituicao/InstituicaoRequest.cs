namespace EduAssess.Application.DTOs.Instituicao
{
    /// <summary>
    /// Os campos depois de Email são NOVOS e opcionais (clientes antigos da API continuam funcionando):
    /// Tipo ("Faculdade" ou "Colegio"), Sigla, mantenedor (por Id ou por Nome, que cria se não existir) e cores #RRGGBB.
    /// </summary>
    public record InstituicaoRequest(
        string Nome,
        string Cnpj,
        string Endereco,
        string Telefone,
        string Email,
        string? Tipo = null,
        string? Sigla = null,
        Guid? MantenedorId = null,
        string? MantenedorNome = null,
        string? CorPrimaria = null,
        string? CorSecundaria = null
    );
}
