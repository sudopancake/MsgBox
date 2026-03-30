using LiteDB;

namespace MsgBox.Data.Models;

public class AppliedMigrationRecord
{
    [BsonId]
    public string Id { get; set; } = "";

    public DateTime AppliedUtc { get; set; }
}
