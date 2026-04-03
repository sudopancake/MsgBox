using Microsoft.AspNetCore.Mvc;
using MsgBox.Data.Models;
using MsgBox.Data.Repositories;
using MsgBox.Services;

namespace MsgBox.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PeopleController : ControllerBase
{
    private readonly PersonRepository _people;
    private readonly UploadStorage _uploads;

    public PeopleController(PersonRepository people, UploadStorage uploads)
    {
        _people = people;
        _uploads = uploads;
    }

    [HttpGet]
    public ActionResult<List<PersonResponseDto>> GetAll()
    {
        var list = _people.GetAll().Select(ToDto).ToList();
        return list;
    }

    [HttpGet("{id}")]
    public ActionResult<PersonResponseDto> GetById(string id)
    {
        var p = _people.GetById(id);
        if (p == null)
            return NotFound();
        return ToDto(p);
    }

    [HttpPost]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<PersonResponseDto>> Create(
        [FromForm] string? firstName,
        [FromForm] string? lastName,
        [FromForm] string? displayName,
        [FromForm] string? foreColor,
        [FromForm] string? backColor,
        IFormFile? avatar,
        CancellationToken ct)
    {
        var fn = firstName?.Trim() ?? "";
        var ln = lastName?.Trim() ?? "";
        var dn = string.IsNullOrWhiteSpace(displayName) ? $"{fn} {ln}".Trim() : displayName.Trim();
        if (string.IsNullOrEmpty(dn))
            dn = "Unnamed";

        var now = DateTime.UtcNow;
        var person = new Person
        {
            Id = "person_" + Guid.NewGuid().ToString("n"),
            FirstName = fn,
            LastName = ln,
            DisplayName = dn,
            ForeColor = string.IsNullOrWhiteSpace(foreColor) ? "#ffffff" : foreColor.Trim(),
            BackColor = string.IsNullOrWhiteSpace(backColor) ? "#0d6efd" : backColor.Trim(),
            CreatedUtc = now,
            UpdatedUtc = now
        };

        if (avatar is { Length: > 0 })
        {
            var saved = await _uploads.SaveImageAsync(avatar, "avatars", ct);
            person.AvatarPath = saved.StorageKey;
            person.AvatarPathContentType = saved.ContentType;
        }

        _people.Insert(person);
        return CreatedAtAction(nameof(GetById), new { id = person.Id }, ToDto(person));
    }

    [HttpPut("{id}")]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<PersonResponseDto>> Update(
        string id,
        [FromForm] string? firstName,
        [FromForm] string? lastName,
        [FromForm] string? displayName,
        [FromForm] string? foreColor,
        [FromForm] string? backColor,
        IFormFile? avatar,
        CancellationToken ct)
    {
        var existing = _people.GetById(id);
        if (existing == null)
            return NotFound();

        if (firstName != null)
            existing.FirstName = firstName.Trim();
        if (lastName != null)
            existing.LastName = lastName.Trim();
        if (displayName != null)
            existing.DisplayName = string.IsNullOrWhiteSpace(displayName) ? $"{existing.FirstName} {existing.LastName}".Trim() : displayName.Trim();
        if (string.IsNullOrEmpty(existing.DisplayName))
            existing.DisplayName = "Unnamed";
        if (foreColor != null)
            existing.ForeColor = string.IsNullOrWhiteSpace(foreColor) ? existing.ForeColor : foreColor.Trim();
        if (backColor != null)
            existing.BackColor = string.IsNullOrWhiteSpace(backColor) ? existing.BackColor : backColor.Trim();

        if (avatar is { Length: > 0 })
        {
            var oldAvatar = existing.AvatarPath;
            var saved = await _uploads.SaveImageAsync(avatar, "avatars", ct);
            existing.AvatarPath = saved.StorageKey;
            existing.AvatarPathContentType = saved.ContentType;
            if (!string.IsNullOrWhiteSpace(oldAvatar))
                _uploads.TryDeleteUpload(oldAvatar);
        }

        existing.UpdatedUtc = DateTime.UtcNow;
        _people.Update(existing);
        return ToDto(existing);
    }

    private PersonResponseDto ToDto(Person p) => new()
    {
        Id = p.Id,
        FirstName = p.FirstName,
        LastName = p.LastName,
        DisplayName = p.DisplayName,
        ForeColor = p.ForeColor,
        BackColor = p.BackColor,
        AvatarPath = string.IsNullOrWhiteSpace(p.AvatarPath) ? null : _uploads.GetInlineImageUrl(p.AvatarPath),
        AvatarStorageKey = UploadStorage.NormalizeStorageKey(p.AvatarPath),
        CreatedUtc = p.CreatedUtc,
        UpdatedUtc = p.UpdatedUtc
    };
}
