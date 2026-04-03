using Microsoft.AspNetCore.Mvc;
using MsgBox.Data.Models;
using MsgBox.Data.Repositories;

namespace MsgBox.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly SettingsRepository _settings;

    public SettingsController(SettingsRepository settings) => _settings = settings;

    [HttpGet]
    public ActionResult<ThemeResponseDto> Get()
    {
        var s = _settings.GetOrCreate();
        return new ThemeResponseDto
        {
            IsDark = s.Theme.IsDark,
            AccentColor = s.Theme.AccentColor
        };
    }

    [HttpPut("theme")]
    public ActionResult<ThemeResponseDto> PutTheme([FromBody] ThemeUpdateDto body)
    {
        var theme = new ThemeSettings
        {
            IsDark = body.IsDark,
            AccentColor = string.IsNullOrWhiteSpace(body.AccentColor) ? "blue" : body.AccentColor.Trim()
        };
        _settings.UpsertTheme(theme);
        return new ThemeResponseDto { IsDark = theme.IsDark, AccentColor = theme.AccentColor };
    }
}
