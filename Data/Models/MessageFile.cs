namespace MsgBox.Data.Models;

public class MessageFile
{
    public string FileName { get; set; } = "";
    public string Path { get; set; } = "";
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
}
