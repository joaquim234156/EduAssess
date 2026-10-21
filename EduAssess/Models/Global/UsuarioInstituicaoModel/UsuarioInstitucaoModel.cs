namespace EduAssess.Models.Global
{
    public class UsuarioInstitucaoModel
    {
        protected UsuarioInstitucaoModel() { }

        public UsuarioInstitucaoModel(Guid idUsuario, Guid instituicaoId, bool ativa, DateTime vinculadoEm)
        {
            IdUsuario = idUsuario;
            InstituicaoId = instituicaoId;
            Ativa = ativa;
            VinculadoEm = vinculadoEm;
        }

        public Guid IdUsuario { get; private set; }
        public Guid InstituicaoId { get; set; }
        public bool Ativa { get; set; }
        public DateTime VinculadoEm { get; set; }
    }
}
