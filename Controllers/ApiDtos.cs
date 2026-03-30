namespace MsgBox.Controllers;

public class PersonResponseDto
{
    public string Id { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string ForeColor { get; set; } = "";
    public string BackColor { get; set; } = "";
    public string? AvatarPath { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class ChatSidebarDto
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string ChatType { get; set; } = "";
    public string? LatestMessageText { get; set; }
    public DateTime? LatestMessageUtc { get; set; }
}

public class ChatDetailResponseDto
{
    public string Id { get; set; } = "";
    public string? ChatName { get; set; }
    public string ChatType { get; set; } = "";
    public List<string> PersonIds { get; set; } = new();
    public DateTime CreatedUtc { get; set; }
    public List<PersonResponseDto> People { get; set; } = new();
}

public class MessageAuthorDto
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string ForeColor { get; set; } = "";
    public string BackColor { get; set; } = "";
    public string? AvatarPath { get; set; }
}

public class MessageFileDto
{
    public string FileName { get; set; } = "";
    public string Path { get; set; } = "";
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
}

public class ReactionDto
{
    public string Emoji { get; set; } = "";
    public int Count { get; set; }
    public List<string> PeopleNames { get; set; } = new();
    public string TooltipText { get; set; } = "";
}

public class MessageUiDto
{
    public string Id { get; set; } = "";
    public string ChatId { get; set; } = "";
    public DateTime SentUtc { get; set; }
    public string Text { get; set; } = "";
    public MessageAuthorDto Author { get; set; } = new();
    public List<MessageFileDto> ImageFiles { get; set; } = new();
    public List<MessageFileDto> Attachments { get; set; } = new();
    public List<ReactionDto> Reactions { get; set; } = new();
}

public class ThemeResponseDto
{
    public bool IsDark { get; set; }
    public string AccentColor { get; set; } = "blue";
}

public class ThemeUpdateDto
{
    public bool IsDark { get; set; }
    public string AccentColor { get; set; } = "blue";
}

public class CreateChatRequestDto
{
    public string? ChatName { get; set; }
    public string ChatType { get; set; } = "person";
    public List<string> PersonIds { get; set; } = new();
}

public class MessageReactionInputDto
{
    public string Emoji { get; set; } = "";
    public List<string> PersonIds { get; set; } = new();
}

public class BulkMessageItemDto
{
    public string ChatId { get; set; } = "";
    public string AuthorPersonId { get; set; } = "";
    public DateTime SentUtc { get; set; }
    public string Text { get; set; } = "";
    public List<MessageFileDto>? ImageFiles { get; set; }
    public List<MessageFileDto>? Attachments { get; set; }
    public List<MessageReactionInputDto>? Reactions { get; set; }
}
