namespace MsgBox.Services.Import;

/// <summary>
/// Future hook for alternate bulk JSON formats (Discord export, Slack, etc.).
/// The default pipeline uses <see cref="MessageBulkImportService"/> with <see cref="Controllers.BulkImportJsonRowDto"/>.
/// </summary>
public interface IBulkImportJsonAdapter
{
    string AdapterId { get; }
}
