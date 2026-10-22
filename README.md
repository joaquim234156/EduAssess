# EduAssess

Plataforma de gestão acadêmica (cursos, disciplinas, turmas, matrículas e notas) em **ASP.NET Core 8 + Blazor Server + SQLite**, com painel web e API REST documentada no Swagger.

## Como rodar (VS Code)

Requisitos: [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
dotnet restore
dotnet run --project .\EduAssess\EduAssess.csproj --launch-profile "Painel + API (HTTP)"
```

Abra <http://localhost:5062>. Na **primeira execução em Desenvolvimento** o sistema cria sozinho o banco (`EduAssess/eduassess.db`), um administrador e dados de demonstração. Nada precisa ser configurado.

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador | `admin@eduassess.local` | `Admin@12345!` |
| Gestor | `gestor@eduassess.demo` | `Demo@12345` |
| Professor | `professor@eduassess.demo` | `Demo@12345` |
| Aluno | `aluno@eduassess.demo` | `Demo@12345` |
| Conta sem perfil | `diego@eduassess.demo` | `Demo@12345` |

Essas contas existem **somente em Desenvolvimento**. Para não criar os dados de demonstração: `"Seed": { "DemoData": false }` em `appsettings.Development.json`.

Swagger (API): perfil **Somente API + Swagger (HTTPS)** ou <http://localhost:5062/swagger> no modo `Full`. Exemplos de chamadas em `EduAssess/EduAssess.http`.

## Estrutura do site (seguindo o PreparaEdu)

Público: **Início** (hero com prévia do painel, "para quem é", desafio, resposta, módulos, fluxo, por que o EduAssess, números, contato, conteúdos), **Quem somos**, **O método**, **Conteúdos**, **Entrar**, **Criar conta**.
Painel (por perfil): visão geral da gestão, cursos, disciplinas, turmas, equipe, alunos, matrículas, notas, solicitações de contato, painel do professor, painel do aluno e "Minha conta".

Os textos institucionais (Quem somos, Método, Conteúdos) são ponto de partida: ajuste ao seu contexto em `Components/Pages/*.razor` e `Components/Shared/Articles.cs`.

## Perfis e permissões

- **Admin**: cria gestores e vê toda a gestão.
- **Gestor**: cursos, disciplinas, turmas, matrículas, notas, cria professores.
- **Professor**: lança notas só nas **suas turmas e disciplinas** e atribui perfil de aluno.
- **Aluno**: vê as próprias turmas, notas e médias.
- **User** (cadastro público): sem privilégios até alguém atribuir o perfil de aluno.

## Produção / segredos

- `JwtSettings:Secret` é **obrigatório** em produção (mín. 32 bytes), ex.: variável `JwtSettings__Secret`. A chave que estava no `appsettings.json` foi removida e é rejeitada.
- Para criar o primeiro admin: `BootstrapAdmin:Email` e `BootstrapAdmin:Password` (mín. 12 caracteres), via variáveis de ambiente ou `dotnet user-secrets`.
- Em desenvolvimento o segredo JWT é gerado em `EduAssess/App_Data/dev-jwt-secret.txt` (ignorado pelo Git).

## Banco de dados

- Banco novo: schema criado do modelo atual e migrations marcadas como aplicadas.
- Banco existente (ex.: o `eduassess.db` antigo): migrations pendentes são aplicadas, incluindo `AlinharDisciplinasEContatos`, que adiciona as colunas `ProfessorId`/`Ativa` em `Disciplinas` e a tabela `Contatos`. Disciplinas antigas ficam "Sem professor" até você editá-las.
- Para recomeçar do zero: apague `eduassess.db` (e `-shm`/`-wal`).

## O que foi corrigido

Veja o resumo completo na resposta de entrega. Principais: políticas de autorização inexistentes (`Gestao`, `Gestor`, `AtribuirAluno`) que quebravam rotas; `RequireAuthorization("Admin","Gestor")` que exigia as duas ao mesmo tempo; rate limiter nunca configurado; colunas de `Disciplinas` ausentes no banco; `NotaService` com `UsuarioId`/`DisciplinaId` trocados; exclusão de disciplina sem `SaveChanges`; aluno preso em loop de redirecionamento no login; sessão perdida ao recarregar a página; JWT/senha de admin fixos no repositório.
