namespace MsgBox.Data.Models;

public class MessageReaction
{
    public string Emoji { get; set; } = "";
    public List<string> PersonIds { get; set; } = new();
}
