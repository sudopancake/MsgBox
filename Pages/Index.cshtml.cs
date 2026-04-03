using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MsgBox.Pages;

public class IndexModel : PageModel
{
    private readonly IAntiforgery _antiforgery;

    public IndexModel(IAntiforgery antiforgery) => _antiforgery = antiforgery;

    public string AntiforgeryToken { get; private set; } = "";

    public void OnGet()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        AntiforgeryToken = tokens.RequestToken ?? "";
    }
}
