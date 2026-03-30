using LiteDB;

namespace MsgBox.Data.Models;

public class Message
{
    [BsonId]
    public string Id { get; set; } = "";

    public string ChatId { get; set; } = "";
    public string AuthorPersonId { get; set; } = "";
    public DateTime SentUtc { get; set; }
    public string Text { get; set; } = "";
    public List<MessageFile> ImageFiles { get; set; } = new();
    public List<MessageFile> Attachments { get; set; } = new();
    public List<MessageReaction> Reactions { get; set; } = new();
}
