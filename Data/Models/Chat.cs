using LiteDB;

namespace MsgBox.Data.Models;

public class Chat
{
    [BsonId]
    public string Id { get; set; } = "";

    public string? ChatName { get; set; }
    public string ChatType { get; set; } = "person";
    public List<string> PersonIds { get; set; } = new();
    public DateTime CreatedUtc { get; set; }
}
