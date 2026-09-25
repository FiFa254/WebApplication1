namespace DevFolio.Infrastructure;

/// <summary>
/// Stores profile images on disk and serves them under /uploads.
/// The folder comes from Storage:UploadsPath (a Docker volume in production) and defaults to wwwroot/uploads.
/// </summary>
public class ImageStorage
{
    public const long MaxImageBytes = 2 * 1024 * 1024;
    public const string RequestPath = "/uploads";

    private const string ProfileFolder = "profiles";

    private readonly ILogger<ImageStorage> _logger;

    public ImageStorage(IConfiguration configuration, IWebHostEnvironment environment, ILogger<ImageStorage> logger)
    {
        _logger = logger;

        var configured = configuration["Storage:UploadsPath"];
        RootPath = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads")
            : Path.GetFullPath(configured);

        Directory.CreateDirectory(Path.Combine(RootPath, ProfileFolder));
    }

    public string RootPath { get; }

    /// <summary>
    /// Validates the file by its content (JPEG, PNG or WebP signature) and saves it.
    /// Returns the public URL, or an error message when the file is rejected.
    /// </summary>
    public async Task<(string? Url, string? Error)> SaveProfileImageAsync(IFormFile image, CancellationToken cancellationToken = default)
    {
        if (image.Length == 0)
        {
            return (null, "The image file is empty.");
        }

        if (image.Length > MaxImageBytes)
        {
            return (null, "Profile image must not exceed 2 MB.");
        }

        var header = new byte[12];
        await using (var peek = image.OpenReadStream())
        {
            var read = await peek.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
            if (read < header.Length)
            {
                return (null, "Profile image must be a JPG, PNG, or WebP file.");
            }
        }

        var extension = DetectExtension(header);
        if (extension is null)
        {
            return (null, "Profile image must be a JPG, PNG, or WebP file.");
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(RootPath, ProfileFolder, fileName);

        await using (var target = File.Create(filePath))
        await using (var source = image.OpenReadStream())
        {
            await source.CopyToAsync(target, cancellationToken);
        }

        return ($"{RequestPath}/{ProfileFolder}/{fileName}", null);
    }

    /// <summary>Deletes an image previously returned by <see cref="SaveProfileImageAsync"/>. Unknown paths are ignored.</summary>
    public void Delete(string? url)
    {
        var prefix = $"{RequestPath}/{ProfileFolder}/";
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        // Only a bare file name is accepted, so the path can never leave the uploads folder.
        var fileName = url[prefix.Length..];
        if (fileName.Length == 0 || fileName != Path.GetFileName(fileName))
        {
            return;
        }

        try
        {
            File.Delete(Path.Combine(RootPath, ProfileFolder, fileName));
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not delete image {Url}.", url);
        }
    }

    public static string? DetectExtension(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ".jpg";
        }

        ReadOnlySpan<byte> png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (header.StartsWith(png))
        {
            return ".png";
        }

        if (header.Length >= 12
            && header[..4].SequenceEqual("RIFF"u8)
            && header[8..12].SequenceEqual("WEBP"u8))
        {
            return ".webp";
        }

        return null;
    }
}
