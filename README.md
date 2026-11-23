# EduAssess

Plataforma web de gestão acadêmica e avaliação. O EduAssess reúne cadastros acadêmicos, organização de turmas e matrículas, lançamento e consulta de notas, banco de questões, provas/simulados, prática dos alunos e histórico de atividades. A aplicação foi construída para atender mais de uma instituição, mantendo os dados acadêmicos separados por instituição e permitindo agrupar instituições sob um mesmo mantenedor.

> **Estado da documentação:** este arquivo descreve o código presente neste repositório. Recursos planejados que ainda não possuem fluxo implementado não são apresentados como concluídos.

## 1. Tecnologias

### Frontend

- **Blazor Web App / Razor Components**, com renderização interativa no servidor.
- **Razor**, HTML, CSS e JavaScript para páginas, componentes e interações.
- Componentes compartilhados para navegação, modais, formulários, ícones, questões, selos, mensagens e indicadores.
- CSS próprio para identidade visual, responsividade, temas por perfil e temas por unidade.

### Backend

- **.NET 8 / ASP.NET Core**.
- **Minimal APIs** para os endpoints HTTP; o projeto também registra suporte a Controllers.
- **Entity Framework Core 8** para persistência e consultas.
- **Npgsql** como provedor de PostgreSQL.
- **PostgreSQL** como banco de dados da aplicação atual.
- **JWT Bearer e cookies de autenticação**: JWT para consumo da API e cookie para a sessão do painel web.
- **BCrypt.Net** para hash e verificação de senhas.
- **MiniValidation** para validação de DTOs nas rotas.
- **Swashbuckle / Swagger** para documentação interativa da API.
- **ImageSharp** para validar, redimensionar, remover metadados e converter imagens para WebP.
- Integração opcional com **Google Gemini** para geração assistida de questões.

O projeto atual **não usa SQLite em tempo de execução**. Há uma pasta `Migrations.SQLiteArchive` excluída da compilação, mantida como arquivo histórico; a aplicação em execução usa PostgreSQL.

## 2. Estrutura do repositório

```text
EduAssess/
├── EduAssess.slnx
├── README.md
├── SECURITY.md
└── EduAssess/
    ├── Application/
    │   ├── DTOs/                 # Contratos de entrada e saída
    │   ├── Interfaces/           # Contratos dos serviços
    │   ├── Routes/               # Minimal APIs por módulo
    │   └── Services/             # Regras de negócio
    ├── Components/
    │   ├── Layout/               # Layouts do painel e páginas públicas
    │   ├── Pages/                # Páginas Razor
    │   └── Shared/               # Componentes reutilizáveis
    ├── Converters/               # Conversores JSON
    ├── Data/
    │   ├── Audit/                # Interceptor e contexto de auditoria
    │   ├── AppDbContext.cs       # Modelo e mapeamento EF Core
    │   └── DatabaseInitializer.cs# Migrações e dados iniciais
    ├── Extensions/               # Extensões compartilhadas
    ├── Migrations/               # Migrações PostgreSQL e snapshot EF Core
    ├── Models/                   # Entidades e enums
    ├── Properties/               # Configuração de execução local
    ├── Web/                      # Serviços de tela, sessão e view models
    ├── wwwroot/                  # CSS, JavaScript e imagens estáticas
    ├── appsettings.json          # Configuração não secreta
    └── EduAssess.csproj
```

### Organização em camadas

- **Models:** entidades de domínio, estados e classificações.
- **Data:** `AppDbContext`, mapeamentos, interceptor de auditoria e inicialização do banco.
- **Application/DTOs:** contratos usados para receber e retornar dados.
- **Application/Interfaces:** contratos dos serviços de aplicação.
- **Application/Services:** validações e regras de negócio compartilhadas pela API.
- **Application/Routes:** endpoints HTTP agrupados por módulo.
- **Web:** serviços usados pelas telas Blazor, sessão, modelos de apresentação e identidade visual.
- **Components:** páginas, layouts e componentes de interface.

## 3. Requisitos para desenvolvimento

- SDK do **.NET 8**.
- Instância acessível do **PostgreSQL**.
- Git e editor/IDE compatível com .NET, como Visual Studio ou Visual Studio Code.
- Para geração de questões por IA, uma chave válida da API do Google Gemini. A aplicação pode funcionar sem essa chave; a funcionalidade de geração por IA ficará indisponível.

## 4. Configuração e execução local

Os comandos abaixo partem da pasta que contém `EduAssess.slnx`.

### 4.1. Preparar o banco

