namespace MsgBox.Data;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>When true, runs the SeedDemoData migration on an empty database.</summary>
    public bool RunSeedDemoData { get; set; }
}
