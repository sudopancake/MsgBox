using Microsoft.AspNetCore.Mvc;
using MsgBox.Data.Models;
using MsgBox.Data.Repositories;
using MsgBox.Services;
using System.Text.Json;

namespace MsgBox.Controllers;

[ApiController]
[Route("api/[controller]")]
[IgnoreAntiforgeryToken]
public class MessagesController : ControllerBase
{
    private readonly MessageRepository _messages;
    private readonly MessageDtoMapper _mapper;
    private readonly UploadStorage _uploads;
    private readonly MessageBulkImportService _bulkImport;

    public MessagesController(
        MessageRepository messages,
        MessageDtoMapper mapper,
        UploadStorage uploads,
        MessageBulkImportService bulkImport)
    {
        _messages = messages;
        _mapper = mapper;
        _uploads = uploads;
        _bulkImport = bulkImport;
    }

    [HttpGet("{id}")]
    public ActionResult<MessageUiDto> GetById(string id)
    {
        var message = _messages.GetById(id);
        if (message == null)
            return NotFound();
        return _mapper.ToUiDto(message);
    }

    [HttpGet("by-chat/{chatId}")]
    public ActionResult<List<MessageUiDto>> GetByChat(string chatId, [FromQuery] int take = 25, [FromQuery] string? beforeMessageId = null)
    {
        if (take < 1 || take > 200)
            take = 25;

        DateTime? olderThan = null;
        if (!string.IsNullOrEmpty(beforeMessageId))
        {
            var refMsg = _messages.GetById(beforeMessageId);
            if (refMsg == null || refMsg.ChatId != chatId)
                return BadRequest("Invalid beforeMessageId for this chat.");
            olderThan = refMsg.SentUtc;
        }

        var page = _messages.GetLatestPage(chatId, take, olderThan);
        return _mapper.ToUiDtos(page);
    }

