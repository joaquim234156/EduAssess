# Relatório — Nova etapa de ajustes do EduAssess

## Resultado geral

Foram concluídas as alterações de código e interface previstas para esta etapa, preservando a estrutura Blazor/.NET existente. O recurso de mantenedores foi reaproveitado; não foi criada uma segunda entidade de grupo/rede.

**Limite da validação:** este ambiente não possui o SDK/CLI do .NET nem uma instância PostgreSQL configurada. Portanto, não foi possível executar `dotnet restore`, `dotnet build`, aplicar a migration ou realizar testes de interface/API contra um banco real. As verificações concluídas foram estáticas e de integridade dos arquivos. A entrega não deve ser considerada homologada em runtime até a execução desses passos em ambiente .NET 8 + PostgreSQL.

## 1. Funcionalidades implementadas

### Categoria institucional

- O formulário de cadastro e edição de instituição oferece Faculdade / Ensino Superior, Colégio / Ensino Básico, Escola / Ensino Básico, Instituto e Centro técnico.
- O campo aparece no início do formulário de instituição, antes dos dados cadastrais, porque descreve a natureza da organização e não uma unidade física.
- A categoria aparece na ficha da instituição, na visualização da rede e no seletor de instituição do topo.
- Somente Admin pode criar/alterar categoria e mantenedor. Gestor pode editar os demais dados autorizados, sem alterar esses campos.
- A lista é centralizada em `TipoInstituicao`; os valores existentes Faculdade e Colegio foram preservados.

### Identidade visual das unidades

- O formulário de criação/edição de unidade permite enviar logotipo, escolher cor primária e secundária e marcar individualmente cada cor para herdar da instituição.
- O upload usa o mesmo serviço de processamento de imagens e os mesmos padrões de interface da identidade institucional.
- Os logotipos das unidades são gravados em diretório separado (`EduAssess/uploads/unit-logos`), com validação do conteúdo real, limite de 8 MB, conversão para WebP e remoção de metadados.
- Cada cor herda individualmente quando seu campo está nulo. A unidade só prevalece nos campos em que possui configuração própria.
- A marca efetiva é aplicada ao layout do painel e à marca exibida no cabeçalho. A personalização de uma unidade não altera as propriedades da instituição ou de outra unidade.
- Remover o logotipo próprio faz a unidade voltar a usar o logotipo institucional.

### Tipos de unidade

- Adicionado `TipoUnidade.Predio = 3`.
- `Campus = 1` e `Polo = 2` foram mantidos sem renumeração.
- Cadastro, edição, cartões e identificação do tipo incluem Prédio.

### Rede institucional / mantenedor

- Foi preservado `MantenedorModel` e o relacionamento `InstituicaoModel.MantenedorId` já presente.
- A tela **Instituição e unidades** exibe as instituições do mesmo mantenedor, com categoria e estado, e oferece troca para outra instituição somente quando o usuário possui vínculo nela.
- No cadastro de uma instituição do tipo Colégio, a interface sugere “Colégio Luciano Feijão” e “CLF” e seleciona o mantenedor da instituição atual quando disponível.
- A FLF base permanece vinculada ao mantenedor Luciano Feijão pelo inicializador.
- O CLF não é criado com CNPJ, endereço ou contatos inventados. Um Admin deve concluir seu cadastro com dados oficiais e escolher o mesmo mantenedor da FLF.
- Compartilhar mantenedor não compartilha cursos, alunos, professores, turmas, matrículas, avaliações, notas ou permissões.

### Permissões e isolamento

- As rotas de instituições, unidades e vínculos usuário–instituição verificam o acesso à instituição alvo.
- Admin mantém escopo global. Gestor fica restrito às instituições nas quais possui vínculo ativo de Gestão.
- O Gestor não recebe no diretório de lotação contas vinculadas exclusivamente a outro tenant.
- A API também bloqueia que Gestor crie vínculo para uma conta já pertencente a outra instituição; compartilhamento entre instituições exige ação de Admin.
- A visualização da rede retorna apenas metadados cadastrais dos membros do mantenedor; não concede acesso acadêmico à instituição irmã.
- O endpoint de logotipo da unidade exige vínculo ativo com a instituição da unidade, ou papel Admin.

## 2. Arquivos alterados

