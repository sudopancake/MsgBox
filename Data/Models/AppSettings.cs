using LiteDB;

namespace MsgBox.Data.Models;

public class AppSettings
{
    [BsonId]
    public string Id { get; set; } = "app_settings";

    public ThemeSettings Theme { get; set; } = new();
}
