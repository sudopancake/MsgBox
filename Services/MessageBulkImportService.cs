using System.Text.Json;
using MsgBox.Controllers;
using MsgBox.Data.Models;
using MsgBox.Data.Repositories;

namespace MsgBox.Services;

public class MessageBulkImportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly PersonRepository _people;
    private readonly ChatRepository _chats;
    private readonly MessageRepository _messages;

    public MessageBulkImportService(PersonRepository people, ChatRepository chats, MessageRepository messages)
    {
        _people = people;
        _chats = chats;
        _messages = messages;
    }

    public BulkImportPreviewResponse Preview(string chatId, string json)
    {
        var response = new BulkImportPreviewResponse();
        if (!TryParse(chatId, json, out var chat, out var rows, out var parseError))
        {
            response.ParseOk = false;
            response.ParseError = parseError;
            return response;
        }

        response.ParseOk = true;
        response.TotalRows = rows!.Count;
        for (var i = 0; i < rows.Count; i++)
        {
            var (previewRow, _) = ValidateRow(i, rows[i], chat!);
            response.Rows.Add(previewRow);
        }

        response.ValidCount = response.Rows.Count(r => r.IsValid);
        response.InvalidCount = response.Rows.Count(r => !r.IsValid);
        return response;
    }

    public BulkImportCommitResponse Commit(string chatId, string json)
    {
        var result = new BulkImportCommitResponse();
        if (!TryParse(chatId, json, out var chat, out var rows, out var parseError))
        {
            result.Failures.Add(new BulkImportFailureDto
            {
                Index = -1,
                Reasons = new List<string> { parseError ?? "Invalid input." }
            });
            result.FailedCount = 1;
            return result;
        }

        var validated = new List<(int Index, ValidatedImportRow Row)>();
        for (var i = 0; i < rows!.Count; i++)
        {
            var (previewRow, mapped) = ValidateRow(i, rows[i], chat!);
            if (mapped != null)
                validated.Add((i, mapped));
            else
            {
                result.Failures.Add(new BulkImportFailureDto
                {
                    Index = i,
                    Reasons = previewRow.Errors.ToList()
                });
            }
        }

        result.FailedCount = result.Failures.Count;

        var ordered = validated
            .OrderBy(x => x.Row.SentUtc)
            .ThenBy(x => x.Index)
            .ToList();

        foreach (var (_, row) in ordered)
        {
            var entity = new Message
            {
                Id = "msg_" + Guid.NewGuid().ToString("n"),
                ChatId = chat!.Id,
                AuthorPersonId = row.AuthorPersonId,
                SentUtc = row.SentUtc,
                Text = row.Text,
                ImageFiles = new List<MessageFile>(),
                Attachments = new List<MessageFile>(),
                Reactions = row.Reactions
            };
            _messages.Insert(entity);
            result.ImportedIds.Add(entity.Id);
        }

        result.ImportedCount = result.ImportedIds.Count;
        return result;
    }

    private bool TryParse(
        string chatId,
        string json,
        out Chat? chat,
        out List<BulkImportJsonRowDto>? rows,
        out string? parseError)
    {
        chat = null;
        rows = null;
        parseError = null;

        if (string.IsNullOrWhiteSpace(chatId))
        {
            parseError = "chatId is required.";
            return false;
        }

        chat = _chats.GetById(chatId.Trim());
        if (chat == null)
        {
            parseError = "Chat not found.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            parseError = "JSON is empty.";
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<BulkImportJsonRowDto>>(json.Trim(), JsonOptions);
            if (parsed == null)
            {
                parseError = "JSON must be an array of message objects.";
                return false;
            }

            rows = parsed;
        }
        catch (JsonException ex)
        {
            parseError = "Invalid JSON: " + ex.Message;
            return false;
        }

        return true;
    }

    private (BulkImportPreviewRowDto preview, ValidatedImportRow? row) ValidateRow(
        int index,
        BulkImportJsonRowDto source,
        Chat chat)
    {
        var errors = new List<string>();
        var preview = new BulkImportPreviewRowDto
        {
            Index = index,
            Errors = errors,
            Preview = new BulkImportRowPreviewFields()
        };

        if (source.Images is { Count: > 0 })
            errors.Add("Images in JSON bulk import are not supported yet; remove the \"images\" field or leave it empty.");
        if (source.Attachments is { Count: > 0 })
            errors.Add("Attachments in JSON bulk import are not supported yet; remove the \"attachments\" field or leave it empty.");

        var by = source.By?.Trim() ?? "";
        if (string.IsNullOrEmpty(by))
            errors.Add("Field \"by\" (author display name) is required.");

        var messageText = source.Message ?? "";
        if (string.IsNullOrWhiteSpace(messageText))
            errors.Add("Field \"message\" is required.");

        var dateStr = source.Date?.Trim() ?? "";
        if (string.IsNullOrEmpty(dateStr))
            errors.Add("Field \"date\" is required.");

        DateTime sentUtc = default;
        if (!string.IsNullOrEmpty(dateStr) && !TryParseImportDate(dateStr, out sentUtc))
            errors.Add($"Could not parse \"date\": \"{dateStr}\".");

        Person? authorPerson = null;
        if (!string.IsNullOrEmpty(by))
        {
            var matches = _people.FindByDisplayName(by);
            if (matches.Count == 0)
                errors.Add($"Unknown author display name: \"{by}\".");
            else if (matches.Count > 1)
                errors.Add($"Ambiguous author display name \"{by}\" ({matches.Count} people match).");
            else
                authorPerson = matches[0];

            if (authorPerson != null && !chat.PersonIds.Contains(authorPerson.Id))
                errors.Add($"Author \"{by}\" is not a member of this chat.");
        }

        var reactions = new List<MessageReaction>();
        if (source.Emojis != null)
        {
            for (var r = 0; r < source.Emojis.Count; r++)
            {
                var em = source.Emojis[r];
                var emoji = em.Emoji?.Trim() ?? "";
                if (string.IsNullOrEmpty(emoji))
                {
                    errors.Add($"emojis[{r}]: \"emoji\" is required.");
                    continue;
                }

                var personIds = new List<string>();
                if (em.Bys != null)
                {
                    foreach (var name in em.Bys)
                    {
                        var n = name?.Trim() ?? "";
                        if (string.IsNullOrEmpty(n))
                            continue;
                        var nameMatches = _people.FindByDisplayName(n);
                        if (nameMatches.Count == 0)
                            errors.Add($"emojis[{r}]: unknown person \"{n}\" in \"bys\".");
                        else if (nameMatches.Count > 1)
                            errors.Add($"emojis[{r}]: ambiguous name \"{n}\" ({nameMatches.Count} people).");
                        else
                        {
                            var p = nameMatches[0];
                            if (!chat.PersonIds.Contains(p.Id))
                                errors.Add($"emojis[{r}]: \"{n}\" is not in this chat.");
                            else
                                personIds.Add(p.Id);
                        }
                    }
                }

                reactions.Add(new MessageReaction
                {
                    Emoji = emoji,
                    PersonIds = personIds.Distinct().ToList()
                });
            }
        }

        preview.Preview!.By = by;
        preview.Preview.Date = string.IsNullOrEmpty(dateStr) ? "—" : dateStr;
        preview.Preview.MessageSnippet = messageText.Length > 80 ? messageText[..80] + "…" : messageText;
        preview.Preview.ReactionSummary = source.Emojis == null || source.Emojis.Count == 0
            ? "—"
            : string.Join("; ", reactions.Select(x => x.Emoji + "(" + x.PersonIds.Count + ")"));

        if (errors.Count > 0)
        {
            preview.IsValid = false;
            return (preview, null);
        }

        preview.IsValid = true;
        var row = new ValidatedImportRow
        {
            AuthorPersonId = authorPerson!.Id,
            SentUtc = sentUtc,
            Text = messageText,
            Reactions = reactions
        };
        return (preview, row);
    }

    private static bool TryParseImportDate(string dateStr, out DateTime utc)
    {
        utc = default;
        if (DateTime.TryParse(dateStr, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
        {
            utc = dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime();
            return true;
        }

        return false;
    }

    private sealed class ValidatedImportRow
    {
        public string AuthorPersonId { get; init; } = "";
        public DateTime SentUtc { get; init; }
        public string Text { get; init; } = "";
        public List<MessageReaction> Reactions { get; init; } = new();
    }
}
