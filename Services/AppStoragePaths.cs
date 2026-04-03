namespace MsgBox.Services;

public class AppStoragePaths
{
    public AppStoragePaths(IWebHostEnvironment env)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Root = Path.Combine(localAppData, "MsgBox");
        DataRoot = Path.Combine(Root, "data");
        UploadsRoot = Path.Combine(Root, "uploads");
        AvatarsRoot = Path.Combine(UploadsRoot, "avatars");
        ImagesRoot = Path.Combine(UploadsRoot, "images");
        AttachmentsRoot = Path.Combine(UploadsRoot, "attachments");
        DatabasePath = Path.Combine(DataRoot, "msgbox.db");
        LegacyDatabasePath = Path.Combine(env.ContentRootPath, "Data", "msgbox.db");
        LegacyUploadsRoot = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
    }

    public string Root { get; }
    public string DataRoot { get; }
    public string UploadsRoot { get; }
    public string AvatarsRoot { get; }
    public string ImagesRoot { get; }
    public string AttachmentsRoot { get; }
    public string DatabasePath { get; }
    public string LegacyDatabasePath { get; }
    public string LegacyUploadsRoot { get; }
}
