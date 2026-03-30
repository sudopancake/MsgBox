namespace MsgBox.Data;

public class LiteDbOptions
{
    public const string SectionName = "LiteDb";

    public string ConnectionString { get; set; } = "Filename=Data/msgbox.db;Connection=shared";
}
