using EduAssess.Models.Local;

namespace EduAssess.Models.Global
{
    public enum PapelInstituicao { Aluno = 1, Professor = 2, Gestor = 3 }

    /// <summary>
    /// Vínculo de uma pessoa com a instituição (Faculdade Luciano Feijão).
    /// ALTERADO: ganhou <see cref="UnidadeId"/>, a unidade (prédio) em que a pessoa está lotada:
    /// Prédio Principal, Anexo ou Prédio Odontologia. É por ela que o painel escolhe o
    /// visual exclusivo de cada prédio (o visual de Medicina vem do CURSO, veja UiSession). Opcional: sem unidade = visual da rede.
    /// </summary>
    public class UsuarioInstituicaoModel
    {
        protected UsuarioInstituicaoModel() { }

        public UsuarioInstituicaoModel(Guid usuarioId, Guid instituicaoId, PapelInstituicao papel, Guid? unidadeId = null)
        {
            UsuarioId = usuarioId;
            InstituicaoId = instituicaoId;
            Papel = papel;
            UnidadeId = unidadeId;
            Ativa = true;
            VinculadoEm = DateTime.UtcNow;
            DesvinculoEm = null;
        }

        public Guid UsuarioId { get; private set; }
        public Guid InstituicaoId { get; set; }
        public PapelInstituicao Papel { get; set; }

        /// <summary>NOVO: unidade (prédio) da pessoa. Nula = sem unidade definida.</summary>
        public Guid? UnidadeId { get; set; }

        public bool Ativa { get; set; }
        public DateTime VinculadoEm { get; set; }
        public DateTime? DesvinculoEm { get; set; }
        public UserModel? Usuario { get; set; }
        public InstituicaoModel? Instituicao { get; set; }
        public UnidadeModel? Unidade { get; set; }

        public void Update(PapelInstituicao papel)
        {
            Papel = papel;
        }

        /// <summary>NOVO: atualiza papel e unidade de uma vez (usado pela tela "Instituição").</summary>
        public void Update(PapelInstituicao papel, Guid? unidadeId)
        {
            Papel = papel;
            UnidadeId = unidadeId;
        }

        public void Desvincular()
        {
            Ativa = false;
            DesvinculoEm = DateTime.UtcNow;
        }

        /// <summary>NOVO: reativa um vínculo que havia sido encerrado (evita duplicar a chave composta).</summary>
        public void Reativar(PapelInstituicao papel, Guid? unidadeId)
        {
            Papel = papel;
            UnidadeId = unidadeId;
            Ativa = true;
            VinculadoEm = DateTime.UtcNow;
            DesvinculoEm = null;
        }
    }
}
