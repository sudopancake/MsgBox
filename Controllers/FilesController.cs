using Microsoft.AspNetCore.Mvc;
using MsgBox.Data.Repositories;
using MsgBox.Services;

namespace MsgBox.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly UploadStorage _uploads;
    private readonly PersonRepository _people;
    private readonly MessageRepository _messages;

    public FilesController(UploadStorage uploads, PersonRepository people, MessageRepository messages)
    {
        _uploads = uploads;
        _people = people;
        _messages = messages;
    }

    [HttpGet("image")]
    public IActionResult GetImage([FromQuery] string key)
    {
        var normalizedKey = UploadStorage.NormalizeStorageKey(key);
        if (string.IsNullOrEmpty(normalizedKey))
            return BadRequest("key is required.");

        var avatarOwner = _people.FindByAvatarPath(normalizedKey);
        var imageFile = avatarOwner == null ? _messages.FindImageFile(normalizedKey) : null;
        if (avatarOwner == null && imageFile == null)
            return NotFound();

        if (!_uploads.IsInlineSafeImage(normalizedKey))
            return NotFound();

        if (!_uploads.TryResolvePhysicalPath(normalizedKey, out var physicalPath))
            return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        var contentType = _uploads.GetContentType(normalizedKey, avatarOwner?.AvatarPathContentType ?? imageFile?.ContentType);
        return PhysicalFile(physicalPath, contentType);
    }

    [HttpGet("attachment")]
    public IActionResult GetAttachment([FromQuery] string key)
    {
        var normalizedKey = UploadStorage.NormalizeStorageKey(key);
        if (string.IsNullOrEmpty(normalizedKey))
            return BadRequest("key is required.");

        var attachment = _messages.FindAttachmentFile(normalizedKey);
        if (attachment == null)
            return NotFound();

        if (!_uploads.TryResolvePhysicalPath(normalizedKey, out var physicalPath))
            return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(
            physicalPath,
            _uploads.GetContentType(normalizedKey, attachment.ContentType),
            _uploads.SanitizeDownloadFileName(attachment.FileName));
    }
}