    [HttpPost("import/preview")]
    public ActionResult<BulkImportPreviewResponse> ImportPreview([FromBody] BulkImportPreviewRequest body)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.ChatId))
            return BadRequest("chatId is required.");
        return _bulkImport.Preview(body.ChatId.Trim(), body.Json ?? "");
    }

    [HttpPost("import/commit")]
    public ActionResult<BulkImportCommitResponse> ImportCommit([FromBody] BulkImportCommitRequest body)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.ChatId))
            return BadRequest("chatId is required.");
        return _bulkImport.Commit(body.ChatId.Trim(), body.Json ?? "");
    }

    [HttpPost]
    [RequestSizeLimit(104_857_600)]
    public async Task<ActionResult<MessageUiDto>> Create(
        [FromForm] string chatId,
        [FromForm] string authorPersonId,
        [FromForm] DateTime sentUtc,
        [FromForm] string? text,
        [FromForm] string? reactionsJson,
        [FromForm(Name = "images")] List<IFormFile>? images,
        [FromForm(Name = "attachments")] List<IFormFile>? attachments,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(chatId) || string.IsNullOrWhiteSpace(authorPersonId))
            return BadRequest("chatId and authorPersonId are required.");

        images ??= new List<IFormFile>();
        attachments ??= new List<IFormFile>();

        var message = new Message
        {
            Id = "msg_" + Guid.NewGuid().ToString("n"),
            ChatId = chatId.Trim(),
            AuthorPersonId = authorPersonId.Trim(),
            SentUtc = sentUtc.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(sentUtc, DateTimeKind.Utc) : sentUtc.ToUniversalTime(),
            Text = text ?? "",
            ImageFiles = new List<MessageFile>(),
            Attachments = new List<MessageFile>(),
            Reactions = ParseReactions(reactionsJson)
        };

        await AppendUploadedFilesAsync(message, images, attachments, ct);

        _messages.Insert(message);
        return _mapper.ToUiDto(message);
    }

    [HttpPut("{id}")]
    [RequestSizeLimit(104_857_600)]
    public async Task<ActionResult<MessageUiDto>> Update(
        string id,
        [FromForm] string chatId,
        [FromForm] string authorPersonId,
        [FromForm] DateTime sentUtc,
        [FromForm] string? text,
        [FromForm] string? reactionsJson,
        [FromForm] string? removeImagePathsJson,
        [FromForm] string? removeAttachmentPathsJson,
        [FromForm(Name = "images")] List<IFormFile>? images,
        [FromForm(Name = "attachments")] List<IFormFile>? attachments,
        CancellationToken ct)
    {
        var existing = _messages.GetById(id);
        if (existing == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(chatId) || string.IsNullOrWhiteSpace(authorPersonId))
            return BadRequest("chatId and authorPersonId are required.");

        if (!string.Equals(existing.ChatId.Trim(), chatId.Trim(), StringComparison.Ordinal))
            return BadRequest("chatId does not match this message.");

        images ??= new List<IFormFile>();
        attachments ??= new List<IFormFile>();

        foreach (var path in ParsePathList(removeImagePathsJson))
        {
            var file = existing.ImageFiles.FirstOrDefault(f => f.Path == path);
            if (file != null)
            {
                existing.ImageFiles.Remove(file);
                _uploads.TryDeleteUpload(path);
            }
        }

        foreach (var path in ParsePathList(removeAttachmentPathsJson))
        {
            var file = existing.Attachments.FirstOrDefault(f => f.Path == path);
            if (file != null)
            {
                existing.Attachments.Remove(file);
                _uploads.TryDeleteUpload(path);
            }
        }

        existing.AuthorPersonId = authorPersonId.Trim();
        existing.SentUtc = sentUtc.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(sentUtc, DateTimeKind.Utc) : sentUtc.ToUniversalTime();
        existing.Text = text ?? "";
        existing.Reactions = ParseReactions(reactionsJson);

        await AppendUploadedFilesAsync(existing, images, attachments, ct);

        _messages.Update(existing);
        return _mapper.ToUiDto(existing);
    }

    [HttpPost("bulk")]
    public ActionResult<List<MessageUiDto>> Bulk([FromBody] List<BulkMessageItemDto> items)
    {
        if (items == null || items.Count == 0)
            return BadRequest("No messages.");

        var entities = new List<Message>();
        foreach (var item in items)
        {
            var sent = item.SentUtc.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(item.SentUtc, DateTimeKind.Utc)
                : item.SentUtc.ToUniversalTime();

            var reactions = new List<MessageReaction>();
            if (item.Reactions != null)
            {
                foreach (var r in item.Reactions)
                {
                    reactions.Add(new MessageReaction
                    {
                        Emoji = r.Emoji,
                        PersonIds = r.PersonIds.Distinct().ToList()
                    });
                }
            }

            entities.Add(new Message
            {
                Id = "msg_" + Guid.NewGuid().ToString("n"),
                ChatId = item.ChatId,
                AuthorPersonId = item.AuthorPersonId,
                SentUtc = sent,
                Text = item.Text ?? "",
                ImageFiles = item.ImageFiles?.Select(f => new MessageFile
                {
                    FileName = f.FileName,
                    Path = f.Path,
                    ContentType = f.ContentType,
                    SizeBytes = f.SizeBytes
                }).ToList() ?? new List<MessageFile>(),
                Attachments = item.Attachments?.Select(f => new MessageFile
                {
                    FileName = f.FileName,
                    Path = f.Path,
                    ContentType = f.ContentType,
                    SizeBytes = f.SizeBytes
                }).ToList() ?? new List<MessageFile>(),
                Reactions = reactions
            });
        }

        _messages.InsertMany(entities);
        return _mapper.ToUiDtos(entities);
    }

    private async Task AppendUploadedFilesAsync(Message message, List<IFormFile> images, List<IFormFile> attachments, CancellationToken ct)
    {
        foreach (var file in images.Where(f => f.Length > 0))
        {
            var path = await _uploads.SaveFileAsync(file, "images", ct);
            message.ImageFiles.Add(new MessageFile
            {
                FileName = file.FileName,
                Path = path,
                ContentType = file.ContentType,
                SizeBytes = file.Length
            });
        }

        foreach (var file in attachments.Where(f => f.Length > 0))
        {
            var path = await _uploads.SaveFileAsync(file, "attachments", ct);
            message.Attachments.Add(new MessageFile
            {
                FileName = file.FileName,
                Path = path,
                ContentType = file.ContentType,
                SizeBytes = file.Length
            });
        }
    }

    private static List<string> ParsePathList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct().ToList()
                   ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static List<MessageReaction> ParseReactions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<MessageReaction>();

        try
        {
            var parsed = JsonSerializer.Deserialize<List<MessageReactionInputDto>>(json);
            if (parsed == null)
                return new List<MessageReaction>();
            return parsed
                .Where(r => !string.IsNullOrWhiteSpace(r.Emoji))
                .Select(r => new MessageReaction
                {
                    Emoji = r.Emoji.Trim(),
                    PersonIds = r.PersonIds.Distinct().ToList()
                })
                .ToList();
        }
        catch
        {
            return new List<MessageReaction>();
        }
    }
}
