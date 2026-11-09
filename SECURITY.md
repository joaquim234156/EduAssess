# Política de Segurança

O **EduAssess** trata dados pessoais de estudantes, professores e gestores (nome, e-mail, CPF, telefone, data de nascimento, foto, matrículas e notas). Levamos a sério qualquer falha que possa expô-los. Obrigado por nos ajudar a manter o projeto seguro.

## Versões com suporte

O projeto está em desenvolvimento ativo e ainda não tem versões numeradas. Correções de segurança são aplicadas **somente na branch `main`**.

| Versão | Suporte a correções de segurança |
|---|---|
| `main` (mais recente) | ✅ |
| Qualquer outra | ❌ |

## Como reportar uma vulnerabilidade

**Não abra uma *issue* pública** para falhas de segurança: isso expõe o problema antes da correção.

Use um destes canais privados:

1. **Recomendado:** aba **Security → Report a vulnerability** deste repositório (relatório privado do GitHub).
2. **E-mail:** `[PREENCHER: e-mail do responsável pela segurança]`

> 📝 **Mantenedores:** ative *Private vulnerability reporting* em *Settings → Code security* e preencha o e-mail acima antes de publicar o repositório.

### O que incluir no relatório

- Descrição do problema e do **impacto** (que dados ou funções ficam expostos).
- **Passos para reproduzir**, de preferência mínimos (rota, perfil de usuário usado, requisição).
- Versão ou *commit* afetado e o ambiente (Desenvolvimento ou Produção).
- Prova de conceito, se houver. **Não inclua dados pessoais reais** de terceiros.

## O que esperar de nós

Estas são metas de atendimento, e não garantias contratuais:

| Etapa | Meta |
|---|---|
| Confirmação de recebimento | até **3 dias úteis** |
| Triagem e classificação de severidade | até **7 dias corridos** |
| Correção de severidade **crítica** | até **7 dias** |
| Correção de severidade **alta** | até **30 dias** |
| Correção de severidade **média** ou **baixa** | até **90 dias** |

Mantemos você informado do andamento e, se desejar, damos o **crédito** pela descoberta quando a correção for publicada.

## Escopo

**Dentro do escopo**

- Autenticação e sessão (login, cookie, JWT) e controle de acesso: acesso a dados de outros usuários ou instituições, elevação de privilégio entre perfis (`Admin`, `Gestor`, `Professor`, `Aluno`, `User`).
- Rotas da API (`/auth`, `/cursos`, `/turmas`, `/disciplina`, `/matricula`, `/notas`, `/questoes`) e telas do painel.
- Upload e entrega de fotos de perfil.
- Injeção de comandos ou consultas, XSS, CSRF, exposição de segredos, vazamento de dados pessoais.
- Geração de questões por IA: injeção de instruções (*prompt injection*) que vaze dados ou mude o comportamento do sistema.

**Fora do escopo**

- Negação de serviço volumétrica, engenharia social e ataques físicos.
- Falhas em serviços de terceiros (por exemplo, Google Gemini ou o provedor de hospedagem): reporte ao fornecedor.
- Problemas que dependam de contas de demonstração (`*@eduassess.demo`, `admin@eduassess.local`) em ambiente de **Desenvolvimento**: elas existem só para uso local.
- Relatórios automáticos de ferramentas sem demonstração de impacto.

## Boas práticas ao testar (porto seguro)

Se você agir de boa-fé e seguir estas regras, não tomaremos medidas contra você:

- Teste apenas em **instância própria** ou com autorização por escrito da instituição responsável. Nunca em dados de produção de terceiros.
- Acesse o **mínimo** de dados necessário para demonstrar o problema; não copie, altere nem apague informações de outras pessoas.
- Não degrade o serviço nem execute testes de carga.
- Dê-nos prazo razoável para corrigir antes de divulgar publicamente (**divulgação coordenada**).

## Dados pessoais e LGPD

Se a falha tiver exposto **dados pessoais reais**, o fato deve ser comunicado imediatamente à instituição controladora (Faculdade Luciano Feijão) e ao seu encarregado de dados. Em geral, a comunicação de incidente de segurança à ANPD e aos titulares afetados tem **prazo de 3 dias úteis** a partir do conhecimento (Resolução CD/ANPD nº 15/2024). Confirme o enquadramento do caso com o encarregado e com o jurídico.

## Recomendações para quem opera a plataforma

- Defina `JwtSettings:Secret` (mínimo de 32 bytes) por variável de ambiente ou cofre de segredos e **gire-o** periodicamente.
- Nunca versione `appsettings` com segredos, `*.db` ou `App_Data/` (o CI bloqueia isso).
- Use HTTPS em todo o tráfego e mantenha o HSTS ativo.
- Mantenha as dependências atualizadas (o Dependabot e o CI alertam sobre pacotes vulneráveis).
- Faça cópias de segurança criptografadas do banco e teste a restauração.
- Em produção, prefira banco gerenciado com criptografia em repouso (consulte o *Roadmap* do README).

---

*Última revisão: 2026.*
