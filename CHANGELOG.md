# Changelog

Todas as mudanças relevantes do projeto. Formato inspirado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/).
Os mesmos comentários estão no código, junto de cada alteração (procure por `NOVO`, `ALTERADO` e `CORREÇÃO`).

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
