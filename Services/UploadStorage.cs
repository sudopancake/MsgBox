using Microsoft.AspNetCore.StaticFiles;

namespace MsgBox.Services;

public class UploadStorage
{
    private static readonly HashSet<string> InlineSafeImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp"
    };

    private static readonly HashSet<string> AllowedSubfolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "avatars", "images", "attachments"
    };

    private readonly AppStoragePaths _paths;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    public UploadStorage(AppStoragePaths paths) => _paths = paths;

    public async Task<StoredUploadResult> SaveImageAsync(IFormFile file, string subfolder, CancellationToken ct = default)
        => await SaveFileAsync(file, subfolder, requireInlineSafeImage: true, ct);

    public async Task<StoredUploadResult> SaveAttachmentAsync(IFormFile file, CancellationToken ct = default)
        => await SaveFileAsync(file, "attachments", requireInlineSafeImage: false, ct);

    public string GetInlineImageUrl(string storageKey)
        => "/api/files/image?key=" + Uri.EscapeDataString(NormalizeStorageKey(storageKey));

    public string GetAttachmentUrl(string storageKey)
        => "/api/files/attachment?key=" + Uri.EscapeDataString(NormalizeStorageKey(storageKey));

    public string GetContentType(string storageKey, string? storedContentType = null)
    {
        var normalizedKey = NormalizeStorageKey(storageKey);
        var ext = Path.GetExtension(normalizedKey);
        if (!string.IsNullOrWhiteSpace(storedContentType) &&
            string.Equals(storedContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase) == false)
            return storedContentType;
        if (!string.IsNullOrEmpty(ext) && _contentTypeProvider.TryGetContentType("file" + ext, out var contentType))
            return contentType;
        return "application/octet-stream";
    }

    public bool IsInlineSafeImage(string storageKey)
    {
        var ext = Path.GetExtension(NormalizeStorageKey(storageKey));
        return !string.IsNullOrEmpty(ext) && InlineSafeImageExtensions.Contains(ext);
    }

    public bool TryResolvePhysicalPath(string storageKey, out string physicalPath)
    {
        physicalPath = "";
        var normalized = NormalizeStorageKey(storageKey);
        if (!IsValidStorageKey(normalized))
            return false;

        var full = Path.GetFullPath(Path.Combine(_paths.UploadsRoot, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var uploadsRoot = Path.GetFullPath(_paths.UploadsRoot);
        if (!full.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!File.Exists(full))
            return false;

        physicalPath = full;
        return true;
    }

    public bool TryDeleteUpload(string storageKey)
    {
        if (!TryResolvePhysicalPath(storageKey, out var full))
            return false;
        try
        {
            File.Delete(full);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string SanitizeDownloadFileName(string? fileName)
    {
        var cleaned = Path.GetFileName(fileName ?? "");
        return string.IsNullOrWhiteSpace(cleaned) ? "download.bin" : cleaned;
    }

    public static string NormalizeStorageKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var normalized = value.Trim().Replace('\\', '/').TrimStart('/');
        if (normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["uploads/".Length..];
        return normalized;
    }

    private async Task<StoredUploadResult> SaveFileAsync(
        IFormFile file,
        string subfolder,
        bool requireInlineSafeImage,
        CancellationToken ct)
    {
        if (!AllowedSubfolders.Contains(subfolder))
            throw new InvalidOperationException("Unsupported upload subfolder.");

        var sanitizedFileName = SanitizeDownloadFileName(file.FileName);
        var ext = Path.GetExtension(sanitizedFileName);
        if (string.IsNullOrEmpty(ext))
            ext = requireInlineSafeImage ? ".bin" : ".bin";
        if (requireInlineSafeImage && !InlineSafeImageExtensions.Contains(ext))
            throw new InvalidOperationException("Only PNG, JPEG, GIF, and WebP images are allowed.");

        var name = Guid.NewGuid().ToString("n") + ext.ToLowerInvariant();
        var relativeKey = subfolder + "/" + name;
        var dir = Path.Combine(_paths.UploadsRoot, subfolder);
        Directory.CreateDirectory(dir);
        var physical = Path.Combine(dir, name);

        await using (var stream = File.Create(physical))
        {
            await file.CopyToAsync(stream, ct);
        }

        return new StoredUploadResult
        {
            StorageKey = relativeKey,
            FileName = sanitizedFileName,
            ContentType = GetContentType(relativeKey, file.ContentType),
            SizeBytes = file.Length
        };
    }

    private static bool IsValidStorageKey(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Contains("..", StringComparison.Ordinal))
            return false;

        var slash = normalized.IndexOf('/');
        if (slash <= 0)
            return false;

        var folder = normalized[..slash];
        return AllowedSubfolders.Contains(folder);
    }
}

public class StoredUploadResult
{
    public string StorageKey { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
}
