namespace EduAssess.Components.Shared;

public sealed record Article(string Slug, string Kicker, string Title, string Summary, string[] Body);

/// <summary>Conteúdos editoriais exibidos na home e em /conteudos.</summary>
public static class Articles
{
    public static readonly Article[] All =
    [
        new("notas-contam-uma-historia", "Avaliação",
            "Por que notas soltas não contam uma história",
            "Uma nota isolada diz pouco. Conectada a matrícula, disciplina e período, ela vira evidência.",
            [
                "Uma nota de 6,5 pode significar coisas muito diferentes: um aluno que começou em 3 e evoluiu, ou alguém que vinha de 9 e caiu. Sem contexto, a coordenação só enxerga o número final.",
                "Quando cada nota fica ligada à matrícula, à disciplina e a quem a lançou, o histórico passa a contar uma trajetória. É isso que permite conversar com o estudante antes de o semestre acabar.",
                "O primeiro passo é simples: um único lugar onde todas as avaliações são registradas, com regras claras sobre quem pode lançar o quê."
            ]),
        new("quem-pode-o-que", "Gestão acadêmica",
            "Hierarquia de acesso: quem pode o quê",
            "Permissões bem definidas protegem os dados e dão autonomia a cada função.",
            [
                "Em uma instituição, nem todo mundo precisa ver tudo. O administrador cuida da estrutura, o gestor organiza equipe e turmas, o professor avalia as próprias turmas e o aluno acompanha seu desempenho.",
                "Definir essa hierarquia no sistema — e não apenas em conversas — evita erros e vazamentos. Cada ação deve ser validada no servidor, e não apenas escondida na tela.",
                "Contas novas começam sem privilégios. Só depois de uma pessoa autorizada atribuir o perfil adequado é que o acesso acadêmico é liberado."
            ]),
        new("da-nota-a-decisao", "Dados",
            "Do lançamento da nota à decisão pedagógica",
            "Indicadores por curso mostram onde agir primeiro — se a informação chega a tempo.",
            [
                "Comparar a média de cada curso com a meta da instituição revela rapidamente onde o esforço de apoio deve começar. O segredo é que o dado esteja disponível enquanto ainda dá tempo de agir.",
                "Painéis atualizados no momento do lançamento substituem os relatórios de fim de período. A coordenação passa de reagir a antecipar.",
                "Comece pequeno: uma média por curso, as matrículas recentes e uma lista de contas pendentes já mudam a conversa nas reuniões."
            ])
    ];
}
