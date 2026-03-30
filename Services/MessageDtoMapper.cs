using MsgBox.Controllers;
using MsgBox.Data.Models;
using MsgBox.Data.Repositories;

namespace MsgBox.Services;

public class MessageDtoMapper
{
    private readonly PersonRepository _people;

    public MessageDtoMapper(PersonRepository people) => _people = people;

    public List<MessageUiDto> ToUiDtos(IEnumerable<Message> messages)
    {
        var ids = messages.SelectMany(m => new[] { m.AuthorPersonId }
            .Concat(m.Reactions.SelectMany(r => r.PersonIds))).Distinct().ToList();
        var map = _people.GetByIds(ids);
        return messages.Select(m => ToUiDto(m, map)).ToList();
    }

    public MessageUiDto ToUiDto(Message m, Dictionary<string, Person>? peopleMap = null)
    {
        peopleMap ??= _people.GetByIds(new[] { m.AuthorPersonId }.Concat(m.Reactions.SelectMany(r => r.PersonIds)));
        peopleMap.TryGetValue(m.AuthorPersonId, out var author);
        author ??= new Person { Id = m.AuthorPersonId, DisplayName = m.AuthorPersonId, ForeColor = "#fff", BackColor = "#6c757d" };

        return new MessageUiDto
        {
            Id = m.Id,
            ChatId = m.ChatId,
            SentUtc = m.SentUtc,
            Text = m.Text,
            Author = new MessageAuthorDto
            {
                Id = author.Id,
                DisplayName = author.DisplayName,
                ForeColor = author.ForeColor,
                BackColor = author.BackColor,
                AvatarPath = author.AvatarPath
            },
            ImageFiles = m.ImageFiles.Select(f => new MessageFileDto
            {
                FileName = f.FileName,
                Path = f.Path,
                ContentType = f.ContentType,
                SizeBytes = f.SizeBytes
            }).ToList(),
            Attachments = m.Attachments.Select(f => new MessageFileDto
            {
                FileName = f.FileName,
                Path = f.Path,
                ContentType = f.ContentType,
                SizeBytes = f.SizeBytes
            }).ToList(),
            Reactions = m.Reactions.Select(r => ToReactionDto(r, peopleMap)).ToList()
        };
    }

    private static ReactionDto ToReactionDto(MessageReaction r, Dictionary<string, Person> peopleMap)
    {
        var distinctIds = r.PersonIds.Distinct().ToList();
        var names = distinctIds
            .Select(id => peopleMap.TryGetValue(id, out var p) ? p.DisplayName : id)
            .ToList();
        var tooltip = string.Join(", ", names);
        return new ReactionDto
        {
            Emoji = r.Emoji,
            Count = distinctIds.Count,
            PeopleNames = names,
            TooltipText = tooltip
        };
    }
}
