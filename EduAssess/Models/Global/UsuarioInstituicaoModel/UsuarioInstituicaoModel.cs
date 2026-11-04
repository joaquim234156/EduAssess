using EduAssess.Models.Local;

namespace EduAssess.Models.Global
{
    public enum PapelInstituicao { Aluno = 1, Professor = 2, Gestor = 3 }
    public class UsuarioInstituicaoModel
    {
        protected UsuarioInstituicaoModel() { }

        public UsuarioInstituicaoModel(Guid usuarioId, Guid instituicaoId, PapelInstituicao papel)
        {
            UsuarioId = usuarioId;
            InstituicaoId = instituicaoId;
            Papel = papel;
            Ativa = true;
            VinculadoEm = DateTime.UtcNow;
            DesvinculoEm = null;
        }

        public Guid UsuarioId { get; private set; }
        public Guid InstituicaoId { get; set; }
        public PapelInstituicao Papel { get; set; }
        public bool Ativa { get; set; }
        public DateTime VinculadoEm { get; set; }
        public DateTime? DesvinculoEm { get; set; }
        public UserModel? Usuario { get; set; }
        public InstituicaoModel? Instituicao { get; set; }

        public void Update(PapelInstituicao papel)
        {
            Papel = papel;
        }

        public void Desvincular()
        {
            Ativa = false;
            DesvinculoEm = DateTime.UtcNow;
        }
    }
}
