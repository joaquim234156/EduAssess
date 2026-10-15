# EduAssess
Projeto de uma plataforma estudantil.

## Interface web

O projeto inclui um painel Blazor Server integrado ao mesmo host ASP.NET Core e ao banco SQLite da aplicação. Para iniciar localmente:

```powershell
dotnet run --project .\EduAssess\EduAssess.csproj
```

Abra a URL exibida no terminal. A interface permite consultar e gerenciar cursos, turmas, alunos, professores, matrículas e notas. O painel aceita apenas os perfis Admin e Gestor; as operações de gestão ficam disponíveis para esses perfis. O Swagger permanece disponível em `/swagger` no ambiente de desenvolvimento.

Na primeira inicialização, se o banco ainda não tiver usuários, a aplicação cria a conta administrativa:

- E-mail: `admin@eduassess.com`
- Senha: `Admin1234`

Altere a senha padrão antes de disponibilizar a aplicação fora de um ambiente de teste.
