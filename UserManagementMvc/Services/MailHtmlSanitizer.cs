using Ganss.Xss;

namespace UserManagementMvc.Services;

public class MailHtmlSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public MailHtmlSanitizer()
    {
        _sanitizer = new HtmlSanitizer();

        _sanitizer.AllowedAttributes.Add("class");

        _sanitizer.AllowedSchemes.Add("mailto");
    }

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return "";
        }

        return _sanitizer.Sanitize(html);
    }
}