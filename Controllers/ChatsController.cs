using Microsoft.AspNetCore.Mvc;
using MsgBox.Data.Models;
using MsgBox.Data.Repositories;
using MsgBox.Services;

namespace MsgBox.Controllers;

[ApiController]
[Route("api/[controller]")]
[IgnoreAntiforgeryToken]
public class ChatsController : ControllerBase
{
    private readonly ChatRepository _chats;
    private readonly PersonRepository _people;
    private readonly MessageRepository _messages;

    public ChatsController(ChatRepository chats, PersonRepository people, MessageRepository messages)
    {
        _chats = chats;
        _people = people;
        _messages = messages;
    }

    [HttpGet]
    public ActionResult<List<ChatSidebarDto>> GetAll([FromQuery] string? forPersonId = null)
    {
        if (string.IsNullOrWhiteSpace(forPersonId))
            forPersonId = null;

        var allChats = _chats.GetAll();
        var allPeople = _people.GetAll();
        var map = allPeople.ToDictionary(p => p.Id);

        var rows = new List<ChatSidebarDto>();
        foreach (var c in allChats)
        {
            var (preview, latestUtc) = _messages.GetLatestForChat(c.Id);
            rows.Add(new ChatSidebarDto
            {
                Id = c.Id,
                DisplayName = ChatDisplayNameHelper.Resolve(c, map, forPersonId),
                ChatType = c.ChatType,
                LatestMessageText = preview,
                LatestMessageUtc = latestUtc
            });
        }

        return rows;
    }

    [HttpGet("{id}")]
    public ActionResult<ChatDetailResponseDto> GetById(string id)
    {
        var dto = BuildDetail(id);
        if (dto == null)
            return NotFound();
        return dto;
    }

    [HttpPost]
    public ActionResult<ChatDetailResponseDto> Create([FromBody] CreateChatRequestDto body)
    {
        if (body.PersonIds == null || body.PersonIds.Count == 0)
            return BadRequest("At least one person is required.");

        var distinct = body.PersonIds.Distinct().ToList();
        var chat = new Chat
        {
            Id = "chat_" + Guid.NewGuid().ToString("n"),
            ChatName = string.IsNullOrWhiteSpace(body.ChatName) ? null : body.ChatName.Trim(),
            ChatType = string.IsNullOrWhiteSpace(body.ChatType) ? "person" : body.ChatType.Trim(),
            PersonIds = distinct,
            CreatedUtc = DateTime.UtcNow
        };

        _chats.Insert(chat);
        var detail = BuildDetail(chat.Id);
        return CreatedAtAction(nameof(GetById), new { id = chat.Id }, detail);
    }

    private ChatDetailResponseDto? BuildDetail(string id)
    {
        var chat = _chats.GetById(id);
        if (chat == null)
            return null;

        var peopleMap = _people.GetByIds(chat.PersonIds);
        var people = chat.PersonIds
            .Where(peopleMap.ContainsKey)
            .Select(pid => peopleMap[pid])
            .Select(p => new PersonResponseDto
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                DisplayName = p.DisplayName,
                ForeColor = p.ForeColor,
                BackColor = p.BackColor,
                AvatarPath = p.AvatarPath,
                CreatedUtc = p.CreatedUtc,
                UpdatedUtc = p.UpdatedUtc
            })
            .ToList();

        return new ChatDetailResponseDto
        {
            Id = chat.Id,
            ChatName = chat.ChatName,
            ChatType = chat.ChatType,
            PersonIds = chat.PersonIds,
            CreatedUtc = chat.CreatedUtc,
            People = people
        };
    }
}
