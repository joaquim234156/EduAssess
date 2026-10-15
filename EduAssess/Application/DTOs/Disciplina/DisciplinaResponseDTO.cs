namespace EduAssess.Application.DTOs.Disciplina
{
    public record DisciplinaResponseDTO(
        Guid IdDisciplina,
        string NomeDisciplina,
        string DescricaoDisciplina
        );
}
