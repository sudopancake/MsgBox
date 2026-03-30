using LiteDB;

namespace MsgBox.Data.Models;

public class Person
{
    [BsonId]
    public string Id { get; set; } = "";

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string ForeColor { get; set; } = "#ffffff";
    public string BackColor { get; set; } = "#0d6efd";
    public string? AvatarPath { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
