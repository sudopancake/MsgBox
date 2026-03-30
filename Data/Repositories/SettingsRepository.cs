using MsgBox.Data.Models;

namespace MsgBox.Data.Repositories;

public class SettingsRepository
{
    private readonly LiteDbContext _db;

    public SettingsRepository(LiteDbContext db) => _db = db;

    public AppSettings GetOrCreate()
    {
        using var database = _db.Open();
        var col = database.GetCollection<AppSettings>("app_settings");
        var existing = col.FindById("app_settings");
        if (existing != null)
            return existing;

        var created = new AppSettings();
        col.Insert(created);
        return created;
    }

    public void UpsertTheme(ThemeSettings theme)
    {
        using var database = _db.Open();
        var col = database.GetCollection<AppSettings>("app_settings");
        var existing = col.FindById("app_settings");
        if (existing == null)
        {
            col.Insert(new AppSettings { Theme = theme });
            return;
        }

        existing.Theme = theme;
        col.Update(existing);
    }
}
