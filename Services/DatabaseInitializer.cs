using LiteDB;
using MsgBox.Data;
using MsgBox.Data.Migrations;
using MsgBox.Data.Models;

namespace MsgBox.Services;

public class DatabaseInitializer
{
    private readonly LiteDbContext _context;
    private readonly MigrationRunner _migrations;
    private readonly AppStoragePaths _paths;

    public DatabaseInitializer(LiteDbContext context, AppStoragePaths paths, MigrationRunner migrations)
    {
        _context = context;
        _paths = paths;
        _migrations = migrations;
    }

    public void EnsureFilesystem()
    {
        Directory.CreateDirectory(_paths.Root);
        Directory.CreateDirectory(_paths.DataRoot);
        Directory.CreateDirectory(_paths.UploadsRoot);
        Directory.CreateDirectory(_paths.AvatarsRoot);
        Directory.CreateDirectory(_paths.ImagesRoot);
        Directory.CreateDirectory(_paths.AttachmentsRoot);
        MigrateLegacyDataIfNeeded();
    }

    public void EnsureIndexesAndMigrate()
    {
        using var db = _context.Open();

        var people = db.GetCollection<Person>("people");
        people.EnsureIndex(x => x.DisplayName);

        var chats = db.GetCollection<Chat>("chats");
        chats.EnsureIndex(x => x.ChatType);

        var messages = db.GetCollection<Message>("messages");
        messages.EnsureIndex(x => x.ChatId);
        messages.EnsureIndex(x => x.SentUtc);
        messages.EnsureIndex(x => x.AuthorPersonId);

        db.GetCollection<AppSettings>("app_settings");

        _migrations.ApplyPending(db);
    }

    private void MigrateLegacyDataIfNeeded()
    {
        if (!File.Exists(_paths.DatabasePath) && File.Exists(_paths.LegacyDatabasePath))
            File.Copy(_paths.LegacyDatabasePath, _paths.DatabasePath);

        CopyLegacyFolderIfMissing("avatars", _paths.AvatarsRoot);
        CopyLegacyFolderIfMissing("images", _paths.ImagesRoot);
        CopyLegacyFolderIfMissing("attachments", _paths.AttachmentsRoot);
    }

    private void CopyLegacyFolderIfMissing(string folderName, string destinationRoot)
    {
        if (Directory.EnumerateFileSystemEntries(destinationRoot).Any())
            return;

        var legacyRoot = Path.Combine(_paths.LegacyUploadsRoot, folderName);
        if (!Directory.Exists(legacyRoot))
            return;

        foreach (var file in Directory.EnumerateFiles(legacyRoot))
        {
            var dest = Path.Combine(destinationRoot, Path.GetFileName(file));
            if (!File.Exists(dest))
                File.Copy(file, dest);
        }
    }
}
