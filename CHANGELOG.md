# Changelog

Todas as mudanças relevantes do projeto. Formato inspirado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/).
Os mesmos comentários estão no código, junto de cada alteração (procure por `NOVO`, `ALTERADO` e `CORREÇÃO`).

## [Não publicado] — Identidade das instituições, CLF, selos de questões, simulados do aluno e auditoria

### Adicionado
- **Instituições:** categoria (`Faculdade` ou `Colegio`), sigla, logotipo (upload com validação por conteúdo, WebP até 512 px, transparência mantida), cor primária e secundária (`#RRGGBB`) e mantenedor editáveis na tela `/instituicao` e na API (`PUT /instituicoes/{id}/identidade`, `POST|DELETE|GET /instituicoes/{id}/logo`, `GET /instituicoes/mantenedores`).
- **CLF (Colégio Luciano Feijão):** é uma instituição própria (`Tipo = Colegio`), gerenciada à parte, ligada ao **mesmo mantenedor** da faculdade (o colégio exige mantenedor). Cadastro pelo Admin em "Nova instituição" (o botão "Colégio (CLF)" já preenche nome, sigla e mantenedor). Card "Rede do mantenedor" para alternar entre as instituições.
- **Seletor de instituição** no topo (quando a pessoa atua em mais de uma), gravado no cookie (`POST /ui/instituicao/selecionar`). Login passa a gravar a instituição padrão.
- **Questões:** edição completa na UI (enunciado, alternativas, gabarito, justificativa, assunto, dificuldade, disciplina e selos), inclusive dos rascunhos da IA antes de salvar.
- **Selos:** catálogo `SelosTipos` (IA, ENADE, ENAMED, ENEM com ano, Vestibular com instituição e ano) + `QuestoesSelos` (sem duplicidade; o professor escolhe quais exibir). Aparecem no banco, nas provas e na prática do aluno. Novo tipo de selo = uma linha no catálogo (`POST /questoes/selos/tipos`, Admin).
- **Simulados do aluno:** tabela `ResultadosPratica`, separada das notas oficiais; o aluno escolhe contabilizar ou não, na hora e depois. Seção "Simulados e treinos" no painel e em Minhas notas, com visual distinto e rótulo "Não oficial".
- **Histórico de atividades (auditoria):** tela `/historico` e `GET /auditoria/list`. Admin vê tudo; Gestor, a gestão acadêmica da instituição; Professor, suas turmas e conteúdos e a atividade dos alunos. Somente leitura (interceptor do EF + gatilhos do PostgreSQL). Registra criação, edição, exclusão, mudança de status, login/logout/acesso recusado e a atividade do aluno.
- Migration `AddInstitutionTypesSealsPracticeResultsAndAudit` e `AppDbContextModelSnapshot` (gerado do modelo real; sem diferenças pendentes). A partir daqui `dotnet ef migrations add` gera só o que for novo.

### Alterado
- `AddDbContextFactory` passou de Singleton para Scoped (o interceptor precisa saber quem está salvando).
- `GetOverview` e a lotação de pessoas agora trabalham na instituição **em uso** (antes: "a primeira ativa").
- Criar instituição pela API ficou só com o Admin.
- Questões criadas pela API passam a herdar a instituição da disciplina.

## [Não publicado] — Instituição, prédios e visual por perfil

### Adicionado
- **Instituição e unidades (camada Global):** API `/instituicoes`, `/unidades` e `/usuario-instituicao` (criada pela equipe) integrada ao projeto, com migrations `MigrationAtt` e `MigrationUsuarioInstituicao`.
- **Lotação por unidade:** coluna `UnidadeId` em `UsuariosInstituicoes` (migration `MigrationUnidadeDoUsuario`) e rota `PUT /usuario-instituicao/atualizar`.
- **Tela `/instituicao`** (Admin e Gestor): dados da faculdade, cartões dos prédios com contagem por papel, perfil de Medicina e lotação das pessoas.
- **Visual por perfil e por prédio:** `temas.css`, `UiTheme` e `UiSession` (tema por lotação e por curso), componente `RoleHero`, selo do prédio na barra lateral e seletor "ver como" para o Admin.
- **Login por prédio:** `/login?unidade=principal|anexo|odontologia|medicina`.
- **Instituição base na primeira execução:** Faculdade Luciano Feijão com Prédio Principal, **Anexo** e Prédio Odontologia (`Seed:BaseInstitution`).
- **Contas de demonstração** em todas as combinações de perfil × prédio e curso de Medicina de exemplo.
- **Avaliações (provas e simulados)**, banco de questões com IA, área de notas do aluno, foto de perfil com compressão, `SECURITY.md`, CI (build, auditoria de dependências, CodeQL) e Dependabot (entregas anteriores, mantidas).

### Alterado
- **Medicina deixou de ser prédio.** O visual de Medicina vale para alunos e professores do **curso** de Medicina (em qualquer prédio) e tem prioridade sobre o do prédio. O prédio que se chamava "Prédio Medicina" agora é o **Anexo**.
- `VincularUsuarioInstituicao` recebe o `UsuarioInstituicaoRequest` (antes recebia o `ResponseDTO`) e reaproveita vínculos encerrados.
- Listagens de vínculos devolvem só vínculos ativos.
- `AtivarInstituicao` usa `Enable()` (atualiza a data de alteração).
- A raiz `/` abre direto no login.

### Corrigido
- **Segurança:** as rotas de instituição e de unidade aceitavam qualquer pessoa sem login, inclusive `DELETE`, ativar e atualizar. Agora leituras exigem login e alterações exigem Gestão (excluir ou reativar instituição, só Admin).
- `PUT /unidades/atualizar/{id}` exigia a policy `Professor`; agora `Gestao`.
- `POST /unidades/register` comparava o ID do usuário com o ID da instituição; verificação removida.
- Erros de regra (não encontrado, CNPJ/nome/sigla repetidos, unidade com pessoas lotadas, papel inválido) deixaram de virar erro 500 e respondem 404, 400 ou 409.
- Namespace `EduAssess.EduAssess.Aplication...` (duplicado e com erro de digitação) renomeado para `EduAssess.Application...`.
- Rotas de questões: policy inexistente, ordem dos campos no DTO, leitura sem login e checagem de posse (entregas anteriores).

### Segurança
- Veja [SECURITY.md](SECURITY.md) e a seção *Segurança e LGPD* do README.