- `README.md`
- `CHANGELOG.md`
- `SECURITY.md`
- `EduAssess/Application/DTOs/Instituicao/InstituicaoRequestPut.cs`
- `EduAssess/Application/DTOs/Unidade/UnidadeIdentidadeRequest.cs` (novo)
- `EduAssess/Application/DTOs/Unidade/UnidadeResponseDTO.cs`
- `EduAssess/Application/Interfaces/IUnidadeService.cs`
- `EduAssess/Application/Routes/InstituicaoRoutes/InstituicaoRoute.cs`
- `EduAssess/Application/Routes/UnidadeRoutes/UnidadeRoute.cs`
- `EduAssess/Application/Routes/UsuarioInstituicaoRoutes/UsuarioInstituicaoRoute.cs`
- `EduAssess/Application/Services/InstituicaoService.cs`
- `EduAssess/Application/Services/UnidadeService.cs`
- `EduAssess/Components/Layout/AppLayout.razor`
- `EduAssess/Components/Pages/Instituicao.razor`
- `EduAssess/Components/Shared/Logo.razor`
- `EduAssess/Data/AppDbContext.cs`
- `EduAssess/Migrations/20261216090000_AddUnitBranding.cs` (novo)
- `EduAssess/Migrations/AppDbContextModelSnapshot.cs`
- `EduAssess/Models/Global/InstituicaoModel/InstituicaoModel.cs`
- `EduAssess/Models/Global/UnidadeModel/UnidadeModel.cs`
- `EduAssess/Program.cs`
- `EduAssess/Web/ImageStorageService.cs`
- `EduAssess/Web/InstitutionOption.cs`
- `EduAssess/Web/InstitutionWebService.cs`
- `EduAssess/Web/UiFormat.cs`
- `EduAssess/Web/UiSession.cs`
- `EduAssess/Web/UiTheme.cs`
- `EduAssess/Web/ViewModels.cs`
- `EduAssess/wwwroot/css/site.css`

## 3. Alterações no banco de dados

A migration `20261216090000_AddUnitBranding` adiciona à tabela `Unidades`:

- `LogoPath` — `text`, nullable;
- `CorPrimaria` — `varchar(7)`, nullable;
- `CorSecundaria` — `varchar(7)`, nullable.

Não é necessária migration para acrescentar categorias institucionais: `TipoInstituicao` já é persistido como texto. Não é necessária conversão dos tipos de unidade: `TipoUnidade` é persistido numericamente e os valores Campus/Polo permanecem 1/2; Prédio usa 3.

O relacionamento de rede já existia como `InstituicaoModel.MantenedorId`; não foi criada tabela duplicada. A aplicação chama `Database.MigrateAsync()` no inicializador, de modo que a migration pendente será aplicada ao iniciar a aplicação com a configuração do banco válida. Faça backup antes de iniciar em produção.

## 4. Localização na interface

- **Categoria institucional:** modal de cadastro/edição de instituição, primeiro campo do formulário, visível ao Admin.
- **Rede/mantenedor:** seção “Rede de [mantenedor]” na página `/instituicao`.
- **Tipo Campus/Polo/Prédio:** seletor segmentado “Tipo de unidade” no modal de criação/edição da unidade.
- **Identidade visual da unidade:** seção “Logotipo ou imagem da unidade” e campos “Cor primária”/“Cor secundária”, no mesmo modal.
- **Categoria resumida:** ficha da instituição, cartões da rede e seletor do topo.

## 5. Verificações realizadas

- Revisão estática dos modelos, DTOs, serviços, rotas, tela Razor, sessão, layout, inicializador e mapeamento EF Core envolvidos.
- Verificação estrutural de delimitadores nos arquivos C# alterados: sem desequilíbrio de chaves, parênteses ou colchetes.
- Verificação de compatibilidade dos valores `Campus = 1`, `Polo = 2`, `Predio = 3`.
- Verificação de presença e correspondência das três colunas novas na migration e no snapshot.
- Revisão das regras de autorização das rotas de instituição, unidade e vínculo usuário–instituição.
- Revisão de que o vínculo de mantenedor não é utilizado como autorização acadêmica entre instituições.
- Revisão da documentação para refletir PostgreSQL/Npgsql e os módulos presentes no código.

**Não executado neste ambiente:** compilação, restauração de pacotes, aplicação real da migration, execução do app, testes HTTP, teste visual do Blazor, upload com arquivos reais e teste integrado com dois tenants no PostgreSQL. Não há projeto automatizado xUnit/NUnit/MSTest configurado na solução atualmente.

## 6. Pendências para homologação

1. Em ambiente com .NET 8 SDK e PostgreSQL, executar `dotnet restore EduAssess/EduAssess.csproj` e `dotnet build EduAssess/EduAssess.csproj`.
2. Fazer backup do banco e executar `dotnet ef database update --project EduAssess/EduAssess.csproj`, ou iniciar a aplicação com a string de conexão correta para o inicializador aplicar a migration.
3. Testar cadastro/edição de categorias como Admin e confirmar que Gestor não as altera.
4. Testar upload, substituição e remoção de logotipos; testar cada cor própria e herança independente.
5. Testar cadastro/edição de Campus, Polo e Prédio e conferir que registros anteriores preservam seus valores.
6. Cadastrar o CLF com dados cadastrais oficiais e vinculá-lo ao mantenedor Luciano Feijão; confirmar que ambas as instituições permanecem independentes.
7. Testar tentativas de acesso e vínculo cruzado com Admin, Gestor, Professor e Aluno em duas instituições distintas.
8. Definir backup persistente do diretório `EduAssess/uploads/` em conjunto com o backup do PostgreSQL.

Não foi criado CLF de demonstração nem foram usados dados fictícios para simular sua conclusão.
