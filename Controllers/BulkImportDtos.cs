using System.Text.Json.Serialization;

namespace MsgBox.Controllers;

public class BulkImportJsonRowDto
{
    [JsonPropertyName("by")]
    public string? By { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("emojis")]
    public List<BulkImportJsonEmojiDto>? Emojis { get; set; }

    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }

    [JsonPropertyName("attachments")]
    public List<string>? Attachments { get; set; }
}

public class BulkImportJsonEmojiDto
{
    [JsonPropertyName("emoji")]
    public string? Emoji { get; set; }

    [JsonPropertyName("bys")]
    public List<string>? Bys { get; set; }
}

public class BulkImportPreviewRequest
{
    public string ChatId { get; set; } = "";
    public string Json { get; set; } = "";
}

public class BulkImportCommitRequest
{
    public string ChatId { get; set; } = "";
    public string Json { get; set; } = "";
}

public class BulkImportPreviewResponse
{
    public bool ParseOk { get; set; }
    public string? ParseError { get; set; }
    public int TotalRows { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }
    public List<BulkImportPreviewRowDto> Rows { get; set; } = new();
}

public class BulkImportPreviewRowDto
{
    public int Index { get; set; }
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public BulkImportRowPreviewFields? Preview { get; set; }
}

public class BulkImportRowPreviewFields
{
    public string By { get; set; } = "";
    public string Date { get; set; } = "";
    public string MessageSnippet { get; set; } = "";
    public string ReactionSummary { get; set; } = "";
}

public class BulkImportCommitResponse
{
    public int ImportedCount { get; set; }
    public int FailedCount { get; set; }
    public List<BulkImportFailureDto> Failures { get; set; } = new();
    public List<string> ImportedIds { get; set; } = new();
}

public class BulkImportFailureDto
{
    public int Index { get; set; }
    public List<string> Reasons { get; set; } = new();
}
