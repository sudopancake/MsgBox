using LiteDB;
using MsgBox.Data;
using MsgBox.Data.Migrations;
using MsgBox.Data.Models;

namespace MsgBox.Services;

public class DatabaseInitializer
{
    private readonly LiteDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly MigrationRunner _migrations;

    public DatabaseInitializer(LiteDbContext context, IWebHostEnvironment env, MigrationRunner migrations)
    {
        _context = context;
        _env = env;
        _migrations = migrations;
    }

    public void EnsureFilesystem()
    {
        Directory.CreateDirectory(Path.Combine(_env.ContentRootPath, "Data"));
        Directory.CreateDirectory(Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "avatars"));
        Directory.CreateDirectory(Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "images"));
        Directory.CreateDirectory(Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "attachments"));
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
}
