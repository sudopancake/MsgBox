namespace MsgBox.Services;

public class UploadStorage
{
    private readonly IWebHostEnvironment _env;

    public UploadStorage(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveFileAsync(IFormFile file, string subfolder, CancellationToken ct = default)
    {
        var root = _env.WebRootPath ?? throw new InvalidOperationException("WebRootPath not set.");
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext))
            ext = ".bin";
        var name = Guid.NewGuid().ToString("n") + ext;
        var dir = Path.Combine(root, "uploads", subfolder);
        Directory.CreateDirectory(dir);
        var physical = Path.Combine(dir, name);
        await using (var stream = File.Create(physical))
        {
            await file.CopyToAsync(stream, ct);
        }

        return "/uploads/" + subfolder + "/" + name;
    }

    /// <summary>
    /// Deletes a file under wwwroot if <paramref name="webPath"/> is a safe /uploads/... path.
    /// </summary>
    public bool TryDeleteUpload(string webPath)
    {
        var root = _env.WebRootPath ?? throw new InvalidOperationException("WebRootPath not set.");
        var normalized = webPath.Replace('\\', '/').TrimStart('/');
        if (!normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            return false;

        var full = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var rootFull = Path.GetFullPath(root);
        if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!File.Exists(full))
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
}
