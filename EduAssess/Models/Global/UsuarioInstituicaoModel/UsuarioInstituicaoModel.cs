using EduAssess.Models.Local;

namespace EduAssess.Models.Global
{
    public enum PapelInstituicao { Aluno = 1, Professor = 2, Gestor = 3 }
    public class UsuarioInstituicaoModel
    {
        protected UsuarioInstituicaoModel() { }

        public UsuarioInstituicaoModel(Guid usuarioId, Guid instituicaoId, PapelInstituicao papel, bool ativa, DateTime vinculadoEm)
        {
            UsuarioId = usuarioId;
            InstituicaoId = instituicaoId;
            Papel = papel;
            Ativa = ativa;
            VinculadoEm = DateTime.UtcNow;
        }

        public Guid UsuarioId { get; private set; }
        public Guid InstituicaoId { get; set; }
        public PapelInstituicao Papel { get; set; }
        public bool Ativa { get; set; }
        public DateTime VinculadoEm { get; set; }
        public UserModel? Usuario { get; set; }
        public InstituicaoModel? Instituicao { get; set; }
    }
}
