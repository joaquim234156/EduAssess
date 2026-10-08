using System;

namespace EduAssess.Models.CursoModel
{
    public class CursoModel
    {
        public CursoModel(string nome, string descricao, string professor, int duracaoEmAnos)
        {
            Id = Guid.NewGuid();
            Nome = nome;
            Descricao = descricao;
            Professor = professor;
            DuracaoEmAnos = duracaoEmAnos;
            Ativo = true;
        }

        public Guid Id { get; init; }
        public string Nome { get; private set; }
        public string Descricao { get; private set; }
        public string Professor { get; private set; }
        public int DuracaoEmAnos { get; private set; }
        public bool Ativo { get; private set; }

        public void Update(string nome, string descricao, string professor, int duracaoEmAnos)
        {
            Nome = nome;
            Descricao = descricao;
            Professor = professor;
            DuracaoEmAnos = duracaoEmAnos;
        }
        public void Disable()
        {
            Ativo = false;
        }
    }
}