Crie um banco PostgreSQL, por exemplo:

```sql
CREATE DATABASE eduassess;
```

Configure a conexão por variável de ambiente, ou altere `ConnectionStrings:DefaultConnection` no arquivo local de configuração. Exemplo no PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=eduassess;Username=postgres;Password=SUA_SENHA"
```

Não grave senhas reais no repositório. Use variáveis de ambiente, User Secrets ou um gerenciador de segredos.

### 4.2. Restaurar dependências e executar

```bash
dotnet restore EduAssess/EduAssess.csproj
dotnet run --project EduAssess/EduAssess.csproj
```

Na configuração padrão, o projeto executa o painel web e a API no mesmo host. O endereço exato é informado pelo ASP.NET Core ao iniciar. Em ambiente de desenvolvimento, a documentação Swagger fica disponível em `/swagger`.

### 4.3. Migrações

Na inicialização, `DatabaseInitializer.InitializeAsync` chama `Database.MigrateAsync()`. As migrações pendentes são aplicadas automaticamente antes de a aplicação começar a atender requisições.

Para administrar migrações manualmente, instale a ferramenta EF Core compatível com a versão 8 e execute, a partir da raiz do repositório:

```bash
dotnet tool install --global dotnet-ef --version 8.0.31
dotnet ef migrations list --project EduAssess/EduAssess.csproj
# Opcional: aplicar manualmente, com o banco e as credenciais já configurados
dotnet ef database update --project EduAssess/EduAssess.csproj
```

Antes de aplicar alterações em produção, faça backup do banco e valide a restauração em um ambiente separado.

## 5. Configurações de ambiente

As chaves abaixo podem ser definidas em `appsettings.*.json`, User Secrets ou variáveis de ambiente. Para variáveis de ambiente do .NET, use `__` para representar `:`.

| Chave de configuração | Uso |
|---|---|
| `ConnectionStrings__DefaultConnection` | String de conexão PostgreSQL. |
| `JwtSettings__Secret` | Segredo JWT com pelo menos 32 bytes. Obrigatório fora de Development. |
| `BootstrapAdmin__Name` | Nome do primeiro administrador, quando o banco ainda não possui usuários. |
| `BootstrapAdmin__Email` | E-mail do primeiro administrador. |
| `BootstrapAdmin__Password` | Senha do primeiro administrador; mínimo de 12 caracteres e limite de 72 bytes. |
| `Gemini__ApiKey` | Chave para geração de questões com Gemini. |
| `Gemini__Model` | Modelo Gemini; o padrão no serviço é `gemini-flash-lite-latest`. |
| `Seed__DemoData` | Controla dados demonstrativos. Só são criados em Development; padrão `true` nesse ambiente. |
| `Seed__BaseInstitution` | Controla a criação da instituição base FLF; padrão `true`. |
| `APP_MODE` | `Full` (padrão), `App` (painel sem Swagger) ou `ApiOnly` (API/Swagger sem o painel Blazor). |

### Administrador inicial e dados demonstrativos

- Se não houver nenhum usuário e o ambiente for **Development**, o inicializador cria um administrador local de desenvolvimento caso `BootstrapAdmin` não tenha sido configurado: `admin@eduassess.local` / `Admin@12345!`.
- Essas credenciais são **exclusivas para desenvolvimento local**. Configure um administrador seguro antes de qualquer implantação real.
- Contas e dados demonstrativos são criados somente em Development, quando `Seed:DemoData` está habilitado. As contas demonstrativas usam a senha `Demo@12345` e não devem ser utilizadas em produção.
- A instituição base Faculdade Luciano Feijão e o mantenedor `Luciano Feijão` são criados pelo inicializador quando habilitado e quando ainda não há outra instituição cadastrada. Não há criação automática do CLF: seus dados cadastrais reais devem ser informados por um Admin ao cadastrá-lo.
- Em Development, se nenhum segredo JWT válido for configurado, o serviço gera uma chave local em `App_Data/dev-jwt-secret.txt`. Fora de Development, `JwtSettings__Secret` é obrigatório.

## 6. Banco de dados e persistência

`AppDbContext` é o ponto central do modelo relacional. O provedor atual é PostgreSQL via Npgsql e os mapeamentos são mantidos junto às migrações em `Migrations/`.

Entre os principais conjuntos de dados estão:

- Usuários e vínculos usuário–instituição.
- Instituições, mantenedores e unidades.
- Cursos, disciplinas, turmas, matrículas e notas.
- Questões, tipos de selos e selos aplicados às questões.
- Avaliações, composição de avaliações e resultados de prática.
- Contatos e registros de auditoria.

Os dados acadêmicos que pertencem a uma instituição possuem relacionamento/identificador institucional. Os serviços do painel consultam os registros no contexto da instituição selecionada; o vínculo com um mantenedor não reúne cursos, pessoas, matrículas, avaliações ou notas de instituições diferentes.

### Arquivos de imagem

Os bytes das imagens não são armazenados no PostgreSQL. `ImageStorageService` grava arquivos WebP fora de `wwwroot`, sob `Environment.SpecialFolder.LocalApplicationData`:

```text
EduAssess/uploads/
├── avatars/       # Fotos de perfil (versão completa e miniatura)
├── logos/         # Logotipos das instituições
└── unit-logos/    # Logotipos específicos das unidades
```

A entidade guarda a referência ao arquivo. O processamento limita o upload a 8 MB, confere o formato real, limita dimensões/pixels, remove metadados e converte a imagem. Em produção, o diretório precisa ter permissão de escrita, persistência entre reinicializações e rotina de backup/restauração, assim como o banco.

## 7. Módulos e funcionalidades disponíveis

### 7.1. Autenticação e contas

- Cadastro de conta e autenticação.
- Sessão web baseada em cookie e autenticação JWT para API.
- Recuperação do perfil atual e validação da conta ativa.
- Perfil, foto de usuário e alteração de senha na área Minha conta.
- Contas sem perfil acadêmico definido ficam na área de aguardando perfil.
- A API de autenticação também contempla operações de gestão de contas e perfis.

### 7.2. Gestão acadêmica

- **Cursos:** cadastro, edição, ativação/desativação e listagem de cursos por instituição.
- **Disciplinas:** cadastro, edição e status; vinculação a professor ativo da instituição.
- **Turmas:** criação e manutenção de turmas associadas a curso e professor.
- **Alunos e equipe:** listagem de alunos, gestores e professores, contas pendentes e vinculação à instituição.
- **Matrículas:** criação, edição, encerramento e reativação de vínculos entre aluno e turma.
- **Notas:** lançamento e consulta de notas oficiais; escala de 0 a 10.
- **Painéis:** visão de gestão com indicadores da instituição, visão de professor com suas turmas e notas, e visão do aluno com disciplinas, turmas e desempenho.

### 7.3. Banco de questões

- Cadastro manual e edição de questões.
- Organização por disciplina e assunto, com dificuldade.
- Questões de múltipla escolha com alternativas, gabarito e explicação.
- Filtros por disciplina, assunto, dificuldade e selo.
- Classificação por selos, incluindo o catálogo inicial IA, ENADE, ENAMED, ENEM e Vestibular; selos podem carregar ano/detalhe e opção de exibição.
- Questões de professores são vinculadas ao autor e à instituição. Professor gerencia as próprias questões; gestão possui visão administrativa mais ampla dentro da instituição.

### 7.4. Geração de questões com IA

- Integração com Google Gemini, quando `Gemini:ApiKey` está configurada.
- Geração de questões por disciplina, assunto, dificuldade e quantidade, com orientações adicionais.
- As questões geradas retornam como rascunhos para revisão do professor; a geração não grava automaticamente no banco.
- O usuário revisa e escolhe quais rascunhos salvar. Erros de configuração, cota ou comunicação com o provedor são retornados como falha da integração.

### 7.5. Avaliações, provas e simulados

- Cadastro de prova ou simulado, disciplina, título, detalhes e periodicidade.
- Inclusão, remoção e reordenação de questões do banco.
- Estados de rascunho e aplicada.
- Código para localizar/compartilhar uma avaliação aplicada com alunos.
- Acesso do aluno às avaliações que foram aplicadas na instituição.

### 7.6. Prática e desempenho do aluno

- Treino livre por disciplina, com questões sorteadas do banco.
- Resposta e correção durante a prática, com exibição de explicação.
- Resposta a provas/simulados aplicados, inclusive por código.
- Histórico individual de tentativas e resultados de prática.
- O aluno escolhe se cada resultado de treino deve ser contabilizado no seu resumo de prática.
- Resultados de prática são armazenados separadamente das notas oficiais; não alteram a tabela de notas acadêmicas.
- Painel do aluno apresenta notas, evolução e resumos de desempenho por disciplina.

### 7.7. Instituições, mantenedores e unidades

A estrutura tem três níveis distintos:

1. **Mantenedor/rede:** organização administrativa responsável por uma ou mais instituições.
2. **Instituição:** entidade educacional independente, com sua própria categoria, usuários, dados acadêmicos e identidade visual.
3. **Unidade:** estrutura vinculada a uma única instituição, classificada como Campus, Polo ou Prédio.

#### Categorias institucionais

O cadastro/edição de instituição oferece as categorias Faculdade / Ensino Superior, Colégio / Ensino Básico, Escola / Ensino Básico, Instituto e Centro técnico. A categoria institucional não é o tipo de unidade física. A lista está centralizada no enum `TipoInstituicao` e pode ser estendida com novas opções.

A categoria é definida no formulário da instituição. A alteração da categoria e do mantenedor é restrita ao Admin da plataforma; o Gestor da instituição pode editar os outros dados permitidos, sem mudar esses campos administrativos.

#### Rede Faculdade Luciano Feijão / Colégio Luciano Feijão

O modelo `MantenedorModel` e a chave `InstituicaoModel.MantenedorId` já representam a rede. A FLF base é criada pelo inicializador e vinculada ao mantenedor `Luciano Feijão`. Um Admin pode cadastrar o CLF como outra instituição, escolher Colégio / Ensino Básico e selecionar o mesmo mantenedor. O formulário sugere o nome “Colégio Luciano Feijão” e a sigla “CLF” ao selecionar a categoria Colégio, mas os dados cadastrais obrigatórios (como CNPJ, contato e endereço) devem ser confirmados e preenchidos pelo responsável.

As instituições vinculadas aparecem agrupadas na área **Instituição e unidades**. O vínculo permite visualizar informações cadastrais da rede e alternar para outra instituição somente quando o usuário também possui vínculo nela. Ser do mesmo mantenedor, por si só, não dá acesso aos dados acadêmicos da instituição irmã.

#### Unidades e identidade visual

Cada instituição pode cadastrar unidades dos tipos:

- Campus (`TipoUnidade.Campus`, valor legado 1).
- Polo (`TipoUnidade.Polo`, valor legado 2).
- Prédio (`TipoUnidade.Predio`, valor 3).

Cada unidade pode ter logotipo, cor primária e cor secundária próprios. Cada cor é independente: quando a propriedade não possui valor próprio, herda a cor da instituição. O logotipo da instituição também é usado quando não existe logotipo próprio da unidade. Ao personalizar uma unidade, somente aquela unidade é afetada; os dados de identidade da instituição e das demais unidades permanecem separados.

A personalização pode ser gerenciada no formulário de criação/edição de unidade em **Instituição e unidades**. Os campos de cores reutilizam os seletores da interface existente, com opção “Herdar da instituição”.

### 7.8. Histórico de atividades

- Registro de alterações de entidades e eventos de acesso/atividade.
- Interceptor de persistência para registrar operações de banco, complementado por eventos explícitos de login, falha de login e prática do aluno.
- A página `/historico` e a API de auditoria são somente leitura; não há operações de edição ou exclusão de registros de auditoria.
- O Admin tem visão global; Gestor e Professor têm escopo institucional e de responsabilidade conforme suas permissões. O endereço IP é exibido somente ao Admin.

### 7.9. Contatos e páginas públicas

- Página pública de apresentação, método e conteúdos editoriais.
- Formulário de contato e caixa de solicitações para a equipe.
- Essas páginas editoriais não constituem, por si só, um módulo de distribuição de materiais acadêmicos para turmas.

## 8. Perfis e autorização

A autorização é aplicada no backend; ocultar um botão na interface não substitui as verificações de acesso.

| Perfil | Escopo geral |
|---|---|
| **Admin** | Administração global da plataforma; cadastro de instituições e mantenedores; acesso administrativo a instituições e unidades; supervisão global conforme as rotas. |
| **Gestor** | Gestão acadêmica e administrativa somente nas instituições em que possui vínculo ativo de Gestor. Categoria institucional e mantenedor permanecem sob responsabilidade do Admin. |
| **Professor** | Disciplinas/turmas e conteúdos de sua responsabilidade, lançamento de notas conforme a atribuição e recursos de avaliação/prática disponíveis ao perfil. |
| **Aluno** | Próprias turmas, disciplinas, notas, avaliações aplicadas e resultados de prática. |
| **User** | Conta sem perfil acadêmico atribuído; acesso restrito ao fluxo de aguardando perfil. |

A escolha da instituição ativa é validada contra vínculos no banco. O mantenedor agrupa instituições, mas não substitui os vínculos de usuário nem amplia automaticamente as permissões. Nas rotas de instituições, unidades e vínculos usuário–instituição, o Admin tem escopo global e o Gestor fica limitado às instituições em que possui vínculo ativo de Gestão. O Gestor também não pode criar um novo vínculo para uma conta que já pertença a outra instituição; o compartilhamento entre instituições exige ação do Admin.

## 9. Principais páginas do painel

| Página | Rota | Finalidade |
|---|---|---|
| Login | `/` e `/login` | Entrar no painel. |
| Painel de gestão | `/painel` | Indicadores e visão geral da instituição selecionada. |
| Cursos | `/cursos` | Catálogo de cursos. |
| Disciplinas | `/disciplinas` | Disciplinas e professores responsáveis. |
| Turmas | `/turmas` | Turmas e seus vínculos. |
| Matrículas | `/matriculas` | Matrículas de alunos. |
| Notas | `/notas` | Lançamento e consulta de notas oficiais. |
| Alunos | `/alunos` | Alunos e contas pendentes. |
| Equipe | `/equipe` | Gestores e professores. |
| Banco de questões | `/banco-questoes` | Questões, filtros, selos e geração assistida. |
| Avaliações | `/avaliacoes` | Provas, simulados e composição de questões. |
| Praticar | `/praticar` | Treinos e avaliações aplicadas ao aluno. |
| Meu painel | `/aluno` | Visão do aluno. |
| Minhas notas | `/aluno/notas` | Desempenho em notas oficiais. |
| Meu painel de professor | `/professor` | Turmas e notas de responsabilidade. |
| Instituição e unidades | `/instituicao` | Instituição, mantenedor/rede, unidades, identidade visual e lotação de pessoas. |
| Histórico | `/historico` | Consulta de registros e atividade. |
| Minha conta | `/conta` | Dados pessoais, foto e senha. |
| Solicitações | `/contatos` | Caixa de mensagens/contatos. |

## 10. API e documentação HTTP

As Minimal APIs são registradas em `Program.cs` e organizadas por módulo em `Application/Routes/`. Entre os grupos atuais estão:

- `/auth`
- `/instituicoes`
- `/unidades`
- `/usuario-instituicao`
- `/cursos`
- `/disciplina`
- `/turmas`
- `/matricula`
- `/notas`
- `/questoes`
- `/auditoria`

A documentação interativa fica em `/swagger` quando `APP_MODE` não está definido como `App`. Os endpoints de upload recebem `multipart/form-data`; os demais usam os DTOs declarados em `Application/DTOs/`.

## 11. Migrações e compatibilidade desta atualização

A ampliação da classificação institucional usa enum armazenado como texto. Os valores anteriores `Faculdade` e `Colegio` são preservados; novas opções foram acrescentadas ao enum sem renumerar os valores existentes.

Os tipos de unidade são armazenados como enum numérico. `Campus = 1` e `Polo = 2` foram mantidos; `Predio = 3` foi adicionado, portanto os registros existentes não precisam ser convertidos.

A migration `20261216090000_AddUnitBranding` adiciona à tabela `Unidades` três colunas opcionais:

- `LogoPath` (`text`, nulo permitido).
- `CorPrimaria` (`varchar(7)`, nulo permitido).
- `CorSecundaria` (`varchar(7)`, nulo permitido).

Como são colunas novas e opcionais, unidades anteriores continuam válidas e herdam a identidade visual da instituição até que recebam personalização própria. A aplicação executa a migração pendente na inicialização.

## 12. Testes e validação

O repositório contém arquivos `.http` para chamadas manuais à API e a pasta `Test/`, mas não contém atualmente um projeto automatizado xUnit/NUnit/MSTest configurado no `.slnx`.

Ao alterar o sistema, valide em ambiente local, pelo menos:

1. Login com Admin e Gestor vinculados a instituições diferentes.
2. Tentativa de acessar ou alterar uma instituição/unidade sem vínculo apropriado.
3. Cadastro e alteração de categoria institucional por Admin; confirmação de que Gestor não altera categoria nem mantenedor.
4. Cadastro e edição de Campus, Polo e Prédio, preservando os valores antigos.
5. Upload, substituição e remoção do logotipo de unidade.
6. Personalização separada de cor primária e secundária; herança quando cada campo está vazio.
7. Edição de uma unidade sem alterar a marca da instituição ou de outra unidade.
8. Cadastro de FLF e CLF como instituições diferentes com o mesmo mantenedor; confirmação de que cursos, pessoas, turmas, matrículas, avaliações e notas continuam separados.
9. Consulta do histórico por perfil.

Não existe uma bateria automatizada de integração para substituir esses testes manuais no estado atual do repositório.
