using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Elmah.Io.OpenTelemetry.AspNetCore80.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // This won't get logged since Default LogLevel is set to Warning in Program.cs
            _logger.LogInformation("Request to the frontpage");
        }
    }
}
