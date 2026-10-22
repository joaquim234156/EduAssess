using System.Security.Cryptography;
using System.Text;

namespace EduAssess.Application.Services
{
    /// <summary>
    /// Resolve o segredo JWT sem deixar nenhum valor fixo no repositório.
    /// - Produção: exige JwtSettings:Secret (variável de ambiente JwtSettings__Secret ou gerenciador de segredos).
    /// - Desenvolvimento: se nada estiver configurado, gera um segredo aleatório uma única vez e o guarda em
    ///   App_Data/dev-jwt-secret.txt (pasta ignorada pelo Git), para o projeto rodar sem configuração manual.
    /// </summary>
    public static class JwtSecretResolver
    {
        // Chave que já esteve versionada no repositório: considerada comprometida e sempre rejeitada.
        private const string CompromisedSecret =
            "#2V$)#@VV21##V87$Y#35V$YVN#@M1324#2$V#$M#@)90#NV@$__)UV)%$@(_%%6V+$@V3U)";

        public static string Resolve(IConfiguration configuration, IHostEnvironment environment, ILogger logger)
        {
            var configured = configuration["JwtSettings:Secret"];
            if (IsAcceptable(configured))
                return configured!;

            if (!string.IsNullOrWhiteSpace(configured))
                logger.LogWarning("O segredo JWT configurado é fraco (menos de 32 bytes) ou é a chave histórica comprometida; ele foi ignorado.");

            if (!environment.IsDevelopment())
                throw new InvalidOperationException(
                    "JwtSettings:Secret não configurado. Defina um segredo aleatório com pelo menos 32 bytes " +
                    "(ex.: variável de ambiente JwtSettings__Secret) antes de iniciar em produção.");

            var directory = Path.Combine(environment.ContentRootPath, "App_Data");
            var file = Path.Combine(directory, "dev-jwt-secret.txt");
            Directory.CreateDirectory(directory);

            if (File.Exists(file))
            {
                var stored = File.ReadAllText(file).Trim();
                if (IsAcceptable(stored))
                    return stored;
            }

            var generated = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            File.WriteAllText(file, generated);
            logger.LogWarning("Segredo JWT de desenvolvimento gerado em {File}. Ele não é versionado.", file);
            return generated;
        }

        private static bool IsAcceptable(string? secret) =>
            !string.IsNullOrWhiteSpace(secret)
            && secret != CompromisedSecret
            && Encoding.UTF8.GetByteCount(secret) >= 32;
    }
}
