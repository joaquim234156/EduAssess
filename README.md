# EduAssess
Plataforma estudantil com API ASP.NET Core, painel Blazor Server e banco SQLite.

## Configuração local

O segredo JWT e as credenciais de provisionamento do primeiro administrador não ficam versionados. Configure-os em User Secrets para desenvolvimento:

```powershell
dotnet user-secrets set "JwtSettings:Secret" "<segredo aleatório com pelo menos 32 bytes>" --project .\EduAssess\EduAssess.csproj
dotnet user-secrets set "BootstrapAdmin:Email" "admin@instituicao.edu.br" --project .\EduAssess\EduAssess.csproj
dotnet user-secrets set "BootstrapAdmin:Password" "<senha forte de pelo menos 12 caracteres>" --project .\EduAssess\EduAssess.csproj
dotnet run --project .\EduAssess\EduAssess.csproj
```

A senha administrativa deve ter no máximo 72 bytes. Na primeira inicialização com banco vazio, a conta Admin é criada somente quando as duas configurações `BootstrapAdmin` estiverem definidas. Em implantação, use o gerenciador de segredos do ambiente e configure `JwtSettings__Secret`, `BootstrapAdmin__Email` e `BootstrapAdmin__Password`. O segredo JWT deve ser novo: uma chave que já esteve no repositório deve ser considerada comprometida e rotacionada. A aplicação rejeita a chave histórica conhecida; rotacione-a em todos os ambientes e invalide os tokens emitidos com ela. Essa proteção não remove a chave do histórico Git.

Abra a URL exibida no terminal. O Swagger fica disponível em `/swagger` no ambiente de desenvolvimento.

## Cadastro e hierarquia de perfis

- Qualquer visitante pode criar uma conta em `/cadastro`; ela recebe somente o perfil neutro `User`.
- Admin pode criar Gestores.
- Gestor pode criar Professores.
- Admin, Gestor ou Professor pode atribuir o perfil Aluno a uma conta existente na página `/alunos`.
- Somente Admin e Gestor acessam as demais áreas de gestão. Professores autenticados ficam limitados à atribuição de alunos.

As mesmas restrições são validadas no servidor/API, não apenas pela interface. A API usa `POST /auth/register` para o cadastro público, `POST /auth/users/managers` para Admin, `POST /auth/users/professors` para Gestor e `POST /auth/users/{id}/assign-student` para os perfis autorizados. Consulte `EduAssess.http` para exemplos de chamadas.

As operações protegidas do painel revalidam no banco se a conta continua ativa e se o perfil ainda está autorizado, evitando que uma sessão aberta preserve permissões após a desativação ou alteração do perfil.

## Verificação

```powershell
dotnet build .\EduAssess\EduAssess.csproj
```
