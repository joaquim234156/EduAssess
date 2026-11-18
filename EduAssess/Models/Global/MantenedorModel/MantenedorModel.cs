namespace EduAssess.Models.Global;

public class MantenedorModel
{
    protected MantenedorModel() { }

    public MantenedorModel(string nome)
    {
        Id = Guid.NewGuid();
        Nome = nome.Trim();
        CriadoEm = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }
    public ICollection<InstituicaoModel> Instituicoes { get; private set; } = new List<InstituicaoModel>();
}
