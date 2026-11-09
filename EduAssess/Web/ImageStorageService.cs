using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace EduAssess.Web;

/// <summary>Resultado de um upload: o quanto a foto encolheu no servidor.</summary>
public sealed record AvatarResult(long OriginalBytes, long StoredBytes, long ThumbBytes)
{
    /// <summary>Economia em % (ex.: 94 = a versão salva é 94% menor que o arquivo enviado).</summary>
    public int SavedPercent => OriginalBytes <= 0
        ? 0
        : (int)Math.Clamp(Math.Round((1 - (double)StoredBytes / OriginalBytes) * 100), 0, 99);
}

/// <summary>
/// Guarda as fotos de perfil. Cada foto enviada vira DOIS arquivos WebP:
///   • "full"  — versão leve que fica no servidor (máx. 1024 px, qualidade 70): é a "qualidade reduzida".
///   • "thumb" — o ícone/avatar (256x256 recortado ao centro, qualidade 90): fica nítido nas telas.
/// O arquivo original NUNCA é guardado: assim o servidor não enche de fotos de 5 MB e os metadados
/// (localização do GPS, modelo do celular etc.) são descartados.
/// Não usa banco de dados: os arquivos se chamam {idDoUsuario}.full.webp / .thumb.webp.
/// </summary>
public sealed class ImageStorageService
{
    public const long MaxUploadBytes = 8 * 1024 * 1024;   // 8 MB aceitos no envio
    private const int MaxFullSide = 1024;
    private const int ThumbSide = 256;
    private const long MaxPixels = 2_000_000;             // limite adequado para avatar e para reduzir o pico de memória

    private static readonly string[] AllowedFormats = ["JPEG", "PNG", "WEBP", "GIF"];
    private static readonly SemaphoreSlim ProcessingGate = new(1, 1);

    private readonly string _root;

    public ImageStorageService(IWebHostEnvironment env)
    {
        // App_Data já está no .gitignore, então as fotos nunca vão para o repositório.
        _root = Path.Combine(env.ContentRootPath, "App_Data", "uploads", "avatars");
        Directory.CreateDirectory(_root);
    }

    public string FullPath(Guid userId) => Path.Combine(_root, $"{userId:N}.full.webp");
    public string ThumbPath(Guid userId) => Path.Combine(_root, $"{userId:N}.thumb.webp");

    public bool HasAvatar(Guid userId) => File.Exists(ThumbPath(userId));

    /// <summary>Número que muda quando a foto muda (usado em ?v= para o navegador atualizar o cache).</summary>
    public long Version(Guid userId) =>
        HasAvatar(userId) ? File.GetLastWriteTimeUtc(ThumbPath(userId)).Ticks : 0;

    public async Task<AvatarResult> SaveAvatarAsync(Guid userId, Stream input, CancellationToken ct = default)
    {
        await ProcessingGate.WaitAsync(ct);
        try
        {
            return await SaveAvatarCoreAsync(userId, input, ct);
        }
        finally
        {
            ProcessingGate.Release();
        }
    }

    private async Task<AvatarResult> SaveAvatarCoreAsync(Guid userId, Stream input, CancellationToken ct)
    {
        // 1. Ler tudo para a memória (já limitado a 8 MB por quem abriu o stream).
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, ct);
        var originalBytes = buffer.Length;
        if (originalBytes == 0)
            throw new ArgumentException("O arquivo está vazio.");

        // 2. Identificar SEM decodificar: confere o formato real pelo conteúdo (não pela extensão)
        //    e o tamanho em pixels antes de gastar memória.
        buffer.Position = 0;
        ImageInfo info;
        try
        {
            info = await Image.IdentifyAsync(buffer, ct);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            throw new ArgumentException("Esse arquivo não é uma imagem válida. Envie JPG, PNG, WebP ou GIF.");
        }

        var format = info.Metadata.DecodedImageFormat?.Name ?? "";
        if (!AllowedFormats.Contains(format, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Formato não aceito. Envie JPG, PNG, WebP ou GIF.");
        if ((long)info.Width * info.Height > MaxPixels)
            throw new ArgumentException("A resolução da imagem excede o limite de 2 megapixels. Reduza a resolução e tente novamente.");

        // 3. Decodificar, corrigir a rotação do celular e remover metadados.
        buffer.Position = 0;
        byte[] fullBytes, thumbBytes;
        try
        {
            using var image = await Image.LoadAsync(buffer, ct);

            // GIF animado: ficamos só com o primeiro quadro.
            while (image.Frames.Count > 1)
                image.Frames.RemoveFrame(image.Frames.Count - 1);

            image.Mutate(x => x.AutoOrient());
            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;

            // 4a. Reduz a imagem original em memória antes de criar outras versões.
            if (image.Width > MaxFullSide || image.Height > MaxFullSide)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxFullSide, MaxFullSide)
                }));
            }

            using (var ms = new MemoryStream())
            {
                await image.SaveAsync(ms, new WebpEncoder { Quality = 70 }, ct);
                fullBytes = ms.ToArray();
            }

            // 4b. Ícone: quadrado 256x256, recortado no centro, com qualidade alta.
            // A miniatura parte da imagem já reduzida para limitar o uso de memória.
            using (var thumb = image.Clone(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Crop,
                Size = new Size(ThumbSide, ThumbSide),
                Position = AnchorPositionMode.Center
            })))
            {
                using var ms = new MemoryStream();
                await thumb.SaveAsync(ms, new WebpEncoder { Quality = 90 }, ct);
                thumbBytes = ms.ToArray();
            }
        }
        catch (Exception ex) when (ex is InvalidImageContentException or NotSupportedException)
        {
            throw new ArgumentException("Não foi possível ler essa imagem. Tente outro arquivo.");
        }

        // 5. Gravar com segurança: escreve em arquivo temporário e troca, para nunca ficar meio gravado.
        await WriteAtomicAsync(FullPath(userId), fullBytes, ct);
        await WriteAtomicAsync(ThumbPath(userId), thumbBytes, ct);

        return new AvatarResult(originalBytes, fullBytes.Length, thumbBytes.Length);
    }

    public void DeleteAvatar(Guid userId)
    {
        foreach (var path in new[] { FullPath(userId), ThumbPath(userId) })
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static async Task WriteAtomicAsync(string path, byte[] bytes, CancellationToken ct)
    {
        var temp = path + ".tmp";
        await File.WriteAllBytesAsync(temp, bytes, ct);
        File.Move(temp, path, overwrite: true);
    }
}
